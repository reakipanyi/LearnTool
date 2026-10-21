using LearningAssistant.Models.Pdf;
using System.Text.RegularExpressions;

namespace LearningAssistant.Services.Pdf;

/// <summary>
/// 把音频匹配规则编译为正则。统一处理正则/通配符两种写法，
/// 并在用户把通配符误填为正则时自动降级解释，避免规则被静默跳过。
/// </summary>
public static class PdfAudioPatternCompiler
{
    /// <summary>
    /// 编译规则；返回 null 表示不可用，diagnostic 中给出原因或自动修正提示。
    /// </summary>
    public static Regex? TryCompile(AudioMatchRule rule, out string? diagnostic)
    {
        diagnostic = null;
        var pattern = (rule.FileNamePattern ?? string.Empty).Trim();
        if (pattern.Length == 0)
        {
            diagnostic = $"规则「{rule.Name}」未填写匹配内容，已跳过";
            return null;
        }

        if (rule.PatternMode == AudioPatternMode.Wildcard)
            return BuildWildcardRegex(pattern);

        try
        {
            return new Regex(pattern, RegexOptions.IgnoreCase);
        }
        catch (ArgumentException ex)
        {
            // 常见误写：*.mp3、第*课 这类通配符不是合法正则，自动按通配符解释
            if (LooksLikeWildcard(pattern))
            {
                diagnostic = $"规则「{rule.Name}」不是合法正则（{ex.Message}），已自动按通配符解释";
                return BuildWildcardRegex(pattern);
            }

            diagnostic = $"规则「{rule.Name}」正则非法：{ex.Message}";
            return null;
        }
    }

    /// <summary>把通配符写法转成锚定到整个字符串的正则。</summary>
    public static Regex BuildWildcardRegex(string pattern)
    {
        var escaped = Regex.Escape(pattern.Trim())
            .Replace(@"\*", ".*")
            .Replace(@"\?", ".");

        return new Regex("^" + escaped + "$", RegexOptions.IgnoreCase);
    }

    /// <summary>是否包含通配符字符（用于 UI 提示与自动降级判断）。</summary>
    public static bool LooksLikeWildcard(string? pattern)
        => pattern != null && (pattern.IndexOf('*') >= 0 || pattern.IndexOf('?') >= 0);
}
