namespace LearningAssistant.Services.TTS
{
    public class SpeakStateChangedEventArgs : EventArgs
    {
        public string SpeakKey { get; }
        public bool IsPlaying { get; }

        public SpeakStateChangedEventArgs(string speakKey, bool isPlaying)
        {
            SpeakKey = speakKey;
            IsPlaying = isPlaying;
        }
    }
}
