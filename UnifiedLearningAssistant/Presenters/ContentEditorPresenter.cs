using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Learning;
using LearningAssistant.Views;

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
        /// 脏标记，标识当前数据是否有未保存的更改
        /// </summary>
        private bool _isDirty = false;

        /// <summary>
        /// 类别类型名称映射字典，将类别常量映射为中文显示名称
        /// </summary>
        private static readonly Dictionary<string, string> CategoryTypeNames = new()
        {
            { Constants.SubCategory.ChineseCharacter, "识字" },
            { Constants.SubCategory.ChineseIdiom, "成语" },
            { Constants.SubCategory.ChinesePhrase, "短语" },
            { Constants.SubCategory.ChinesePoem, "诗词" },
            { Constants.SubCategory.ChineseComprehensive, "语文综合" },
            { Constants.SubCategory.EnglishWord, "英语单词" },
            { Constants.SubCategory.EnglishPhrase, "英语短语" },
            { Constants.SubCategory.EnglishSentence, "英语句子" },
            { Constants.SubCategory.EnglishComprehensive, "英语综合" }
        };

        /// <summary>
        /// 表头中英文映射字典
        /// </summary>
        private static readonly Dictionary<string, string> ColumnHeaderNames = new()
        {
            { "Character", "汉字" },
            { "Pinyin", "拼音" },
            { "Meaning", "释义" },
            { "StrokeCount", "笔画数" },
            { "Radical", "部首" },
            { "Words", "组词" },
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

        /// <summary>
        /// 类别模板字典，定义每个类别对应的字段结构
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, object>> CategoryTemplates = new()
        {
            {
                Constants.SubCategory.ChineseCharacter, new Dictionary<string, object>
                {
                    { "Character", "" }, { "Pinyin", "" }, { "Meaning", "" }, { "StrokeCount", "" }, { "Radical", "" }, { "Words", new List<string> { "", "", "", "", "" } }
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
                    { "Title", "" }, { "Author", "" }, { "Dynasty", "" }, { "Verses", new List<string> { "", "", "", "" } }, { "Annotation", "" }
                }
            },
            {
                Constants.SubCategory.EnglishWord, new Dictionary<string, object>
                {
                    { "Word", "" }, { "Phonetic", "" }, { "PartOfSpeech", "" }, { "Meaning", "" }, { "Example", "" }
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
            }
        };

        /// <summary>
        /// JSON格式提示字典，用于AI生成时指定输出格式
        /// </summary>
        private static readonly Dictionary<string, string> JsonFormatHints = new()
        {
            { Constants.SubCategory.ChineseCharacter, @"[  {""Character"":"""",""Pinyin"":"""",""Meaning"":"""",""StrokeCount"":"""",""Radical"":"""",""Words"":["""","""","""","""",""""]} ]" },
            { Constants.SubCategory.ChineseIdiom, @"[  {""Idiom"":"""",""Pinyin"":"""",""Meaning"":"""",""Origin"":"""",""Example"":""""} ]" },
            { Constants.SubCategory.ChinesePhrase, @"[  {""Phrase"":"""",""Pinyin"":"""",""Meaning"":"""",""Example"":""""} ]" },
            { Constants.SubCategory.ChinesePoem, @"[  {""Title"":"""",""Author"":"""",""Dynasty"":"""",""Verses"":["""","""","""",""""],""Annotation"":""""} ]" },
            { Constants.SubCategory.ChineseComprehensive, @"[  {""Title"":"""",""Content"":"""",""Questions"":[{""Question"":"""",""Answer"":""""}],""Analysis"":""""} ]" },
            { Constants.SubCategory.EnglishWord, @"[  {""Word"":"""",""Phonetic"":"""",""PartOfSpeech"":"""",""Meaning"":"""",""Example"":""""} ]" },
            { Constants.SubCategory.EnglishPhrase, @"[  {""Phrase"":"""",""Meaning"":"""",""Example"":""""} ]" },
            { Constants.SubCategory.EnglishSentence, @"[  {""Sentence"":"""",""Translation"":"""",""Grammar"":""""} ]" },
            { Constants.SubCategory.EnglishComprehensive, @"[  {""Title"":"""",""Content"":"""",""Questions"":[{""Question"":"""",""Answer"":""""}],""Analysis"":""""} ]" }
        };

        /// <summary>
        /// 构造函数，初始化ContentEditorPresenter
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="view">视图接口</param>
        /// <param name="contentLoaderService">内容加载服务</param>
        /// <param name="aiQuestionService">AI问答服务</param>
        /// <exception cref="ArgumentNullException">当任一参数为null时抛出</exception>
        public ContentEditorPresenter(
            ILogger<ContentEditorPresenter> logger,
            IContentEditorView view,
            IContentLoaderService contentLoaderService,
            IAiQuestionService aiQuestionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _contentLoaderService = contentLoaderService ?? throw new ArgumentNullException(nameof(contentLoaderService));
            _aiQuestionService = aiQuestionService ?? throw new ArgumentNullException(nameof(aiQuestionService));

            _view.LanguageChanged += (_, _) => OnLanguageChanged();
            _view.SubCategoryChanged += (_, _) => OnSubCategoryChanged();
            _view.TemplateAddClicked += (_, _) => OnTemplateAddClicked();
            _view.TemplateSaveClicked += (_, _) => OnTemplateSaveClicked();
            _view.TemplateDeleteClicked += (_, _) => OnTemplateDeleteClicked();
            _view.ImportClicked += (_, _) => OnImportClicked();
            _view.ExportClicked += (_, _) => OnExportClicked();
            _view.GenerateWithAIClicked += (_, _) => OnGenerateWithAIClicked();
            _view.GridCellEndEdit += (_, _) => OnGridValueChanged();
            _view.GridRowsAdded += (_, _) => OnGridValueChanged();

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
        /// 语言切换事件处理方法
        /// </summary>
        private void OnLanguageChanged()
        {
            if (CheckAndSaveUnsavedChanges())
            {
                LoadSubCategories();
                LoadItems();
            }
        }

        /// <summary>
        /// 子类别切换事件处理方法
        /// </summary>
        private void OnSubCategoryChanged()
        {
            if (CheckAndSaveUnsavedChanges())
            {
                LoadItems();
            }
        }

        /// <summary>
        /// 根据当前语言加载子类别列表
        /// </summary>
        private void LoadSubCategories()
        {
            var language = _view.SelectedLanguage;
            var subCategories = language == Constants.Language.Chinese
                ? new List<string>
                {
                    Constants.SubCategory.ChineseCharacter,
                    Constants.SubCategory.ChinesePhrase,
                    Constants.SubCategory.ChineseIdiom,
                    Constants.SubCategory.ChinesePoem,
                    Constants.SubCategory.ChineseComprehensive
                }
                : new List<string>
                {
                    Constants.SubCategory.EnglishWord,
                    Constants.SubCategory.EnglishPhrase,
                    Constants.SubCategory.EnglishSentence,
                    Constants.SubCategory.EnglishComprehensive
                };
            _view.RefreshSubCategories(subCategories);
        }

        /// <summary>
        /// 加载当前类别的数据项
        /// </summary>
        private void LoadItems()
        {
            var category = _view.SelectedSubCategory;
            var items = _contentLoaderService.LoadItems(category);
            _view.ItemData = ConvertToDataTable(items, category);
            _isDirty = false;
        }

        /// <summary>
        /// 获取列的中文名称
        /// </summary>
        /// <param name="columnName">英文列名</param>
        /// <returns>中文列名，如果没有映射则返回原名称</returns>
        private static string GetChineseColumnName(string columnName)
        {
            return ColumnHeaderNames.TryGetValue(columnName, out var chineseName) ? chineseName : columnName;
        }

        /// <summary>
        /// 将对象列表转换为DataTable，所有列均为string类型以避免类型推断问题
        /// </summary>
        /// <param name="items">对象列表</param>
        /// <param name="category">类别名称</param>
        /// <returns>转换后的DataTable</returns>
        private DataTable ConvertToDataTable(List<object> items, string category)
        {
            var table = new DataTable();

            if (items.Count == 0)
            {
                if (CategoryTemplates.TryGetValue(category, out var template))
                {
                    foreach (var key in template.Keys)
                    {
                        var column = table.Columns.Add(key, typeof(string));
                        column.Caption = GetChineseColumnName(key);
                    }
                }
                return table;
            }

            var properties = items[0].GetType().GetProperties();
            foreach (var prop in properties)
            {
                var column = table.Columns.Add(prop.Name, typeof(string));
                column.Caption = GetChineseColumnName(prop.Name);
            }

            foreach (var item in items)
            {
                var row = table.NewRow();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(item);
                    row[prop.Name] = value switch
                    {
                        List<string> list => list.Count > 0 ? string.Join(", ", list) : "",
                        null => "",
                        _ => value.ToString() ?? ""
                    };
                }
                table.Rows.Add(row);
            }

            return table;
        }

        /// <summary>
        /// 添加模板事件处理方法，显示当前类别的JSON模板
        /// </summary>
        private void OnTemplateAddClicked()
        {
            if (!CheckAndSaveUnsavedChanges()) return;
            _view.CurrentEditItemJson = GetTemplateJson(_view.SelectedSubCategory);
        }

        /// <summary>
        /// 保存事件处理方法，将JSON内容保存到当前类别
        /// </summary>
        private void OnTemplateSaveClicked()
        {
            var json = _view.CurrentEditItemJson;
            var category = _view.SelectedSubCategory;

            if (string.IsNullOrEmpty(json))
            {
                _view.ShowMessage("请先输入或生成JSON内容！");
                return;
            }

            if (string.IsNullOrEmpty(category))
            {
                _view.ShowMessage("请选择一个类别！");
                return;
            }

            try
            {
                SaveFromJson(json, category);
                _view.ClearEditForm();
                LoadItems();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save items to category {Category}", category);
                _view.ShowMessage($"保存失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 从JSON字符串解析并保存数据项
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <param name="category">目标类别</param>
        private void SaveFromJson(string json, string category)
        {
            var items = ParseJsonToItems(json, category);
            if (items.Count == 0)
            {
                _view.ShowMessage("JSON为空或解析失败！");
                return;
            }
            var itemsOld = _contentLoaderService.LoadItems(category);

            foreach (var newItem in items)
            {
                if (newItem is LearningItem newLearningItem)
                {
                    var newMainContent = newLearningItem.GetMainContent().Trim().ToLower();
                    var existingIndex = itemsOld.FindIndex(item =>
                        item is LearningItem existingItem &&
                        existingItem.GetMainContent().Trim().ToLower() == newMainContent);

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
                else
                {
                    itemsOld.Add(newItem);
                }
            }

            _contentLoaderService.SaveItems(category, itemsOld);
            _logger.LogInformation("Successfully saved {Count} items to category {Category}", itemsOld.Count, category);
        }

        /// <summary>
        /// 将JSON字符串解析为对象列表
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <param name="category">类别名称，用于确定对象类型</param>
        /// <returns>解析后的对象列表</returns>
        private List<object> ParseJsonToItems(string json, string category)
        {
            var items = new List<object>();
            var itemType = _contentLoaderService.GetItemType(category);

            if (!json.TrimStart().StartsWith("[")) json = $"[{json}]";

            var listType = typeof(List<>).MakeGenericType(itemType);
            var data = System.Text.Json.JsonSerializer.Deserialize(json, listType,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            var importedItems = ((System.Collections.IList)data).Cast<object>().ToList();
            foreach (var item in importedItems)
            {
                items.Add(item);
            }

            return items;
        }

        /// <summary>
        /// 将DataTable转换为对象列表
        /// </summary>
        /// <param name="table">DataTable数据源</param>
        /// <param name="category">类别名称，用于确定对象类型</param>
        /// <returns>转换后的对象列表</returns>
        private List<object> ConvertDataTableToItems(DataTable table, string category)
        {
            var itemType = _contentLoaderService.GetItemType(category);
            return table.Rows.Cast<DataRow>().Select(row =>
            {
                var jsonObj = new JObject();
                foreach (DataColumn col in table.Columns)
                {
                    var value = row[col]?.ToString();
                    jsonObj[col.ColumnName] = TryParseAsList(value) ?? value ?? "";
                }
                return jsonObj.ToObject(itemType);
            }).Where(item => item != null).Cast<object>().ToList();
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
        private void OnTemplateDeleteClicked()
        {
            var selectedIndices = _view.SelectedRowIndices;
            var category = _view.SelectedSubCategory;

            if (selectedIndices == null || selectedIndices.Count == 0)
            {
                _view.ShowMessage("请在列表中选择要删除的条目");
                return;
            }

            if (string.IsNullOrEmpty(category))
            {
                _view.ShowMessage("请选择一个类别！");
                return;
            }

            try
            {
                var items = _contentLoaderService.LoadItems(category);
                foreach (var index in selectedIndices.OrderByDescending(i => i).Where(i => i >= 0 && i < items.Count))
                    items.RemoveAt(index);

                _contentLoaderService.SaveItems(category, items);
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
        private void OnGridValueChanged()
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
                _view.CurrentEditItemJson = JsonConvert.SerializeObject(rows, Formatting.Indented);
            }
        }

        /// <summary>
        /// 导入事件处理方法，从JSON文件导入数据
        /// </summary>
        private void OnImportClicked()
        {
            if (!CheckAndSaveUnsavedChanges()) return;

            using var dialog = new OpenFileDialog { Filter = "JSON文件 (*.json)|*.json" };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var content = File.ReadAllText(dialog.FileName);
                var importedItems = JsonConvert.DeserializeObject<List<object>>(content,
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

                if (importedItems?.Count > 0)
                {
                    var existingItems = _contentLoaderService.LoadItems(_view.SelectedSubCategory);

                    foreach (var newItem in importedItems)
                    {
                        if (newItem is LearningItem newLearningItem)
                        {
                            var newMainContent = newLearningItem.GetMainContent().Trim().ToLower();
                            var existingIndex = existingItems.FindIndex(item =>
                                item is LearningItem existingItem &&
                                existingItem.GetMainContent().Trim().ToLower() == newMainContent);

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
                        else
                        {
                            existingItems.Add(newItem);
                        }
                    }

                    _contentLoaderService.SaveItems(_view.SelectedSubCategory, existingItems);
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
        private void OnExportClicked()
        {
            using var dialog = new SaveFileDialog { Filter = "JSON文件 (*.json)|*.json" };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var items = _contentLoaderService.LoadItems(_view.SelectedSubCategory);
                var json = JsonConvert.SerializeObject(items, Formatting.Indented,
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

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
        /// AI生成内容事件处理方法
        /// </summary>
        private async void OnGenerateWithAIClicked()
        {
            var category = _view.SelectedSubCategory;

            if (string.IsNullOrEmpty(category))
            {
                _view.ShowMessage("请先选择一个类别！");
                return;
            }

            if (!int.TryParse(_view.GenerateCount, out var count))
            {
                _view.ShowMessage("请输入有效的生成数量！");
                return;
            }

            var range = string.IsNullOrWhiteSpace(_view.GenerateRange) || _view.GenerateRange == "请输入关键词或范围"
                ? "常用" : _view.GenerateRange;

            try
            {
                var prompt = GetAIPrompt(category, count, range);
                _view.PromptText = prompt;
                _logger.LogInformation("Generating {Count} {Range} {Category} items with AI", count, range, category);
                var response = await _aiQuestionService.AskAsync(prompt);

                if (!string.IsNullOrEmpty(response))
                {


                    _view.CurrentEditItemJson = response;
                    //OnTemplateSaveClicked();
                    _logger.LogInformation("Successfully generated {Count} {Category} items with AI", count, category);
                }
                else
                {
                    _view.ShowMessage("AI生成失败，请重试！");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("OnGenerateWithAIClicked was cancelled");
                _view.ShowMessage("操作已取消");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "AI API HTTP error for category {Category}", category);
                _view.ShowMessage(GetFriendlyErrorMessage(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate items with AI for category {Category}", category);
                _view.ShowMessage($"生成失败：{ex.Message}");
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
        /// 生成AI请求提示词
        /// </summary>
        /// <param name="category">内容类别</param>
        /// <param name="count">生成数量</param>
        /// <param name="range">关键词或范围</param>
        /// <returns>格式化后的AI提示词</returns>
        private string GetAIPrompt(string category, int count, string range)
        {
            var typeName = CategoryTypeNames.GetValueOrDefault(category, "内容");
            var format = JsonFormatHints.GetValueOrDefault(category, "[]");

            if (category == Constants.SubCategory.ChineseComprehensive)
            {
                return $"生成{count}个语文综合练习题（{range}），包含标题、内容、3-5道题目及答案、解析。格式：{format}";
            }
            else if (category == Constants.SubCategory.EnglishComprehensive)
            {
                return $"Generate {count} English exercises ({range}) with title, content, 3-5 questions and answers, analysis. Format: {format}";
            }

            return $"生成{count}个{range}的{typeName}。格式：{format}";
        }

        /// <summary>
        /// 获取指定类别的JSON模板
        /// </summary>
        /// <param name="category">类别名称</param>
        /// <returns>JSON格式的模板字符串</returns>
        private static string GetTemplateJson(string category)
        {
            return CategoryTemplates.TryGetValue(category, out var template)
                ? JsonConvert.SerializeObject(template, Formatting.Indented)
                : "{}";
        }

        /// <summary>
        /// 清理AI返回的JSON结果，处理换行符和特殊字符
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

            jsonContent = jsonContent.Replace("\r\n", "\\n")
                                     .Replace("\r", "\\n")
                                     .Replace("\n", "\\n")
                                     .Replace("\"", "\\\"")
                                     .Replace("\t", "\\t");

            return jsonContent;
        }

        /// <summary>
        /// 检查并保存未保存的更改
        /// </summary>
        /// <returns>如果允许继续操作返回true，否则返回false</returns>
        private bool CheckAndSaveUnsavedChanges()
        {
            if (!_isDirty) return true;
            OnTemplateSaveClicked();
            return true;
        }

        /// <summary>
        /// 释放Presenter资源，在窗口关闭时调用
        /// </summary>
        public void Dispose()
        {

            OnTemplateSaveClicked();


            _view.LanguageChanged -= (_, _) => OnLanguageChanged();
            _view.SubCategoryChanged -= (_, _) => OnSubCategoryChanged();
            _view.TemplateAddClicked -= (_, _) => OnTemplateAddClicked();
            _view.TemplateSaveClicked -= (_, _) => OnTemplateSaveClicked();
            _view.TemplateDeleteClicked -= (_, _) => OnTemplateDeleteClicked();
            _view.ImportClicked -= (_, _) => OnImportClicked();
            _view.ExportClicked -= (_, _) => OnExportClicked();
            _view.GenerateWithAIClicked -= (_, _) => OnGenerateWithAIClicked();
            _view.GridCellEndEdit -= (_, _) => OnGridValueChanged();
            _view.GridRowsAdded -= (_, _) => OnGridValueChanged();

            _logger.LogInformation("ContentEditorPresenter disposed");
        }
    }
}
