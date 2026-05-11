namespace UnifiedLearningAssistant.Models.Pdf
{
    public class PdfAnnotation
    {
        public string PdfPath { get; set; } = string.Empty;
        public int PageIndex { get; set; }
        public List<AnnotationStroke> Strokes { get; set; } = new List<AnnotationStroke>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class AnnotationStroke
    {
        public float[] Points { get; set; } = Array.Empty<float>();
        public int ColorArgb { get; set; }
        public float Thickness { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}