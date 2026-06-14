using LearningAssistant.Common;
using LearningAssistant.Models.Config;
using LearningAssistant.Presenters;
using LearningAssistant.Services;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Data;

namespace LearningAssistant.Forms
{
    public partial class ContentEditorForm : Form, IContentEditorView
    {
        private readonly ILogger<ContentEditorForm> _logger;
        private readonly AppConfig _appConfig;
        private readonly IAIPanelPopupService _aiPanelPopupService;
        private readonly Services.Learning.IPendingContentService? _pendingContentService;
        private ContentEditorPresenter? _presenter;
        private TableLayoutPanel mainPanel;
        private Panel topPanel;
        private Panel gridPanel;
        private FlowLayoutPanel buttonPanel;

        private Panel groupBoxLanguage;
        private RadioButton radioEnglish;
        private RadioButton radioChinese;
        private Label labelCategory;
        private ComboBox comboBoxSubCategory;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private bool _disposed = false;

        public ContentEditorForm(ILogger<ContentEditorForm> logger, AppConfig appConfig, IAIPanelPopupService aiPanelPopupService, Services.Learning.IPendingContentService? pendingContentService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _aiPanelPopupService = aiPanelPopupService ?? throw new ArgumentNullException(nameof(aiPanelPopupService));
            _pendingContentService = pendingContentService;
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
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataTable ItemData
        {
            set
            {
                dataGridView.DataSource = value;
                ApplyChineseColumnHeaders();
            }
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]

        public string CurrentEditItemJson
        {
            get => textBoxJson.Text;
            set
            {
                textBoxJson.Text = value;
            }
        }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]

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

            // 获取当前编辑的内容作为上下文
            string context = textBoxJson.Text.Trim();
            string prompt;

            if (string.IsNullOrEmpty(context))
            {
                prompt = "请帮我生成学习内容，格式为JSON数组，每个元素包含 content（内容）和 displayText（显示文本）字段。";
            }
            else
            {
                prompt = $"请帮我完善或扩展以下学习内容：\n\n{context}";
            }

            // 使用AI面板服务显示AIAbilityPanel，传递提示词和上下文
            _aiPanelPopupService.ShowAIAbilityPanel(this, prompt, null, context);
        }

        private void ButtonLoadPending_Click(object? sender, EventArgs e)
        {
            if (_pendingContentService == null)
            {
                ShowMessage("待添加内容服务未初始化");
                return;
            }

            var pendingItems = _pendingContentService.GetAll();
            if (pendingItems.Count == 0)
            {
                ShowMessage("没有待添加的学习内容");
                return;
            }

            // 切换到JSON编辑标签页
            tabControl1.SelectedIndex = 1;

            // 将待添加内容追加到JSON文本框
            foreach (var item in pendingItems)
            {
                if (!string.IsNullOrEmpty(textBoxJson.Text))
                {
                    textBoxJson.Text += "," + Environment.NewLine;
                }
                textBoxJson.Text += item.Content;
            }

            // 清除已加载的内容
            _pendingContentService.Clear();

            ShowMessage($"已加载 {pendingItems.Count} 条待添加内容到JSON编辑区，请编辑后保存");
        }


