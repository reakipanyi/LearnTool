using LearningAssistant.Common;
using LearningAssistant.Models.Config;
using LearningAssistant.Presenters;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;

namespace LearningAssistant.Forms
{
    public partial class ContentEditorForm : Form, IContentEditorView
    {
        private readonly ILogger<ContentEditorForm> _logger;
        private readonly AppConfig _appConfig;
        private ContentEditorPresenter? _presenter;
        private TableLayoutPanel mainPanel;
        private Panel topPanel;
        private Panel promptPanel;
        private TextBox textBoxPrompt;
        private Panel gridPanel;
        private Panel jsonPanel;
        private FlowLayoutPanel buttonPanel;
        private FlowLayoutPanel rightBottomPanel;

        private GroupBox groupBoxLanguage;
        private RadioButton radioEnglish;
        private RadioButton radioChinese;
        private TextBox textBoxRange;
        private Label labelRange;
        private Label labelCategory;
        private ComboBox comboBoxSubCategory;
        private bool _disposed = false;

        public ContentEditorForm(ILogger<ContentEditorForm> logger, AppConfig appConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
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
            set
            {
                dataGridView.DataSource = value;
                ApplyChineseColumnHeaders();
            }
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

        public void SetInitialLanguage(string language)
        {
            if (language == Constants.Language.Chinese)
            {
                radioChinese.Checked = true;
            }
            else if (language == Constants.Language.English)
            {
                radioEnglish.Checked = true;
            }
        }

        public void SetInitialSubCategory(string subCategory)
        {
            if (comboBoxSubCategory.Items.Contains(subCategory))
            {
                comboBoxSubCategory.SelectedItem = subCategory;
            }
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

        private static readonly Dictionary<string, string> ColumnHeaderNames = new()
        {
            { "Character", "汉字" },
            { "Pinyin", "拼音" },
            { "Meaning", "释义" },
            { "StrokeCount", "笔画数" },
            { "Words", "组词" },
            { "Radical", "部首" },
            { "Idiom", "成语" },
            { "Origin", "出处" },
            { "Example", "例句" },
            { "Phrase", "短语" },
            { "Title", "标题" },
            { "Author", "作者" },
            { "Dynasty", "朝代" },
            { "Verses", "诗句" },
            { "Annotation", "注释" },
            { "Word", "单词" },
            { "Phonetic", "音标" },
            { "PartOfSpeech", "词性" },
            { "Sentence", "句子" },
            { "Translation", "翻译" },
            { "Grammar", "语法" },
            { "Content", "内容" },
            { "Questions", "题目" },
            { "Question", "问题" },
            { "Answer", "答案" },
            { "Analysis", "解析" }
        };


        private static string GetChineseColumnName(string columnName)
        {
            return ColumnHeaderNames.TryGetValue(columnName, out var chineseName) ? chineseName : columnName;
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
                                var column = dataTable.Columns.Add(prop.Name, typeof(string));
                                column.Caption = GetChineseColumnName(prop.Name);
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
                        ApplyChineseColumnHeaders();
                    }
                }
                else if (json.TrimStart().StartsWith("{"))
                {
                    var obj = JObject.Parse(json);
                    var dataTable = new DataTable();
                    foreach (var prop in obj.Properties())
                    {
                        var column = dataTable.Columns.Add(prop.Name, typeof(string));
                        column.Caption = GetChineseColumnName(prop.Name);
                    }
                    DataRow row = dataTable.NewRow();
                    foreach (var prop in obj.Properties())
                    {
                        row[prop.Name] = ConvertJTokenToString(prop.Value);
                    }
                    dataTable.Rows.Add(row);
                    dataGridView.DataSource = dataTable;
                    ApplyChineseColumnHeaders();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating grid from JSON");
                dataGridView.DataSource = null;
            }
        }

