using LearningAssistant.Models.Pdf;
namespace LearningAssistant.Services.Pdf;

public interface IPdfAudioMatcher
{
    AudioMatchResult Match(string pdfPath, int pageCount, IReadOnlyList<AudioTrack> tracks,
        IReadOnlyList<AudioMatchRule> rules, IReadOnlyDictionary<int, string>? manualOverrides);
}