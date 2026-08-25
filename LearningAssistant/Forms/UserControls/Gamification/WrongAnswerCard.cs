using LearningAssistant.Common;
using LearningAssistant.Common.UI;
using LearningAssistant.Models.Learning;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls.Gamification
{
    /// <summary>
    /// 错题卡片控件
    /// </summary>
    public class WrongAnswerCard : UserControl
    {
        private WrongAnswerItem? _wrongAnswer;
        private bool _isHovered;
        private bool _isSelected;
        private int _borderRadius = 10;
        private readonly CheckBox _selectCheckBox;

        [Category("数据")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public WrongAnswerItem? WrongAnswer
        {
            get => _wrongAnswer;
            set
            {
                _wrongAnswer = value;
                Invalidate();
            }
        }

        [Category("外观")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                _selectCheckBox.Checked = value;
                Invalidate();
            }
        }

        [Category("外观")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowCheckBox { get; set; } = false;

        [Category("外观")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int BorderRadius
        {
            get => _borderRadius;
            set
            {
                _borderRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        public event EventHandler? CardClicked;
        public event EventHandler<bool>? CheckedChanged;

        public WrongAnswerCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint
                   | ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Size = new Size(320, 130);
            Cursor = Cursors.Hand;

            _selectCheckBox = new CheckBox
            {
                Visible = false,
                Location = new Point(12, 12),
                AutoSize = true
            };
            _selectCheckBox.CheckedChanged += (s, e) =>
            {
                _isSelected = _selectCheckBox.Checked;
                CheckedChanged?.Invoke(this, _selectCheckBox.Checked);
                Invalidate();
            };
            Controls.Add(_selectCheckBox);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            var rect = ClientRectangle;
            var padding = 3;
            var contentRect = new Rectangle(
                rect.X + padding + (_isHovered ? 1 : 2),
                rect.Y + padding + (_isHovered ? 1 : 2),
                rect.Width - padding * 2 - (_isHovered ? 2 : 4),
                rect.Height - padding * 2 - (_isHovered ? 2 : 4)
            );

            if (_isHovered || _isSelected)
            {
                using var shadowPath = GdiHelper.CreateRoundedRectPath(
                    new Rectangle(contentRect.X + 1, contentRect.Y + 2, contentRect.Width - 2, contentRect.Height - 2),
                    _borderRadius);
                using var shadowBrush = new SolidBrush(Color.FromArgb(_isSelected ? 50 : 25, 0, 0, 0));
                g.FillPath(shadowBrush, shadowPath);
            }

            using var cardPath = GdiHelper.CreateRoundedRectPath(contentRect, _borderRadius);
            using var bgBrush = new SolidBrush(_isSelected ? Color.FromArgb(255, 243, 224) : Color.White);
            g.FillPath(bgBrush, cardPath);

            if (_isSelected)
            {
                using var selectedPen = new Pen(Color.FromArgb(255, 152, 0), 2);
                g.DrawPath(selectedPen, cardPath);
            }
            else
            {
                using var borderPen = new Pen(Color.FromArgb(30, 0, 0, 0), 1);
                g.DrawPath(borderPen, cardPath);
            }

            if (_wrongAnswer == null) return;

            var leftPadding = ShowCheckBox ? 35 : 15;

            var masteryColor = GetMasteryColor(_wrongAnswer.Mastery);
            var masteryBadgeRect = new Rectangle(contentRect.X + leftPadding, contentRect.Y + 12, 65, 22);
            using var masteryPath = GdiHelper.CreateRoundedRectPath(masteryBadgeRect, 6);
            using var masteryBrush = new SolidBrush(masteryColor.bg);
            g.FillPath(masteryBrush, masteryPath);
            TextRenderer.DrawText(g, $"{_wrongAnswer.MasteryIcon} {_wrongAnswer.MasteryText}",
                new Font("微软雅黑", 8F, FontStyle.Bold),
                masteryBadgeRect,
                masteryColor.text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            var wrongCountText = $"错 {_wrongAnswer.WrongCount} 次";
            using var wrongFont = new Font("微软雅黑", 8F, FontStyle.Bold);
            var wrongSize = g.MeasureString(wrongCountText, wrongFont);
            var wrongRect = new RectangleF(
                masteryBadgeRect.Right + 10,
                masteryBadgeRect.Y + 2,
                wrongSize.Width,
                wrongSize.Height);
            g.DrawString(wrongCountText, wrongFont,
                new SolidBrush(Color.FromArgb(239, 83, 80)),
                wrongRect);

            var subjectText = _wrongAnswer.Subject == SubjectType.Unknown
                ? "未分类"
                : _wrongAnswer.Subject.ToString();
            using var subjectFont = new Font("微软雅黑", 8F);
            var subjectSize = g.MeasureString(subjectText, subjectFont);
            g.DrawString(subjectText, subjectFont,
                new SolidBrush(Color.FromArgb(120, 120, 120)),
                contentRect.Right - subjectSize.Width - 15,
                masteryBadgeRect.Y + 3);

            var titleY = masteryBadgeRect.Bottom + 8;
            var titleRect = new RectangleF(
                contentRect.X + leftPadding,
                titleY,
                contentRect.Width - leftPadding - 15,
                36
            );
            using var titleFont = new Font("微软雅黑", 9.5F, FontStyle.Bold);
            TextRenderer.DrawText(g, _wrongAnswer.DisplayTitle, titleFont,
                Rectangle.Round(titleRect),
                Color.FromArgb(33, 33, 33),
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);

            var answerY = titleRect.Bottom + 6;
            var answerRect = new RectangleF(
                contentRect.X + leftPadding,
                answerY,
                contentRect.Width - leftPadding - 15,
                22
            );
            var answerText = _wrongAnswer.CorrectAnswer.Length > 60
                ? _wrongAnswer.CorrectAnswer.Substring(0, 60) + "..."
                : _wrongAnswer.CorrectAnswer;
            using var answerFont = new Font("微软雅黑", 8.5F);
            TextRenderer.DrawText(g, $"答案: {answerText}", answerFont,
                Rectangle.Round(answerRect),
                Color.FromArgb(76, 175, 80),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            var bottomY = contentRect.Bottom - 22;

            if (_wrongAnswer.Category != SubCategoryType.Unknown)
            {
                var categoryRect = new Rectangle(contentRect.X + leftPadding, bottomY, 70, 18);
                using var categoryPath = GdiHelper.CreateRoundedRectPath(categoryRect, 4);
                using var categoryBrush = new SolidBrush(Color.FromArgb(227, 242, 253));
                g.FillPath(categoryBrush, categoryPath);
                TextRenderer.DrawText(g, $"📂 {_wrongAnswer.Category.ToString()}",
                    new Font("微软雅黑", 7.5F),
                    categoryRect,
                    Color.FromArgb(33, 150, 243),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            if (_wrongAnswer.TagsList.Count > 0)
            {
                var tagText = string.Join(" · ", _wrongAnswer.TagsList.Take(2));
                if (_wrongAnswer.TagsList.Count > 2) tagText += " ...";
                using var tagFont = new Font("微软雅黑", 7.5F);
                var tagSize = g.MeasureString(tagText, tagFont);
                g.DrawString(tagText, tagFont,
                    new SolidBrush(Color.FromArgb(120, 120, 120)),
                    contentRect.Right - tagSize.Width - 15,
                    bottomY + 2);
            }

            var dateText = _wrongAnswer.AddedAt.ToString("MM-dd");
            using var dateFont = new Font("微软雅黑", 7.5F);
            var dateSize = g.MeasureString(dateText, dateFont);
            g.DrawString(dateText, dateFont,
                new SolidBrush(Color.FromArgb(160, 160, 160)),
                contentRect.Right - dateSize.Width - 15,
                contentRect.Y + 15);

            _selectCheckBox.Visible = ShowCheckBox;
        }

        private static (Color bg, Color text) GetMasteryColor(MasteryLevel mastery) => mastery switch
        {
            MasteryLevel.NotMastered => (bg: Color.FromArgb(255, 235, 235), text: Color.FromArgb(211, 47, 47)),
            MasteryLevel.Fuzzy => (bg: Color.FromArgb(255, 248, 224), text: Color.FromArgb(245, 124, 0)),
            MasteryLevel.Mastered => (bg: Color.FromArgb(232, 245, 233), text: Color.FromArgb(56, 142, 60)),
            _ => (bg: Color.FromArgb(245, 245, 245), text: Color.FromArgb(100, 100, 100))
        };

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == MouseButtons.Left && !ShowCheckBox)
            {
                CardClicked?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }
    }
}
