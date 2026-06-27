using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls
{
    public class LearningCard : Panel
    {
        #region 控件字段
        private readonly Panel _accentBar;
        private readonly Panel _iconPanel;
        private readonly Label _iconLabel;
        private readonly Label _titleLabel;
        private readonly Label _contentLabel;
        private readonly Label _categoryLabel;
        #endregion

        #region 状态字段
        private bool _isHovered;
        private bool _isSelected;
        #endregion

        #region 全局复用字体（Dispose统一销毁，防止GDI句柄泄漏）
        private readonly Font _fontIcon = new Font("Arial", 24F, FontStyle.Regular, GraphicsUnit.Point, 0);
        private readonly Font _fontTitle = new Font("微软雅黑", 42F, FontStyle.Bold, GraphicsUnit.Point, 134);
        private readonly Font _fontContent = new Font("微软雅黑", 18F, FontStyle.Regular, GraphicsUnit.Point, 134);
        private readonly Font _fontCategoryTag = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
        #endregion

        public LearningCard()
        {
            // 开启双缓冲抗锯齿绘制
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw, true);

            // 1. 实例化所有子控件
            this._accentBar = new Panel();
            this._iconPanel = new Panel();
            this._iconLabel = new Label();
            this._titleLabel = new Label();
            this._contentLabel = new Label();
            this._categoryLabel = new Label();

            // 仅运行时初始化子控件，设计器跳过防止加载异常
            if (!DesignMode)
            {
                InitChildControls();
            }

            // 卡片基础样式
            this.Height = 280;
            this.MinimumSize = new Size(120, 160); // 限制最小尺寸，hover缩放不会过小
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.None;
            this.Padding = new Padding(0);

            // 绑定绘制、鼠标、尺寸变更事件
            this.MouseEnter += LearningCard_MouseEnter;
            this.MouseLeave += LearningCard_MouseLeave;
            this.Paint += LearningCard_Paint;
            this.Resize += (s, e) => this.Invalidate();
        }

        /// <summary>子控件布局初始化（抽离，最小改动修正Dock顺序）</summary>
        private void InitChildControls()
        {
            // 2. 左侧色条 Dock.Left 优先添加
            this._accentBar.Dock = DockStyle.Left;
            this._accentBar.Width = 4;
            this._accentBar.BackColor = Color.FromArgb(76, 175, 80);

            // 3. 图标容器 左停靠，移除写死宽高冲突
            this._iconPanel.Dock = DockStyle.Left;
            this._iconPanel.Width = 60;
            this._iconPanel.Margin = new Padding(10, 10, 0, 10);

            // 图标文字
            this._iconLabel.Text = "📚";
            this._iconLabel.Font = _fontIcon;
            this._iconLabel.Dock = DockStyle.Fill;
            this._iconLabel.TextAlign = ContentAlignment.MiddleCenter;
            this._iconPanel.Controls.Add(this._iconLabel);

            // 分类标签 Dock.Right 次优先添加
            this._categoryLabel.Font = _fontCategoryTag;
            this._categoryLabel.ForeColor = Color.White;
            this._categoryLabel.BackColor = Color.FromArgb(108, 117, 125);
            this._categoryLabel.Dock = DockStyle.Right;
            this._categoryLabel.Padding = new Padding(8, 4, 8, 4);
            this._categoryLabel.AutoSize = true;
            this._categoryLabel.Margin = new Padding(0, 10, 15, 0);
            this._categoryLabel.TextAlign = ContentAlignment.MiddleCenter;

            // 内容描述 Dock.Bottom
            this._contentLabel.Font = _fontContent;
            this._contentLabel.ForeColor = Color.FromArgb(100, 100, 100);
            this._contentLabel.Dock = DockStyle.Bottom;
            this._contentLabel.Padding = new Padding(15, 5, 15, 10);
            this._contentLabel.AutoSize = false;
            this._contentLabel.TextAlign = ContentAlignment.TopCenter;
            this._contentLabel.UseMnemonic = false;
            this._contentLabel.Height = 180; // 缩小高度，给标题留出空间

            // 标题标签 DockStyle.Fill 最后添加（填充剩余区域）
            this._titleLabel.Font = _fontTitle;
            this._titleLabel.ForeColor = Color.FromArgb(33, 33, 33);
            this._titleLabel.Dock = DockStyle.Fill;
            this._titleLabel.Padding = new Padding(15, 10, 15, 10);
            this._titleLabel.AutoSize = false;
            this._titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            this._titleLabel.UseMnemonic = false;

            // Dock标准添加顺序：Left → Right → Bottom → Fill
            this.Controls.Add(this._accentBar);
            this.Controls.Add(this._iconPanel);
            this.Controls.Add(this._categoryLabel);
            this.Controls.Add(this._contentLabel);
            this.Controls.Add(this._titleLabel);
        }

        #region 对外公开属性
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title
        {
            get => _titleLabel.Text;
            set => _titleLabel.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Content
        {
            get => _contentLabel.Text;
            set => _contentLabel.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Category
        {
            get => _categoryLabel.Text;
            set => _categoryLabel.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Icon
        {
            get => _iconLabel.Text;
            set => _iconLabel.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color AccentColor
        {
            get => _accentBar.BackColor;
            set => _accentBar.BackColor = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                this.Invalidate();
            }
        }
        #endregion

        #region 鼠标交互
        private void LearningCard_MouseEnter(object sender, EventArgs e)
        {
            _isHovered = true;
            AnimateScale(1.02f);
            this.Invalidate();
        }

        private void LearningCard_MouseLeave(object sender, EventArgs e)
        {
            _isHovered = false;
            AnimateScale(1.0f);
            this.Invalidate();
        }

        /// <summary>缩放动画逻辑（增加最小尺寸保护，防止卡片过小）</summary>
        private void AnimateScale(float scale)
        {
            int targetW = Math.Max((int)(this.Width * scale), this.MinimumSize.Width);
            int targetH = Math.Max((int)(this.Height * scale), this.MinimumSize.Height);
            int offsetX = (this.Width - targetW) / 2;
            int offsetY = (this.Height - targetH) / 2;

            this.Size = new Size(targetW, targetH);
            this.Location = new Point(this.Location.X + offsetX, this.Location.Y + offsetY);
        }
        #endregion

        #region 圆角自定义绘制
        private void LearningCard_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int radius = 8;
            Rectangle rect = this.ClientRectangle;

            // 圆角裁剪区域
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                path.CloseAllFigures();
                this.Region = new Region(path);

                // 悬浮/选中描边
                if (_isHovered || _isSelected)
                {
                    using (Pen pen = new Pen(Color.FromArgb(76, 175, 80), 2))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            // 悬浮浅底色
            if (_isHovered)
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(5, 76, 175, 80)))
                {
                    g.FillRectangle(brush, rect);
                }
            }

            // 选中加深底色
            if (_isSelected)
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(10, 76, 175, 80)))
                {
                    g.FillRectangle(brush, rect);
                }
            }
        }
        #endregion

        #region 资源释放，解决Font GDI泄漏
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fontIcon?.Dispose();
                _fontTitle?.Dispose();
                _fontContent?.Dispose();
                _fontCategoryTag?.Dispose();
                this.Region?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}