using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Common.UI
{
    /// <summary>
    /// PDF 顶部工具栏的细滑杆：自绘圆角轨道与品牌色滑块，替代原生 TrackBar，
    /// 避免系统默认的灰底蓝滑块与工具栏设计语言割裂。支持点击定位、拖动与方向键微调。
    /// </summary>
    public sealed class PdfToolbarSlider : Control, ISidebarThemeAware
    {
        // 轨道两端的留白，保证滑块移到极值时不被裁切
        private const int EdgePadding = 9;
        private const int TrackThickness = 4;
        private const int ThumbIdleSize = 12;
        private const int ThumbActiveSize = 15;

        private int _minimum;
        private int _maximum = 100;
        private int _value;
        private int _smallChange = 10;
        private bool _hover;
        private bool _dragging;

        public PdfToolbarSlider()
        {
            SetStyle(ControlStyles.UserPaint
                     | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw
                     | ControlStyles.SupportsTransparentBackColor
                     | ControlStyles.Selectable, true);

            BackColor = Color.Transparent;
            Size = new Size(100, 28);
            TabStop = false;
            Cursor = Cursors.Hand;
        }

        /// <summary>用户交互改变取值时触发（拖动、点击、滚轮、键盘）；代码赋值不触发。</summary>
        public event EventHandler? ValueChanged;

        [DefaultValue(0)]
        public int Minimum
        {
            get => _minimum;
            set
            {
                if (_minimum == value) return;
                _minimum = value;
                if (_maximum < _minimum) _maximum = _minimum;
                if (_value < _minimum) SetValue(_minimum, raiseEvent: false);
                Invalidate();
            }
        }

        [DefaultValue(100)]
        public int Maximum
        {
            get => _maximum;
            set
            {
                int clamped = Math.Max(value, _minimum);
                if (_maximum == clamped) return;
                _maximum = clamped;
                if (_value > _maximum) SetValue(_maximum, raiseEvent: false);
                Invalidate();
            }
        }

        /// <summary>
        /// 当前值。代码赋值与原生 TrackBar 一致，不触发 <see cref="ValueChanged"/>，
        /// 避免缩放控制器回写滑块时再次触发缩放逻辑。
        /// </summary>
        [DefaultValue(0)]
        public int Value
        {
            get => _value;
            set => SetValue(value, raiseEvent: false);
        }

        /// <summary>方向键/滚轮每次的调整步长。</summary>
        [DefaultValue(10)]
        public int SmallChange
        {
            get => _smallChange;
            set => _smallChange = Math.Max(1, value);
        }

        public void ApplyTheme(bool isDark) => Invalidate();

        private void SetValue(int value, bool raiseEvent)
        {
            int clamped = Math.Clamp(value, _minimum, _maximum);
            if (_value == clamped) return;

            _value = clamped;
            Invalidate();
            if (raiseEvent) ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>把鼠标 X 坐标换算为取值；点击轨道可直接跳转。</summary>
        private void SetValueFromX(int x)
        {
            int left = EdgePadding;
            int right = Width - EdgePadding - 1;
            int span = right - left;
            if (span <= 0) return;

            double ratio = Math.Clamp((x - left) / (double)span, 0d, 1d);
            double value = _minimum + ratio * (_maximum - _minimum);
            SetValue((int)Math.Round(value), raiseEvent: true);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || !Enabled) return;

            _dragging = true;
            Capture = true;
            Focus();
            SetValueFromX(e.X);
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            bool hover = ClientRectangle.Contains(e.Location);
            if (hover != _hover)
            {
                _hover = hover;
                Invalidate();
            }

            if (_dragging && Enabled)
                SetValueFromX(e.X);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;

            _dragging = false;
            Capture = false;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_dragging) return;

            if (_hover)
            {
                _hover = false;
                Invalidate();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!Enabled) return;

            int delta = e.Delta > 0 ? _smallChange : -_smallChange;
            SetValue(_value + delta, raiseEvent: true);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.Home:
                case Keys.End:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!Enabled) return;

            int change = Math.Max(1, _smallChange);
            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Down:
                    SetValue(_value - change, raiseEvent: true);
                    break;
                case Keys.Right:
                case Keys.Up:
                    SetValue(_value + change, raiseEvent: true);
                    break;
                case Keys.Home:
                    SetValue(_minimum, raiseEvent: true);
                    break;
                case Keys.End:
                    SetValue(_maximum, raiseEvent: true);
                    break;
                default:
                    return;
            }

            e.Handled = true;
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var palette = PdfSidebarTheme.PaletteFor(this);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int left = EdgePadding;
            int right = Width - EdgePadding - 1;
            if (right <= left) return;

            int trackY = Height / 2 - TrackThickness / 2;
            var trackRect = new Rectangle(left, trackY, right - left, TrackThickness);

            Color trackColor = Enabled
                ? palette.Border
                : (palette.IsDark ? Color.FromArgb(58, 58, 58) : Color.FromArgb(230, 230, 230));

            using (var trackPath = GdiHelper.CreateRoundedRectPath(trackRect, TrackThickness / 2))
            using (var trackBrush = new SolidBrush(trackColor))
            {
                g.FillPath(trackBrush, trackPath);
            }

            double ratio = _maximum > _minimum
                ? (_value - _minimum) / (double)(_maximum - _minimum)
                : 0d;
            int thumbCenterX = left + (int)Math.Round(ratio * (right - left));

            int filledWidth = thumbCenterX - left;
            if (filledWidth > TrackThickness)
            {
                var fillRect = new Rectangle(left, trackY, filledWidth, TrackThickness);
                using var fillPath = GdiHelper.CreateRoundedRectPath(fillRect, TrackThickness / 2);
                using var fillBrush = new SolidBrush(Enabled ? PdfSidebarTheme.Brand : palette.Muted);
                g.FillPath(fillBrush, fillPath);
            }

            int diameter = _hover || _dragging ? ThumbActiveSize : ThumbIdleSize;
            var thumbRect = new Rectangle(
                thumbCenterX - diameter / 2,
                Height / 2 - diameter / 2,
                diameter,
                diameter);

            // 滑块：外层留白环取工具栏底色，内层品牌色，避免出现突兀的灰色描边
            using (var outerBrush = new SolidBrush(ResolveSurfaceColor()))
            {
                g.FillEllipse(outerBrush, thumbRect);
            }

            var innerRect = Rectangle.Inflate(thumbRect, -2, -2);
            using (var innerBrush = new SolidBrush(Enabled ? PdfSidebarTheme.Brand : palette.Muted))
            {
                g.FillEllipse(innerBrush, innerRect);
            }
        }

        /// <summary>
        /// 向上查找第一个不透明的父容器底色，作为滑块外圈的留白色（日间白 / 夜间深灰）。
        /// </summary>
        private Color ResolveSurfaceColor()
        {
            for (Control? parent = Parent; parent != null; parent = parent.Parent)
            {
                Color back = parent.BackColor;
                if (back.A == 255 && back != Color.Transparent)
                    return back;
            }

            return PdfSidebarTheme.PaletteFor(this).IsDark
                ? Color.FromArgb(45, 45, 45)
                : Color.White;
        }
    }
}
