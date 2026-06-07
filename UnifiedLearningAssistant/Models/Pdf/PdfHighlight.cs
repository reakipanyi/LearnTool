namespace LearningAssistant.Models.Pdf
{
    public class PdfHighlight
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
            if (!string.IsNullOrEmpty(Note))
            {
                return $"{pageNum}页 - {colorName}高亮 ({Note})";
            }
            if (!string.IsNullOrEmpty(Text))
            {
                var displayText = Text.Length > 20 ? Text.Substring(0, 20) + "..." : Text;
                return $"{pageNum}页 - {colorName}高亮 ({displayText})";
            }
            return $"{pageNum}页 - {colorName}高亮";
        }

        private string GetColorName(HighlightColor color)
        {
            return color switch
            {
                HighlightColor.Red => "红色",
                HighlightColor.Yellow => "黄色",
                HighlightColor.Green => "绿色",
                HighlightColor.Blue => "蓝色",
                HighlightColor.Pink => "粉色",
                HighlightColor.Orange => "橙色",
                _ => "高亮"
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
        public string PdfPath { get; set; } = string.Empty;
        public List<PdfHighlight> Highlights { get; set; } = new List<PdfHighlight>();
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
