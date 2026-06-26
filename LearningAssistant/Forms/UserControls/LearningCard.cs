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
        private readonly Font _fontIcon = new Font("Arial", 24F);
        private readonly Font _fontTitle = new Font("微软雅黑", 36F, FontStyle.Bold);
        private readonly Font _fontContent = new Font("微软雅黑", 11F);
        private readonly Font _fontCategoryTag = new Font("微软雅黑", 9F);
        #endregion

        public LearningCard()
        {
            // 开启双缓冲抗锯齿绘制
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint, true);

            // 1. 实例化所有子控件
            this._accentBar = new Panel();
            this._iconPanel = new Panel();
            this._iconLabel = new Label();
            this._titleLabel = new Label();
            this._contentLabel = new Label();
            this._categoryLabel = new Label();

            // 2. 左侧色条
            this._accentBar.Dock = DockStyle.Left;
            this._accentBar.Width = 4;
            this._accentBar.BackColor = Color.FromArgb(76, 175, 80);

            // 3. 图标容器
            this._iconPanel.Width = 50;
            this._iconPanel.Height = 50;
            this._iconPanel.Margin = new Padding(10, 10, 0, 10);
            this._iconPanel.Dock = DockStyle.Left;

            // 图标文字
            this._iconLabel.Text = "📚";
            this._iconLabel.Font = _fontIcon;
            this._iconLabel.Dock = DockStyle.Fill;
            this._iconLabel.TextAlign = ContentAlignment.MiddleCenter;
            this._iconPanel.Controls.Add(this._iconLabel);

            // 标题标签
            this._titleLabel.Font = _fontTitle;
            this._titleLabel.ForeColor = Color.FromArgb(33, 33, 33);
            this._titleLabel.Dock = DockStyle.Fill;
            this._titleLabel.Padding = new Padding(15, 10, 15, 10);
            this._titleLabel.AutoSize = false;
            this._titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            this._titleLabel.UseMnemonic = false;

            // 内容描述
            this._contentLabel.Font = _fontContent;
            this._contentLabel.ForeColor = Color.FromArgb(100, 100, 100);
            this._contentLabel.Dock = DockStyle.Bottom;
            this._contentLabel.Padding = new Padding(15, 5, 15, 10);
            this._contentLabel.AutoSize = false;
            this._contentLabel.TextAlign = ContentAlignment.TopCenter;
            this._contentLabel.UseMnemonic = false;
            this._contentLabel.Height = 120;

            // 分类标签
            this._categoryLabel.Font = _fontCategoryTag;
            this._categoryLabel.ForeColor = Color.White;
            this._categoryLabel.BackColor = Color.FromArgb(108, 117, 125);
            this._categoryLabel.Dock = DockStyle.Right;
            this._categoryLabel.Padding = new Padding(8, 4, 8, 4);
            this._categoryLabel.AutoSize = true;
            this._categoryLabel.Margin = new Padding(0, 10, 15, 0);
            this._categoryLabel.TextAlign = ContentAlignment.MiddleCenter;

            // 按Dock层级倒序添加控件
            this.Controls.Add(this._accentBar);
            this.Controls.Add(this._categoryLabel);
            this.Controls.Add(this._titleLabel);
            this.Controls.Add(this._contentLabel);
            this.Controls.Add(this._iconPanel);

            // 卡片基础样式
            this.Height = 280;
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.None;
            this.Padding = new Padding(0);

            // 绑定绘制与鼠标事件
            this.MouseEnter += LearningCard_MouseEnter;
            this.MouseLeave += LearningCard_MouseLeave;
            this.Paint += LearningCard_Paint;
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
        private void LearningCard_MouseEnter(object? sender, EventArgs e)
        {
            _isHovered = true;
            AnimateScale(1.02f);
            this.Invalidate();
        }

        private void LearningCard_MouseLeave(object? sender, EventArgs e)
        {
            _isHovered = false;
            AnimateScale(1.0f);
            this.Invalidate();
        }

        /// <summary>缩放动画逻辑</summary>
        private void AnimateScale(float scale)
        {
            int newWidth = (int)(this.Width * scale);
            int newHeight = (int)(this.Height * scale);
            int offsetX = (this.Width - newWidth) / 2;
            int offsetY = (this.Height - newHeight) / 2;

            this.Size = new Size(newWidth, newHeight);
            this.Location = new Point(this.Location.X + offsetX, this.Location.Y + offsetY);
        }
        #endregion

        #region 圆角自定义绘制
        private void LearningCard_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int radius = 8;

            // 圆角裁剪区域
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(0, 0, radius, radius, 180, 90);
                path.AddArc(this.Width - radius, 0, radius, radius, 270, 90);
                path.AddArc(this.Width - radius, this.Height - radius, radius, radius, 0, 90);
                path.AddArc(0, this.Height - radius, radius, radius, 90, 90);
                path.CloseAllFigures();
                this.Region = new Region(path);
            }

            // 悬浮/选中描边
            if (_isHovered || _isSelected)
            {
                using (Pen pen = new Pen(Color.FromArgb(76, 175, 80), 2))
                {
                    g.DrawArc(pen, 0, 0, radius, radius, 180, 90);
                    g.DrawArc(pen, this.Width - radius, 0, radius, radius, 270, 90);
                    g.DrawArc(pen, this.Width - radius, this.Height - radius, radius, radius, 0, 90);
                    g.DrawArc(pen, 0, this.Height - radius, radius, radius, 90, 90);
                }
            }

            // 悬浮浅底色
            if (_isHovered)
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(5, 76, 175, 80)))
                {
                    g.FillRectangle(brush, this.ClientRectangle);
                }
            }

            // 选中加深底色
            if (_isSelected)
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(10, 76, 175, 80)))
                {
                    g.FillRectangle(brush, this.ClientRectangle);
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
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}