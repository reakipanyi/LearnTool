using LearningAssistant.Abstractions.Media;
using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf.Audio;

public class PdfAudioPlaybackService
{
    private readonly IMediaPlayer _player;
    public IMediaPlayer Player => _player;

    public PdfAudioPlaybackService(IMediaPlayer player) => _player = player;

    public void Load(AudioTrack track)
    {
        _player.Stop();
        _player.Load(track.FilePath);
        _player.LoadSubtitle(track.SubtitlePath);
    }

    public Task PlayAsync() => _player.PlayAsync();
    public void Pause() => _player.Pause();
    public void Resume() => _player.Resume();
    public void Stop() => _player.Stop();
    public void Seek(TimeSpan p) => _player.Seek(p);
    public void SetSpeed(double s) { if (s >= 0.6 && s <= 2.0) _player.Speed = s; }
    public void SetAbLoop(TimeSpan? a, TimeSpan? b) => _player.AbLoop = (a, b);
}