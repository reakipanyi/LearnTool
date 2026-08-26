using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls.Common
{
    /// <summary>
    /// 文本注解对话框结果
    /// </summary>
    public class TextAnnotationDialogResult
    {
        public string Text { get; set; } = string.Empty;
        public Color SelectedColor { get; set; } = Color.Red;
        public float FontSize { get; set; } = 16f;
        public string FontFamily { get; set; } = "Microsoft YaHei UI";
        public bool Confirmed { get; set; }
    }

    /// <summary>
    /// 文本注解对话框 - 用于创建和编辑PDF文本注解
    /// </summary>
    public partial class TextAnnotationDialog : Form
    {
        #region 控件字段统一声明
        private TextBox _textBox;
        private ComboBox _colorCombo;
        private ComboBox _sizeCombo;
        private ComboBox _fontCombo;
        private Label _labelWordCount;
        private Button _okButton;
        private Button _cancelButton;
        private Label _labelContent;
        private Label _labelColor;
        private Label _labelSize;
        private Label _labelFont;
        #endregion

        /// <summary>
        /// 预设的颜色选项
        /// </summary>
        public static readonly Color[] AvailableColors =
        {
            Color.RoyalBlue, Color.LimeGreen, Color.Orange, Color.Red, Color.Black, Color.White
        };

        /// <summary>
        /// 预设的字号选项
        /// </summary>
        public static readonly (string Display, float Size)[] AvailableSizes =
        {
            ("小 (12)", 12f),
            ("中 (16)", 16f),
            ("大 (20)", 20f),
            ("特大 (28)", 28f)
        };

        /// <summary>
        /// 初始文本内容
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string InitialText
        {
            get => _textBox.Text;
            set => _textBox.Text = value;
        }

        /// <summary>
        /// 选中的颜色
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SelectedColor
        {
            get
            {
                var index = _colorCombo.SelectedIndex;
                return index >= 0 && index < AvailableColors.Length
                    ? AvailableColors[index]
                    : Color.Red;
            }
            set
            {
                var index = Array.IndexOf(AvailableColors, value);
                _colorCombo.SelectedIndex = index >= 0 ? index : 3;
            }
        }

        /// <summary>
        /// 选中的字号
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float SelectedFontSize
        {
            get
            {
                var index = _sizeCombo.SelectedIndex;
                return index >= 0 && index < AvailableSizes.Length
                    ? AvailableSizes[index].Size
                    : 16f;
            }
            set
            {
                for (int i = 0; i < AvailableSizes.Length; i++)
                {
                    if (Math.Abs(AvailableSizes[i].Size - value) < 1)
                    {
                        _sizeCombo.SelectedIndex = i;
                        return;
                    }
                }
                _sizeCombo.SelectedIndex = 1;
            }
        }

        /// <summary>
        /// 选中的字体
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedFontFamily
        {
            get => _fontCombo.SelectedItem?.ToString() ?? "Microsoft YaHei UI";
            set
            {
                for (int i = 0; i < _fontCombo.Items.Count; i++)
                {
                    if (string.Equals(_fontCombo.Items[i].ToString(), value, StringComparison.OrdinalIgnoreCase))
                    {
                        _fontCombo.SelectedIndex = i;
                        return;
                    }
                }
                _fontCombo.SelectedIndex = 0;
            }
        }

        private IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="initialText">初始文本（用于编辑模式）</param>
        /// <param name="initialColor">初始颜色</param>
        /// <param name="initialFontSize">初始字号</param>
        /// <param name="initialFontFamily">初始字体</param>
        public TextAnnotationDialog(
            string title = "文字注解",
            string initialText = "",
            Color? initialColor = null,
            float initialFontSize = 16f,
            string? initialFontFamily = null)
        {
            InitializeComponent(title);
            InitialText = initialText;
            SelectedColor = initialColor ?? Color.Red;
            SelectedFontSize = initialFontSize;
            if (!string.IsNullOrEmpty(initialFontFamily))
                SelectedFontFamily = initialFontFamily;
        }

        #region Windows Form Designer generated code
        private void InitializeComponent(string formTitle)
        {
            this._labelContent = new Label();
            this._textBox = new TextBox();
            this._labelWordCount = new Label();
            this._labelColor = new Label();
            this._colorCombo = new ComboBox();
            this._labelFont = new Label();
            this._fontCombo = new ComboBox();
            this._labelSize = new Label();
            this._sizeCombo = new ComboBox();
            this._okButton = new Button();
            this._cancelButton = new Button();
            this.SuspendLayout();

            // 
            // _labelContent
            // 
            this._labelContent.Text = "请输入文字内容：";
            this._labelContent.Location = new Point(20, 15);
            this._labelContent.Size = new Size(150, 20);
            this._labelContent.Name = "_labelContent";
            this._labelContent.TabIndex = 0;

            // 
            // _textBox
            // 
            this._textBox.Location = new Point(20, 40);
            this._textBox.Size = new Size(360, 80);
            this._textBox.Multiline = true;
            this._textBox.ScrollBars = ScrollBars.Vertical;
            this._textBox.Name = "_textBox";
            this._textBox.TabIndex = 1;
            this._textBox.TextChanged += TextBox_TextChanged;

            // 
            // _labelWordCount
            // 
            this._labelWordCount.Text = "字数: 0";
            this._labelWordCount.Location = new Point(20, 125);
            this._labelWordCount.Size = new Size(150, 20);
            this._labelWordCount.Name = "_labelWordCount";
            this._labelWordCount.ForeColor = Color.Gray;
            this._labelWordCount.TabIndex = 8;

            // 
            // _labelColor
            // 
            this._labelColor.Text = "颜色：";
            this._labelColor.Location = new Point(20, 155);
            this._labelColor.Size = new Size(40, 20);
            this._labelColor.Name = "_labelColor";
            this._labelColor.TabIndex = 2;

            // 
            // _colorCombo
            // 
            this._colorCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            this._colorCombo.Location = new Point(60, 152);
            this._colorCombo.Size = new Size(80, 25);
            this._colorCombo.Items.AddRange(new object[] { "蓝色", "绿色", "橙色", "红色", "黑色", "白色" });
            this._colorCombo.SelectedIndex = 3;
            this._colorCombo.Name = "_colorCombo";
            this._colorCombo.TabIndex = 3;

            // 
            // _labelFont
            // 
            this._labelFont.Text = "字体：";
            this._labelFont.Location = new Point(150, 155);
            this._labelFont.Size = new Size(40, 20);
            this._labelFont.Name = "_labelFont";
            this._labelFont.TabIndex = 6;

            // 
            // _fontCombo
            // 
            this._fontCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            this._fontCombo.Location = new Point(190, 152);
            this._fontCombo.Size = new Size(190, 25);
            this._fontCombo.Name = "_fontCombo";
            this._fontCombo.TabIndex = 7;
            this._fontCombo.Items.AddRange(GetSystemFonts());
            this._fontCombo.SelectedIndex = 0;

            // 
            // _labelSize
            // 
            this._labelSize.Text = "字号：";
            this._labelSize.Location = new Point(20, 190);
            this._labelSize.Size = new Size(40, 20);
            this._labelSize.Name = "_labelSize";
            this._labelSize.TabIndex = 4;

            // 
            // _sizeCombo
            // 
            this._sizeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            this._sizeCombo.Location = new Point(60, 187);
            this._sizeCombo.Size = new Size(100, 25);
            this._sizeCombo.Items.AddRange(AvailableSizes.Select(s => s.Display).ToArray());
            this._sizeCombo.SelectedIndex = 1;
            this._sizeCombo.Name = "_sizeCombo";
            this._sizeCombo.TabIndex = 5;

            // 
            // _okButton
            // 
            this._okButton.Text = "确定";
            this._okButton.Location = new Point(240, 230);
            this._okButton.Size = new Size(70, 30);
            this._okButton.DialogResult = DialogResult.OK;
            this._okButton.Name = "_okButton";
            this._okButton.TabIndex = 6;

            // 
            // _cancelButton
            // 
            this._cancelButton.Text = "取消";
            this._cancelButton.Location = new Point(320, 230);
            this._cancelButton.Size = new Size(70, 30);
            this._cancelButton.DialogResult = DialogResult.Cancel;
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.TabIndex = 7;

            // 
            // TextAnnotationDialog
            // 
            this.Text = formTitle;
            this.Size = new Size(420, 310);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.AcceptButton = this._okButton;
            this.CancelButton = this._cancelButton;
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._sizeCombo);
            this.Controls.Add(this._labelSize);
            this.Controls.Add(this._fontCombo);
            this.Controls.Add(this._labelFont);
            this.Controls.Add(this._colorCombo);
            this.Controls.Add(this._labelColor);
            this.Controls.Add(this._labelWordCount);
            this.Controls.Add(this._textBox);
            this.Controls.Add(this._labelContent);
            this.Name = "TextAnnotationDialog";

            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion

        /// <summary>
        /// 获取系统字体列表
        /// </summary>
        private static string[] GetSystemFonts()
        {
            try
            {
                using var fontCollection = new System.Drawing.Text.InstalledFontCollection();
                return fontCollection
                    .Families
                    .Select(f => f.Name)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .OrderBy(name => name)
                    .ToArray();
            }
            catch
            {
                return new[] { "Microsoft YaHei UI", "Arial", "Times New Roman", "Consolas" };
            }
        }

        /// <summary>
        /// 文本框内容变化时更新字数统计
        /// </summary>
        private void TextBox_TextChanged(object? sender, EventArgs e)
        {
            UpdateWordCount();
        }

        /// <summary>
        /// 更新字数统计显示
        /// </summary>
        private void UpdateWordCount()
        {
            string text = _textBox.Text;
            int charCount = text.Length;
            int wordCount = 0;

            if (!string.IsNullOrWhiteSpace(text))
            {
                // 检测是否包含中文字符（中文按字数统计）
                bool hasChinese = text.Any(c => c >= 0x4E00 && c <= 0x9FFF);
                if (hasChinese)
                {
                    // 中文字符按字符数，英文单词按空格分隔
                    int chineseChars = text.Count(c => c >= 0x4E00 && c <= 0x9FFF);
                    int nonChineseWords = text
                        .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                        .Sum(s => s.Count(c => c < 0x4E00 || c > 0x9FFF) > 0 ? 1 : 0);
                    wordCount = chineseChars + nonChineseWords;
                }
                else
                {
                    wordCount = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                }
            }

            _labelWordCount.Text = $"字数: {charCount} | 词数: {wordCount}";
        }

        /// <summary>
        /// 显示对话框并获取结果
        /// </summary>
        public new TextAnnotationDialogResult ShowDialog(IWin32Window? owner = null)
        {
            var result = new TextAnnotationDialogResult();

            if (base.ShowDialog(owner) == DialogResult.OK && !string.IsNullOrWhiteSpace(_textBox.Text))
            {
                result.Text = _textBox.Text;
                result.SelectedColor = SelectedColor;
                result.FontSize = SelectedFontSize;
                result.FontFamily = SelectedFontFamily;
                result.Confirmed = true;
            }
            else
            {
                result.Confirmed = false;
            }

            return result;
        }
    }
}