using LearningAssistant.Models.Pdf;
namespace LearningAssistant.Services.Pdf;

public interface IPdfAudioProfileStore
{
    PdfAudioProfile? Load(string pdfPath);
    void Save(PdfAudioProfile profile);
    string GetProfilePath(string pdfPath);
}