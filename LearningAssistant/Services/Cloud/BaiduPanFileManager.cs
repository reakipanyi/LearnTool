using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DotnetSpider.Sample.samples
{
    public class BaiduPanApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessToken;
        private readonly ILogger<BaiduPanApiClient>? _logger;
        private const string BaseUrl = "https://pan.baidu.com";

        // 构造函数初始化
        public BaiduPanApiClient(string accessToken, ILogger<BaiduPanApiClient>? logger = null)
        {
            _accessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
            _logger = logger;
            _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("pan.baidu.com");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        #region 1. 获取文件列表（list接口）
        public async Task<ListFileResponse> GetFileListAsync(
            string dir = "/",
            string order = "name",
            int desc = 0,
            int start = 0,
            int limit = 1000,
            int web = 0,
            int folder = 0,
            int showempty = 0)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["method"] = "list",
                ["access_token"] = _accessToken,
                ["dir"] = Uri.EscapeDataString(dir),
                ["order"] = order,
                ["desc"] = desc.ToString(),
                ["start"] = start.ToString(),
                ["limit"] = limit.ToString(),
                ["web"] = web.ToString(),
                ["folder"] = folder.ToString(),
                ["showempty"] = showempty.ToString()
            };

            var url = $"/rest/2.0/xpan/file?{BuildQueryString(queryParams)}";
            return await SendGetRequestAsync<ListFileResponse>(url);
        }
        #endregion

        #region 2. 递归获取文件列表（listall接口）
        public async Task<ListAllFileResponse> GetFileListRecursiveAsync(
            string path = "/",
            int recursion = 0,
            string order = "time",
            int desc = 0,
            int start = 0,
            int limit = 1000,
            long ctime = 0,
            long mtime = 0,
            int web = 0,
            string deviceId = null)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["method"] = "listall",
                ["access_token"] = _accessToken,
                ["path"] = Uri.EscapeDataString(path),
                ["recursion"] = recursion.ToString(),
                ["order"] = order,
                ["desc"] = desc.ToString(),
                ["start"] = start.ToString(),
                ["limit"] = limit.ToString(),
                ["web"] = web.ToString()
            };

            if (ctime > 0) queryParams.Add("ctime", ctime.ToString());
            if (mtime > 0) queryParams.Add("mtime", mtime.ToString());
            if (!string.IsNullOrEmpty(deviceId)) queryParams.Add("device_id", deviceId);

            var url = $"/rest/2.0/xpan/multimedia?{BuildQueryString(queryParams)}";
            return await SendGetRequestAsync<ListAllFileResponse>(url);
        }
        #endregion

        #region 3. 获取文档列表（doclist接口）
        public async Task<DocListResponse> GetDocListAsync(
            string parentPath = "/",
            int? page = null,
            int num = 1000,
            string order = "time",
            int desc = 1,
            int recursion = 0,
            int web = 0)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["method"] = "doclist",
                ["access_token"] = _accessToken,
                ["parent_path"] = Uri.EscapeDataString(parentPath),
                ["order"] = order,
                ["desc"] = desc.ToString(),
                ["recursion"] = recursion.ToString(),
                ["web"] = web.ToString(),
                ["num"] = num.ToString()
            };

            if (page.HasValue && page.Value >= 1) queryParams.Add("page", page.Value.ToString());

            var url = $"/rest/2.0/xpan/file?{BuildQueryString(queryParams)}";
            return await SendGetRequestAsync<DocListResponse>(url);
        }
        #endregion

        #region 4. 获取图片列表（imagelist接口）
        public async Task<ImageListResponse> GetImageListAsync(
            string parentPath = "/",
            int? page = null,
            int num = 1000,
            string order = "time",
            int desc = 1,
            int recursion = 0,
            int web = 0)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["method"] = "imagelist",
                ["access_token"] = _accessToken,
                ["parent_path"] = Uri.EscapeDataString(parentPath),
                ["order"] = order,
                ["desc"] = desc.ToString(),
                ["recursion"] = recursion.ToString(),
                ["web"] = web.ToString(),
                ["num"] = num.ToString()
            };

            if (page.HasValue && page.Value >= 1) queryParams.Add("page", page.Value.ToString());

            var url = $"/rest/2.0/xpan/file?{BuildQueryString(queryParams)}";
            return await SendGetRequestAsync<ImageListResponse>(url);
        }
        #endregion

        #region 5. 获取视频列表（videolist接口）
        public async Task<VideoListResponse> GetVideoListAsync(
            string parentPath = "/",
            int? page = null,
            int num = 1000,
            string order = "time",
            int desc = 1,
            int recursion = 0,
            int web = 1)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["method"] = "videolist",
                ["access_token"] = _accessToken,
                ["parent_path"] = Uri.EscapeDataString(parentPath),
                ["order"] = order,
                ["desc"] = desc.ToString(),
                ["recursion"] = recursion.ToString(),
                ["web"] = web.ToString(),
                ["num"] = num.ToString()
            };

            if (page.HasValue && page.Value >= 1) queryParams.Add("page", page.Value.ToString());

            var url = $"/rest/2.0/xpan/file?{BuildQueryString(queryParams)}";
            return await SendGetRequestAsync<VideoListResponse>(url);
        }
        #endregion

        #region 6. 获取分类文件数量（categoryinfo接口）

        public async Task<CategoryCountStats> GetCategoryFileCountAsync(
            FileCategory category,
            string parentPath = "",
            int recursion = 0)
        {
            if (!Enum.IsDefined(typeof(FileCategory), category))
                throw new ArgumentException("无效的文件类型", nameof(category));

            // 处理空路径逻辑
            string finalPath = string.IsNullOrWhiteSpace(parentPath) ? "/" : parentPath;
            int finalRecursion = string.IsNullOrWhiteSpace(parentPath) ? 1 : recursion;

            var queryParams = new Dictionary<string, string>
            {
                ["access_token"] = _accessToken,
                ["category"] = ((int)category).ToString(),
                ["parent_path"] = Uri.EscapeDataString(finalPath),
                ["recursion"] = finalRecursion.ToString()
            };

            var url = $"/api/categoryinfo?{BuildQueryString(queryParams)}";
            var response = await SendGetRequestAsync<CategoryCountResponse>(url);

            var categoryKey = ((int)category).ToString();
            return response.Info.TryGetValue(categoryKey, out var stats)
                ? stats
                : new CategoryCountStats { Count = 0, Size = 0, Total = 0 };
        }

        #endregion

        #region 7. 获取分类文件列表（categorylist接口）
        public async Task<CategoryFileResponse> GetCategoryFileListAsync(
            List<FileCategory> categories,
            string parentPath = "/",
            int recursion = 0,
            string ext = "",
            int start = 0,
            int limit = 1000,
            string order = "time",
            int desc = 1,
            int showDir = 0,
            string deviceId = null)
        {
            if (categories == null || categories.Count == 0)
                throw new ArgumentException("至少指定一种文件类型", nameof(categories));

            var categoryStr = string.Join(",", categories.Select(c => (int)c));
            var queryParams = new Dictionary<string, string>
            {
                ["method"] = "categorylist",
                ["access_token"] = _accessToken,
                ["category"] = categoryStr,
                ["parent_path"] = Uri.EscapeDataString(parentPath),
                ["recursion"] = recursion.ToString(),
                ["start"] = start.ToString(),
                ["limit"] = limit.ToString(),
                ["order"] = order,
                ["desc"] = desc.ToString(),
                ["show_dir"] = showDir.ToString()
            };

            if (!string.IsNullOrEmpty(ext)) queryParams.Add("ext", ext);
            if (!string.IsNullOrEmpty(deviceId)) queryParams.Add("device_id", deviceId);

            var url = $"/rest/2.0/xpan/multimedia?{BuildQueryString(queryParams)}";
            return await SendGetRequestAsync<CategoryFileResponse>(url);
        }
        #endregion

        #region 8. 查询文件信息（filemetas接口）
        public async Task<FileMetaResponse> GetFileMetaAsync(
            List<long> fsIds,
            int dlink = 0,
            string path = "",
            int thumb = 0,
            int extra = 0,
            int needmedia = 0,
            int detail = 0,
            string deviceId = null,
            int fromApaas = 0)
        {
            if (fsIds == null || fsIds.Count == 0 || fsIds.Count > 100)
                throw new ArgumentException("文件ID列表不能为空且不超过100个", nameof(fsIds));

            var fsIdsStr = JsonConvert.SerializeObject(fsIds);
            var queryParams = new Dictionary<string, string>
            {
                ["method"] = "filemetas",
                ["access_token"] = _accessToken,
                ["fsids"] = Uri.EscapeDataString(fsIdsStr),
                ["dlink"] = dlink.ToString(),
                ["thumb"] = thumb.ToString(),
                ["extra"] = extra.ToString(),
                ["needmedia"] = needmedia.ToString(),
                ["detail"] = detail.ToString(),
                ["from_apaas"] = fromApaas.ToString()
            };

            if (!string.IsNullOrEmpty(path)) queryParams.Add("path", Uri.EscapeDataString(path));
            if (!string.IsNullOrEmpty(deviceId)) queryParams.Add("device_id", deviceId);

            var url = $"/rest/2.0/xpan/multimedia?{BuildQueryString(queryParams)}";
            return await SendGetRequestAsync<FileMetaResponse>(url);
        }
        #endregion

        #region 9. 关键词搜索文件（search接口）
        public async Task<SearchFileResponse> SearchFileAsync(
            string key,
            string dir = "/",
            FileCategory? category = null,
            int recursion = 0,
            int web = 0,
            string deviceId = null)
        {
            if (string.IsNullOrEmpty(key) || key.Length > 30)
                throw new ArgumentException("搜索关键词不能为空且不超过30字符", nameof(key));

            var queryParams = new Dictionary<string, string>
            {
                ["method"] = "search",
                ["access_token"] = _accessToken,
                ["key"] = Uri.EscapeDataString(key),
                ["dir"] = Uri.EscapeDataString(dir),
                ["category"] = category == null ? "" : ((int)category).ToString(),
                ["recursion"] = recursion.ToString(),
                ["web"] = web.ToString(),
                ["num"] = "500" // 固定500，不可修改
            };
            //method String  是 search  URL参数 本接口固定为search
            //access_token String  是   12.a6b7dbd428f731035f771b8d15063f61.86400.1292922000 - 2346678 - 124328 URL参数 接口鉴权参数
            //key string 是   "day"   URL参数 搜索关键字，最大30字符（UTF8格式）
            //dir string 否   / 测试目录   URL参数 搜索目录，默认根目录
            //category    int 否   2   URL参数 文件类型，1 视频、2 音频、3 图片、4 文档、5 应用、6 其他、7 种子
            //num int 否   500 URL参数 默认为500，不能修改
            //recursion   int 否   1   URL参数 是否递归，带这个参数就会递归，否则不递归
            //web int 否   0   URL参数 是否展示缩略图信息，带这个参数会返回缩略图信息，否则不展示缩略图信息
            //device_id   string 否   104771607rs1607808 URL参数   设备ID，设备注册接口下发，硬件设备必传
            if (category.HasValue) queryParams.Add("category", ((int)category.Value).ToString());
            if (!string.IsNullOrEmpty(deviceId)) queryParams.Add("device_id", deviceId);

            var url = $"/rest/2.0/xpan/file?{BuildQueryString(queryParams)}";
            return await SendGetRequestAsync<SearchFileResponse>(url);
        }
        #endregion

        #region 10. 语义搜索文件（unisearch接口）
        public async Task<SemanticSearchResponse> SemanticSearchFileAsync(
            string query,
            List<string> dirs = null,
            List<FileCategory> categories = null,
            int num = 500,
            int stream = 0,
            SearchType searchType = SearchType.Auto,
            List<SearchSource> sources = null)
        {
            if (string.IsNullOrEmpty(query))
                throw new ArgumentException("搜索查询不能为空", nameof(query));

            var queryParams = new Dictionary<string, string>
            {
                ["access_token"] = _accessToken,
                ["scene"] = "mcpserver",
                ["query"] = Uri.EscapeDataString(query),
                ["num"] = num.ToString(),
                ["stream"] = stream.ToString(),
                ["search_type"] = ((int)searchType).ToString()
            };
            //access_token    string 是       鉴权access_token，获取方式参考：https://pan.baidu.com/union/doc/ol0rsap9s
            //scene   string 是       搜索场景，固定传mcpserver
            //query   string 是       搜索query
            //dir[]string 否   根目录"/"  指定路径搜索
            //category[]int 否[]，不指定文件类型 文件类型。1 - 视频、2 - 音频、3 - 图片、4 - 文档、5 - 应用、6 - 其他、7 - 种子
            //num int 否   500 搜索返回的最大数量
            //stream  int 否   0   是否流式响应
            //search_type int 否   0   搜索方式。
            //0 - 简单搜索（query为关键词），1 - 语义搜索（query为复杂的自然语言描述），2 - 自动（按query长度自动区分简单 / 语义搜索，目前『自动』策略为：5字以上走语义搜索，5字及以下走简单搜索）
            //sources[]int 否[]，不指定召回来源 搜索来源。
            //通过query的关键词搜索：4 - 文件名关键词搜索，5 - 图片OCR搜索（图片内文字），11 - 文档内容搜索（文档文本关键词），14 - 图片语义搜索（图片时间 / 地点 / 分类等），13 - 卡证搜索
            //通过query的语义向量搜索：7 - 文档向量搜索，8 - 视频向量搜索，9 - 音频向量搜索
            //sources为空时，search_type为0（简单搜索）时sources默认设置为[4, 5, 11, 13, 14]，search_type为1（语义搜索）时sources默认设置为[4, 5, 11, 13, 14, 7, 8, 9]
            //data json    是 传空，有非空校验
            // 处理目录参数
            if (dirs != null && dirs.Count > 0)
                queryParams.Add("dir", Uri.EscapeDataString(JsonConvert.SerializeObject(dirs)));

            // 处理文件类型参数
            if (categories != null && categories.Count > 0)
            {
                var categoryStr = string.Join(",", categories.Select(c => (int)c));
                queryParams.Add("category", categoryStr);
            }

            // 处理搜索来源参数
            if (sources != null && sources.Count > 0)
            {
                var sourceStr = string.Join(",", sources.Select(s => (int)s));
                queryParams.Add("sources", sourceStr);
            }

            var url = $"/xpan/unisearch?{BuildQueryString(queryParams)}";
            var content = new StringContent(JsonConvert.SerializeObject(new { }), Encoding.UTF8, "application/json");
            return await SendPostRequestAsync<SemanticSearchResponse>(url, content);
        }
        #endregion

        #region 11. 管理文件（复制/移动/重命名/删除）
        public async Task<FileManagerResponse> ManageFileAsync(
            FileOperation opera,
            List<FileManagerFileItem> fileList,
            int async = 1,
            OnDupStrategy onDup = OnDupStrategy.Fail)
        {
            if (fileList == null || fileList.Count == 0)
                throw new ArgumentException("待操作文件列表不能为空", nameof(fileList));

            // 转换操作类型字符串
            var operaStr = opera switch
            {
                FileOperation.Copy => "copy",
                FileOperation.Move => "move",
                FileOperation.Rename => "rename",
                FileOperation.Delete => "delete",
                _ => throw new ArgumentException("无效的文件操作类型", nameof(opera))
            };

            // 转换重复策略字符串
            var onDupStr = onDup switch
            {
                OnDupStrategy.Fail => "fail",
                OnDupStrategy.NewCopy => "newcopy",
                OnDupStrategy.Overwrite => "overwrite",
                OnDupStrategy.Skip => "skip",
                _ => "fail"
            };

            // 构建查询参数
            var queryParams = new Dictionary<string, string>
            {
                ["method"] = "filemanager",
                ["access_token"] = _accessToken,
                ["opera"] = operaStr
            };

            // 构建请求体
            var requestBody = new Dictionary<string, string>
            {
                ["async"] = async.ToString(),
                ["filelist"] = JsonConvert.SerializeObject(fileList),
                ["ondup"] = onDupStr
            };

            var url = $"/rest/2.0/xpan/file?{BuildQueryString(queryParams)}";
            var content = new FormUrlEncodedContent(requestBody);
            return await SendPostRequestAsync<FileManagerResponse>(url, content);
        }
        #endregion

        #region 私有工具方法
        // 构建查询字符串
        private string BuildQueryString(Dictionary<string, string> parameters)
        {
            return string.Join("&", parameters.Select(kv => $"{kv.Key}={kv.Value}"));
        }

        /// <summary>
        /// 发送GET请求（带限流控制+失败重试）
        /// </summary>
        /// <typeparam name="T">响应模型类型</typeparam>
        /// <param name="url">请求URL</param>
        /// <param name="maxRetryCount">最大重试次数（默认3次）</param>
        /// <param name="rateLimitDelay">限流延迟（默认7秒，符合接口每分钟8-10次要求）</param>
        /// <returns>反序列化后的响应结果</returns>
        private async Task<T> SendGetRequestAsync<T>(
            string url,
            int maxRetryCount = 3,
            int rateLimitDelay = 7000)
        {
            int retryCount = 0;
            // 记录上一次请求时间，用于限流控制
            DateTime _lastRequestTime = DateTime.MinValue;

            while (retryCount <= maxRetryCount)
            {
                try
                {
                    // 1. 限流控制：确保两次请求间隔不小于rateLimitDelay
                    if (DateTime.Now - _lastRequestTime < TimeSpan.FromMilliseconds(rateLimitDelay))
                    {
                        var delay = TimeSpan.FromMilliseconds(rateLimitDelay) - (DateTime.Now - _lastRequestTime);
                        _logger?.LogDebug("触发限流，延迟{Delay}ms后执行请求", delay.TotalMilliseconds);
                        await Task.Delay(delay);
                    }

                    // 2. 发送HTTP GET请求
                    var response = await _httpClient.GetAsync(url);
                    _lastRequestTime = DateTime.Now; // 更新最后请求时间

                    // 3. 处理HTTP状态码（429为频控专用状态码，单独捕获）
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        retryCount++;
                        var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(10);
                        _logger?.LogWarning("命中接口频控（429），{RetryAfter}秒后重试（第{RetryCount}/{MaxRetryCount}次）", retryAfter.TotalSeconds, retryCount, maxRetryCount);
                        await Task.Delay(retryAfter);
                        continue;
                    }

                    response.EnsureSuccessStatusCode(); // 抛出其他非成功状态码异常

                    // 4. 解析响应内容
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<T>(content);

                    // 5. 验证业务响应（如errno是否为0）
                    ValidateResponse(result);
                    return result;
                }
                catch (HttpRequestException ex)
            {
                retryCount++;
                var errorMsg = ex.Message.Contains("429") ? "接口频控限制" : "HTTP请求异常";
                _logger?.LogWarning(ex, "{ErrorMsg}，{Remaining}次重试机会", errorMsg, maxRetryCount - retryCount + 1);

                if (retryCount > maxRetryCount)
                    throw new Exception($"HTTP请求异常（已重试{maxRetryCount}次）：{ex.Message}", ex);

                var retryDelay = TimeSpan.FromSeconds(Math.Pow(3, retryCount));
                await Task.Delay(retryDelay);
            }
                catch (JsonException ex)
                {
                    if (ex.Message.Contains("\"errno\": 20012,") || ex.Message.Contains("\"errno\": 31034,"))
                    {
                        var errorMsg = ex.Message.Contains("\"errno\": 20012,") || ex.Message.Contains("\"errno\": 31034,") ? "接口频控限制" : "HTTP请求异常";
                        _logger?.LogWarning(ex, "{ErrorMsg}，{Remaining}次重试机会", errorMsg, maxRetryCount - retryCount + 1);

                        if (retryCount > maxRetryCount)
                            throw new Exception($"HTTP请求异常（已重试{maxRetryCount}次）：{ex.Message}", ex);

                        var retryDelay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                        await Task.Delay(retryDelay);
                    }
                    else
                        throw new Exception("JSON解析失败（响应格式异常）", ex);
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger?.LogWarning(ex, "接口调用失败，{Remaining}次重试机会", maxRetryCount - retryCount + 1);

                    if (retryCount > maxRetryCount)
                        throw new Exception($"接口调用失败（已重试{maxRetryCount}次）：{ex.Message}", ex);

                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                }
            }

            throw new Exception($"接口调用失败，已达到最大重试次数({maxRetryCount}次)");
        }
        /// <summary>
        /// 发送POST请求（带限流控制+失败重试+频控适配）
        /// </summary>
        /// <typeparam name="T">响应模型类型</typeparam>
        /// <param name="url">请求URL</param>
        /// <param name="content">POST请求体</param>
        /// <param name="maxRetryCount">最大重试次数（默认3次）</param>
        /// <param name="rateLimitDelay">限流延迟（默认7秒，符合网盘接口每分钟8-10次要求）</param>
        /// <returns>反序列化后的响应结果</returns>
        private async Task<T> SendPostRequestAsync<T>(
      string url,
      HttpContent content,
      int maxRetryCount = 5,
      int rateLimitDelay = 10000)
        {
            int retryCount = 0;
            // 静态变量共享限流时间（与GET请求共用，避免跨方法频控）
            DateTime _lastRequestTime = DateTime.MinValue;

            // 深拷贝请求体（避免重试时请求体已释放/读取）
            HttpContent requestContent = CloneHttpContent(content);

            while (retryCount <= maxRetryCount)
            {
                try
                {
                    // ========== 核心限流控制 ==========
                    // 确保两次请求（含GET/POST）间隔不小于限流延迟
                    var timeSinceLastRequest = DateTime.Now - _lastRequestTime;
                    if (timeSinceLastRequest < TimeSpan.FromMilliseconds(rateLimitDelay))
                    {
                        var delayMs = rateLimitDelay - (int)timeSinceLastRequest.TotalMilliseconds;
                        _logger?.LogDebug("[POST限流] 距离上次请求仅{TimeSinceLastRequest}ms，延迟{DelayMs}ms执行", timeSinceLastRequest.TotalMilliseconds, delayMs);
                        await Task.Delay(delayMs);
                    }

                    // ========== 发送POST请求 ==========
                    //Console.WriteLine($"[POST请求] 执行请求：{url}（第{retryCount + 1}次）");
                    var response = await _httpClient.PostAsync(url, requestContent);
                    _lastRequestTime = DateTime.Now; // 更新最后请求时间

                    // ========== 频控专用处理（429状态码） ==========
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        retryCount++;
                        var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(10);
                        _logger?.LogWarning("[POST频控] 命中429 TooManyRequests，{RetryAfter}秒后重试（剩余{Remaining}次）", retryAfter.TotalSeconds, maxRetryCount - retryCount);

                        requestContent = CloneHttpContent(content);
                        await Task.Delay(retryAfter);
                        continue;
                    }

                    // 其他非成功状态码直接抛出
                    response.EnsureSuccessStatusCode();

                    // ========== 解析响应 ==========
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<T>(responseContent);

                    // 验证业务响应（如errno是否为0）
                    ValidateResponse(result);

                    //Console.WriteLine($"[POST成功] 请求{url}响应：{responseContent[..Math.Min(200, responseContent.Length)]}...");
                    return result;
                }
                catch (HttpRequestException ex)
                {
                    retryCount++;
                    var errorType = ex.Message.Contains("429") ? "频控限制" : "HTTP异常";
                    _logger?.LogWarning(ex, "[POST失败] {ErrorType}：{Message}（剩余{Remaining}次重试）", errorType, ex.Message, maxRetryCount - retryCount);

                    if (retryCount > maxRetryCount)
                    {
                        throw new Exception($"POST请求失败（已重试{maxRetryCount}次）：{ex.Message}", ex);
                    }

                    var retryDelay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    requestContent = CloneHttpContent(content);
                    await Task.Delay(retryDelay);
                }
                catch (JsonException ex)
                {
                    if (ex.Message.Contains("\"errno\": 20012,") || ex.Message.Contains("\"errno\": 31034,"))
                    {
                        var errorMsg = ex.Message.Contains("\"errno\": 20012,") || ex.Message.Contains("\"errno\": 31034,") ? "接口频控限制" : "HTTP请求异常";
                        _logger?.LogWarning(ex, "{ErrorMsg}，{Message}，{Remaining}次重试机会", errorMsg, ex.Message, maxRetryCount - retryCount + 1);

                        if (retryCount > maxRetryCount)
                            throw new Exception($"HTTP请求异常（已重试{maxRetryCount}次）：{ex.Message}", ex);

                        var retryDelay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                        await Task.Delay(retryDelay);
                    }
                    else
                        throw new Exception($"POST响应JSON解析失败：{ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger?.LogWarning(ex, "[POST异常] 未知错误：{Message}（剩余{Remaining}次重试）", ex.Message, maxRetryCount - retryCount);

                    if (retryCount > maxRetryCount)
                    {
                        throw new Exception($"POST接口调用失败（已重试{maxRetryCount}次）：{ex.Message}", ex);
                    }

                    // 通用异常重试延迟
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                    requestContent = CloneHttpContent(content); // 重置请求体
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));

            throw new Exception($"POST请求{url}失败，已达到最大重试次数({maxRetryCount}次)");
        }

        /// <summary>
        /// 深拷贝HttpContent（解决重试时请求体流已释放问题）
        /// </summary>
        private HttpContent CloneHttpContent(HttpContent originalContent)
        {
            if (originalContent == null) return null;

            // 读取原始内容
            var contentBytes = originalContent.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            var clonedContent = new ByteArrayContent(contentBytes);

            // 复制请求头（如Content-Type）
            foreach (var header in originalContent.Headers)
            {
                clonedContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clonedContent;
        }
        // 验证响应是否成功（基于errno字段）
        private void ValidateResponse(object response)
        {
            if (response == null) return;

            var type = response.GetType();
            var errnoProp = type.GetProperty("ErrorCode") ?? type.GetProperty("error_no");
            if (errnoProp == null) return;

            var errno = (int)errnoProp.GetValue(response);
            if (errno != 0)
            {
                var errMsgProp = type.GetProperty("ErrorMsg") ?? type.GetProperty("error_msg");
                var errMsg = errMsgProp?.GetValue(response)?.ToString() ?? "未知错误";
                throw new Exception($"业务错误：错误码={errno}，描述={errMsg}");
            }
        }

        // 发送GET请求
        private async Task<T> SendGetRequestAsyncOrgi<T>(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<T>(content);
                ValidateResponse(result);
                return result;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"HTTP请求异常：{ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new Exception("JSON解析失败", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"接口调用失败：{ex.Message}", ex);
            }
        }
        // 发送POST请求
        private async Task<T> SendPostRequestAsyncOrgi<T>(string url, HttpContent content)
        {
            try
            {
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<T>(responseContent);
                ValidateResponse(result);
                return result;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"HTTP请求异常：{ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new Exception("JSON解析失败", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"接口调用失败：{ex.Message}", ex);
            }
        }

        // 释放资源
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
        #endregion
    }

    #region 模型

    // 视频媒体信息模型（查询文件信息接口用）
    public class VideoMediaInfo
    {
        [JsonProperty("channels")]
        public int Channels { get; set; }

        [JsonProperty("duration")]
        public long Duration { get; set; }

        [JsonProperty("duration_ms")]
        public long DurationMs { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("frame_rate")]
        public double FrameRate { get; set; }
    }


    /// <summary>
    /// 文件基础信息模型（多个接口通用）
    /// </summary>
    public class BaseFileInfo
    {
        /// <summary>
        /// 文件在云端的唯一标识ID
        /// </summary>
        [JsonProperty("fs_id")]
        public long FsId { get; set; }

        /// <summary>
        /// 文件的绝对路径
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary>
        /// 文件名称
        /// </summary>
        [JsonProperty("server_filename")]
        public string ServerFileName { get; set; }

        /// <summary>
        /// 文件大小，单位B
        /// </summary>
        [JsonProperty("size")]
        public long Size { get; set; }

        /// <summary>
        /// 文件在服务器修改时间
        /// </summary>
        [JsonProperty("server_mtime")]
        public long ServerMtime { get; set; }

        /// <summary>
        /// 文件在服务器创建时间
        /// </summary>
        [JsonProperty("server_ctime")]
        public long ServerCtime { get; set; }

        /// <summary>
        /// 文件在客户端修改时间
        /// </summary>
        [JsonProperty("local_mtime")]
        public long LocalMtime { get; set; }

        /// <summary>
        /// 文件在客户端创建时间
        /// </summary>
        [JsonProperty("local_ctime")]
        public long LocalCtime { get; set; }

        /// <summary>
        /// 是否为目录，0 文件、1 目录
        /// </summary>
        [JsonProperty("isdir")]
        public int IsDir { get; set; }

        /// <summary>
        /// 文件类型，1 视频、2 音频、3 图片、4 文档、5 应用、6 其他、7 种子
        /// </summary>
        [JsonProperty("category")]
        public int Category { get; set; }

        /// <summary>
        /// 云端哈希（非文件真实MD5），只有是文件类型时，该字段才存在
        /// </summary>
        [JsonProperty("md5")]
        public string Md5 { get; set; }

        /// <summary>
        /// 该目录是否存在子目录，只有请求参数web=1且该条目为目录时，该字段才存在，0为存在，1为不存在
        /// </summary>
        [JsonProperty("dir_empty")]
        public int DirEmpty { get; set; }

        /// <summary>
        /// 只有请求参数web = 1且该条目分类为图片时，该字段才存在，包含三个尺寸的缩略图URL
        /// </summary>
        [JsonProperty("thumbs")]
        public ThumbnailInfo Thumbs { get; set; }
    }


    /// <summary>
    /// 缩略图信息
    /// </summary>
    public class ThumbnailInfo
    {
        /// <summary>
        /// 缩略图URL，尺寸可能为 48x48
        /// </summary>
        [JsonProperty("url1")]
        public string Url1 { get; set; }

        /// <summary>
        /// 缩略图URL，尺寸可能为 128x128
        /// </summary>
        [JsonProperty("url2")]
        public string Url2 { get; set; }

        /// <summary>
        /// 缩略图URL，尺寸可能为 256x256
        /// </summary>
        [JsonProperty("url3")]
        public string Url3 { get; set; }
    }

    /// <summary>
    /// 文件类型枚举（与API category字段对应）
    /// </summary>
    public enum FileCategory
    {
        /// <summary>未知类型</summary>
        Unknown = 0,
        /// <summary>视频</summary>
        Video = 1,
        /// <summary>音频</summary>
        Audio = 2,
        /// <summary>图片</summary>
        Image = 3,
        /// <summary>文档</summary>
        Document = 4,
        /// <summary>应用</summary>
        Application = 5,
        /// <summary>其他</summary>
        Other = 6,
        /// <summary>种子</summary>
        Torrent = 7
    }



    // 1. 获取文件列表（list接口）响应模型
    public class ListFileResponse
    {
        [JsonProperty("errno")]
        public int ErrorCode { get; set; }

        [JsonProperty("list")]
        public List<BaseFileInfo> FileList { get; set; }

        [JsonProperty("request_id")]
        public long RequestId { get; set; }
    }

    // 2. 递归获取文件列表（listall接口）响应模型
    public class ListAllFileResponse
    {
        [JsonProperty("errno")]
        public int ErrorCode { get; set; }

        [JsonProperty("errmsg")]
        public string ErrorMsg { get; set; }

        [JsonProperty("has_more")]
        public int HasMore { get; set; }

        [JsonProperty("cursor")]
        public int Cursor { get; set; }

        [JsonProperty("list")]
        public List<BaseFileInfo> FileList { get; set; }

        [JsonProperty("request_id")]
        public string RequestId { get; set; }
    }

    // 3. 获取文档列表（doclist接口）响应模型
    public class DocListResponse
    {
        [JsonProperty("errno")]
        public int ErrorCode { get; set; }

        [JsonProperty("info")]
        public List<BaseFileInfo> DocList { get; set; }

        [JsonProperty("request_id")]
        public long RequestId { get; set; }
    }

    // 4. 获取图片列表（imagelist接口）响应模型
    public class ImageListResponse
    {
        [JsonProperty("errno")]
        public int ErrorCode { get; set; }

        [JsonProperty("info")]
        public List<BaseFileInfo> ImageList { get; set; }

        [JsonProperty("request_id")]
        public long RequestId { get; set; }
    }

    // 5. 获取视频列表（videolist接口）响应模型
    public class VideoListResponse
    {
        [JsonProperty("errno")]
        public int ErrorCode { get; set; }

        [JsonProperty("info")]
        public List<BaseFileInfo> VideoList { get; set; }

        [JsonProperty("request_id")]
        public long RequestId { get; set; }
    }

    // 6. 获取分类文件数量（categoryinfo接口）响应模型
    public class CategoryCountStats
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }
    }

    public class CategoryCountResponse
    {
        [JsonProperty("errno")]
        public int ErrorCode { get; set; }

        [JsonProperty("info")]
        public Dictionary<string, CategoryCountStats> Info { get; set; }

        [JsonProperty("request_id")]
        public long RequestId { get; set; }
    }

    // 7. 获取分类文件列表（categorylist接口）响应模型
    public class CategoryFileResponse
    {
        [JsonProperty("errno")]
        public int ErrorCode { get; set; }

        [JsonProperty("errmsg")]
        public string ErrorMsg { get; set; }

        [JsonProperty("has_more")]
        public int HasMore { get; set; }

        [JsonProperty("cursor")]
        public int Cursor { get; set; }

        [JsonProperty("list")]
        public List<BaseFileInfo> FileList { get; set; }

        [JsonProperty("request_id")]
        public string RequestId { get; set; }
    }

    // 8. 查询文件信息（filemetas接口）响应模型
    public class FileMetaInfo : BaseFileInfo
    {
        [JsonProperty("filename")]
        public string FileName { get; set; }

        [JsonProperty("dlink")]
        public string Dlink { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("date_taken")]
        public long DateTaken { get; set; }

        [JsonProperty("duration")]
        public long Duration { get; set; }

        [JsonProperty("media_info")]
        public VideoMediaInfo MediaInfo { get; set; }
    }

    public class FileMetaResponse
    {
        [JsonProperty("errno")]
        public int ErrorCode { get; set; }

        [JsonProperty("errmsg")]
        public string ErrorMsg { get; set; }

        [JsonProperty("list")]
        public List<FileMetaInfo> FileMetaList { get; set; }

        [JsonProperty("request_id")]
        public string RequestId { get; set; }
    }

    // 9. 关键词搜索文件（search接口）响应模型
    public class SearchFileResponse
    {
        [JsonProperty("errno")]
        public int ErrorCode { get; set; }

        [JsonProperty("list")]
        public List<BaseFileInfo> FileList { get; set; }

        [JsonProperty("has_more")]
        public int HasMore { get; set; }

        [JsonProperty("request_id")]
        public long RequestId { get; set; }
    }

    // 10. 语义搜索文件（unisearch接口）响应模型
    public class SemanticSearchItem : BaseFileInfo
    {
        [JsonProperty("filename")]
        public string FileName { get; set; }

        [JsonProperty("parent_path")]
        public string ParentPath { get; set; }

        [JsonProperty("ocr")]
        public string Ocr { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("pid")]
        public long Pid { get; set; }
    }

    public class SemanticSearchResponse
    {
        [JsonProperty("error_no")]
        public int ErrorCode { get; set; }

        [JsonProperty("error_msg")]
        public string ErrorMsg { get; set; }

        [JsonProperty("data")]
        public List<dynamic> Data { get; set; } // 适配嵌套结构

        [JsonProperty("is_end")]
        public bool IsEnd { get; set; }

        [JsonProperty("request_id")]
        public long RequestId { get; set; }

        // 解析文件列表（简化嵌套结构）
        public List<SemanticSearchItem> GetFileList()
        {
            var result = new List<SemanticSearchItem>();
            foreach (var item in Data)
            {
                var list = JsonConvert.DeserializeObject<List<SemanticSearchItem>>(item.list.ToString());
                result.AddRange(list);
            }
            return result;
        }
    }

    // 11. 管理文件（filemanager接口）请求参数模型
    public class FileManagerFileItem
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("dest")]
        public string Dest { get; set; } // copy/move用

        [JsonProperty("newname")]
        public string NewName { get; set; } // rename用
    }

    // 管理文件响应模型
    public class FileManagerResponse
    {
        [JsonProperty("errno")]
        public int ErrorCode { get; set; }

        [JsonProperty("info")]
        public List<dynamic> Info { get; set; }

        [JsonProperty("taskid")]
        public long TaskId { get; set; }

        [JsonProperty("request_id")]
        public long RequestId { get; set; }
    }




    // 文件操作类型（管理文件接口用）
    public enum FileOperation
    {
        Copy = 0,    // 复制
        Move = 1,    // 移动
        Rename = 2,  // 重命名
        Delete = 3   // 删除
    }

    // 重复文件处理策略（管理文件接口用）
    public enum OnDupStrategy
    {
        Fail = 0,    // 失败（默认）
        NewCopy = 1, // 重命名
        Overwrite = 2,// 覆盖
        Skip = 3     // 跳过
    }

    // 搜索来源枚举（语义搜索接口用）
    public enum SearchSource
    {
        FileName = 4,        // 文件名关键词
        ImageOcr = 5,        // 图片OCR
        DocumentContent = 11,// 文档内容关键词
        CardSearch = 13,     // 卡证搜索
        ImageSemantic = 14,  // 图片语义
        DocumentVector = 7,  // 文档向量
        VideoVector = 8,     // 视频向量
        AudioVector = 9      // 音频向量
    }

    // 搜索类型枚举（语义搜索接口用）
    public enum SearchType
    {
        Simple = 0,  // 简单搜索（关键词）
        Semantic = 1,// 语义搜索（自然语言）
        Auto = 2     // 自动区分
    }
    #endregion
}
