namespace LearningAssistant.Models.Pdf
{
    public class PdfAnnotation
    {
        public string PdfPath { get; set; } = string.Empty;
        public int PageIndex { get; set; }
        public List<AnnotationStroke> Strokes { get; set; } = new List<AnnotationStroke>();
        public List<AnnotationText> Texts { get; set; } = new List<AnnotationText>();
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
}