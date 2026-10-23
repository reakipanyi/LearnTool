using LearningAssistant.Models.Pdf;
namespace LearningAssistant.Services.Pdf;

public interface IPdfAudioMatcher
{
    /// <summary>最近一次匹配产生的诊断信息（如正则非法被跳过）</summary>
    IReadOnlyList<string> Diagnostics { get; }

    AudioMatchResult Match(string pdfPath, int pageCount, IReadOnlyList<AudioTrack> tracks,
        IReadOnlyList<AudioMatchRule> rules, IReadOnlyDictionary<int, string>? manualOverrides,
        PdfAudioMatchMode matchMode = PdfAudioMatchMode.PerPage, string? singleAudioFile = null);
}