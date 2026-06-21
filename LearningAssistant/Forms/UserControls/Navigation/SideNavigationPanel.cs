using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls.Navigation
{
    /// <summary>
    /// 侧边导航栏控件
    /// 支持图标+文字导航，可折叠，选中高亮，悬停效果
    /// </summary>
    public class SideNavigationPanel : UserControl
    {
        private readonly List<NavigationItem> _items = new();
        private readonly List<NavButton> _buttons = new();
        private string? _selectedKey;
        private bool _collapsed;
        private int _itemHeight = 48;
        private int _itemPadding = 12;
        private Color _hoverColor = Color.FromArgb(230, 230, 250);
        private Color _selectedColor = Color.FromArgb(200, 200, 255);
        private Color _selectedTextColor = Color.FromArgb(63, 63, 180);
        private Color _textColor = Color.FromArgb(80, 80, 80);
        private Color _dividerColor = Color.FromArgb(230, 230, 230);
        private int _iconSize = 20;
        private int _collapsedWidth = 64;
        private int _expandedWidth = 220;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        public bool Collapsed
        {
            get => _collapsed;
            set
            {
                _collapsed = value;
                Width = _collapsed ? _collapsedWidth : _expandedWidth;
                LayoutButtons();
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        public int ItemHeight
        {
            get => _itemHeight;
            set
            {
                _itemHeight = value;
                LayoutButtons();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        public Color HoverColor
        {
            get => _hoverColor;
            set { _hoverColor = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        public Color SelectedColor
        {
            get => _selectedColor;
            set { _selectedColor = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        public Color TextColor
        {
            get => _textColor;
            set { _textColor = value; Invalidate(); }
        }

        [Browsable(false)]
        public string? SelectedKey => _selectedKey;

        [Browsable(false)]
        public List<NavigationItem> Items => _items;

        public event EventHandler<string>? NavigationItemClicked;

        public SideNavigationPanel()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            Width = _expandedWidth;
            BackColor = Color.FromArgb(248, 248, 252);
        }

        public void AddItem(NavigationItem item)
        {
            _items.Add(item);
            CreateButton(item);
            LayoutButtons();
        }

        public void AddItems(IEnumerable<NavigationItem> items)
        {
            foreach (var item in items)
            {
                _items.Add(item);
                CreateButton(item);
            }
            LayoutButtons();
        }

        public void ClearItems()
        {
            _items.Clear();
            foreach (var btn in _buttons)
            {
                Controls.Remove(btn);
                btn.Dispose();
            }
            _buttons.Clear();
        }

        public void SelectItem(string key)
        {
            _selectedKey = key;
            foreach (var btn in _buttons)
            {
                btn.IsSelected = btn.Item.Key == key;
            }
        }

        public void ToggleCollapse()
        {
            Collapsed = !_collapsed;
        }

        private void CreateButton(NavigationItem item)
        {
            var btn = new NavButton(item, _collapsed)
            {
                Height = _itemHeight,
                HoverColor = _hoverColor,
                SelectedColor = _selectedColor,
                SelectedTextColor = _selectedTextColor,
                TextColor = _textColor,
                IconSize = _iconSize,
                ItemPadding = _itemPadding
            };
            btn.Click += OnNavButtonClick;
            _buttons.Add(btn);
            Controls.Add(btn);
        }

        private void OnNavButtonClick(object? sender, EventArgs e)
        {
            if (sender is NavButton btn && btn.Item != null)
            {
                _selectedKey = btn.Item.Key;
                foreach (var b in _buttons)
                {
                    b.IsSelected = b == btn;
                }
                NavigationItemClicked?.Invoke(this, btn.Item.Key);
            }
        }

        private void LayoutButtons()
        {
            int y = 10;
            string? lastGroup = null;

            var sortedItems = _items.Where(i => i.Visible).OrderBy(i => i.Order).ToList();

            foreach (var item in sortedItems)
            {
                var btn = _buttons.FirstOrDefault(b => b.Item.Key == item.Key);
                if (btn == null) continue;

                if (lastGroup != null && item.Group != lastGroup)
                {
                    y += 8;
                }

                btn.Location = new Point(0, y);
                btn.Width = Width;
                btn.Collapsed = _collapsed;
                y += _itemHeight;
                lastGroup = item.Group;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            foreach (var btn in _buttons)
            {
                btn.Width = Width;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var pen = new Pen(_dividerColor);
            g.DrawLine(pen, Width - 1, 0, Width - 1, Height);
        }

        private class NavButton : Control
        {
            public NavigationItem Item { get; }
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    _isSelected = value;
                    Invalidate();
                }
            }
            private bool _isSelected;
            public bool IsHovered { get; private set; }
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public bool Collapsed { get; set; }
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Color HoverColor { get; set; }
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Color SelectedColor { get; set; }
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Color SelectedTextColor { get; set; }
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Color TextColor { get; set; }
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public int IconSize { get; set; }
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public int ItemPadding { get; set; }

            public NavButton(NavigationItem item, bool collapsed)
            {
                Item = item;
                Collapsed = collapsed;
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);
                Cursor = Cursors.Hand;
                DoubleBuffered = true;
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                IsHovered = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                IsHovered = false;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Parent?.BackColor ?? Color.Transparent);

                if (IsSelected)
                {
                    using var brush = new SolidBrush(SelectedColor);
                    var rect = new Rectangle(8, 4, Width - 16, Height - 8);
                    using var path = RoundedRect(rect, 8);
                    g.FillPath(brush, path);
                }
                else if (IsHovered)
                {
                    using var brush = new SolidBrush(HoverColor);
                    var rect = new Rectangle(8, 4, Width - 16, Height - 8);
                    using var path = RoundedRect(rect, 8);
                    g.FillPath(brush, path);
                }

                if (IsSelected)
                {
                    using var indicatorBrush = new SolidBrush(SelectedTextColor);
                    g.FillRoundedRectangle(indicatorBrush, new Rectangle(4, Height / 2 - 12, 4, 24), new Size(2, 2));
                }

                var iconFont = new Font("Segoe UI Emoji", IconSize / 1.8f);
                var iconSize = g.MeasureString(Item.Icon, iconFont);
                var iconX = ItemPadding;
                var iconY = (Height - iconSize.Height) / 2;

                if (Collapsed)
                {
                    iconX = (int)(Width - iconSize.Width) / 2;
                }

                using var iconBrush = new SolidBrush(IsSelected ? SelectedTextColor : TextColor);
                g.DrawString(Item.Icon, iconFont, iconBrush, iconX, iconY);

                if (!Collapsed)
                {
                    var textFont = new Font("微软雅黑", 10F);
                    var textX = iconX + (int)iconSize.Width + 12;
                    var textSize = g.MeasureString(Item.Text, textFont);
                    var textY = (Height - textSize.Height) / 2;

                    using var textBrush = new SolidBrush(IsSelected ? SelectedTextColor : TextColor);
                    g.DrawString(Item.Text, textFont, textBrush, textX, textY);
                }
            }

            private static GraphicsPath RoundedRect(Rectangle rect, int radius)
            {
                var path = new GraphicsPath();
                var r = radius;

                path.AddArc(rect.X, rect.Y, r, r, 180, 90);
                path.AddArc(rect.X + rect.Width - r, rect.Y, r, r, 270, 90);
                path.AddArc(rect.X + rect.Width - r, rect.Y + rect.Height - r, r, r, 0, 90);
                path.AddArc(rect.X, rect.Y + rect.Height - r, r, r, 90, 90);
                path.CloseFigure();

                return path;
            }
        }
    }
}
