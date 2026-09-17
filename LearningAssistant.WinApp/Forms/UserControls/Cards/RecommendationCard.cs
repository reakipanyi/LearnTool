using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls.Cards
{
    public class RecommendationCard : Panel, IThemeable
    {
        private readonly Label _iconLabel;
        private readonly Label _titleLabel;
        private readonly Label _reasonLabel;
        private readonly Label _timeLabel;
        private readonly Panel _priorityBar;
        private bool _isHovered;

        private readonly Font _fontIcon = new Font("Segoe UI Emoji", 16F);
        private readonly Font _fontTitle = new Font("微软雅黑", 11F, FontStyle.Bold);
        private readonly Font _fontReason = new Font("微软雅黑", 9F);
        private readonly Font _fontTime = new Font("微软雅黑", 8F);

        public RecommendationCard()
        {
            this.SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint, true);

            _iconLabel = new Label
            {
                Font = _fontIcon,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Left,
                Width = 40,
                BackColor = Color.Transparent
            };

            _priorityBar = new Panel
            {
                Dock = DockStyle.Right,
                Width = 4,
                BackColor = Color.FromArgb(200, 200, 205)
            };

            _titleLabel = new Label
            {
                Font = _fontTitle,
                ForeColor = Color.FromArgb(33, 33, 33),
                Dock = DockStyle.Top,
                Height = 20,
                Padding = new Padding(5, 2, 0, 0),
                UseMnemonic = false
            };

            _reasonLabel = new Label
            {
                Font = _fontReason,
                ForeColor = Color.FromArgb(100, 100, 100),
                Dock = DockStyle.Top,
                Height = 20,
                Padding = new Padding(5, 0, 0, 0),
                UseMnemonic = false
            };

            _timeLabel = new Label
            {
                Font = _fontTime,
                ForeColor = Color.FromArgb(150, 150, 150),
                Dock = DockStyle.Bottom,
                Height = 18,
                Padding = new Padding(5, 0, 0, 0),
                UseMnemonic = false
            };

            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            contentPanel.Controls.Add(_timeLabel);
            contentPanel.Controls.Add(_reasonLabel);
            contentPanel.Controls.Add(_titleLabel);

            this.Controls.Add(_priorityBar);
            this.Controls.Add(contentPanel);
            this.Controls.Add(_iconLabel);

            this.BackColor = Color.White;
            this.Cursor = Cursors.Hand;
            this.Padding = new Padding(0);

            this.MouseEnter += RecommendationCard_MouseEnter;
            this.MouseLeave += RecommendationCard_MouseLeave;
            this.Paint += RecommendationCard_Paint;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title
        {
            get => _titleLabel.Text;
            set => _titleLabel.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Reason
        {
            get => _reasonLabel.Text;
            set => _reasonLabel.Text = value;
        }

        private string _type = "general";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                UpdateIcon();
                UpdatePriorityColor();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int EstimatedMinutes
        {
            get => _estimatedMinutes;
            set
            {
                _estimatedMinutes = value;
                _timeLabel.Text = value > 0 ? $"{value}分钟" : "";
            }
        }
        private int _estimatedMinutes;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Priority
        {
            get => _priority;
            set
            {
                _priority = value;
                UpdatePriorityColor();
            }
        }
        private int _priority;

        private void UpdateIcon()
        {
            _iconLabel.Text = _type switch
            {
                "review" => "🔄",
                "weakpoint" => "⚠️",
                "path" => "🚀",
                "explore" => "🌐",
                "goal" => "🎯",
                "wronganswer" => "📕",
                "learn" => "📚",
                _ => "💡"
            };
        }

        private void UpdatePriorityColor()
        {
            _priorityBar.BackColor = _priority switch
            {
                >= 9 => Color.FromArgb(244, 67, 54),
                >= 7 => Color.FromArgb(255, 152, 0),
                >= 5 => Color.FromArgb(255, 193, 7),
                >= 3 => Color.FromArgb(76, 175, 80),
                _ => Color.FromArgb(200, 200, 205)
            };
        }

        private void RecommendationCard_MouseEnter(object? sender, EventArgs e)
        {
            _isHovered = true;
            this.Invalidate();
        }

        private void RecommendationCard_MouseLeave(object? sender, EventArgs e)
        {
            _isHovered = false;
            this.Invalidate();
        }

        private void RecommendationCard_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int radius = 8;
            var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

            using (GraphicsPath path = CreateRoundedRectPath(rect, radius))
            {
                this.Region = new Region(path);

                Color borderColor = _isHovered ? Color.FromArgb(76, 175, 80) : Color.FromArgb(230, 230, 235);
                int borderWidth = _isHovered ? 2 : 1;

                using (Pen pen = new Pen(borderColor, borderWidth))
                {
                    g.DrawPath(pen, path);
                }

                if (_isHovered)
                {
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(4, 76, 175, 80)))
                    {
                        g.FillPath(brush, path);
                    }
                }
            }
        }

        private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int r = radius;

            path.AddArc(rect.X, rect.Y, r, r, 180, 90);
            path.AddArc(rect.X + rect.Width - r, rect.Y, r, r, 270, 90);
            path.AddArc(rect.X + rect.Width - r, rect.Y + rect.Height - r, r, r, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - r, r, r, 90, 90);
            path.CloseAllFigures();

            return path;
        }

        public void ApplyTheme(ThemeColors colors)
        {
            if (_titleLabel != null)
                _titleLabel.ForeColor = colors.TextPrimary;

            if (_reasonLabel != null)
                _reasonLabel.ForeColor = colors.TextSecondary;

            if (_timeLabel != null)
                _timeLabel.ForeColor = colors.TextDisabled;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fontIcon?.Dispose();
                _fontTitle?.Dispose();
                _fontReason?.Dispose();
                _fontTime?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}