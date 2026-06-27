using LearningAssistant.Common;
using LearningAssistant.Common.Themes;

namespace LearningAssistant.Forms.UserControls
{
    public class AIHistoryPanel : Panel, IThemeable
    {
        #region 控件字段
        private readonly ListBox _historyList;
        private readonly Button _clearButton;
        private readonly Label _titleLabel;
        private readonly List<AIHistoryItem> _historyItems = new();
        #endregion

        #region 状态字段
        private ThemeMode _currentTheme = ThemeMode.Light;
        private int _hoveredIndex = -1;
        private Color _clearButtonOriginalColor = Color.FromArgb(244, 67, 54);
        #endregion

        #region 全局复用字体（Dispose统一销毁，避免GDI泄漏）
        private readonly Font _fontTitle = new Font("微软雅黑", 12F, FontStyle.Bold);
        private readonly Font _fontListMain = new Font("微软雅黑", 10F);
        private readonly Font _fontListTime = new Font("微软雅黑", 8F);
        private readonly Font _fontBtnClear = new Font("微软雅黑", 9F);
        #endregion

        public event EventHandler<AIHistoryEventArgs>? HistoryItemSelected;

        public AIHistoryPanel()
        {
            // 基础面板样式
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Padding = new Padding(10);

            // 1. 创建所有子控件
            this._titleLabel = new Label();
            this._historyList = new ListBox();
            this._clearButton = new Button();

            // 2. 标题标签配置
            this._titleLabel.Text = "🤖 AI问答历史";
            this._titleLabel.Font = _fontTitle;
            this._titleLabel.ForeColor = Color.FromArgb(33, 33, 33);
            this._titleLabel.Dock = DockStyle.Top;
            this._titleLabel.Padding = new Padding(5, 5, 5, 10);

            // 3. 历史列表配置
            this._historyList.Dock = DockStyle.Fill;
            this._historyList.Font = _fontListMain;
            this._historyList.ForeColor = Color.FromArgb(66, 66, 66);
            this._historyList.BackColor = Color.FromArgb(248, 248, 248);
            this._historyList.BorderStyle = BorderStyle.None;
            this._historyList.ItemHeight = 44;
            this._historyList.DrawMode = DrawMode.OwnerDrawFixed;
            this._historyList.Cursor = Cursors.Hand;
            this._historyList.DrawItem += HistoryList_DrawItem;
            this._historyList.SelectedIndexChanged += HistoryList_SelectedIndexChanged;
            this._historyList.MouseMove += HistoryList_MouseMove;
            this._historyList.MouseLeave += HistoryList_MouseLeave;

            // 4. 清空按钮配置
            this._clearButton.Text = "清空历史";
            this._clearButton.Font = _fontBtnClear;
            this._clearButton.BackColor = Color.FromArgb(244, 67, 54);
            this._clearButton.ForeColor = Color.White;
            this._clearButton.FlatStyle = FlatStyle.Flat;
            this._clearButton.FlatAppearance.BorderSize = 0;
            this._clearButton.Dock = DockStyle.Bottom;
            this._clearButton.Height = 32;
            this._clearButton.Cursor = Cursors.Hand;
            this._clearButton.Click += ClearButton_Click;
            this._clearButton.MouseEnter += ClearButton_MouseEnter;
            this._clearButton.MouseLeave += ClearButton_MouseLeave;

            // 5. 按Dock层级倒序添加（Bottom -> Fill -> Top）
            this.Controls.Add(this._clearButton);
            this.Controls.Add(this._historyList);
            this.Controls.Add(this._titleLabel);
        }

        #region IThemeable 实现
        public void ApplyTheme(ThemeColors colors)
        {
            _currentTheme = colors.ThemeMode;
            bool isDark = colors.ThemeMode == ThemeMode.Dark;

            this.BackColor = isDark ? colors.Surface : Color.White;
            _titleLabel.ForeColor = isDark ? colors.TextPrimary : Color.FromArgb(33, 33, 33);
            _historyList.BackColor = isDark ? colors.SurfaceElevated : Color.FromArgb(248, 248, 248);
            _historyList.ForeColor = isDark ? colors.TextPrimary : Color.FromArgb(66, 66, 66);

            _clearButtonOriginalColor = isDark
                ? Color.FromArgb(200, 60, 60)
                : Color.FromArgb(244, 67, 54);
            _clearButton.BackColor = _clearButtonOriginalColor;

            _historyList.Invalidate();
        }
        #endregion

