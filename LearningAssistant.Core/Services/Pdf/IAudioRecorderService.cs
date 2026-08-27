using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    public enum AudioRecorderState
    {
        Idle,
        Recording,
        Paused
    }

    public interface IAudioRecorderService
    {
        AudioRecorderState State { get; }

        event EventHandler<AudioRecording>? RecordingCompleted;
        event EventHandler<TimeSpan>? PositionChanged;
        event EventHandler<float>? AudioLevelChanged;

        void StartRecording(string pdfPath, int pageIndex, string pdfFileName);
        void StopRecording();
        AudioRecording? GetCurrentRecording();
        TimeSpan CurrentPosition { get; }
        long CurrentPositionMs { get; }
    }
}