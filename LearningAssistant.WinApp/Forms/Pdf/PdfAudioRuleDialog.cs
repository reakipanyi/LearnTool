using LearningAssistant.Managers;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;

namespace LearningAssistant.Forms.Pdf;

/// <summary>
/// 音频匹配规则编辑对话框。控件均在设计器中创建，便于可视化预览。
/// </summary>
public partial class PdfAudioRuleDialog : Form
{
    private readonly PdfAudioProfile _profile;
    private readonly List<AudioMatchRule> _rules;
    private readonly PdfAudioTrackManager? _manager;
    private readonly PdfAudioMatchMode _originalMatchMode;
    private string? _singleAudioFile;
    /// <summary>字段区当前对应的规则；切换选择前先把未点“应用修改”的编辑回写</summary>
    private AudioMatchRule? _editingRule;

    private readonly List<AudioPageMapMode> _modes = new()
    { AudioPageMapMode.PageIndex, AudioPageMapMode.Sequence, AudioPageMapMode.Range };
    private bool _populating;

    /// <summary>供 Visual Studio 设计器实例化使用。</summary>
    public PdfAudioRuleDialog()
    {
        InitializeComponent();
        _profile = new PdfAudioProfile();
        _rules = new List<AudioMatchRule>();
        _originalMatchMode = PdfAudioMatchMode.PerPage;
        InitializeModeSelector();
    }

    public PdfAudioRuleDialog(PdfAudioProfile profile, IPdfReaderFormAccess form, PdfAudioTrackManager manager)
    {
        _profile = profile;
        _manager = manager;
        _originalMatchMode = profile.MatchMode;
        _singleAudioFile = profile.SingleAudioFile;
        // 拷贝一份规则编辑：取消时不能影响已生效的配置
        _rules = _profile.Rules.OrderByDescending(r => r.Priority).Select(CloneRule).ToList();

        InitializeComponent();
        lvRules.ShowItemToolTips = true;

        var pdfName = Path.GetFileName(_profile.PdfPath);
        if (!string.IsNullOrEmpty(pdfName))
            Text = $"音频匹配规则 - {pdfName}（作用于本目录所有书）";

        cmbSyntax.Items.Add("正则表达式");
        cmbSyntax.Items.Add("通配符");
        cmbSyntax.SelectedIndex = 0;
        foreach (var m in _modes) cmbMode.Items.Add(m.ToString());
        InitializeModeSelector();
        foreach (var r in _rules) AddListItem(r);
        if (lvRules.Items.Count > 0) lvRules.Items[0].Selected = true;
        PopulateFields();
        UpdateModeHint();
        RefreshPreview();

        // 非模态窗口里按钮的 DialogResult 不会自动关窗，必须显式关闭
        btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
    }

    /// <summary>初始化匹配方式下拉框，并按保存的配置恢复选择。</summary>
    private void InitializeModeSelector()
    {
        cmbMatchMode.Items.Add("按页匹配（一个 PDF 对应多个音频）");
        cmbMatchMode.Items.Add("整篇共用一个音频");
        cmbMatchMode.SelectedIndex = _profile.MatchMode == PdfAudioMatchMode.SingleAudio ? 1 : 0;
        UpdateSingleAudioLabel();
        UpdateModeAvailability();
    }

    private PdfAudioMatchMode SelectedMatchMode
        => cmbMatchMode.SelectedIndex == 1 ? PdfAudioMatchMode.SingleAudio : PdfAudioMatchMode.PerPage;

    private void UpdateSingleAudioLabel()
    {
        if (string.IsNullOrWhiteSpace(_singleAudioFile))
        {
            labelSingleAudio.Text = "未选择音频";
            tooltip.SetToolTip(labelSingleAudio, null);
            return;
        }

        var isExternal = Path.IsPathRooted(_singleAudioFile);
        labelSingleAudio.Text = isExternal
            ? $"{Path.GetFileName(_singleAudioFile)}（外部文件）"
            : _singleAudioFile;
        tooltip.SetToolTip(labelSingleAudio,
            isExternal ? _singleAudioFile : Path.Combine(Path.GetDirectoryName(_profile.PdfPath) ?? "", _singleAudioFile));
    }

