using LearningAssistant.Models.Pdf;
namespace LearningAssistant.Services.Pdf;

public interface IPdfAudioDirectoryScanner
{
    List<AudioTrack> Scan(string pdfDirectoryPath, bool includeSubDirectories);
}