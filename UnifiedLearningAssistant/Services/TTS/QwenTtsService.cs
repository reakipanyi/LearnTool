using NAudio.Wave;
using UnifiedLearningAssistant.Models.Config;

namespace UnifiedLearningAssistant.Services.TTS
{
    public class QwenTtsService : ITTSService, IDisposable
    {
        private readonly QwenTtsClient _client;
        private readonly TtsConfig _config;
        private WaveOutEvent? _waveOut;
        private bool _isSpeaking;
        private readonly object _lock = new object();
        private bool _disposed = false;

        public QwenTtsService(TtsConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _client = new QwenTtsClient(config.ApiKey, config.BaseUrl);
        }

        public bool IsSpeaking => _isSpeaking;

        public bool Available => !string.IsNullOrWhiteSpace(_config.ApiKey);

        public async Task SpeakAsync(string text, string language = "zh", float speed = 1.0f)
        {
            if (!Available)
                return;

            await StopAsync();

            lock (_lock)
            {
                _isSpeaking = true;
            }

            try
            {
                var finalSpeed = speed * _config.Speed;
                finalSpeed = Math.Max(0.5f, Math.Min(2.0f, finalSpeed));
                
                var audioBytes = await _client.SynthesizeAsync(text, _config.Model, _config.Voice, finalSpeed, _config.Volume);
                if (audioBytes == null || audioBytes.Length == 0)
                {
                    lock (_lock)
                    {
                        _isSpeaking = false;
                    }
                    return;
                }

                using var memoryStream = new MemoryStream(audioBytes);
                using var reader = new Mp3FileReader(memoryStream);
                
                WaveOutEvent? waveOut = new WaveOutEvent();
                waveOut.Init(reader);
                
                bool playbackCompleted = false;
                waveOut.PlaybackStopped += (s, e) =>
                {
                    lock (_lock)
                    {
                        _isSpeaking = false;
                        playbackCompleted = true;
                        if (_waveOut == waveOut)
                        {
                            _waveOut = null;
                        }
                    }
                    try
                    {
                        waveOut.Dispose();
                    }
                    catch { }
                };

                lock (_lock)
                {
                    _waveOut = waveOut;
                }

                waveOut.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TTS语音合成失败: {ex.Message}");
                lock (_lock)
                {
                    _isSpeaking = false;
                }
            }
        }

        public async Task StopAsync()
        {
            WaveOutEvent? waveOutToStop = null;
            
            lock (_lock)
            {
                if (_waveOut != null)
                {
                    waveOutToStop = _waveOut;
                    _waveOut = null;
                    _isSpeaking = false;
                }
            }

            if (waveOutToStop != null)
            {
                try
                {
                    waveOutToStop.Stop();
                    await Task.Delay(100);
                }
                finally
                {
                    waveOutToStop.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                StopAsync().Wait(1000);
                _waveOut?.Dispose();
            }

            _disposed = true;
        }
    }
}