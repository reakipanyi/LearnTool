using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
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
    public partial class ContentEditorForm : Form, IContentEditorView, IThemeable
    {
        private readonly ILogger<ContentEditorForm> _logger;
        private readonly AppConfig _appConfig;
        private readonly IAIPanelPopupService _aiPanelPopupService;
        private readonly Services.Learning.IPendingContentService? _pendingContentService;
        private readonly IThemeService _themeService;
        private TableLayoutPanel mainPanel;
        private Panel topPanel;
        private Panel gridPanel;
        private FlowLayoutPanel buttonPanel;

        private Panel groupBoxSubject;
        private ComboBox comboBoxSubject;
        private Label labelCategory;
        private ComboBox comboBoxSubCategory;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private bool _disposed = false;

        public ContentEditorForm(ILogger<ContentEditorForm> logger, AppConfig appConfig, IAIPanelPopupService aiPanelPopupService, IThemeService themeService, Services.Learning.IPendingContentService? pendingContentService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _aiPanelPopupService = aiPanelPopupService ?? throw new ArgumentNullException(nameof(aiPanelPopupService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _pendingContentService = pendingContentService;
            InitializeComponent();

            InitSubjectComboBox();

            _themeService.RegisterThemeable(this);
        }

        private void InitSubjectComboBox()
        {
            comboBoxSubject.Items.Clear();
            var subjects = new List<string>
            {
                Constants.Subject.Chinese,
                Constants.Subject.English,
                Constants.Subject.Math,
                Constants.Subject.Physics,
                Constants.Subject.Chemistry,
                Constants.Subject.History,
                Constants.Subject.Geography,
                Constants.Subject.Biology
            };
            foreach (var subject in subjects)
            {
                comboBoxSubject.Items.Add(subject);
            }
            if (comboBoxSubject.Items.Count > 0)
                comboBoxSubject.SelectedIndex = 1; // 默认选英语
        }


        public void SetPresenter(ContentEditorPresenter presenter)
        {
            if (presenter == null) throw new ArgumentNullException(nameof(presenter));
            presenter.Initialize();
            _logger.LogInformation("ContentEditorPresenter 已设置并初始化");
        }



        public string SelectedSubject => comboBoxSubject.SelectedItem?.ToString() ?? "";

        public string SelectedLanguage
        {
            get
            {
                if (SelectedSubject == Constants.Subject.Chinese) return Constants.Language.Chinese;
                if (SelectedSubject == Constants.Subject.English) return Constants.Language.English;
                return Constants.Language.Chinese;
            }
        }

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
        public event EventHandler? SubjectChanged;
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
                SetInitialSubject(Constants.Subject.Chinese);
            }
            else if (language == Constants.Language.English)
            {
                SetInitialSubject(Constants.Subject.English);
            }
        }

        public void SetInitialSubject(string subject)
        {
            var index = comboBoxSubject.Items.IndexOf(subject);
            if (index >= 0)
            {
                comboBoxSubject.SelectedIndex = index;
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

        private static readonly Dictionary<string, Dictionary<string, string>> CategoryColumnHeaders = new()
        {
            {
                Constants.SubCategory.ChineseCharacter, new Dictionary<string, string>
                {
                    { "Character", "汉字" }, { "Pinyin", "拼音" }, { "Meaning", "释义" },
                    { "StrokeCount", "笔画数" }, { "Radical", "部首" }, { "StrokeOrder", "笔顺" },
                    { "Words", "组词" }
                }
            },
            {
                Constants.SubCategory.ChineseIdiom, new Dictionary<string, string>
                {
                    { "Idiom", "成语" }, { "Pinyin", "拼音" }, { "Meaning", "释义" },
                    { "Origin", "出处" }, { "Example", "例句" }
                }
            },
            {
                Constants.SubCategory.ChinesePhrase, new Dictionary<string, string>
                {
                    { "Phrase", "短语" }, { "Pinyin", "拼音" }, { "Meaning", "释义" },
                    { "Example", "例句" }
                }
            },
            {
                Constants.SubCategory.ChinesePoem, new Dictionary<string, string>
                {
                    { "Title", "诗名" }, { "Author", "作者" }, { "Dynasty", "朝代" },
                    { "Verses", "诗句" }, { "Annotation", "注释" }
                }
            },
            {
                Constants.SubCategory.ChineseComprehensive, new Dictionary<string, string>
                {
                    { "Title", "课文标题" }, { "Content", "课文内容" }, { "Questions", "课后习题" },
                    { "Question", "题目" }, { "Answer", "答案" }, { "Analysis", "解析" }
                }
            },
            {
                Constants.SubCategory.EnglishWord, new Dictionary<string, string>
                {
                    { "Word", "单词" }, { "Phonetic", "音标" }, { "PartOfSpeech", "词性" },
                    { "SyllableBreakdown", "音节拼读" }, { "Meaning", "中文释义" }, { "Example", "例句" }
                }
            },
            {
                Constants.SubCategory.EnglishPhrase, new Dictionary<string, string>
                {
                    { "Phrase", "短语" }, { "Meaning", "中文释义" }, { "Example", "例句" }
                }
            },
            {
                Constants.SubCategory.EnglishSentence, new Dictionary<string, string>
                {
                    { "Sentence", "句子" }, { "Translation", "中文翻译" }, { "Grammar", "语法点" }
                }
            },
            {
                Constants.SubCategory.EnglishComprehensive, new Dictionary<string, string>
                {
                    { "Title", "文章标题" }, { "Content", "文章内容" }, { "Questions", "阅读理解题" },
                    { "Question", "题目" }, { "Answer", "答案" }, { "Analysis", "解析" }
                }
            },
            {
                Constants.SubCategory.MathFormula, new Dictionary<string, string>
                {
                    { "Name", "公式名称" }, { "Formula", "公式表达式" }, { "Description", "公式说明" },
                    { "Conditions", "适用条件" }, { "Example", "应用举例" },
                    { "Difficulty", "难度等级" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.MathExample, new Dictionary<string, string>
                {
                    { "Title", "例题标题" }, { "Problem", "题目描述" }, { "Solution", "解答过程" },
                    { "KeySteps", "关键步骤" }, { "Analysis", "方法总结" },
                    { "Difficulty", "难度等级" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.MathConcept, new Dictionary<string, string>
                {
                    { "Name", "概念名称" }, { "Definition", "定义" }, { "Properties", "性质" },
                    { "Example", "举例说明" }, { "Notes", "注意事项" },
                    { "Difficulty", "难度等级" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.MathComprehensive, new Dictionary<string, string>
                {
                    { "Title", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" },
                    { "Example", "典型例题" }, { "Explanation", "答案解析" },
                    { "Difficulty", "难度等级" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.PhysicsLaw, new Dictionary<string, string>
                {
                    { "Name", "定律名称" }, { "Statement", "定律内容" }, { "Formula", "公式" },
                    { "Conditions", "适用条件" }, { "Application", "应用场景" },
                    { "Difficulty", "难度等级" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.PhysicsExperiment, new Dictionary<string, string>
                {
                    { "Name", "实验名称" }, { "Purpose", "实验目的" }, { "Equipment", "实验器材" },
                    { "Procedure", "实验步骤" }, { "Conclusion", "实验结论" },
                    { "Difficulty", "难度等级" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.PhysicsDerivation, new Dictionary<string, string>
                {
                    { "Name", "公式名称" }, { "Formula", "推导结果" }, { "DerivationSteps", "推导步骤" },
                    { "Conditions", "前提条件" }, { "Example", "应用实例" },
                    { "Difficulty", "难度等级" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.PhysicsComprehensive, new Dictionary<string, string>
                {
                    { "Title", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" },
                    { "Example", "典型例题" }, { "Explanation", "答案解析" },
                    { "Difficulty", "难度等级" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.ChemistryEquation, new Dictionary<string, string>
                {
                    { "Name", "反应名称" }, { "Reactants", "反应物" }, { "Products", "生成物" },
                    { "Equation", "化学方程式" }, { "Conditions", "反应条件" },
                    { "Phenomenon", "反应现象" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.ChemistryElement, new Dictionary<string, string>
                {
                    { "Name", "元素名称" }, { "Symbol", "元素符号" }, { "AtomicNumber", "原子序数" },
                    { "Properties", "元素性质" }, { "Uses", "主要用途" },
                    { "Difficulty", "难度等级" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.ChemistryExperiment, new Dictionary<string, string>
                {
                    { "Name", "实验名称" }, { "Purpose", "实验目的" }, { "Equipment", "实验器材" },
                    { "Procedure", "操作步骤" }, { "Phenomenon", "实验现象" },
                    { "Conclusion", "实验结论" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.ChemistryComprehensive, new Dictionary<string, string>
                {
                    { "Title", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" },
                    { "Example", "典型例题" }, { "Explanation", "答案解析" },
                    { "Difficulty", "难度等级" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.HistoryEvent, new Dictionary<string, string>
                {
                    { "Name", "事件名称" }, { "Time", "发生时间" }, { "Location", "发生地点" },
                    { "Background", "历史背景" }, { "Process", "事件经过" },
                    { "Impact", "历史影响" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.HistoryPerson, new Dictionary<string, string>
                {
                    { "Name", "人物姓名" }, { "Dynasty", "所处朝代" }, { "Lifetime", "生卒年月" },
                    { "Achievements", "主要成就" }, { "Evaluation", "历史评价" },
                    { "Works", "代表作品" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.HistoryTimeline, new Dictionary<string, string>
                {
                    { "Era", "时代名称" }, { "TimePeriod", "时间范围" }, { "KeyEvents", "重要事件" },
                    { "Characteristics", "时代特征" }, { "ImportantFigures", "重要人物" },
                    { "Notes", "备注" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.HistoryComprehensive, new Dictionary<string, string>
                {
                    { "Title", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" },
                    { "Example", "典型例题" }, { "Explanation", "答案解析" },
                    { "Difficulty", "难度等级" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.GeographyKnowledge, new Dictionary<string, string>
                {
                    { "Name", "地理名称" }, { "Category", "地理分类" }, { "Description", "地理描述" },
                    { "Distribution", "分布地区" }, { "Features", "主要特征" },
                    { "Notes", "备注" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.GeographyMap, new Dictionary<string, string>
                {
                    { "Name", "地图名称" }, { "Region", "所属地区" }, { "Features", "地理特征" },
                    { "KeyLocations", "重要地点" }, { "ReadingTips", "读图技巧" },
                    { "Notes", "备注" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.GeographyClimate, new Dictionary<string, string>
                {
                    { "Type", "气候类型" }, { "Distribution", "分布地区" }, { "Characteristics", "气候特征" },
                    { "Causes", "形成原因" }, { "Vegetation", "植被类型" },
                    { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.GeographyComprehensive, new Dictionary<string, string>
                {
                    { "Title", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" },
                    { "Example", "典型例题" }, { "Explanation", "答案解析" },
                    { "Difficulty", "难度等级" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.BiologyConcept, new Dictionary<string, string>
                {
                    { "Name", "概念名称" }, { "Definition", "定义" }, { "Classification", "分类" },
                    { "Features", "主要特征" }, { "Function", "功能作用" },
                    { "Example", "实例" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.BiologyExperiment, new Dictionary<string, string>
                {
                    { "Name", "实验名称" }, { "Purpose", "实验目的" }, { "Materials", "实验材料" },
                    { "Steps", "实验步骤" }, { "Result", "实验结果" },
                    { "Conclusion", "实验结论" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.BiologyPhenomenon, new Dictionary<string, string>
                {
                    { "Name", "现象名称" }, { "Description", "现象描述" }, { "Type", "现象类型" },
                    { "Causes", "产生原因" }, { "Examples", "常见实例" },
                    { "Significance", "生物意义" }, { "Tags", "标签" }
                }
            },
            {
                Constants.SubCategory.BiologyComprehensive, new Dictionary<string, string>
                {
                    { "Title", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" },
                    { "Example", "典型例题" }, { "Explanation", "答案解析" },
                    { "Difficulty", "难度等级" }, { "Tags", "标签" }
                }
            }
        };

        private string GetChineseColumnName(string columnName)
        {
            var subCategory = SelectedSubCategory;
            if (!string.IsNullOrEmpty(subCategory) &&
                CategoryColumnHeaders.TryGetValue(subCategory, out var headers) &&
                headers.TryGetValue(columnName, out var chineseName))
            {
                return chineseName;
            }
            return columnName;
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
                    column.HeaderText = GetChineseColumnName(column.Name);
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



        public void ClearEditForm()
        {
            textBoxJson.Text = "";
            dataGridView.DataSource = null;
        }

        public void RefreshTreeView(TreeNodeCollection nodes)
        {
            // 树形视图刷新方法 - 如果需要可以在此实现
        }

        public void UpdateItemList()
        {
            // 更新学习项列表显示 - 如果需要可以在此实现
        }

        public void LoadItemForEdit(dynamic item)
        {
            // 加载学习项进行编辑 - 如果需要可以在此实现
        }

        private void DataGridView_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            GridCellEndEdit?.Invoke(this, EventArgs.Empty);
        }

        private void DataGridView_RowsAdded(object? sender, DataGridViewRowsAddedEventArgs e)
        {
            GridRowsAdded?.Invoke(this, EventArgs.Empty);
        }

        private void ComboBoxSubject_SelectedIndexChanged(object? sender, EventArgs e)
        {
            SubjectChanged?.Invoke(this, EventArgs.Empty);
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
            // 获取当前编辑的内容作为上下文
            string context = textBoxJson.Text.Trim();

            // 构建一个用于示例结构的 JSON 片段：优先使用 context 中的第一个元素来体现结构
            string exampleStructure = string.Empty;
            if (!string.IsNullOrEmpty(context))
            {
                try
                {
                    var token = JToken.Parse(context);

                    JToken? exampleToken = null;
                    if (token.Type == JTokenType.Array)
                    {
                        exampleToken = token.First ?? token; // 使用第一个元素或数组本身
                    }
                    else
                    {
                        exampleToken = token;
                    }

                    if (exampleToken is JObject obj)
                    {
                        // 构造一个只包含键的示例对象，值为占位符以体现结构
                        var demo = new JObject();
                        foreach (var prop in obj.Properties())
                        {
                            demo[prop.Name] = "示例";
                        }
                        // 将示例包装为数组以展示最终输出应该是数组
                        var demoArray = new JArray { demo };
                        exampleStructure = demoArray.ToString(Formatting.Indented);
                    }
                    else
                    {
                        // 如果第一个元素不是对象（例如字符串或数字），直接使用其值作为示例
                        exampleStructure = new JArray { exampleToken }.ToString(Formatting.Indented);
                    }
                }
                catch
                {
                    // 无法解析时使用一个简短的默认示例结构
                    exampleStructure = new JArray
                    {
                        new JObject
                        {
                            ["Character"] = "示例",
                            ["Pinyin"] = "示例",
                            ["Meaning"] = "示例"
                        }
                    }.ToString(Formatting.Indented);
                }
            }
            else
            {
                // 空上下文时使用默认结构示例
                exampleStructure = new JArray
                {
                    new JObject
                    {
                        ["Character"] = "示例",
                        ["Pinyin"] = "示例",
                        ["Meaning"] = "示例"
                    }
                }.ToString(Formatting.Indented);
            }

            // 构建提示词：包含上下文（如果存在）和仅用于展示结构的示例（只用第一个元素的结构）
            string prompt = string.Empty;
            if (string.IsNullOrEmpty(context))
            {
                prompt = $"请帮我生成学习内容，输出格式为 JSON 数组。示例结构仅用于说明字段和层级（示例只取一项）：\n\n{exampleStructure}\n\n请仅返回 JSON 数组，且遵循上述结构。";
            }
            else
            {
                prompt = $"下面是当前编辑区的内容，请帮我完善或扩展这些学习内容（保留现有字段并补充/生成更多条目）。\n\n当前内容：\n{context}\n\n示例结构（仅展示第一个条目的结构以示范字段）：\n{exampleStructure}\n\n请返回一个 JSON 数组，结构与示例一致。";
            }

            // 使用AI面板服务显示AIAbilityPanel，传递提示词和上下文
            _aiPanelPopupService.ShowAIAbilityPanel(this, prompt, null, context);

            // 在AI服务调用后触发事件，允许事件处理程序获取最新状态
            GenerateWithAIClicked?.Invoke(this, EventArgs.Empty);
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
            groupBoxSubject = new Panel();
            comboBoxSubject = new ComboBox();
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
            groupBoxSubject.SuspendLayout();
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
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
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
            topPanel.Controls.Add(groupBoxSubject);
            topPanel.Controls.Add(labelCategory);
            topPanel.Controls.Add(comboBoxSubCategory);
            topPanel.Location = new Point(3, 3);
            topPanel.Name = "topPanel";
            topPanel.Size = new Size(1212, 40);
            topPanel.TabIndex = 0;
            // 
            // groupBoxSubject
            // 
            groupBoxSubject.BackColor = Color.FromArgb(255, 250, 240);
            groupBoxSubject.Controls.Add(comboBoxSubject);
            groupBoxSubject.Dock = DockStyle.Left;
            groupBoxSubject.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            groupBoxSubject.ForeColor = Color.FromArgb(33, 33, 33);
            groupBoxSubject.Location = new Point(0, 0);
            groupBoxSubject.Name = "groupBoxSubject";
            groupBoxSubject.Size = new Size(180, 40);
            groupBoxSubject.TabIndex = 21;
            // 
            // comboBoxSubject
            // 
            comboBoxSubject.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxSubject.Font = new Font("微软雅黑", 10F);
            comboBoxSubject.FormattingEnabled = true;
            comboBoxSubject.Location = new Point(15, 8);
            comboBoxSubject.Name = "comboBoxSubject";
            comboBoxSubject.Size = new Size(150, 28);
            comboBoxSubject.TabIndex = 0;
            comboBoxSubject.SelectedIndexChanged += ComboBoxSubject_SelectedIndexChanged;
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
            groupBoxSubject.ResumeLayout(false);
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
                _themeService?.UnregisterThemeable(this);
                // 控件资源由 base.Dispose 自动清理
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;

            if (topPanel != null)
            {
                topPanel.BackColor = colors.Background;
            }


            if (gridPanel != null)
            {
                gridPanel.BackColor = colors.Background;
            }

            if (mainPanel != null)
            {
                mainPanel.BackColor = colors.Background;
            }

            if (buttonPanel != null)
            {
                buttonPanel.BackColor = colors.Background;
            }

            if (tabControl1 != null)
            {
                tabControl1.BackColor = colors.Surface;
            }

            if (tabPage1 != null)
            {
                tabPage1.BackColor = colors.Surface;
            }

            if (tabPage2 != null)
            {
                tabPage2.BackColor = colors.Surface;
            }

            if (dataGridView != null)
            {
                dataGridView.BackgroundColor = colors.Surface;
                dataGridView.DefaultCellStyle.BackColor = colors.Surface;
                dataGridView.DefaultCellStyle.ForeColor = colors.TextPrimary;
                dataGridView.DefaultCellStyle.SelectionBackColor = colors.Primary;
                dataGridView.DefaultCellStyle.SelectionForeColor = Color.White;
                dataGridView.ColumnHeadersDefaultCellStyle.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.FromArgb(245, 245, 245);
                dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = colors.TextPrimary;
                dataGridView.GridColor = colors.ThemeMode == ThemeMode.Dark ? colors.Divider : Color.FromArgb(224, 224, 224);
                dataGridView.EnableHeadersVisualStyles = false;
            }

            if (textBoxJson != null)
            {
                textBoxJson.BackColor = colors.Surface;
                textBoxJson.ForeColor = colors.TextPrimary;
            }

            foreach (Control control in Controls)
            {
                ApplyThemeToControl(control, colors);
            }
        }

        private void ApplyThemeToControl(Control control, ThemeColors colors)
        {
            if (control is Label label)
            {
                label.ForeColor = colors.TextPrimary;
            }
            else if (control is RadioButton radioButton)
            {
                radioButton.ForeColor = colors.TextPrimary;
                radioButton.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.FromArgb(255, 250, 240);
            }
            else if (control is ComboBox comboBox)
            {
                comboBox.BackColor = colors.Surface;
                comboBox.ForeColor = colors.TextPrimary;
            }
            else if (control is TextBox textBox)
            {
                textBox.BackColor = colors.Surface;
                textBox.ForeColor = colors.TextPrimary;
            }
            else if (control is TabPage tabPage)
            {
                tabPage.BackColor = colors.Surface;
                tabPage.ForeColor = colors.TextPrimary;
            }
            else if (control is Panel panel)
            {
                panel.BackColor = colors.Background;
            }
            else if (control is FlowLayoutPanel flowLayoutPanel)
            {
                flowLayoutPanel.BackColor = colors.Background;
            }
            else if (control is TableLayoutPanel tableLayoutPanel)
            {
                tableLayoutPanel.BackColor = colors.Background;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child, colors);
            }
        }

        private void Button_HoverEnter(object? sender, EventArgs e)
        {
            if (sender is Button button)
            {
                button.BackColor = Color.FromArgb(
                    Math.Min(255, (int)button.BackColor.R + 25),
                    Math.Min(255, (int)button.BackColor.G + 25),
                    Math.Min(255, (int)button.BackColor.B + 25));
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
