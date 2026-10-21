using System.Text.Json.Serialization;

namespace LearningAssistant.Models.Pdf
{
    /// <summary>页面文本来源</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PdfTextSource
    {
        /// <summary>未取到文本</summary>
        None = 0,
        /// <summary>直接读取 PDF 文本层</summary>
        TextLayer = 1,
        /// <summary>渲染后 OCR 识别</summary>
        Ocr = 2
    }

    /// <summary>单页文本索引项</summary>
    public sealed class PdfTextIndexPage
    {
        /// <summary>页码索引（从 0 开始）</summary>
        public int PageIndex { get; set; }

        /// <summary>页面文本（已去除首尾空白）</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>文本来源</summary>
        public PdfTextSource Source { get; set; }

        /// <summary>自动检测到的语言（Chinese/English/Mixed/Unknown）</summary>
        public string DetectedLanguage { get; set; } = string.Empty;

        /// <summary>字符数</summary>
        public int CharacterCount { get; set; }
    }

    /// <summary>
    /// 文档级文本索引：按文件指纹缓存每页文本，供 AI、搜索、总结共用。
    /// </summary>
    public sealed class PdfTextIndexDocument
    {
        /// <summary>索引格式版本，升级后旧缓存自动失效</summary>
        public int Version { get; set; } = PdfDocumentTextIndexVersion.Current;

        /// <summary>PDF 完整路径</summary>
        public string DocumentPath { get; set; } = string.Empty;

        /// <summary>文件指纹（路径 + 大小 + 修改时间）</summary>
        public string Fingerprint { get; set; } = string.Empty;

        /// <summary>总页数</summary>
        public int PageCount { get; set; }

        /// <summary>构建时间（UTC）</summary>
        public DateTime BuiltAtUtc { get; set; }

        /// <summary>页级文本</summary>
        public List<PdfTextIndexPage> Pages { get; set; } = new();

        /// <summary>全文总字符数</summary>
        [JsonIgnore]
        public int TotalCharacterCount => Pages.Sum(p => p.CharacterCount);

        /// <summary>按页码索引取页文本，越界返回空</summary>
        public PdfTextIndexPage? GetPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= Pages.Count) return null;
            return Pages[pageIndex];
        }
    }

    /// <summary>索引格式版本常量</summary>
    public static class PdfDocumentTextIndexVersion
    {
        /// <summary>当前版本</summary>
        public const int Current = 1;
    }

    /// <summary>文本索引构建进度</summary>
    public sealed class PdfTextIndexProgress
    {
        /// <summary>当前处理页码索引（从 0 开始）</summary>
        public int PageIndex { get; init; }

        /// <summary>已处理页数</summary>
        public int ProcessedPages { get; init; }

        /// <summary>总页数</summary>
        public int TotalPages { get; init; }

        /// <summary>是否直接命中磁盘缓存</summary>
        public bool FromCache { get; init; }

        /// <summary>当前页面文本来源</summary>
        public PdfTextSource Source { get; init; }

        /// <summary>状态描述</summary>
        public string Message { get; init; } = string.Empty;
    }
}
