using System.Drawing.Drawing2D;
using LearningAssistant.Common;
using LearningAssistant.Common.Themes;

namespace LearningAssistant.Forms.UserControls.Navigation
{
    /// <summary>
    /// 用户切换入口控件（01 方案 3.1-3.5）。
    /// 顶部以"圆形头像 + 昵称 + 三角指示"作为单个可点击入口，替代原下拉框；
    /// 点击弹出浮层面板：当前用户概览 + 用户列表（高亮当前，点击即切换）+ 添加/管理入口。
    /// 支持主题联动（IThemeable）与鼠标/键盘操作。
    /// </summary>
    public class UserSwitcherControl : UserControl, IThemeable
    {
        private string _userName = string.Empty;
        private int _streakDays;
        private bool _hovered;
        private readonly List<string> _users = new();
        private Form? _popup;

        // 主题配色（默认浅色，ApplyTheme 时更新）
        private Color _surface = Color.White;
        private Color _textPrimary = Color.FromArgb(33, 33, 33);
        private Color _textSecondary = Color.FromArgb(117, 117, 117);
        private Color _hover = Color.FromArgb(238, 238, 238);
        private Color _primary = Color.FromArgb(63, 81, 181);
        private Color _divider = Color.FromArgb(225, 225, 225);

        private const int AvatarSize = 30;
        private const int EntryHeight = 34;

        /// <summary>面板内用户被选中并切换时触发（参数为用户昵称）。</summary>
        public event EventHandler<string>? UserSelected;

        /// <summary>点击"添加/管理用户"时触发，统一跳转设置窗体。</summary>
        public event EventHandler? OpenSettingsClicked;

