namespace LearningAssistant.Models.Pdf
{
    public class PdfAnnotation
    {
        public string PdfPath { get; set; } = string.Empty;
        public int PageIndex { get; set; }
        public List<AnnotationStroke> Strokes { get; set; } = new List<AnnotationStroke>();
        public List<AnnotationText> Texts { get; set; } = new List<AnnotationText>();
        public List<ChecklistItem> Checklists { get; set; } = new List<ChecklistItem>();
        public List<EmbeddedImage> EmbeddedImages { get; set; } = new List<EmbeddedImage>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class AnnotationStroke
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int PageIndex { get; set; }
        public float[] Points { get; set; } = Array.Empty<float>();
        public int ColorArgb { get; set; }
        public float Thickness { get; set; }
        public string? ShapeType { get; set; }
        /// <summary>实线(Solid)/虚线(Dash)/点线(Dot)/箭头线(Arrow)</summary>
        public string DashStyle { get; set; } = "Dash";
        /// <summary>画笔类型: Pencil(铅笔)/Pen(水笔)/Marker(马克笔)</summary>
        public string PenType { get; set; } = "Pen";
        /// <summary>笔划样式: 默认(Solid)/点线(DotLine)/箭头线(ArrowLine)</summary>
        public string StrokeStyle { get; set; } = "Solid";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class AnnotationText
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public float NormalizedX { get; set; }
        public float NormalizedY { get; set; }
        public string Content { get; set; } = string.Empty;
        public int ColorArgb { get; set; }
        public float FontSize { get; set; } = 14f;
        public string FontFamily { get; set; } = "Microsoft YaHei UI";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class ChecklistItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public float NormalizedX { get; set; }
        public float NormalizedY { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsChecked { get; set; }
        public float FontSize { get; set; } = 14f;
        public int ColorArgb { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class EmbeddedImage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public float NormalizedX { get; set; }
        public float NormalizedY { get; set; }
        public float NormalizedWidth { get; set; }
        public float NormalizedHeight { get; set; }
        /// <summary>Base64 encoded image data (PNG)</summary>
        public string ImageData { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}