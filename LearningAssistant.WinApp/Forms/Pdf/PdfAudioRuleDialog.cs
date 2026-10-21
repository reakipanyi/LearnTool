using LearningAssistant.Managers;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using System.Text.RegularExpressions;

namespace LearningAssistant.Forms.Pdf;

public class PdfAudioRuleDialog : Form
{
    private readonly PdfAudioProfile _profile;
    private readonly List<AudioMatchRule> _rules;
    private readonly PdfAudioTrackManager _manager;

    private readonly ListView _lvRules = new()
    {
        Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true
    };
    private readonly TextBox _txtName = new() { Dock = DockStyle.Top };
    private readonly TextBox _txtPattern = new() { Dock = DockStyle.Top };
    private readonly ComboBox _cmbMode = new() { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _txtCapture = new() { Dock = DockStyle.Top };
    private readonly NumericUpDown _numOffset = new() { Dock = DockStyle.Top, Minimum = -1000, Maximum = 10000 };
    private readonly NumericUpDown _numPagesPerUnit = new() { Dock = DockStyle.Top, Minimum = 1, Maximum = 1000, Value = 1 };
    private readonly NumericUpDown _numPriority = new() { Dock = DockStyle.Top, Minimum = 0, Maximum = 10000 };
    private readonly CheckBox _chkEnabled = new() { Dock = DockStyle.Top, Text = "启用", Checked = true };

    private readonly ListBox _lstPreview = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly Label _labelPreviewSummary = new()
    {
        Dock = DockStyle.Top, AutoSize = false, Height = 22, ForeColor = Color.FromArgb(50, 70, 90)
    };
    private readonly Label _labelDiagnostics = new()
    {
        Dock = DockStyle.Top, AutoSize = false, Height = 22, ForeColor = Color.FromArgb(220, 38, 38)
    };
    private readonly System.Windows.Forms.Timer _previewTimer = new() { Interval = 200 };

    private readonly List<AudioPageMapMode> _modes = new()
    { AudioPageMapMode.PageIndex, AudioPageMapMode.Sequence, AudioPageMapMode.Range, AudioPageMapMode.Custom };
    private bool _populating;

    public PdfAudioRuleDialog(PdfAudioProfile profile, IPdfReaderFormAccess form, PdfAudioTrackManager manager)
    {
        _profile = profile;
        _manager = manager;
        // 拷贝一份规则编辑：取消时不能影响已生效的配置
        _rules = _profile.Rules.Select(CloneRule).ToList();
        Text = "音频匹配规则";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(820, 560);
        MinimumSize = new Size(720, 480);
        BuildLayout();
        foreach (var m in _modes) _cmbMode.Items.Add(m.ToString());
        foreach (var r in _rules) AddListItem(r);
        if (_lvRules.Items.Count > 0) _lvRules.Items[0].Selected = true;
        RefreshPreview();
    }

    private void BuildLayout()
    {
        _lvRules.Columns.Add("名称", 140);
        _lvRules.Columns.Add("模式", 90);
        _lvRules.Columns.Add("优先级", 60);
        _lvRules.Columns.Add("启用", 50);
        _lvRules.Columns.Add("正则", 360);

        var btnAdd = new Button { Text = "新增", Width = 70 };
        var btnDelete = new Button { Text = "删除", Width = 70 };
        var btnApply = new Button { Text = "应用修改", Width = 90 };
        var btnSuggest = new Button { Text = "由手动匹配归纳", Width = 110 };
        var btnOk = new Button { Text = "保存并关闭", Width = 100, DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "取消", Width = 70, DialogResult = DialogResult.Cancel };

        btnAdd.Click += (_, _) => OnAdd();
        btnDelete.Click += (_, _) => OnDelete();
        btnApply.Click += (_, _) => { ApplyToSelected(); RefreshPreview(); };
        btnSuggest.Click += (_, _) => OnSuggestFromManual();
        btnOk.Click += (_, _) =>
        {
            ApplyToSelected();
            if (!ValidateAllRules(out var message))
            {
                btnOk.DialogResult = DialogResult.None;
                MessageBox.Show(this, message, "正则非法", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            btnOk.DialogResult = DialogResult.OK;
            _profile.Rules = _rules;
        };

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8) };
        bottom.Controls.AddRange(new Control[] { btnAdd, btnDelete, btnApply, btnSuggest, btnOk, btnCancel });

        var editPanel = new TableLayoutPanel { Dock = DockStyle.Right, Width = 320, ColumnCount = 2, Padding = new Padding(8) };
        editPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        editPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        int row = 0;
        AddField(editPanel, ref row, "名称", _txtName);
        AddField(editPanel, ref row, "正则", _txtPattern);
        AddField(editPanel, ref row, "模式", _cmbMode);
        AddField(editPanel, ref row, "捕获组(1起)", _txtCapture);
        AddField(editPanel, ref row, "页码偏移", _numOffset);
        AddField(editPanel, ref row, "每单元页数", _numPagesPerUnit);
        AddField(editPanel, ref row, "优先级", _numPriority);
        AddField(editPanel, ref row, "启用", _chkEnabled);

        // 左侧：上为规则列表，下为试算预览
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 260,
            FixedPanel = FixedPanel.Panel1
        };
        split.Panel1.Controls.Add(_lvRules);
        split.Panel2.Controls.Add(_lstPreview);
        split.Panel2.Controls.Add(_labelDiagnostics);
        split.Panel2.Controls.Add(_labelPreviewSummary);

        Controls.Add(split);
        Controls.Add(editPanel);
        Controls.Add(bottom);

        _lvRules.SelectedIndexChanged += (_, _) => { PopulateFields(); SchedulePreview(); };

        // 编辑项变化时延后 200ms 重算试算，避免逐字符触发
        _previewTimer.Tick += (_, _) => { _previewTimer.Stop(); RefreshPreview(); };
        _txtName.TextChanged += (_, _) => SchedulePreview();
        _txtPattern.TextChanged += (_, _) => { ValidatePatternField(); SchedulePreview(); };
        _cmbMode.SelectedIndexChanged += (_, _) => SchedulePreview();
        _txtCapture.TextChanged += (_, _) => SchedulePreview();
        _numOffset.ValueChanged += (_, _) => SchedulePreview();
        _numPagesPerUnit.ValueChanged += (_, _) => SchedulePreview();
        _numPriority.ValueChanged += (_, _) => SchedulePreview();
        _chkEnabled.CheckedChanged += (_, _) => SchedulePreview();
    }

