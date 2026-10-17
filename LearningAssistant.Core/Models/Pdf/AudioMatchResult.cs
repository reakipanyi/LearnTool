namespace LearningAssistant.Models.Pdf
{
    public class AudioMatchResult
    {
        public Dictionary<int, AudioTrack> PageIndex { get; set; } = new();
        public HashSet<int> UnmatchedPages { get; set; } = new();
        public List<AudioTrack> Tracks { get; set; } = new();
    }
}