using NAudio.Wave;
using LearningAssistant.Abstractions;

namespace LearningAssistant.Platform
{
    /// <summary>
    /// WinForms 端 IAudioPlayer 实现，用 NAudio.WaveOutEvent。
    /// </summary>
    public class WinFormsAudioPlayer : IAudioPlayer
    {
        private WaveOutEvent? _waveOut;
        private AudioFileReader? _audioReader;
        private volatile bool _isPlaying;

        public bool IsPlaying => _isPlaying;
        public event EventHandler? PlaybackFinished;

        public async Task PlayAsync(string audioFilePath, double speed = 1.0, CancellationToken ct = default)
        {
            Stop();

            _audioReader = new AudioFileReader(audioFilePath);
            _waveOut = new WaveOutEvent();
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            _waveOut.PlaybackStopped += (s, e) =>
            {
                _isPlaying = false;
                PlaybackFinished?.Invoke(this, EventArgs.Empty);
                tcs.TrySetResult(true);
            };

            using (ct.Register(() => { try { _waveOut?.Stop(); } catch { } }))
            {
                _waveOut.Init(_audioReader);
                _isPlaying = true;
                _waveOut.Play();
                await tcs.Task;
            }
        }

        public void Stop()
        {
            try { _waveOut?.Stop(); } catch { }
            _isPlaying = false;
            _audioReader?.Dispose();
            _audioReader = null;
            _waveOut?.Dispose();
            _waveOut = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}