using System.Text.Json.Serialization;

namespace LearningAssistant.Models.Favorites
{
    /// <summary>
    /// 收藏夹文件夹
    /// </summary>
    public class FavoriteFolder
    {
        /// <summary>
        /// 文件夹ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 文件夹名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 父文件夹ID（根文件夹为 null）
        /// </summary>
        public string? ParentId { get; set; }

        /// <summary>
        /// 排序位置
        /// </summary>
        public int OrderIndex { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 图标
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// 子文件夹数量（运行时计算）
        /// </summary>
        [JsonIgnore]
        public int SubFolderCount { get; set; }

        /// <summary>
        /// 收藏项数量（运行时计算）
        /// </summary>
        [JsonIgnore]
        public int ItemCount { get; set; }
    }

    /// <summary>
    /// 收藏项类型
    /// </summary>
    public enum FavoriteItemType
    {
        /// <summary>
        /// PDF文档
        /// </summary>
        Pdf,

        /// <summary>
        /// PDF页面
        /// </summary>
        PdfPage,

        /// <summary>
        /// 文本
        /// </summary>
        Text,

        /// <summary>
        /// 网址
        /// </summary>
        Url,

        /// <summary>
        /// 图片
        /// </summary>
        Image,

        /// <summary>
        /// 笔记
        /// </summary>
        Note,

        /// <summary>
        /// 其他
        /// </summary>
        Other
    }

    /// <summary>
    /// 收藏项
    /// </summary>
    public class FavoriteItem
    {
        /// <summary>
        /// 收藏项ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 所属文件夹ID
        /// </summary>
        public string FolderId { get; set; } = "root";

        /// <summary>
        /// 收藏项类型
        /// </summary>
        public FavoriteItemType Type { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 描述/备注
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 内容数据（根据类型不同而不同）
        /// PDF: 文件路径
        /// PdfPage: 文件路径|页码
        /// Text: 文本内容
        /// Url: 网址
        /// Image: 图片路径
        /// Note: 笔记内容
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// 答案/解释
        /// </summary>
        public string? Answer { get; set; }

        /// <summary>
        /// 学科
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// 子类别
        /// </summary>
        public string? SubCategory { get; set; }

        /// <summary>
        /// 附加数据（JSON格式）
        /// </summary>
        public string? ExtraData { get; set; }

        /// <summary>
        /// 标签列表
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// 是否置顶
        /// </summary>
        public bool IsPinned { get; set; }

        /// <summary>
        /// 排序位置
        /// </summary>
        public int OrderIndex { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后访问时间
        /// </summary>
        public DateTime? LastVisitedAt { get; set; }

        /// <summary>
        /// 访问次数
        /// </summary>
        public int VisitCount { get; set; }

        /// <summary>
        /// 缩略图路径
        /// </summary>
        public string? ThumbnailPath { get; set; }

        /// <summary>
        /// 是否加入间隔重复复习
        /// </summary>
        public bool IsMarkedForReview { get; set; }

        /// <summary>
        /// 最后复习时间
        /// </summary>
        public DateTime? LastReviewedAt { get; set; }

        /// <summary>
        /// 复习次数
        /// </summary>
        public int ReviewCount { get; set; }

        /// <summary>
        /// 类型显示名称
        /// </summary>
        [JsonIgnore]
        public string TypeDisplayName => Type switch
        {
            FavoriteItemType.Pdf => "PDF文档",
            FavoriteItemType.PdfPage => "PDF页面",
            FavoriteItemType.Text => "文本",
            FavoriteItemType.Url => "网址",
            FavoriteItemType.Image => "图片",
            FavoriteItemType.Note => "笔记",
            _ => "其他"
        };
    }

    /// <summary>
    /// 收藏夹搜索参数
    /// </summary>
    public class FavoriteSearchParams
    {
        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 文件夹ID
        /// </summary>
        public string? FolderId { get; set; }

        /// <summary>
        /// 收藏项类型过滤
        /// </summary>
        public List<FavoriteItemType>? Types { get; set; }

        /// <summary>
        /// 标签过滤
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// 是否只显示置顶
        /// </summary>
        public bool PinnedOnly { get; set; }

        /// <summary>
        /// 排序方式
        /// </summary>
        public FavoriteSortOrder SortOrder { get; set; } = FavoriteSortOrder.CreatedDesc;

        /// <summary>
        /// 页码（从1开始）
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页数量
        /// </summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// 收藏排序方式
    /// </summary>
    public enum FavoriteSortOrder
    {
        /// <summary>
        /// 创建时间倒序
        /// </summary>
        CreatedDesc,

        /// <summary>
        /// 创建时间正序
        /// </summary>
        CreatedAsc,

        /// <summary>
        /// 更新时间倒序
        /// </summary>
        UpdatedDesc,

        /// <summary>
        /// 更新时间正序
        /// </summary>
        UpdatedAsc,

        /// <summary>
        /// 名称正序
        /// </summary>
        NameAsc,

        /// <summary>
        /// 名称倒序
        /// </summary>
        NameDesc,

        /// <summary>
        /// 自定义排序
        /// </summary>
        Custom,

        /// <summary>
        /// 访问次数倒序
        /// </summary>
        VisitCountDesc,

        /// <summary>
        /// 最后访问倒序
        /// </summary>
        LastVisitedDesc
    }

    /// <summary>
    /// 分页结果
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// 数据列表
        /// </summary>
        public List<T> Items { get; set; } = new();

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// 每页数量
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        /// <summary>
        /// 是否有上一页
        /// </summary>
        public bool HasPreviousPage => Page > 1;

        /// <summary>
        /// 是否有下一页
        /// </summary>
        public bool HasNextPage => Page < TotalPages;
    }
}
