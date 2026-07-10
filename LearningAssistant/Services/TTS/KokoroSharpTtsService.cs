using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Processing;
using LearningAssistant.Common;
using LearningAssistant.Models.Config;
using Microsoft.Extensions.Logging;
using System.Media;
using System.Security.Cryptography;
using System.Text;

namespace LearningAssistant.Services.TTS
{
    public class KokoroSharpTtsService : ITTSService, IDisposable
    {
        private readonly ILogger<KokoroSharpTtsService>? _logger;
        private readonly TtsConfig _config;
        private KokoroTTS? _tts;
        private KokoroVoice? _defaultVoice;
        private const long MaxCacheSizeBytes = 100 * 1024 * 1024;
        private const int SynthesizeTimeoutMs = 300000;

        private float ClampSpeed(float speed)
        {
            if (speed <= 0 || float.IsNaN(speed) || float.IsInfinity(speed))
            {
                _logger?.LogWarning("Invalid speed value: {Speed}, using default 1.0", speed);
                return 1.0f;
            }
            return Math.Clamp(speed, 0.5f, 2.0f);
        }

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

                    if (_defaultVoice == null)
                    {
                        _logger?.LogError("KokoroSharp TTS initialization failed: no voice available");
                        _tts = null;
                        _isInitialized = false;
                        return;
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
                int[] tokens = Tokenizer.Tokenize("Hello.", "en", preprocess: true);

                var tcs = new TaskCompletionSource<bool>();
                var job = KokoroJob.Create(tokens, _defaultVoice, 1.0f, OnComplete: (_) =>
                {
                    tcs.TrySetResult(true);
                });
                _tts?.EnqueueJob(job);

                using var cts = new CancellationTokenSource(5000);
                await tcs.Task.WaitAsync(cts.Token);

                sw.Stop();
                _logger?.LogInformation("KokoroSharp TTS warmup completed in {Elapsed}ms", sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                _logger?.LogWarning("KokoroSharp TTS warmup timed out");
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

        public async Task<string?> SpeakAsync(string text, string? language = null, float? speed = null, CancellationToken cancellationToken = default)
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

            _logger?.LogInformation("SpeakAsync: start, text length={Len}, language={Language}, speed={Speed}", text.Length, language, speed);

            StopPlayback();
            _stopRequested = false;

            try
            {
                Directory.CreateDirectory(AppPaths.GetUserTtsCacheDir());

                var actualVoice = SelectVoiceForSegment(language ?? "en", language) ?? _defaultVoice;
                string paddedText = PadShortText(text);
                string path = GetCacheFilePath(paddedText, language, speed, actualVoice?.Name);

                if (File.Exists(path))
                {
                    File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
                    _logger?.LogInformation("SpeakAsync: using cached audio, path={Path}", path);
                    await PlayAudioAsync(path, cancellationToken);
                    _logger?.LogInformation("SpeakAsync: cached audio playback completed");
                    return path;
                }

                cancellationToken.ThrowIfCancellationRequested();

                _logger?.LogInformation("SpeakAsync: synthesizing new audio, text length={Len}, padded={Padded}", text.Length, paddedText);
                var sw = System.Diagnostics.Stopwatch.StartNew();

                float actualSpeed = speed ?? _config.Speed;
                var wavBytes = await SynthesizeAndPlayStreamAsync(paddedText, language, actualSpeed, cancellationToken).ConfigureAwait(false);

                sw.Stop();
                _logger?.LogInformation("SpeakAsync: total synthesis time {Elapsed}ms, wavBytes={WavBytesLength}", sw.ElapsedMilliseconds, wavBytes?.Length ?? 0);

                if (wavBytes == null || wavBytes.Length == 0)
                {
                    _logger?.LogWarning("SpeakAsync: synthesis returned empty result");
                    return null;
                }

                await File.WriteAllBytesAsync(path, wavBytes, cancellationToken).ConfigureAwait(false);
                _logger?.LogInformation("SpeakAsync: audio saved to cache, path={Path}", path);

                CleanupOldCache();

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
                _logger?.LogError(ex, "KokoroSharp TTS speak failed for text: {Text}", text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                StopPlayback();
                throw;
            }
        }

        public async Task<byte[]?> SpeakStreamAsync(string text, string? language = null, float? speed = null, string? format = null)
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

        private async Task<byte[]?> SynthesizeAndPlayStreamAsync(string text, string? language, float speed, CancellationToken cancellationToken = default)
        {
            if (_tts == null || _defaultVoice == null)
            {
                _logger?.LogError("SynthesizeAndPlayStreamAsync: TTS engine or default voice is null");
                return null;
            }

            float safeSpeed = ClampSpeed(speed);
            string trimmedText = text.Trim();

            var langSegments = SplitTextByLanguage(trimmedText);
            if (langSegments.Count == 0)
            {
                _logger?.LogWarning("SynthesizeAndPlayStreamAsync: No language segments found");
                return null;
            }

            _logger?.LogInformation("SynthesizeAndPlayStreamAsync: {SegmentCount} language segments", langSegments.Count);

            if (langSegments.Count == 1)
            {
                var segment = langSegments[0];
                return await ProcessSingleSegmentAsync(segment.Text, segment.LangCode, safeSpeed, language, cancellationToken);
            }

            return await ProcessMultiSegmentsAsync(langSegments, safeSpeed, language, cancellationToken);
        }

        private async Task<byte[]?> SynthesizeToWavCoreAsync(string text, string? language, float speed)
        {
            if (_tts == null || _defaultVoice == null) return null;

            float safeSpeed = ClampSpeed(speed);
            string trimmedText = text.Trim();

            var langSegments = SplitTextByLanguage(trimmedText);
            if (langSegments.Count == 0)
                return null;

            if (langSegments.Count == 1)
                {
                    var langCode = langSegments[0].LangCode;
                    var voice = SelectVoiceForSegment(langCode, language);
                    if (voice == null) return null;
                    var tokens = Tokenizer.Tokenize(langSegments[0].Text, MapLangCodeToTokenizerLang(langCode), preprocess: true);
                    return await SynthesizeTokensToWavAsync(tokens, voice, safeSpeed, langSegments[0].Text).ConfigureAwait(false);
                }

                var allSamples = new List<float>();
                foreach (var seg in langSegments)
                {
                    var segVoice = SelectVoiceForSegment(seg.LangCode, language);
                    if (segVoice == null) continue;
                    
                    var tokens = Tokenizer.Tokenize(seg.Text, MapLangCodeToTokenizerLang(seg.LangCode), preprocess: true);
                    var wavBytes = await SynthesizeTokensToWavAsync(tokens, segVoice, safeSpeed, seg.Text).ConfigureAwait(false);
                if (wavBytes == null || wavBytes.Length == 0)
                    continue;

                var samples = ExtractPcmFloatFromWav(wavBytes);
                allSamples.AddRange(samples);
            }

            if (allSamples.Count == 0)
                return null;

            return ConvertPcmFloatToWav(allSamples.ToArray(), 24000, 1);
        }

        private async Task<byte[]?> ProcessSingleSegmentAsync(string text, string langCode, float speed, string? language = null, CancellationToken cancellationToken = default)
        {
            var voice = SelectVoiceForSegment(langCode, language);

            if (voice == null)
            {
                _logger?.LogError("ProcessSingleSegmentAsync: No voice found for language {LangCode}", langCode);
                return null;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                string tokenizerLang = MapLangCodeToTokenizerLang(langCode);
                _logger?.LogDebug("ProcessSingleSegmentAsync: langCode={LangCode}, tokenizerLang={TokenizerLang}, voice={Voice}, requestedLang={RequestedLang}", langCode, tokenizerLang, voice.Name, language);
                var tokens = Tokenizer.Tokenize(text, tokenizerLang, preprocess: true);
                var wavBytes = await SynthesizeTokensToWavAsync(tokens, voice, speed, text).ConfigureAwait(false);

                if (wavBytes != null && wavBytes.Length > 0)
                {
                    await PlayAudioFromBytesAsync(wavBytes, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _logger?.LogWarning("ProcessSingleSegmentAsync: Synthesis returned empty result for lang {LangCode}", langCode);
                }

                return wavBytes;
            }
            catch (OperationCanceledException)
            {
                _logger?.LogDebug("ProcessSingleSegmentAsync: cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ProcessSingleSegmentAsync: Synthesis failed for lang {LangCode}", langCode);
                return null;
            }
        }

        private async Task<byte[]?> ProcessMultiSegmentsAsync(List<(string Text, string LangCode)> segments, float speed, string? language = null, CancellationToken cancellationToken = default)
        {
            var allSamples = new List<float>();

            for (int i = 0; i < segments.Count; i++)
            {
                if (_stopRequested || cancellationToken.IsCancellationRequested)
                {
                    _logger?.LogInformation("ProcessMultiSegmentsAsync: Stop requested, breaking at segment {Index}", i);
                    break;
                }

                var segment = segments[i];
                var voice = SelectVoiceForSegment(segment.LangCode, language);

                if (voice == null)
                {
                    _logger?.LogWarning("ProcessMultiSegmentsAsync: No voice for segment {Index}, lang {LangCode}, skipping", i, segment.LangCode);
                    continue;
                }

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string tokenizerLang = MapLangCodeToTokenizerLang(segment.LangCode);
                    _logger?.LogDebug("ProcessMultiSegmentsAsync: segment {Index}, langCode={LangCode}, tokenizerLang={TokenizerLang}, voice={Voice}, requestedLang={RequestedLang}", i, segment.LangCode, tokenizerLang, voice.Name, language);
                    var tokens = Tokenizer.Tokenize(segment.Text, tokenizerLang, preprocess: true);
                    var wavBytes = await SynthesizeTokensToWavAsync(tokens, voice, speed, segment.Text).ConfigureAwait(false);

                    if (wavBytes != null && wavBytes.Length > 0)
                    {
                        var samples = ExtractPcmFloatFromWav(wavBytes);
                        allSamples.AddRange(samples);

                        await PlayAudioFromBytesAsync(wavBytes, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger?.LogDebug("ProcessMultiSegmentsAsync: cancelled at segment {Index}", i);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "ProcessMultiSegmentsAsync: Segment {Index} synthesis failed", i);
                }
            }

            if (allSamples.Count == 0)
            {
                _logger?.LogWarning("ProcessMultiSegmentsAsync: All samples are empty after multi-language synthesis");
                return null;
            }

            return ConvertPcmFloatToWav(allSamples.ToArray(), 24000, 1);
        }

        private async Task PlayAudioFromBytesAsync(byte[] wavBytes, CancellationToken cancellationToken = default)
        {
            if (wavBytes == null || wavBytes.Length == 0 || _stopRequested || cancellationToken.IsCancellationRequested) return;

            string tempFile = Path.Combine(AppPaths.GetUserTtsCacheDir(), $"_temp_{Guid.NewGuid():N}.wav");
            try
            {
                await File.WriteAllBytesAsync(tempFile, wavBytes, cancellationToken).ConfigureAwait(false);
                await PlayAudioAsync(tempFile, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                try 
                { 
                    if (File.Exists(tempFile))
                        File.Delete(tempFile); 
                } 
                catch { }
            }
        }

        private async Task<byte[]?> SynthesizeToWavAsync(string text, string? language, float speed)
        {
            _logger?.LogInformation("SynthesizeToWavAsync: inputSpeed={InputSpeed}, configSpeed={ConfigSpeed}",
                speed, _config.Speed);

            return await SynthesizeToWavCoreAsync(text, language, speed).ConfigureAwait(false);
        }

        private async Task<byte[]?> SynthesizeTokensToWavAsync(int[] tokens, KokoroVoice voice, float speed, string? text = null)
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

        private bool IsShortText(string text)
        {
            string trimmed = text.Trim();
            return trimmed.Length <= 6 || trimmed.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length <= 2;
        }

        private string PadShortText(string text)
        {
            string trimmed = text.Trim();
            if (!IsShortText(trimmed))
                return text;

            if (!trimmed.EndsWith(".") && !trimmed.EndsWith("!") && !trimmed.EndsWith("?"))
            {
                _logger?.LogDebug("PadShortText: padding short text '{Text}' with period", text);
                return trimmed + ".";
            }

            return text;
        }

        private float[] RemoveAudioRepetitions(float[] samples, int sampleRate)
        {
            if (samples.Length < sampleRate / 2)
                return samples;

            float[] normalized = NormalizeSamples(samples);
            
            int minPatternLength = sampleRate / 10;
            int maxPatternLength = sampleRate * 2;
            int searchWindow = sampleRate * 3;

            List<int> cuts = new List<int>();
            int i = 0;

            while (i < normalized.Length - minPatternLength)
            {
                bool foundRepeat = false;

                for (int patternLen = Math.Min(maxPatternLength, (normalized.Length - i) / 2); patternLen >= minPatternLength; patternLen--)
                {
                    int nextStart = i + patternLen;
                    if (nextStart + patternLen > normalized.Length)
                        continue;

                    float similarity = CalculatePatternSimilarity(normalized, i, nextStart, patternLen);

                    if (similarity > 0.85)
                    {
                        cuts.Add(nextStart);
                        i = nextStart + patternLen;
                        foundRepeat = true;
                        break;
                    }
                }

                if (!foundRepeat)
                    i += minPatternLength / 4;
            }

            if (cuts.Count == 0)
                return samples;

            cuts.Sort();
            cuts = cuts.Distinct().ToList();

            List<float> result = new List<float>();
            int prev = 0;

            foreach (int cut in cuts)
            {
                if (cut > prev)
                    result.AddRange(samples.Skip(prev).Take(cut - prev));
                prev = cut;
            }

            if (prev < samples.Length)
                result.AddRange(samples.Skip(prev));

            float removalRatio = 1.0f - (float)result.Count / samples.Length;
            if (removalRatio > 0.5f)
            {
                _logger?.LogInformation("RemoveAudioRepetitions: removal ratio {Ratio:P0} exceeds 50% threshold, returning original audio", removalRatio);
                return samples;
            }

            _logger?.LogInformation("RemoveAudioRepetitions: removed {CutCount} repetitions, original length={Original}, result length={Result}, removal ratio={Ratio:P0}", cuts.Count, samples.Length, result.Count, removalRatio);
            return result.ToArray();
        }

        private float[] NormalizeSamples(float[] samples)
        {
            float maxAbs = samples.Max(s => Math.Abs(s));
            if (maxAbs < float.Epsilon)
                return samples;

            float[] normalized = new float[samples.Length];
            for (int i = 0; i < samples.Length; i++)
                normalized[i] = samples[i] / maxAbs;

            return normalized;
        }

        private float CalculatePatternSimilarity(float[] samples, int start1, int start2, int length)
        {
            float sumDiff = 0f;
            float sumAbs1 = 0f;
            float sumAbs2 = 0f;

            for (int i = 0; i < length; i++)
            {
                float diff = samples[start1 + i] - samples[start2 + i];
                sumDiff += diff * diff;
                sumAbs1 += Math.Abs(samples[start1 + i]);
                sumAbs2 += Math.Abs(samples[start2 + i]);
            }

            float rmsDiff = (float)Math.Sqrt(sumDiff / length);
            float avgEnergy = (sumAbs1 + sumAbs2) / (length * 2);

            if (avgEnergy < 0.01f)
                return 0f;

            float similarity = 1.0f - Math.Min(rmsDiff / avgEnergy, 1.0f);
            return similarity;
        }

        private List<(string Text, string LangCode)> SplitTextByLanguage(string text)
        {
            var result = new List<(string Text, string LangCode)>();
            if (string.IsNullOrWhiteSpace(text))
                return result;

            var currentSegment = new StringBuilder();
            string currentLang = "en";

            foreach (char c in text)
            {
                string charLang = "en";
                if (c >= 0x4E00 && c <= 0x9FFF)
                    charLang = "zh";
                else
                    charLang = "en";

                if (charLang != currentLang && currentSegment.Length > 0)
                {
                    result.Add((Text: currentSegment.ToString().Trim(), LangCode: currentLang));
                    currentSegment.Clear();
                    currentLang = charLang;
                }

                currentSegment.Append(c);
            }

            if (currentSegment.Length > 0)
            {
                result.Add((Text: currentSegment.ToString().Trim(), LangCode: currentLang));
            }

            var merged = new List<(string Text, string LangCode)>();
            foreach (var seg in result)
            {
                if (string.IsNullOrWhiteSpace(seg.Text))
                    continue;
                merged.Add(seg);
            }

            return merged;
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
                if (voice != null)
                {
                    _logger?.LogDebug("GetVoiceForLanguage: found voice '{VoiceName}' for language '{Language}'", voiceName, language);
                    return voice;
                }
                _logger?.LogWarning("GetVoiceForLanguage: voice '{VoiceName}' not found for language '{Language}', falling back to default", voiceName, language);
                return _defaultVoice;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "GetVoiceForLanguage: failed to get voice '{VoiceName}' for language '{Language}', falling back to default", voiceName, language);
                return _defaultVoice;
            }
        }

        private static string MapLangCodeToTokenizerLang(string langCode)
        {
            if (langCode == "zh")
                return "zh";
            return "en";
        }

        private KokoroVoice? SelectVoiceForSegment(string langCode, string? language)
        {
            string targetLang = !string.IsNullOrWhiteSpace(language) ? language.ToLowerInvariant() : langCode;
            
            if (targetLang.Contains("zh") || targetLang.Contains("cn"))
            {
                var voice = GetVoiceForLanguage("zh");
                if (voice != null)
                {
                    _logger?.LogDebug("SelectVoiceForSegment: using Chinese voice, langCode={LangCode}, language={Language}, voice={Voice}", langCode, language, voice.Name);
                    return voice;
                }
                _logger?.LogWarning("SelectVoiceForSegment: Chinese voice not available, falling back to default, langCode={LangCode}, language={Language}", langCode, language);
            }
            
            var englishVoice = GetVoiceForLanguage("en");
            if (englishVoice != null)
            {
                _logger?.LogDebug("SelectVoiceForSegment: using English voice, langCode={LangCode}, language={Language}, voice={Voice}", langCode, language, englishVoice.Name);
                return englishVoice;
            }
            _logger?.LogWarning("SelectVoiceForSegment: English voice not available, falling back to default, langCode={LangCode}, language={Language}", langCode, language);
            return _defaultVoice;
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

        private async Task PlayAudioAsync(string filePath, CancellationToken cancellationToken = default)
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

        private string GetCacheFilePath(string text, string? language, float? speed, string? voiceName = null)
        {
            using var sha1 = SHA1.Create();
            var actualVoiceName = voiceName ?? _defaultVoice?.Name ?? string.Empty;
            var meta = "kokoro_v2|" + (text ?? string.Empty) + "|" + (language ?? string.Empty) + "|" + (speed?.ToString() ?? string.Empty) + "|" + actualVoiceName;
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
