using System.Windows.Forms;

namespace LearningAssistant.Forms.UserControls
{
    /// <summary>
    /// 文本注解对话框结果
    /// </summary>
    public class TextAnnotationDialogResult
    {
        public string Text { get; set; } = string.Empty;
        public Color SelectedColor { get; set; } = Color.Red;
        public float FontSize { get; set; } = 16f;
        public bool Confirmed { get; set; }
    }

    /// <summary>
    /// 文本注解对话框 - 用于创建和编辑PDF文本注解
    /// </summary>
    public class TextAnnotationDialog : Form
    {
        private readonly TextBox _textBox;
        private readonly ComboBox _colorCombo;
        private readonly ComboBox _sizeCombo;
        private readonly Button _okButton;
        private readonly Button _cancelButton;

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
        public string InitialText
        {
            get => _textBox.Text;
            set => _textBox.Text = value;
        }

        /// <summary>
        /// 选中的颜色
        /// </summary>
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
                _sizeCombo.SelectedIndex = 1; // 默认中号
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="initialText">初始文本（用于编辑模式）</param>
        /// <param name="initialColor">初始颜色</param>
        /// <param name="initialFontSize">初始字号</param>
        public TextAnnotationDialog(
            string title = "文字注解", 
            string initialText = "", 
            Color? initialColor = null, 
            float initialFontSize = 16f)
        {
            InitializeComponents(title);
            
            _textBox.Text = initialText;
            SelectedColor = initialColor ?? Color.Red;
            SelectedFontSize = initialFontSize;
        }

        private void InitializeComponents(string title)
        {
            Text = title;
            Size = new Size(420, 260);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            // 文本标签和输入框
            var label = new Label
            {
                Text = "请输入文字内容：",
                Location = new Point(20, 15),
                Size = new Size(150, 20)
            };

            _textBox = new TextBox
            {
                Location = new Point(20, 40),
                Size = new Size(360, 80),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            // 颜色选择
            var colorLabel = new Label
            {
                Text = "颜色：",
                Location = new Point(20, 135),
                Size = new Size(40, 20)
            };

            _colorCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(60, 132),
                Size = new Size(100, 25)
            };
            _colorCombo.Items.AddRange(new object[] { "蓝色", "绿色", "橙色", "红色", "黑色", "白色" });
            _colorCombo.SelectedIndex = 3;

            // 字号选择
            var sizeLabel = new Label
            {
                Text = "字号：",
                Location = new Point(180, 135),
                Size = new Size(40, 20)
            };

            _sizeCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(220, 132),
                Size = new Size(100, 25)
            };
            _sizeCombo.Items.AddRange(AvailableSizes.Select(s => s.Display).ToArray());
            _sizeCombo.SelectedIndex = 1;

            // 按钮
            _okButton = new Button
            {
                Text = "确定",
                Location = new Point(240, 180),
                Size = new Size(70, 30),
                DialogResult = DialogResult.OK
            };

            _cancelButton = new Button
            {
                Text = "取消",
                Location = new Point(320, 180),
                Size = new Size(70, 30),
                DialogResult = DialogResult.Cancel
            };

            // 添加控件
            Controls.AddRange(new Control[]
            {
                label, _textBox, colorLabel, _colorCombo, sizeLabel, _sizeCombo,
                _okButton, _cancelButton
            });

            AcceptButton = _okButton;
            CancelButton = _cancelButton;
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
