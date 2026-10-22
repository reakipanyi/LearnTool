using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;

namespace LearningAssistant.Common.UI
{
    /// <summary>侧栏面板可响应主题切换的控件约定。</summary>
    public interface ISidebarThemeAware
    {
        void ApplyTheme(bool isDark);
    }

    /// <summary>某一夜间模式下的配色快照，绘制时按所属窗口解析，避免多窗口互相覆盖。</summary>
    public readonly struct PdfSidebarPalette
    {
        public PdfSidebarPalette(bool isDark) => IsDark = isDark;

        public bool IsDark { get; }
        public Color Background => IsDark ? Color.FromArgb(15, 23, 42) : Color.FromArgb(247, 248, 251);
        public Color Surface => IsDark ? Color.FromArgb(30, 41, 59) : Color.White;
        public Color SurfaceAlt => IsDark ? Color.FromArgb(51, 65, 85) : Color.FromArgb(241, 245, 249);
        public Color Border => IsDark ? Color.FromArgb(51, 65, 85) : Color.FromArgb(226, 232, 240);
        public Color Text => IsDark ? Color.FromArgb(248, 250, 252) : Color.FromArgb(15, 23, 42);
        public Color Muted => IsDark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139);
        public Color BrandSoft => IsDark ? Color.FromArgb(67, 56, 145) : Color.FromArgb(237, 233, 254);
        public Color FieldBackground => IsDark ? Color.FromArgb(15, 23, 42) : Color.White;
        public Color Hover => IsDark ? Color.FromArgb(71, 85, 105) : Color.FromArgb(241, 245, 249);
    }

    /// <summary>
    /// PDF 阅读器侧栏统一主题与设计令牌。
    /// 翻译 / 高亮 / 听读三个面板共用同一套表面、文字、语义色与间距。
    /// </summary>
    public static class PdfSidebarTheme
    {
        // 每个顶层窗口的夜间模式独立存储，避免多窗口互相覆盖
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Form, StrongBox<bool>> WindowModes = new();

        /// <summary>未注册窗口（如独立对话框）的兜底模式。</summary>
        public static bool IsDark { get; private set; }

        public static event EventHandler? ThemeChanged;

        public static Color Background => new PdfSidebarPalette(IsDark).Background;
        public static Color Surface => new PdfSidebarPalette(IsDark).Surface;
        public static Color SurfaceAlt => new PdfSidebarPalette(IsDark).SurfaceAlt;
        public static Color Border => new PdfSidebarPalette(IsDark).Border;
        public static Color Text => new PdfSidebarPalette(IsDark).Text;
        public static Color Muted => new PdfSidebarPalette(IsDark).Muted;
        public static Color Brand => Color.FromArgb(108, 92, 231);
        public static Color BrandSoft => new PdfSidebarPalette(IsDark).BrandSoft;
        public static Color Danger => Color.FromArgb(239, 68, 68);
        public static Color Success => Color.FromArgb(34, 197, 94);
        public static Color Score => Color.FromArgb(139, 92, 246);
        public static Color FieldBackground => new PdfSidebarPalette(IsDark).FieldBackground;
        public static Color Hover => new PdfSidebarPalette(IsDark).Hover;

        /// <summary>记录指定窗口的夜间模式；绘制与主题应用均按窗口解析。</summary>
        public static void SetWindowDark(Form window, bool isDark)
        {
            if (window == null) return;
            WindowModes.Remove(window);
            WindowModes.Add(window, new StrongBox<bool>(isDark));
        }

        /// <summary>解析控件所属窗口（含 Owner 链）的夜间模式。</summary>
        public static bool IsDarkFor(Control? control)
        {
            Form? form = control as Form ?? control?.FindForm();
            while (form != null)
            {
                if (WindowModes.TryGetValue(form, out var state)) return state.Value;
                form = form.Owner;
            }
            return IsDark;
        }

        /// <summary>获取控件所属窗口的配色快照，用于自绘控件。</summary>
        public static PdfSidebarPalette PaletteFor(Control? control) => new(IsDarkFor(control));

        /// <summary>通知依赖主题的窗口重绘，不修改任何窗口的夜间状态。</summary>
        public static void NotifyThemeChanged() => ThemeChanged?.Invoke(null, EventArgs.Empty);

        /// <summary>把主题递归应用到指定容器（侧栏三个面板）。</summary>
        public static void ApplyRecursive(Control? root)
            => ApplyRecursive(root, root == null ? IsDark : IsDarkFor(root));

        /// <summary>按指定夜间模式把主题递归应用到容器。</summary>
        public static void ApplyRecursive(Control? root, bool isDark)
        {
            if (root == null) return;

            var palette = new PdfSidebarPalette(isDark);
            bool insideCard = IsInsideCard(root);

            switch (root)
            {
                case TabPage:
                    root.BackColor = palette.Background;
                    break;
                case TableLayoutPanel or FlowLayoutPanel or SplitContainer:
                    root.BackColor = insideCard ? palette.Surface : palette.Background;
                    break;
                case Panel:
                    root.BackColor = root.Tag is string surfaceTag && surfaceTag == "surface"
                        ? palette.Surface
                        : insideCard ? palette.Surface : palette.Background;
                    break;
                case TextBox:
                    root.BackColor = palette.FieldBackground;
                    root.ForeColor = palette.Text;
                    break;
                case ListBox:
                    root.BackColor = palette.Surface;
                    root.ForeColor = palette.Text;
                    break;
                case ComboBox:
                    root.BackColor = palette.Surface;
                    root.ForeColor = palette.Text;
                    break;
                case Label label:
                    label.BackColor = Color.Transparent;
                    label.ForeColor = label.Tag is string labelTag && labelTag == "muted" ? palette.Muted
                        : label.Tag is string scoreTag && scoreTag == "score" ? Score
                        : label.Tag is string accentTag && accentTag == "accent" ? Brand
                        : palette.Text;
                    break;
                case CheckBox or RadioButton:
                    if (root.Tag is string keepTag && keepTag == "keep") break;
                    root.BackColor = Color.Transparent;
                    root.ForeColor = palette.Text;
                    break;
                case GroupBox:
                    root.BackColor = palette.Background;
                    root.ForeColor = palette.Text;
                    break;
                case Button button:
                    if (button.Tag is string tag && (tag == "accent" || tag == "danger" || tag == "record" || tag == "keep"))
                        break;
                    button.BackColor = palette.SurfaceAlt;
                    button.ForeColor = palette.Text;
                    button.FlatAppearance.BorderColor = palette.Border;
                    button.FlatAppearance.MouseOverBackColor = palette.Hover;
                    break;
            }

            if (root is ISidebarThemeAware themed)
                themed.ApplyTheme(isDark);

            foreach (Control child in root.Controls)
                ApplyRecursive(child, isDark);
        }

        private static bool IsInsideCard(Control control)
        {
            for (Control? parent = control.Parent; parent != null; parent = parent.Parent)
            {
                if (parent is SidebarCard) return true;
            }
            return false;
        }
    }

    /// <summary>统一页头：图标 + 标题 + 状态副标题 + 右侧主操作。</summary>
    public sealed class SidebarHeaderBar : Panel, ISidebarThemeAware
    {
        private readonly Label _icon = new();
        private readonly Label _title = new();
        private readonly Label _subtitle = new();
        private readonly Button? _action;

        public Label Subtitle => _subtitle;

        public SidebarHeaderBar(string icon, string title, string? actionText = null, EventHandler? onAction = null)
        {
            Height = 48;
            Dock = DockStyle.Top;
            BackColor = PdfSidebarTheme.Background;
            Padding = new Padding(2, 0, 2, 0);

            _icon.Text = icon;
            _icon.Font = new Font("Segoe UI Emoji", 13F);
            _icon.AutoSize = true;
            _icon.Location = new Point(0, 10);

            _title.Text = title;
            _title.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold);
            _title.AutoSize = true;
            _title.Location = new Point(28, 6);

            _subtitle.Font = new Font("Microsoft YaHei UI", 8.5F);
            _subtitle.AutoSize = true;
            _subtitle.Location = new Point(28, 25);
            _subtitle.ForeColor = PdfSidebarTheme.Muted;
            _subtitle.Tag = "muted";

            Controls.Add(_icon);
            Controls.Add(_title);
            Controls.Add(_subtitle);

            if (!string.IsNullOrEmpty(actionText))
            {
                _action = SidebarUi.CreatePrimaryButton(actionText, 28);
                _action.Dock = DockStyle.None;
                _action.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                _action.Click += onAction;
                Controls.Add(_action);
            }

            ApplyTheme(PdfSidebarTheme.IsDark);
        }

        public void SetSubtitle(string text) => _subtitle.Text = text;

        public void ApplyTheme(bool isDark)
        {
            var palette = new PdfSidebarPalette(isDark);
            BackColor = palette.Background;
            _title.ForeColor = palette.Text;
            _subtitle.ForeColor = palette.Muted;
            if (_action != null)
            {
                _action.BackColor = PdfSidebarTheme.Brand;
                _action.ForeColor = Color.White;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_action != null)
                _action.Location = new Point(Width - _action.Width - 4, 10);
        }
    }

    /// <summary>白底卡片容器：标题行 + 内容区，替代 GroupBox。</summary>
    public sealed class SidebarCard : Panel, ISidebarThemeAware
    {
        private readonly Label _title = new();
        private readonly Panel _body = new();
        private Button? _actionButton;
        private bool _isDark;

        public Panel Body => _body;

        /// <summary>供 Visual Studio 设计器实例化；运行时请使用带标题的构造函数。</summary>
        public SidebarCard() : this(string.Empty)
        {
        }

        /// <summary>卡片标题；设计器通过该属性设置标题。</summary>
        [System.ComponentModel.DefaultValue("")]
        public string Title
        {
            get => _title.Text;
            set => _title.Text = value;
        }

        public SidebarCard(string title, string? actionText = null, EventHandler? onAction = null)
        {
            BackColor = PdfSidebarTheme.Background;
            Padding = new Padding(2, 1, 2, 1);

            _title.Text = title;
            _title.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            _title.AutoSize = true;
            _title.Location = new Point(12, 9);

            _body.Dock = DockStyle.Fill;
            _body.Padding = new Padding(10, 30, 10, 10);
            _body.BackColor = PdfSidebarTheme.Surface;

            Controls.Add(_body);
            Controls.Add(_title);

            if (!string.IsNullOrEmpty(actionText))
            {
                _actionButton = SidebarUi.CreateGhostButton(actionText, 26);
                _actionButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                _actionButton.Click += onAction;
                Controls.Add(_actionButton);
            }

            ApplyTheme(PdfSidebarTheme.IsDark);
        }

        public void ApplyTheme(bool isDark)
        {
            _isDark = isDark;
            var palette = new PdfSidebarPalette(isDark);
            BackColor = palette.Background;
            _title.ForeColor = palette.Text;
            _body.BackColor = palette.Surface;
            if (_actionButton != null)
            {
                _actionButton.BackColor = palette.Surface;
                _actionButton.ForeColor = palette.Muted;
                _actionButton.FlatAppearance.MouseOverBackColor = palette.Hover;
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var rect = new Rectangle(2, 1, Width - 5, Height - 3);
            if (rect.Width <= 0 || rect.Height <= 0) return;

            var palette = new PdfSidebarPalette(_isDark);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = GdiHelper.CreateRoundedRectPath(rect, 10);
            using var fill = new SolidBrush(palette.Surface);
            using var border = new Pen(palette.Border);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_actionButton != null)
                _actionButton.Location = new Point(Width - _actionButton.Width - 12, 6);

            // 让内容区跟随卡片圆角，避免方角覆盖圆角边框
            if (_body.Width > 0 && _body.Height > 0)
            {
                using var path = GdiHelper.CreateRoundedRectPath(
                    new Rectangle(0, 0, _body.Width, _body.Height), 9);
                var oldRegion = _body.Region;
                _body.Region = new Region(path);
                oldRegion?.Dispose();
            }
        }
    }

    /// <summary>分段切换条：用于「配套音频 / 整页录音 / 句子跟读」。</summary>
    public sealed class SegmentedTabs : Control, ISidebarThemeAware
    {
        private readonly List<string> _items = new();
        private int _selectedIndex;
        private int _hoverIndex = -1;
        private bool _isDark;

        public event EventHandler? SelectedIndexChanged;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                int clamped = _items.Count == 0 ? -1 : Math.Clamp(value, 0, _items.Count - 1);
                if (clamped == _selectedIndex) return;
                _selectedIndex = clamped;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public SegmentedTabs()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Height = 36;
            Dock = DockStyle.Top;
            BackColor = PdfSidebarTheme.Background;
            Cursor = Cursors.Hand;
        }

        public void SetItems(params string[] items)
        {
            _items.Clear();
            _items.AddRange(items);
            _selectedIndex = _items.Count > 0 ? 0 : -1;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = HitTest(e.X);
            if (index != _hoverIndex)
            {
                _hoverIndex = index;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverIndex = -1;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int index = HitTest(e.X);
            if (index >= 0)
                SelectedIndex = index;
        }

        private int HitTest(int x)
        {
            if (_items.Count == 0 || Width <= 0) return -1;
            int itemWidth = Width / _items.Count;
            if (itemWidth <= 0) return -1;
            int index = x / itemWidth;
            return index >= 0 && index < _items.Count ? index : -1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var palette = new PdfSidebarPalette(_isDark);

            var track = new Rectangle(2, 4, Width - 5, Height - 9);
            if (track.Width <= 0 || track.Height <= 0) return;

            using (var trackPath = GdiHelper.CreateRoundedRectPath(track, 8))
            using (var trackBrush = new SolidBrush(palette.SurfaceAlt))
                g.FillPath(trackBrush, trackPath);

            if (_items.Count == 0) return;

            int itemWidth = track.Width / _items.Count;
            for (int i = 0; i < _items.Count; i++)
            {
                var itemRect = new Rectangle(track.X + i * itemWidth, track.Y, itemWidth, track.Height);
                bool selected = i == _selectedIndex;
                bool hover = i == _hoverIndex && !selected;

                if (selected || hover)
                {
                    using var itemPath = GdiHelper.CreateRoundedRectPath(
                        new Rectangle(itemRect.X + 2, itemRect.Y + 2, itemRect.Width - 4, itemRect.Height - 4), 6);
                    using var itemBrush = new SolidBrush(selected
                        ? (palette.IsDark ? Color.FromArgb(71, 85, 105) : Color.White)
                        : palette.Hover);
                    g.FillPath(itemBrush, itemPath);
                }

                using var font = new Font("Microsoft YaHei UI", 9F,
                    selected ? FontStyle.Bold : FontStyle.Regular);
                using var textBrush = new SolidBrush(selected
                    ? (palette.IsDark ? Color.White : PdfSidebarTheme.Brand)
                    : palette.Muted);
                using var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(_items[i], font, textBrush, itemRect, format);
            }
        }

        public void ApplyTheme(bool isDark)
        {
            _isDark = isDark;
            BackColor = new PdfSidebarPalette(isDark).Background;
            Invalidate();
        }
    }

    /// <summary>跟读窗口的倒计时 / 录音状态圆环。</summary>
    public sealed class CircularStatus : Control, ISidebarThemeAware
    {
        private int _countdown;
        private bool _recording;
        private bool _isDark;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int Countdown
        {
            get => _countdown;
            set
            {
                _countdown = value;
                Invalidate();
            }
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool Recording
        {
            get => _recording;
            set
            {
                _recording = value;
                Invalidate();
            }
        }

        public CircularStatus()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Size = new Size(88, 88);
            BackColor = Color.LightBlue;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int size = Math.Min(Width, Height) - 8;
            if (size <= 0) return;
            var rect = new Rectangle((Width - size) / 2, (Height - size) / 2, size, size);

            var palette = new PdfSidebarPalette(_isDark);
            using var trackPen = new Pen(palette.Border, 5);
            g.DrawEllipse(trackPen, rect);

            var activeColor = _recording ? PdfSidebarTheme.Danger : PdfSidebarTheme.Brand;
            float ratio = _recording ? 1f : Math.Clamp(_countdown / 3f, 0f, 1f);
            if (ratio > 0)
            {
                using var arcPen = new Pen(activeColor, 5) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawArc(arcPen, rect, -90, 360 * ratio);
            }

            string text = _recording ? "REC" : (_countdown > 0 ? _countdown.ToString() : "●");
            using var font = new Font("Microsoft YaHei UI", _recording ? 12F : 18F,
                _recording ? FontStyle.Bold : FontStyle.Regular);
            using var brush = new SolidBrush(activeColor);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(text, font, brush, rect, format);
        }

        public void ApplyTheme(bool isDark)
        {
            _isDark = isDark;
            Invalidate();
        }
    }

    /// <summary>
    /// 启用双缓冲的 TabControl。
    /// 侧栏页签为自绘图标，悬停切换背景色时整条页签会重绘，
    /// 原生 TabControl 未开启双缓冲会出现图标/标题闪烁，这里统一开启。
    /// </summary>
    public sealed class BufferedTabControl : TabControl
    {
        public BufferedTabControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            DoubleBuffered = true;
        }
    }

    /// <summary>侧栏与跟读窗口共用的控件工厂。</summary>
    public static class SidebarUi
    {
        public static Button CreatePrimaryButton(string text, int height = 30)
        {
            var button = new Button();
            button.Text = text;
            button.Height = height;
            button.AutoSize = false;
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = PdfSidebarTheme.Brand;
            button.ForeColor = Color.White;
            button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.Tag = "accent";
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(88, 72, 211);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(72, 58, 185);
            return button;
        }

        public static Button CreateSecondaryButton(string text, int height = 30)
        {
            var button = new Button();
            button.Text = text;
            button.Height = height;
            button.AutoSize = false;
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = PdfSidebarTheme.Surface;
            button.ForeColor = PdfSidebarTheme.Text;
            button.Font = new Font("Microsoft YaHei UI", 9F);
            button.Cursor = Cursors.Hand;
            button.FlatAppearance.BorderColor = PdfSidebarTheme.Border;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = PdfSidebarTheme.Hover;
            return button;
        }

        public static Button CreateGhostButton(string text, int height = 26)
        {
            var button = CreateSecondaryButton(text, height);
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = PdfSidebarTheme.Surface;
            button.ForeColor = PdfSidebarTheme.Muted;
            return button;
        }

        public static Button CreateIconButton(string text, string tooltip, EventHandler? onClick = null)
        {
            var button = new Button();
            button.Text = text;
            button.Size = new Size(30, 30);
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = PdfSidebarTheme.Surface;
            button.ForeColor = PdfSidebarTheme.Text;
            button.Font = new Font("Segoe UI Emoji", 10F);
            button.Cursor = Cursors.Hand;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = PdfSidebarTheme.Hover;
            if (onClick != null) button.Click += onClick;
            var tip = new ToolTip();
            tip.SetToolTip(button, tooltip);
            return button;
        }

        public static Label CreateSectionLabel(string text)
        {
            var label = new Label();
            label.Text = text;
            label.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold);
            label.ForeColor = PdfSidebarTheme.Muted;
            label.AutoSize = true;
            label.Tag = "muted";
            return label;
        }

        public static void StyleTextBox(TextBox textBox)
        {
            textBox.BorderStyle = BorderStyle.None;
            textBox.BackColor = PdfSidebarTheme.FieldBackground;
            textBox.ForeColor = PdfSidebarTheme.Text;
            textBox.Font = new Font("Microsoft YaHei UI", 9.5F);
            textBox.Multiline = true;
            textBox.ScrollBars = ScrollBars.Vertical;
        }

        public static void StyleCheckBox(CheckBox checkBox)
        {
            checkBox.FlatStyle = FlatStyle.System;
            checkBox.ForeColor = PdfSidebarTheme.Text;
            checkBox.BackColor = Color.Transparent;
        }

        public static Panel CreateEmptyState(string icon, string text, string? actionText = null, EventHandler? onAction = null)
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = PdfSidebarTheme.Surface;
            panel.Tag = "surface";

            var iconLabel = new Label();
            iconLabel.Text = icon;
            iconLabel.Font = new Font("Segoe UI Emoji", 22F);
            iconLabel.ForeColor = PdfSidebarTheme.Muted;
            iconLabel.AutoSize = false;
            iconLabel.TextAlign = ContentAlignment.MiddleCenter;
            iconLabel.Size = new Size(48, 44);
            iconLabel.Tag = "muted";

            var textLabel = new Label();
            textLabel.Text = text;
            textLabel.Font = new Font("Microsoft YaHei UI", 9F);
            textLabel.ForeColor = PdfSidebarTheme.Muted;
            textLabel.AutoSize = false;
            textLabel.TextAlign = ContentAlignment.MiddleCenter;
            textLabel.Height = 40;
            textLabel.Tag = "muted";

            panel.Controls.Add(iconLabel);
            panel.Controls.Add(textLabel);

            Button? action = null;
            if (!string.IsNullOrEmpty(actionText) && onAction != null)
            {
                action = CreateSecondaryButton(actionText, 28);
                action.Width = 140;
                action.Click += onAction;
                panel.Controls.Add(action);
            }

            void Layout()
            {
                int centerX = panel.ClientSize.Width / 2;
                int centerY = panel.ClientSize.Height / 2;
                iconLabel.Location = new Point(centerX - iconLabel.Width / 2, centerY - 70);
                textLabel.Location = new Point(8, centerY - 22);
                textLabel.Width = Math.Max(20, panel.ClientSize.Width - 16);
                if (action != null)
                    action.Location = new Point(centerX - action.Width / 2, centerY + 24);
            }

            panel.Resize += (_, _) => Layout();
            Layout();
            return panel;
        }
    }
}
