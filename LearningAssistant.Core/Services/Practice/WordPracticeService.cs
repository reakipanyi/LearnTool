using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Models.Practice;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Pdf;
using LearningAssistant.Services.Shadowing;
using LearningAssistant.Services.TTS;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Services.Practice
{
    /// <summary>听写判定结果（分数为 0-100 的相似度参考）。</summary>
    public sealed class DictationCheckResult
    {
        public bool IsCorrect { get; init; }
        public int Score { get; init; }
        public string Expected { get; init; } = string.Empty;
        public string NormalizedInput { get; init; } = string.Empty;
    }

    public interface IWordPracticeService
    {
        bool IsPronunciationScoringAvailable { get; }
        string? PronunciationUnavailableReason { get; }

        Task PlayModelAsync(LearningItem item, CancellationToken cancellationToken = default);
        bool StartShadowing(LearningItem item);
        Task<PracticeRecord?> StopShadowingAsync(LearningItem item, int? selfScore = null, CancellationToken cancellationToken = default);
        DictationCheckResult CheckDictation(LearningItem item, string? input);
        Task<PracticeRecord> RecordDictationAsync(LearningItem item, string? input, DictationCheckResult check, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 单词跟读/听写（P2）：复用 TTS、全局录音服务与发音参考评分器。
    /// 跟读只记录练习结果；听写未通过时才发布 ItemWrongEvent 进入错题本。
    /// </summary>
    public sealed class WordPracticeService : IWordPracticeService
    {
        private readonly ITTSService _ttsService;
        private readonly IAudioRecorderService _recorderService;
        private readonly IPronunciationScorer? _scorer;
        private readonly IPracticeRecordService _recordService;
        private readonly IUserSessionService _sessionService;
        private readonly IEventBus? _eventBus;
        private readonly ILogger<WordPracticeService>? _logger;

        private readonly object _recordingLock = new object();
        private TaskCompletionSource<AudioRecording>? _pendingRecording;
        private EventHandler<AudioRecording>? _recordingHandler;
        private DateTime _recordingStartedAt;

        public WordPracticeService(
            ITTSService ttsService,
            IAudioRecorderService recorderService,
            IPracticeRecordService recordService,
            IUserSessionService sessionService,
            IPronunciationScorer? scorer = null,
            IEventBus? eventBus = null,
            ILogger<WordPracticeService>? logger = null)
        {
            _ttsService = ttsService;
            _recorderService = recorderService;
            _recordService = recordService;
            _sessionService = sessionService;
            _scorer = scorer;
            _eventBus = eventBus;
            _logger = logger;
        }

        public bool IsPronunciationScoringAvailable => _scorer?.IsAvailable == true;

        public string? PronunciationUnavailableReason =>
            _scorer == null
                ? "未配置发音识别引擎，按完成度记录参考分"
                : _scorer.UnavailableReason;

        public Task PlayModelAsync(LearningItem item, CancellationToken cancellationToken = default)
        {
            var text = item.GetMainContent();
            if (string.IsNullOrWhiteSpace(text)) return Task.CompletedTask;

            var language = item.Subject == SubjectType.English ? "en" : "zh";
            return _ttsService.SpeakAsync(text, language, cancellationToken: cancellationToken);
        }

        public bool StartShadowing(LearningItem item)
        {
            if (item == null) return false;
            if (_recorderService.State == AudioRecorderState.Recording) return false;

            lock (_recordingLock)
            {
                CleanupRecordingSubscription();

                _pendingRecording = new TaskCompletionSource<AudioRecording>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                _recordingStartedAt = DateTime.Now;

                _recordingHandler = (_, recording) =>
                {
                    lock (_recordingLock)
                    {
                        _pendingRecording?.TrySetResult(recording);
                    }
                };

                _recorderService.RecordingCompleted += _recordingHandler;
            }

            try
            {
                var itemKey = string.IsNullOrWhiteSpace(item.Id)
                    ? LearningItemIdentity.GetCanonicalKey(item)
                    : item.Id;

                _recorderService.StartRecording(
                    itemKey,
                    0,
                    BuildRecordingName(item),
                    AudioRecordingScope.WordShadowing,
                    itemKey);

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "启动单词跟读录音失败");
                lock (_recordingLock)
                {
                    CleanupRecordingSubscription();
                }
                return false;
            }
        }

        public async Task<PracticeRecord?> StopShadowingAsync(
            LearningItem item,
            int? selfScore = null,
            CancellationToken cancellationToken = default)
        {
            if (item == null) return null;

            TaskCompletionSource<AudioRecording>? pending;
            lock (_recordingLock)
            {
                pending = _pendingRecording;
            }

            if (pending == null) return null;

            try
            {
                _recorderService.StopRecording();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "停止单词跟读录音失败");
                lock (_recordingLock)
                {
                    CleanupRecordingSubscription();
                }
                return null;
            }

            AudioRecording recording;
            try
            {
                recording = await pending.Task.WaitAsync(TimeSpan.FromSeconds(8), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "等待单词跟读录音完成超时");
                lock (_recordingLock)
                {
                    CleanupRecordingSubscription();
                }
                return null;
            }
            finally
            {
                lock (_recordingLock)
                {
                    CleanupRecordingSubscription();
                }
            }

            PronunciationScoreResult? pronunciation = null;
            if (IsPronunciationScoringAvailable
                && !string.IsNullOrWhiteSpace(recording.FilePath)
                && File.Exists(recording.FilePath))
            {
                try
                {
                    pronunciation = await _scorer!.ScoreAsync(
                        item.GetMainContent(), recording.FilePath, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "单词跟读评分失败，回退到完成度记录");
                }
            }

            var durationMs = recording.DurationMs > 0
                ? recording.DurationMs
                : (int)(DateTime.Now - _recordingStartedAt).TotalMilliseconds;

            var record = new PracticeRecord
            {
                UserId = ResolveUserId(item),
                LearningItemId = item.Id,
                ItemContent = item.GetMainContent(),
                Type = PracticeType.Shadowing,
                SourceType = LearningItemClassification.GetEffectiveSourceType(item),
                Score = pronunciation?.Score ?? selfScore ?? 100,
                MaxScore = 100,
                DurationMs = Math.Max(0, durationMs),
                RecordingPath = string.IsNullOrWhiteSpace(recording.FilePath) ? null : recording.FilePath,
                MetaJson = JsonSerializer.Serialize(new
                {
                    recognized = pronunciation?.RecognizedText ?? string.Empty,
                    basis = pronunciation?.Basis ?? "completion",
                    selfRated = pronunciation == null && selfScore.HasValue
                })
            };

            await _recordService.SaveAsync(record, cancellationToken);
            return record;
        }

        public DictationCheckResult CheckDictation(LearningItem item, string? input)
        {
            var expected = item.GetMainContent().Trim();
            var normalizedExpected = Normalize(expected);
            var normalizedInput = Normalize(input ?? string.Empty);

            if (normalizedExpected.Length == 0)
            {
                return new DictationCheckResult
                {
                    IsCorrect = true,
                    Score = 100,
                    Expected = expected,
                    NormalizedInput = normalizedInput
                };
            }

            var score = normalizedExpected == normalizedInput
                ? 100
                : (int)Math.Round(Similarity(normalizedExpected, normalizedInput) * 100);

            return new DictationCheckResult
            {
                IsCorrect = score >= 80,
                Score = score,
                Expected = expected,
                NormalizedInput = normalizedInput
            };
        }

        public async Task<PracticeRecord> RecordDictationAsync(
            LearningItem item,
            string? input,
            DictationCheckResult check,
            CancellationToken cancellationToken = default)
        {
            var record = new PracticeRecord
            {
                UserId = ResolveUserId(item),
                LearningItemId = item.Id,
                ItemContent = item.GetMainContent(),
                Type = PracticeType.Dictation,
                SourceType = LearningItemClassification.GetEffectiveSourceType(item),
                Score = check.Score,
                MaxScore = 100,
                MetaJson = JsonSerializer.Serialize(new
                {
                    input = input ?? string.Empty,
                    normalizedInput = check.NormalizedInput,
                    passed = check.IsCorrect
                })
            };

            await _recordService.SaveAsync(record, cancellationToken);

            if (!check.IsCorrect)
            {
                _eventBus?.Publish(new ItemWrongEvent
                {
                    UserId = record.UserId,
                    ItemId = item.Id ?? string.Empty,
                    ItemContent = item.GetMainContent(),
                    CorrectAnswer = item.GetDisplayText(),
                    UserAnswer = input ?? string.Empty,
                    SubCategory = item.SubCategory.ToString(),
                    LearningType = "听写"
                });
            }

            return record;
        }

        private void CleanupRecordingSubscription()
        {
            if (_recordingHandler != null)
            {
                _recorderService.RecordingCompleted -= _recordingHandler;
                _recordingHandler = null;
            }

            _pendingRecording = null;
        }

        private string ResolveUserId(LearningItem item)
        {
            // 学习项本身不带用户 Id，使用当前会话，避免写错用户目录。
            var sessionUserId = _sessionService.CurrentUserId;
            return string.IsNullOrWhiteSpace(sessionUserId)
                ? Constants.DefaultUserId
                : sessionUserId;
        }

        private static string BuildRecordingName(LearningItem item)
        {
            var content = item.GetMainContent();
            if (string.IsNullOrWhiteSpace(content)) return "word";

            var safe = new string(content
                .Where(c => !Path.GetInvalidFileNameChars().Contains(c) && !char.IsWhiteSpace(c))
                .Take(24)
                .ToArray());

            return string.IsNullOrWhiteSpace(safe) ? "word" : safe;
        }

        private static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var builder = new System.Text.StringBuilder(text.Length);
            foreach (var ch in text.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch) || ch > 127)
                    builder.Append(ch);
            }

            return builder.ToString();
        }

        /// <summary>基于编辑距离的相似度（0-1）。</summary>
        private static double Similarity(string expected, string actual)
        {
            if (expected.Length == 0 && actual.Length == 0) return 1.0;
            if (expected.Length == 0 || actual.Length == 0) return 0.0;

            var previous = new int[actual.Length + 1];
            var current = new int[actual.Length + 1];

            for (var j = 0; j <= actual.Length; j++)
                previous[j] = j;

            for (var i = 1; i <= expected.Length; i++)
            {
                current[0] = i;
                for (var j = 1; j <= actual.Length; j++)
                {
                    var cost = expected[i - 1] == actual[j - 1] ? 0 : 1;
                    current[j] = Math.Min(
                        Math.Min(current[j - 1] + 1, previous[j] + 1),
                        previous[j - 1] + cost);
                }

                (previous, current) = (current, previous);
            }

            var distance = previous[actual.Length];
            return 1.0 - (double)distance / Math.Max(expected.Length, actual.Length);
        }
    }
}
