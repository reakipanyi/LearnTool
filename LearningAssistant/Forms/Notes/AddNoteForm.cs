using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.Notes
{
    public class AddNoteForm : Form
    {
        private readonly INoteService _noteService;
        private readonly string _userId;

        private TextBox _textBoxTitle;
        private ComboBox _comboBoxCategory;
        private TextBox _textBoxTags;
        private RichTextBox _richTextBoxContent;
        private Button _buttonSave;
        private Button _buttonCancel;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title
        {
            get => _textBoxTitle.Text;
            set => _textBoxTitle.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Category
        {
            get => _comboBoxCategory.SelectedItem?.ToString() ?? "学习笔记";
            set
            {
                int index = _comboBoxCategory.FindStringExact(value);
                if (index >= 0)
                    _comboBoxCategory.SelectedIndex = index;
            }
        }

        public AddNoteForm(INoteService noteService, string userId)
        {
            _noteService = noteService;
            _userId = userId;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "快速记笔记";
            this.Size = new Size(450, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            this.SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint, true);

            int y = 15;
            int labelWidth = 70;
            int controlWidth = 340;
            int controlHeight = 30;

            Label labelTitle = new Label
            {
                Text = "标题:",
                Font = new Font("微软雅黑", 10F),
                Location = new Point(20, y),
                Size = new Size(labelWidth, controlHeight),
                TextAlign = ContentAlignment.MiddleRight
            };

            _textBoxTitle = new TextBox
            {
                Font = new Font("微软雅黑", 10F),
                Location = new Point(95, y),
                Size = new Size(controlWidth, controlHeight),
                Margin = new Padding(3)
            };
            y += controlHeight + 10;

            Label labelCategory = new Label
            {
                Text = "分类:",
                Font = new Font("微软雅黑", 10F),
                Location = new Point(20, y),
                Size = new Size(labelWidth, controlHeight),
                TextAlign = ContentAlignment.MiddleRight
            };

            _comboBoxCategory = new ComboBox
            {
                Font = new Font("微软雅黑", 10F),
                Location = new Point(95, y),
                Size = new Size(controlWidth, controlHeight),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _comboBoxCategory.Items.AddRange(new[] { "学习笔记", "读书笔记", "错题分析", "心得感悟", "其他" });
            _comboBoxCategory.SelectedIndex = 0;
            y += controlHeight + 10;

            Label labelTags = new Label
            {
                Text = "标签:",
                Font = new Font("微软雅黑", 10F),
                Location = new Point(20, y),
                Size = new Size(labelWidth, controlHeight),
                TextAlign = ContentAlignment.MiddleRight
            };

            _textBoxTags = new TextBox
            {
                Font = new Font("微软雅黑", 10F),
                Location = new Point(95, y),
                Size = new Size(controlWidth, controlHeight),
                PlaceholderText = "多个标签用逗号分隔"
            };
            y += controlHeight + 15;

            Label labelContent = new Label
            {
                Text = "内容:",
                Font = new Font("微软雅黑", 10F),
                Location = new Point(20, y),
                Size = new Size(labelWidth, controlHeight),
                TextAlign = ContentAlignment.MiddleRight
            };

            _richTextBoxContent = new RichTextBox
            {
                Font = new Font("微软雅黑", 10F),
                Location = new Point(95, y),
                Size = new Size(controlWidth, 200),
                BorderStyle = BorderStyle.FixedSingle
            };
            y += 200 + 15;

            _buttonSave = new Button
            {
                Text = "保存",
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(270, y),
                Size = new Size(80, 35)
            };
            ApplyRoundedStyle(_buttonSave, 6);
            _buttonSave.Click += ButtonSave_Click;

            _buttonCancel = new Button
            {
                Text = "取消",
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(360, y),
                Size = new Size(80, 35)
            };
            ApplyRoundedStyle(_buttonCancel, 6);
            _buttonCancel.Click += ButtonCancel_Click;

            this.Controls.Add(labelTitle);
            this.Controls.Add(_textBoxTitle);
            this.Controls.Add(labelCategory);
            this.Controls.Add(_comboBoxCategory);
            this.Controls.Add(labelTags);
            this.Controls.Add(_textBoxTags);
            this.Controls.Add(labelContent);
            this.Controls.Add(_richTextBoxContent);
            this.Controls.Add(_buttonSave);
            this.Controls.Add(_buttonCancel);

            this.AcceptButton = _buttonSave;
            this.CancelButton = _buttonCancel;
        }

        private void ApplyRoundedStyle(Button button, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(button.Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(button.Width - radius, button.Height - radius, radius, radius, 0, 90);
            path.AddArc(0, button.Height - radius, radius, radius, 90, 90);
            path.CloseAllFigures();
            button.Region = new Region(path);
        }

        private void ButtonSave_Click(object? sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_textBoxTitle.Text))
                {
                    MessageBox.Show("请输入笔记标题", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var tags = _textBoxTags.Text.Split(',')
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();

                var note = new NoteItem
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = _userId,
                    Title = _textBoxTitle.Text.Trim(),
                    Content = _richTextBoxContent.Text,
                    Category = Category,
                    Tags = tags,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsFavorite = false
                };

                _noteService.AddNote(_userId, note);
                MessageBox.Show("笔记保存成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存笔记失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonCancel_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        }
    }
}