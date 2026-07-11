namespace LearningAssistant.Services.TTS
{
    public class SpeakStateChangedEventArgs : EventArgs
    {
        public string SpeakKey { get; }
        public bool IsSpeaking { get; }

        public SpeakStateChangedEventArgs(string speakKey, bool isSpeaking)
        {
            SpeakKey = speakKey;
            IsSpeaking = isSpeaking;
        }
    }
}