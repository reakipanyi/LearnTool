using LearningAssistant.Common;
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
    public interface IWordPracticeService
    {
        bool IsPronunciationScoringAvailable { get; }
        string? PronunciationUnavailableReason { get; }

        Task PlayModelAsync(LearningItem item, CancellationToken cancellationToken = default);
        bool StartShadowing(LearningItem item);
        Task<PracticeRecord?> StopShadowingAsync(LearningItem item, int? selfScore = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 单词跟读（P2）：复用 TTS、全局录音服务与发音参考评分器。
    /// </summary>
    public sealed class WordPracticeService : IWordPracticeService
    {
        private readonly ITTSService _ttsService;
        private readonly IAudioRecorderService _recorderService;
        private readonly IPronunciationScorer? _scorer;
        private readonly IPracticeRecordService _recordService;
        private readonly IUserSessionService _sessionService;
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
            ILogger<WordPracticeService>? logger = null)
        {
            _ttsService = ttsService;
            _recorderService = recorderService;
            _recordService = recordService;
            _sessionService = sessionService;
            _scorer = scorer;
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
    }
}
