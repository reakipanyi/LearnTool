using LearningAssistant.Abstractions.Media;
namespace LearningAssistant.Services.Media;

public interface ISubtitleParser
{
    IReadOnlyList<SubtitleCue> Parse(string filePath);
}