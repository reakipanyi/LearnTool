using LearningAssistant.Models.Config;
using LearningAssistant.Services.Persistence;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LearningAssistant.Services.Cloud
{
    /// <summary>
    /// 百度网盘服务实现
    /// </summary>
    public class BaiduNetdiskService : ICloudStorageService, IDisposable
    {
        private const string AuthorizeUrl = "https://openapi.baidu.com/oauth/2.0/authorize";
        private const string TokenUrl = "https://openapi.baidu.com/oauth/2.0/token";
        private const string ListUrl = "https://pan.baidu.com/rest/2.0/xpan/file";
        private const string DownloadUrl = "https://d.pcs.baidu.com/rest/2.0/pcs/file";

        private readonly HttpClient _httpClient;
        private readonly ILogger<BaiduNetdiskService>? _logger;
        private readonly CloudStorageConfig _config;
        private readonly IDataPersistenceService? _persistenceService;
        private string? _accessToken;
        private string? _refreshToken;
        private DateTime _tokenExpireTime;
        private bool _disposed = false;

        #region 公共属性

        public string ServiceName => "百度网盘";
        public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && DateTime.Now < _tokenExpireTime;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_config.BaiduClientId) && !string.IsNullOrWhiteSpace(_config.BaiduClientSecret);

        #endregion

        public BaiduNetdiskService(CloudStorageConfig config, ILogger<BaiduNetdiskService>? logger = null, IDataPersistenceService? persistenceService = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger;
            _persistenceService = persistenceService;

            _accessToken = !string.IsNullOrWhiteSpace(_config.BaiduAccessToken) ? _config.BaiduAccessToken : null;
            _refreshToken = !string.IsNullOrWhiteSpace(_config.BaiduRefreshToken) ? _config.BaiduRefreshToken : null;
            _tokenExpireTime = _config.BaiduTokenExpireTime ?? DateTime.MinValue;

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }


        #region 配置方法

        public void Configure(string clientId, string clientSecret)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client ID cannot be null or empty", nameof(clientId));
            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new ArgumentException("Client Secret cannot be null or empty", nameof(clientSecret));

            _config.BaiduClientId = clientId;
            _config.BaiduClientSecret = clientSecret;
            _logger?.LogInformation("百度网盘服务配置已更新");
        }

        #endregion

        #region Dispose 模式

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _httpClient?.Dispose();
                _logger?.LogInformation("BaiduNetdiskService disposed");
            }

            _disposed = true;
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BaiduNetdiskService));
        }

        #endregion

        #region 授权与认证

        public async Task<string> GetAuthorizationUrlAsync()
        {
            return await GetAuthorizationUrlAsync(null, true, false);
        }

        public async Task<string> GetAuthorizationUrlAsync(string? state = null, bool showQrcode = true, bool forceLogin = false)
        {
            CheckDisposed();

            if (!IsConfigured)
            {
                _logger?.LogWarning("尝试获取授权 URL 但服务未配置");
                throw new InvalidOperationException("百度网盘服务未配置，请先配置 Client ID 和 Client Secret");
            }

            var query = new Dictionary<string, string>
            {
                { "response_type", "code" },
                { "client_id", _config.BaiduClientId! },
                { "redirect_uri", "oob" },
                { "scope", "basic,netdisk" },
                { "display", "popup" },
                { "qrcode", showQrcode ? "1" : "0" },
                { "force_login", forceLogin ? "1" : "0" }
            };

            if (!string.IsNullOrWhiteSpace(state))
            {
                query.Add("state", state);
            }

            var queryString = BuildQueryString(query);
            _logger?.LogInformation("生成百度网盘授权 URL");
            return $"{AuthorizeUrl}?{queryString}";
        }

        public async Task<bool> RefreshAccessTokenAsync()
        {
            CheckDisposed();

            if (!IsConfigured)
            {
                _logger?.LogWarning("尝试刷新 Token 但服务未配置");
                return false;
            }

            if (string.IsNullOrEmpty(_refreshToken))
            {
                _logger?.LogWarning("没有可用的 Refresh Token");
                return false;
            }

            try
            {
                _logger?.LogInformation("开始刷新 Access Token");

                var formData = new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "refresh_token", _refreshToken },
                    { "client_id", _config.BaiduClientId! },
                    { "client_secret", _config.BaiduClientSecret! }
                };

                var content = new FormUrlEncodedContent(formData);
                var response = await _httpClient.PostAsync(TokenUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("刷新 Token 请求失败: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                var tokenResult = JsonSerializer.Deserialize<TokenResponse>(json);

                if (tokenResult != null && !string.IsNullOrEmpty(tokenResult.AccessToken))
                {
                    _accessToken = tokenResult.AccessToken;
                    _refreshToken = tokenResult.RefreshToken;
                    _tokenExpireTime = DateTime.Now.AddSeconds(tokenResult.ExpiresIn);
                    SaveTokens();
                    await TryPersistConfigAsync();
                    _logger?.LogInformation("Token 刷新成功，新令牌有效期: {ExpiresIn} 秒", tokenResult.ExpiresIn);
                    return true;
                }

                _logger?.LogWarning("刷新 Token 响应无效: {Json}", json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "刷新 Token 失败");
            }

            return false;
        }

        public async Task<string?> GetValidAccessTokenAsync()
        {
            CheckDisposed();

            if (!IsConfigured)
            {
                _logger?.LogWarning("尝试获取有效 Token 但服务未配置");
                return null;
            }

            if (IsAuthenticated)
            {
                return _accessToken;
            }

            if (!string.IsNullOrEmpty(_refreshToken))
            {
                if (await RefreshAccessTokenAsync())
                {
                    return _accessToken;
                }
            }

            _logger?.LogWarning("无法获取有效 Access Token，请重新授权");
            return null;
        }

        public async Task<bool> AuthenticateAsync(string authCode)
        {
            CheckDisposed();

            if (!IsConfigured)
            {
                _logger?.LogWarning("尝试授权但服务未配置");
                return false;
            }

            try
            {
                _logger?.LogInformation("开始百度网盘授权流程");

                var formData = new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "code", authCode },
                    { "client_id", _config.BaiduClientId! },
                    { "client_secret", _config.BaiduClientSecret! },
                    { "redirect_uri", "oob" }
                };

                var content = new FormUrlEncodedContent(formData);
                var response = await _httpClient.PostAsync(TokenUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("百度网盘授权请求失败: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                var tokenResult = JsonSerializer.Deserialize<TokenResponse>(json);

                if (tokenResult != null && !string.IsNullOrEmpty(tokenResult.AccessToken))
                {
                    _accessToken = tokenResult.AccessToken;
                    _refreshToken = tokenResult.RefreshToken;
                    _tokenExpireTime = DateTime.Now.AddSeconds(tokenResult.ExpiresIn);
                    SaveTokens();
                    await TryPersistConfigAsync();
                    _logger?.LogInformation("百度网盘授权成功，令牌有效期: {ExpiresIn} 秒", tokenResult.ExpiresIn);
                    return true;
                }

                _logger?.LogWarning("百度网盘授权响应无效: {Json}", json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "百度网盘授权失败");
            }

            return false;
        }



        #endregion

        #region 文件操作

        public async Task<bool> DownloadFileAsync(string cloudPath, string localPath, Action<int>? progress = null)
        {
            CheckDisposed();

            try
            {
                await EnsureAuthenticatedAsync();

                _logger?.LogInformation("开始下载文件: {CloudPath} -> {LocalPath}", cloudPath, localPath);

                var query = new Dictionary<string, string>
                {
                    { "method", "download" },
                    { "path", cloudPath },
                    { "access_token", _accessToken! }
                };

                var url = $"{DownloadUrl}?{BuildQueryString(query)}";
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(localPath, FileMode.Create);

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var buffer = new byte[8192];
                long bytesRead = 0;
                int read;

                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    bytesRead += read;
                    progress?.Invoke(totalBytes > 0 ? (int)(bytesRead * 100 / totalBytes) : 0);
                }

                _logger?.LogInformation("文件下载成功: {Path}", cloudPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "文件下载失败: {Path}", cloudPath);
                return false;
            }
        }

        public async Task<bool> UploadFileAsync(string localPath, string cloudPath, Action<int>? progress = null)
        {
            CheckDisposed();

            try
            {
                await EnsureAuthenticatedAsync();

                if (!File.Exists(localPath))
                {
                    _logger?.LogError("本地文件不存在: {Path}", localPath);
                    return false;
                }

                _logger?.LogInformation("开始上传文件: {LocalPath} -> {CloudPath}", localPath, cloudPath);

                var uploadUrl = await GetUploadUrlAsync();
                if (string.IsNullOrEmpty(uploadUrl))
                {
                    _logger?.LogError("获取上传地址失败");
                    return false;
                }

                using var fileStream = new FileStream(localPath, FileMode.Open);
                var content = new MultipartFormDataContent();
                content.Add(new StreamContent(fileStream), "file", Path.GetFileName(localPath));
                content.Add(new StringContent(cloudPath), "path");

                var response = await _httpClient.PostAsync(uploadUrl, content);
                response.EnsureSuccessStatusCode();

                _logger?.LogInformation("文件上传成功: {Path}", cloudPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "文件上传失败: {Path}", cloudPath);
                return false;
            }
        }

        public async Task<List<CloudFileInfo>> ListFilesAsync(string cloudFolder)
        {
            CheckDisposed();

            var result = new List<CloudFileInfo>();

            try
            {
                await EnsureAuthenticatedAsync();

                _logger?.LogInformation("获取文件列表: {Folder}", cloudFolder);

                var query = new Dictionary<string, string>
                {
                    { "method", "list" },
                    { "path", cloudFolder },
                    { "access_token", _accessToken! },
                    { "order", "time" },
                    { "desc", "1" },
                    { "web", "1" }
                };

                var url = $"{ListUrl}?{BuildQueryString(query)}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var listResult = JsonSerializer.Deserialize<ListResponse>(json);

                if (listResult?.List != null)
                {
                    foreach (var item in listResult.List)
                    {
                        result.Add(new CloudFileInfo
                        {
                            Path = item.Path,
                            Name = item.ServerFilename,
                            Size = item.Size,
                            ModifiedTime = DateTimeOffset.FromUnixTimeSeconds(item.Mtime).DateTime,
                            IsFolder = item.IsDir == 1
                        });
                    }
                    _logger?.LogInformation("获取到 {Count} 个文件/文件夹", result.Count);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取文件列表失败: {Folder}", cloudFolder);
            }

            return result;
        }

        public async Task<bool> FileExistsAsync(string cloudPath)
        {
            CheckDisposed();

            try
            {
                await EnsureAuthenticatedAsync();

                var query = new Dictionary<string, string>
                {
                    { "method", "meta" },
                    { "path", cloudPath },
                    { "access_token", _accessToken! }
                };

                var url = $"{ListUrl}?{BuildQueryString(query)}";
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "检查文件存在失败: {Path}", cloudPath);
                return false;
            }
        }

        public async Task<bool> DeleteFileAsync(string cloudPath)
        {
            CheckDisposed();

            try
            {
                await EnsureAuthenticatedAsync();

                _logger?.LogInformation("删除文件: {Path}", cloudPath);

                var query = new Dictionary<string, string>
                {
                    { "method", "delete" },
                    { "path", cloudPath },
                    { "access_token", _accessToken! }
                };

                var url = $"{ListUrl}?{BuildQueryString(query)}";
                var response = await _httpClient.PostAsync(url, null);
                response.EnsureSuccessStatusCode();

                _logger?.LogInformation("文件删除成功: {Path}", cloudPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "文件删除失败: {Path}", cloudPath);
                return false;
            }
        }

        public async Task<bool> CreateFolderAsync(string cloudFolderPath)
        {
            CheckDisposed();

            try
            {
                await EnsureAuthenticatedAsync();

                _logger?.LogInformation("创建文件夹: {Path}", cloudFolderPath);

                var query = new Dictionary<string, string>
                {
                    { "method", "mkdir" },
                    { "path", cloudFolderPath },
                    { "access_token", _accessToken! }
                };

                var url = $"{ListUrl}?{BuildQueryString(query)}";
                var response = await _httpClient.PostAsync(url, null);
                response.EnsureSuccessStatusCode();

                _logger?.LogInformation("文件夹创建成功: {Path}", cloudFolderPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "文件夹创建失败: {Path}", cloudFolderPath);
                return false;
            }
        }

        #endregion

        #region 私有辅助方法

        private void SaveTokens()
        {
            _config.BaiduAccessToken = _accessToken ?? "";
            _config.BaiduRefreshToken = _refreshToken ?? "";
            _config.BaiduTokenExpireTime = _tokenExpireTime;
            _logger?.LogInformation("Tokens saved to config");
        }

        private async Task TryPersistConfigAsync()
        {
            try
            {
                if (_persistenceService != null)
                {
                    var fullConfig = _persistenceService.LoadConfig();
                    if (fullConfig.CloudStorageConfig != null)
                    {
                        fullConfig.CloudStorageConfig.BaiduAccessToken = _accessToken ?? "";
                        fullConfig.CloudStorageConfig.BaiduRefreshToken = _refreshToken ?? "";
                        fullConfig.CloudStorageConfig.BaiduTokenExpireTime = _tokenExpireTime;
                        _persistenceService.SaveConfig(fullConfig);
                        _logger?.LogInformation("Config persisted successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to persist config to file");
            }
        }

        private async Task EnsureAuthenticatedAsync()
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException("百度网盘服务未配置，请先配置 Client ID 和 Client Secret");
            }

            if (!IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(_refreshToken))
                {
                    if (!await RefreshAccessTokenAsync())
                    {
                        throw new InvalidOperationException("Token 刷新失败，请重新授权");
                    }
                }
                else
                {
                    throw new InvalidOperationException("尚未进行百度网盘授权，请先完成授权");
                }
            }
        }

        private async Task<string?> GetUploadUrlAsync()
        {
            var query = new Dictionary<string, string>
            {
                { "method", "upload" },
                { "access_token", _accessToken! }
            };

            var url = $"{ListUrl}?{BuildQueryString(query)}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<UploadResponse>(json);
                return result?.Url;
            }

            return null;
        }

        private string BuildQueryString(Dictionary<string, string> parameters)
        {
            return string.Join("&", parameters.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        }

        #endregion

        #region 内部响应模型

        private class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;

            [JsonPropertyName("refresh_token")]
            public string RefreshToken { get; set; } = string.Empty;

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; } = 3600;
        }

        private class ListResponse
        {
            [JsonPropertyName("list")]
            public List<ListItem>? List { get; set; }
        }

        private class ListItem
        {
            [JsonPropertyName("path")]
            public string Path { get; set; } = string.Empty;

            [JsonPropertyName("server_filename")]
            public string ServerFilename { get; set; } = string.Empty;

            [JsonPropertyName("size")]
            public long Size { get; set; }

            [JsonPropertyName("mtime")]
            public long Mtime { get; set; }

            [JsonPropertyName("isdir")]
            public int IsDir { get; set; }
        }

        private class UploadResponse
        {
            [JsonPropertyName("url")]
            public string Url { get; set; } = string.Empty;
        }

        #endregion
    }
}
