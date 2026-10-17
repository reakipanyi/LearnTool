namespace LearningAssistant.Models.Pdf
{
    public class AudioMatchRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "";
        public string FileNamePattern { get; set; } = "";
        public int? CaptureGroupIndex { get; set; }
        public string? CaptureGroupName { get; set; }
        public AudioPageMapMode MapMode { get; set; } = AudioPageMapMode.PageIndex;
        public int PageOffset { get; set; }
        public int PagesPerUnit { get; set; } = 1;
        public bool Enabled { get; set; } = true;
        public int Priority { get; set; }
    }
}