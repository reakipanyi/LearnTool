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

        public QwenTtsService(TtsConfig config)
        {
            _config = config;
            _client = new QwenTtsClient(config.ApiKey, config.BaseUrl);
        }

        public bool IsSpeaking => _isSpeaking;

        public bool Available => !string.IsNullOrWhiteSpace(_config.ApiKey);

        public async Task SpeakAsync(string text, string language = "zh", float speed = 1.0f)
        {
            if (!Available)
                return;

            lock (_lock)
            {
                if (_isSpeaking)
                {
                    StopAsync().Wait();
                }
                _isSpeaking = true;
            }

            try
            {
                var audioBytes = await _client.SynthesizeAsync(text, _config.Model, _config.Voice, speed * _config.Speed, _config.Volume);
                if (audioBytes == null || audioBytes.Length == 0)
                    return;

                using var memoryStream = new MemoryStream(audioBytes);
                using var reader = new Mp3FileReader(memoryStream);
                
                _waveOut = new WaveOutEvent();
                _waveOut.Init(reader);
                _waveOut.PlaybackStopped += (s, e) =>
                {
                    lock (_lock)
                    {
                        _isSpeaking = false;
                    }
                    _waveOut?.Dispose();
                    _waveOut = null;
                };
                _waveOut.Play();
            }
            catch
            {
                lock (_lock)
                {
                    _isSpeaking = false;
                }
            }
        }

        public Task StopAsync()
        {
            lock (_lock)
            {
                if (_waveOut != null)
                {
                    _waveOut.Stop();
                    _waveOut.Dispose();
                    _waveOut = null;
                }
                _isSpeaking = false;
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            StopAsync().Wait();
            _waveOut?.Dispose();
        }
    }
}