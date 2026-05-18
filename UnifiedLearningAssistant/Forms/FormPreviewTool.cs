using UnifiedLearningAssistant.Common;

namespace UnifiedLearningAssistant.Forms
{
    /// <summary>
    /// 窗体预览工具 - 启动界面
    /// </summary>
    public partial class FormPreviewTool : Form
    {
        public FormPreviewTool()
        {
            InitializeComponent();
            ApplyTheme();
            PopulateFormList();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 窗体属性
            this.Text = "窗体预览工具";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 标签
            var label = new Label
            {
                Text = "选择要预览的窗体：",
                Location = new Point(20, 20),
                Size = new Size(200, 25),
                Font = ThemeHelper.Fonts.LargeBold
            };

            // 列表框
            listBoxForms = new ListBox
            {
                Location = new Point(20, 50),
                Size = new Size(440, 200),
                Font = ThemeHelper.Fonts.Default
            };
            listBoxForms.DoubleClick += ListBoxForms_DoubleClick;

            // 预览按钮
            var buttonPreview = new Button
            {
                Text = "预览窗体",
                Location = new Point(20, 270),
                Size = new Size(210, 40),
                BackColor = ThemeHelper.Colors.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = ThemeHelper.Fonts.LargeBold
            };
            buttonPreview.Click += ButtonPreview_Click;
            ThemeHelper.AddButtonHoverEffect(buttonPreview, ThemeHelper.Colors.Primary);

            // 关闭按钮
            var buttonClose = new Button
            {
                Text = "关闭",
                Location = new Point(250, 270),
                Size = new Size(210, 40),
                BackColor = ThemeHelper.Colors.Gray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = ThemeHelper.Fonts.LargeBold
            };
            buttonClose.Click += (s, e) => Close();
            ThemeHelper.AddButtonHoverEffect(buttonClose, ThemeHelper.Colors.Gray);

            // 添加控件
            this.Controls.AddRange(new Control[] { label, listBoxForms, buttonPreview, buttonClose });

            this.ResumeLayout(false);
        }

        private ListBox listBoxForms = null!;

        private void ApplyTheme()
        {
            this.BackColor = ThemeHelper.Colors.WarmBackground;
        }

        private void PopulateFormList()
        {
            // var forms = FormPreviewer.GetPreviewableForms();
            // listBoxForms.Items.AddRange(forms);
            // if (forms.Length > 0)
            //     listBoxForms.SelectedIndex = 0;
        }

        private void ListBoxForms_DoubleClick(object? sender, EventArgs e)
        {
            ShowSelectedForm();
        }

        private void ButtonPreview_Click(object? sender, EventArgs e)
        {
            ShowSelectedForm();
        }

        private void ShowSelectedForm()
        {//
         // if (listBoxForms.SelectedItem is string formName)
         // {
         //     try
         //     {
         //         var form = FormPreviewer.CreatePreviewForm(formName);
         //         form.Show();
         //     }
         //     catch (Exception ex)
         //     {
         //         MessageBox.Show(
         //             $"无法预览窗体: {ex.Message}\n\n{ex.StackTrace}",
         //             "预览错误",
         //             MessageBoxButtons.OK,
         //             MessageBoxIcon.Error
         //         );
         //     }
         // }
        }
    }
}
