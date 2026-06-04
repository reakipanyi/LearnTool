using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace UnifiedLearningAssistant.Services.Cloud
{
    /// <summary>
    /// 百度网盘服务实现
    /// </summary>
    public class BaiduNetdiskService : ICloudStorageService
    {
        private const string AuthorizeUrl = "https://openapi.baidu.com/oauth/2.0/authorize";
        private const string TokenUrl = "https://openapi.baidu.com/oauth/2.0/token";
        private const string ListUrl = "https://pan.baidu.com/rest/2.0/xpan/file";
        private const string DownloadUrl = "https://d.pcs.baidu.com/rest/2.0/pcs/file";
        
        private readonly HttpClient _httpClient;
        private readonly ILogger<BaiduNetdiskService>? _logger;
        private string? _accessToken;
        private DateTime _tokenExpireTime;

        public string ServiceName => "百度网盘";
        public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && DateTime.Now < _tokenExpireTime;

        public BaiduNetdiskService(ILogger<BaiduNetdiskService>? logger = null)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<string> GetAuthorizationUrlAsync()
        {
            var query = new Dictionary<string, string>
            {
                { "response_type", "code" },
                { "client_id", "your_client_id" },
                { "redirect_uri", "oob" },
                { "scope", "basic,netdisk" },
                { "display", "popup" }
            };

            var queryString = string.Join("&", query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            return $"{AuthorizeUrl}?{queryString}";
        }

        public async Task<bool> AuthenticateAsync(string authCode)
        {
            try
            {
                var formData = new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "code", authCode },
                    { "client_id", "your_client_id" },
                    { "client_secret", "your_client_secret" },
                    { "redirect_uri", "oob" }
                };

                var content = new FormUrlEncodedContent(formData);
                var response = await _httpClient.PostAsync(TokenUrl, content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var tokenResult = JsonSerializer.Deserialize<TokenResponse>(json);

                if (tokenResult != null && !string.IsNullOrEmpty(tokenResult.AccessToken))
                {
                    _accessToken = tokenResult.AccessToken;
                    _tokenExpireTime = DateTime.Now.AddSeconds(tokenResult.ExpiresIn);
                    _logger?.LogInformation("百度网盘授权成功");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "百度网盘授权失败");
            }

            return false;
        }

        public async Task<bool> DownloadFileAsync(string cloudPath, string localPath, Action<int>? progress = null)
        {
            try
            {
                EnsureAuthenticated();

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
            try
            {
                EnsureAuthenticated();

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
            var result = new List<CloudFileInfo>();

            try
            {
                EnsureAuthenticated();

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
            try
            {
                EnsureAuthenticated();

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
            try
            {
                EnsureAuthenticated();

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
            try
            {
                EnsureAuthenticated();

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

        private void EnsureAuthenticated()
        {
            if (!IsAuthenticated)
            {
                throw new InvalidOperationException("尚未进行百度网盘授权");
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

        private class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;

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
    }
}
