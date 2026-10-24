using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;

namespace LearningAssistant.Forms.Pdf;

/// <summary>
/// 书级规则补丁编辑器：禁用目录规则、追加本书规则，或完全独立。
/// 只为当前书保存增量，目录规则仍由其它书共享。
/// </summary>
public sealed class PdfAudioRulePatchDialog : Form
{
    private readonly CheckedListBox _listRules = new();
    private readonly CheckBox _chkReplaceAll = new()
    {
        Text = "完全独立：不继承目录规则，只用下面的附加规则"
    };
    private readonly DataGridView _grid = new();
    private readonly List<RuleItem> _items = new();

    public PdfAudioRulePatchDialog(PdfAudioProfile profile)
    {
        Text = $"本书规则补丁 - {Path.GetFileName(profile.PdfPath)}";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 580);
        MinimumSize = new Size(640, 480);
        ShowInTaskbar = false;
        MinimizeBox = false;

        BuildLayout();

        foreach (var rule in profile.DirectoryRules)
        {
            var item = new RuleItem(rule);
            _items.Add(item);
            _listRules.Items.Add(item, !IsDisabled(profile.RulePatch, rule.Id));
        }

        var patch = profile.RulePatch;
        _chkReplaceAll.Checked = patch?.ReplaceAll == true;
        foreach (var extra in patch?.ExtraRules ?? new List<AudioMatchRule>())
            AddRuleRow(PdfAudioRuleMerger.Clone(extra));

