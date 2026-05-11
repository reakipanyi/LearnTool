namespace UnifiedLearningAssistant.Views
{
    public interface ISettingView
    {
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

        event EventHandler? SaveClicked;
        event EventHandler? CancelClicked;

        void ShowMessage(string msg);
        void CloseView();
    }
}
