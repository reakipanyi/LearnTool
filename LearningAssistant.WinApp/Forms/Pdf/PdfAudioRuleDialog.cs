using LearningAssistant.Managers;
using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Forms.Pdf;

public class PdfAudioRuleDialog : Form
{
    private readonly PdfAudioProfile _profile;
    private readonly List<AudioMatchRule> _rules;

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

    private readonly List<AudioPageMapMode> _modes = new()
    { AudioPageMapMode.PageIndex, AudioPageMapMode.Sequence, AudioPageMapMode.Range, AudioPageMapMode.Custom };

    public PdfAudioRuleDialog(PdfAudioProfile profile, IPdfReaderFormAccess form)
    {
        _profile = profile;
        _rules = _profile.Rules;
        Text = "音频匹配规则";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(760, 460);
        BuildLayout();
        foreach (var m in _modes) _cmbMode.Items.Add(m.ToString());
        foreach (var r in _rules) AddListItem(r);
        if (_lvRules.Items.Count > 0) _lvRules.Items[0].Selected = true;
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
        var btnOk = new Button { Text = "保存并关闭", Width = 100, DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "取消", Width = 70, DialogResult = DialogResult.Cancel };

        btnAdd.Click += (_, _) => OnAdd();
        btnDelete.Click += (_, _) => OnDelete();
        btnApply.Click += (_, _) => ApplyToSelected();
        btnOk.Click += (_, _) => { ApplyToSelected(); };

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8) };
        bottom.Controls.AddRange(new Control[] { btnAdd, btnDelete, btnApply, btnOk, btnCancel });

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

        Controls.Add(_lvRules);
        Controls.Add(editPanel);
        Controls.Add(bottom);

        _lvRules.SelectedIndexChanged += (_, _) => PopulateFields();
    }

    private static void AddField(TableLayoutPanel panel, ref int row, string label, Control control)
    {
        panel.RowCount = row + 1;
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 8, 6) }, 0, row);
        control.Margin = new Padding(0, 4, 0, 4);
        panel.Controls.Add(control, 1, row);
        row++;
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
        var rule = new AudioMatchRule { Name = "新规则", FileNamePattern = @"(\d+)\.mp3" };
        _rules.Add(rule);
        AddListItem(rule);
        _lvRules.Items[_lvRules.Items.Count - 1].Selected = true;
    }

    private void OnDelete()
    {
        if (_lvRules.SelectedItems.Count == 0) return;
        var item = _lvRules.SelectedItems[0];
        if (item.Tag is AudioMatchRule r) _rules.Remove(r);
        _lvRules.Items.Remove(item);
    }

    private void PopulateFields()
    {
        if (_lvRules.SelectedItems.Count == 0) return;
        if (_lvRules.SelectedItems[0].Tag is not AudioMatchRule r) return;
        _txtName.Text = r.Name;
        _txtPattern.Text = r.FileNamePattern;
        _cmbMode.SelectedIndex = Math.Max(0, _modes.IndexOf(r.MapMode));
        _txtCapture.Text = r.CaptureGroupIndex?.ToString() ?? "";
        _numOffset.Value = Math.Clamp(r.PageOffset, -1000m, 10000m);
        _numPagesPerUnit.Value = Math.Clamp(r.PagesPerUnit, 1m, 1000m);
        _numPriority.Value = Math.Clamp(r.Priority, 0m, 10000m);
        _chkEnabled.Checked = r.Enabled;
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
    }

    private void RefreshListItem(ListViewItem item, AudioMatchRule r)
    {
        item.Text = r.Name;
        item.SubItems[1].Text = r.MapMode.ToString();
        item.SubItems[2].Text = r.Priority.ToString();
        item.SubItems[3].Text = r.Enabled ? "是" : "否";
        item.SubItems[4].Text = r.FileNamePattern;
    }
}