using LearningAssistant.Common;
using LearningAssistant.Models.Config;
using Microsoft.Extensions.Logging;
using System.Media;
using System.Security.Cryptography;
using System.Text;
using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Processing;
using KokoroSharp.Utilities;

namespace LearningAssistant.Services.TTS
{
    public class KokoroSharpTtsService : ITTSService, IDisposable
    {
        private readonly ILogger<KokoroSharpTtsService>? _logger;
        private readonly TtsConfig _config;
        private KokoroTTS? _tts;
        private KokoroVoice? _defaultVoice;
        private const long MaxCacheSizeBytes = 100 * 1024 * 1024;
        private const int MaxTokensPerSegment = 400;
        private const int SynthesizeTimeoutMs = 300000;

        private SoundPlayer? _currentPlayer;
        private volatile bool _stopRequested = false;
        private readonly object _playerLock = new object();
        private volatile bool _isInitialized = false;
        private volatile bool _isLoading = false;
        private readonly object _initLock = new object();
        private bool _disposed = false;

        public KokoroSharpTtsService(TtsConfig config, ILogger<KokoroSharpTtsService>? logger = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger;
        }

        private void EnsureInitialized()
        {
            if (_isInitialized) return;

            lock (_initLock)
            {
                if (_isInitialized) return;

                try
                {
                    string modelPath = GetModelPath();
                    _logger?.LogInformation("Loading KokoroSharp model from: {ModelPath}", modelPath);
                    _tts = KokoroTTS.LoadModel(modelPath);

                    string voiceName = string.IsNullOrWhiteSpace(_config.Voice) ? "af_heart" : _config.Voice;
                    try
                    {
                        _defaultVoice = KokoroVoiceManager.GetVoice(voiceName);
                    }
                    catch (Exception vex)
                    {
                        _logger?.LogWarning("Voice '{VoiceName}' not found, falling back to 'af_heart': {Error}", voiceName, vex.Message);
                        _defaultVoice = null;
                    }
                    if (_defaultVoice == null)
                    {
                        try
                        {
                            _defaultVoice = KokoroVoiceManager.GetVoice("af_heart");
                        }
                        catch (Exception fex)
                        {
                            _logger?.LogError(fex, "Failed to load fallback voice 'af_heart'");
                            _defaultVoice = null;
                        }
                    }

                    CleanupOldCache();
                    _isInitialized = true;
                    _logger?.LogInformation("KokoroSharp TTS initialized successfully with voice: {Voice}", _defaultVoice?.Name ?? "af_heart");
                    _ = WarmupAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to initialize KokoroSharp TTS");
                    _tts = null;
                    _defaultVoice = null;
                    _isInitialized = false;
                }
            }
        }

        private async Task WarmupAsync()
        {
            try
            {
                _logger?.LogInformation("KokoroSharp TTS warmup starting...");
                var sw = System.Diagnostics.Stopwatch.StartNew();
                int[] tokens = Tokenizer.Tokenize("Hello.", "a", preprocess: true);
                var job = KokoroJob.Create(tokens, _defaultVoice, 1.0f, OnComplete: (_) => { });
                _tts?.EnqueueJob(job);
                await Task.Delay(50);
                sw.Stop();
                _logger?.LogInformation("KokoroSharp TTS warmup completed in {Elapsed}ms", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "KokoroSharp TTS warmup failed");
            }
        }

        public void StartBackgroundInitialization()
        {
            if (_isInitialized || _isLoading) return;
            _isLoading = true;

            Task.Run(() =>
            {
                try
                {
                    EnsureInitialized();
                }
                finally
                {
                    _isLoading = false;
                }
            });
        }

        private string GetModelPath()
        {
            if (!string.IsNullOrWhiteSpace(_config.Model) && File.Exists(_config.Model))
            {
                return _config.Model;
            }

            string assemblyDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] searchPaths = new[]
            {
                Path.Combine(assemblyDir, "kokoro.onnx"),
                Path.Combine(assemblyDir, "models", "kokoro.onnx"),
                Path.Combine(assemblyDir, "KokoroModels", "kokoro.onnx"),
                Path.Combine(AppPaths.DataRoot, "kokoro.onnx"),
                Path.Combine(AppPaths.DataRoot, "models", "kokoro.onnx"),
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    _logger?.LogDebug("Found Kokoro model at: {Path}", path);
                    return path;
                }
            }

