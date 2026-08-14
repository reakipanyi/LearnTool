using LearningAssistant.Baidu;
using LearningAssistant.Common;
using LearningAssistant.Models.Config;
using LearningAssistant.Services.Persistence;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.PanAnalysis;

/// <summary>
/// 百度网盘 Token 管理器（线程安全）
/// </summary>
public interface IPanTokenManager
{
    /// <summary>当前 Access Token</summary>
    string? AccessToken { get; }

    /// <summary>Token 是否有效</summary>
    bool IsTokenValid { get; }

    /// <summary>确保 Token 有效（过期自动刷新）</summary>
    Task<string> EnsureValidTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>强制刷新 Token</summary>
    Task<string> RefreshTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>从配置重新加载 Token（授权成功后调用，使内存缓存同步到最新凭据）</summary>
    void ReloadFromConfig();

    /// <summary>Token 状态变化事件</summary>
    event EventHandler<TokenStateChangedEventArgs>? TokenStateChanged;
}

public class TokenStateChangedEventArgs : EventArgs
{
    public TokenState OldState { get; set; }
    public TokenState NewState { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum TokenState
{
    Uninitialized,
    Valid,
    Expired,
    Refreshing,
    RefreshFailed,
    Invalid
}

/// <summary>
/// Token 管理器实现
/// </summary>
public class PanTokenManager : IPanTokenManager
{
    private readonly CloudStorageConfig _config;
    private readonly IDataPersistenceService _dataPersistence;
    private readonly ILogger<PanTokenManager> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _cachedToken;
    private DateTime? _tokenExpireTime;
    private TokenState _state = TokenState.Uninitialized;

    public event EventHandler<TokenStateChangedEventArgs>? TokenStateChanged;

    public string? AccessToken => _cachedToken;

    public bool IsTokenValid =>
        !string.IsNullOrEmpty(_cachedToken) &&
        _tokenExpireTime.HasValue &&
        _tokenExpireTime.Value > DateTime.UtcNow.AddMinutes(5);

    public PanTokenManager(
        CloudStorageConfig config,
        IDataPersistenceService dataPersistence,
        ILogger<PanTokenManager> logger)
    {
        _config = config;
        _dataPersistence = dataPersistence;
        _logger = logger;

        // 从配置加载 Token
        _cachedToken = string.IsNullOrWhiteSpace(config.BaiduAccessToken) ? null : config.BaiduAccessToken;
        _tokenExpireTime = config.BaiduTokenExpireTime;
        _state = IsTokenValid ? TokenState.Valid : (string.IsNullOrEmpty(_cachedToken) ? TokenState.Invalid : TokenState.Expired);
    }

    public async Task<string> EnsureValidTokenAsync(CancellationToken cancellationToken = default)
    {
        if (IsTokenValid)
            return _cachedToken!;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // 双重检查
            if (IsTokenValid)
                return _cachedToken!;

            return await RefreshTokenInternalAsync(cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<string> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            return await RefreshTokenInternalAsync(cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// 从配置重新加载 Token。
    /// 授权成功后，OAuth 流程会把新凭据写入共享的 CloudStorageConfig，
    /// 调用此方法使内存缓存同步到最新凭据。
    /// </summary>
    public void ReloadFromConfig()
    {
        _cachedToken = string.IsNullOrWhiteSpace(_config.BaiduAccessToken) ? null : _config.BaiduAccessToken;
        _tokenExpireTime = _config.BaiduTokenExpireTime;
        var oldState = _state;
        _state = IsTokenValid ? TokenState.Valid : (string.IsNullOrEmpty(_cachedToken) ? TokenState.Invalid : TokenState.Expired);
        if (oldState != _state)
        {
            OnStateChanged(oldState, _state);
        }
        _logger.LogInformation("已从配置重新加载百度网盘 Token，状态：{State}", _state);
    }

    private async Task<string> RefreshTokenInternalAsync(CancellationToken cancellationToken)
    {
        var oldState = _state;
        _state = TokenState.Refreshing;
        OnStateChanged(oldState, _state);

        try
        {
            if (string.IsNullOrEmpty(_config.BaiduRefreshToken))
                throw new PanAuthException("未找到 RefreshToken，请重新授权");

            _logger.LogInformation("正在刷新百度网盘 AccessToken...");

            // 调用现有授权管理器
            var authConfig = new BaiduPanAuthCodeManager.AuthCodeConfig
            {
                ClientId = _config.BaiduClientId,
                ClientSecret = _config.BaiduClientSecret,
                RedirectUri = "oob"
            };

            using var authManager = new BaiduPanAuthCodeManager(authConfig);
            var tokenResponse = await authManager.RefreshAccessTokenAsync(_config.BaiduRefreshToken);
            cancellationToken.ThrowIfCancellationRequested();

            var accessToken = tokenResponse.AccessToken;
            var refreshToken = tokenResponse.RefreshToken;
            var expiresIn = tokenResponse.ExpiresIn;

            // 更新配置
            _cachedToken = accessToken;
            _tokenExpireTime = DateTime.UtcNow.AddSeconds(Math.Max(expiresIn - 300, 60)); // 提前5分钟过期
            _config.BaiduAccessToken = accessToken;
            _config.BaiduRefreshToken = string.IsNullOrEmpty(refreshToken) ? _config.BaiduRefreshToken : refreshToken;
            _config.BaiduTokenExpireTime = _tokenExpireTime.Value;

            // 持久化
            await PersistConfigAsync();

            _state = TokenState.Valid;
            OnStateChanged(TokenState.Refreshing, _state);

            _logger.LogInformation("AccessToken 刷新成功，有效期至 {ExpireTime}", _tokenExpireTime);

            return accessToken;
        }
        catch (PanAuthException)
        {
            _state = TokenState.RefreshFailed;
            OnStateChanged(TokenState.Refreshing, _state, "未找到 RefreshToken");
            throw;
        }
        catch (Exception ex)
        {
            _state = TokenState.RefreshFailed;
            OnStateChanged(TokenState.Refreshing, _state, ex.Message);

            _logger.LogError(ex, "AccessToken 刷新失败");
            throw new PanAuthException($"Token 刷新失败：{ex.Message}", ex);
        }
    }

    private async Task PersistConfigAsync()
    {
        try
        {
            var fullConfig = _dataPersistence.LoadConfig();
            if (fullConfig.CloudStorageConfig != null)
            {
                fullConfig.CloudStorageConfig.BaiduAccessToken = _config.BaiduAccessToken;
                fullConfig.CloudStorageConfig.BaiduRefreshToken = _config.BaiduRefreshToken;
                fullConfig.CloudStorageConfig.BaiduTokenExpireTime = _config.BaiduTokenExpireTime;
                _dataPersistence.SaveConfig(fullConfig);
                _logger.LogInformation("百度网盘 Token 配置已持久化");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "持久化百度网盘 Token 配置失败");
        }
    }

    private void OnStateChanged(TokenState oldState, TokenState newState, string? errorMessage = null)
    {
        TokenStateChanged?.Invoke(this, new TokenStateChangedEventArgs
        {
            OldState = oldState,
            NewState = newState,
            ErrorMessage = errorMessage
        });
    }
}
