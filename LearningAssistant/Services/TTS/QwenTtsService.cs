using LearningAssistant.Common;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.TTS
{
    public class QwenTtsService : BaseTtsService
    {
        private readonly QwenTtsClient? _client;

        public QwenTtsService(string? apiKey, string? endpoint, ILogger<QwenTtsService>? logger = null)
            : base(logger)
        {
            try
            {
                _client = new QwenTtsClient(apiKey, endpoint);
                CleanupOldCache();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize QwenTtsService");
                _client = null;
            }
        }

        public override bool Available => _client != null && _client.Available;

        public override async Task<string?> SpeakAsync(string text, string? language = null, float? speed = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (_client == null || !_client.Available) return null;

            StopPlayback();

            try
            {
                Directory.CreateDirectory(AppPaths.GetUserTtsCacheDir());

                string lang = MapLanguageCode(language ?? "English");
                string path = GetCacheFilePath(text, language, speed);

                if (File.Exists(path))
                {
                    File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
                }
                else
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var wav = await _client.SynthesizeAsync(text: text, voice: "Cherry", language: lang, speed: speed ?? 1.0f, format: "wav").ConfigureAwait(false);

                    await File.WriteAllBytesAsync(path, wav, cancellationToken).ConfigureAwait(false);
                }

                await PlayAudioAsync(path, cancellationToken);

                return path;
            }
            catch (OperationCanceledException)
            {
                _logger?.LogDebug("SpeakAsync: cancelled");
                StopPlayback();
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "TTS speak failed for text: {Text}", text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                StopPlayback();
                throw;
            }
        }

        public override async Task<string?> SpeakToCacheAsync(string text, string? language = null, float? speed = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (_client == null || !_client.Available) return null;

            try
            {
                Directory.CreateDirectory(AppPaths.GetUserTtsCacheDir());

                string lang = MapLanguageCode(language ?? "English");
                string path = GetCacheFilePath(text, language, speed);

                if (File.Exists(path))
                {
                    _logger?.LogDebug("SpeakToCacheAsync: already cached, path={Path}", path);
                    return path;
                }

                var wav = await _client.SynthesizeAsync(text: text, voice: "Cherry", language: lang, speed: speed ?? 1.0f, format: "wav").ConfigureAwait(false);

                await File.WriteAllBytesAsync(path, wav, cancellationToken).ConfigureAwait(false);
                _logger?.LogDebug("SpeakToCacheAsync: audio cached, path={Path}", path);

                CleanupOldCache();
                return path;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "TTS cache failed for text: {Text}", text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                return null;
            }
        }

        public override async Task<byte[]?> SpeakStreamAsync(string text, string? language = null, float? speed = null, string? format = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (_client == null || !_client.Available) return null;

            try
            {
                var fmt = string.IsNullOrWhiteSpace(format) ? "wav" : format;
                var lang = MapLanguageCode(language ?? "English");

                var bytes = await _client.SynthesizeAsync(text: text, voice: "Cherry", language: lang, speed: speed ?? 1.0f, format: fmt).ConfigureAwait(false);
                return bytes;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "TTS speak stream failed for text: {Text}", text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                throw;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            try { _client?.Dispose(); } catch { }
        }
    }
}