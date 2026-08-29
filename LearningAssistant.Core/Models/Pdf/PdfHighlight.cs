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
                var displayNote = Note.Length > 40 ? Note.Substring(0, 40) + "..." : Note;
                return $"{colorName}·P{pageNum} [{displayNote}]";
            }
            if (!string.IsNullOrEmpty(Text))
            {
                var displayText = Text.Length > 40 ? Text.Substring(0, 40) + "..." : Text;
                return $"{colorName}·P{pageNum} {displayText}";
            }
            return $"{colorName}·P{pageNum} (无文本)";
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
        /// <summary>形状类型（Rectangle/Ellipse/Arrow/Strikethrough 等），仅在 Type=Stroke 时有值</summary>
        public string? ShapeType { get; set; }
        /// <summary>画笔类型（Pencil/Pen/Marker），仅在 Type=Stroke 时有值</summary>
        public string? PenType { get; set; }

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
                AnnotationType.Stroke => GetStrokeTypeName(),
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

        private string GetStrokeTypeName()
        {
            if (!string.IsNullOrEmpty(ShapeType))
            {
                return ShapeType switch
                {
                    "Rectangle" => "矩形",
                    "Ellipse" => "椭圆",
                    "Arrow" => "箭头",
                    "Strikethrough" => "删除线",
                    "Mosaic" => "马赛克",
                    "Pen" => PenType switch
                    {
                        "Pencil" => "铅笔",
                        "Marker" => "马克笔",
                        _ => "水笔"
                    },
                    _ => ShapeType
                };
            }
            // 无 ShapeType 的笔划按画笔类型归类
            return PenType switch
            {
                "Pencil" => "铅笔",
                "Marker" => "马克笔",
                _ => "水笔"
            };
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
