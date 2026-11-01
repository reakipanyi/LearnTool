using System.Text.Json;

namespace LearningAssistant.Forms.Pdf;

/// <summary>
/// 阅读器命令分组：与工具栏/菜单的视觉分组保持一致，
/// 后续入口去重时按分组决定“谁是主入口、谁只做别名”。
/// </summary>
internal enum PdfCommandGroup
{
    /// <summary>选择/浏览：不产生标注的默认态。</summary>
    Selection,

    /// <summary>阅读标注：高亮、文字、马赛克等阅读时最高频的标注。</summary>
    Markup,

    /// <summary>绘制工具：矩形、椭圆、箭头、画笔、橡皮等。</summary>
    Draw,

    /// <summary>阅读辅助：阅读引导线、护眼色层、阅读时长等纯视图能力。</summary>
    Reading,

    /// <summary>智能入口：翻译与 AI。</summary>
    Smart,

    /// <summary>编辑动作：撤销、重做、更多工具等。</summary>
    Edit,

    /// <summary>视图：缩放、双页、夜间模式、全屏等。</summary>
    View,

    /// <summary>文件：打开文件夹、导出、打印等。</summary>
    File,

    /// <summary>学习：摘录、跟读、闪卡等。</summary>
    Study,

    /// <summary>系统：设置、缓存管理等。</summary>
    System
}

/// <summary>
/// 命令可出现的入口。<see cref="Toolbar"/> 表示一级条主入口，
/// 其余入口在去重阶段逐步收敛到同一个命令 Id。
/// </summary>
[Flags]
internal enum PdfCommandSurfaces
{
    None = 0,
    Toolbar = 1,
    ContextMenu = 2,
    MoreMenu = 4,
    CommandPalette = 8,
    Sidebar = 16,
    Header = 32,
    Shortcut = 64
}

/// <summary>
/// 一条阅读器命令的静态定义。只描述“是什么、在哪出现、是否危险”，
/// 不持有窗体状态，便于工具栏、右键菜单、命令面板共用同一份目录。
/// </summary>
internal sealed class PdfCommandDefinition
{
    public PdfCommandDefinition(string id, string title, PdfCommandGroup group)
    {
        Id = id;
        Title = title;
        Group = group;
    }

    /// <summary>稳定命令 Id，例如 tool.highlight / edit.undo。</summary>
    public string Id { get; }

    /// <summary>展示名，用于菜单与命令面板检索。</summary>
    public string Title { get; }

    public PdfCommandGroup Group { get; }

    /// <summary>补充说明或别名关键词，命令面板按它做二次匹配。</summary>
    public string Hint { get; init; } = string.Empty;

    /// <summary>展示用快捷键文本，例如 Ctrl+Z。</summary>
    public string? Shortcut { get; init; }

    /// <summary>命令当前出现的入口集合。</summary>
    public PdfCommandSurfaces Surfaces { get; init; } = PdfCommandSurfaces.None;

    /// <summary>破坏性操作：后续需要二次确认与撤销覆盖。</summary>
    public bool IsDestructive { get; init; }

    /// <summary>是否依赖已打开的文档。</summary>
    public bool RequiresDocument { get; init; }

    /// <summary>开关型命令：需要回显开启态，例如翻译、阅读引导线。</summary>
    public bool IsToggle { get; init; }

    /// <summary>对应的功能开关（如 PdfHiddenFeatures 的项），空表示常开。</summary>
    public string? FeatureFlag { get; init; }

    /// <summary>本地遥测键，用于后续统计点击量；不联网。</summary>
    public string? TelemetryKey { get; init; }
}

/// <summary>
/// 单个命令的本地使用记录：累计次数与最近一次使用时间（Ticks）。
/// </summary>
internal sealed class PdfCommandUsageRecord
{
    public int Count { get; set; }

    public long LastUsedTicks { get; set; }
}

/// <summary>
/// 阅读器命令注册表：命令目录 + 本地使用计数。
/// 计数只落在当前用户目录（pdf_command_usage.json），不联网；
/// 读取失败或写入失败都静默降级，不影响任何阅读功能。
/// </summary>
internal sealed class PdfReaderCommandRegistry
{
    /// <summary>使用计数文件名。</summary>
    public const string UsageFileName = "pdf_command_usage.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly Dictionary<string, PdfCommandDefinition> _definitions =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, PdfCommandUsageRecord> _usage =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Func<string> _directoryProvider;
    private readonly object _gate = new();
    private bool _usageDirty;

    public PdfReaderCommandRegistry(Func<string> directoryProvider)
    {
        _directoryProvider = directoryProvider ?? throw new ArgumentNullException(nameof(directoryProvider));
        LoadUsage();
    }

