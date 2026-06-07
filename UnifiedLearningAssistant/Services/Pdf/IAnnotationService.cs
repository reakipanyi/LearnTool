using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    public interface IAnnotationService
    {
        Bitmap? LoadAnnotation(string pdfPath, int pageIndex, int targetWidth, int targetHeight, SizeF pageOriginalSize);
        void SaveAnnotation(string pdfPath, int pageIndex, Bitmap overlayBitmap);
        void ClearAnnotation(string pdfPath, int pageIndex);
        void AddStroke(string pdfPath, int pageIndex, AnnotationStroke stroke);
        void ClearRedo(string pdfPath, int pageIndex);
        IEnumerable<AnnotationStroke> GetStrokes(string pdfPath, int pageIndex);
    }
}