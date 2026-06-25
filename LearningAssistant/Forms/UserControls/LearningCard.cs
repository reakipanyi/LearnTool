using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LearningAssistant.Forms.UserControls
{
    public class LearningCard : Panel
    {
        private readonly Label _titleLabel;
        private readonly Label _contentLabel;
        private readonly Label _categoryLabel;
        private readonly Panel _iconPanel;
        private readonly Label _iconLabel;
        private readonly Panel _accentBar;
        private bool _isHovered;
        private bool _isSelected;

        public LearningCard()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            _accentBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 4,
                BackColor = Color.FromArgb(76, 175, 80)
            };

            _iconPanel = new Panel
            {
                Width = 50,
                Height = 50,
                Margin = new Padding(10, 10, 0, 10),
                Dock = DockStyle.Left
            };

            _iconLabel = new Label
            {
                Text = "📚",
                Font = new Font("Arial", 24F),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _iconPanel.Controls.Add(_iconLabel);

            _titleLabel = new Label
            {
                Font = new Font("微软雅黑", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Dock = DockStyle.Top,
                Padding = new Padding(15, 10, 15, 5),
                AutoSize = true
            };

            _contentLabel = new Label
            {
                Font = new Font("微软雅黑", 11F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 0, 15, 10),
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft,
                LineHeight = 20,
                UseMnemonic = false
            };

            _categoryLabel = new Label
            {
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(108, 117, 125),
                Dock = DockStyle.Right,
                Padding = new Padding(8, 4, 8, 4),
                AutoSize = true,
                Margin = new Padding(0, 10, 15, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Controls.Add(_accentBar);
            Controls.Add(_categoryLabel);
            Controls.Add(_contentLabel);
            Controls.Add(_titleLabel);
            Controls.Add(_iconPanel);

            Height = 120;
            BackColor = Color.White;
            BorderStyle = BorderStyle.None;
            Padding = new Padding(0);

            MouseEnter += LearningCard_MouseEnter;
            MouseLeave += LearningCard_MouseLeave;
            Paint += LearningCard_Paint;
        }

        public string Title
        {
            get => _titleLabel.Text;
            set => _titleLabel.Text = value;
        }

        public string Content
        {
            get => _contentLabel.Text;
            set => _contentLabel.Text = value;
        }

        public string Category
        {
            get => _categoryLabel.Text;
            set => _categoryLabel.Text = value;
        }

        public string Icon
        {
            get => _iconLabel.Text;
            set => _iconLabel.Text = value;
        }

        public Color AccentColor
        {
            get => _accentBar.BackColor;
            set => _accentBar.BackColor = value;
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                Invalidate();
            }
        }

        private void LearningCard_MouseEnter(object? sender, EventArgs e)
        {
            _isHovered = true;
            AnimateScale(1.02f);
            Invalidate();
        }

        private void LearningCard_MouseLeave(object? sender, EventArgs e)
        {
            _isHovered = false;
            AnimateScale(1.0f);
            Invalidate();
        }

        private void AnimateScale(float scale)
        {
            int newWidth = (int)(Width * scale);
            int newHeight = (int)(Height * scale);
            int offsetX = (Width - newWidth) / 2;
            int offsetY = (Height - newHeight) / 2;

            Size = new Size(newWidth, newHeight);
            Location = new Point(Location.X + offsetX, Location.Y + offsetY);
        }

        private void LearningCard_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int radius = 8;
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(0, 0, radius, radius, 180, 90);
                path.AddArc(Width - radius, 0, radius, radius, 270, 90);
                path.AddArc(Width - radius, Height - radius, radius, radius, 0, 90);
                path.AddArc(0, Height - radius, radius, radius, 90, 90);
                path.CloseAllFigures();
                Region = new Region(path);
            }

            if (_isHovered || _isSelected)
            {
                using (Pen pen = new Pen(Color.FromArgb(76, 175, 80), 2))
                {
                    g.DrawArc(pen, 0, 0, radius, radius, 180, 90);
                    g.DrawArc(pen, Width - radius, 0, radius, radius, 270, 90);
                    g.DrawArc(pen, Width - radius, Height - radius, radius, radius, 0, 90);
                    g.DrawArc(pen, 0, Height - radius, radius, radius, 90, 90);
                }
            }

            if (_isHovered)
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(5, 76, 175, 80)))
                {
                    g.FillRectangle(brush, ClientRectangle);
                }
            }

            if (_isSelected)
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(10, 76, 175, 80)))
                {
                    g.FillRectangle(brush, ClientRectangle);
                }
            }
        }
    }
}