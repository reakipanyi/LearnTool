using LearningAssistant.Common;
using Microsoft.Extensions.Logging;
using System.Media;
using System.Security.Cryptography;
using System.Text;

namespace LearningAssistant.Services.TTS
{
    public class QwenTtsService : ITTSService
    {
        private readonly QwenTtsClient? _client;
        private readonly ILogger<QwenTtsService>? _logger;
        private const long MaxCacheSizeBytes = 100 * 1024 * 1024; // 100MB 缓存上限

        private SoundPlayer? _currentPlayer;
        private bool _stopRequested = false;
        private readonly object _playerLock = new object();

        public QwenTtsService(string? apiKey, string? endpoint, ILogger<QwenTtsService>? logger = null)
        {
            _logger = logger;
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

        public bool Available => _client != null && _client.Available;

        public bool IsSpeaking
        {
            get
            {
                lock (_playerLock)
                {
                    return _currentPlayer != null && !_stopRequested;
                }
            }
        }



        public async Task<string?> SpeakAsync(string text, string? language = null, float? speed = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (_client == null || !_client.Available) return null;

            StopPlayback();

            try
            {
                Directory.CreateDirectory(AppPaths.GetUserTtsCacheDir());

                string path = GetCacheFilePath(text, language, speed);

                if (File.Exists(path))
                {
                    File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
                }
                else
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string lang = language switch
                    {
                        "zh" => "Chinese",
                        "en" => "English",
                        _ => language ?? "English"
                    };

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

        private async Task PlayAudioAsync(string filePath, CancellationToken cancellationToken = default)
        {
            _stopRequested = false;

            SoundPlayer player = null;
            try
            {
                player = new SoundPlayer(filePath);
                player.Load();

                lock (_playerLock)
                {
                    _currentPlayer?.Dispose();
                    _currentPlayer = player;
                }

                if (_stopRequested || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var playbackTask = Task.Run(() =>
                {
                    try
                    {
                        player.PlaySync();
                    }
                    catch
                    {
                    }
                }, cancellationToken);

                await playbackTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                StopPlayback();
                throw;
            }
            catch (Exception)
            {
            }
        }

        private void StopPlayback()
        {
            lock (_playerLock)
            {
                _stopRequested = true;
                if (_currentPlayer != null)
                {
                    try
                    {
                        _currentPlayer.Stop();
                        _currentPlayer.Dispose();
                    }
                    catch { }
                    _currentPlayer = null;
                }
            }
        }

        public async Task<string?> SpeakToCacheAsync(string text, string? language = null, float? speed = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (_client == null || !_client.Available) return null;

            try
            {
                Directory.CreateDirectory(AppPaths.GetUserTtsCacheDir());

                string path = GetCacheFilePath(text, language, speed);

                if (File.Exists(path))
                {
                    _logger?.LogDebug("SpeakToCacheAsync: already cached, path={Path}", path);
                    return path;
                }

                string lang = language switch
                {
                    "zh" => "Chinese",
                    "en" => "English",
                    _ => language ?? "English"
                };

                var wav = await _client.SynthesizeAsync(text: text, voice: "Cherry", language: lang, speed: speed ?? 1.0f, format: "wav").ConfigureAwait(false);

                await File.WriteAllBytesAsync(path, wav).ConfigureAwait(false);
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

        public async Task<byte[]?> SpeakStreamAsync(string text, string? language = null, float? speed = null, string? format = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (_client == null || !_client.Available) return null;
            try
            {
                var fmt = string.IsNullOrWhiteSpace(format) ? "wav" : format;

                string lang = language switch
                {
                    "zh" => "Chinese",
                    "en" => "English",
                    _ => language ?? "English"
                };

                var bytes = await _client.SynthesizeAsync(text: text, voice: "Cherry", language: lang, speed: speed ?? 1.0f, format: fmt).ConfigureAwait(false);
                return bytes;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "TTS speak stream failed for text: {Text}", text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                throw;
            }
        }

        public void Dispose()
        {
            StopPlayback();
            try { _client?.Dispose(); } catch { }
        }

        public Task StopAsync()
        {
            StopPlayback();
            return Task.CompletedTask;
        }

        private string GetCacheFilePath(string text, string? language, float? speed)
        {
            using var sha1 = SHA1.Create();
            var meta = (text ?? string.Empty) + "|" + (language ?? string.Empty) + "|" + (speed?.ToString() ?? string.Empty);
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(meta));
            var sb = new StringBuilder();
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return Path.Combine(AppPaths.GetUserTtsCacheDir(), sb.ToString() + ".wav");
        }

        private void CleanupOldCache()
        {
            try
            {
                if (!Directory.Exists(AppPaths.GetUserTtsCacheDir())) return;

                var files = new DirectoryInfo(AppPaths.GetUserTtsCacheDir())
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

