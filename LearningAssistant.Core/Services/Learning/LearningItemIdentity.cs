using System.Text;
using System.Text.RegularExpressions;
using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习项身份工具：统一稳定 Id 的生成与判重键归一化。
    /// 判重键仅用于去重，不作为持久化主键。
    /// </summary>
    public static class LearningItemIdentity
    {
        /// <summary>确保学习项拥有稳定 Id；返回最终使用的 Key。</summary>
        public static string EnsureKey(LearningItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (string.IsNullOrWhiteSpace(item.Id))
                item.Id = Guid.NewGuid().ToString("N");
            return item.Id;
        }

        /// <summary>判重键：学科 + 子类别 + 规范化内容。</summary>
        public static string GetCanonicalKey(LearningItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            return $"{item.Subject}|{item.SubCategory}|{Normalize(item.GetMainContent())}";
        }

        /// <summary>
        /// 只做大小写、全半角与空白归一化，避免过度合并不同语义的内容。
        /// </summary>
        public static string Normalize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var value = text.Normalize(NormalizationForm.FormKC).Trim();
            value = Regex.Replace(value, @"\s+", " ");
            return value.ToLowerInvariant();
        }
    }
}
