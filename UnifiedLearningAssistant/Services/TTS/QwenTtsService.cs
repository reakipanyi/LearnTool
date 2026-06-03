using System.Media;
using System.Security.Cryptography;
using System.Text;

namespace UnifiedLearningAssistant.Services.TTS
{
    public class QwenTtsService : ITTSService
    {
        private readonly QwenTtsClient? _client;
        private const long MaxCacheSizeBytes = 100 * 1024 * 1024; // 100MB 缓存上限
        private static readonly string CacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TTSTemp");

        public QwenTtsService(string? apiKey, string? endpoint)
        {
            try
            {
                _client = new QwenTtsClient(apiKey, endpoint);
                CleanupOldCache();
            }
            catch
            {
                _client = null;
            }
        }

        public bool Available => _client != null && _client.Available;

        public bool IsSpeaking => false;



        public async Task<string?> SpeakAsync(string text, string? language = null, float? speed = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (_client == null || !_client.Available) return null;

            try
            {
                Directory.CreateDirectory(CacheDirectory);

                // create deterministic filename based on SHA1 of text + language + speed
                string path = GetCacheFilePath(text, language, speed);

                if (File.Exists(path))
                {
                    File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
                    return path;
                }

                // 转换语言代码为完整语言名称
                string lang = language switch
                {
                    "zh" => "Chinese",
                    "en" => "English",
                    _ => language ?? "English"
                };

                var wav = await _client.SynthesizeAsync(text: text, voice: "Cherry", language: lang, speed: speed ?? 1.0f, format: "wav").ConfigureAwait(false);

                await File.WriteAllBytesAsync(path, wav).ConfigureAwait(false);


                using (var player = new SoundPlayer(path))
                {
                    player.PlaySync();
                }


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
                
                // 转换语言代码为完整语言名称
                string lang = language switch
                {
                    "zh" => "Chinese",
                    "en" => "English",
                    _ => language ?? "English"
                };
                
                var bytes = await _client.SynthesizeAsync(text: text, voice: "Cherry", language: lang, speed: speed ?? 1.0f, format: fmt).ConfigureAwait(false);
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
            return Task.CompletedTask;
        }

        private string GetCacheFilePath(string text, string? language, float? speed)
        {
            using var sha1 = SHA1.Create();
            var meta = (text ?? string.Empty) + "|" + (language ?? string.Empty) + "|" + (speed?.ToString() ?? string.Empty);
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(meta));
            var sb = new StringBuilder();
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return Path.Combine(CacheDirectory, sb.ToString() + ".wav");
        }

        private void CleanupOldCache()
        {
            try
            {
                if (!Directory.Exists(CacheDirectory)) return;

                var files = new DirectoryInfo(CacheDirectory)
                    .GetFiles("*.wav")
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .ToList();

                long totalSize = files.Sum(f => f.Length);

                if (totalSize > MaxCacheSizeBytes)
                {
                    foreach (var file in files)
                    {
                        try
                        {
                            file.Delete();
                            totalSize -= file.Length;
                            if (totalSize <= MaxCacheSizeBytes * 0.8)
                                break;
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }


    }
}

