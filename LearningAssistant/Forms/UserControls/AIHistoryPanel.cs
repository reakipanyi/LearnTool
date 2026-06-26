namespace LearningAssistant.Forms.UserControls
{
    public class AIHistoryPanel : Panel
    {
        #region 控件字段
        private readonly ListBox _historyList;
        private readonly Button _clearButton;
        private readonly Label _titleLabel;
        private readonly List<AIHistoryItem> _historyItems = new();
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
            this._historyList.ItemHeight = 40;
            this._historyList.DrawMode = DrawMode.OwnerDrawFixed;
            this._historyList.DrawItem += HistoryList_DrawItem;
            this._historyList.SelectedIndexChanged += HistoryList_SelectedIndexChanged;

            // 4. 清空按钮配置
            this._clearButton.Text = "清空历史";
            this._clearButton.Font = _fontBtnClear;
            this._clearButton.BackColor = Color.FromArgb(244, 67, 54);
            this._clearButton.ForeColor = Color.White;
            this._clearButton.FlatStyle = FlatStyle.Flat;
            this._clearButton.Dock = DockStyle.Bottom;
            this._clearButton.Height = 30;
            this._clearButton.Click += ClearButton_Click;

            // 5. 按Dock层级倒序添加（Bottom -> Fill -> Top）
            this.Controls.Add(this._clearButton);
            this.Controls.Add(this._historyList);
            this.Controls.Add(this._titleLabel);
        }

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

            e.DrawBackground();
            var item = listBox.Items[e.Index] as AIHistoryItem;
            if (item == null)
                return;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Brush textBrush = isSelected ? Brushes.White : Brushes.Black;
            Brush subTextBrush = isSelected ? Brushes.LightGray : Brushes.Gray;

            string questionPreview = item.Question.Length > 30
                ? item.Question.Substring(0, 30) + "..."
                : item.Question;
            string timeStr = item.Timestamp.ToShortTimeString();

            e.Graphics.DrawString(questionPreview, _fontListMain, textBrush, e.Bounds.X + 10, e.Bounds.Y + 5);
            e.Graphics.DrawString(timeStr, _fontListTime, subTextBrush, e.Bounds.X + 10, e.Bounds.Y + 25);

            if (isSelected)
            {
                using Pen pen = new Pen(Color.White, 2);
                e.Graphics.DrawRectangle(pen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
            }

            e.DrawFocusRectangle();
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