        private void ApplyChineseColumnHeaders()
        {
            if (dataGridView.DataSource is DataTable dataTable)
            {
                foreach (DataGridViewColumn column in dataGridView.Columns)
                {
                    if (ColumnHeaderNames.TryGetValue(column.Name, out var chineseName))
                    {
                        column.HeaderText = chineseName;
                    }
                }
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting JToken to string");
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

        // TODO: 实现树形视图刷新逻辑
        public void RefreshTreeView(TreeNodeCollection nodes)
        {
        }

        // TODO: 实现加载项目编辑逻辑
        public void LoadItemForEdit(dynamic item)
        {
        }

        public void ClearEditForm()
        {
            textBoxJson.Text = "";
            dataGridView.DataSource = null;
        }

        // TODO: 实现项目列表更新逻辑
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

        // TODO: 实现单元格双击逻辑
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
            labelCount = new Label();
            textBoxCount = new TextBox();
            dataGridView = new DataGridView();
            mainPanel = new TableLayoutPanel();
            topPanel = new Panel();
            groupBoxLanguage = new GroupBox();
            radioEnglish = new RadioButton();
            radioChinese = new RadioButton();
            textBoxRange = new TextBox();
            labelRange = new Label();
            labelCategory = new Label();
            comboBoxSubCategory = new ComboBox();
            promptPanel = new Panel();
            textBoxPrompt = new TextBox();
            gridPanel = new Panel();
            jsonPanel = new Panel();
            buttonPanel = new FlowLayoutPanel();
            rightBottomPanel = new FlowLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)dataGridView).BeginInit();
            mainPanel.SuspendLayout();
            topPanel.SuspendLayout();
            groupBoxLanguage.SuspendLayout();
            promptPanel.SuspendLayout();
            gridPanel.SuspendLayout();
            jsonPanel.SuspendLayout();
            buttonPanel.SuspendLayout();
            rightBottomPanel.SuspendLayout();
            SuspendLayout();
            // 
            // textBoxJson
            // 
            textBoxJson.BackColor = Color.White;
            textBoxJson.BorderStyle = BorderStyle.FixedSingle;
            textBoxJson.Dock = DockStyle.Fill;
            textBoxJson.Font = new Font("微软雅黑", 10F);
            textBoxJson.Location = new Point(0, 0);
            textBoxJson.Multiline = true;
            textBoxJson.Name = "textBoxJson";
            textBoxJson.ScrollBars = ScrollBars.Both;
            textBoxJson.Size = new Size(601, 530);
            textBoxJson.TabIndex = 2;
            // 
            // buttonAdd
            // 
            buttonAdd.BackColor = Color.FromArgb(255, 152, 0);
            buttonAdd.FlatAppearance.BorderSize = 0;
            buttonAdd.FlatStyle = FlatStyle.Flat;
            buttonAdd.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            buttonAdd.ForeColor = Color.White;
            buttonAdd.Location = new Point(3, 3);
            buttonAdd.Name = "buttonAdd";
            buttonAdd.Size = new Size(93, 42);
            buttonAdd.TabIndex = 0;
            buttonAdd.Text = "➕ 新增";
            buttonAdd.UseVisualStyleBackColor = false;
            buttonAdd.Click += ButtonAdd_Click;
            buttonAdd.MouseEnter += Button_HoverEnter;
            buttonAdd.MouseLeave += Button_HoverLeave;
            // 
            // buttonSave
            // 
            buttonSave.BackColor = Color.FromArgb(76, 175, 80);
            buttonSave.FlatAppearance.BorderSize = 0;
            buttonSave.FlatStyle = FlatStyle.Flat;
            buttonSave.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            buttonSave.ForeColor = Color.White;
            buttonSave.Location = new Point(102, 3);
            buttonSave.Name = "buttonSave";
            buttonSave.Size = new Size(93, 42);
            buttonSave.TabIndex = 1;
            buttonSave.Text = "💾 保存";
            buttonSave.UseVisualStyleBackColor = false;
            buttonSave.Click += ButtonSave_Click;
            buttonSave.MouseEnter += Button_HoverEnter;
            buttonSave.MouseLeave += Button_HoverLeave;
            // 
            // buttonDelete
            // 
            buttonDelete.BackColor = Color.FromArgb(244, 67, 54);
            buttonDelete.FlatAppearance.BorderSize = 0;
            buttonDelete.FlatStyle = FlatStyle.Flat;
            buttonDelete.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            buttonDelete.ForeColor = Color.White;
            buttonDelete.Location = new Point(201, 3);
            buttonDelete.Name = "buttonDelete";
            buttonDelete.Size = new Size(93, 42);
            buttonDelete.TabIndex = 2;
            buttonDelete.Text = "🗑 删除";
            buttonDelete.UseVisualStyleBackColor = false;
            buttonDelete.Click += ButtonDelete_Click;
            buttonDelete.MouseEnter += Button_HoverEnter;
            buttonDelete.MouseLeave += Button_HoverLeave;
            // 
            // buttonImport
            // 
            buttonImport.BackColor = Color.FromArgb(100, 181, 246);
            buttonImport.FlatAppearance.BorderSize = 0;
            buttonImport.FlatStyle = FlatStyle.Flat;
            buttonImport.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            buttonImport.ForeColor = Color.White;
            buttonImport.Location = new Point(300, 3);
            buttonImport.Name = "buttonImport";
            buttonImport.Size = new Size(93, 42);
            buttonImport.TabIndex = 3;
            buttonImport.Text = "📥 导入";
            buttonImport.UseVisualStyleBackColor = false;
            buttonImport.Click += ButtonImport_Click;
            buttonImport.MouseEnter += Button_HoverEnter;
            buttonImport.MouseLeave += Button_HoverLeave;
            // 
            // buttonExport
            // 
            buttonExport.BackColor = Color.FromArgb(156, 39, 176);
            buttonExport.FlatAppearance.BorderSize = 0;
            buttonExport.FlatStyle = FlatStyle.Flat;
            buttonExport.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            buttonExport.ForeColor = Color.White;
            buttonExport.Location = new Point(399, 3);
            buttonExport.Name = "buttonExport";
            buttonExport.Size = new Size(93, 42);
            buttonExport.TabIndex = 4;
            buttonExport.Text = "📤 导出";
            buttonExport.UseVisualStyleBackColor = false;
            buttonExport.Click += ButtonExport_Click;
            buttonExport.MouseEnter += Button_HoverEnter;
            buttonExport.MouseLeave += Button_HoverLeave;
            // 
            // buttonGenerateAI
            // 
            buttonGenerateAI.BackColor = Color.FromArgb(103, 58, 183);
            buttonGenerateAI.FlatAppearance.BorderSize = 0;
            buttonGenerateAI.FlatStyle = FlatStyle.Flat;
            buttonGenerateAI.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            buttonGenerateAI.ForeColor = Color.White;
            buttonGenerateAI.Location = new Point(498, 3);
            buttonGenerateAI.Name = "buttonGenerateAI";
            buttonGenerateAI.Size = new Size(93, 42);
            buttonGenerateAI.TabIndex = 5;
            buttonGenerateAI.Text = "🤖 AI生成";
            buttonGenerateAI.UseVisualStyleBackColor = false;
            buttonGenerateAI.Click += ButtonGenerateAI_Click;
            buttonGenerateAI.MouseEnter += Button_HoverEnter;
            buttonGenerateAI.MouseLeave += Button_HoverLeave;
            // 
            // labelCount
            // 
            labelCount.Font = new Font("微软雅黑", 10F);
            labelCount.ForeColor = Color.FromArgb(33, 33, 33);
            labelCount.Location = new Point(462, 0);
            labelCount.Name = "labelCount";
            labelCount.Size = new Size(80, 28);
            labelCount.TabIndex = 12;
            labelCount.Text = "生成数量:";
            labelCount.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // textBoxCount
            // 
            textBoxCount.BackColor = Color.White;
            textBoxCount.BorderStyle = BorderStyle.FixedSingle;
            textBoxCount.Font = new Font("微软雅黑", 10F);
            textBoxCount.Location = new Point(548, 3);
            textBoxCount.Name = "textBoxCount";
            textBoxCount.Size = new Size(50, 25);
            textBoxCount.TabIndex = 14;
            textBoxCount.Text = "5";
            // 
            // dataGridView
            // 
            dataGridView.AllowUserToOrderColumns = true;
            dataGridView.BorderStyle = BorderStyle.None;
            dataGridView.Dock = DockStyle.Fill;
            dataGridView.GridColor = Color.FromArgb(224, 224, 224);
            dataGridView.Location = new Point(0, 0);
            dataGridView.Name = "dataGridView";
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView.Size = new Size(600, 530);
            dataGridView.TabIndex = 1;
            dataGridView.CellEndEdit += DataGridView_CellEndEdit;
            dataGridView.RowsAdded += DataGridView_RowsAdded;
            dataGridView.SelectionChanged += DataGridView_SelectionChanged;
            // 
            // mainPanel
            // 
            mainPanel.ColumnCount = 3;
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 5F));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainPanel.Controls.Add(topPanel, 0, 0);
            mainPanel.Controls.Add(promptPanel, 0, 1);
            mainPanel.Controls.Add(gridPanel, 0, 2);
            mainPanel.Controls.Add(jsonPanel, 2, 2);
            mainPanel.Controls.Add(buttonPanel, 0, 3);
            mainPanel.Controls.Add(rightBottomPanel, 2, 3);
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Location = new Point(0, 0);
            mainPanel.Name = "mainPanel";
            mainPanel.RowCount = 4;
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 111F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 73F));
            mainPanel.Size = new Size(1218, 766);
            mainPanel.TabIndex = 0;
            // 
            // topPanel
            // 
            mainPanel.SetColumnSpan(topPanel, 3);
            topPanel.Controls.Add(groupBoxLanguage);
            topPanel.Controls.Add(textBoxRange);
            topPanel.Controls.Add(labelRange);
            topPanel.Controls.Add(labelCategory);
            topPanel.Controls.Add(comboBoxSubCategory);
            topPanel.Dock = DockStyle.Fill;
            topPanel.Location = new Point(3, 3);
            topPanel.Name = "topPanel";
            topPanel.Size = new Size(1212, 40);
            topPanel.TabIndex = 0;
            // 
            // groupBoxLanguage
            // 
            groupBoxLanguage.BackColor = Color.FromArgb(255, 250, 240);
            groupBoxLanguage.Controls.Add(radioEnglish);
            groupBoxLanguage.Controls.Add(radioChinese);
            groupBoxLanguage.FlatStyle = FlatStyle.Flat;
            groupBoxLanguage.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            groupBoxLanguage.ForeColor = Color.FromArgb(33, 33, 33);
            groupBoxLanguage.Location = new Point(7, 2);
            groupBoxLanguage.Name = "groupBoxLanguage";
            groupBoxLanguage.Size = new Size(295, 42);
            groupBoxLanguage.TabIndex = 21;
            groupBoxLanguage.TabStop = false;
            groupBoxLanguage.Text = "🌐 语言选择";
            // 
            // radioEnglish
            // 
            radioEnglish.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            radioEnglish.ForeColor = Color.FromArgb(33, 33, 33);
            radioEnglish.Location = new Point(217, 11);
            radioEnglish.Name = "radioEnglish";
            radioEnglish.Size = new Size(80, 27);
            radioEnglish.TabIndex = 2;
            radioEnglish.Text = "🇬🇧 英语";
            radioEnglish.CheckedChanged += RadioEnglish_CheckedChanged;
            // 
            // radioChinese
            // 
            radioChinese.Checked = true;
            radioChinese.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            radioChinese.ForeColor = Color.FromArgb(33, 33, 33);
            radioChinese.Location = new Point(99, 11);
            radioChinese.Name = "radioChinese";
            radioChinese.Size = new Size(80, 27);
            radioChinese.TabIndex = 1;
            radioChinese.TabStop = true;
            radioChinese.Text = "🇨🇳 中文";
            radioChinese.CheckedChanged += RadioChinese_CheckedChanged;
            // 
            // textBoxRange
            // 
            textBoxRange.BackColor = Color.White;
            textBoxRange.BorderStyle = BorderStyle.FixedSingle;
            textBoxRange.Font = new Font("微软雅黑", 10F);
            textBoxRange.Location = new Point(747, 9);
            textBoxRange.Name = "textBoxRange";
            textBoxRange.Size = new Size(200, 25);
            textBoxRange.TabIndex = 15;
            // 
            // labelRange
            // 
            labelRange.AutoSize = true;
            labelRange.Font = new Font("微软雅黑", 10F);
            labelRange.ForeColor = Color.FromArgb(33, 33, 33);
            labelRange.Location = new Point(657, 11);
            labelRange.Name = "labelRange";
            labelRange.Size = new Size(77, 20);
            labelRange.TabIndex = 13;
            labelRange.Text = "🔍 关键词:";
            // 
            // labelCategory
            // 
            labelCategory.AutoSize = true;
            labelCategory.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelCategory.ForeColor = Color.FromArgb(33, 33, 33);
            labelCategory.Location = new Point(337, 12);
            labelCategory.Name = "labelCategory";
            labelCategory.Size = new Size(93, 19);
            labelCategory.TabIndex = 10;
            labelCategory.Text = "📁 学习品类:";
            // 
            // comboBoxSubCategory
            // 
            comboBoxSubCategory.BackColor = Color.White;
            comboBoxSubCategory.FlatStyle = FlatStyle.Flat;
            comboBoxSubCategory.Font = new Font("微软雅黑", 10F);
            comboBoxSubCategory.FormattingEnabled = true;
            comboBoxSubCategory.Location = new Point(433, 8);
            comboBoxSubCategory.Name = "comboBoxSubCategory";
            comboBoxSubCategory.Size = new Size(167, 27);
            comboBoxSubCategory.TabIndex = 17;
            comboBoxSubCategory.SelectedIndexChanged += ComboBoxSubCategory_SelectedIndexChanged;
            // 
            // promptPanel
            // 
            mainPanel.SetColumnSpan(promptPanel, 3);
            promptPanel.Controls.Add(textBoxPrompt);
            promptPanel.Dock = DockStyle.Fill;
            promptPanel.Location = new Point(3, 49);
            promptPanel.Name = "promptPanel";
            promptPanel.Size = new Size(1212, 105);
            promptPanel.TabIndex = 1;
            // 
            // textBoxPrompt
            // 
            textBoxPrompt.BackColor = Color.White;
            textBoxPrompt.BorderStyle = BorderStyle.FixedSingle;
            textBoxPrompt.Dock = DockStyle.Fill;
            textBoxPrompt.Font = new Font("微软雅黑", 10F);
            textBoxPrompt.ForeColor = Color.FromArgb(33, 33, 33);
            textBoxPrompt.Location = new Point(0, 0);
            textBoxPrompt.Multiline = true;
            textBoxPrompt.Name = "textBoxPrompt";
            textBoxPrompt.ScrollBars = ScrollBars.Both;
            textBoxPrompt.Size = new Size(1212, 105);
            textBoxPrompt.TabIndex = 18;
            textBoxPrompt.Text = "💡 AI生成提示词将显示在这里...";
            // 
            // gridPanel
            // 
            gridPanel.Controls.Add(dataGridView);
            gridPanel.Dock = DockStyle.Fill;
            gridPanel.Location = new Point(3, 160);
            gridPanel.Name = "gridPanel";
            gridPanel.Size = new Size(600, 530);
            gridPanel.TabIndex = 2;
            // 
            // jsonPanel
            // 
            jsonPanel.Controls.Add(textBoxJson);
            jsonPanel.Dock = DockStyle.Fill;
            jsonPanel.Location = new Point(614, 160);
            jsonPanel.Name = "jsonPanel";
            jsonPanel.Size = new Size(601, 530);
            jsonPanel.TabIndex = 3;
            // 
            // buttonPanel
            // 
            buttonPanel.Controls.Add(buttonAdd);
            buttonPanel.Controls.Add(buttonSave);
            buttonPanel.Controls.Add(buttonDelete);
            buttonPanel.Controls.Add(buttonImport);
            buttonPanel.Controls.Add(buttonExport);
            buttonPanel.Controls.Add(buttonGenerateAI);
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.Location = new Point(3, 696);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Size = new Size(600, 67);
            buttonPanel.TabIndex = 4;
            buttonPanel.WrapContents = false;
            // 
            // rightBottomPanel
            // 
            rightBottomPanel.Controls.Add(textBoxCount);
            rightBottomPanel.Controls.Add(labelCount);
            rightBottomPanel.Dock = DockStyle.Fill;
            rightBottomPanel.FlowDirection = FlowDirection.RightToLeft;
            rightBottomPanel.Location = new Point(614, 696);
            rightBottomPanel.Name = "rightBottomPanel";
            rightBottomPanel.Size = new Size(601, 67);
            rightBottomPanel.TabIndex = 5;
            rightBottomPanel.WrapContents = false;
            // 
            // ContentEditorForm
            // 
            BackColor = Color.FromArgb(255, 244, 230);
            ClientSize = new Size(1218, 766);
            Controls.Add(mainPanel);
            Name = "ContentEditorForm";
            Text = "📝 内容编辑器";
            ((System.ComponentModel.ISupportInitialize)dataGridView).EndInit();
            mainPanel.ResumeLayout(false);
            topPanel.ResumeLayout(false);
            topPanel.PerformLayout();
            groupBoxLanguage.ResumeLayout(false);
            promptPanel.ResumeLayout(false);
            promptPanel.PerformLayout();
            gridPanel.ResumeLayout(false);
            jsonPanel.ResumeLayout(false);
            jsonPanel.PerformLayout();
            buttonPanel.ResumeLayout(false);
            rightBottomPanel.ResumeLayout(false);
            rightBottomPanel.PerformLayout();
            ResumeLayout(false);
        }


        private DataGridView dataGridView;

        private TextBox textBoxJson;
        private Button buttonAdd;
        private Button buttonSave;
        private Button buttonDelete;
        private Button buttonImport;
        private Button buttonExport;
        private Button buttonGenerateAI;
        private Label labelCount;
        private TextBox textBoxCount;

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // 释放托管资源
                // 如果有需要释放的资源，可以在这里添加
                // 例如：_presenter?.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        private void Button_HoverEnter(object? sender, EventArgs e)
        {
            if (sender is Button button)
            {
                button.BackColor = Color.FromArgb(
                    Math.Min(255, button.BackColor.R + 25),
                    Math.Min(255, button.BackColor.G + 25),
                    Math.Min(255, button.BackColor.B + 25));
                button.Cursor = Cursors.Hand;
            }
        }

        private void Button_HoverLeave(object? sender, EventArgs e)
        {
            if (sender is Button button)
            {
                var originalColor = button.Name switch
                {
                    "buttonAdd" => ThemeHelper.Colors.Orange,
                    "buttonSave" => ThemeHelper.Colors.Success,
                    "buttonDelete" => ThemeHelper.Colors.Error,
                    "buttonImport" => ThemeHelper.Colors.SoftBlue,
                    "buttonExport" => ThemeHelper.Colors.Purple,
                    "buttonGenerateAI" => ThemeHelper.Colors.PurpleDark,
                    _ => SystemColors.Control
                };
                button.BackColor = originalColor;
            }
        }
    }
}
