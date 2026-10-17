namespace LearningAssistant.Models.Pdf
{
    public class PdfAudioProfile
    {
        public string PdfPath { get; set; } = "";
        public List<AudioMatchRule> Rules { get; set; } = new();
        public Dictionary<int, string> ManualOverrides { get; set; } = new();
        public List<AudioTrack> Tracks { get; set; } = new();
        public bool IncludeSubDirectories { get; set; }
        public string ScanFingerprint { get; set; } = "";
    }
}