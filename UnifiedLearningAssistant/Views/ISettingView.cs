namespace LearningAssistant.Views
{
    public interface ISettingView
    {
        string Provider { get; set; }
        string ApiKey { get; set; }
        string ApiEndpoint { get; set; }
        string Model { get; set; }
        bool TTSEnabled { get; set; }
        string TtsApiKey { get; set; }
        string TtsVoice { get; set; }

        //string VoiceGender { get; set; }
        int TTSSpeed { get; set; }
        int TTSVolume { get; set; }
        int FontSize { get; set; }
        string Theme { get; set; }
        string BaiduAppId { get; set; }
        string BaiduSecret { get; set; }
        string BaiduNetdiskClientId { get; set; }
        string BaiduNetdiskClientSecret { get; set; }
        bool IsVoiceEnabled { get; set; }
        int PronunciationScope { get; set; }
        bool IsAIExplanationEnabled { get; set; }

        event EventHandler? SaveClicked;
        event EventHandler? CancelClicked;

        void ShowMessage(string msg);
        void CloseView();
    }
}
