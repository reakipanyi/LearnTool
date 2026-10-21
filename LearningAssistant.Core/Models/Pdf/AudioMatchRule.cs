namespace LearningAssistant.Models.Pdf
{
    public class AudioMatchRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "";
        public string FileNamePattern { get; set; } = "";
        /// <summary>写法：正则表达式或通配符（* 任意字符，? 单个字符）</summary>
        public AudioPatternMode PatternMode { get; set; } = AudioPatternMode.Regex;
        public int? CaptureGroupIndex { get; set; }
        public string? CaptureGroupName { get; set; }
        public AudioPageMapMode MapMode { get; set; } = AudioPageMapMode.PageIndex;
        public int PageOffset { get; set; }
        public int PagesPerUnit { get; set; } = 1;
        public bool Enabled { get; set; } = true;
        public int Priority { get; set; }
    }
}