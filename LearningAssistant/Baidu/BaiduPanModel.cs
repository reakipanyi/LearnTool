using Newtonsoft.Json;

namespace LearningAssistant.Baidu
{

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
