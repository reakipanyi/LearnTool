namespace LearningAssistant.Services.Shadowing
{
    /// <summary>
    /// 单句发音参考评分结果。
    /// </summary>
    public class PronunciationScoreResult
    {
        /// <summary>
        /// 0-100 的综合参考分。
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// 词级编辑距离相似度（0-1）。
        /// </summary>
        public double Accuracy { get; set; }

        /// <summary>
        /// 原句词汇被识别出的覆盖率（0-1）。
        /// </summary>
        public double Coverage { get; set; }

        /// <summary>
        /// 未被识别出的原句词汇。
        /// </summary>
        public List<string> MissingWords { get; set; } = new();

        public string RecognizedText { get; set; } = string.Empty;

        /// <summary>
        /// 评分依据：text=语音识别文本相似度；completion=录音完成度降级。
        /// </summary>
        public string Basis { get; set; } = "text";

        /// <summary>
        /// 是否是一次有效评分（原句为空时为 false）。
        /// </summary>
        public bool IsValid { get; set; }
    }

    /// <summary>
    /// 纯算法发音参考评分：不依赖语音识别引擎，便于单元测试。
    /// </summary>
    public static class TextSimilarityScorer
    {
        private const double AccuracyWeight = 0.6;
        private const double CoverageWeight = 0.4;

        public static PronunciationScoreResult Score(string? expectedText, string? recognizedText)
        {
            var expected = Tokenize(expectedText);
            var recognized = Tokenize(recognizedText);

            if (expected.Count == 0)
            {
                return new PronunciationScoreResult
                {
                    Score = 0,
                    IsValid = false,
                    RecognizedText = recognizedText ?? string.Empty
                };
            }

            if (recognized.Count == 0)
            {
                return new PronunciationScoreResult
                {
                    Score = 0,
                    Accuracy = 0,
                    Coverage = 0,
                    MissingWords = new List<string>(expected),
                    IsValid = true,
                    RecognizedText = recognizedText ?? string.Empty
                };
            }

            int distance = LevenshteinDistance(expected, recognized);
            double accuracy = 1.0 - (double)distance / Math.Max(expected.Count, recognized.Count);

            var remaining = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var word in recognized)
            {
                remaining.TryGetValue(word, out int count);
                remaining[word] = count + 1;
            }

            int covered = 0;
            var missing = new List<string>();
            foreach (var word in expected)
            {
                if (remaining.TryGetValue(word, out int count) && count > 0)
                {
                    remaining[word] = count - 1;
                    covered++;
                }
                else
                {
                    missing.Add(word);
                }
            }

            double coverage = (double)covered / expected.Count;
            int score = (int)Math.Round(100 * (AccuracyWeight * accuracy + CoverageWeight * coverage));

            return new PronunciationScoreResult
            {
                Score = Math.Clamp(score, 0, 100),
                Accuracy = Math.Clamp(accuracy, 0, 1),
                Coverage = Math.Clamp(coverage, 0, 1),
                MissingWords = missing,
                IsValid = true,
                RecognizedText = recognizedText ?? string.Empty
            };
        }

        /// <summary>
        /// 无可用语音识别引擎时的降级评分：按录音时长相对原句音频时长的完成度给出参考分。
        /// 只表示「是否完整读完」，不代表发音准确度，界面需明确标注为完成度。
        /// </summary>
        /// <param name="expectedDurationMs">原句音频时长（毫秒）。</param>
        /// <param name="recordedDurationMs">本次录音时长（毫秒）。</param>
        public static PronunciationScoreResult ScoreCompletion(int expectedDurationMs, int recordedDurationMs)
        {
            if (expectedDurationMs <= 0 || recordedDurationMs <= 0)
            {
                return new PronunciationScoreResult
                {
                    Score = 0,
                    IsValid = false,
                    Basis = "completion"
                };
            }

            double ratio = (double)recordedDurationMs / expectedDurationMs;
            double score;
            if (ratio < 0.5)
                score = 60 * ratio / 0.5;
            else if (ratio <= 1.8)
                score = 70 + 30 * (1 - Math.Abs(ratio - 1.0) / 0.8);
            else
                score = Math.Max(0, 70 - 20 * (ratio - 1.8));

            return new PronunciationScoreResult
            {
                Score = Math.Clamp((int)Math.Round(score), 0, 100),
                Coverage = Math.Clamp(ratio, 0, 1),
                IsValid = true,
                Basis = "completion"
            };
        }

        /// <summary>
        /// 转小写并去掉标点，仅保留字母、数字、撇号；中文按字保留。
        /// </summary>
        internal static List<string> Tokenize(string? text)
        {
            var tokens = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return tokens;

            var sb = new System.Text.StringBuilder();
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c) || c == '\'')
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
                else if (sb.Length > 0)
                {
                    tokens.Add(sb.ToString());
                    sb.Clear();
                }
            }

            if (sb.Length > 0)
                tokens.Add(sb.ToString());

            return tokens;
        }

        private static int LevenshteinDistance(List<string> left, List<string> right)
        {
            int n = left.Count;
            int m = right.Count;
            var previous = new int[m + 1];
            var current = new int[m + 1];

            for (int j = 0; j <= m; j++)
                previous[j] = j;

            for (int i = 1; i <= n; i++)
            {
                current[0] = i;
                for (int j = 1; j <= m; j++)
                {
                    int cost = string.Equals(left[i - 1], right[j - 1], StringComparison.Ordinal) ? 0 : 1;
                    current[j] = Math.Min(
                        Math.Min(current[j - 1] + 1, previous[j] + 1),
                        previous[j - 1] + cost);
                }

                (previous, current) = (current, previous);
            }

            return previous[m];
        }
    }
}