        UpdateRuleListEnabled();
        _chkReplaceAll.CheckedChanged += (_, _) => UpdateRuleListEnabled();
    }

    /// <summary>保存后的补丁；null 表示完全继承目录规则。</summary>
    public PdfAudioRulePatch? Patch { get; private set; }

    private static bool IsDisabled(PdfAudioRulePatch? patch, string ruleId)
        => patch != null
           && !patch.ReplaceAll
           && patch.DisabledRuleIds.Any(id => string.Equals(id, ruleId, StringComparison.OrdinalIgnoreCase));

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(14, 12, 14, 10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 66));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        root.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(680, 0),
            Text = "目录规则由本目录所有书共享。取消勾选即在本书禁用；附加规则只作用于当前书。",
            ForeColor = SystemColors.GrayText
        }, 0, 0);

        root.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "目录规则",
            Font = new Font(Font, FontStyle.Bold)
        }, 0, 1);

        _listRules.Dock = DockStyle.Fill;
        _listRules.CheckOnClick = true;
        _listRules.IntegralHeight = false;
        _listRules.Margin = new Padding(0, 2, 0, 6);
        root.Controls.Add(_listRules, 0, 2);

        _chkReplaceAll.AutoSize = true;
        _chkReplaceAll.Margin = new Padding(0, 2, 0, 6);
        root.Controls.Add(_chkReplaceAll, 0, 3);

        root.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "本书附加规则",
            Font = new Font(Font, FontStyle.Bold)
        }, 0, 4);

        BuildGrid();
        root.Controls.Add(_grid, 0, 5);
        root.Controls.Add(BuildButtons(), 0, 6);

        Controls.Add(root);
        CancelButton = FindCancelButton();
    }

    private void BuildGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BackgroundColor = SystemColors.Window;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

        AddTextColumn("名称", 16);
        AddTextColumn("文件名模式", 30);
        AddComboColumn("写法", 12, Enum.GetValues<AudioPatternMode>());
        AddComboColumn("页码映射", 13, Enum.GetValues<AudioPageMapMode>());
        AddTextColumn("捕获组", 8);
        AddTextColumn("偏移", 7);
        AddTextColumn("每单位页", 8);
        AddTextColumn("优先级", 8);
        AddCheckColumn("启用", 6);
    }

    private void AddTextColumn(string header, int weight)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            FillWeight = weight,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private void AddComboColumn(string header, int weight, Array values)
    {
        var column = new DataGridViewComboBoxColumn
        {
            HeaderText = header,
            FillWeight = weight,
            FlatStyle = FlatStyle.Flat,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            DataSource = values
        };
        _grid.Columns.Add(column);
    }

    private void AddCheckColumn(string header, int weight)
    {
        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = header,
            FillWeight = weight,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private Control BuildButtons()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };

        var btnSave = new Button
        {
            Text = "保存",
            Size = new Size(76, 30),
            Margin = new Padding(6, 0, 0, 0)
        };
        var btnCancel = new Button
        {
            Text = "取消",
            Size = new Size(76, 30),
            DialogResult = DialogResult.Cancel,
            Margin = new Padding(6, 0, 0, 0)
        };
        var btnDelete = new Button
        {
            Text = "删除选中行",
            Size = new Size(96, 30),
            Margin = new Padding(6, 0, 0, 0)
        };
        var btnAdd = new Button
        {
            Text = "添加规则",
            Size = new Size(86, 30),
            Margin = new Padding(0)
        };

        btnSave.Click += (_, _) => OnSave();
        btnAdd.Click += (_, _) => AddRuleRow(new AudioMatchRule
        {
            Name = $"附加规则 {_grid.Rows.Count + 1}",
            PatternMode = AudioPatternMode.Wildcard,
            MapMode = AudioPageMapMode.PageIndex,
            CaptureGroupIndex = 1,
            PagesPerUnit = 1
        });
        btnDelete.Click += (_, _) => DeleteSelectedRow();

        panel.Controls.Add(btnSave);
        panel.Controls.Add(btnCancel);
        panel.Controls.Add(btnDelete);
        panel.Controls.Add(btnAdd);
        return panel;
    }

    private Button? FindCancelButton()
        => Controls.OfType<TableLayoutPanel>().FirstOrDefault()?.Controls
            .OfType<FlowLayoutPanel>().FirstOrDefault()?.Controls
            .OfType<Button>().FirstOrDefault(b => b.DialogResult == DialogResult.Cancel);

    private void UpdateRuleListEnabled()
    {
        _listRules.Enabled = !_chkReplaceAll.Checked;
        if (_chkReplaceAll.Checked)
        {
            // 完全独立时不保留禁用项，避免歧义
            for (int i = 0; i < _listRules.Items.Count; i++) _listRules.SetItemChecked(i, true);
        }
    }

    private void AddRuleRow(AudioMatchRule rule)
    {
        int index = _grid.Rows.Add(
            rule.Name,
            rule.FileNamePattern,
            rule.PatternMode,
            rule.MapMode,
            rule.CaptureGroupIndex?.ToString() ?? "",
            rule.PageOffset,
            rule.PagesPerUnit,
            rule.Priority,
            rule.Enabled);
        _grid.Rows[index].Tag = rule;
    }

    private void DeleteSelectedRow()
    {
        if (_grid.CurrentRow == null) return;
        _grid.Rows.Remove(_grid.CurrentRow);
    }

    private void OnSave()
    {
        _grid.EndEdit();
        var extras = new List<AudioMatchRule>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            var baseRule = row.Tag as AudioMatchRule ?? new AudioMatchRule();
            var rule = new AudioMatchRule
            {
                Id = baseRule.Id,
                Name = CellText(row, 0),
                FileNamePattern = CellText(row, 1),
                PatternMode = EnumValue(row.Cells[2].Value, AudioPatternMode.Wildcard),
                MapMode = EnumValue(row.Cells[3].Value, AudioPageMapMode.PageIndex),
                CaptureGroupIndex = int.TryParse(CellText(row, 4), out var capture) ? capture : null,
                PageOffset = int.TryParse(CellText(row, 5), out var offset) ? offset : 0,
                PagesPerUnit = int.TryParse(CellText(row, 6), out var per) && per > 0 ? per : 1,
                Priority = int.TryParse(CellText(row, 7), out var priority) ? priority : 0,
                Enabled = row.Cells[8].Value is bool enabled ? enabled : true
            };
            if (string.IsNullOrWhiteSpace(rule.Name)) rule.Name = $"附加规则 {extras.Count + 1}";

            if (rule.Enabled)
            {
                var regex = PdfAudioPatternCompiler.TryCompile(rule, out var diagnostic);
                if (regex == null)
                {
                    MessageBox.Show(this, diagnostic, "附加规则不可用",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            extras.Add(rule);
        }

        if (_chkReplaceAll.Checked)
        {
            Patch = new PdfAudioRulePatch { ReplaceAll = true, ExtraRules = extras };
        }
        else
        {
            var disabled = new List<string>();
            for (int i = 0; i < _items.Count; i++)
            {
                if (_listRules.GetItemChecked(i)) continue;
                var id = _items[i].Rule.Id;
                if (!string.IsNullOrWhiteSpace(id)) disabled.Add(id);
            }
            Patch = disabled.Count == 0 && extras.Count == 0
                ? null
                : new PdfAudioRulePatch { DisabledRuleIds = disabled, ExtraRules = extras };
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private static string CellText(DataGridViewRow row, int index)
        => row.Cells[index].Value?.ToString()?.Trim() ?? "";

    private static T EnumValue<T>(object? value, T fallback) where T : struct, Enum
    {
        if (value is T typed) return typed;
        if (value is string text && Enum.TryParse<T>(text, ignoreCase: true, out var parsed)) return parsed;
        return fallback;
    }

    private sealed class RuleItem
    {
        public RuleItem(AudioMatchRule rule) => Rule = rule;

        public AudioMatchRule Rule { get; }

        public override string ToString() => $"{Rule.Name}  ·  {Rule.FileNamePattern}";
    }
}
