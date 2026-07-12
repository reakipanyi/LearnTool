using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Processing;
using LearningAssistant.Common;
using LearningAssistant.Models.Config;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace LearningAssistant.Services.TTS
{
    public class KokoroSharpTtsService : BaseTtsService
    {
        private readonly TtsConfig _config;
        private KokoroTTS? _tts;
        private KokoroVoice? _defaultVoice;
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

        private volatile bool _isInitialized = false;
        private volatile bool _isLoading = false;
        private readonly object _initLock = new object();

        public KokoroSharpTtsService(TtsConfig config, ILogger<KokoroSharpTtsService>? logger = null)
            : base(logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
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
            if (_tts == null || _defaultVoice == null)
            {
                _logger?.LogWarning("KokoroSharp TTS warmup skipped: TTS engine or voice not initialized");
                return;
            }

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
                _tts.EnqueueJob(job);

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
            if (_isLoading) return;
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

        public void ReloadVoiceSettings()
        {
            _logger?.LogInformation("Reloading voice settings");
            lock (_initLock)
            {
                _isInitialized = false;
                _defaultVoice = null;
            }
            StartBackgroundInitialization();
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

        public override bool Available
        {
            get
            {
                return _isInitialized && _tts != null && _defaultVoice != null;
            }
        }

        public override async Task<string?> SpeakAsync(string text, string? language = null, float? speed = null, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("=== KOKORO TTS SPEAKASYNC START ===");
            _logger?.LogInformation("KokoroTts SpeakAsync: input text='{Text}', language='{Language}', speed={Speed}, textLength={TextLength}",
                text, language, speed, text?.Length ?? 0);

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

            _logger?.LogInformation("SpeakAsync: TTS initialized, voice={Voice}", _defaultVoice?.Name ?? "unknown");
            _logger?.LogInformation("SpeakAsync: start, text length={Len}, language={Language}, speed={Speed}", text.Length, language, speed);

            StopPlayback();
            _stopRequested = false;

            try
            {
                Directory.CreateDirectory(AppPaths.GetTtsCacheDir());

                string paddedText = PadShortText(text);
                float actualSpeed = speed ?? _config.Speed;

                var langSegments = SplitTextByLanguage(paddedText);
                bool isMultiLanguage = langSegments.Count > 1;

                if (isMultiLanguage)
                {
                    _logger?.LogInformation("SpeakAsync: detected {SegmentCount} language segments, skipping cache", langSegments.Count);
                    var resultBytes = await SynthesizeAndPlayStreamAsync(paddedText, language, actualSpeed, cancellationToken).ConfigureAwait(false);
                    return resultBytes != null && resultBytes.Length > 0 ? null : null;
                }

                var actualVoice = SelectVoiceForSegment(language ?? "en", language) ?? _defaultVoice;
                string path = GetCacheFilePath(paddedText, language, actualSpeed, actualVoice?.Name);

                if (File.Exists(path))
                {
                    File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
                    _logger?.LogInformation("SpeakAsync: using cached audio, path={Path}", path);
                    await PlayAudioAsync(path, _config.Volume, actualSpeed, cancellationToken);
                    _logger?.LogInformation("SpeakAsync: cached audio playback completed");
                    return path;
                }

                cancellationToken.ThrowIfCancellationRequested();

                _logger?.LogInformation("SpeakAsync: synthesizing new audio, text length={Len}, padded={Padded}", text.Length, paddedText);
                var sw = System.Diagnostics.Stopwatch.StartNew();

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

        public override async Task<string?> SpeakToCacheAsync(string text, string? language = null, float? speed = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger?.LogWarning("SpeakToCacheAsync: text is empty");
                return null;
            }

            EnsureInitialized();
            if (!Available)
            {
                _logger?.LogWarning("SpeakToCacheAsync: not available");
                return null;
            }

            StopPlayback();
            _stopRequested = false;

            try
            {
                Directory.CreateDirectory(AppPaths.GetTtsCacheDir());

                var actualVoice = SelectVoiceForSegment(language ?? "en", language) ?? _defaultVoice;
                string paddedText = PadShortText(text);
                float actualSpeed = speed ?? _config.Speed;
                string path = GetCacheFilePath(paddedText, language, actualSpeed, actualVoice?.Name);

                if (File.Exists(path))
                {
                    _logger?.LogDebug("SpeakToCacheAsync: already cached, path={Path}", path);
                    return path;
                }

                var wavBytes = await SynthesizeToWavAsync(paddedText, language, actualSpeed, cancellationToken).ConfigureAwait(false);

                if (wavBytes == null || wavBytes.Length == 0)
                {
                    _logger?.LogWarning("SpeakToCacheAsync: synthesis returned empty result");
                    return null;
                }

                await File.WriteAllBytesAsync(path, wavBytes, cancellationToken).ConfigureAwait(false);
                _logger?.LogDebug("SpeakToCacheAsync: audio cached, path={Path}", path);

                CleanupOldCache();
                return path;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "KokoroSharp TTS cache failed for text: {Text}", text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                return null;
            }
        }

        public override async Task<byte[]?> SpeakStreamAsync(string text, string? language = null, float? speed = null, string? format = null)
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
            _logger?.LogInformation("=== KOKORO TTS SYNTHESIZE START ===");
            _logger?.LogInformation("SynthesizeAndPlayStreamAsync: text='{Text}', language='{Language}', speed={Speed}", text, language, speed);

            if (_tts == null || _defaultVoice == null)
            {
                _logger?.LogError("SynthesizeAndPlayStreamAsync: TTS engine or default voice is null");
                return null;
            }

            float safeSpeed = ClampSpeed(speed);
            string trimmedText = text.Trim();

            _logger?.LogInformation("SynthesizeAndPlayStreamAsync: trimmedText='{TrimmedText}', safeSpeed={SafeSpeed}", trimmedText, safeSpeed);

            var langSegments = SplitTextByLanguage(trimmedText);
            if (langSegments.Count == 0)
            {
                _logger?.LogWarning("SynthesizeAndPlayStreamAsync: No language segments found");
                return null;
            }

            _logger?.LogInformation("SynthesizeAndPlayStreamAsync: {SegmentCount} language segments detected", langSegments.Count);
            for (int i = 0; i < langSegments.Count; i++)
            {
                var seg = langSegments[i];
                _logger?.LogInformation("SynthesizeAndPlayStreamAsync: segment[{Index}]: text='{Text}', langCode='{LangCode}', textLength={TextLength}",
                    i, seg.Text, seg.LangCode, seg.Text.Length);
            }

            if (langSegments.Count == 1)
            {
                var segment = langSegments[0];
                return await ProcessSingleSegmentAsync(segment.Text, segment.LangCode, safeSpeed, language, cancellationToken);
            }

            return await ProcessMultiSegmentsAsync(langSegments, safeSpeed, language, cancellationToken);
        }

        private async Task<byte[]?> SynthesizeToWavCoreAsync(string text, string? language, float speed, CancellationToken cancellationToken = default)
        {
            if (_tts == null || _defaultVoice == null) return null;

            float safeSpeed = ClampSpeed(speed);
            string trimmedText = text.Trim();

            var langSegments = SplitTextByLanguage(trimmedText);
            if (langSegments.Count == 0)
                return null;

            _logger?.LogDebug("SynthesizeToWavCoreAsync: {SegmentCount} language segments detected", langSegments.Count);

            if (langSegments.Count == 1)
            {
                var langCode = langSegments[0].LangCode;
                var voice = SelectVoiceForSegment(langCode, language);
                if (voice == null) return null;
                _logger?.LogDebug("SynthesizeToWavCoreAsync: single segment, lang={LangCode}, voice={Voice}, speed={Speed}", langCode, voice.Name, safeSpeed);
                var tokens = Tokenizer.Tokenize(langSegments[0].Text, MapLangCodeToTokenizerLang(langCode), preprocess: true);
                return await SynthesizeTokensToWavAsync(tokens, voice, safeSpeed, langSegments[0].Text, cancellationToken).ConfigureAwait(false);
            }

            var allSamples = new List<float>();
            foreach (var seg in langSegments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var segVoice = SelectVoiceForSegment(seg.LangCode, language);
                if (segVoice == null)
                {
                    _logger?.LogWarning("SynthesizeToWavCoreAsync: no voice for segment lang {LangCode}, skipping", seg.LangCode);
                    continue;
                }

                _logger?.LogDebug("SynthesizeToWavCoreAsync: processing segment lang={LangCode}, voice={Voice}, textLength={TextLength}, speed={Speed}", seg.LangCode, segVoice.Name, seg.Text.Length, safeSpeed);
                var tokens = Tokenizer.Tokenize(seg.Text, MapLangCodeToTokenizerLang(seg.LangCode), preprocess: true);
                var wavBytes = await SynthesizeTokensToWavAsync(tokens, segVoice, safeSpeed, seg.Text, cancellationToken).ConfigureAwait(false);
                if (wavBytes == null || wavBytes.Length == 0)
                {
                    _logger?.LogWarning("SynthesizeToWavCoreAsync: segment synthesis returned empty result, lang={LangCode}", seg.LangCode);
                    continue;
                }

                var samples = ExtractPcmFloatFromWav(wavBytes);
                allSamples.AddRange(samples);
            }

            if (allSamples.Count == 0)
            {
                _logger?.LogWarning("SynthesizeToWavCoreAsync: all samples empty after multi-language synthesis");
                return null;
            }

            _logger?.LogDebug("SynthesizeToWavCoreAsync: completed, total samples={SampleCount}", allSamples.Count);
            return ConvertPcmFloatToWav(allSamples.ToArray(), 24000, 1);
        }

        private async Task<byte[]?> ProcessSingleSegmentAsync(string text, string langCode, float speed, string? language = null, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("=== PROCESSING SINGLE SEGMENT ===");
            _logger?.LogInformation("ProcessSingleSegmentAsync: text='{Text}', langCode='{LangCode}', speed={Speed}, requestedLang='{RequestedLang}', textLength={TextLength}",
                text, langCode, speed, language, text.Length);

            var voice = SelectVoiceForSegment(langCode, language);

            if (voice == null)
            {
                _logger?.LogError("ProcessSingleSegmentAsync: No voice found for language {LangCode}", langCode);
                return null;
            }

            _logger?.LogInformation("ProcessSingleSegmentAsync: selected voice='{VoiceName}'", voice.Name);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                string tokenizerLang = MapLangCodeToTokenizerLang(langCode);
                _logger?.LogInformation("ProcessSingleSegmentAsync: tokenizerLang='{TokenizerLang}', synthesizing...", tokenizerLang);
                
                var tokens = Tokenizer.Tokenize(text, tokenizerLang, preprocess: true);
                _logger?.LogInformation("ProcessSingleSegmentAsync: tokenized text, tokenCount={TokenCount}", tokens.Length);
                
                var wavBytes = await SynthesizeTokensToWavAsync(tokens, voice, speed, text).ConfigureAwait(false);

                if (wavBytes != null && wavBytes.Length > 0)
                {
                    await PlayAudioFromBytesAsync(wavBytes, speed, cancellationToken).ConfigureAwait(false);
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
            _logger?.LogInformation("=== PROCESSING MULTI SEGMENTS ===");
            _logger?.LogInformation("ProcessMultiSegmentsAsync: totalSegments={TotalSegments}, speed={Speed}, requestedLang='{RequestedLang}'",
                segments.Count, speed, language);

            var allSamples = new List<float>();

            for (int i = 0; i < segments.Count; i++)
            {
                if (_stopRequested || cancellationToken.IsCancellationRequested)
                {
                    _logger?.LogInformation("ProcessMultiSegmentsAsync: Stop requested, breaking at segment {Index}", i);
                    break;
                }

                var segment = segments[i];
                _logger?.LogInformation("ProcessMultiSegmentsAsync: processing segment[{Index}]: text='{Text}', langCode='{LangCode}', textLength={TextLength}",
                    i, segment.Text, segment.LangCode, segment.Text.Length);

                var voice = SelectVoiceForSegment(segment.LangCode, language);

                if (voice == null)
                {
                    _logger?.LogWarning("ProcessMultiSegmentsAsync: No voice for segment {Index}, lang {LangCode}, skipping", i, segment.LangCode);
                    continue;
                }

                _logger?.LogInformation("ProcessMultiSegmentsAsync: segment[{Index}] selected voice='{VoiceName}'", i, voice.Name);

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string tokenizerLang = MapLangCodeToTokenizerLang(segment.LangCode);
                    _logger?.LogInformation("ProcessMultiSegmentsAsync: segment[{Index}] tokenizerLang='{TokenizerLang}', synthesizing...", i, tokenizerLang);

                    var tokens = Tokenizer.Tokenize(segment.Text, tokenizerLang, preprocess: true);
                    _logger?.LogInformation("ProcessMultiSegmentsAsync: segment[{Index}] tokenized, tokenCount={TokenCount}", i, tokens.Length);

                    var wavBytes = await SynthesizeTokensToWavAsync(tokens, voice, speed, segment.Text).ConfigureAwait(false);

                    if (wavBytes != null && wavBytes.Length > 0)
                    {
                        var samples = ExtractPcmFloatFromWav(wavBytes);
                        allSamples.AddRange(samples);

                        await PlayAudioFromBytesAsync(wavBytes, speed, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger?.LogDebug("ProcessMultiSegmentsAsync: cancelled at segment {Index}", i);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "ProcessMultiSegmentsAsync: Segment {Index} synthesis failed, text='{Text}'", i, segment.Text.Length > 50 ? segment.Text.Substring(0, 50) + "..." : segment.Text);
                }
            }

            if (allSamples.Count == 0)
            {
                _logger?.LogWarning("ProcessMultiSegmentsAsync: All samples are empty after multi-language synthesis");
                return null;
            }

            return ConvertPcmFloatToWav(allSamples.ToArray(), 24000, 1);
        }

        private async Task PlayAudioFromBytesAsync(byte[] wavBytes, float speed, CancellationToken cancellationToken = default)
        {
            if (wavBytes == null || wavBytes.Length == 0 || _stopRequested || cancellationToken.IsCancellationRequested) return;

            string tempFile = Path.Combine(AppPaths.GetTtsCacheDir(), $"_temp_{Guid.NewGuid():N}.wav");
            try
            {
                await File.WriteAllBytesAsync(tempFile, wavBytes, cancellationToken).ConfigureAwait(false);
                await PlayAudioAsync(tempFile, _config.Volume, speed, cancellationToken).ConfigureAwait(false);
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

        private async Task<byte[]?> SynthesizeToWavAsync(string text, string? language, float speed, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("SynthesizeToWavAsync: inputSpeed={InputSpeed}, configSpeed={ConfigSpeed}",
                speed, _config.Speed);

            cancellationToken.ThrowIfCancellationRequested();
            return await SynthesizeToWavCoreAsync(text, language, speed, cancellationToken).ConfigureAwait(false);
        }

        private async Task<byte[]?> SynthesizeTokensToWavAsync(int[] tokens, KokoroVoice voice, float speed, string? text = null, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);

            using var cts = new CancellationTokenSource(SynthesizeTimeoutMs);
            cts.Token.Register(() => tcs.TrySetCanceled());

            var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken).Token;
            linkedToken.Register(() => tcs.TrySetCanceled());

            try
            {
                var job = KokoroJob.Create(tokens, voice, 1.0f, OnComplete: (float[] samples) =>
                {
                    try
                    {
                        _logger?.LogDebug("SynthesizeTokensToWavAsync: SamplesCount={Count}", samples.Length);
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

            string lastChar = trimmed.Length > 0 ? trimmed.Substring(trimmed.Length - 1) : "";
            bool hasEndPunctuation = lastChar == "." || lastChar == "!" || lastChar == "?" ||
                                     lastChar == "。" || lastChar == "！" || lastChar == "？";

            if (!hasEndPunctuation)
            {
                if (IsChineseText(trimmed))
                {
                    _logger?.LogDebug("PadShortText: padding Chinese short text '{Text}' with period", text);
                    return trimmed + "。";
                }
                _logger?.LogDebug("PadShortText: padding short text '{Text}' with period", text);
                return trimmed + ".";
            }

            return text;
        }

        private bool IsChineseText(string text)
        {
            foreach (char c in text)
            {
                if ((c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3400 && c <= 0x4DBF))
                    return true;
            }
            return false;
        }

        private List<(string Text, string LangCode)> SplitTextByLanguage(string text)
        {
            _logger?.LogInformation("SplitTextByLanguage: input text='{Text}', textLength={TextLength}", text, text?.Length ?? 0);

            var result = new List<(string Text, string LangCode)>();
            if (string.IsNullOrWhiteSpace(text))
                return result;

            var currentSegment = new System.Text.StringBuilder();
            string currentLang = "en";
            int charIndex = 0;

            foreach (char c in text)
            {
                string charLang = DetectLanguageFromChar(c);

                _logger?.LogDebug("SplitTextByLanguage: char[{Index}]='{Char}', charCode=0x{CharCode:X4}, detectedLang='{CharLang}'",
                    charIndex, c, (int)c, charLang);

                if (charLang != currentLang && currentSegment.Length > 0)
                {
                    _logger?.LogInformation("SplitTextByLanguage: language switch from '{OldLang}' to '{NewLang}', segment='{Segment}'",
                        currentLang, charLang, currentSegment.ToString().Trim());

                    result.Add((Text: currentSegment.ToString().Trim(), LangCode: currentLang));
                    currentSegment.Clear();
                    currentLang = charLang;
                }

                currentSegment.Append(c);
                charIndex++;
            }

            if (currentSegment.Length > 0)
            {
                _logger?.LogInformation("SplitTextByLanguage: final segment, lang='{Lang}', text='{Text}'", currentLang, currentSegment.ToString().Trim());
                result.Add((Text: currentSegment.ToString().Trim(), LangCode: currentLang));
            }

            var merged = new List<(string Text, string LangCode)>();
            foreach (var seg in result)
            {
                if (string.IsNullOrWhiteSpace(seg.Text) || IsPurePunctuation(seg.Text))
                {
                    _logger?.LogDebug("SplitTextByLanguage: skipping empty/punctuation segment '{Text}', lang='{Lang}'", seg.Text, seg.LangCode);
                    continue;
                }
                merged.Add(seg);
            }

            _logger?.LogInformation("SplitTextByLanguage: returning {Count} segments", merged.Count);
            return merged;
        }

        private static bool IsPurePunctuation(string text)
        {
            foreach (char c in text)
            {
                if (!char.IsPunctuation(c) && !char.IsWhiteSpace(c))
                    return false;
            }
            return true;
        }

        private string DetectLanguageFromChar(char c)
        {
            string result;
            if ((c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3400 && c <= 0x4DBF) || (c >= 0x3000 && c <= 0x303F))
                result = "zh";
            else
                result = "en";

            _logger?.LogDebug("DetectLanguageFromChar: char='{Char}', code=0x{Code:X4}, range=[0x4E00-0x9FFF]={InChineseRange}, detected={Result}",
                c, (int)c, (c >= 0x4E00 && c <= 0x9FFF), result);

            return result;
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

        private readonly Dictionary<string, string[]> _chineseVoices = new Dictionary<string, string[]>
        {
            { "female", new[] { "zf_xiaoxiao", "zf_xiaobei", "zf_xiaoni", "zf_tingting" } },
            { "male", new[] { "zm_yunjian" } }
        };

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
                    voiceName = SelectBestChineseVoice();
                    break;
                case "en":
                case "en-us":
                case "americanenglish":
                    voiceName = string.IsNullOrWhiteSpace(_config.Voice) ? "af_heart" : _config.Voice;
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

        private string SelectBestChineseVoice()
        {
            if (!string.IsNullOrWhiteSpace(_config.Voice))
            {
                if (_config.Voice.StartsWith("zf_") || _config.Voice.StartsWith("zm_"))
                {
                    return _config.Voice;
                }
            }

            foreach (var voice in _chineseVoices["female"])
            {
                try
                {
                    if (KokoroVoiceManager.GetVoice(voice) != null)
                    {
                        _logger?.LogDebug("SelectBestChineseVoice: using voice '{Voice}'", voice);
                        return voice;
                    }
                }
                catch { }
            }

            foreach (var voice in _chineseVoices["male"])
            {
                try
                {
                    if (KokoroVoiceManager.GetVoice(voice) != null)
                    {
                        _logger?.LogDebug("SelectBestChineseVoice: falling back to male voice '{Voice}'", voice);
                        return voice;
                    }
                }
                catch { }
            }

            return "zf_xiaoxiao";
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

            var voice = GetVoiceForLanguage(targetLang);
            if (voice != null)
            {
                _logger?.LogDebug("SelectVoiceForSegment: using voice for language '{TargetLang}', langCode={LangCode}, language={Language}, voice={Voice}", targetLang, langCode, language, voice.Name);
                return voice;
            }

            _logger?.LogWarning("SelectVoiceForSegment: No voice available for language '{TargetLang}', falling back to default, langCode={LangCode}, language={Language}", targetLang, langCode, language);
            return _defaultVoice;
        }

        private static byte[] ConvertPcmFloatToWav(float[] samples, int sampleRate, int channels)
        {
            using var ms = new MemoryStream();
            var waveFormat = new WaveFormat(sampleRate, 16, channels);
            using (var writer = new WaveFileWriter(ms, waveFormat))
            {
                writer.WriteSamples(samples, 0, samples.Length);
            }
            return ms.ToArray();
        }

        public override void Dispose()
        {
            base.Dispose();

            try
            {
                _tts?.Dispose();
                _tts = null;
            }
            catch { }
        }
    }
}