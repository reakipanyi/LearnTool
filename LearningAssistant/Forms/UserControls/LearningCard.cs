using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.TTS;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls
{
    public class LearningCard : Panel, IThemeable
    {
        #region 控件字段
        private readonly Panel _accentBar;
        private readonly Label _iconLabel;
        private readonly Label _titleLabel;
        private readonly Label _categoryLabel;
        private readonly TableLayoutPanel _innerLayout;
        private readonly Panel _fieldsContainer;
        private readonly List<ContentFieldRow> _fieldRows;
        #endregion

        #region 状态字段
        private bool _isHovered;
        private bool _isSelected;
        private ISpeechCoordinator? _speechCoordinator;
        #endregion

        #region 全局复用字体（Dispose统一销毁，防止GDI句柄泄漏）
        private readonly Font _fontIcon = new Font("Arial", 20F, FontStyle.Regular, GraphicsUnit.Point, 0);
        private readonly Font _fontTitle = new Font("微软雅黑", 48F, FontStyle.Bold, GraphicsUnit.Point, 134);
        private readonly Font _fontCategoryTag = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
        #endregion

        public LearningCard()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw, true);

            this._accentBar = new Panel();
            this._iconLabel = new Label();
            this._titleLabel = new Label();
            this._categoryLabel = new Label();
            this._innerLayout = new TableLayoutPanel();
            this._fieldsContainer = new Panel();
            this._fieldRows = new List<ContentFieldRow>();

            InitChildControls();

            this.MinimumSize = new Size(120, 160);
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.None;
            this.Padding = new Padding(0);

            this.MouseEnter += LearningCard_MouseEnter;
            this.MouseLeave += LearningCard_MouseLeave;
            this.Paint += LearningCard_Paint;
            this.Resize += (s, e) => this.Invalidate();
        }

        private void InitChildControls()
        {
            _accentBar.Dock = DockStyle.Left;
            _accentBar.Width = 4;
            _accentBar.BackColor = Color.FromArgb(76, 175, 80);

            _innerLayout.ColumnCount = 1;
            _innerLayout.RowCount = 3;
            _innerLayout.Dock = DockStyle.Fill;
            _innerLayout.Padding = new Padding(15, 10, 15, 10);
            _innerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            _innerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
            _innerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _iconLabel.Text = "📚";
            _iconLabel.Font = _fontIcon;
            _iconLabel.TextAlign = ContentAlignment.MiddleCenter;
            _iconLabel.Dock = DockStyle.Fill;

            _titleLabel.Font = _fontTitle;
            _titleLabel.ForeColor = Color.FromArgb(33, 33, 33);
            _titleLabel.Dock = DockStyle.Fill;
            _titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            _titleLabel.UseMnemonic = false;
            _titleLabel.AutoSize = false;

            _categoryLabel.Font = _fontCategoryTag;
            _categoryLabel.ForeColor = Color.White;
            _categoryLabel.BackColor = Color.FromArgb(108, 117, 125);
            _categoryLabel.Dock = DockStyle.Right;
            _categoryLabel.Padding = new Padding(8, 4, 8, 4);
            _categoryLabel.AutoSize = true;
            _categoryLabel.Margin = new Padding(0, 5, 15, 0);
            _categoryLabel.TextAlign = ContentAlignment.TopCenter;

            _fieldsContainer.Dock = DockStyle.Fill;
            _fieldsContainer.AutoScroll = true;
            _fieldsContainer.BackColor = Color.Transparent;
            _fieldsContainer.Padding = new Padding(0, 2, 0, 0);

            _innerLayout.Controls.Add(_iconLabel, 0, 0);
            _innerLayout.Controls.Add(_titleLabel, 0, 1);
            _innerLayout.Controls.Add(_fieldsContainer, 0, 2);

            this.Controls.Add(_accentBar);
            this.Controls.Add(_categoryLabel);
            this.Controls.Add(_innerLayout);
        }

        #region 对外公开属性
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title
        {
            get => _titleLabel.Text;
            set => _titleLabel.Text = value;
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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ISpeechCoordinator? SpeechCoordinator
        {
            get => _speechCoordinator;
            set
            {
                _speechCoordinator = value;
                foreach (var row in _fieldRows)
                {
                    row.SpeechCoordinator = value;
                }
            }
        }
        #endregion

        #region 主题
        /// <summary>
        /// 应用主题颜色（夜间模式下卡片背景与文字随主题切换）
        /// </summary>
        public void ApplyTheme(ThemeColors colors)
        {
            this.BackColor = colors.SurfaceElevated;
            _titleLabel.ForeColor = colors.TextPrimary;
            _categoryLabel.BackColor = colors.ThemeMode == ThemeMode.Dark
                ? Color.FromArgb(66, 66, 66)
                : Color.FromArgb(108, 117, 125);
            foreach (var row in _fieldRows)
            {
                row.ApplyTheme(colors);
            }
            this.Invalidate();
        }
        #endregion

        #region 字段设置与行复用
        public void SetFields(IEnumerable<ContentField> fields)
        {
            var fieldList = fields?.ToList() ?? new List<ContentField>();

            int existingCount = _fieldRows.Count;
            int neededCount = fieldList.Count;

            _fieldsContainer.SuspendLayout();

            if (neededCount > existingCount)
            {
                for (int i = existingCount; i < neededCount; i++)
                {
                    var row = new ContentFieldRow();
                    row.SpeechCoordinator = _speechCoordinator;
                    _fieldRows.Add(row);
                    _fieldsContainer.Controls.Add(row);
                }
            }
            else if (neededCount < existingCount)
            {
                for (int i = existingCount - 1; i >= neededCount; i--)
                {
                    var row = _fieldRows[i];
                    _fieldsContainer.Controls.Remove(row);
                    row.Dispose();
                    _fieldRows.RemoveAt(i);
                }
            }

            for (int i = 0; i < neededCount; i++)
            {
                _fieldRows[i].Field = fieldList[i];
            }

            _fieldsContainer.ResumeLayout();
            this.PerformLayout();
        }

        public int FieldCount => _fieldRows.Count;

        public ContentField? GetField(int index)
        {
            if (index >= 0 && index < _fieldRows.Count)
            {
                return _fieldRows[index].Field;
            }
            return null;
        }

        public void TriggerFieldSpeak(int index)
        {
            if (index >= 0 && index < _fieldRows.Count)
            {
                _fieldRows[index].TriggerSpeak();
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
                _fontCategoryTag?.Dispose();

                foreach (var row in _fieldRows)
                {
                    row.Dispose();
                }
                _fieldRows.Clear();

                _fieldsContainer?.Dispose();
                _innerLayout?.Dispose();
                this.Region?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}