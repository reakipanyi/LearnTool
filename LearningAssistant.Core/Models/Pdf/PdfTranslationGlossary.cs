namespace LearningAssistant.Models.Pdf
{
    /// <summary>术语表条目：原文术语 → 统一译名</summary>
    public sealed class PdfGlossaryEntry
    {
        public string Source { get; init; } = string.Empty;

        public string Target { get; init; } = string.Empty;
    }

    /// <summary>按文档指纹缓存的术语表</summary>
    public sealed class PdfTranslationGlossary
    {
        public string DocumentFingerprint { get; init; } = string.Empty;

        public List<PdfGlossaryEntry> Entries { get; init; } = new();
    }

    /// <summary>翻译润色结果：失败时 Text 为原始机器译文，便于调用方直接回退</summary>
    public sealed class PdfTranslationPolishResult
    {
        public bool Success { get; init; }

        public string? ErrorMessage { get; init; }

        public string Text { get; init; } = string.Empty;

        /// <summary>原文出现但译文中未使用指定译名的术语（一致性检查结果）</summary>
        public IReadOnlyList<PdfGlossaryEntry> MissingTerms { get; init; } = Array.Empty<PdfGlossaryEntry>();

        public static PdfTranslationPolishResult Ok(string text, IReadOnlyList<PdfGlossaryEntry>? missingTerms = null)
            => new() { Success = true, Text = text, MissingTerms = missingTerms ?? Array.Empty<PdfGlossaryEntry>() };

        public static PdfTranslationPolishResult Fail(string fallbackText, string message)
            => new() { Success = false, Text = fallbackText, ErrorMessage = message };
    }
}
