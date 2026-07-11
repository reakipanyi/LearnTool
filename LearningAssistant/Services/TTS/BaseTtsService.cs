using LearningAssistant.Common;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System.Security.Cryptography;
using System.Text;

namespace LearningAssistant.Services.TTS
{
    public abstract class BaseTtsService : ITTSService, IDisposable
    {
        protected readonly ILogger? _logger;
        protected const long MaxCacheSizeBytes = 100 * 1024 * 1024;

        private WaveOutEvent? _waveOut;
        private AudioFileReader? _audioReader;
        private WaveStream? _playbackStream;
        protected volatile bool _stopRequested = false;
        private readonly object _playerLock = new object();
        private bool _disposed = false;
        private TaskCompletionSource<bool>? _playbackTcs;

        public abstract bool Available { get; }

        public bool IsSpeaking
        {
            get
            {
                lock (_playerLock)
                {
                    return _waveOut != null && !_stopRequested && _waveOut.PlaybackState == PlaybackState.Playing;
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

        protected async Task PlayAudioAsync(string filePath, float volume = 1.0f, float speed = 1.0f, CancellationToken cancellationToken = default)
        {
            _playbackTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            WaveOutEvent? waveOut = null;
            AudioFileReader? audioReader = null;
            WaveStream? playbackStream = null;
            bool playerAssigned = false;

            try
            {
                audioReader = new AudioFileReader(filePath);

                float safeVolume = Math.Clamp(volume, 0f, 2f);
                audioReader.Volume = safeVolume;

                float safeSpeed = Math.Clamp(speed, 0.5f, 2.0f);
                if (Math.Abs(safeSpeed - 1.0f) > 0.001f)
                {
                    playbackStream = new VarispeedWaveStream(audioReader, safeSpeed);
                    _logger?.LogDebug("PlayAudioAsync: using playback speed={Speed}", safeSpeed);
                }
                else
                {
                    playbackStream = audioReader;
                }

                waveOut = new WaveOutEvent();
                waveOut.PlaybackStopped += (s, e) =>
                {
                    try
                    {
                        if (e.Exception != null)
                        {
                            _playbackTcs?.TrySetException(e.Exception);
                        }
                        else
                        {
                            _playbackTcs?.TrySetResult(true);
                        }
                    }
                    catch { }
                };

                waveOut.Init(playbackStream);

                lock (_playerLock)
                {
                    StopPlaybackInternal();
                    _stopRequested = false;
                    _waveOut = waveOut;
                    _audioReader = audioReader;
                    _playbackStream = playbackStream;
                    playerAssigned = true;
                }

                if (_stopRequested || cancellationToken.IsCancellationRequested)
                {
                    _logger?.LogDebug("PlayAudioAsync: stop requested before playback");
                    return;
                }

                var fileInfo = new FileInfo(filePath);
                _logger?.LogInformation("PlayAudioAsync: starting playback, file={FilePath}, size={Size}, volume={Volume}, speed={Speed}",
                    filePath, fileInfo.Length, safeVolume, safeSpeed);

                waveOut.Play();

                using (cancellationToken.Register(() =>
                {
                    try { waveOut?.Stop(); } catch { }
                }))
                {
                    await _playbackTcs.Task.ConfigureAwait(false);
                }

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
                if (!playerAssigned)
                {
                    playbackStream?.Dispose();
                    audioReader?.Dispose();
                    waveOut?.Dispose();
                }
            }
        }

        protected async Task PlayAudioFromStreamAsync(Stream stream, float volume = 1.0f, float speed = 1.0f, CancellationToken cancellationToken = default)
        {
            _playbackTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            WaveOutEvent? waveOut = null;
            WaveFileReader? waveFileReader = null;
            bool playerAssigned = false;

            try
            {
                waveFileReader = new WaveFileReader(stream);

                float safeVolume = Math.Clamp(volume, 0f, 2f);

                waveOut = new WaveOutEvent();
                waveOut.Volume = safeVolume;
                waveOut.PlaybackStopped += (s, e) =>
                {
                    try
                    {
                        if (e.Exception != null)
                        {
                            _playbackTcs?.TrySetException(e.Exception);
                        }
                        else
                        {
                            _playbackTcs?.TrySetResult(true);
                        }
                    }
                    catch { }
                };

                waveOut.Init(waveFileReader);

                lock (_playerLock)
                {
                    StopPlaybackInternal();
                    _stopRequested = false;
                    _waveOut = waveOut;
                    playerAssigned = true;
                }

                if (_stopRequested || cancellationToken.IsCancellationRequested)
                {
                    _logger?.LogDebug("PlayAudioFromStreamAsync: stop requested before playback");
                    return;
                }

                _logger?.LogInformation("PlayAudioFromStreamAsync: starting playback, streamLength={Length}, volume={Volume}, speed={Speed}",
                    stream.Length, safeVolume, speed);

                waveOut.Play();

                using (cancellationToken.Register(() =>
                {
                    try { waveOut?.Stop(); } catch { }
                }))
                {
                    await _playbackTcs.Task.ConfigureAwait(false);
                }

                _logger?.LogInformation("PlayAudioFromStreamAsync: playback completed");
            }
            catch (OperationCanceledException)
            {
                _logger?.LogDebug("PlayAudioFromStreamAsync: cancelled");
                StopPlayback();
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "PlayAudioFromStreamAsync: Failed to play audio from stream");
                if (!playerAssigned)
                {
                    waveFileReader?.Dispose();
                    waveOut?.Dispose();
                }
            }
        }

        private class VarispeedWaveStream : WaveStream
        {
            private readonly AudioFileReader _source;
            private readonly float _speed;
            private readonly WaveFormat _waveFormat;
            private readonly long _length;

            public VarispeedWaveStream(AudioFileReader source, float speed)
            {
                _source = source;
                _speed = speed;
                int newSampleRate = (int)(source.WaveFormat.SampleRate / speed);
                _waveFormat = new WaveFormat(newSampleRate, source.WaveFormat.BitsPerSample, source.WaveFormat.Channels);
                _length = source.Length;
            }

            public override WaveFormat WaveFormat => _waveFormat;
            public override long Length => _length;

            public override long Position
            {
                get => _source.Position;
                set => _source.Position = value;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _source.Read(buffer, offset, count);
            }
        }

        private void StopPlaybackInternal()
        {
            if (_waveOut != null)
            {
                try
                {
                    _waveOut.Stop();
                }
                catch { }
                try
                {
                    _waveOut.Dispose();
                }
                catch { }
                _waveOut = null;
            }
            if (_playbackStream != null)
            {
                try
                {
                    _playbackStream.Dispose();
                }
                catch { }
                _playbackStream = null;
            }
            if (_audioReader != null)
            {
                try
                {
                    _audioReader.Dispose();
                }
                catch { }
                _audioReader = null;
            }
        }

        protected void StopPlayback()
        {
            lock (_playerLock)
            {
                _stopRequested = true;
                StopPlaybackInternal();
                _playbackTcs?.TrySetCanceled();
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
