using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace LearningAssistant.Baidu
{
    /// <summary>
    /// 百度网盘授权管理类（支持三种授权模式+Token刷新）
    /// </summary>
    /// <summary>
    /// 百度网盘授权码模式专用授权类（严格遵循官方GET请求规范）
    /// </summary>
    public class BaiduPanAuthCodeManager : IDisposable
    {
        #region 常量定义
        /// <summary>
        /// 授权基础地址
        /// </summary>
        private const string AuthBaseUrl = "https://openapi.baidu.com/oauth/2.0";

        /// <summary>
        /// Code有效期（官方定义10分钟，单位：秒）
        /// </summary>
        private const int CodeExpireSeconds = 600;

        /// <summary>
        /// Token有效期（官方定义30天，单位：秒）
        /// </summary>
        private const int TokenExpireSeconds = 2592000;
        #endregion

        #region 数据模型
        /// <summary>
        /// 授权码模式应用配置
        /// </summary>
        public class AuthCodeConfig
        {
            /// <summary>应用AppKey（从开放平台获取）</summary>
            public string ClientId { get; set; }

            /// <summary>应用SecretKey（从开放平台获取）</summary>
            public string ClientSecret { get; set; }

            /// <summary>授权回调地址（需与开放平台配置一致）</summary>
            public string RedirectUri { get; set; }

            /// <summary>授权范围（官方固定值：basic,netdisk）</summary>
            public string Scope { get; set; } = "basic,netdisk";

            /// <summary>设备ID（硬件应用必填）</summary>
            public string DeviceId { get; set; }
        }

        /// <summary>
        /// Token响应模型（换取/刷新Token返回）
        /// </summary>
        public class AuthTokenResponse
        {
            /// <summary>访问令牌</summary>
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            /// <summary>Token有效期（秒）</summary>
            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; } = TokenExpireSeconds;

            /// <summary>刷新令牌（有效期10年）</summary>
            [JsonProperty("refresh_token")]
            public string RefreshToken { get; set; }

            /// <summary>Session密钥</summary>
            [JsonProperty("session_secret")]
            public string SessionSecret { get; set; }

            /// <summary>SessionKey</summary>
            [JsonProperty("session_key")]
            public string SessionKey { get; set; }

            /// <summary>授权范围</summary>
            [JsonProperty("scope")]
            public string Scope { get; set; }

            /// <summary>错误码（失败时返回）</summary>
            [JsonProperty("error")]
            public string Error { get; set; }

            /// <summary>错误描述（失败时返回）</summary>
            [JsonProperty("error_description")]
            public string ErrorDescription { get; set; }

            /// <summary>Token创建时间（本地记录）</summary>
            [JsonIgnore]
            public DateTime CreateTime { get; set; } = DateTime.Now;

            /// <summary>是否过期（提前60秒判定）</summary>
            [JsonIgnore]
            public bool IsExpired => DateTime.Now > CreateTime.AddSeconds(ExpiresIn - 60);
        }

        /// <summary>
        /// 授权码模式异常
        /// </summary>
        public class BaiduPanAuthCodeException : Exception
        {
            /// <summary>错误码</summary>
            public string ErrorCode { get; }

            public BaiduPanAuthCodeException(string errorCode, string message) : base(message)
            {
                ErrorCode = errorCode;
            }
        }
        #endregion

        #region 私有字段
        private readonly HttpClient _httpClient;
        private readonly AuthCodeConfig _config;
        private AuthTokenResponse _currentToken; // 当前Token缓存
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化授权码模式管理器
        /// </summary>
        /// <param name="config">应用配置</param>
        /// <param name="timeout">请求超时时间（默认30秒）</param>
        public BaiduPanAuthCodeManager(AuthCodeConfig config, int timeout = 30)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            ValidateConfig();

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(timeout)
            };
            // 设置官方要求的User-Agent
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("pan.baidu.com");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        #endregion

        #region 核心授权方法（严格按官方GET规范实现）
        /// <summary>
        /// 生成授权码Code请求URL（引导用户授权）
        /// </summary>
        /// <param name="display">授权页面展示样式（可选）</param>
        /// <param name="state">防CSRF攻击参数（可选）</param>
        /// <param name="qrcode">是否展示二维码登录（1=展示）</param>
        /// <param name="forceLogin">是否强制重新登录（1=强制）</param>
        /// <returns>授权URL</returns>
        public string GenerateCodeRequestUrl(
            string display = null,
            string state = null,
            int qrcode = 0,
            int forceLogin = 0)
        {
            var urlBuilder = new StringBuilder($"{AuthBaseUrl}/authorize?");
            // 必选参数
            urlBuilder.Append($"response_type=code");
            urlBuilder.Append($"&client_id={Uri.EscapeDataString(_config.ClientId)}");
            urlBuilder.Append($"&redirect_uri={Uri.EscapeDataString(_config.RedirectUri)}");
            urlBuilder.Append($"&scope={Uri.EscapeDataString(_config.Scope)}");

            // 可选参数
            if (!string.IsNullOrWhiteSpace(_config.DeviceId))
                urlBuilder.Append($"&device_id={Uri.EscapeDataString(_config.DeviceId)}");
            if (!string.IsNullOrWhiteSpace(display))
                urlBuilder.Append($"&display={Uri.EscapeDataString(display)}");
            if (!string.IsNullOrWhiteSpace(state))
                urlBuilder.Append($"&state={Uri.EscapeDataString(state)}");
            if (qrcode == 1)
                urlBuilder.Append($"&qrcode=1");
            if (forceLogin == 1)
                urlBuilder.Append($"&force_login=1");

            return urlBuilder.ToString();
        }

        /// <summary>
        /// 换取Access Token（官方GET请求）
        /// </summary>
        /// <param name="code">用户授权后返回的Code（10分钟有效期，仅一次有效）</param>
        /// <returns>Token信息</returns>
        public async Task<AuthTokenResponse> ExchangeCodeForTokenAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentNullException(nameof(code), "授权码Code不能为空");

            // 构建GET请求URL（严格按官方参数顺序）
            var urlBuilder = new StringBuilder($"{AuthBaseUrl}/token?");
            urlBuilder.Append($"grant_type=authorization_code");
            urlBuilder.Append($"&code={Uri.EscapeDataString(code)}");
            urlBuilder.Append($"&client_id={Uri.EscapeDataString(_config.ClientId)}");
            urlBuilder.Append($"&client_secret={Uri.EscapeDataString(_config.ClientSecret)}");
            urlBuilder.Append($"&redirect_uri={Uri.EscapeDataString(_config.RedirectUri)}");

            // 发送GET请求
            var tokenResponse = await SendGetTokenRequestAsync(urlBuilder.ToString());
            _currentToken = tokenResponse;
            return tokenResponse;
        }

        /// <summary>
        /// 刷新Access Token（官方GET请求）
        /// </summary>
        /// <param name="refreshToken">上一次返回的RefreshToken（仅一次有效）</param>
        /// <returns>新的Token信息</returns>
        public async Task<AuthTokenResponse> RefreshAccessTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ArgumentNullException(nameof(refreshToken), "RefreshToken不能为空");

            // 构建GET请求URL（严格按官方参数顺序）
            var urlBuilder = new StringBuilder($"{AuthBaseUrl}/token?");
            urlBuilder.Append($"grant_type=refresh_token");
            urlBuilder.Append($"&refresh_token={Uri.EscapeDataString(refreshToken)}");
            urlBuilder.Append($"&client_id={Uri.EscapeDataString(_config.ClientId)}");
            urlBuilder.Append($"&client_secret={Uri.EscapeDataString(_config.ClientSecret)}");

            try
            {
                // 发送GET请求
                var tokenResponse = await SendGetTokenRequestAsync(urlBuilder.ToString());
                _currentToken = tokenResponse;
                return tokenResponse;
            }
            catch (BaiduPanAuthCodeException)
            {
                // 刷新失败：旧RefreshToken失效，清空缓存，需重新授权
                _currentToken = null;
                throw;
            }
        }

        /// <summary>
        /// 获取当前有效AccessToken（自动刷新）
        /// </summary>
        /// <returns>有效的AccessToken</returns>
        public async Task<string> GetValidAccessTokenAsync()
        {
            if (_currentToken == null)
                throw new InvalidOperationException("未获取到Token，请先通过Code换取");

            // 未过期直接返回
            if (!_currentToken.IsExpired)
                return _currentToken.AccessToken;

            // 过期则刷新
            return (await RefreshAccessTokenAsync(_currentToken.RefreshToken)).AccessToken;
        }
        #endregion

        #region 私有核心方法
        /// <summary>
        /// 验证应用配置
        /// </summary>
        private void ValidateConfig()
        {
            if (string.IsNullOrWhiteSpace(_config.ClientId))
                throw new InvalidOperationException("ClientId（AppKey）不能为空");

            if (string.IsNullOrWhiteSpace(_config.ClientSecret))
                throw new InvalidOperationException("ClientSecret（SecretKey）不能为空");

            if (string.IsNullOrWhiteSpace(_config.RedirectUri))
                throw new InvalidOperationException("RedirectUri（回调地址）不能为空");

            if (_config.Scope != "basic,netdisk")
                throw new InvalidOperationException("Scope必须为固定值：basic,netdisk");
        }

        /// <summary>
        /// 发送GET请求获取Token（通用方法）
        /// </summary>
        /// <param name="url">完整的GET请求URL</param>
        /// <returns>Token响应</returns>
        private async Task<AuthTokenResponse> SendGetTokenRequestAsync(string url)
        {
            try
            {
                // 发送GET请求（严格遵循官方GET方式）
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // 解析响应
                var responseJson = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<AuthTokenResponse>(responseJson);

                // 处理错误响应
                if (!string.IsNullOrWhiteSpace(tokenResponse.Error))
                {
                    throw new BaiduPanAuthCodeException(
                        tokenResponse.Error,
                        tokenResponse.ErrorDescription ?? "Token请求失败");
                }

                // 记录Token创建时间
                tokenResponse.CreateTime = DateTime.Now;
                return tokenResponse;
            }
            catch (BaiduPanAuthCodeException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Token请求异常：{ex.Message}", ex);
            }
        }
        #endregion

        #region 资源释放
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
        #endregion
    }
}