    private static void AddField(TableLayoutPanel panel, ref int row, string label, Control control)
    {
        panel.RowCount = row + 1;
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 8, 6) }, 0, row);
        control.Margin = new Padding(0, 4, 0, 4);
        panel.Controls.Add(control, 1, row);
        row++;
    }

    private static AudioMatchRule CloneRule(AudioMatchRule r)
    {
        return new AudioMatchRule
        {
            Id = r.Id,
            Name = r.Name,
            FileNamePattern = r.FileNamePattern,
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
        _lvRules.Items.Add(item);
    }

    private void OnAdd()
    {
        var rule = new AudioMatchRule { Name = "新规则", FileNamePattern = @"(?:^|[\\/])(\d+)\.(?:mp3|wav|m4a|aac|flac|ogg|wma)$" };
        _rules.Add(rule);
        AddListItem(rule);
        _lvRules.Items[_lvRules.Items.Count - 1].Selected = true;
        RefreshPreview();
    }

    private void OnDelete()
    {
        if (_lvRules.SelectedItems.Count == 0) return;
        var item = _lvRules.SelectedItems[0];
        if (item.Tag is AudioMatchRule r) _rules.Remove(r);
        _lvRules.Items.Remove(item);
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
        _lvRules.Items[_lvRules.Items.Count - 1].Selected = true;
        RefreshPreview();

        var note = string.IsNullOrEmpty(suggestion.Note) ? "" : "\n" + suggestion.Note;
        MessageBox.Show(this,
            $"已生成规则：{suggestion.Rule.FileNamePattern}\n覆盖 {suggestion.OverrideCount} 处手动匹配。" +
            $"请确认试算结果后再点“保存并关闭”。{note}",
            "归纳完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void PopulateFields()
    {
        if (_lvRules.SelectedItems.Count == 0) return;
        if (_lvRules.SelectedItems[0].Tag is not AudioMatchRule r) return;
        _populating = true;
        try
        {
            _txtName.Text = r.Name;
            _txtPattern.Text = r.FileNamePattern;
            _cmbMode.SelectedIndex = Math.Max(0, _modes.IndexOf(r.MapMode));
            _txtCapture.Text = r.CaptureGroupIndex?.ToString() ?? "";
            _numOffset.Value = Math.Clamp(r.PageOffset, -1000m, 10000m);
            _numPagesPerUnit.Value = Math.Clamp(r.PagesPerUnit, 1m, 1000m);
            _numPriority.Value = Math.Clamp(r.Priority, 0m, 10000m);
            _chkEnabled.Checked = r.Enabled;
        }
        finally
        {
            _populating = false;
        }
        ValidatePatternField();
    }

    private void ApplyToSelected()
    {
        if (_lvRules.SelectedItems.Count == 0) return;
        if (_lvRules.SelectedItems[0].Tag is not AudioMatchRule r) return;
        r.Name = _txtName.Text.Trim();
        r.FileNamePattern = _txtPattern.Text;
        r.MapMode = _modes[Math.Max(0, _cmbMode.SelectedIndex)];
        r.CaptureGroupIndex = int.TryParse(_txtCapture.Text.Trim(), out var g) ? g : null;
        r.PageOffset = (int)_numOffset.Value;
        r.PagesPerUnit = (int)_numPagesPerUnit.Value;
        r.Priority = (int)_numPriority.Value;
        r.Enabled = _chkEnabled.Checked;
        RefreshListItem(_lvRules.SelectedItems[0], r);
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
        _previewTimer.Stop();
        _previewTimer.Start();
    }

    /// <summary>用当前编辑中的字段（未点“应用修改”）组装试算规则</summary>
    private List<AudioMatchRule> BuildPreviewRules()
    {
        var list = _rules.Select(CloneRule).ToList();
        if (_lvRules.SelectedItems.Count == 0 || _lvRules.SelectedItems[0].Tag is not AudioMatchRule selected)
            return list;
        var index = _rules.IndexOf(selected);
        if (index < 0) return list;
        list[index] = new AudioMatchRule
        {
            Id = selected.Id,
            Name = string.IsNullOrWhiteSpace(_txtName.Text) ? "未命名规则" : _txtName.Text.Trim(),
            FileNamePattern = _txtPattern.Text,
            CaptureGroupIndex = int.TryParse(_txtCapture.Text.Trim(), out var group) ? group : null,
            MapMode = _modes[Math.Max(0, _cmbMode.SelectedIndex)],
            PageOffset = (int)_numOffset.Value,
            PagesPerUnit = (int)_numPagesPerUnit.Value,
            Enabled = _chkEnabled.Checked,
            Priority = (int)_numPriority.Value
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

        _lstPreview.BeginUpdate();
        _lstPreview.Items.Clear();
        foreach (var track in result.Tracks)
        {
            var pages = pagesByTrack.TryGetValue(track.FilePath, out var list)
                ? string.Join(",", list.OrderBy(p => p).Take(6)) + (list.Count > 6 ? "…" : "")
                : "未匹配";
            _lstPreview.Items.Add($"{track.RelativePath}  →  {pages}");
        }
        _lstPreview.EndUpdate();

        var total = _manager.PageCount;
        _labelPreviewSummary.Text = total > 0
            ? $"试算：已匹配 {result.PageIndex.Count}/{total} 页，未匹配 {result.UnmatchedPages.Count} 页"
            : "试算：请先打开 PDF";
        _labelDiagnostics.Text = _manager.Diagnostics.Count > 0
            ? string.Join("；", _manager.Diagnostics)
            : "";
    }

    private bool ValidatePatternField()
    {
        var pattern = _txtPattern.Text;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            _txtPattern.BackColor = Color.MistyRose;
            return false;
        }
        try
        {
            _ = new Regex(pattern);
            _txtPattern.BackColor = SystemColors.Window;
            return true;
        }
        catch (ArgumentException)
        {
            _txtPattern.BackColor = Color.MistyRose;
            return false;
        }
    }

    private bool ValidateAllRules(out string message)
    {
        foreach (var rule in _rules.Where(r => r.Enabled))
        {
            if (string.IsNullOrWhiteSpace(rule.FileNamePattern))
            {
                message = $"规则「{rule.Name}」正则不能为空";
                return false;
            }
            try
            {
                _ = new Regex(rule.FileNamePattern);
            }
            catch (ArgumentException ex)
            {
                message = $"规则「{rule.Name}」正则非法：{ex.Message}";
                return false;
            }
        }
        message = "";
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _previewTimer.Dispose();
        base.Dispose(disposing);
    }
}
