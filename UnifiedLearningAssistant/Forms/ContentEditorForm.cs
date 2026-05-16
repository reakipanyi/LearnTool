using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Presenters;
using UnifiedLearningAssistant.Views;

namespace UnifiedLearningAssistant.Forms
{
    public partial class ContentEditorForm : Form, IContentEditorView
    {
        private readonly ILogger<ContentEditorForm> _logger;
        private ContentEditorPresenter? _presenter;
        private GroupBox groupBoxLanguage;
        private RadioButton radioChinese;
        private RadioButton radioEnglish;
        private ComboBox comboBoxSubCategory;
        private bool _disposed = false;

        public ContentEditorForm(ILogger<ContentEditorForm> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeComponent();
        }





        public void SetPresenter(ContentEditorPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _presenter.Initialize();
            _logger.LogInformation("ContentEditorPresenter 已设置并初始化");
        }

        public string SelectedLanguage => radioChinese.Checked ? Constants.Language.Chinese : Constants.Language.English;

        public string SelectedSubCategory => comboBoxSubCategory.SelectedItem?.ToString() ?? "";
        public DataTable ItemData
        {
            set => dataGridView.DataSource = value;
        }

        public string CurrentEditItemJson
        {
            get => textBoxJson.Text;
            set
            {
                textBoxJson.Text = value;
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

        public string PromptText
        {
            get => textBoxPrompt?.Text ?? "";
            set
            {
                if (textBoxPrompt != null)
                {
                    textBoxPrompt.Text = value;
                }
            }
        }

        public object? GridDataSource
        {
            get => dataGridView.DataSource;
            set => dataGridView.DataSource = value;
        }

        public List<int> SelectedRowIndices => dataGridView.SelectedRows.Cast<DataGridViewRow>().Select(row => row.Index).ToList();


        public event EventHandler? LanguageChanged;
        public event EventHandler? SubCategoryChanged;
        public event EventHandler? TemplateAddClicked;
        public event EventHandler? TemplateSaveClicked;
        public event EventHandler? TemplateDeleteClicked;
        public event EventHandler? ImportClicked;
        public event EventHandler? ExportClicked;
        public event EventHandler? GenerateWithAIClicked;
        public event EventHandler? GridCellEndEdit;
        public event EventHandler? GridRowsAdded;

        public event EventHandler? ItemSelected;
        public void ShowMessage(string msg)
        {
            MessageBox.Show(msg);
        }

        public void RefreshSubCategories(IEnumerable<string> subCategories)
        {
            comboBoxSubCategory.Items.Clear();
            foreach (string subCategory in subCategories)
            {
                comboBoxSubCategory.Items.Add(subCategory);
            }
            if (comboBoxSubCategory.Items.Count > 0)
            {
                comboBoxSubCategory.SelectedIndex = 0;
            }
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
                    // 直接将 JSON 数组转换为 DataTable
                    var jsonArray = JsonConvert.DeserializeObject<JArray>(json);
                    if (jsonArray.Count > 0)
                    {
                        var dataTable = new DataTable();
                        var firstItem = jsonArray[0] as JObject;
                        if (firstItem != null)
                        {
                            // 从第一个对象添加列，所有列都使用 string 类型以避免类型不匹配
                            foreach (var prop in firstItem.Properties())
                            {
                                dataTable.Columns.Add(prop.Name, typeof(string));
                            }

                            // 添加所有行
                            foreach (var item in jsonArray)
                            {
                                var obj = item as JObject;
                                if (obj != null)
                                {
                                    var row = dataTable.NewRow();
                                    foreach (var prop in obj.Properties())
                                    {
                                        if (dataTable.Columns.Contains(prop.Name))
                                        {
                                            row[prop.Name] = ConvertJTokenToString(prop.Value);
                                        }
                                    }
                                    dataTable.Rows.Add(row);
                                }
                            }
                        }
                        dataGridView.DataSource = dataTable;
                    }
                }
                else if (json.TrimStart().StartsWith("{"))
                {
                    var obj = JObject.Parse(json);
                    var dataTable = new DataTable();
                    foreach (var prop in obj.Properties())
                    {
                        dataTable.Columns.Add(prop.Name, typeof(string));
                    }
                    DataRow row = dataTable.NewRow();
                    foreach (var prop in obj.Properties())
                    {
                        row[prop.Name] = ConvertJTokenToString(prop.Value);
                    }
                    dataTable.Rows.Add(row);
                    dataGridView.DataSource = dataTable;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating grid from JSON");
                dataGridView.DataSource = null;
            }
        }

        /// <summary>
        /// 将 JToken 转换为安全的字符串，特别处理数组和对象
        /// </summary>
        private string ConvertJTokenToString(JToken? token)
        {
            if (token == null)
                return "";

            try
            {
                if (token.Type == JTokenType.Array)
                {
                    // 将数组转换为逗号分隔的字符串
                    var array = token as JArray;
                    if (array != null)
                    {
                        var values = array.Select(x => x.ToString());
                        return string.Join(", ", values);
                    }
                }
                else if (token.Type == JTokenType.Object)
                {
                    // 将对象转换为 JSON 字符串
                    return token.ToString(Formatting.None);
                }
            }
            catch
            {
                // 如果转换失败，返回空字符串
            }

            return token?.ToString() ?? "";
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

        public void RefreshTreeView(TreeNodeCollection nodes)
        {
        }

        public void LoadItemForEdit(dynamic item)
        {
        }

        public void ClearEditForm()
        {
            textBoxJson.Text = "";
            dataGridView.DataSource = null;
        }
        public void UpdateItemList()
        {
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

        private void RadioChinese_CheckedChanged(object? sender, EventArgs e)
        {
            if (radioChinese.Checked)
                LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RadioEnglish_CheckedChanged(object? sender, EventArgs e)
        {
            if (radioEnglish.Checked)
                LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ComboBoxSubCategory_SelectedIndexChanged(object? sender, EventArgs e)
        {
            SubCategoryChanged?.Invoke(this, EventArgs.Empty);
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

        private void DataGridView_SelectionChanged(object sender, EventArgs e)
        {
            ItemSelected?.Invoke(this, EventArgs.Empty);
        }
        private void ButtonGenerateAI_Click(object? sender, EventArgs e)
        {
            GenerateWithAIClicked?.Invoke(this, EventArgs.Empty);
        }

        private void InitializeComponent()
        {
            textBoxJson = new TextBox();
            buttonAdd = new Button();
            buttonSave = new Button();
            buttonDelete = new Button();
            buttonImport = new Button();
            buttonExport = new Button();
            buttonGenerateAI = new Button();
            labelCategory = new Label();
            labelCount = new Label();
            labelRange = new Label();
            textBoxCount = new TextBox();
            textBoxRange = new TextBox();
            dataGridView = new DataGridView();
            radioChinese = new RadioButton();
            radioEnglish = new RadioButton();
            comboBoxSubCategory = new ComboBox();
            textBoxPrompt = new TextBox();
            labelPrompt = new Label();
            groupBoxLanguage = new GroupBox();
            ((System.ComponentModel.ISupportInitialize)dataGridView).BeginInit();
            groupBoxLanguage.SuspendLayout();
            SuspendLayout();
            // 
            // textBoxJson
            // 
            textBoxJson.BackColor = SystemColors.HighlightText;
            textBoxJson.Location = new Point(613, 194);
            textBoxJson.Multiline = true;
            textBoxJson.Name = "textBoxJson";
            textBoxJson.ScrollBars = ScrollBars.Both;
            textBoxJson.Size = new Size(591, 452);
            textBoxJson.TabIndex = 2;
            // 
            // buttonAdd
            // 
            buttonAdd.Location = new Point(100, 681);
            buttonAdd.Name = "buttonAdd";
            buttonAdd.Size = new Size(102, 44);
            buttonAdd.TabIndex = 3;
            buttonAdd.Text = "📝 新增";
            buttonAdd.Click += ButtonAdd_Click;
            // 
            // buttonSave
            // 
            buttonSave.Location = new Point(254, 681);
            buttonSave.Name = "buttonSave";
            buttonSave.Size = new Size(102, 44);
            buttonSave.TabIndex = 4;
            buttonSave.Text = "💾 保存";
            buttonSave.Click += ButtonSave_Click;
            // 
            // buttonDelete
            // 
            buttonDelete.Location = new Point(408, 681);
            buttonDelete.Name = "buttonDelete";
            buttonDelete.Size = new Size(102, 44);
            buttonDelete.TabIndex = 5;
            buttonDelete.Text = "🗑️ 删除";
            buttonDelete.Click += ButtonDelete_Click;
            // 
            // buttonImport
            // 
            buttonImport.Location = new Point(562, 681);
            buttonImport.Name = "buttonImport";
            buttonImport.Size = new Size(102, 44);
            buttonImport.TabIndex = 6;
            buttonImport.Text = "📥 导入";
            buttonImport.Click += ButtonImport_Click;
            // 
            // buttonExport
            // 
            buttonExport.Location = new Point(716, 681);
            buttonExport.Name = "buttonExport";
            buttonExport.Size = new Size(102, 44);
            buttonExport.TabIndex = 7;
            buttonExport.Text = "📤 导出";
            buttonExport.Click += ButtonExport_Click;
            // 
            // buttonGenerateAI
            // 
            buttonGenerateAI.Location = new Point(866, 681);
            buttonGenerateAI.Name = "buttonGenerateAI";
            buttonGenerateAI.Size = new Size(102, 44);
            buttonGenerateAI.TabIndex = 9;
            buttonGenerateAI.Text = "🤖 AI生成";
            buttonGenerateAI.Click += ButtonGenerateAI_Click;
            // 
            // labelCategory
            // 
            labelCategory.Location = new Point(279, 39);
            labelCategory.Name = "labelCategory";
            labelCategory.Size = new Size(80, 20);
            labelCategory.TabIndex = 10;
            labelCategory.Text = "学习品类:";
            // 
            // labelCount
            // 
            labelCount.Location = new Point(1000, 695);
            labelCount.Name = "labelCount";
            labelCount.Size = new Size(60, 20);
            labelCount.TabIndex = 12;
            labelCount.Text = "生成数量:";
            // 
            // labelRange
            // 
            labelRange.Location = new Point(574, 37);
            labelRange.Name = "labelRange";
            labelRange.Size = new Size(60, 20);
            labelRange.TabIndex = 13;
            labelRange.Text = "关键词或范围:";
            // 
            // textBoxCount
            // 
            textBoxCount.Location = new Point(1060, 692);
            textBoxCount.Name = "textBoxCount";
            textBoxCount.Size = new Size(50, 23);
            textBoxCount.TabIndex = 14;
            textBoxCount.Text = "5";
            // 
            // textBoxRange
            // 
            textBoxRange.Location = new Point(640, 34);
            textBoxRange.Name = "textBoxRange";
            textBoxRange.Size = new Size(546, 23);
            textBoxRange.TabIndex = 15;
            // 
            // dataGridView
            // 
            dataGridView.AllowUserToOrderColumns = true;
            dataGridView.Location = new Point(5, 194);
            dataGridView.Name = "dataGridView";
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView.Size = new Size(602, 452);
            dataGridView.TabIndex = 1;
            dataGridView.CellEndEdit += DataGridView_CellEndEdit;
            dataGridView.RowsAdded += DataGridView_RowsAdded;
            dataGridView.SelectionChanged += DataGridView_SelectionChanged;
            // 
            // radioChinese
            // 
            radioChinese.Checked = true;
            radioChinese.Location = new Point(36, 11);
            radioChinese.Name = "radioChinese";
            radioChinese.Size = new Size(80, 27);
            radioChinese.TabIndex = 1;
            radioChinese.TabStop = true;
            radioChinese.Text = "中文";
            radioChinese.CheckedChanged += RadioChinese_CheckedChanged;
            // 
            // radioEnglish
            // 
            radioEnglish.Location = new Point(126, 11);
            radioEnglish.Name = "radioEnglish";
            radioEnglish.Size = new Size(80, 27);
            radioEnglish.TabIndex = 2;
            radioEnglish.Text = "英语";
            radioEnglish.CheckedChanged += RadioEnglish_CheckedChanged;
            // 
            // comboBoxSubCategory
            // 
            comboBoxSubCategory.FormattingEnabled = true;
            comboBoxSubCategory.Location = new Point(352, 34);
            comboBoxSubCategory.Name = "comboBoxSubCategory";
            comboBoxSubCategory.Size = new Size(180, 25);
            comboBoxSubCategory.TabIndex = 17;
            comboBoxSubCategory.SelectedIndexChanged += ComboBoxSubCategory_SelectedIndexChanged;
            // 
            // textBoxPrompt
            // 
            textBoxPrompt.BackColor = Color.LightYellow;
            textBoxPrompt.Location = new Point(85, 93);
            textBoxPrompt.Multiline = true;
            textBoxPrompt.Name = "textBoxPrompt";
            textBoxPrompt.ScrollBars = ScrollBars.Both;
            textBoxPrompt.Size = new Size(1119, 86);
            textBoxPrompt.TabIndex = 18;
            textBoxPrompt.Text = "AI生成提示词将显示在这里...";
            // 
            // labelPrompt
            // 
            labelPrompt.Location = new Point(5, 96);
            labelPrompt.Name = "labelPrompt";
            labelPrompt.Size = new Size(80, 20);
            labelPrompt.TabIndex = 19;
            labelPrompt.Text = "提示词:";
            // 
            // groupBoxLanguage
            // 
            groupBoxLanguage.Controls.Add(radioEnglish);
            groupBoxLanguage.Controls.Add(radioChinese);
            groupBoxLanguage.Location = new Point(25, 21);
            groupBoxLanguage.Name = "groupBoxLanguage";
            groupBoxLanguage.Size = new Size(250, 42);
            groupBoxLanguage.TabIndex = 20;
            groupBoxLanguage.TabStop = false;
            groupBoxLanguage.Text = "语言";
            // 
            // ContentEditorForm
            // 
            ClientSize = new Size(1218, 766);
            Controls.Add(groupBoxLanguage);
            Controls.Add(comboBoxSubCategory);
            Controls.Add(textBoxJson);
            Controls.Add(dataGridView);
            Controls.Add(buttonAdd);
            Controls.Add(buttonSave);
            Controls.Add(buttonDelete);
            Controls.Add(buttonImport);
            Controls.Add(buttonExport);
            Controls.Add(buttonGenerateAI);
            Controls.Add(labelCategory);
            Controls.Add(labelCount);
            Controls.Add(labelRange);
            Controls.Add(textBoxCount);
            Controls.Add(textBoxRange);
            Controls.Add(textBoxPrompt);
            Controls.Add(labelPrompt);
            Name = "ContentEditorForm";
            Text = "📝 内容编辑器";
            ((System.ComponentModel.ISupportInitialize)dataGridView).EndInit();
            groupBoxLanguage.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }
        private DataGridView dataGridView;

        private TextBox textBoxJson;
        private Button buttonAdd;
        private Button buttonSave;
        private Button buttonDelete;
        private Button buttonImport;
        private Button buttonExport;
        private Button buttonGenerateAI;
        private Label labelCategory;
        private Label labelJson;
        private Label labelCount;
        private Label labelRange;
        private TextBox textBoxCount;
        private TextBox textBoxRange;
        private TextBox textBoxPrompt;
        private Label labelPrompt;

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