        private void InitializeComponent()
        {
            textBoxJson = new TextBox();
            buttonAdd = new Button();
            buttonSave = new Button();
            buttonDelete = new Button();
            buttonImport = new Button();
            buttonExport = new Button();
            dataGridView = new DataGridView();
            mainPanel = new TableLayoutPanel();
            topPanel = new Panel();
            groupBoxLanguage = new Panel();
            radioEnglish = new RadioButton();
            radioChinese = new RadioButton();
            labelCategory = new Label();
            comboBoxSubCategory = new ComboBox();
            gridPanel = new Panel();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            tabPage2 = new TabPage();
            buttonPanel = new FlowLayoutPanel();
            // 移除空的AIWebView标签页 tabPage3
            ((ISupportInitialize)dataGridView).BeginInit();
            mainPanel.SuspendLayout();
            topPanel.SuspendLayout();
            groupBoxLanguage.SuspendLayout();
            gridPanel.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            buttonPanel.SuspendLayout();
            SuspendLayout();
            // 
            // textBoxJson
            // 
            textBoxJson.BackColor = Color.White;
            textBoxJson.BorderStyle = BorderStyle.FixedSingle;
            textBoxJson.Dock = DockStyle.Fill;
            textBoxJson.Font = new Font("微软雅黑", 10F);
            textBoxJson.Location = new Point(3, 3);
            textBoxJson.Multiline = true;
            textBoxJson.Name = "textBoxJson";
            textBoxJson.ScrollBars = ScrollBars.Both;
            textBoxJson.Size = new Size(1198, 605);
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
            buttonGenerateAI = new Button();
            buttonGenerateAI.BackColor = Color.FromArgb(96, 125, 139);
            buttonGenerateAI.FlatAppearance.BorderSize = 0;
            buttonGenerateAI.FlatStyle = FlatStyle.Flat;
            buttonGenerateAI.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            buttonGenerateAI.ForeColor = Color.White;
            buttonGenerateAI.Location = new Point(498, 3);
            buttonGenerateAI.Name = "buttonGenerateAI";
            buttonGenerateAI.Size = new Size(110, 42);
            buttonGenerateAI.TabIndex = 5;
            buttonGenerateAI.Text = "🤖 AI生成";
            buttonGenerateAI.UseVisualStyleBackColor = false;
            buttonGenerateAI.Click += ButtonGenerateAI_Click;
            buttonGenerateAI.MouseEnter += Button_HoverEnter;
            buttonGenerateAI.MouseLeave += Button_HoverLeave;
            //
            // buttonLoadPending
            //
            var buttonLoadPending = new Button();
            buttonLoadPending.BackColor = Color.FromArgb(255, 152, 0);
            buttonLoadPending.FlatAppearance.BorderSize = 0;
            buttonLoadPending.FlatStyle = FlatStyle.Flat;
            buttonLoadPending.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            buttonLoadPending.ForeColor = Color.White;
            buttonLoadPending.Location = new Point(614, 3);
            buttonLoadPending.Name = "buttonLoadPending";
            buttonLoadPending.Size = new Size(130, 42);
            buttonLoadPending.TabIndex = 6;
            buttonLoadPending.Text = "📥 加载待添加";
            buttonLoadPending.UseVisualStyleBackColor = false;
            buttonLoadPending.Click += ButtonLoadPending_Click;
            buttonLoadPending.MouseEnter += Button_HoverEnter;
            buttonLoadPending.MouseLeave += Button_HoverLeave;
            //
            // dataGridView
            // 
            dataGridView.AllowUserToOrderColumns = true;
            dataGridView.BorderStyle = BorderStyle.None;
            dataGridView.Dock = DockStyle.Fill;
            dataGridView.GridColor = Color.FromArgb(224, 224, 224);
            dataGridView.Location = new Point(3, 3);
            dataGridView.Name = "dataGridView";
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView.Size = new Size(1198, 605);
            dataGridView.TabIndex = 1;
            dataGridView.CellEndEdit += DataGridView_CellEndEdit;
            dataGridView.RowsAdded += DataGridView_RowsAdded;
            dataGridView.SelectionChanged += DataGridView_SelectionChanged;
            // 
            // mainPanel
            // 
            mainPanel.ColumnCount = 1;
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            mainPanel.Controls.Add(topPanel, 0, 0);
            mainPanel.Controls.Add(gridPanel, 0, 1);
            mainPanel.Controls.Add(buttonPanel, 0, 2);
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Location = new Point(0, 0);
            mainPanel.Name = "mainPanel";
            mainPanel.RowCount = 3;
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 73F));
            mainPanel.Size = new Size(1218, 766);
            mainPanel.TabIndex = 0;
            // 
            // topPanel
            // 
            topPanel.Controls.Add(groupBoxLanguage);
            topPanel.Controls.Add(labelCategory);
            topPanel.Controls.Add(comboBoxSubCategory);
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
            groupBoxLanguage.Dock = DockStyle.Left;
            groupBoxLanguage.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            groupBoxLanguage.ForeColor = Color.FromArgb(33, 33, 33);
            groupBoxLanguage.Location = new Point(0, 0);
            groupBoxLanguage.Name = "groupBoxLanguage";
            groupBoxLanguage.Size = new Size(328, 40);
            groupBoxLanguage.TabIndex = 21;
            // 
            // radioEnglish
            // 
            radioEnglish.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            radioEnglish.ForeColor = Color.FromArgb(33, 33, 33);
            radioEnglish.Location = new Point(151, 8);
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
            radioChinese.Location = new Point(33, 8);
            radioChinese.Name = "radioChinese";
            radioChinese.Size = new Size(80, 27);
            radioChinese.TabIndex = 1;
            radioChinese.TabStop = true;
            radioChinese.Text = "🇨🇳 中文";
            radioChinese.CheckedChanged += RadioChinese_CheckedChanged;
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
            comboBoxSubCategory.Location = new Point(427, 9);
            comboBoxSubCategory.Name = "comboBoxSubCategory";
            comboBoxSubCategory.Size = new Size(167, 27);
            comboBoxSubCategory.TabIndex = 17;
            comboBoxSubCategory.SelectedIndexChanged += ComboBoxSubCategory_SelectedIndexChanged;
            // 
            // gridPanel
            // 
            gridPanel.Controls.Add(tabControl1);
            gridPanel.Dock = DockStyle.Fill;
            gridPanel.Location = new Point(3, 49);
            gridPanel.Name = "gridPanel";
            gridPanel.Size = new Size(1212, 641);
            gridPanel.TabIndex = 2;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            // 移除空的AIWebView标签页
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1212, 641);
            tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(dataGridView);
            tabPage1.Location = new Point(4, 26);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(1204, 611);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "表格";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(textBoxJson);
            tabPage2.Location = new Point(4, 26);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(1204, 611);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Json";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // buttonPanel
            // 
            buttonPanel.Controls.Add(buttonAdd);
            buttonPanel.Controls.Add(buttonSave);
            buttonPanel.Controls.Add(buttonDelete);
            buttonPanel.Controls.Add(buttonImport);
            buttonPanel.Controls.Add(buttonExport);
            buttonPanel.Controls.Add(buttonGenerateAI);
            buttonPanel.Controls.Add(buttonLoadPending);
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.Location = new Point(3, 696);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Size = new Size(1212, 67);
            buttonPanel.TabIndex = 4;
            buttonPanel.WrapContents = false;
            // 
            // ContentEditorForm
            // 
            BackColor = Color.FromArgb(255, 244, 230);
            ClientSize = new Size(1218, 766);
            Controls.Add(mainPanel);
            Name = "ContentEditorForm";
            Text = "📝 内容编辑器";
            ((ISupportInitialize)dataGridView).EndInit();
            mainPanel.ResumeLayout(false);
            topPanel.ResumeLayout(false);
            topPanel.PerformLayout();
            groupBoxLanguage.ResumeLayout(false);
            gridPanel.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            buttonPanel.ResumeLayout(false);
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
                    "buttonGenerateAI" => Color.FromArgb(96, 125, 139),
                    _ => SystemColors.Control
                };
                button.BackColor = originalColor;
            }
        }
    }
}
