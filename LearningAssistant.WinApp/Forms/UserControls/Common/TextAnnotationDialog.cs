using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace LearningAssistant.Forms.UserControls.Common
{
    public class TextAnnotationDialogResult
    {
        public string Text { get; set; } = string.Empty;
        public Color SelectedColor { get; set; } = Color.Red;
        public float FontSize { get; set; } = 16f;
        public string FontFamily { get; set; } = "Microsoft YaHei UI";
        public bool Confirmed { get; set; }
    }

    public partial class TextAnnotationDialog : Form
    {
        private TextBox _textBox;
        private Label _labelWordCount;
        private Panel _colorPanel;
        private ComboBox _fontCombo;
        private Panel _sizePanel;
        private TrackBar _sizeSlider;
        private Label _sizeValueLabel;
        private Panel _previewPanel;
        private Label _previewLabel;
        private Button _okButton;
        private Button _cancelButton;
        private Label _labelContent;

        private Color _selectedColor = Color.Red;
        private float _selectedFontSize = 16f;
        private string _selectedFontFamily = "Microsoft YaHei UI";

        private static readonly (Color Color, string Name)[] AvailableColors =
        {
            (Color.FromArgb(244, 67, 54),  "红色"),
            (Color.FromArgb(255, 87, 34), "橙色"),
            (Color.FromArgb(255, 152, 0), "琥珀"),
            (Color.FromArgb(255, 193, 7), "黄色"),
            (Color.FromArgb(255, 235, 59), "亮黄"),
            (Color.FromArgb(139, 195, 74), "浅绿"),
            (Color.FromArgb(76, 175, 80),  "绿色"),
            (Color.FromArgb(0, 150, 136),  "青色"),
            (Color.FromArgb(0, 172, 193),  "青蓝"),
            (Color.FromArgb(33, 150, 243), "蓝色"),
            (Color.FromArgb(63, 81, 181),  "靛蓝"),
            (Color.FromArgb(156, 39, 176), "紫色"),
            (Color.FromArgb(233, 30, 99),  "玫红"),
            (Color.FromArgb(121, 85, 72),  "棕色"),
            (Color.FromArgb(96, 96, 96),   "灰色"),
            (Color.Black,                  "黑色"),
            (Color.White,                  "白色"),
            (Color.FromArgb(240, 240, 240),"浅灰"),
        };

        private static readonly (float Size, string Label)[] AvailableSizes =
        {
            (12f, "12"), (14f, "14"), (16f, "16"), (18f, "18"),
            (20f, "20"), (24f, "24"), (28f, "28"), (36f, "36"), (48f, "48")
        };

        private static readonly string[] CommonFonts =
        {
            "Microsoft YaHei UI", "SimSun", "SimHei", "KaiTi",
            "Arial", "Times New Roman", "Consolas", "Segoe UI"
        };

        private static Color _panelBg = Color.FromArgb(245, 245, 250);
        private static Color _sectionBg = Color.White;
        private static Color _accentColor = Color.FromArgb(25, 118, 210);
        private static Color _textPrimary = Color.FromArgb(50, 50, 60);
        private static Color _textSecondary = Color.FromArgb(140, 140, 150);

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string InitialText
        {
            get => _textBox.Text;
            set => _textBox.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SelectedColor
        {
            get => _selectedColor;
            set => _selectedColor = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float SelectedFontSize
        {
            get => _selectedFontSize;
            set => _selectedFontSize = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedFontFamily
        {
            get => _selectedFontFamily;
            set => _selectedFontFamily = value;
        }

        private IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        public TextAnnotationDialog(
            string title = "文字注解",
            string initialText = "",
            Color? initialColor = null,
            float initialFontSize = 16f,
            string? initialFontFamily = null)
        {
            _selectedColor = initialColor ?? Color.Red;
            _selectedFontSize = initialFontSize;
            if (!string.IsNullOrEmpty(initialFontFamily))
                _selectedFontFamily = initialFontFamily;

            InitializeComponent(title);
            InitialText = initialText;
            InitializeColorPanel();
            InitializeFontCombo();
            InitializeSizePanel();
            UpdatePreview();
        }

        private void InitializeComponent(string formTitle)
        {
            this.Text = formTitle;
            this.Size = new Size(520, 620);
            this.MinimumSize = new Size(520, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = _panelBg;
            this.Padding = new Padding(0);

            _labelContent = new Label();
            _textBox = new TextBox();
            _labelWordCount = new Label();
            _colorPanel = new Panel();
            _fontCombo = new ComboBox();
            _sizePanel = new Panel();
            _sizeSlider = new TrackBar();
            _sizeValueLabel = new Label();
            _previewPanel = new Panel();
            _previewLabel = new Label();
            _okButton = new Button();
            _cancelButton = new Button();

            int margin = 24;
            int width = 472;

            // 
            // _labelContent
            // 
            _labelContent.Text = "✏ 文字内容";
            _labelContent.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            _labelContent.ForeColor = _textPrimary;
            _labelContent.Location = new Point(margin, 18);
            _labelContent.Size = new Size(120, 22);

            // 
            // _textBox
            // 
            _textBox.Location = new Point(margin, 46);
            _textBox.Size = new Size(width, 70);
            _textBox.Multiline = true;
            _textBox.ScrollBars = ScrollBars.Vertical;
            _textBox.BorderStyle = BorderStyle.FixedSingle;
            _textBox.Font = new Font("Microsoft YaHei UI", 10F);
            _textBox.TextChanged += TextBox_TextChanged;

            // 
            // _labelWordCount
            // 
            _labelWordCount.Text = "字数: 0";
            _labelWordCount.ForeColor = _textSecondary;
            _labelWordCount.Font = new Font("Microsoft YaHei UI", 9F);
            _labelWordCount.Location = new Point(margin, 120);
            _labelWordCount.Size = new Size(100, 18);

            // 
            // _colorPanel
            // 
            _colorPanel.Location = new Point(margin, 148);
            _colorPanel.Size = new Size(width, 80);
            _colorPanel.BackColor = _sectionBg;
            _colorPanel.Paint += ColorPanel_Paint;

            // 
            // _fontCombo
            // 
            _fontCombo.Location = new Point(margin, 248);
            _fontCombo.Size = new Size(width, 28);
            _fontCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _fontCombo.DrawMode = DrawMode.OwnerDrawVariable;
            _fontCombo.ItemHeight = 28;
            _fontCombo.DropDownHeight = 280;
            _fontCombo.MaxDropDownItems = 10;
            _fontCombo.DrawItem += FontCombo_DrawItem;
            _fontCombo.MeasureItem += FontCombo_MeasureItem;
            _fontCombo.SelectedIndexChanged += FontCombo_SelectedIndexChanged;

            // 
            // _sizeSlider
            // 
            _sizeSlider.Location = new Point(margin + 4, 293);
            _sizeSlider.Size = new Size(width - 80, 30);
            _sizeSlider.Minimum = 8;
            _sizeSlider.Maximum = 72;
            _sizeSlider.TickFrequency = 8;
            _sizeSlider.SmallChange = 2;
            _sizeSlider.LargeChange = 8;
            _sizeSlider.TickStyle = TickStyle.None;
            _sizeSlider.Value = (int)_selectedFontSize;
            _sizeSlider.ValueChanged += SizeSlider_ValueChanged;

            _sizeValueLabel.Text = _selectedFontSize.ToString("0");
            _sizeValueLabel.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
            _sizeValueLabel.ForeColor = _accentColor;
            _sizeValueLabel.Location = new Point(margin + width - 58, 292);
            _sizeValueLabel.Size = new Size(50, 28);
            _sizeValueLabel.TextAlign = ContentAlignment.MiddleRight;

            // 
            // _sizePanel
            // 
            _sizePanel.Location = new Point(margin, 328);
            _sizePanel.Size = new Size(width, 48);
            _sizePanel.BackColor = Color.Transparent;

            // 
            // _previewPanel
            // 
            _previewPanel.Location = new Point(margin, 390);
            _previewPanel.Size = new Size(width, 100);
            _previewPanel.BackColor = _sectionBg;
            _previewPanel.Paint += PreviewPanel_Paint;
            _previewPanel.Padding = new Padding(16, 8, 16, 8);

            _previewLabel = new Label
            {
                AutoSize = false,
                Location = new Point(16, 8),
                Size = new Size(width - 32, 84),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = _selectedColor,
                BackColor = Color.Transparent,
                Font = new Font(_selectedFontFamily, Math.Min(_selectedFontSize, 36f))
            };
            _previewPanel.Controls.Add(_previewLabel);

            // 
            // _okButton
            // 
            _okButton.Text = "确定";
            _okButton.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            _okButton.ForeColor = Color.White;
            _okButton.BackColor = _accentColor;
            _okButton.FlatStyle = FlatStyle.Flat;
            _okButton.FlatAppearance.BorderSize = 0;
            _okButton.Location = new Point(margin + width - 180, 510);
            _okButton.Size = new Size(80, 38);
            _okButton.Cursor = Cursors.Hand;
            _okButton.DialogResult = DialogResult.OK;
            _okButton.Paint += (s, e) =>
            {
                var btn = (Button)s!;
                var r = new Rectangle(0, 0, btn.Width, btn.Height);
                using var path = new GraphicsPath();
                path.AddArc(r.X, r.Y, 6, 6, 180, 90);
                path.AddArc(r.Right - 6, r.Y, 6, 6, 270, 90);
                path.AddArc(r.Right - 6, r.Bottom - 6, 6, 6, 0, 90);
                path.AddArc(r.X, r.Bottom - 6, 6, 6, 90, 90);
                path.CloseFigure();
                btn.Region = new Region(path);
            };
            _okButton.Click += (s, e) => { _textBox.Text = _textBox.Text.Trim(); };

            // 
            // _cancelButton
            // 
            _cancelButton.Text = "取消";
            _cancelButton.Font = new Font("Microsoft YaHei UI", 10F);
            _cancelButton.ForeColor = _textPrimary;
            _cancelButton.BackColor = Color.FromArgb(230, 230, 235);
            _cancelButton.FlatStyle = FlatStyle.Flat;
            _cancelButton.FlatAppearance.BorderSize = 0;
            _cancelButton.Location = new Point(margin + width - 88, 510);
            _cancelButton.Size = new Size(80, 38);
            _cancelButton.Cursor = Cursors.Hand;
            _cancelButton.DialogResult = DialogResult.Cancel;
            _cancelButton.Paint += (s, e) =>
            {
                var btn = (Button)s!;
                var r = new Rectangle(0, 0, btn.Width, btn.Height);
                using var path = new GraphicsPath();
                path.AddArc(r.X, r.Y, 6, 6, 180, 90);
                path.AddArc(r.Right - 6, r.Y, 6, 6, 270, 90);
                path.AddArc(r.Right - 6, r.Bottom - 6, 6, 6, 0, 90);
                path.AddArc(r.X, r.Bottom - 6, 6, 6, 90, 90);
                path.CloseFigure();
                btn.Region = new Region(path);
            };

            // 
            // TextAnnotationDialog
            // 
            this.SuspendLayout();
            var headerPanel = new Panel
            {
                Size = new Size(520, 2),
                Location = new Point(0, 0),
                BackColor = _accentColor
            };
            this.Controls.Add(headerPanel);
            this.Controls.AddRange(new Control[]
            {
                _labelContent, _textBox, _labelWordCount,
                _colorPanel, _fontCombo,
                _sizeSlider, _sizeValueLabel, _sizePanel,
                _previewPanel, _okButton, _cancelButton
            });
            this.ResumeLayout(false);
            this.PerformLayout();

            this.AcceptButton = _okButton;
            this.CancelButton = _cancelButton;
        }

        private void ColorPanel_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var bgBrush = new SolidBrush(_sectionBg);
            g.FillRectangle(bgBrush, _colorPanel.ClientRectangle);

            using var titleFont = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold);
            using var titleBrush = new SolidBrush(_textSecondary);
            g.DrawString("颜色", titleFont, titleBrush, 2, 4);
        }

        private void InitializeColorPanel()
        {
            int startX = 6;
            int startY = 22;
            int circleSize = 28;
            int spacing = 10;
            int cols = 9;

            var colorWithNames = new[]
            {
                (Color.FromArgb(244, 67, 54),  "红色"),
                (Color.FromArgb(255, 87, 34), "橙色"),
                (Color.FromArgb(255, 152, 0), "琥珀"),
                (Color.FromArgb(255, 193, 7), "黄色"),
                (Color.FromArgb(255, 235, 59), "亮黄"),
                (Color.FromArgb(139, 195, 74), "浅绿"),
                (Color.FromArgb(76, 175, 80),  "绿色"),
                (Color.FromArgb(0, 150, 136),  "青色"),
                (Color.FromArgb(0, 172, 193),  "青蓝"),
                (Color.FromArgb(33, 150, 243), "蓝色"),
                (Color.FromArgb(63, 81, 181),  "靛蓝"),
                (Color.FromArgb(156, 39, 176), "紫色"),
                (Color.FromArgb(233, 30, 99),  "玫红"),
                (Color.FromArgb(121, 85, 72),  "棕色"),
                (Color.FromArgb(96, 96, 96),   "灰色"),
                (Color.Black,                  "黑色"),
                (Color.White,                  "白色"),
                (Color.FromArgb(240, 240, 240),"浅灰"),
            };

            for (int i = 0; i < colorWithNames.Length; i++)
            {
                var (color, name) = colorWithNames[i];
                var col = i % cols;
                var row = i / cols;
                var x = startX + col * (circleSize + spacing);
                var y = startY + row * (circleSize + spacing + 4);

                var btn = new Button
                {
                    Size = new Size(circleSize, circleSize),
                    Location = new Point(x, y),
                    FlatStyle = FlatStyle.Flat,
                    FlatAppearance = { BorderSize = 0 },
                    BackColor = color,
                    Cursor = Cursors.Hand,
                    Tag = color,
                    TabStop = false
                };

                Color borderColor = (color == Color.White || color == Color.FromArgb(240, 240, 240))
                    ? Color.FromArgb(200, 200, 200)
                    : Color.FromArgb(60, 60, 60);

                var capturedName = name;
                var capturedColor = color;
                var capturedBorder = borderColor;

                btn.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    var isSelected = capturedColor.ToArgb() == _selectedColor.ToArgb();

                    var rect = new Rectangle(2, 2, circleSize - 4, circleSize - 4);
                    g.FillEllipse(new SolidBrush(capturedColor), rect);

                    if (capturedColor == Color.White || capturedColor == Color.FromArgb(240, 240, 240))
                        g.DrawEllipse(new Pen(Color.FromArgb(200, 200, 200), 1), rect);
                    else
                        g.DrawEllipse(new Pen(Color.FromArgb(60, 60, 60, 60), 1), rect);

                    if (isSelected)
                    {
                        using var selPen = new Pen(_accentColor, 3f);
                        g.DrawEllipse(selPen, new Rectangle(0, 0, circleSize - 1, circleSize - 1));

                        using var checkBrush = new SolidBrush(
                            capturedColor.GetBrightness() > 0.6 ? _accentColor : Color.White);
                        using var checkFont = new Font("Segoe UI", 11F, FontStyle.Bold);
                        var textSize = g.MeasureString("✓", checkFont);
                        g.DrawString("✓", checkFont, checkBrush,
                            (circleSize - textSize.Width) / 2,
                            (circleSize - textSize.Height) / 2);
                    }
                };

                btn.MouseHover += (s, e) =>
                {
                    _toolTip?.Dispose();
                    _toolTip = new ToolTip();
                    _toolTip.SetToolTip(btn, capturedName);
                };

                btn.Click += (s, e) =>
                {
                    _selectedColor = capturedColor;
                    _colorPanel.Invalidate();
                    foreach (Control c in _colorPanel.Controls)
                        c.Invalidate();
                    UpdatePreview();
                };

                _colorPanel.Controls.Add(btn);
            }
        }

        private ToolTip _toolTip;

        private void InitializeFontCombo()
        {
            _fontCombo.Items.Clear();
            var fonts = GetSystemFonts();

            if (fonts.Length > 8)
            {
                foreach (var f in CommonFonts)
                    if (fonts.Contains(f))
                        _fontCombo.Items.Add(f);

                _fontCombo.Items.Add(new FontSeparator());

                foreach (var f in fonts)
                    if (!CommonFonts.Contains(f))
                        _fontCombo.Items.Add(f);
            }
            else
            {
                foreach (var f in fonts)
                    _fontCombo.Items.Add(f);
            }

            for (int i = 0; i < _fontCombo.Items.Count; i++)
            {
                if (_fontCombo.Items[i] is string s &&
                    string.Equals(s, _selectedFontFamily, StringComparison.OrdinalIgnoreCase))
                {
                    _fontCombo.SelectedIndex = i;
                    return;
                }
            }
            _fontCombo.SelectedIndex = 0;
        }

        private void FontCombo_MeasureItem(object? sender, MeasureItemEventArgs e)
        {
            if (_fontCombo.Items[e.Index] is FontSeparator)
            {
                e.ItemHeight = 6;
                return;
            }
            e.ItemHeight = 28;
        }

        private void FontCombo_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            if (_fontCombo.Items[e.Index] is FontSeparator)
            {
                var midY = e.Bounds.Y + e.Bounds.Height / 2;
                using var sepPen = new Pen(Color.FromArgb(200, 200, 210));
                e.Graphics.DrawLine(sepPen, e.Bounds.X + 4, midY, e.Bounds.Right - 4, midY);
                return;
            }

            var fontName = _fontCombo.Items[e.Index].ToString() ?? "Microsoft YaHei UI";
            var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var isComboEdit = (e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit;

            var bgColor = isSelected
                ? Color.FromArgb(230, 244, 255)
                : Color.White;
            using var bgBrush = new SolidBrush(bgColor);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

            try
            {
                using var previewFont = new Font(fontName, isComboEdit ? 10f : 11f, FontStyle.Regular);
                var textColor = isSelected ? _accentColor : Color.FromArgb(50, 50, 50);
                using var textBrush = new SolidBrush(textColor);
                e.Graphics.DrawString(fontName, previewFont, textBrush,
                    e.Bounds.X + 8, e.Bounds.Y + (isComboEdit ? 2 : 4));
            }
            catch
            {
                using var fallbackFont = new Font("Microsoft YaHei UI", 11f);
                using var textBrush = new SolidBrush(isSelected ? _accentColor : Color.FromArgb(50, 50, 50));
                e.Graphics.DrawString(fontName, fallbackFont, textBrush,
                    e.Bounds.X + 8, e.Bounds.Y + 4);
            }

            e.DrawFocusRectangle();
        }

        private void FontCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_fontCombo.SelectedItem is string fontName)
            {
                _selectedFontFamily = fontName;
                UpdatePreview();
            }
        }

        private void InitializeSizePanel()
        {
            _sizePanel.Controls.Clear();
            int btnW = 48, btnH = 34;
            int spacing = 6;
            int totalW = AvailableSizes.Length * btnW + (AvailableSizes.Length - 1) * spacing;
            int startX = (_sizePanel.Width - totalW) / 2;
            if (startX < 0) startX = 0;

            for (int i = 0; i < AvailableSizes.Length; i++)
            {
                var (size, label) = AvailableSizes[i];
                var x = startX + i * (btnW + spacing);
                var isSelected = Math.Abs(size - _selectedFontSize) < 0.5f;

                var btn = new Button
                {
                    Text = label,
                    Font = new Font("Microsoft YaHei UI", size >= 28 ? 9F : 10F,
                        isSelected ? FontStyle.Bold : FontStyle.Regular),
                    Size = new Size(btnW, btnH),
                    Location = new Point(x, 6),
                    FlatStyle = FlatStyle.Flat,
                    FlatAppearance = { BorderSize = 0 },
                    BackColor = isSelected ? _accentColor : _sectionBg,
                    ForeColor = isSelected ? Color.White : _textPrimary,
                    Cursor = Cursors.Hand,
                    Tag = size,
                    TabStop = false
                };
                btn.FlatAppearance.MouseOverBackColor = isSelected ? _accentColor : Color.FromArgb(240, 240, 248);
                btn.Click += (s, e) =>
                {
                    _selectedFontSize = (float)((Button)s!).Tag!;
                    _sizeSlider.Value = Math.Max(_sizeSlider.Minimum, Math.Min(_sizeSlider.Maximum, (int)_selectedFontSize));
                    UpdateSizeButtons();
                    UpdatePreview();
                };
                _sizePanel.Controls.Add(btn);
            }
        }

        private void UpdateSizeButtons()
        {
            foreach (Control c in _sizePanel.Controls)
            {
                if (c is Button btn && btn.Tag is float size)
                {
                    var isSelected = Math.Abs(size - _selectedFontSize) < 0.5f;
                    btn.BackColor = isSelected ? _accentColor : _sectionBg;
                    btn.ForeColor = isSelected ? Color.White : _textPrimary;
                    btn.Font = new Font("Microsoft YaHei UI", size >= 28 ? 9F : 10F,
                        isSelected ? FontStyle.Bold : FontStyle.Regular);
                    btn.FlatAppearance.MouseOverBackColor = isSelected ? _accentColor : Color.FromArgb(240, 240, 248);
                }
            }
        }

        private void SizeSlider_ValueChanged(object? sender, EventArgs e)
        {
            _selectedFontSize = _sizeSlider.Value;
            _sizeValueLabel.Text = _selectedFontSize.ToString("0");
            UpdateSizeButtons();
            UpdatePreview();
        }

        private void PreviewPanel_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var bgBrush = new SolidBrush(_sectionBg);
            g.FillRectangle(bgBrush, _previewPanel.ClientRectangle);

            using var borderPen = new Pen(Color.FromArgb(220, 220, 230));
            var r = _previewPanel.ClientRectangle;
            g.DrawRectangle(borderPen, r.X, r.Y, r.Width - 1, r.Height - 1);

            using var titleFont = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold);
            using var titleBrush = new SolidBrush(_textSecondary);
            var titleSize = g.MeasureString("预览效果", titleFont);
            using var titleBgBrush = new SolidBrush(_sectionBg);
            g.FillRectangle(titleBgBrush, r.X + 12, r.Y, titleSize.Width + 8, titleSize.Height);
            g.DrawString("预览效果", titleFont, titleBrush, r.X + 16, r.Y + 2);
        }

        private void UpdatePreview()
        {
            try
            {
                var displaySize = Math.Min(_selectedFontSize, 42f);
                _previewLabel.Font = new Font(_selectedFontFamily, displaySize,
                    _selectedFontSize >= 36 ? FontStyle.Bold : FontStyle.Regular);
            }
            catch
            {
                _previewLabel.Font = new Font("Microsoft YaHei UI", 16f);
            }
            _previewLabel.ForeColor = _selectedColor;
            _previewLabel.Text = string.IsNullOrWhiteSpace(_textBox.Text)
                ? "预览文字效果"
                : _textBox.Text;
        }

        private void TextBox_TextChanged(object? sender, EventArgs e)
        {
            _labelWordCount.Text = $"字数: {_textBox.Text.Length}";
            UpdatePreview();
        }

        private static string[] GetSystemFonts()
        {
            try
            {
                using var fontCollection = new InstalledFontCollection();
                return fontCollection.Families
                    .Select(f => f.Name)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .OrderBy(name => name)
                    .ToArray();
            }
            catch
            {
                return new[] { "Microsoft YaHei UI", "Arial", "Times New Roman", "Consolas", "Segoe UI" };
            }
        }

        private class FontSeparator { }
    }
}