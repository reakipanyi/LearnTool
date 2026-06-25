using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LearningAssistant.Forms.UserControls
{
    public class AIHistoryPanel : Panel
    {
        private readonly ListBox _historyList;
        private readonly Button _clearButton;
        private readonly Label _titleLabel;
        private readonly List<AIHistoryItem> _historyItems = new();

        public AIHistoryPanel()
        {
            BackColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;
            Padding = new Padding(10);

            _titleLabel = new Label
            {
                Text = "🤖 AI问答历史",
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Dock = DockStyle.Top,
                Padding = new Padding(5, 5, 5, 10)
            };

            _historyList = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 10F),
                ForeColor = Color.FromArgb(66, 66, 66),
                BackColor = Color.FromArgb(248, 248, 248),
                BorderStyle = BorderStyle.None,
                ItemHeight = 40
            };
            _historyList.DrawMode = DrawMode.OwnerDrawFixed;
            _historyList.DrawItem += HistoryList_DrawItem;
            _historyList.SelectedIndexChanged += HistoryList_SelectedIndexChanged;

            _clearButton = new Button
            {
                Text = "清空历史",
                Font = new Font("微软雅黑", 9F),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance.BorderSize = 0,
                Dock = DockStyle.Bottom,
                Height = 30
            };
            _clearButton.Click += ClearButton_Click;

            Controls.Add(_clearButton);
            Controls.Add(_historyList);
            Controls.Add(_titleLabel);
        }

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

        private void HistoryList_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not ListBox listBox || e.Index < 0) return;

            e.DrawBackground();

            var item = listBox.Items[e.Index] as AIHistoryItem;
            if (item == null) return;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Brush textBrush = isSelected ? Brushes.White : Brushes.Black;
            Brush subTextBrush = isSelected ? Brushes.LightGray : Brushes.Gray;

            string questionPreview = item.Question.Length > 30 ? item.Question.Substring(0, 30) + "..." : item.Question;
            string timeStr = item.Timestamp.ToShortTimeString();

            e.Graphics.DrawString(questionPreview, new Font("微软雅黑", 10F, FontStyle.Bold), textBrush, e.Bounds.X + 10, e.Bounds.Y + 5);
            e.Graphics.DrawString(timeStr, new Font("微软雅黑", 8F), subTextBrush, e.Bounds.X + 10, e.Bounds.Y + 25);

            if (isSelected)
            {
                e.Graphics.DrawRectangle(new Pen(Color.White, 2), e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
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

        public event EventHandler<AIHistoryEventArgs>? HistoryItemSelected;
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