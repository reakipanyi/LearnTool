using LearningAssistant.Common;
using Microsoft.Extensions.Logging;
using System.Media;
using System.Security.Cryptography;
using System.Text;

namespace LearningAssistant.Services.TTS
{
    public abstract class BaseTtsService : ITTSService, IDisposable
    {
        protected readonly ILogger? _logger;
        protected const long MaxCacheSizeBytes = 100 * 1024 * 1024;

        private SoundPlayer? _currentPlayer;
        protected volatile bool _stopRequested = false;
        private readonly object _playerLock = new object();
        private bool _disposed = false;

        public abstract bool Available { get; }

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

        protected BaseTtsService(ILogger? logger)
        {
            _logger = logger;
        }

        public abstract Task<string?> SpeakAsync(string text, string? language = null, float? speed = null, CancellationToken cancellationToken = default);

        public abstract Task<byte[]?> SpeakStreamAsync(string text, string? language = null, float? speed = null, string? format = null);

        public abstract Task<string?> SpeakToCacheAsync(string text, string? language = null, float? speed = null, CancellationToken cancellationToken = default);

        protected async Task PlayAudioAsync(string filePath, CancellationToken cancellationToken = default)
        {
            _stopRequested = false;

            SoundPlayer? player = null;
            bool playerAssigned = false;

            try
            {
                player = new SoundPlayer(filePath);
                player.Load();

                lock (_playerLock)
                {
                    _currentPlayer?.Dispose();
                    _currentPlayer = player;
                    playerAssigned = true;
                }

                if (_stopRequested || cancellationToken.IsCancellationRequested)
                {
                    _logger?.LogDebug("PlayAudioAsync: stop requested before playback");
                    return;
                }

                var fileInfo = new FileInfo(filePath);
                _logger?.LogInformation("PlayAudioAsync: starting playback, file={FilePath}, size={Size}", filePath, fileInfo.Length);

                var playbackTask = Task.Run(() =>
                {
                    try
                    {
                        player!.PlaySync();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "PlayAudioAsync: PlaySync failed for {FilePath}", filePath);
                    }
                }, cancellationToken);

                await playbackTask.WaitAsync(cancellationToken).ConfigureAwait(false);

                _logger?.LogInformation("PlayAudioAsync: playback completed");
            }
            catch (OperationCanceledException)
            {
                _logger?.LogDebug("PlayAudioAsync: cancelled");
                StopPlayback();
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "PlayAudioAsync: Failed to load or play audio file {FilePath}", filePath);
                if (!playerAssigned && player != null)
                {
                    player.Dispose();
                }
            }
        }

        protected void StopPlayback()
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

        public Task StopAsync()
        {
            StopPlayback();
            return Task.CompletedTask;
        }

        protected string GetCacheFilePath(string text, string? language, float? speed, string? voiceName = null)
        {
            using var sha1 = SHA1.Create();
            var meta = (voiceName ?? string.Empty) + "|" + (text ?? string.Empty) + "|" + (language ?? string.Empty) + "|" + (speed?.ToString() ?? string.Empty);
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(meta));
            var sb = new StringBuilder();
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return Path.Combine(AppPaths.GetUserTtsCacheDir(), sb.ToString() + ".wav");
        }

        protected void CleanupOldCache()
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

        protected static string MapLanguageCode(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return "English";

            var lang = language.ToLowerInvariant();
            return lang switch
            {
                "zh" or "zh-cn" or "zh-cn" => "Chinese",
                "en" or "en-us" or "en-gb" => "English",
                _ => language
            };
        }

        public virtual void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            StopPlayback();
        }
    }
}