        /// <summary>当前用户名：驱动头像首字与昵称展示。</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value ?? string.Empty;
                Invalidate();
            }
        }

        /// <summary>当前用户连续学习天数（用于浮层概览展示）。</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int StreakDays
        {
            get => _streakDays;
            set { _streakDays = value; }
        }

        public UserSwitcherControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            TabStop = true;
            Cursor = Cursors.Hand;
            Size = new Size(160, EntryHeight);
        }

        /// <summary>更新用户列表（保留当前选中，超出用有序列表）。</summary>
        public void SetUsers(IEnumerable<string> users)
        {
            var list = users?.Where(u => !string.IsNullOrWhiteSpace(u)).ToList() ?? new List<string>();
            _users.Clear();
            _users.AddRange(list);

            // 保持当前选中不受列表更新影响
            if (string.IsNullOrEmpty(_userName) && _users.Count > 0)
                UserName = _users[0];

            Invalidate();
        }

        private string Initial => _userName.Length > 0 ? _userName.Substring(0, 1) : "?";

        /// <summary>按用户名稳定哈希生成默认头像底色，避免随机冲突。</summary>
        private static Color AvatarColor(string name)
        {
            uint hash = 2166136261;
            foreach (char c in name)
                hash = (hash ^ c) * 16777619;

            var palette = new[]
            {
                Color.FromArgb(63, 81, 181), Color.FromArgb(156, 39, 176), Color.FromArgb(0, 137, 123),
                Color.FromArgb(255, 87, 34), Color.FromArgb(255, 152, 0), Color.FromArgb(76, 175, 80),
                Color.FromArgb(33, 150, 243), Color.FromArgb(236, 64, 122)
            };
            return palette[(int)(hash % (uint)palette.Length)];
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_hovered)
            {
                using var bg = new GraphicsPath();
                bg.AddRoundedRectangle(new Rectangle(1, 1, Width - 2, Height - 2), 12);
                using var b = new SolidBrush(_hover);
                g.FillPath(b, bg);
            }

            // 头像
            var avatarRect = new Rectangle(2, (Height - AvatarSize) / 2, AvatarSize, AvatarSize);
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(avatarRect);
                using var ab = new SolidBrush(AvatarColor(_userName));
                g.FillPath(ab, path);
            }

            var capRect = new Rectangle(avatarRect.X + 9, avatarRect.Y + 9, 12, 12);
            using (var cp = new GraphicsPath())
            {
                cp.AddEllipse(capRect.InflateMe(-1, -1));
                using var cb = new SolidBrush(Color.FromArgb(255, 255, 255));
                g.FillPath(cb, cp);
            }
            using (var tf = new Font(Font.FontFamily, 9F, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, Initial, tf, avatarRect, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            // 昵称
            var textRect = new Rectangle(avatarRect.Right + 6, 0, Math.Max(Width - avatarRect.Right - 6 - 16, 40), Height);
            TextRenderer.DrawText(g, _userName, Font, textRect, _textPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

            // 三角指示符
            var triX = Width - 14;
            var triY = Height / 2 - 2;
            using var tri = new SolidBrush(_textSecondary);
            g.FillPolygon(tri, new[]
            {
                new PointF(triX, triY),
                new PointF(triX + 8, triY),
                new PointF(triX + 4, triY + 5)
            });
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            ShowPopup();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                ShowPopup();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                ShowPopup();
                e.Handled = true;
            }
        }

        public void ApplyTheme(ThemeColors colors)
        {
            _surface = colors.Surface;
            _textPrimary = colors.TextPrimary;
            _textSecondary = colors.TextSecondary;
            _primary = colors.Primary;
            _hover = Color.FromArgb(colors.ThemeMode == ThemeMode.Dark ? 60 : 238, 238, 238, 238);
            _divider = colors.Divider;
            Invalidate();
        }

        private void ShowPopup()
        {
            if (_popup != null && !_popup.IsDisposed)
            {
                _popup.BringToFront();
                return;
            }

            var location = PointToScreen(new Point(0, Height + 2));
            _popup = BuildPopup(location);
            _popup.Show(this);
        }

        private void ClosePopup()
        {
            if (_popup != null && !_popup.IsDisposed)
                _popup.Close();
            _popup = null;
        }

        private Form BuildPopup(Point location)
        {
            const int width = 268;
            int entryH = 64;      // 当前用户概览
            int listH = _users.Count * 40;
            int actionH = 52;     // 添加/管理
            int height = entryH + listH + actionH + 8;

            var popup = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                Location = location,
                Size = new Size(width, height),
                ShowInTaskbar = false,
                BackColor = _surface,
                KeyPreview = true
            };

            var content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _surface,
                Padding = new Padding(8)
            };
            popup.Controls.Add(content);

            int cursorY = 6;

            // 当前用户概览
            cursorY = AddOverview(content, cursorY);
            cursorY = AddDivider(content, cursorY).Bottom;

            // 用户列表
            int selectedIndex = _users.IndexOf(_userName);
            for (int i = 0; i < _users.Count; i++)
            {
                var name = _users[i];
                bool isCurrent = i == selectedIndex;
                cursorY = AddUserRow(content, cursorY, name, isCurrent).Bottom + 4;
            }

            cursorY = AddDivider(content, cursorY).Bottom;

            // 操作区
            cursorY = AddAction(content, cursorY, "➕ 添加用户", (s, ev) =>
            {
                popup.Close();
                OpenSettingsClicked?.Invoke(this, EventArgs.Empty);
            });
            cursorY = AddAction(content, cursorY, "⚙️ 管理用户", (s, ev) =>
            {
                popup.Close();
                OpenSettingsClicked?.Invoke(this, EventArgs.Empty);
            });

            popup.Deactivate += (s, ev) => popup.Close();

            // 键盘：Esc 关闭，↑/↓ 移动，Enter 选择
            int navIndex = selectedIndex >= 0 ? selectedIndex : 0;
            popup.KeyDown += (s, ev) =>
            {
                if (ev.KeyCode == Keys.Escape)
                {
                    popup.Close();
                }
                else if (ev.KeyCode == Keys.Down || ev.KeyCode == Keys.Up)
                {
                    var delta = ev.KeyCode == Keys.Down ? 1 : -1;
                    navIndex = (_users.Count == 0) ? -1 : (navIndex + delta + _users.Count) % _users.Count;
                    ev.Handled = true;
                }
                else if (ev.KeyCode == Keys.Enter && navIndex >= 0 && navIndex < _users.Count)
                {
                    SelectUser(_users[navIndex]);
                }
            };

            popup.Click += (s, ev) => { }; // 防止空白区点击立即关闭（交给 Deactivate）

            // 由初始化时直接注册 ListView 选择
            return popup;
        }

        private void SelectUser(string name)
        {
            if (name == _userName)
            {
                ClosePopup();
                return;
            }
            _userName = name;
            Invalidate();
            ClosePopup();
            UserSelected?.Invoke(this, name);
        }

        private int AddOverview(Panel content, int top)
        {
            int w = content.Width - content.Padding.Horizontal;
            var avatarRect = new Rectangle(content.Padding.Left + 4, top, 40, 40);
            var avatar = new PictureBox { Bounds = avatarRect, BackColor = Color.Transparent };
            avatar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = new GraphicsPath();
                path.AddEllipse(new Rectangle(0, 0, 40, 40));
                using var b = new SolidBrush(AvatarColor(_userName));
                e.Graphics.FillPath(b, path);
                TextRenderer.DrawText(e.Graphics, Initial, Font, new Rectangle(0, 0, 40, 40), Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            content.Controls.Add(avatar);

            var nameLabel = new Label
            {
                Text = _userName,
                Font = new Font(Font.FontFamily, 11F, FontStyle.Bold),
                ForeColor = _textPrimary,
                AutoSize = true,
                Location = new Point(avatarRect.Right + 10, top + 2),
                BackColor = Color.Transparent
            };
            content.Controls.Add(nameLabel);

            var streakLabel = new Label
            {
                Text = _streakDays > 0 ? $"🔥 连续学习 {_streakDays} 天" : "开始学习之旅",
                Font = new Font(Font.FontFamily, 9F),
                ForeColor = _textSecondary,
                AutoSize = true,
                Location = new Point(avatarRect.Right + 10, nameLabel.Bottom + 4),
                BackColor = Color.Transparent
            };
            content.Controls.Add(streakLabel);

            return top + 44;
        }

        private Control AddDivider(Panel content, int top)
        {
            var div = new Line { Bounds = new Rectangle(6, top, content.Width - 12, 1), Color = _divider };
            content.Controls.Add(div);
            return div;
        }

        private Control AddUserRow(Panel content, int top, string name, bool isCurrent)
        {
            const int h = 40;
            var row = new Panel { Bounds = new Rectangle(2, top, content.Width - 4, h), BackColor = isCurrent ? _hover : Color.Transparent, Cursor = Cursors.Hand };
            var avatarRect = new Rectangle(6, 5, 30, 30);
            var avatar = new PictureBox { Bounds = avatarRect, BackColor = Color.Transparent };
            avatar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = new GraphicsPath();
                path.AddEllipse(new Rectangle(0, 0, 30, 30));
                using var b = new SolidBrush(AvatarColor(name));
                e.Graphics.FillPath(b, path);
                var initial = name.Length > 0 ? name.Substring(0, 1) : "?";
                TextRenderer.DrawText(e.Graphics, initial, Font, new Rectangle(0, 0, 30, 30), Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            row.Controls.Add(avatar);

            var lbl = new Label
            {
                Text = name + (isCurrent ? "（当前）" : ""),
                Font = new Font(Font.FontFamily, 10F, isCurrent ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = _textPrimary,
                AutoSize = true,
                Location = new Point(42, 9),
                BackColor = Color.Transparent
            };
            row.Controls.Add(lbl);

            row.Click += (s, ev) => SelectUser(name);

            content.Controls.Add(row);
            content.Controls.SetChildIndex(row, 0);
            return row;
        }

        private int AddAction(Panel content, int top, string text, EventHandler handler)
        {
            const int h = 24;
            var btn = new Label
            {
                Text = text,
                Font = new Font(Font.FontFamily, 10F),
                ForeColor = _textPrimary,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Bounds = new Rectangle(8, top, content.Width - 16, h),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            btn.MouseEnter += (s, ev) => btn.BackColor = _hover;
            btn.MouseLeave += (s, ev) => btn.BackColor = Color.Transparent;
            btn.Click += handler;
            content.Controls.Add(btn);
            return top + h;
        }
    }

    /// <summary>细分隔线控件。</summary>
    internal class Line : Control
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color Color { get; set; } = Color.FromArgb(225, 225, 225);

        public Line()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Color, 1);
            e.Graphics.DrawLine(pen, 0, 0, Width, 0);
        }
    }

    internal static class RectExt
    {
        public static Rectangle InflateMe(this Rectangle r, int dx, int dy)
        {
            var nr = r;
            nr.Inflate(dx, dy);
            return nr;
        }
    }

    internal static class PathExt
    {
        public static void AddRoundedRectangle(this GraphicsPath path, Rectangle rect, int radius)
        {
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
        }
    }
}