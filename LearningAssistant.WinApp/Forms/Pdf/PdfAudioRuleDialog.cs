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

    private readonly List<AudioPageMapMode> _modes = new()
    { AudioPageMapMode.PageIndex, AudioPageMapMode.Sequence, AudioPageMapMode.Range };
    private bool _populating;

    /// <summary>供 Visual Studio 设计器实例化使用。</summary>
    public PdfAudioRuleDialog()
    {
        InitializeComponent();
        _profile = new PdfAudioProfile();
        _rules = new List<AudioMatchRule>();
    }

    public PdfAudioRuleDialog(PdfAudioProfile profile, IPdfReaderFormAccess form, PdfAudioTrackManager manager)
    {
        _profile = profile;
        _manager = manager;
        // 拷贝一份规则编辑：取消时不能影响已生效的配置
        _rules = _profile.Rules.OrderByDescending(r => r.Priority).Select(CloneRule).ToList();

        InitializeComponent();

        cmbSyntax.Items.Add("正则表达式");
        cmbSyntax.Items.Add("通配符");
        cmbSyntax.SelectedIndex = 0;
        foreach (var m in _modes) cmbMode.Items.Add(m.ToString());
        foreach (var r in _rules) AddListItem(r);
        if (lvRules.Items.Count > 0) lvRules.Items[0].Selected = true;
        UpdateModeHint();
        RefreshPreview();
    }

    private void LvRules_SelectedIndexChanged(object? sender, EventArgs e)
    {
        PopulateFields();
        SchedulePreview();
    }

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
        if (!ValidateAllRules(out var message))
        {
            btnOk.DialogResult = DialogResult.None;
            MessageBox.Show(this, message, "匹配规则不可用", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        btnOk.DialogResult = DialogResult.OK;
        _profile.Rules = _rules;
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
        if (lvRules.SelectedItems.Count == 0) return;
        if (lvRules.SelectedItems[0].Tag is not AudioMatchRule r) return;
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
        if (lvRules.SelectedItems.Count == 0) return;
        if (lvRules.SelectedItems[0].Tag is not AudioMatchRule r) return;
        r.Name = txtName.Text.Trim();
        r.FileNamePattern = txtPattern.Text;
        r.PatternMode = GetSelectedPatternMode();
        r.MapMode = _modes[Math.Max(0, cmbMode.SelectedIndex)];
        r.CaptureGroupIndex = int.TryParse(txtCapture.Text.Trim(), out var g) ? g : null;
        r.PageOffset = (int)numOffset.Value;
        r.PagesPerUnit = (int)numPagesPerUnit.Value;
        r.Priority = (int)numPriority.Value;
        r.Enabled = chkEnabled.Checked;
        RefreshListItem(lvRules.SelectedItems[0], r);
        ValidatePatternField();
    }

    private void RefreshListItem(ListViewItem item, AudioMatchRule r)
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

        var result = _manager.PreviewRules(BuildPreviewRules());
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
        foreach (var track in result.Tracks)
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
