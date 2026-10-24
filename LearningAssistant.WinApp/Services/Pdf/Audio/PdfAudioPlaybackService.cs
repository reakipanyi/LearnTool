using LearningAssistant.Abstractions.Media;
using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf.Audio;

public class PdfAudioPlaybackService
{
    private readonly IMediaPlayer _player;
    public IMediaPlayer Player => _player;

    private const int FadeMilliseconds = 30;

    public PdfAudioPlaybackService(IMediaPlayer player) => _player = player;

    public void Load(AudioTrack track)
    {
        // 正在播放时先淡出旧曲再换；暂停状态只换不播，也不做淡入
        bool wasPlaying = _player.State == MediaPlaybackState.Playing;
        if (wasPlaying) _player.FadeOut(FadeMilliseconds, blocking: true);

        _player.Stop();
        _player.Load(track.FilePath);
        _player.LoadSubtitle(track.SubtitlePath);

        if (wasPlaying) _player.FadeIn(FadeMilliseconds);
    }

    public Task PlayAsync() => _player.PlayAsync();
    public void Pause() => _player.Pause();
    public void Resume() => _player.Resume();
    public void Stop() => _player.Stop();
    public void Seek(TimeSpan p) => _player.Seek(p);
    public void SetSpeed(double s) { if (s >= 0.6 && s <= 2.0) _player.Speed = s; }
    public void SetAbLoop(TimeSpan? a, TimeSpan? b) => _player.AbLoop = (a, b);
}