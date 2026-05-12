using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Views;

namespace UnifiedLearningAssistant.Forms
{
    public partial class ContentEditorForm : Form, IContentEditorView
    {
        private readonly ILogger<ContentEditorForm> _logger;
        private bool _disposed = false;

        public ContentEditorForm(ILogger<ContentEditorForm> logger)
        {
            InitializeComponent();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string SelectedCategory => "";

        public string SelectedSubCategory => comboBoxCategory.SelectedItem?.ToString() ?? "";

        public string CurrentEditItemJson
        {
            get => textBoxJson.Text;
            set
            {
                textBoxJson.Text = value;
                UpdateGridFromJson();
            }
        }

        public string GenerateCount
        {
            get => textBoxCount.Text;
            set => textBoxCount.Text = value;
        }

        public string GenerateRange
        {
            get => textBoxRange.Text;
            set => textBoxRange.Text = value;
        }

        public object? GridDataSource
        {
            get => dataGridView.DataSource;
            set => dataGridView.DataSource = value;
        }

        public int[] SelectedRowIndices
        {
            get
            {
                return dataGridView.SelectedRows.Cast<DataGridViewRow>()
                    .Select(r => r.Index)
                    .OrderBy(i => i)
                    .ToArray();
            }
        }

        public event EventHandler? CategoryChanged;
        public event EventHandler? TemplateAddClicked;
        public event EventHandler? TemplateSaveClicked;
        public event EventHandler? TemplateDeleteClicked;
        public event EventHandler? ImportClicked;
        public event EventHandler? ExportClicked;
        public event EventHandler? GenerateWithAIClicked;
        public event EventHandler? InsertTemplateClicked;
        public event EventHandler? GridCellEndEdit;
        public event EventHandler? GridRowsAdded;

        public void ShowMessage(string msg)
        {
            MessageBox.Show(msg);
        }

        public void ClearEditForm()
        {
            textBoxJson.Text = "";
            dataGridView.DataSource = null;
        }

        public void AppendJson(string json)
        {
            if (!string.IsNullOrEmpty(textBoxJson.Text))
            {
                textBoxJson.Text += "," + Environment.NewLine;
            }
            textBoxJson.Text += json;
            UpdateGridFromJson();
        }

        public void UpdateGridFromJson()
        {
            string json = textBoxJson.Text;
            if (string.IsNullOrWhiteSpace(json))
            {
                dataGridView.DataSource = null;
                return;
            }

            try
            {
                if (json.TrimStart().StartsWith("["))
                {
                    var dataTable = JsonConvert.DeserializeObject<DataTable>(json);
                    dataGridView.DataSource = dataTable;
                }
                else if (json.TrimStart().StartsWith("{"))
                {
                    var obj = JObject.Parse(json);
                    var dataTable = new DataTable();
                    foreach (var prop in obj.Properties())
                    {
                        dataTable.Columns.Add(prop.Name);
                    }
                    DataRow row = dataTable.NewRow();
                    foreach (var prop in obj.Properties())
                    {
                        row[prop.Name] = prop.Value?.ToString() ?? "";
                    }
                    dataTable.Rows.Add(row);
                    dataGridView.DataSource = dataTable;
                }
            }
            catch
            {
                dataGridView.DataSource = null;
            }
        }

        private void DataGridView_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            GridCellEndEdit?.Invoke(this, EventArgs.Empty);
        }

        private void DataGridView_RowsAdded(object? sender, DataGridViewRowsAddedEventArgs e)
        {
            GridRowsAdded?.Invoke(this, EventArgs.Empty);
        }

        private void DataGridView_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
        }

