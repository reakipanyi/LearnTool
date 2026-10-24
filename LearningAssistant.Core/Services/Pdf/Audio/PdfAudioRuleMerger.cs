using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf;

/// <summary>
/// 目录共享规则 + 书级 rulePatch 的合并逻辑。
/// 存储层与运行时共用，避免两处实现语义漂移。
/// </summary>
public static class PdfAudioRuleMerger
{
    public static List<AudioMatchRule> Merge(IEnumerable<AudioMatchRule> directoryRules,
        PdfAudioRulePatch? patch)
    {
        if (patch == null) return directoryRules.Select(Clone).ToList();

        if (patch.ReplaceAll)
            return patch.ExtraRules.Select(Clone).ToList();

        var disabled = patch.DisabledRuleIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var effective = directoryRules
            .Where(rule => !disabled.Contains(rule.Id))
            .Select(Clone)
            .ToList();

        foreach (var extra in patch.ExtraRules)
        {
            int existing = effective.FindIndex(
                rule => string.Equals(rule.Id, extra.Id, StringComparison.OrdinalIgnoreCase));
            if (existing >= 0) effective[existing] = Clone(extra);
            else effective.Add(Clone(extra));
        }
        return effective;
    }

    public static AudioMatchRule Clone(AudioMatchRule rule) => new()
    {
        Id = rule.Id,
        Name = rule.Name,
        FileNamePattern = rule.FileNamePattern,
        PatternMode = rule.PatternMode,
        CaptureGroupIndex = rule.CaptureGroupIndex,
        CaptureGroupName = rule.CaptureGroupName,
        MapMode = rule.MapMode,
        PageOffset = rule.PageOffset,
        PagesPerUnit = rule.PagesPerUnit,
        Enabled = rule.Enabled,
        Priority = rule.Priority
    };
}
