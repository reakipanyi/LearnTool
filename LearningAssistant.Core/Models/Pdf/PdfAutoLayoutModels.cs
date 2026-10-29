using LearningAssistant.Abstractions;

namespace LearningAssistant.Models.Pdf
{
    /// <summary>文本行来源：用于界面提示与缓存失效判断</summary>
    public enum PdfTextLineSource
    {
        None,
        TextLayer,
        Ocr
    }

    /// <summary>
    /// 一行带坐标的文本。坐标已归一化到页面 0~1，原点为左上角，分别对应高亮的 NX/NY/NW/NH。
    /// </summary>
    public sealed class PdfTextLine
    {
        public string Text { get; set; } = string.Empty;
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        /// <summary>文本层为 PDF 点字号；OCR 行为像素高度（仅用于相对比较）</summary>
        public float FontSize { get; set; }

        public float Right => X + Width;
        public float Bottom => Y + Height;
        public float CenterX => X + Width / 2f;
        public float CenterY => Y + Height / 2f;
    }

    /// <summary>自动框选的一个文字块（通常是一个段落或一个独立标题/列表项）。</summary>
    public sealed class PdfAutoTextBlock
    {
        public string Text { get; set; } = string.Empty;
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public int LineCount { get; set; } = 1;
        public float FontSize { get; set; }
    }

    /// <summary>单页自动框选版面。</summary>
    public sealed class PdfAutoLayoutPage
    {
        public int PageIndex { get; set; }
        public PdfTextLineSource Source { get; set; }
        /// <summary>本版面是否按多行合并规则生成，用于区分两种分块模式的缓存</summary>
        public bool MergeMultiLine { get; set; } = true;
        public List<PdfAutoTextBlock> Blocks { get; set; } = new();
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 文档级自动框选状态：记录整本模式是否开启，以及被用户手动清除、不再自动恢复的页。
    /// </summary>
    public sealed class PdfAutoLayoutState
    {
        public string Fingerprint { get; set; } = string.Empty;
        public bool WholeBookEnabled { get; set; }
        public HashSet<int> SuppressedPages { get; set; } = new();
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>整本自动框选进度。</summary>
    public sealed class PdfAutoLayoutProgress
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public PdfTextLineSource Source { get; set; }
        public bool Skipped { get; set; }
    }

    /// <summary>整本自动框选结果汇总。</summary>
    public sealed class PdfAutoLayoutSummary
    {
        public int TotalPages { get; set; }
        public int TextLayerPages { get; set; }
        public int OcrPages { get; set; }
        public int SkippedPages { get; set; }
        public int FailedPages { get; set; }
    }

    /// <summary>OCR 行框识别结果（图像像素坐标）。</summary>
    public sealed class OcrLayoutResult
    {
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public string Text { get; set; } = string.Empty;
        public List<OcrTextLine> Lines { get; set; } = new();
    }

    /// <summary>OCR 单行文本及其图像像素包围盒。</summary>
    public sealed class OcrTextLine
    {
        public string Text { get; set; } = string.Empty;
        public RectInt Bounds { get; set; }
    }
}
