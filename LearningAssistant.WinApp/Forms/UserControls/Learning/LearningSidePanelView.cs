using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.Practice;
using LearningAssistant.Services.Learning;

namespace LearningAssistant.Forms.UserControls.Learning
{
    /// <summary>
    /// 学习窗口右侧可折叠面板（P3）：显示来源与原文，并提供跟读入口。
    /// 默认折叠，不干扰中央卡片与“认识/不会”主操作。
    /// </summary>
    public sealed class LearningSidePanelView : UserControl, IThemeable
    {
        private readonly Panel _headerPanel = new();
        private readonly Label _labelHeader = new();
        private readonly Button _buttonCollapse = new();
        private readonly Label _labelSourceType = new();
        private readonly Label _labelPage = new();
        private readonly Label _labelExcerpt = new();
        private readonly Button _buttonJump = new();
        private readonly Label _labelPracticeHeader = new();
        private readonly Button _buttonShadowing = new();
        private readonly Label _labelPracticeStats = new();

        private string? _sourcePdfPath;
        private int _sourcePageIndex = -1;

        public LearningSidePanelView()
        {
            BuildLayout();
        }

        /// <summary>请求收起面板。</summary>
        public event EventHandler? CollapseRequested;

        /// <summary>请求回跳到 PDF 原文页。</summary>
        public event EventHandler<SourceJumpEventArgs>? JumpToSourceRequested;

        /// <summary>请求开始单词跟读。</summary>
        public event EventHandler? ShadowingRequested;