    /// <summary>整篇单音频模式下按页规则不可用，反之单音频选择不可用。</summary>
    private void UpdateModeAvailability()
    {
        bool single = SelectedMatchMode == PdfAudioMatchMode.SingleAudio;
        lvRules.Enabled = !single;
        editPanel.Enabled = !single;
        btnAdd.Enabled = !single;
        btnDelete.Enabled = !single;
        btnUp.Enabled = !single;
        btnDown.Enabled = !single;
        btnApply.Enabled = !single;
        btnSuggest.Enabled = !single;
        btnClearRules.Enabled = !single;
        btnPickSingle.Enabled = single;
        labelSingleAudio.Enabled = single;
    }

    private void CmbMatchMode_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_populating) return;
        // 切到整篇单音频时自动带入与 PDF 同名的音频，免去逐本手动选择
        if (SelectedMatchMode == PdfAudioMatchMode.SingleAudio && string.IsNullOrWhiteSpace(_singleAudioFile))
            _singleAudioFile = _manager?.AutoResolveSingleAudio();
        UpdateSingleAudioLabel();
        UpdateModeAvailability();
        SchedulePreview();
    }

    private void ButtonPickSingle_Click(object? sender, EventArgs e)
    {
        var pdfDir = Path.GetDirectoryName(_profile.PdfPath);
        using var ofd = new OpenFileDialog
        {
            Title = "选择整篇共用的音频",
            Filter = "音频文件|*.mp3;*.wav;*.m4a;*.aac;*.flac;*.ogg;*.wma",
            InitialDirectory = !string.IsNullOrEmpty(pdfDir) && Directory.Exists(pdfDir) ? pdfDir : ""
        };
        if (ofd.ShowDialog(this) != DialogResult.OK) return;

        _singleAudioFile = ToProfilePath(ofd.FileName);
        UpdateSingleAudioLabel();
        SchedulePreview();
    }

    private void ButtonClearRules_Click(object? sender, EventArgs e)
    {
        if (_rules.Count == 0) return;
        if (MessageBox.Show(this,
                $"确定清空本目录共享的 {_rules.Count} 条匹配规则？\n点「保存并关闭」后生效，会影响本目录所有书。" +
                "\n如需只调整本书，请改用「本书规则补丁…」。",
                "清空规则", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        _rules.Clear();
        lvRules.Items.Clear();
        _editingRule = null;
        RefreshPreview();
    }

    private void ButtonReset_Click(object? sender, EventArgs e)
    {
        var pdfName = string.IsNullOrEmpty(_profile.PdfPath) ? "当前文件" : Path.GetFileName(_profile.PdfPath);
        if (MessageBox.Show(this,
                $"将清空本目录共享的匹配规则与「{pdfName}」的书级补丁、手动匹配、匹配缓存、整篇音频设置。\n" +
                "该操作立即生效，规则变更会影响本目录所有书，确定继续？",
                "重置本目录规则", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        _manager?.ResetMatching();

        _rules.Clear();
        lvRules.Items.Clear();
        _editingRule = null;
        _singleAudioFile = null;
        cmbMatchMode.SelectedIndex = 0;
        UpdateSingleAudioLabel();
        UpdateModeAvailability();
        RefreshPreview();
    }

    private void LvRules_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var selected = SelectedRule;
        // 切换行之前把字段区未点“应用修改”的编辑回写，避免改完直接点下一行就丢了
        if (_editingRule != null && !ReferenceEquals(_editingRule, selected))
            ApplyFieldsTo(_editingRule);
        _editingRule = selected;
        PopulateFields();
        SchedulePreview();
    }

    private AudioMatchRule? SelectedRule
        => lvRules.SelectedItems.Count > 0 && lvRules.SelectedItems[0].Tag is AudioMatchRule r ? r : null;

    private void Field_TextChanged(object? sender, EventArgs e) => SchedulePreview();

    private void Field_ValueChanged(object? sender, EventArgs e) => SchedulePreview();

    private void Field_CheckedChanged(object? sender, EventArgs e) => SchedulePreview();

    private void TxtPattern_TextChanged(object? sender, EventArgs e)
    {
        ValidatePatternField();
        SchedulePreview();
    }

    private void CmbSyntax_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdatePatternHint();
        ValidatePatternField();
        SchedulePreview();
    }

    private void CmbMode_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateModeHint();
        SchedulePreview();
    }

    private void PreviewTimer_Tick(object? sender, EventArgs e)
    {
        previewTimer.Stop();
        RefreshPreview();
    }

    private void ButtonAdd_Click(object? sender, EventArgs e) => OnAdd();

    private void ButtonDelete_Click(object? sender, EventArgs e) => OnDelete();

    private void ButtonUp_Click(object? sender, EventArgs e) => MoveSelected(-1);

    private void ButtonDown_Click(object? sender, EventArgs e) => MoveSelected(1);

    private void ButtonApply_Click(object? sender, EventArgs e)
    {
        ApplyToSelected();
        RefreshPreview();
    }

    private void ButtonSuggest_Click(object? sender, EventArgs e) => OnSuggestFromManual();

    private void ButtonOk_Click(object? sender, EventArgs e)
    {
        ApplyToSelected();

        var mode = SelectedMatchMode;
        if (mode == PdfAudioMatchMode.SingleAudio && string.IsNullOrWhiteSpace(_singleAudioFile))
        {
            btnOk.DialogResult = DialogResult.None;
            MessageBox.Show(this, "整篇单音频模式需要先选择一个音频文件（或切回按页匹配）。",
                "尚未选择音频", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (mode == PdfAudioMatchMode.PerPage && !ValidateAllRules(out var message))
        {
            btnOk.DialogResult = DialogResult.None;
            MessageBox.Show(this, message, "匹配规则不可用", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        bool modeChanged = _originalMatchMode != mode;
        bool singleChanged = !string.Equals(_profile.SingleAudioFile, _singleAudioFile, StringComparison.OrdinalIgnoreCase);
        // 切换匹配方式或更换整篇音频后，旧的按页绑定/缓存不再适用，必须清掉
        if (modeChanged || singleChanged)
        {
            _profile.ManualOverrides.Clear();
            _profile.PageIndex.Clear();
        }

        _profile.MatchMode = mode;
        _profile.SingleAudioFile = _singleAudioFile;
        _profile.Rules = _rules;
        DialogResult = DialogResult.OK;
        // 非模态窗口必须显式关闭，否则 FormClosed 不触发、规则不会保存
        Close();
    }

    /// <summary>PDF 同目录内保存相对路径，目录外保存绝对路径。</summary>
    private string ToProfilePath(string fullPath)
    {
        var pdfDir = Path.GetDirectoryName(_profile.PdfPath);
        if (string.IsNullOrEmpty(pdfDir)) return fullPath;
        var baseDir = Path.GetFullPath(pdfDir);
        var target = Path.GetFullPath(fullPath);
        return target.StartsWith(baseDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            ? Path.GetRelativePath(baseDir, target)
            : target;
    }

    private void UpdateModeHint()
    {
        var mode = _modes[Math.Max(0, cmbMode.SelectedIndex)];
        labelModeHint.Text = mode switch
        {
            AudioPageMapMode.PageIndex => "页码 = 捕获数字 + 偏移，例如 P12.mp3 配 -1 映射到第 11 页",
            AudioPageMapMode.Sequence => "匹配曲目按数字升序依次映射，从偏移页开始连续占用页面",
            AudioPageMapMode.Range => "起始页 = (数字 - 1) × 每单元页数 + 偏移，一个文件覆盖多页",
            _ => ""
        };
    }

    /// <summary>按列表顺序交换相邻规则，并重排优先级（越靠前优先级越高）</summary>
    private void MoveSelected(int delta)
    {
        if (lvRules.SelectedItems.Count == 0) return;
        var index = lvRules.SelectedIndices[0];
        var target = index + delta;
        if (target < 0 || target >= _rules.Count) return;

        (_rules[index], _rules[target]) = (_rules[target], _rules[index]);
        var item = lvRules.Items[index];
        lvRules.Items.RemoveAt(index);
        lvRules.Items.Insert(target, item);
        for (var i = 0; i < _rules.Count; i++)
            _rules[i].Priority = _rules.Count - 1 - i;
        for (var i = 0; i < lvRules.Items.Count; i++)
            lvRules.Items[i].SubItems[2].Text = _rules[i].Priority.ToString();

        item.Selected = true;
        item.EnsureVisible();
        RefreshPreview();
    }

    private static AudioMatchRule CloneRule(AudioMatchRule r)
    {
        return new AudioMatchRule
        {
            Id = r.Id,
            Name = r.Name,
            FileNamePattern = r.FileNamePattern,
            PatternMode = r.PatternMode,
            CaptureGroupIndex = r.CaptureGroupIndex,
            CaptureGroupName = r.CaptureGroupName,
            MapMode = r.MapMode,
            PageOffset = r.PageOffset,
            PagesPerUnit = r.PagesPerUnit,
            Enabled = r.Enabled,
            Priority = r.Priority
        };
    }

    private void AddListItem(AudioMatchRule r)
    {
        var item = new ListViewItem(r.Name) { Tag = r };
        item.SubItems.Add(r.MapMode.ToString());
        item.SubItems.Add(r.Priority.ToString());
        item.SubItems.Add(r.Enabled ? "是" : "否");
        item.SubItems.Add(r.FileNamePattern);
        lvRules.Items.Add(item);
    }

    private void OnAdd()
    {
        var rule = new AudioMatchRule
        {
            Name = "新规则",
            PatternMode = AudioPatternMode.Wildcard,
            FileNamePattern = "*.mp3"
        };
        _rules.Add(rule);
        AddListItem(rule);
        lvRules.Items[lvRules.Items.Count - 1].Selected = true;
        RefreshPreview();
    }

    private void OnDelete()
    {
        if (lvRules.SelectedItems.Count == 0) return;
        var item = lvRules.SelectedItems[0];
        if (item.Tag is AudioMatchRule r) _rules.Remove(r);
        _editingRule = null;
        lvRules.Items.Remove(item);
        RefreshPreview();
    }

    /// <summary>把已有手动匹配反推为一条规则，加入列表由用户确认后随“保存并关闭”生效</summary>
    private void OnSuggestFromManual()
    {
        var suggestion = PdfAudioRuleSuggester.Suggest(_profile.ManualOverrides, _profile.Tracks);
        if (suggestion == null)
        {
            MessageBox.Show(this, "当前没有可归纳的手动匹配（需手动指定的文件名中包含数字）。",
                "无法归纳", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        suggestion.Rule.Priority = _rules.Count == 0 ? 0 : _rules.Max(r => r.Priority) + 1;
        _rules.Add(suggestion.Rule);
        AddListItem(suggestion.Rule);
        lvRules.Items[lvRules.Items.Count - 1].Selected = true;
        RefreshPreview();

        var note = string.IsNullOrEmpty(suggestion.Note) ? "" : "\n" + suggestion.Note;
        MessageBox.Show(this,
            $"已生成规则：{suggestion.Rule.FileNamePattern}\n覆盖 {suggestion.OverrideCount} 处手动匹配。" +
            $"请确认试算结果后再点“保存并关闭”。{note}",
            "归纳完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void PopulateFields()
    {
        var r = SelectedRule;
        if (r == null) return;
        _populating = true;
        try
        {
            txtName.Text = r.Name;
            txtPattern.Text = r.FileNamePattern;
            cmbSyntax.SelectedIndex = r.PatternMode == AudioPatternMode.Wildcard ? 1 : 0;
            cmbMode.SelectedIndex = Math.Max(0, _modes.IndexOf(r.MapMode));
            txtCapture.Text = r.CaptureGroupIndex?.ToString() ?? "";
            numOffset.Value = Math.Clamp(r.PageOffset, -1000m, 10000m);
            numPagesPerUnit.Value = Math.Clamp(r.PagesPerUnit, 1m, 1000m);
            numPriority.Value = Math.Clamp(r.Priority, 0m, 10000m);
            chkEnabled.Checked = r.Enabled;
        }
        finally
        {
            _populating = false;
        }
        UpdateModeHint();
        ValidatePatternField();
    }

    private void ApplyToSelected()
    {
        if (SelectedRule is AudioMatchRule r) ApplyFieldsTo(r);
    }

    /// <summary>把字段区内容写回指定规则，并同步列表行显示。</summary>
    private void ApplyFieldsTo(AudioMatchRule r)
    {
        r.Name = txtName.Text.Trim();
        r.FileNamePattern = txtPattern.Text;
        r.PatternMode = GetSelectedPatternMode();
        r.MapMode = _modes[Math.Max(0, cmbMode.SelectedIndex)];
        r.CaptureGroupIndex = int.TryParse(txtCapture.Text.Trim(), out var g) ? g : null;
        r.PageOffset = (int)numOffset.Value;
        r.PagesPerUnit = (int)numPagesPerUnit.Value;
        r.Priority = (int)numPriority.Value;
        r.Enabled = chkEnabled.Checked;
        foreach (ListViewItem item in lvRules.Items)
        {
            if (!ReferenceEquals(item.Tag, r)) continue;
            RefreshListItem(item, r);
            break;
        }
        ValidatePatternField();
    }

    private static void RefreshListItem(ListViewItem item, AudioMatchRule r)
    {
        item.Text = r.Name;
        item.SubItems[1].Text = r.MapMode.ToString();
        item.SubItems[2].Text = r.Priority.ToString();
        item.SubItems[3].Text = r.Enabled ? "是" : "否";
        item.SubItems[4].Text = r.FileNamePattern;
    }

    private void SchedulePreview()
    {
        if (_populating) return;
        previewTimer.Stop();
        previewTimer.Start();
    }

    /// <summary>用当前编辑中的字段（未点“应用修改”）组装试算规则</summary>
    private List<AudioMatchRule> BuildPreviewRules()
    {
        var list = _rules.Select(CloneRule).ToList();
        if (lvRules.SelectedItems.Count == 0 || lvRules.SelectedItems[0].Tag is not AudioMatchRule selected)
            return list;
        var index = _rules.IndexOf(selected);
        if (index < 0) return list;
        list[index] = new AudioMatchRule
        {
            Id = selected.Id,
            Name = string.IsNullOrWhiteSpace(txtName.Text) ? "未命名规则" : txtName.Text.Trim(),
            FileNamePattern = txtPattern.Text,
            PatternMode = GetSelectedPatternMode(),
            CaptureGroupIndex = int.TryParse(txtCapture.Text.Trim(), out var group) ? group : null,
            MapMode = _modes[Math.Max(0, cmbMode.SelectedIndex)],
            PageOffset = (int)numOffset.Value,
            PagesPerUnit = (int)numPagesPerUnit.Value,
            Enabled = chkEnabled.Checked,
            Priority = (int)numPriority.Value
        };
        return list;
    }

    private void RefreshPreview()
    {
        if (_manager == null) return;

        var mode = SelectedMatchMode;
        var result = _manager.PreviewRules(BuildPreviewRules(), mode, _singleAudioFile);
        // 先按曲目反查页码，避免对每个曲目重复扫描整个 PageIndex
        var pagesByTrack = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in result.PageIndex)
        {
            if (!pagesByTrack.TryGetValue(kv.Value.FilePath, out var pages))
                pagesByTrack[kv.Value.FilePath] = pages = new List<int>();
            pages.Add(kv.Key + 1);
        }

        lstPreview.BeginUpdate();
        lstPreview.Items.Clear();
        if (mode == PdfAudioMatchMode.SingleAudio)
        {
            var track = result.Tracks.FirstOrDefault();
            lstPreview.Items.Add(track == null ? "未选择音频" : $"{track.RelativePath}  →  全篇");
        }
        else foreach (var track in result.Tracks)
        {
            var pages = pagesByTrack.TryGetValue(track.FilePath, out var list)
                ? string.Join(",", list.OrderBy(p => p).Take(6)) + (list.Count > 6 ? "…" : "")
                : "未匹配";
            lstPreview.Items.Add($"{track.RelativePath}  →  {pages}");
        }
        lstPreview.EndUpdate();

        var total = _manager.PageCount;
        labelPreviewSummary.Text = total > 0
            ? $"试算：已匹配 {result.PageIndex.Count}/{total} 页，未匹配 {result.UnmatchedPages.Count} 页"
            : "试算：请先打开 PDF";
        labelDiagnostics.Text = _manager.Diagnostics.Count > 0
            ? string.Join("；", _manager.Diagnostics)
            : "";

        UpdateRuleStates();
    }

    /// <summary>
    /// 行内可见性：0 命中的启用规则标黄并给出提示，禁用的规则置灰。
    /// 仅按文件名/相对路径统计，避免为每条规则重复跑整轮页码映射。
    /// </summary>
    private void UpdateRuleStates()
    {
        var tracks = _profile.Tracks;
        var defaultColor = lvRules.ForeColor;
        var warningColor = Color.FromArgb(180, 95, 6);
        for (int i = 0; i < lvRules.Items.Count && i < _rules.Count; i++)
        {
            var rule = _rules[i];
            var item = lvRules.Items[i];
            if (!rule.Enabled)
            {
                item.ForeColor = SystemColors.GrayText;
                item.ToolTipText = "规则已禁用，不参与匹配";
                item.SubItems[4].Text = rule.FileNamePattern;
                continue;
            }

            int hits = tracks.Count == 0 ? -1 : CountPatternMatches(rule, tracks);
            item.ForeColor = hits == 0 ? warningColor : defaultColor;
            item.ToolTipText = hits == 0
                ? "未命中任何音频文件：请检查模式或文件名示例"
                : $"命中 {hits} 个音频文件";
            item.SubItems[4].Text = hits == 0
                ? rule.FileNamePattern + "  · 0 命中"
                : rule.FileNamePattern;
        }
    }

    private static int CountPatternMatches(AudioMatchRule rule, IReadOnlyList<AudioTrack> tracks)
    {
        var regex = PdfAudioPatternCompiler.TryCompile(rule, out _);
        if (regex == null) return 0;
        int count = 0;
        foreach (var track in tracks)
        {
            if (regex.IsMatch(track.FileName) || regex.IsMatch(track.RelativePath)) count++;
        }
        return count;
    }

    private AudioPatternMode GetSelectedPatternMode()
        => cmbSyntax.SelectedIndex == 1 ? AudioPatternMode.Wildcard : AudioPatternMode.Regex;

    private void UpdatePatternHint()
    {
        var wildcard = GetSelectedPatternMode() == AudioPatternMode.Wildcard;
        txtPattern.PlaceholderText = wildcard ? "例：*.mp3、第?课.mp3" : @"例：P(\d+)\.mp3";
        tooltip.SetToolTip(txtPattern, wildcard
            ? "通配符：* 匹配任意字符，? 匹配单个字符；优先匹配文件名，未命中再匹配相对路径；页码取文件名中的第一个数字"
            : "正则：捕获组序号从 1 开始；未写捕获组时自动取文件名中的第一个数字");
    }

    private bool ValidatePatternField()
    {
        var pattern = txtPattern.Text;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            txtPattern.BackColor = Color.MistyRose;
            return false;
        }

        var probe = new AudioMatchRule
        {
            Name = "当前规则",
            FileNamePattern = pattern,
            PatternMode = GetSelectedPatternMode()
        };

        if (PdfAudioPatternCompiler.TryCompile(probe, out _) == null)
        {
            txtPattern.BackColor = Color.MistyRose;
            return false;
        }

        txtPattern.BackColor = SystemColors.Window;
        return true;
    }

    private bool ValidateAllRules(out string message)
    {
        foreach (var rule in _rules.Where(r => r.Enabled))
        {
            if (PdfAudioPatternCompiler.TryCompile(rule, out var diagnostic) == null)
            {
                message = diagnostic ?? $"规则「{rule.Name}」匹配内容不可用";
                return false;
            }
        }
        message = "";
        return true;
    }
}
