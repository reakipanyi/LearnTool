namespace UnifiedLearningAssistant.Models.Config
{
    public class OcrConfig
    {
        public string Language { get; set; } = "chi_sim+eng";
        public string DataPath { get; set; } = "./tessdata";
    }
}