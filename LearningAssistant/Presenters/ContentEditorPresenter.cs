using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Learning;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Text;
using System.Text.Json;

namespace LearningAssistant.Presenters
{
    /// <summary>
    /// 内容编辑器Presenter，负责管理学习内容的编辑、导入、导出和AI生成功能
    /// </summary>
    public class ContentEditorPresenter : IDisposable
    {
        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger<ContentEditorPresenter> _logger;

        /// <summary>
        /// 视图接口，用于与UI层交互
        /// </summary>
        private readonly IContentEditorView _view;

        /// <summary>
        /// 内容加载服务，用于数据的持久化操作
        /// </summary>
        private readonly IContentLoaderService _contentLoaderService;

        /// <summary>
        /// AI问答服务，用于生成学习内容
        /// </summary>
        private readonly IAiQuestionService _aiQuestionService;

        /// <summary>
        /// 科目模板服务，从 SubjectTemplates.json 加载字段模板（支持新增模板动态生效）
        /// </summary>
        private readonly ISubjectTemplateService _subjectTemplateService;

        /// <summary>
        /// 脏标记，标识当前数据是否有未保存的更改
        /// </summary>
        private bool _isDirty = false;



        /// <summary>
        /// 类别模板字典，定义每个类别对应的字段结构
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, object>> CategoryTemplates = new()
        {
            {
                Constants.SubCategory.ChineseCharacter, new Dictionary<string, object>
                {
                    { "Character", "" }, { "Pinyin", "" }, { "Meaning", "" }, { "StrokeCount", "" }, { "Radical", "" }, { "StrokeOrder", "" }, { "Words", "" }, { "SimilarCharacters", "" }, { "Synonyms", "" }, { "Antonyms", "" }, { "CommonMistakes", "" }, { "ExampleSentence", "" }, { "CharacterLevel", "" }, { "Structure", "" }, { "CharacterFormation", "" }, { "OtherPronunciations", "" }
                }
            },
            {
                Constants.SubCategory.ChineseIdiom, new Dictionary<string, object>
                {
                    { "Idiom", "" }, { "Pinyin", "" }, { "Meaning", "" }, { "Origin", "" }, { "Example", "" }
                }
            },
            {
                Constants.SubCategory.ChinesePhrase, new Dictionary<string, object>
                {
                    { "Phrase", "" }, { "Pinyin", "" }, { "Meaning", "" }, { "Example", "" }
                }
            },
            {
                Constants.SubCategory.ChinesePoem, new Dictionary<string, object>
                {
                    { "Title", "" }, { "Author", "" }, { "Dynasty", "" }, { "Verses", "" }, { "Annotation", "" }, { "Translation", "" }, { "Appreciation", "" }, { "CreationBackground", "" }, { "FamousLines", "" }, { "RhetoricalDevices", "" }, { "Theme", "" }, { "AuthorIntro", "" }, { "PoemType", "" }, { "RelatedPoems", "" }, { "DifficultyLevel", "" }
                }
            },
            {
                Constants.SubCategory.EnglishWord, new Dictionary<string, object>
                {
                    { "Word", "" }, { "Phonetic", "" }, { "PartOfSpeech", "" }, { "SyllableBreakdown", "" }, { "Meaning", "" }, { "Example", "" }, { "Synonyms", "" }, { "Antonyms", "" }, { "WordForms", "" }, { "WordRootAffix", "" }, { "Collocations", "" }, { "Phrases", "" }, { "SynonymAnalysis", "" }, { "UkPhonetic", "" }, { "UsPhonetic", "" }, { "VocabularyLevel", "" }, { "Etymology", "" }, { "ConfusableWords", "" }
                }
            },
            {
                Constants.SubCategory.EnglishPhrase, new Dictionary<string, object>
                {
                    { "Phrase", "" }, { "Meaning", "" }, { "Example", "" }
                }
            },
            {
                Constants.SubCategory.EnglishSentence, new Dictionary<string, object>
                {
                    { "Sentence", "" }, { "Translation", "" }, { "Grammar", "" }
                }
            },
            {
                Constants.SubCategory.ChineseComprehensive, new Dictionary<string, object>
                {
                    { "Title", "" }, { "Content", "" }, { "Questions", new List<object> { new Dictionary<string, object> { { "Question", "" }, { "Answer", "" } } } }, { "Analysis", "" }
                }
            },
            {
                Constants.SubCategory.EnglishComprehensive, new Dictionary<string, object>
                {
                    { "Title", "" }, { "Content", "" }, { "Questions", new List<object> { new Dictionary<string, object> { { "Question", "" }, { "Answer", "" } } } }, { "Analysis", "" }
                }
            },
            {
                Constants.SubCategory.MathFormula, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", "" }, { "Principle", "" }, { "Example", "" }, { "Applications", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.MathExample, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "Analysis", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.MathConcept, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Note", "" }, { "Applications", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.MathComprehensive, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Analysis", "" }, { "Question", "" }, { "Answer", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.PhysicsLaw, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", "" }, { "Principle", "" }, { "Applications", "" }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.PhysicsExperiment, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "ExperimentSteps", new List<string> { "", "", "" } }, { "Analysis", "" }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.PhysicsDerivation, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Principle", "" }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.PhysicsComprehensive, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Analysis", "" }, { "Question", "" }, { "Answer", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.ChemistryEquation, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", "" }, { "Principle", "" }, { "Example", "" }, { "Applications", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.ChemistryElement, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", "" }, { "Principle", "" }, { "Applications", "" }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.ChemistryExperiment, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "ExperimentSteps", new List<string> { "", "", "" } }, { "Analysis", "" }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.ChemistryComprehensive, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Analysis", "" }, { "Question", "" }, { "Answer", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.HistoryEvent, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "TimePeriod", "" }, { "RelatedPlaces", "" }, { "Background", "" }, { "Content", "" }, { "Impact", "" }, { "RelatedPeople", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.HistoryPerson, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "TimePeriod", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Analysis", "" }, { "Example", "" }, { "RelatedPlaces", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.HistoryTimeline, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "TimePeriod", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Content", "" }, { "RelatedPeople", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.HistoryComprehensive, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Analysis", "" }, { "Question", "" }, { "Answer", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.GeographyKnowledge, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Category", "" }, { "Content", "" }, { "RelatedPlaces", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.GeographyMap, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "RelatedPlaces", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.GeographyClimate, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "RelatedPlaces", "" }, { "Content", "" }, { "Principle", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.GeographyComprehensive, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Analysis", "" }, { "Question", "" }, { "Answer", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.BiologyConcept, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "Category", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Applications", "" }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.BiologyExperiment, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "ExperimentSteps", new List<string> { "", "", "" } }, { "Analysis", "" }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.BiologyPhenomenon, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "Category", "" }, { "Principle", "" }, { "Example", "" }, { "Impact", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.BiologyComprehensive, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Analysis", "" }, { "Question", "" }, { "Answer", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            }
        };

        /// <summary>
        /// 构造函数，初始化ContentEditorPresenter
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="view">视图接口</param>
        /// <param name="contentLoaderService">内容加载服务</param>
        /// <param name="aiQuestionService">AI问答服务</param>
        /// <param name="subjectTemplateService">科目模板服务</param>
        /// <exception cref="ArgumentNullException">当任一参数为null时抛出</exception>
        public ContentEditorPresenter(
            ILogger<ContentEditorPresenter> logger,
            IContentEditorView view,
            IContentLoaderService contentLoaderService,
            IAiQuestionService aiQuestionService,
            ISubjectTemplateService subjectTemplateService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _contentLoaderService = contentLoaderService ?? throw new ArgumentNullException(nameof(contentLoaderService));
            _aiQuestionService = aiQuestionService ?? throw new ArgumentNullException(nameof(aiQuestionService));
            _subjectTemplateService = subjectTemplateService ?? throw new ArgumentNullException(nameof(subjectTemplateService));

            _view.SubjectChanged += OnSubjectChanged;
            _view.SubCategoryChanged += OnSubCategoryChanged;
            _view.TemplateAddClicked += OnTemplateAddClicked;
            _view.TemplateSaveClicked += OnTemplateSaveClicked;
            _view.TemplateDeleteClicked += OnTemplateDeleteClicked;
            _view.ImportClicked += OnImportClicked;
            _view.ExportClicked += OnExportClicked;
            _view.GridCellEndEdit += OnGridValueChanged;
            _view.GridRowsAdded += OnGridRowsAdded;

            _logger.LogInformation("ContentEditorPresenter initialized");
        }

        /// <summary>
        /// 初始化Presenter，加载子类别和数据
        /// </summary>
        public void Initialize()
        {
            LoadSubCategories();
            LoadItems();
            _isDirty = false;
        }

        /// <summary>
        /// 学科切换事件处理方法
        /// </summary>
        private void OnSubjectChanged(object? sender, EventArgs e)
        {
            if (CheckAndSaveUnsavedChanges())
            {
                LoadSubCategories();
                LoadItems();
            }
        }

        private void OnSubCategoryChanged(object? sender, EventArgs e)
        {
            if (CheckAndSaveUnsavedChanges())
            {
                LoadItems();
            }
        }

        private void LoadSubCategories()
        {
            var subject = _view.SelectedSubject;
            var subCategories = _contentLoaderService.GetSubCategories(subject);
            _view.RefreshSubCategories(subCategories);
        }

        private void LoadItems()
        {
            var subject = _view.SelectedSubject;
            var subCategory = _view.SelectedSubCategory;
            var context = new LearningContext("default_user", subject, subCategory);
            var items = _contentLoaderService.LoadItems(context);
            _view.ItemData = ConvertToDataTable(items, subCategory);
            _isDirty = false;
        }



        /// <summary>
        /// 将对象列表转换为DataTable，所有列均为string类型以避免类型推断问题
        /// </summary>
        /// <param name="items">对象列表</param>
        /// <param name="category">类别</param>
        /// <returns>转换后的DataTable</returns>
        private DataTable ConvertToDataTable(List<LearningItem> items, SubCategoryType category)
        {
            var table = new DataTable();
            var categoryStr = category.ToString();
            var template = GetTemplateDictionary(category);

            if (items.Count == 0)
            {
                if (template.Count > 0)
                {
                    foreach (var key in template.Keys)
                    {
                        var column = table.Columns.Add(key, typeof(string));
                        column.Caption = CategoryConfig.GetChineseColumnName(key, categoryStr);
                    }
                }
                return table;
            }

            var allColumns = new HashSet<string>();
            if (template.Count > 0)
            {
                foreach (var key in template.Keys)
                    allColumns.Add(key);
            }

            foreach (var item in items)
            {
                allColumns.Add("Id");
                allColumns.Add("CreatedAt");
                allColumns.Add("UpdatedAt");

                if (!string.IsNullOrWhiteSpace(item.MainContent))
                {
                    if (categoryStr.StartsWith("English"))
                        allColumns.Add("Word");
                    else if (categoryStr.StartsWith("Chinese"))
                        allColumns.Add(categoryStr.Contains("Character") ? "Character" : categoryStr.Contains("Idiom") ? "Idiom" : categoryStr.Contains("Poem") ? "Title" : "Phrase");
                }

                if (item.Meaning != null)
                    allColumns.Add("Meaning");
                if (item.Example != null)
                {
                    allColumns.Add("Example");
                    if (!string.IsNullOrWhiteSpace(item.Example.Translation))
                        allColumns.Add("ExampleTranslation");
                }
                if (item.Pronunciation != null)
                {
                    allColumns.Add("Phonetic");
                    if (!string.IsNullOrWhiteSpace(item.Pronunciation.UkPhonetic))
                        allColumns.Add("UkPhonetic");
                    if (!string.IsNullOrWhiteSpace(item.Pronunciation.UsPhonetic))
                        allColumns.Add("UsPhonetic");
                }
                if (item.CharacterFeatures != null)
                {
                    allColumns.Add("StrokeCount");
                    allColumns.Add("Radical");
                    allColumns.Add("Structure");
                }
                if (item.WordFeatures != null)
                {
                    allColumns.Add("PartOfSpeech");
                    allColumns.Add("WordForms");
                    allColumns.Add("Collocations");
                    allColumns.Add("SyllableBreakdown");
                }
            }

            foreach (var col in allColumns)
            {
                var column = table.Columns.Add(col, typeof(string));
                column.Caption = CategoryConfig.GetChineseColumnName(col, categoryStr);
            }

            foreach (var item in items)
            {
                var row = table.NewRow();
                row["Id"] = item.Id;
                row["CreatedAt"] = item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                row["UpdatedAt"] = item.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");

                if (categoryStr.StartsWith("English"))
                    row["Word"] = item.MainContent;
                else if (categoryStr.StartsWith("Chinese"))
                {
                    if (categoryStr.Contains("Character"))
                        row["Character"] = item.MainContent;
                    else if (categoryStr.Contains("Idiom"))
                        row["Idiom"] = item.MainContent;
                    else if (categoryStr.Contains("Poem"))
                        row["Title"] = item.MainContent;
                    else
                        row["Phrase"] = item.MainContent;
                }

                if (item.Meaning != null)
                    row["Meaning"] = item.Meaning.Content;
                if (item.Example != null)
                {
                    row["Example"] = item.Example.Content;
                    if (!string.IsNullOrWhiteSpace(item.Example.Translation))
                        row["ExampleTranslation"] = item.Example.Translation;
                }
                if (item.Pronunciation != null)
                {
                    row["Phonetic"] = item.Pronunciation.Main;
                    if (!string.IsNullOrWhiteSpace(item.Pronunciation.UkPhonetic))
                        row["UkPhonetic"] = item.Pronunciation.UkPhonetic;
                    if (!string.IsNullOrWhiteSpace(item.Pronunciation.UsPhonetic))
                        row["UsPhonetic"] = item.Pronunciation.UsPhonetic;
                }
                if (item.CharacterFeatures != null)
                {
                    row["StrokeCount"] = item.CharacterFeatures.StrokeCount;
                    row["Radical"] = item.CharacterFeatures.Radical;
                    row["Structure"] = item.CharacterFeatures.Structure;
                }
                if (item.WordFeatures != null)
                {
                    row["PartOfSpeech"] = item.WordFeatures.PartOfSpeech;
                    row["WordForms"] = item.WordFeatures.WordForms;
                    row["Collocations"] = item.WordFeatures.Collocations;
                    row["SyllableBreakdown"] = item.WordFeatures.SyllableBreakdown;
                }

                try
                {
                    var props = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(item.ExtendedProperties);
                    if (props != null)
                    {
                        foreach (var prop in props)
                        {
                            if (table.Columns.Contains(prop.Key))
                                row[prop.Key] = prop.Value?.ToString() ?? "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "解析学习项扩展属性失败");
                }

                table.Rows.Add(row);
            }

            return table;
        }

        /// <summary>
        /// 添加模板事件处理方法，显示当前类别的JSON模板
        /// </summary>
        private void OnTemplateAddClicked(object? sender, EventArgs e)
        {
            if (!CheckAndSaveUnsavedChanges()) return;
            _view.CurrentEditItemJson = GetTemplateJson(_view.SelectedSubCategory);
        }

        /// <summary>
        /// 保存事件处理方法，将JSON内容保存到当前类别
        /// </summary>
        private void OnTemplateSaveClicked(object? sender, EventArgs e)
        {
            SaveChanges();
        }

        /// <summary>
        /// 保存更改
        /// </summary>
        /// <returns>保存是否成功</returns>
        private bool SaveChanges()
        {
            var json = _view.CurrentEditItemJson;
            var category = _view.SelectedSubCategory;

            if (string.IsNullOrEmpty(json))
            {
                _view.ShowMessage("请先输入或生成JSON内容！");
                return false;
            }

            try
            {
                SaveFromJson(json, category);
                _view.ClearEditForm();
                LoadItems();
                _view.UpdateDirtyStatus(false);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save items to category {Category}", category);
                _view.ShowMessage($"保存失败：{ex.Message}");
                return false;
            }
        }

        private void SaveFromJson(string json, SubCategoryType category)
        {
            var items = ParseJsonToItems(json, category.ToString());
            if (items.Count == 0)
            {
                _view.ShowMessage("JSON为空或解析失败！");
                return;
            }
            var subject = _view.SelectedSubject;
            var context = new LearningContext("default_user", subject, category);
            var itemsOld = _contentLoaderService.LoadItems(context);

            foreach (var newItem in items)
            {
                newItem.Subject = subject;
                newItem.SubCategory = category;

                var newMainContent = newItem.GetMainContent().Trim().ToLower();
                var existingIndex = itemsOld.FindIndex(item =>
                    item.GetMainContent().Trim().ToLower() == newMainContent);

                if (existingIndex >= 0)
                {
                    itemsOld[existingIndex] = newItem;
                    _logger.LogInformation("覆盖重复项: {MainContent}", newMainContent);
                }
                else
                {
                    itemsOld.Add(newItem);
                }
            }

            _contentLoaderService.SaveItems(context, itemsOld);
            _logger.LogInformation("Successfully saved {Count} items to category {Category}", itemsOld.Count, category);
        }

        /// <summary>
        /// 将JSON字符串解析为对象列表
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <param name="category">类别名称，用于确定对象类型</param>
        /// <returns>解析后的对象列表</returns>
        private List<LearningItem> ParseJsonToItems(string json, string category)
        {
            if (!json.TrimStart().StartsWith("[")) json = $"[{json}]";

            try
            {
                using var doc = JsonDocument.Parse(json);
                var memoryStream = new MemoryStream();
                using var writer = new Utf8JsonWriter(memoryStream);
                writer.WriteStartArray();
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    writer.WriteStartObject();
                    foreach (var property in element.EnumerateObject())
                    {
                        var englishName = CategoryConfig.GetEnglishColumnName(property.Name, category);
                        writer.WritePropertyName(englishName);
                        property.Value.WriteTo(writer);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.Flush();
                json = Encoding.UTF8.GetString(memoryStream.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert Chinese column names to English, proceeding with original JSON");
            }

            return JsonHelper.DeserializeLearningItems(json);
        }

        /// <summary>
        /// 尝试将逗号分隔的字符串解析为JSON数组
        /// </summary>
        /// <param name="value">待解析的字符串</param>
        /// <returns>如果解析成功返回JArray，否则返回null</returns>
        private JToken? TryParseAsList(string? value)
        {
            if (string.IsNullOrEmpty(value) || !value.Contains(',')) return null;
            var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(p => p.Trim())
                           .Where(p => !string.IsNullOrEmpty(p))
                           .ToList();
            return parts.Count > 1 ? JArray.FromObject(parts) : null;
        }

        /// <summary>
        /// 删除选中条目事件处理方法
        /// </summary>
        private void OnTemplateDeleteClicked(object? sender, EventArgs e)
        {
            var selectedIndices = _view.SelectedRowIndices;
            var category = _view.SelectedSubCategory;

            if (selectedIndices == null || selectedIndices.Count == 0)
            {
                _view.ShowMessage("请在列表中选择要删除的条目");
                return;
            }

            try
            {
                var subject = _view.SelectedSubject;
                var context = new LearningContext("default_user", subject, category);
                var items = _contentLoaderService.LoadItems(context);
                foreach (var index in selectedIndices.OrderByDescending(i => i).Where(i => i >= 0 && i < items.Count))
                    items.RemoveAt(index);

                _contentLoaderService.SaveItems(context, items);
                _view.ClearEditForm();
                LoadItems();
            }
            catch (Exception ex)
            {
                _view.ShowMessage($"删除失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 网格数据变更事件处理方法
        /// </summary>
        private void OnGridValueChanged(object? sender, EventArgs e)
        {
            _isDirty = true;
            UpdateJsonFromGrid();
        }

        private void OnGridRowsAdded(object? sender, EventArgs e)
        {
            _isDirty = true;
            UpdateJsonFromGrid();
        }

        /// <summary>
        /// 从网格数据更新JSON内容
        /// </summary>
        private void UpdateJsonFromGrid()
        {
            if (_view.GridDataSource is DataTable dataTable)
            {
                var rows = dataTable.Rows.Cast<DataRow>()
                    .Select(row => dataTable.Columns.Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName, col => row[col]?.ToString() ?? ""))
                    .ToList();
                _view.CurrentEditItemJson = System.Text.Json.JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
            }
        }

        /// <summary>
        /// 导入事件处理方法，从JSON文件导入数据
        /// </summary>
        private void OnImportClicked(object? sender, EventArgs e)
        {
            if (!CheckAndSaveUnsavedChanges()) return;

            using var dialog = new OpenFileDialog
            {
                Filter = "JSON文件 (*.json)|*.json",
                FileName = $"{_view.SelectedSubCategory}_学习内容.json",
                Title = "导入学习内容"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var content = File.ReadAllText(dialog.FileName);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new LearningItemJsonConverter() }
                };
                var importedItems = System.Text.Json.JsonSerializer.Deserialize<List<LearningItem>>(content, options);

                if (importedItems?.Count > 0)
                {
                    var subject = _view.SelectedSubject;
                    var subCategory = _view.SelectedSubCategory;
                    var context = new LearningContext("default_user", subject, subCategory);
                    var existingItems = _contentLoaderService.LoadItems(context);

                    foreach (var newItem in importedItems)
                    {
                        var newMainContent = newItem.GetMainContent().Trim().ToLower();
                        var existingIndex = existingItems.FindIndex(item =>
                            item.GetMainContent().Trim().ToLower() == newMainContent);

                        if (existingIndex >= 0)
                        {
                            existingItems[existingIndex] = newItem;
                            _logger.LogInformation("导入时覆盖重复项: {MainContent}", newMainContent);
                        }
                        else
                        {
                            existingItems.Add(newItem);
                        }
                    }

                    _contentLoaderService.SaveItems(context, existingItems);
                    LoadItems();
                    _logger.LogInformation("Successfully imported {Count} items from {FilePath}", importedItems.Count, dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import items from {FilePath}", dialog.FileName);
                _view.ShowMessage("导入失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 导出事件处理方法，将数据导出为JSON文件
        /// </summary>
        private void OnExportClicked(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "JSON文件 (*.json)|*.json",
                FileName = $"{_view.SelectedSubCategory}_学习内容_{DateTime.Now:yyyyMMdd}.json",
                Title = "导出学习内容"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var subject = _view.SelectedSubject;
                var subCategory = _view.SelectedSubCategory;
                var context = new LearningContext("default_user", subject, subCategory);
                var items = _contentLoaderService.LoadItems(context);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new LearningItemJsonConverter() }
                };
                var json = System.Text.Json.JsonSerializer.Serialize(items, options);

                if (!string.IsNullOrEmpty(json))
                {
                    File.WriteAllText(dialog.FileName, json);
                    _view.ShowMessage("导出成功");
                    _logger.LogInformation("Successfully exported {Count} items to {FilePath}", items.Count, dialog.FileName);
                }
                else
                {
                    _view.ShowMessage("没有可导出的内容");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export items to {FilePath}", dialog.FileName);
                _view.ShowMessage("导出失败");
            }
        }

        /// <summary>
        /// 根据HTTP错误信息生成友好的中文错误提示
        /// </summary>
        /// <param name="errorMessage">原始错误信息</param>
        /// <returns>友好的中文错误提示</returns>
        private static string GetFriendlyErrorMessage(string errorMessage)
        {
            return errorMessage.Contains("401") ? "AI服务认证失败，请检查API密钥是否正确！" :
                   errorMessage.Contains("403") ? "AI服务访问被拒绝，请检查API密钥权限！" :
                   errorMessage.Contains("429") ? "AI服务请求过于频繁，请稍后再试！" :
                   errorMessage.Contains("500") || errorMessage.Contains("502") || errorMessage.Contains("503")
                       ? "AI服务暂时不可用，请稍后再试！" :
                       $"AI生成失败：{errorMessage}";
        }

        /// <summary>
        /// 获取指定类别的模板字段字典。
        /// 优先从 SubjectTemplates.json 动态加载（支持新增模板），未命中时回退到硬编码 CategoryTemplates。
        /// </summary>
        private Dictionary<string, object> GetTemplateDictionary(SubCategoryType category)
        {
            // JSON subject 键 = 科目显示名（语文/英语/数学…）；
            // JSON category 键 = 默认词库文件名去掉 .json（识字/公式定理/人物传记…），与 Constants.SubCategory 一致。
            var subjectKey = SubjectSubCategoryMapping.GetSubjectDisplayName(SubjectSubCategoryMapping.GetSubject(category));
            var categoryKey = _contentLoaderService.GetDefaultWordBankFile(category)?.Replace(".json", "");

            if (!string.IsNullOrEmpty(categoryKey))
            {
                var template = _subjectTemplateService.GetCategoryTemplate(subjectKey, categoryKey);
                if (template?.Fields != null && template.Fields.Count > 0)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var field in template.Fields)
                        dict[field] = "";
                    return dict;
                }

                // 回退到硬编码模板（其键为中文 categoryKey）。
                // 注意：硬编码字段可能与 SubjectTemplates.json 不一致，仅作 JSON 缺失时的容错回退。
                _logger?.LogWarning("JSON模板缺失(subject={Subject}, category={Category})，回退到硬编码模板", subjectKey, categoryKey);
                if (CategoryTemplates.TryGetValue(categoryKey, out var hardcoded))
                    return hardcoded;
            }

            return new Dictionary<string, object>();
        }

        /// <summary>
        /// 获取指定类别的JSON模板
        /// </summary>
        /// <param name="category">类别</param>
        /// <returns>JSON格式的模板字符串</returns>
        private string GetTemplateJson(SubCategoryType category)
        {
            var dict = GetTemplateDictionary(category);
            return dict.Count > 0
                ? System.Text.Json.JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true })
                : "{}";
        }

        /// <summary>
        /// 清理AI返回的JSON结果，提取JSON数组部分并使用JSON解析器确保格式正确
        /// </summary>
        /// <param name="result">AI返回的原始字符串</param>
        /// <returns>清理后的JSON字符串</returns>
        private static string CleanJsonResult(string result)
        {
            var startIndex = result.IndexOf('[');
            var endIndex = result.LastIndexOf(']');

            if (startIndex < 0 || endIndex < startIndex)
            {
                return result;
            }

            var jsonContent = result.Substring(startIndex, endIndex - startIndex + 1);

            try
            {
                using var doc = JsonDocument.Parse(jsonContent);
                return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return jsonContent;
            }
        }

        /// <summary>
        /// 检查并保存未保存的更改
        /// </summary>
        /// <returns>如果允许继续操作返回true，否则返回false</returns>
        private bool CheckAndSaveUnsavedChanges()
        {
            if (!_isDirty) return true;
            return SaveChanges();
        }

        /// <summary>
        /// 释放Presenter资源，在窗口关闭时调用
        /// </summary>
        public void Dispose()
        {
            if (_isDirty)
            {
                try
                {
                    var saveResult = SaveChanges();
                    if (!saveResult)
                    {
                        _logger.LogWarning("ContentEditorPresenter.Dispose - Failed to save unsaved changes");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ContentEditorPresenter.Dispose - Exception during save");
                }
            }

            _view.SubjectChanged -= OnSubjectChanged;
            _view.SubCategoryChanged -= OnSubCategoryChanged;
            _view.TemplateAddClicked -= OnTemplateAddClicked;
            _view.TemplateSaveClicked -= OnTemplateSaveClicked;
            _view.TemplateDeleteClicked -= OnTemplateDeleteClicked;
            _view.ImportClicked -= OnImportClicked;
            _view.ExportClicked -= OnExportClicked;
            _view.GridCellEndEdit -= OnGridValueChanged;
            _view.GridRowsAdded -= OnGridRowsAdded;

            _logger.LogInformation("ContentEditorPresenter disposed");
        }
    }
}
