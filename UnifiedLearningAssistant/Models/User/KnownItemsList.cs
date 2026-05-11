namespace UnifiedLearningAssistant.Models.User
{
    public class KnownItemsList
    {
        public string UserId { get; set; } = string.Empty;
        public List<string> KnownChineseCharacters { get; set; } = new List<string>();
        public List<string> KnownChineseIdioms { get; set; } = new List<string>();
        public List<string> KnownChinesePhrases { get; set; } = new List<string>();
        public List<string> KnownChinesePoems { get; set; } = new List<string>();
        public List<string> KnownEnglishWords { get; set; } = new List<string>();
        public List<string> KnownEnglishPhrases { get; set; } = new List<string>();
        public List<string> KnownEnglishSentences { get; set; } = new List<string>();
    }
}