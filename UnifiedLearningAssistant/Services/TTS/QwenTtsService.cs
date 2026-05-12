using System.Security.Cryptography;
using System.Text;

namespace UnifiedLearningAssistant.Services.TTS
{
    public class QwenTtsService : ITTSService
    {
        private readonly QwenTtsClient? _client;

        public QwenTtsService(string? apiKey, string? endpoint)
        {
            try
            {
                _client = new QwenTtsClient(apiKey, endpoint);
            }
            catch
            {
                _client = null;
            }
        }

        public bool Available => _client != null && _client.Available;

        public bool IsSpeaking => throw new NotImplementedException();



        public async Task<string?> SpeakAsync(string text, string? language = null, float? speed = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (_client == null || !_client.Available) return null;

            try
            {
                // create temp dir
                var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TTSTemp");
                Directory.CreateDirectory(dir);

                // create deterministic filename based on SHA1 of text + language + speed
                using var sha1 = SHA1.Create();
                var meta = (text ?? string.Empty) + "|" + (language ?? string.Empty) + "|" + (speed?.ToString() ?? string.Empty);
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(meta));
                var sb = new StringBuilder();
                foreach (var b in hash) sb.Append(b.ToString("x2"));
                var fname = sb.ToString() + ".wav";
                var path = Path.Combine(dir, fname);

                if (File.Exists(path)) return path;

                var wav = await _client.SynthesizeAsync(text: text, voice: "Serena", language: language ?? "English", speed: speed ?? 1.0f, format: "wav").ConfigureAwait(false);

                await File.WriteAllBytesAsync(path, wav).ConfigureAwait(false);
                return path;
            }
            catch
            {
                return null;
            }
        }

        public async Task<byte[]?> SpeakSteamAsync(string text, string? language = null, float? speed = null, string? format = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (_client == null || !_client.Available) return null;
            try
            {
                var fmt = string.IsNullOrWhiteSpace(format) ? "wav" : format;
                var bytes = await _client.SynthesizeAsync(text: text, voice: "Serena", language: language ?? "English", speed: speed ?? 1.0f, format: fmt).ConfigureAwait(false);
                return bytes;
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            try { _client?.Dispose(); } catch { }
        }

        public Task StopAsync()
        {
            throw new NotImplementedException();
        }


    }
}

