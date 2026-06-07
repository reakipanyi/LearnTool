using LearningAssistant.Common;

namespace LearningAssistant.Forms
{
    /// <summary>
    /// 窗体预览工具 - 启动界面
    /// </summary>
    public partial class FormPreviewTool : Form
    {
        public FormPreviewTool()
        {
            InitializeComponent();
            PopulateFormList();
        }

        private void InitializeComponent()
        {
            label = new Label();
            listBoxForms = new ListBox();
            buttonPreview = new Button();
            buttonClose = new Button();
            SuspendLayout();
            // 
            // label
            // 
            label.Location = new Point(0, 0);
            label.Name = "label";
            label.Size = new Size(100, 23);
            label.TabIndex = 0;
            // 
            // listBoxForms
            // 
            listBoxForms.ItemHeight = 17;
            listBoxForms.Location = new Point(0, 0);
            listBoxForms.Name = "listBoxForms";
            listBoxForms.Size = new Size(120, 89);
            listBoxForms.TabIndex = 1;
            listBoxForms.DoubleClick += ListBoxForms_DoubleClick;
            // 
            // buttonPreview
            // 
            buttonPreview.Location = new Point(0, 0);
            buttonPreview.Name = "buttonPreview";
            buttonPreview.Size = new Size(75, 23);
            buttonPreview.TabIndex = 2;
            buttonPreview.Click += ButtonPreview_Click;
            // 
            // buttonClose
            // 
            buttonClose.Location = new Point(0, 0);
            buttonClose.Name = "buttonClose";
            buttonClose.Size = new Size(75, 23);
            buttonClose.TabIndex = 3;
            buttonClose.Click += ButtonClose_Click;
            // 
            // FormPreviewTool
            // 
            ClientSize = new Size(484, 361);
            Controls.Add(label);
            Controls.Add(listBoxForms);
            Controls.Add(buttonPreview);
            Controls.Add(buttonClose);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormPreviewTool";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "窗体预览工具";
            ResumeLayout(false);
        }

        private Label label;
        private Button buttonPreview;
        private Button buttonClose;
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
        private void ButtonClose_Click(object? sender, EventArgs e)
        {
            Close();
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
