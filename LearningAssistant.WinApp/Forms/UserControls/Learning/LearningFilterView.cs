using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Services.Learning;

namespace LearningAssistant.Forms.UserControls.Learning
{
    /// <summary>
    /// 学习窗口顶部筛选条（P3）：知识点 / 适龄段 / 重要等级 / 来源。
    /// 只负责收集筛选条件并抛出事件，实际重载由学习窗口触发。
    /// </summary>
    public sealed class LearningFilterView : UserControl, IThemeable
    {
        private sealed class FilterOption
        {
            public object? Value { get; init; }
            public string Text { get; init; } = string.Empty;
            public override string ToString() => Text;
        }

        private readonly Label _labelKnowledgePoint = new();
        private readonly ComboBox _comboKnowledgePoint = new();
        private readonly Label _labelAge = new();
        private readonly ComboBox _comboAge = new();
        private readonly Label _labelImportance = new();
        private readonly ComboBox _comboImportance = new();
        private readonly Label _labelSource = new();
        private readonly ComboBox _comboSource = new();
        private readonly Button _buttonClear = new();
        private readonly Button _buttonSidePanel = new();

        private bool _suspendEvents;

        public LearningFilterView()
        {
            BuildLayout();
            PopulateStaticOptions();
            WireEvents();
        }

        /// <summary>筛选条件变化（用于重载学习队列）。</summary>
        public event EventHandler? FilterChanged;

        /// <summary>右侧“来源/练习”面板开关。</summary>
        public event EventHandler? SidePanelToggleRequested;

        public string? SelectedKnowledgePointId =>
            (_comboKnowledgePoint.SelectedItem as FilterOption)?.Value as string;

        public AgeRange? SelectedAgeRange => ToNullable<AgeRange>(_comboAge.SelectedItem);

        public ImportanceLevel? SelectedImportance => ToNullable<ImportanceLevel>(_comboImportance.SelectedItem);

        public StudyContentSource? SelectedSourceType => ToNullable<StudyContentSource>(_comboSource.SelectedItem);

        public bool HasActiveFilter =>
            SelectedKnowledgePointId != null
            || SelectedAgeRange != null
            || SelectedImportance != null
            || SelectedSourceType != null;

        private static T? ToNullable<T>(object? selected) where T : struct
        {
            return selected is FilterOption { Value: T value } ? value : null;
        }

        private void BuildLayout()
        {
            BackColor = Color.FromArgb(248, 250, 252);
            Padding = new Padding(12, 4, 12, 4);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 12,
                RowCount = 1
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            // 列：标签/控件交替，最后留一列自适应空白，把面板开关推到最右
            for (int i = 0; i < 11; i++)
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            ConfigureLabel(_labelKnowledgePoint, "知识点");
            ConfigureLabel(_labelAge, "适龄段");
            ConfigureLabel(_labelImportance, "重要等级");
            ConfigureLabel(_labelSource, "来源");

            ConfigureCombo(_comboKnowledgePoint, 190);
            ConfigureCombo(_comboAge, 96);
            ConfigureCombo(_comboImportance, 96);
            ConfigureCombo(_comboSource, 120);

            _buttonClear.Text = "清空";
            _buttonClear.AutoSize = false;
            _buttonClear.Size = new Size(64, 28);
            _buttonClear.Margin = new Padding(6, 4, 0, 4);
            _buttonClear.FlatStyle = FlatStyle.Flat;
            _buttonClear.FlatAppearance.BorderSize = 1;
            _buttonClear.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _buttonClear.BackColor = Color.White;
            _buttonClear.ForeColor = Color.FromArgb(71, 85, 105);
            _buttonClear.Font = new Font("微软雅黑", 9F);
            _buttonClear.Cursor = Cursors.Hand;

            _buttonSidePanel.Text = "📌 来源 / 练习";
            _buttonSidePanel.AutoSize = false;
            _buttonSidePanel.Size = new Size(120, 28);
            _buttonSidePanel.Margin = new Padding(6, 4, 0, 4);
            _buttonSidePanel.Anchor = AnchorStyles.Right;
            _buttonSidePanel.FlatStyle = FlatStyle.Flat;
            _buttonSidePanel.FlatAppearance.BorderSize = 1;
            _buttonSidePanel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _buttonSidePanel.BackColor = Color.White;
            _buttonSidePanel.ForeColor = Color.FromArgb(71, 85, 105);
            _buttonSidePanel.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _buttonSidePanel.Cursor = Cursors.Hand;

            layout.Controls.Add(_labelKnowledgePoint, 0, 0);
            layout.Controls.Add(_comboKnowledgePoint, 1, 0);
            layout.Controls.Add(_labelAge, 2, 0);
            layout.Controls.Add(_comboAge, 3, 0);
            layout.Controls.Add(_labelImportance, 4, 0);
            layout.Controls.Add(_comboImportance, 5, 0);
            layout.Controls.Add(_labelSource, 6, 0);
            layout.Controls.Add(_comboSource, 7, 0);
            layout.Controls.Add(_buttonClear, 8, 0);
            layout.Controls.Add(_buttonSidePanel, 11, 0);

            Controls.Add(layout);
        }

        private static void ConfigureLabel(Label label, string text)
        {
            label.Text = text;
            label.AutoSize = true;
            label.Anchor = AnchorStyles.Left;
            label.Margin = new Padding(0, 0, 4, 0);
            label.ForeColor = Color.FromArgb(100, 116, 139);
            label.Font = new Font("微软雅黑", 9F);
        }

