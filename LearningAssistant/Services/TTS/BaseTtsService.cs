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
                    filePath, fileInfo.Length, safeVolume, speed);

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
            private readonly byte[] _buffer;
            private readonly WaveFormat _waveFormat;
            private long _position;

            public VarispeedWaveStream(AudioFileReader source, float speed)
            {
                _waveFormat = source.WaveFormat;

                // 读取全部float样本
                int bytesPerSample = source.WaveFormat.BitsPerSample / 8;
                int sampleCount = (int)(source.Length / (bytesPerSample * source.WaveFormat.Channels));
                float[] inputSamples = new float[sampleCount];
                source.Read(inputSamples, 0, sampleCount);

                // OLA时间拉伸（变速不变调）
                float[] stretched = TimeStretchOla(inputSamples, speed);

                // 转回bytes
                _buffer = new byte[stretched.Length * 4];
                Buffer.BlockCopy(stretched, 0, _buffer, 0, _buffer.Length);
                _position = 0;
            }

            /// <summary>
            /// WSOLA(波形相似重叠相加)时间拉伸算法，变速不变调，无重音
            /// </summary>
            private static float[] TimeStretchOla(float[] input, float speed)
            {
                if (Math.Abs(speed - 1.0f) < 0.001f) return input;

                int inputLength = input.Length;
                int outputLength = (int)(inputLength / speed) + 4096;
                float[] output = new float[outputLength];

                const int frameSize = 2048; // ~85ms at 24000Hz，帧越大重音越少
                int hopIn = frameSize / 4;  // 输入步进
                int hopOut = (int)(hopIn / speed); // 输出步进
                int searchRange = hopIn;    // 搜索范围

                // Hann窗
                float[] window = new float[frameSize];
                for (int i = 0; i < frameSize; i++)
                {
                    window[i] = 0.5f * (1.0f - (float)Math.Cos(2.0 * Math.PI * i / (frameSize - 1)));
                }

                // 第一帧直接复制
                int firstLen = Math.Min(frameSize, inputLength);
                for (int i = 0; i < firstLen; i++)
                {
                    output[i] = input[i] * window[i];
                }

                int outPos = hopOut;
                int inPos = hopIn;

                while (inPos + frameSize <= inputLength && outPos + frameSize <= outputLength)
                {
                    // WSOLA: 归一化交叉相关搜索最佳对齐
                    int overlapLen = Math.Min(hopOut, frameSize);
                    int bestDelta = 0;
                    float bestCorr = float.MinValue;

                    int searchStart = Math.Max(0, inPos - searchRange);
                    int searchEnd = Math.Min(inputLength - frameSize, inPos + searchRange);

                    int outBase = outPos - hopOut;

                    for (int search = searchStart; search <= searchEnd; search++)
                    {
                        float corr = 0;
                        float normOut = 0;
                        float normIn = 0;

                        for (int i = 0; i < overlapLen; i++)
                        {
                            float o = output[outBase + i];
                            float s = input[search + i];
                            corr += o * s;
                            normOut += o * o;
                            normIn += s * s;
                        }

                        // 归一化交叉相关
                        float denom = (float)Math.Sqrt(normOut * normIn) + 1e-10f;
                        float normCorr = corr / denom;

                        if (normCorr > bestCorr)
                        {
                            bestCorr = normCorr;
                            bestDelta = search - inPos;
                        }
                    }

                    int alignedInPos = inPos + bestDelta;

                    // 用最佳对齐位置复制帧
                    for (int i = 0; i < frameSize; i++)
                    {
                        output[outPos + i] += input[alignedInPos + i] * window[i];
                    }

                    inPos += hopIn;
                    outPos += hopOut;
                }

                int actualLength = (int)(inputLength / speed);
                Array.Resize(ref output, Math.Min(actualLength, outputLength));
                return output;
            }

            public override WaveFormat WaveFormat => _waveFormat;
            public override long Length => _buffer.Length;

            public override long Position
            {
                get => _position;
                set => _position = Math.Clamp(value, 0, _buffer.Length);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int toRead = Math.Min(count, _buffer.Length - (int)_position);
                if (toRead <= 0) return 0;
                Array.Copy(_buffer, _position, buffer, offset, toRead);
                _position += toRead;
                return toRead;
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
            return Path.Combine(AppPaths.GetTtsCacheDir(), sb.ToString() + ".wav");
        }

        protected void CleanupOldCache()
        {
            try
            {
                if (!Directory.Exists(AppPaths.GetTtsCacheDir())) return;

                var files = new DirectoryInfo(AppPaths.GetTtsCacheDir())
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