        /// <summary>添加历史记录，最多保留20条</summary>
        public void AddHistoryItem(string question, string answer)
        {
            var item = new AIHistoryItem
            {
                Question = question,
                Answer = answer,
                Timestamp = DateTime.Now
            };

            _historyItems.Insert(0, item);
            _historyList.Items.Insert(0, item);

            if (_historyItems.Count > 20)
            {
                _historyItems.RemoveAt(_historyItems.Count - 1);
                _historyList.Items.RemoveAt(_historyList.Items.Count - 1);
            }
        }

        /// <summary>列表自定义绘制（复用全局字体，不再临时new Font）</summary>
        private void HistoryList_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not ListBox listBox || e.Index < 0)
                return;

            var item = listBox.Items[e.Index] as AIHistoryItem;
            if (item == null)
                return;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool isHovered = e.Index == _hoveredIndex;
            bool isDark = _currentTheme == ThemeMode.Dark;

            Color backColor;
            Color textColor;
            Color subTextColor;
            Color borderColor;

            if (isSelected)
            {
                backColor = isDark
                    ? Color.FromArgb(70, 130, 200)
                    : Color.FromArgb(33, 150, 243);
                textColor = Color.White;
                subTextColor = Color.FromArgb(220, 220, 220);
                borderColor = Color.Transparent;
            }
            else if (isHovered)
            {
                backColor = isDark
                    ? Color.FromArgb(55, 55, 60)
                    : Color.FromArgb(240, 240, 245);
                textColor = isDark ? Color.FromArgb(250, 250, 250) : Color.FromArgb(33, 33, 33);
                subTextColor = isDark ? Color.FromArgb(160, 160, 160) : Color.FromArgb(120, 120, 120);
                borderColor = isDark ? Color.FromArgb(70, 70, 80) : Color.FromArgb(220, 220, 230);
            }
            else
            {
                backColor = isDark
                    ? Color.FromArgb(40, 40, 45)
                    : Color.FromArgb(248, 248, 248);
                textColor = isDark ? Color.FromArgb(230, 230, 230) : Color.FromArgb(66, 66, 66);
                subTextColor = isDark ? Color.FromArgb(140, 140, 140) : Color.Gray;
                borderColor = Color.Transparent;
            }

            using (var backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            if (borderColor != Color.Transparent)
            {
                using var pen = new Pen(borderColor, 1);
                var rect = new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }

            string questionPreview = item.Question.Length > 30
                ? item.Question.Substring(0, 30) + "..."
                : item.Question;
            string timeStr = item.Timestamp.ToString("MM-dd HH:mm");

            using var textBrush = new SolidBrush(textColor);
            using var subTextBrush = new SolidBrush(subTextColor);

            e.Graphics.DrawString("💬", _fontListMain, textBrush, e.Bounds.X + 10, e.Bounds.Y + 10);
            e.Graphics.DrawString(questionPreview, _fontListMain, textBrush, e.Bounds.X + 35, e.Bounds.Y + 8);
            e.Graphics.DrawString($"⏰ {timeStr}", _fontListTime, subTextBrush, e.Bounds.X + 35, e.Bounds.Y + 28);

            e.DrawFocusRectangle();
        }

        private void HistoryList_MouseMove(object? sender, MouseEventArgs e)
        {
            int hoverIndex = _historyList.IndexFromPoint(e.Location);
            if (hoverIndex != _hoveredIndex && hoverIndex >= 0)
            {
                _hoveredIndex = hoverIndex;
                _historyList.Invalidate();
            }
        }

        private void HistoryList_MouseLeave(object? sender, EventArgs e)
        {
            if (_hoveredIndex >= 0)
            {
                _hoveredIndex = -1;
                _historyList.Invalidate();
            }
        }

        private void HistoryList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_historyList.SelectedItem is AIHistoryItem item)
            {
                HistoryItemSelected?.Invoke(this, new AIHistoryEventArgs(item));
            }
        }

        private void ClearButton_Click(object? sender, EventArgs e)
        {
            _historyItems.Clear();
            _historyList.Items.Clear();
        }

        private void ClearButton_MouseEnter(object? sender, EventArgs e)
        {
            _clearButton.BackColor = ThemeHelper.GetHoverColor(_clearButtonOriginalColor, -25);
        }

        private void ClearButton_MouseLeave(object? sender, EventArgs e)
        {
            _clearButton.BackColor = _clearButtonOriginalColor;
        }

        #region 释放字体资源，防止GDI句柄泄漏
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fontTitle?.Dispose();
                _fontListMain?.Dispose();
                _fontListTime?.Dispose();
                _fontBtnClear?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }

    public class AIHistoryItem
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class AIHistoryEventArgs : EventArgs
    {
        public AIHistoryItem Item { get; }

        public AIHistoryEventArgs(AIHistoryItem item)
        {
            Item = item;
        }
    }
}