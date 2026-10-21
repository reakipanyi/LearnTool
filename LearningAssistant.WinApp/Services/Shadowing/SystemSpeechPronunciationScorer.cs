using System.Speech.Recognition;
using NAudio.Wave;

namespace LearningAssistant.Services.Shadowing
{
    /// <summary>
    /// 基于 Windows System.Speech 的英语参考评分。
    /// 未安装 en-US 识别器时 IsAvailable 为 false，界面应隐藏评分入口（中文不支持）。
    /// </summary>
    public sealed class SystemSpeechPronunciationScorer : IPronunciationScorer, IDisposable
    {
        private const double MinValidDurationSeconds = 0.8;

        private readonly RecognizerInfo? _recognizer;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private SpeechRecognitionEngine? _engine;
        private bool _disposed;

        public bool IsAvailable => _recognizer != null;

        public string? UnavailableReason { get; }

        public SystemSpeechPronunciationScorer()
        {
            var recognizers = SpeechRecognitionEngine.InstalledRecognizers();

            _recognizer = recognizers.FirstOrDefault(r =>
                             r.Culture.Name.Equals("en-US", StringComparison.OrdinalIgnoreCase))
                         ?? recognizers.FirstOrDefault(r =>
                             r.Culture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase));

            if (_recognizer == null)
                UnavailableReason = "未安装英语语音识别器（en-US），参考评分不可用。";
        }

        public async Task<PronunciationScoreResult?> ScoreAsync(
            string expectedText,
            string wavFilePath,
            CancellationToken cancellationToken = default)
        {
            if (!IsAvailable || string.IsNullOrWhiteSpace(expectedText))
                return null;
            if (string.IsNullOrWhiteSpace(wavFilePath) || !File.Exists(wavFilePath))
                return null;

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!HasEnoughDuration(wavFilePath))
                    return null;

                return await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _engine ??= CreateEngine();
                    _engine.SetInputToWaveFile(wavFilePath);
                    RecognitionResult? result = _engine.Recognize();

                    return TextSimilarityScorer.Score(expectedText, result?.Text ?? string.Empty);
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning(
                    $"[PronunciationScorer] 评分失败: {ex.Message}");
                return null;
            }
            finally
            {
                _gate.Release();
            }
        }

        private SpeechRecognitionEngine CreateEngine()
        {
            var engine = new SpeechRecognitionEngine(_recognizer);
            engine.LoadGrammar(new DictationGrammar());
            return engine;
        }

        private static bool HasEnoughDuration(string wavFilePath)
        {
            try
            {
                using var reader = new WaveFileReader(wavFilePath);
                return reader.TotalTime.TotalSeconds >= MinValidDurationSeconds;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning(
                    $"[PronunciationScorer] 读取录音失败: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            _engine?.Dispose();
            _engine = null;
            _gate.Dispose();
        }
    }
}