    /// <summary>当前已注册的全部命令（按分组顺序输出，便于菜单直接枚举）。</summary>
    public IReadOnlyList<PdfCommandDefinition> Definitions => _definitions.Values
        .OrderBy(d => d.Group)
        .ThenBy(d => d.Title, StringComparer.CurrentCulture)
        .ToList();

    /// <summary>登记一条命令；重复 Id 直接覆盖，方便后续迭代时同一 Id 只保留一份定义。</summary>
    public void Register(PdfCommandDefinition definition)
    {
        if (definition == null) return;
        _definitions[definition.Id] = definition;
    }

    /// <summary>按 Id 查找命令定义。</summary>
    public PdfCommandDefinition? Find(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        return _definitions.TryGetValue(id, out var definition) ? definition : null;
    }

    /// <summary>记录一次真实使用：累计次数并刷新最近使用时间。</summary>
    public void RecordUsage(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        lock (_gate)
        {
            if (!_usage.TryGetValue(id, out var record))
            {
                record = new PdfCommandUsageRecord();
                _usage[id] = record;
            }

            record.Count++;
            record.LastUsedTicks = DateTime.UtcNow.Ticks;
            _usageDirty = true;
        }
    }

    /// <summary>读取累计次数；未使用过返回 0。</summary>
    public int GetUsageCount(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return 0;

        lock (_gate)
        {
            return _usage.TryGetValue(id, out var record) ? record.Count : 0;
        }
    }

    /// <summary>读取最近一次使用时间；未使用过返回 <see cref="DateTime.MinValue"/>。</summary>
    public DateTime GetLastUsed(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return DateTime.MinValue;

        lock (_gate)
        {
            return _usage.TryGetValue(id, out var record) && record.LastUsedTicks > 0
                ? new DateTime(record.LastUsedTicks, DateTimeKind.Utc)
                : DateTime.MinValue;
        }
    }

    /// <summary>
    /// 取最近使用过的命令，供后续「最近使用槽」与「更多」菜单置顶使用；
    /// 只返回已注册且有使用记录的命令，可再按分组过滤。
    /// </summary>
    public IReadOnlyList<PdfCommandDefinition> GetRecentlyUsed(int limit, PdfCommandGroup? group = null)
    {
        if (limit <= 0) return Array.Empty<PdfCommandDefinition>();

        List<PdfCommandDefinition> candidates;
        lock (_gate)
        {
            candidates = _usage
                .Where(pair => pair.Value.Count > 0 && pair.Value.LastUsedTicks > 0)
                .Select(pair => new
                {
                    Definition = _definitions.TryGetValue(pair.Key, out var definition) ? definition : null,
                    pair.Value.LastUsedTicks
                })
                .Where(item => item.Definition != null)
                .OrderByDescending(item => item.LastUsedTicks)
                .Select(item => item.Definition!)
                .ToList();
        }

        if (group.HasValue)
            candidates = candidates.Where(d => d.Group == group.Value).ToList();

        return candidates.Take(limit).ToList();
    }

    /// <summary>把挂起的计数写入磁盘；窗口关闭时调用，写入失败静默忽略。</summary>
    public void Flush()
    {
        lock (_gate)
        {
            if (!_usageDirty) return;

            try
            {
                string directory = _directoryProvider();
                if (string.IsNullOrWhiteSpace(directory)) return;

                Directory.CreateDirectory(directory);
                string file = Path.Combine(directory, UsageFileName);
                string temp = file + ".tmp";

                // 只持久化计数与时间，避免把命令定义写进用户数据
                var snapshot = _usage.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value,
                    StringComparer.OrdinalIgnoreCase);

                File.WriteAllText(temp, JsonSerializer.Serialize(snapshot, JsonOptions));
                File.Move(temp, file, true);
                _usageDirty = false;
            }
            catch (Exception)
            {
                // 使用计数只是频次校准数据，写失败不影响阅读功能
            }
        }
    }

    private void LoadUsage()
    {
        try
        {
            string directory = _directoryProvider();
            if (string.IsNullOrWhiteSpace(directory)) return;

            string file = Path.Combine(directory, UsageFileName);
            if (!File.Exists(file)) return;

            var records = JsonSerializer.Deserialize<Dictionary<string, PdfCommandUsageRecord>>(
                File.ReadAllText(file));
            if (records == null) return;

            foreach (var pair in records)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null) continue;
                _usage[pair.Key] = pair.Value;
            }
        }
        catch (Exception)
        {
            // 计数文件损坏时按“从未使用过”处理，下一次记录会重新建文件
        }
    }
}