        private void ComboBoxCategory_SelectedIndexChanged(object? sender, EventArgs e)
        {
            CategoryChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonAdd_Click(object? sender, EventArgs e)
        {
            TemplateAddClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonSave_Click(object? sender, EventArgs e)
        {
            TemplateSaveClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonDelete_Click(object? sender, EventArgs e)
        {
            TemplateDeleteClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonImport_Click(object? sender, EventArgs e)
        {
            ImportClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonExport_Click(object? sender, EventArgs e)
        {
            ExportClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonGenerateAI_Click(object? sender, EventArgs e)
        {
            GenerateWithAIClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonInsertTemplate_Click(object? sender, EventArgs e)
        {
            InsertTemplateClicked?.Invoke(this, EventArgs.Empty);
        }

        private void InitializeComponent()
        {
            comboBoxCategory = new ComboBox();
            textBoxJson = new TextBox();
            buttonAdd = new Button();
            buttonSave = new Button();
            buttonDelete = new Button();
            buttonImport = new Button();
            buttonExport = new Button();
            buttonInsertTemplate = new Button();
            buttonGenerateAI = new Button();
            labelCategory = new Label();
            labelJson = new Label();
            labelCount = new Label();
            labelRange = new Label();
            textBoxCount = new TextBox();
            textBoxRange = new TextBox();
            dataGridView = new DataGridView();
            SuspendLayout();

            // dataGridView
            dataGridView.AllowUserToAddRows = true;
            dataGridView.AllowUserToDeleteRows = true;
            dataGridView.AllowUserToOrderColumns = true;
            dataGridView.Location = new Point(20, 110);
            dataGridView.Name = "dataGridView";
            dataGridView.Size = new Size(880, 300);
            dataGridView.TabIndex = 1;
            dataGridView.MultiSelect = true;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView.CellEndEdit += DataGridView_CellEndEdit;
            dataGridView.RowsAdded += DataGridView_RowsAdded;

            // comboBoxCategory
            comboBoxCategory.Items.AddRange(new object[] { 
                Constants.SubCategory.ChineseCharacter, 
                Constants.SubCategory.ChineseWordCombination, 
                Constants.SubCategory.ChineseIdiom, 
                Constants.SubCategory.ChinesePhrase, 
                Constants.SubCategory.ChinesePoem, 
                Constants.SubCategory.EnglishWord, 
                Constants.SubCategory.EnglishPhrase, 
                Constants.SubCategory.EnglishSentence 
            });
            comboBoxCategory.Location = new Point(100, 20);
            comboBoxCategory.Name = "comboBoxCategory";
            comboBoxCategory.Size = new Size(180, 25);
            comboBoxCategory.TabIndex = 0;
            comboBoxCategory.SelectedIndexChanged += ComboBoxCategory_SelectedIndexChanged;

            // textBoxJson - 改为一行高度
            textBoxJson.Location = new Point(100, 60);
            textBoxJson.Name = "textBoxJson";
            textBoxJson.Size = new Size(800, 23);
            textBoxJson.TabIndex = 2;
            textBoxJson.ScrollBars = ScrollBars.Horizontal;

            // buttonAdd
            buttonAdd.Location = new Point(20, 430);
            buttonAdd.Name = "buttonAdd";
            buttonAdd.Size = new Size(80, 35);
            buttonAdd.TabIndex = 3;
            buttonAdd.Text = "📝 新增";
            buttonAdd.Click += ButtonAdd_Click;

            // buttonSave
            buttonSave.Location = new Point(110, 430);
            buttonSave.Name = "buttonSave";
            buttonSave.Size = new Size(80, 35);
            buttonSave.TabIndex = 4;
            buttonSave.Text = "💾 保存";
            buttonSave.Click += ButtonSave_Click;

            // buttonDelete
            buttonDelete.Location = new Point(200, 430);
            buttonDelete.Name = "buttonDelete";
            buttonDelete.Size = new Size(80, 35);
            buttonDelete.TabIndex = 5;
            buttonDelete.Text = "🗑️ 删除";
            buttonDelete.Click += ButtonDelete_Click;

            // buttonImport
            buttonImport.Location = new Point(290, 430);
            buttonImport.Name = "buttonImport";
            buttonImport.Size = new Size(80, 35);
            buttonImport.TabIndex = 6;
            buttonImport.Text = "📥 导入";
            buttonImport.Click += ButtonImport_Click;

            // buttonExport
            buttonExport.Location = new Point(380, 430);
            buttonExport.Name = "buttonExport";
            buttonExport.Size = new Size(80, 35);
            buttonExport.TabIndex = 7;
            buttonExport.Text = "📤 导出";
            buttonExport.Click += ButtonExport_Click;

            // buttonInsertTemplate
            buttonInsertTemplate.Location = new Point(470, 430);
            buttonInsertTemplate.Name = "buttonInsertTemplate";
            buttonInsertTemplate.Size = new Size(100, 35);
            buttonInsertTemplate.TabIndex = 8;
            buttonInsertTemplate.Text = "📋 插入模板";
            buttonInsertTemplate.Click += ButtonInsertTemplate_Click;

            // buttonGenerateAI
            buttonGenerateAI.Location = new Point(580, 430);
            buttonGenerateAI.Name = "buttonGenerateAI";
            buttonGenerateAI.Size = new Size(90, 35);
            buttonGenerateAI.TabIndex = 9;
            buttonGenerateAI.Text = "🤖 AI生成";
            buttonGenerateAI.Click += ButtonGenerateAI_Click;

            // labelCategory
            labelCategory.Location = new Point(20, 23);
            labelCategory.Name = "labelCategory";
            labelCategory.Size = new Size(80, 20);
            labelCategory.TabIndex = 10;
            labelCategory.Text = "学习品类:";

            // labelJson
            labelJson.Location = new Point(20, 63);
            labelJson.Name = "labelJson";
            labelJson.Size = new Size(80, 20);
            labelJson.TabIndex = 11;
            labelJson.Text = "JSON预览:";

            // labelCount
            labelCount.Location = new Point(300, 23);
            labelCount.Name = "labelCount";
            labelCount.Size = new Size(60, 20);
            labelCount.TabIndex = 12;
            labelCount.Text = "生成数量:";

            // labelRange
            labelRange.Location = new Point(420, 23);
            labelRange.Name = "labelRange";
            labelRange.Size = new Size(60, 20);
            labelRange.TabIndex = 13;
            labelRange.Text = "关键词:";

            // textBoxCount
            textBoxCount.Location = new Point(360, 20);
            textBoxCount.Name = "textBoxCount";
            textBoxCount.Size = new Size(50, 23);
            textBoxCount.TabIndex = 14;
            textBoxCount.Text = "5";

            // textBoxRange
            textBoxRange.Location = new Point(480, 20);
            textBoxRange.Name = "textBoxRange";
            textBoxRange.Size = new Size(500, 23);
            textBoxRange.TabIndex = 15;
            textBoxRange.Text = "请输入关键词或范围";

            // ContentEditorForm
            ClientSize = new Size(920, 500);
            Controls.Add(comboBoxCategory);
            Controls.Add(textBoxJson);
            Controls.Add(dataGridView);
            Controls.Add(buttonAdd);
            Controls.Add(buttonSave);
            Controls.Add(buttonDelete);
            Controls.Add(buttonImport);
            Controls.Add(buttonExport);
            Controls.Add(buttonInsertTemplate);
            Controls.Add(buttonGenerateAI);
            Controls.Add(labelCategory);
            Controls.Add(labelJson);
            Controls.Add(labelCount);
            Controls.Add(labelRange);
            Controls.Add(textBoxCount);
            Controls.Add(textBoxRange);
            Name = "ContentEditorForm";
            Text = "📝 内容编辑器";
            ResumeLayout(false);
            PerformLayout();
        }

        private ComboBox comboBoxCategory;
        private TextBox textBoxJson;
        private DataGridView dataGridView;
        private Button buttonAdd;
        private Button buttonSave;
        private Button buttonDelete;
        private Button buttonImport;
        private Button buttonExport;
        private Button buttonInsertTemplate;
        private Button buttonGenerateAI;
        private Label labelCategory;
        private Label labelJson;
        private Label labelCount;
        private Label labelRange;
        private TextBox textBoxCount;
        private TextBox textBoxRange;

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // 组件已在 designer 生成的代码中处理
            }

            _disposed = true;
            base.Dispose(disposing);
        }
    }
}
