namespace UnifiedLearningAssistant.Services
{
    public interface IAudioService : IDisposable
    {
        bool IsInitialized { get; }
        bool IsPlaying { get; }
        void Initialize(string vlcLibPath);
        void SetMedia(Uri mediaUri);
        void Play();
        void Pause();
        void Stop();
        void SetVolume(int vol);
        void SetRate(float rate);
        int GetLengthMilliseconds();
        int GetPositionMilliseconds();
        void SetPositionByMilliseconds(int ms);
    }
}