namespace LearningAssistant.Models.Config
{
    public class OcrConfig
    {
        public string Language { get; set; } = "eng";
        public string DataPath { get; set; } = "./tessdata";
    }
}