        private static void ConfigureCombo(ComboBox combo, int width)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.FlatStyle = FlatStyle.Flat;
            combo.Font = new Font("微软雅黑", 9F);
            combo.Width = width;
            combo.Margin = new Padding(0, 3, 10, 3);
            combo.Anchor = AnchorStyles.Left;
        }

        private void PopulateStaticOptions()
        {
            _suspendEvents = true;
            try
            {
                _comboAge.Items.Add(new FilterOption { Value = null, Text = "全部" });
                foreach (AgeRange value in Enum.GetValues<AgeRange>())
                    _comboAge.Items.Add(new FilterOption { Value = value, Text = value.ToDisplayString() });
                _comboAge.SelectedIndex = 0;

                _comboImportance.Items.Add(new FilterOption { Value = null, Text = "全部" });
                foreach (ImportanceLevel value in Enum.GetValues<ImportanceLevel>())
                    _comboImportance.Items.Add(new FilterOption { Value = value, Text = value.ToDisplayString() });
                _comboImportance.SelectedIndex = 0;

                _comboSource.Items.Add(new FilterOption { Value = null, Text = "全部" });
                foreach (StudyContentSource value in Enum.GetValues<StudyContentSource>())
                    _comboSource.Items.Add(new FilterOption { Value = value, Text = value.ToDisplayString() });
                _comboSource.SelectedIndex = 0;

                _comboKnowledgePoint.Items.Add(new FilterOption { Value = null, Text = "全部知识点" });
                _comboKnowledgePoint.SelectedIndex = 0;
            }
            finally
            {
                _suspendEvents = false;
            }
        }

        private void WireEvents()
        {
            _comboKnowledgePoint.SelectedIndexChanged += Combo_SelectedIndexChanged;
            _comboAge.SelectedIndexChanged += Combo_SelectedIndexChanged;
            _comboImportance.SelectedIndexChanged += Combo_SelectedIndexChanged;
            _comboSource.SelectedIndexChanged += Combo_SelectedIndexChanged;
            _buttonClear.Click += ButtonClear_Click;
            _buttonSidePanel.Click += (_, _) => SidePanelToggleRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Combo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_suspendEvents)
                FilterChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonClear_Click(object? sender, EventArgs e)
        {
            _suspendEvents = true;
            try
            {
                _comboKnowledgePoint.SelectedIndex = 0;
                _comboAge.SelectedIndex = 0;
                _comboImportance.SelectedIndex = 0;
                _comboSource.SelectedIndex = 0;
            }
            finally
            {
                _suspendEvents = false;
            }

            FilterChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 刷新知识点下拉项（含“未分类”），保持当前选中项。
        /// </summary>
        public void RefreshKnowledgePoints(IReadOnlyList<KnowledgePointNode>? nodes)
        {
            var previousId = SelectedKnowledgePointId;

            _suspendEvents = true;
            try
            {
                _comboKnowledgePoint.Items.Clear();
                _comboKnowledgePoint.Items.Add(new FilterOption { Value = null, Text = "全部知识点" });
                _comboKnowledgePoint.Items.Add(new FilterOption
                {
                    Value = LearningTaxonomyService.UnclassifiedId,
                    Text = "未分类"
                });

                if (nodes != null)
                    AddKnowledgePointNodes(nodes, string.Empty);

                var index = 0;
                if (!string.IsNullOrEmpty(previousId))
                {
                    for (int i = 0; i < _comboKnowledgePoint.Items.Count; i++)
                    {
                        if (_comboKnowledgePoint.Items[i] is FilterOption option
                            && string.Equals(option.Value as string, previousId, StringComparison.Ordinal))
                        {
                            index = i;
                            break;
                        }
                    }
                }

                _comboKnowledgePoint.SelectedIndex = index;
            }
            finally
            {
                _suspendEvents = false;
            }
        }

        private void AddKnowledgePointNodes(IReadOnlyList<KnowledgePointNode> nodes, string indent)
        {
            foreach (var node in nodes)
            {
                _comboKnowledgePoint.Items.Add(new FilterOption
                {
                    Value = node.Id,
                    Text = $"{indent}{node.Name}"
                });

                if (node.Children.Count > 0)
                    AddKnowledgePointNodes(node.Children, indent + "　");
            }
        }

        public void SetSidePanelOpen(bool isOpen)
        {
            _buttonSidePanel.Text = isOpen ? "📌 收起面板" : "📌 来源 / 练习";
            _buttonSidePanel.BackColor = isOpen
                ? Color.FromArgb(237, 233, 254)
                : Color.White;
            _buttonSidePanel.ForeColor = isOpen
                ? Color.FromArgb(109, 40, 217)
                : Color.FromArgb(71, 85, 105);
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;
            foreach (var label in new[] { _labelKnowledgePoint, _labelAge, _labelImportance, _labelSource })
                label.ForeColor = colors.TextSecondary;

            foreach (var combo in new[] { _comboKnowledgePoint, _comboAge, _comboImportance, _comboSource })
            {
                combo.BackColor = colors.Surface;
                combo.ForeColor = colors.TextPrimary;
            }

            _buttonClear.BackColor = colors.Surface;
            _buttonClear.ForeColor = colors.TextSecondary;
            _buttonClear.FlatAppearance.BorderColor = colors.Divider;

            if (!(_buttonSidePanel.Text?.Contains("收起") ?? false))
            {
                _buttonSidePanel.BackColor = colors.Surface;
                _buttonSidePanel.ForeColor = colors.TextSecondary;
            }
            _buttonSidePanel.FlatAppearance.BorderColor = colors.Divider;
        }
    }
}