        private void BuildLayout()
        {
            BackColor = Color.White;
            Padding = new Padding(14, 12, 14, 12);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 10
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));  // 0 header
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));  // 1 来源类型
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));  // 2 页码
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));  // 3 “原文”标题
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // 4 原文
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // 5 回跳按钮
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 1F));   // 6 分隔线
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));  // 7 练习标题
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));  // 8 跟读
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));  // 9 最近练习

            BuildHeader();

            _labelSourceType.Dock = DockStyle.Fill;
            _labelSourceType.Font = new Font("微软雅黑", 9F);
            _labelSourceType.ForeColor = Color.FromArgb(100, 116, 139);
            _labelSourceType.TextAlign = ContentAlignment.MiddleLeft;
            _labelSourceType.Text = "来源：—";

            _labelPage.Dock = DockStyle.Fill;
            _labelPage.Font = new Font("微软雅黑", 9F);
            _labelPage.ForeColor = Color.FromArgb(100, 116, 139);
            _labelPage.TextAlign = ContentAlignment.MiddleLeft;
            _labelPage.Text = "位置：—";

            var excerptTitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = "原文",
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _labelExcerpt.Dock = DockStyle.Fill;
            _labelExcerpt.Font = new Font("微软雅黑", 9.5F);
            _labelExcerpt.ForeColor = Color.FromArgb(15, 23, 42);
            _labelExcerpt.TextAlign = ContentAlignment.TopLeft;
            _labelExcerpt.AutoEllipsis = true;
            _labelExcerpt.Padding = new Padding(0, 4, 0, 4);

            _buttonJump.Text = "↩ 回到原文页";
            _buttonJump.Dock = DockStyle.Fill;
            _buttonJump.FlatStyle = FlatStyle.Flat;
            _buttonJump.FlatAppearance.BorderSize = 1;
            _buttonJump.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _buttonJump.BackColor = Color.White;
            _buttonJump.ForeColor = Color.FromArgb(71, 85, 105);
            _buttonJump.Font = new Font("微软雅黑", 9F);
            _buttonJump.Margin = new Padding(0, 6, 0, 0);
            _buttonJump.Cursor = Cursors.Hand;
            _buttonJump.Enabled = false;
            _buttonJump.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(_sourcePdfPath) || _sourcePageIndex < 0) return;
                JumpToSourceRequested?.Invoke(this, new SourceJumpEventArgs(_sourcePdfPath, _sourcePageIndex));
            };

            var divider = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(226, 232, 240) };

            _labelPracticeHeader.Dock = DockStyle.Fill;
            _labelPracticeHeader.Text = "🎤 练习";
            _labelPracticeHeader.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _labelPracticeHeader.ForeColor = Color.FromArgb(71, 85, 105);
            _labelPracticeHeader.TextAlign = ContentAlignment.MiddleLeft;

            ConfigurePracticeButton(_buttonShadowing, "🗣 单词跟读", Color.FromArgb(139, 92, 246));
            _buttonShadowing.Click += (_, _) => ShadowingRequested?.Invoke(this, EventArgs.Empty);

            _labelPracticeStats.Dock = DockStyle.Fill;
            _labelPracticeStats.Font = new Font("微软雅黑", 8.5F);
            _labelPracticeStats.ForeColor = Color.FromArgb(100, 116, 139);
            _labelPracticeStats.TextAlign = ContentAlignment.MiddleLeft;
            _labelPracticeStats.Text = "最近练习：—";

            root.Controls.Add(_headerPanel, 0, 0);
            root.Controls.Add(_labelSourceType, 0, 1);
            root.Controls.Add(_labelPage, 0, 2);
            root.Controls.Add(excerptTitle, 0, 3);
            root.Controls.Add(_labelExcerpt, 0, 4);
            root.Controls.Add(_buttonJump, 0, 5);
            root.Controls.Add(divider, 0, 6);
            root.Controls.Add(_labelPracticeHeader, 0, 7);
            root.Controls.Add(_buttonShadowing, 0, 8);
            root.Controls.Add(_labelPracticeStats, 0, 9);

            Controls.Add(root);
        }

        private void BuildHeader()
        {
            _headerPanel.Dock = DockStyle.Fill;

            _labelHeader.Dock = DockStyle.Fill;
            _labelHeader.Text = "📄 来源与原文";
            _labelHeader.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _labelHeader.ForeColor = Color.FromArgb(15, 23, 42);
            _labelHeader.TextAlign = ContentAlignment.MiddleLeft;

            _buttonCollapse.Text = "✕";
            _buttonCollapse.Dock = DockStyle.Right;
            _buttonCollapse.Width = 28;
            _buttonCollapse.FlatStyle = FlatStyle.Flat;
            _buttonCollapse.FlatAppearance.BorderSize = 0;
            _buttonCollapse.BackColor = Color.Transparent;
            _buttonCollapse.ForeColor = Color.FromArgb(100, 116, 139);
            _buttonCollapse.Font = new Font("微软雅黑", 10F);
            _buttonCollapse.Cursor = Cursors.Hand;
            _buttonCollapse.Click += (_, _) => CollapseRequested?.Invoke(this, EventArgs.Empty);

            _headerPanel.Controls.Add(_labelHeader);
            _headerPanel.Controls.Add(_buttonCollapse);
        }

        private static void ConfigurePracticeButton(Button button, string text, Color backColor)
        {
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = backColor;
            button.ForeColor = Color.White;
            button.Font = new Font("微软雅黑", 9.5F, FontStyle.Bold);
            button.Margin = new Padding(0, 2, 0, 6);
            button.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// 根据当前学习项刷新来源信息与练习入口可用状态。
        /// </summary>
        public void UpdateItem(LearningItem? item)
        {
            if (item == null)
            {
                _labelSourceType.Text = "来源：—";
                _labelPage.Text = "位置：—";
                _labelExcerpt.Text = "未选择学习项";
                SetJumpTarget(null, -1);
                _buttonShadowing.Enabled = false;
                UpdatePracticeStats(null);
                return;
            }

            var effectiveSource = LearningItemClassification.GetEffectiveSourceType(item);
            _labelSourceType.Text = $"来源：{effectiveSource.ToDisplayString()}";

            var sourceRef = item.SourceRef;
            if (sourceRef?.HasPdfAnchor == true)
            {
                _labelPage.Text = $"位置：第 {sourceRef.PageIndex + 1} 页";
                SetJumpTarget(sourceRef.PdfPath, sourceRef.PageIndex);
            }
            else
            {
                _labelPage.Text = "位置：词库 / 手动录入";
                SetJumpTarget(null, -1);
            }

            var excerpt = item.GetDisplayText();
            _labelExcerpt.Text = string.IsNullOrWhiteSpace(excerpt)
                ? item.GetMainContent()
                : excerpt;

            var hasWord = !string.IsNullOrWhiteSpace(item.GetMainContent());
            _buttonShadowing.Enabled = hasWord;
        }

        /// <summary>
        /// 显示当前学习项最近一次练习结果（P4 数据闭环）。
        /// </summary>
        public void UpdatePracticeStats(PracticeRecord? latest)
        {
            if (latest == null)
            {
                _labelPracticeStats.Text = "最近练习：—";
                return;
            }

            var scoreText = latest.MaxScore > 0
                ? $"{latest.Score}/{latest.MaxScore} 分"
                : $"{latest.Score} 分";
            _labelPracticeStats.Text = $"最近练习：{latest.CreatedAt:MM-dd HH:mm} · {scoreText}";
        }

        private void SetJumpTarget(string? pdfPath, int pageIndex)
        {
            _sourcePdfPath = pdfPath;
            _sourcePageIndex = pageIndex;
            _buttonJump.Enabled = !string.IsNullOrWhiteSpace(pdfPath)
                && pageIndex >= 0
                && File.Exists(pdfPath);
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Surface;
            _headerPanel.BackColor = colors.Surface;
            _labelHeader.ForeColor = colors.TextPrimary;
            _buttonCollapse.ForeColor = colors.TextSecondary;
            _labelSourceType.ForeColor = colors.TextSecondary;
            _labelPage.ForeColor = colors.TextSecondary;
            _labelExcerpt.ForeColor = colors.TextPrimary;
            _labelPracticeHeader.ForeColor = colors.TextPrimary;
            _labelPracticeStats.ForeColor = colors.TextSecondary;

            _buttonJump.BackColor = colors.Surface;
            _buttonJump.ForeColor = colors.TextSecondary;
            _buttonJump.FlatAppearance.BorderColor = colors.Divider;
        }
    }

    /// <summary>回跳 PDF 原文页请求参数。</summary>
    public sealed class SourceJumpEventArgs : EventArgs
    {
        public SourceJumpEventArgs(string pdfPath, int pageIndex)
        {
            PdfPath = pdfPath;
            PageIndex = pageIndex;
        }

        public string PdfPath { get; }
        public int PageIndex { get; }
    }
}
