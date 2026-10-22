using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.Learning.Status;
using LearningAssistant.Models.Learning.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace LearningAssistant.Services.Learning
{
    public enum StudyTextKind
    {
        Auto,
        Word,
        Phrase,
        Sentence,
        Paragraph
    }

    /// <summary>统一学习入口请求：PDF 高亮、OCR、翻译、手动输入均走此契约。</summary>
    public sealed record StudyEntryRequest
    {
        public required string Text { get; init; }
        public string? Translation { get; init; }
        public string? Note { get; init; }
        public SubjectType? Subject { get; init; }
        public SubCategoryType? SubCategory { get; init; }
        public StudyTextKind Kind { get; init; } = StudyTextKind.Auto;

        public string SourceType { get; init; } = "Manual";
        public string? PdfPath { get; init; }
        public string? PdfHash { get; init; }
        public int PageIndex { get; init; } = -1;
        public string? HighlightId { get; init; }
        public string? AnnotationId { get; init; }
        public string? SentenceId { get; init; }
    }

    public sealed record StudyEntryResult(
        string ItemId,
        string CanonicalKey,
        bool Created,
        bool AlreadyExists,
        LearningItem? Item);

    public interface IStudyEntryService
    {
        Task<StudyEntryResult> AddAsync(StudyEntryRequest request, CancellationToken ct = default);

        Task<IReadOnlyList<StudyEntryResult>> AddRangeAsync(
            IEnumerable<StudyEntryRequest> requests, CancellationToken ct = default);
    }

    public sealed class StudyEntryService : IStudyEntryService
    {
        private const int MaxTextLength = 2000;

        private readonly IPersonalContentStore _store;
        private readonly IUserSessionService _session;
        private readonly ILogger<StudyEntryService>? _logger;

        public StudyEntryService(
            IPersonalContentStore store,
            IUserSessionService session,
            ILogger<StudyEntryService>? logger = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _logger = logger;
        }

        public Task<StudyEntryResult> AddAsync(StudyEntryRequest request, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var text = CleanText(request.Text);
            if (string.IsNullOrWhiteSpace(text))
            {
                return Task.FromResult(new StudyEntryResult(string.Empty, string.Empty, false, false, null));
            }

            if (text.Length > MaxTextLength)
            {
                throw new InvalidOperationException("内容过长，请缩短后再添加");
            }

            var userId = string.IsNullOrWhiteSpace(_session.CurrentUserId)
                ? Constants.DefaultUserId
                : _session.CurrentUserId;
            var (subject, subCategory) = ResolveCategory(request, text);

            var canonicalItem = new LearningItem
            {
                Subject = subject,
                SubCategory = subCategory,
                MainContent = text
            };
            var canonicalKey = LearningItemIdentity.GetCanonicalKey(canonicalItem);

            var existing = _store.Load(userId, subject, subCategory)
                .FirstOrDefault(i => string.Equals(
                    LearningItemIdentity.GetCanonicalKey(i), canonicalKey, StringComparison.Ordinal));

            if (existing != null)
            {
                return Task.FromResult(new StudyEntryResult(
                    LearningItemIdentity.EnsureKey(existing), canonicalKey, false, true, existing));
            }

            var item = new LearningItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Subject = subject,
                SubCategory = subCategory,
                MainContent = text,
                Status = LearningStatus.New,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            if (!string.IsNullOrWhiteSpace(request.Translation))
            {
                item.Meaning = Meaning.Create(request.Translation.Trim());
            }

            if (!string.IsNullOrWhiteSpace(request.Note))
            {
                item.Example = Example.Create(request.Note.Trim());
            }

            ApplySourceProperties(item, request);

            _store.Upsert(userId, subject, subCategory, item);
            _logger?.LogInformation(
                "Added study item {ItemId} from {SourceType} ({Subject}/{SubCategory})",
                item.Id, request.SourceType, subject, subCategory);

            return Task.FromResult(new StudyEntryResult(item.Id, canonicalKey, true, false, item));
        }

        public async Task<IReadOnlyList<StudyEntryResult>> AddRangeAsync(
            IEnumerable<StudyEntryRequest> requests, CancellationToken ct = default)
        {
            var results = new List<StudyEntryResult>();
            if (requests == null) return results;

            foreach (var request in requests)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    results.Add(await AddAsync(request, ct).ConfigureAwait(false));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to add study entry from {SourceType}: {Text}",
                        request.SourceType, request.Text);
                    results.Add(new StudyEntryResult(string.Empty, string.Empty, false, false, null));
                }
            }

            return results;
        }

        /// <summary>清理首尾空白、包裹引号并折叠连续空白。</summary>
        internal static string CleanText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var value = text.Trim();
            value = value.Trim('"', '\'', '“', '”', '‘', '’');
            value = Regex.Replace(value, @"\s+", " ");
            return value.Trim();
        }

        internal static (SubjectType Subject, SubCategoryType SubCategory) ResolveCategory(
            StudyEntryRequest request, string text)
        {
            if (request.Subject is { } subject && request.SubCategory is { } subCategory)
            {
                return (subject, subCategory);
            }

            var isEnglish = LooksLikeEnglish(text);

            if (request.Kind != StudyTextKind.Auto)
            {
                return (isEnglish ? SubjectType.English : SubjectType.Chinese, MapKind(request.Kind, isEnglish));
            }

            if (isEnglish)
            {
                var wordCount = CountWords(text);
                if (wordCount <= 1) return (SubjectType.English, SubCategoryType.EnglishWord);
                if (wordCount <= 6 && !EndsWithSentencePunctuation(text))
                    return (SubjectType.English, SubCategoryType.EnglishPhrase);
                return (SubjectType.English, SubCategoryType.EnglishSentence);
            }

            // 中文首期保守判断，避免把段落塞进“识字”。
            if (text.Length <= 2) return (SubjectType.Chinese, SubCategoryType.ChineseCharacter);
            if (text.Length <= 8) return (SubjectType.Chinese, SubCategoryType.ChinesePhrase);
            return (SubjectType.Chinese, SubCategoryType.ChineseComprehensive);
        }

        private static SubCategoryType MapKind(StudyTextKind kind, bool isEnglish)
        {
            if (isEnglish)
            {
                return kind switch
                {
                    StudyTextKind.Word => SubCategoryType.EnglishWord,
                    StudyTextKind.Phrase => SubCategoryType.EnglishPhrase,
                    StudyTextKind.Sentence => SubCategoryType.EnglishSentence,
                    _ => SubCategoryType.EnglishSentence
                };
            }

            return kind switch
            {
                StudyTextKind.Word => SubCategoryType.ChinesePhrase,
                StudyTextKind.Phrase => SubCategoryType.ChinesePhrase,
                StudyTextKind.Sentence => SubCategoryType.ChineseComprehensive,
                _ => SubCategoryType.ChineseComprehensive
            };
        }

        private static void ApplySourceProperties(LearningItem item, StudyEntryRequest request)
        {
            item.SetExtendedProperty("SourceType", request.SourceType);
            item.SetExtendedProperty("SourcePdfPath", request.PdfPath ?? string.Empty);
            item.SetExtendedProperty("SourcePdfHash", request.PdfHash ?? string.Empty);
            item.SetExtendedProperty("SourcePageIndex", request.PageIndex.ToString());
            item.SetExtendedProperty("SourceHighlightId", request.HighlightId ?? string.Empty);
            item.SetExtendedProperty("SourceAnnotationId", request.AnnotationId ?? string.Empty);
            item.SetExtendedProperty("SourceSentenceId", request.SentenceId ?? string.Empty);

            // P1：来源同时落到一等字段与来源锚点，供筛选与回跳原文使用。
            item.SourceType = LearningItemClassification.ParseSourceType(request.SourceType);

            var hasAnchor = request.PageIndex >= 0
                || !string.IsNullOrWhiteSpace(request.PdfPath)
                || !string.IsNullOrWhiteSpace(request.HighlightId)
                || !string.IsNullOrWhiteSpace(request.AnnotationId)
                || !string.IsNullOrWhiteSpace(request.SentenceId);

            if (hasAnchor)
            {
                item.SourceRef = new LearningSourceRef
                {
                    PdfPath = request.PdfPath,
                    PdfHash = request.PdfHash,
                    PageIndex = request.PageIndex,
                    HighlightId = request.HighlightId,
                    AnnotationId = request.AnnotationId,
                    SentenceId = request.SentenceId
                };
            }
        }

        internal static bool LooksLikeEnglish(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            var latin = 0;
            var cjk = 0;
            foreach (var ch in text)
            {
                if (ch >= '\u4e00' && ch <= '\u9fff') cjk++;
                else if (char.IsLetter(ch) && ch <= 127) latin++;
            }

            return latin > cjk;
        }

        internal static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return Regex.Matches(text, @"[A-Za-z0-9]+(?:['\-][A-Za-z0-9]+)*").Count;
        }

        private static bool EndsWithSentencePunctuation(string text)
        {
            var trimmed = text.TrimEnd();
            if (trimmed.Length == 0) return false;
            var last = trimmed[^1];
            return last is '.' or '!' or '?' or '。' or '！' or '？' or ';' or '；';
        }
    }
}
