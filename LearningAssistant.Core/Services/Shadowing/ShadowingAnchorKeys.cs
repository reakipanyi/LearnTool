using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Shadowing
{
    /// <summary>
    /// V2 锚点：身份只由“页 + 规范化文本 + 同文本序号”构成，不含坐标、不含分句 ID，
    /// 自动高亮重建 / PDF 重渲染 / 重新分句（文本边界不变）后仍能找回原声切片。
    /// V1 仅用于读取已写盘的历史切片，新打点只写 V2。
    /// </summary>
    public static class ShadowingAnchorKeys
    {
        private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

        /// <summary>高亮身份：页 + 规范化文本 + 同文本序号；不含坐标，抗重建/重排。</summary>
        public static string BuildHighlightV2(PdfHighlight highlight, int partIndex, int textOrdinal)
        {
            string normalized = NormalizeKey(highlight.Text);
            string raw = string.Join("|", new[]
            {
                highlight.PageIndex.ToString(),
                normalized,
                textOrdinal.ToString()
            });
            return $"hl2:{highlight.PageIndex}:{Hash16(raw)}:{partIndex}";
        }

        /// <summary>页面文字身份：页 + 规范化文本 + 同文本序号；不含 sentence.Id，抗重分句。</summary>
        public static string BuildPageTextV2(int pageIndex, string text, int textOrdinal)
        {
            string normalized = NormalizeKey(text);
            string raw = string.Join("|", normalized, textOrdinal.ToString());
            return $"txt2:{pageIndex}:{Hash16(raw)}";
        }

        /// <summary>
        /// 与 cue 的合成：V2 锚点 + "|" + 组内序号。
        /// 同一 base 锚点覆盖多条 cue 时按句序递增（替代 sentence.Id 保证唯一）。
        /// </summary>
        public static string BuildCueV2(string baseAnchor, int groupOrdinal)
            => $"{baseAnchor}|{groupOrdinal}";

        /// <summary>V1 兼容锚点，用于读取已写盘切片。</summary>
        public static string BuildHighlightV1(PdfHighlight highlight, int partIndex)
            => ShadowingHighlightSource.BuildAnchorKey(highlight, partIndex);

        /// <summary>V1 cue 锚点：base 锚点 + "|" + sentence.Id。</summary>
        public static string BuildCueV1(string baseAnchor, string sentenceId)
            => $"{baseAnchor}|{sentenceId}";

        /// <summary>
        /// 锚点与文本分组的统一规范化：去非字母数字、折叠空白、小写。
        /// 与匹配（FindBestHighlight）共用同一口径，避免锚点与匹配漂移。
        /// </summary>
        public static string NormalizeKey(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var builder = new StringBuilder(text.Length);
            bool pendingSpace = false;
            foreach (char ch in text.Trim())
            {
                if (!char.IsLetterOrDigit(ch))
                {
                    pendingSpace = builder.Length > 0;
                    continue;
                }

                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(char.ToLowerInvariant(ch));
            }

            return Whitespace.Replace(builder.ToString(), " ").Trim();
        }

        private static string Hash16(string raw)
            => Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..16].ToLowerInvariant();
    }
}