            if (!string.IsNullOrWhiteSpace(_config.Model))
            {
                return _config.Model;
            }

            return "kokoro.onnx";
        }

        public bool Available
        {
            get
            {
                return _isInitialized && _tts != null && _defaultVoice != null;
            }
        }

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

        public async Task<string?> SpeakAsync(string text, string? language = null, float? speed = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger?.LogWarning("SpeakAsync: text is empty");
                return null;
            }
            EnsureInitialized();
            if (!Available)
            {
                _logger?.LogWarning("SpeakAsync: not available after EnsureInitialized (isInit={IsInit}, hasTts={HasTts}, hasVoice={HasVoice})", 
                    _isInitialized, _tts != null, _defaultVoice != null);
                return null;
            }

            StopPlayback();

            try
            {
                Directory.CreateDirectory(AppPaths.GetUserTtsCacheDir());

                string path = GetCacheFilePath(text, language, speed);

                if (File.Exists(path))
                {
                    File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
                    _logger?.LogInformation("SpeakAsync: using cached audio");
                    await PlayAudioAsync(path);
                    return path;
                }

                _logger?.LogInformation("SpeakAsync: synthesizing new audio, text length={Len}", text.Length);
                var sw = System.Diagnostics.Stopwatch.StartNew();

                float actualSpeed = speed ?? _config.Speed;
                var wavBytes = await SynthesizeAndPlayStreamAsync(text, language, actualSpeed).ConfigureAwait(false);
                
                sw.Stop();
                _logger?.LogInformation("SpeakAsync: total synthesis time {Elapsed}ms", sw.ElapsedMilliseconds);

                if (wavBytes == null || wavBytes.Length == 0)
                {
                    _logger?.LogWarning("SpeakAsync: synthesis returned empty result");
                    return null;
                }

                await File.WriteAllBytesAsync(path, wavBytes).ConfigureAwait(false);

                return path;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "KokoroSharp TTS speak failed for text: {Text}", text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                StopPlayback();
                throw;
            }
        }

        public async Task<byte[]?> SpeakSteamAsync(string text, string? language = null, float? speed = null, string? format = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            EnsureInitialized();
            if (!Available) return null;

            try
            {
                var fmt = string.IsNullOrWhiteSpace(format) ? "wav" : format.ToLowerInvariant();
                var wavBytes = await SynthesizeToWavAsync(text, language, speed ?? _config.Speed).ConfigureAwait(false);

                if (fmt == "wav")
                    return wavBytes;

                _logger?.LogWarning("KokoroSharp TTS only supports WAV format natively, requested: {Format}", fmt);
                return wavBytes;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "KokoroSharp TTS speak stream failed for text: {Text}", text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                throw;
            }
        }

        private async Task<byte[]?> SynthesizeAndPlayStreamAsync(string text, string? language, float speed)
        {
            if (_tts == null || _defaultVoice == null) return null;

            float safeSpeed = speed;
            if (safeSpeed <= 0 || float.IsNaN(safeSpeed) || float.IsInfinity(safeSpeed))
            {
                _logger?.LogWarning("Invalid speed value: {Speed}, using default 1.0", speed);
                safeSpeed = 1.0f;
            }
            else if (safeSpeed < 0.5f)
            {
                safeSpeed = 0.5f;
            }
            else if (safeSpeed > 2.0f)
            {
                safeSpeed = 2.0f;
            }

            var voice = _defaultVoice;
            if (!string.IsNullOrWhiteSpace(language))
            {
                var langVoice = GetVoiceForLanguage(language);
                if (langVoice != null)
                    voice = langVoice;
            }

            string langCode = voice.GetLangCode();
            string trimmedText = text.Trim();

            var segments = SplitTextIntoSegments(trimmedText, langCode);
            if (segments.Count == 0)
                return null;

            if (segments.Count == 1)
            {
                var tokens = Tokenizer.Tokenize(segments[0], langCode, preprocess: true);
                var wavBytes = await SynthesizeTokensToWavAsync(tokens, voice, safeSpeed).ConfigureAwait(false);
                if (wavBytes != null && wavBytes.Length > 0)
                {
                    await PlayAudioFromBytesAsync(wavBytes).ConfigureAwait(false);
                }
                return wavBytes;
            }

            _logger?.LogInformation("Streaming synthesize & play: {SegmentCount} segments", segments.Count);
            var allSamples = new List<float>();
            var segmentWavFiles = new List<string>();
            int currentPlayIndex = 0;
            bool playStarted = false;
            var allReadyTcs = new TaskCompletionSource<bool>();
            var segmentResults = new byte[segments.Count][];
            int completedCount = 0;

            for (int i = 0; i < segments.Count; i++)
            {
                int idx = i;
                var tokens = Tokenizer.Tokenize(segments[idx], langCode, preprocess: true);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var wav = await SynthesizeTokensToWavAsync(tokens, voice, safeSpeed).ConfigureAwait(false);
                        segmentResults[idx] = wav ?? Array.Empty<byte>();
                    }
                    catch
                    {
                        segmentResults[idx] = Array.Empty<byte>();
                    }
                    finally
                    {
                        Interlocked.Increment(ref completedCount);
                        if (Volatile.Read(ref completedCount) >= segments.Count)
                        {
                            allReadyTcs.TrySetResult(true);
                        }
                    }
                });
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (currentPlayIndex < segments.Count)
            {
                if (segmentResults[currentPlayIndex] != null)
                {
                    var wavBytes = segmentResults[currentPlayIndex];
                    if (wavBytes.Length > 0)
                    {
                        var samples = ExtractPcmFloatFromWav(wavBytes);
                        allSamples.AddRange(samples);

                        if (!playStarted)
                        {
                            playStarted = true;
                            sw.Stop();
                            _logger?.LogInformation("First segment ready in {Elapsed}ms, starting playback", sw.ElapsedMilliseconds);
                        }

                        await PlayAudioFromBytesAsync(wavBytes).ConfigureAwait(false);

                        if (_stopRequested)
                            break;
                    }
                    currentPlayIndex++;
                }
                else
                {
                    if (Volatile.Read(ref completedCount) >= segments.Count)
                        break;
                    await Task.Delay(20).ConfigureAwait(false);
                }
            }

            if (allSamples.Count == 0)
                return null;

            return ConvertPcmFloatToWav(allSamples.ToArray(), 24000, 1);
        }

        private async Task PlayAudioFromBytesAsync(byte[] wavBytes)
        {
            if (wavBytes == null || wavBytes.Length == 0 || _stopRequested) return;

            string tempFile = Path.Combine(AppPaths.GetUserTtsCacheDir(), $"_temp_{Guid.NewGuid():N}.wav");
            try
            {
                await File.WriteAllBytesAsync(tempFile, wavBytes).ConfigureAwait(false);
                await PlayAudioAsync(tempFile).ConfigureAwait(false);
            }
            finally
            {
                try { File.Delete(tempFile); } catch { }
            }
        }

        private async Task<byte[]?> SynthesizeToWavAsync(string text, string? language, float speed)
        {
            if (_tts == null || _defaultVoice == null) return null;

            float safeSpeed = speed;
            if (safeSpeed <= 0 || float.IsNaN(safeSpeed) || float.IsInfinity(safeSpeed))
            {
                _logger?.LogWarning("Invalid speed value: {Speed}, using default 1.0", speed);
                safeSpeed = 1.0f;
            }
            else if (safeSpeed < 0.5f)
            {
                safeSpeed = 0.5f;
            }
            else if (safeSpeed > 2.0f)
            {
                safeSpeed = 2.0f;
            }

            _logger?.LogInformation("SynthesizeToWavAsync: inputSpeed={InputSpeed}, safeSpeed={SafeSpeed}, configSpeed={ConfigSpeed}", 
                speed, safeSpeed, _config.Speed);

            var voice = _defaultVoice;
            if (!string.IsNullOrWhiteSpace(language))
            {
                var langVoice = GetVoiceForLanguage(language);
                if (langVoice != null)
                    voice = langVoice;
            }

            string langCode = voice.GetLangCode();
            string trimmedText = text.Trim();

            var segments = SplitTextIntoSegments(trimmedText, langCode);
            if (segments.Count == 0)
                return null;

            if (segments.Count == 1)
            {
                var tokens = Tokenizer.Tokenize(segments[0], langCode, preprocess: true);
                return await SynthesizeTokensToWavAsync(tokens, voice, safeSpeed).ConfigureAwait(false);
            }

            var allSamples = new List<float>();
            foreach (var segment in segments)
            {
                var tokens = Tokenizer.Tokenize(segment, langCode, preprocess: true);
                var wavBytes = await SynthesizeTokensToWavAsync(tokens, voice, safeSpeed).ConfigureAwait(false);
                if (wavBytes == null || wavBytes.Length == 0)
                    continue;

                var samples = ExtractPcmFloatFromWav(wavBytes);
                allSamples.AddRange(samples);
            }

            if (allSamples.Count == 0)
                return null;

            return ConvertPcmFloatToWav(allSamples.ToArray(), 24000, 1);
        }

        private async Task<byte[]?> SynthesizeTokensToWavAsync(int[] tokens, KokoroVoice voice, float speed)
        {
            var tcs = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);

            using var cts = new CancellationTokenSource(SynthesizeTimeoutMs);
            cts.Token.Register(() => tcs.TrySetCanceled());

            try
            {
                var job = KokoroJob.Create(tokens, voice, speed, OnComplete: (float[] samples) =>
                {
                    try
                    {
                        var wavBytes = ConvertPcmFloatToWav(samples, 24000, 1);
                        tcs.TrySetResult(wavBytes);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });

                _tts!.EnqueueJob(job);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return await tcs.Task.ConfigureAwait(false);
        }

        private List<string> SplitTextIntoSegments(string text, string langCode)
        {
            var segments = new List<string>();

            var sentences = SplitByPunctuation(text);

            var currentSegment = new StringBuilder();
            int currentTokenEstimate = 0;

            foreach (var sentence in sentences)
            {
                if (string.IsNullOrWhiteSpace(sentence))
                    continue;

                int sentenceTokenEstimate = EstimateTokenCount(sentence, langCode);

                if (currentTokenEstimate + sentenceTokenEstimate > MaxTokensPerSegment && currentSegment.Length > 0)
                {
                    segments.Add(currentSegment.ToString().Trim());
                    currentSegment.Clear();
                    currentTokenEstimate = 0;
                }

                if (sentenceTokenEstimate > MaxTokensPerSegment)
                {
                    if (currentSegment.Length > 0)
                    {
                        segments.Add(currentSegment.ToString().Trim());
                        currentSegment.Clear();
                        currentTokenEstimate = 0;
                    }

                    var subSegments = SplitLongSentence(sentence, langCode);
                    segments.AddRange(subSegments);
                }
                else
                {
                    currentSegment.Append(sentence);
                    currentTokenEstimate += sentenceTokenEstimate;
                }
            }

            if (currentSegment.Length > 0)
                segments.Add(currentSegment.ToString().Trim());

            return segments;
        }

        private static List<string> SplitByPunctuation(string text)
        {
            var sentences = new List<string>();
            var current = new StringBuilder();

            char[] sentenceEndings = { '.', '!', '?', '。', '！', '？', '；', ';', '\n', '\r' };

            for (int i = 0; i < text.Length; i++)
            {
                current.Append(text[i]);
                if (Array.Exists(sentenceEndings, c => c == text[i]))
                {
                    if (current.Length > 0 && !string.IsNullOrWhiteSpace(current.ToString()))
                    {
                        sentences.Add(current.ToString());
                        current.Clear();
                    }
                }
            }

            if (current.Length > 0 && !string.IsNullOrWhiteSpace(current.ToString()))
                sentences.Add(current.ToString());

            return sentences;
        }

        private List<string> SplitLongSentence(string sentence, string langCode)
        {
            var result = new List<string>();
            int estimatedTokens = EstimateTokenCount(sentence, langCode);

            if (estimatedTokens <= MaxTokensPerSegment)
            {
                result.Add(sentence);
                return result;
            }

            int approxCharsPerToken = langCode.StartsWith("zh") ? 1 : 4;
            int maxChars = MaxTokensPerSegment * approxCharsPerToken;

            for (int i = 0; i < sentence.Length; i += maxChars)
            {
                int length = Math.Min(maxChars, sentence.Length - i);
                string segment = sentence.Substring(i, length);
                if (!string.IsNullOrWhiteSpace(segment))
                    result.Add(segment);
            }

            return result;
        }

        private static int EstimateTokenCount(string text, string langCode)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            if (langCode.StartsWith("zh") || langCode == "ja")
            {
                int charCount = 0;
                foreach (char c in text)
                {
                    if (!char.IsWhiteSpace(c) && !char.IsPunctuation(c))
                        charCount++;
                }
                return Math.Max(1, charCount);
            }
            else
            {
                int wordCount = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
                return Math.Max(1, (int)(wordCount * 1.3));
            }
        }

        private static float[] ExtractPcmFloatFromWav(byte[] wavBytes)
        {
            const int headerSize = 44;
            if (wavBytes.Length <= headerSize)
                return Array.Empty<float>();

            int dataSize = wavBytes.Length - headerSize;
            int sampleCount = dataSize / 2;
            var samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                short intSample = BitConverter.ToInt16(wavBytes, headerSize + i * 2);
                samples[i] = (float)intSample / short.MaxValue;
            }

            return samples;
        }

        private KokoroVoice? GetVoiceForLanguage(string language)
        {
            string langCode = language.ToLowerInvariant();
            string voiceName;

            switch (langCode)
            {
                case "zh":
                case "zh-cn":
                case "chinese":
                case "mandarinchinese":
                    voiceName = "zf_tingting";
                    break;
                case "en":
                case "en-us":
                case "americanenglish":
                    voiceName = string.IsNullOrWhiteSpace(_config.Voice) ? "af_heart" : _config.Voice;
                    break;
                case "en-gb":
                case "britishenglish":
                    voiceName = "af_sarah";
                    break;
                case "ja":
                case "japanese":
                    voiceName = "jf_gongitsune";
                    break;
                case "es":
                case "spanish":
                    voiceName = "af_sara";
                    break;
                case "fr":
                case "french":
                    voiceName = "af_siobhan";
                    break;
                case "it":
                case "italian":
                    voiceName = "af_whisper";
                    break;
                case "hi":
                case "hindi":
                    voiceName = "af_beta";
                    break;
                case "pt":
                case "pt-br":
                case "brazilianportuguese":
                    voiceName = "af_heart";
                    break;
                default:
                    voiceName = string.IsNullOrWhiteSpace(_config.Voice) ? "af_heart" : _config.Voice;
                    break;
            }

            try
            {
                var voice = KokoroVoiceManager.GetVoice(voiceName);
                return voice ?? _defaultVoice;
            }
            catch
            {
                return _defaultVoice;
            }
        }

        private static byte[] ConvertPcmFloatToWav(float[] samples, int sampleRate, int channels)
        {
            int sampleCount = samples.Length;
            int bytesPerSample = 2;
            int dataSize = sampleCount * bytesPerSample;
            int headerSize = 44;
            int totalSize = headerSize + dataSize;

            byte[] wavBytes = new byte[totalSize];

            using (var ms = new MemoryStream(wavBytes))
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(new char[] { 'R', 'I', 'F', 'F' });
                writer.Write(totalSize - 8);
                writer.Write(new char[] { 'W', 'A', 'V', 'E' });
                writer.Write(new char[] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * bytesPerSample);
                writer.Write((short)(channels * bytesPerSample));
                writer.Write((short)(bytesPerSample * 8));
                writer.Write(new char[] { 'd', 'a', 't', 'a' });
                writer.Write(dataSize);

                for (int i = 0; i < sampleCount; i++)
                {
                    float sample = Math.Clamp(samples[i], -1.0f, 1.0f);
                    short intSample = (short)(sample * short.MaxValue);
                    writer.Write(intSample);
                }
            }

            return wavBytes;
        }

        private async Task PlayAudioAsync(string filePath)
        {
            _stopRequested = false;

            lock (_playerLock)
            {
                _currentPlayer?.Dispose();
                _currentPlayer = new SoundPlayer(filePath);
                _currentPlayer.Load();
            }

            await Task.Run(() =>
            {
                lock (_playerLock)
                {
                    if (_currentPlayer != null && !_stopRequested)
                    {
                        try
                        {
                            _currentPlayer.PlaySync();
                        }
                        catch
                        {
                        }
                    }
                }
            });
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

            try
            {
                _tts?.StopPlayback();
            }
            catch { }
        }

        public Task StopAsync()
        {
            StopPlayback();
            return Task.CompletedTask;
        }

        private string GetCacheFilePath(string text, string? language, float? speed)
        {
            using var sha1 = SHA1.Create();
            var meta = "kokoro|" + (text ?? string.Empty) + "|" + (language ?? string.Empty) + "|" + (speed?.ToString() ?? string.Empty);
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(meta));
            var sb = new StringBuilder();
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return Path.Combine(AppPaths.GetUserTtsCacheDir(), sb.ToString() + ".wav");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            StopPlayback();

            try
            {
                _tts?.Dispose();
                _tts = null;
            }
            catch { }
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
