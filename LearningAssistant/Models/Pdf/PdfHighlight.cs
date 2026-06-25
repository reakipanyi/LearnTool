namespace LearningAssistant.Models.Pdf
{
    public interface IPdfNavigatable
    {
        string PdfPath { get; }
        int PageIndex { get; }
    }

    public class PdfHighlight : IPdfNavigatable
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PdfPath { get; set; } = string.Empty;
        public string PdfHash { get; set; } = string.Empty;
        public int PageIndex { get; set; }
        public float NormalizedX { get; set; }
        public float NormalizedY { get; set; }
        public float NormalizedWidth { get; set; }
        public float NormalizedHeight { get; set; }
        [Obsolete("Use NormalizedX instead")]
        public float X { get; set; }
        [Obsolete("Use NormalizedY instead")]
        public float Y { get; set; }
        [Obsolete("Use NormalizedWidth instead")]
        public float Width { get; set; }
        [Obsolete("Use NormalizedHeight instead")]
        public float Height { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public HighlightColor Color { get; set; } = HighlightColor.Yellow;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public override string ToString()
        {
            var pageNum = PageIndex + 1;
            var colorName = GetColorName(Color);
            var fileName = Path.GetFileName(PdfPath);

            if (!string.IsNullOrEmpty(Note))
            {
                return $"{fileName} P{pageNum} - {colorName} ({Note})";
            }
            if (!string.IsNullOrEmpty(Text))
            {
                // 显示更长的OCR文本（最多50个字符）
                var displayText = Text.Length > 50 ? Text.Substring(0, 50) + "..." : Text;
                return $"{fileName} P{pageNum} - {colorName}: {displayText}";
            }
            return $"{fileName} P{pageNum} - {colorName} (无OCR文本)";
        }

        private string GetColorName(HighlightColor color)
        {
            return color switch
            {
                HighlightColor.Red => "红",
                HighlightColor.Yellow => "黄",
                HighlightColor.Green => "绿",
                HighlightColor.Blue => "蓝",
                HighlightColor.Pink => "粉",
                HighlightColor.Orange => "橙",
                _ => ""
            };
        }
    }

    public enum HighlightColor
    {
        Red,
        Yellow,
        Green,
        Blue,
        Pink,
        Orange
    }

    public class PdfHighlightCollection
    {
        public string FolderPath { get; set; } = string.Empty;
        public List<PdfHighlight> Highlights { get; set; } = new List<PdfHighlight>();
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public enum AnnotationType
    {
        Highlight,
        Stroke,
        Text
    }

    public class PdfAnnotationItem : IPdfNavigatable
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PdfPath { get; set; } = string.Empty;
        public int PageIndex { get; set; }
        public AnnotationType Type { get; set; }

        public float NormalizedX { get; set; }
        public float NormalizedY { get; set; }
        public float NormalizedWidth { get; set; }
        public float NormalizedHeight { get; set; }

        public int ColorArgb { get; set; }
        public float Thickness { get; set; }

        public float[]? StrokePoints { get; set; }
        
        public string Text { get; set; } = string.Empty;
        public HighlightColor HColor { get; set; } = HighlightColor.Yellow;
        public float FontSize { get; set; }
        public string FontFamily { get; set; } = "Microsoft YaHei UI";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public override string ToString()
        {
            var pageNum = PageIndex + 1;
            var fileName = Path.GetFileName(PdfPath);

            string typeName = Type switch
            {
                AnnotationType.Highlight => "高亮",
                AnnotationType.Stroke => "标注",
                AnnotationType.Text => "文字",
                _ => ""
            };

            string typeIcon = Type switch
            {
                AnnotationType.Highlight => "▢",
                AnnotationType.Stroke => "✎",
                AnnotationType.Text => "A",
                _ => ""
            };

            if (!string.IsNullOrEmpty(Text))
            {
                var displayText = Text.Length > 30 ? Text.Substring(0, 30) + "..." : Text;
                return $"{typeIcon} {fileName} P{pageNum} {typeName}: {displayText}";
            }

            return $"{typeIcon} {fileName} P{pageNum} {typeName}";
        }

        private string GetColorName(HighlightColor color)
        {
            return color switch
            {
                HighlightColor.Red => "红",
                HighlightColor.Yellow => "黄",
                HighlightColor.Green => "绿",
                HighlightColor.Blue => "蓝",
                HighlightColor.Pink => "粉",
                HighlightColor.Orange => "橙",
                _ => ""
            };
        }
    }
}
