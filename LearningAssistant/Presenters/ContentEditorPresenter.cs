using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services;
using LearningAssistant.Services.Learning;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;
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
        /// 科目模板服务，从 SubjectTemplates.json 加载字段模板（支持新增模板动态生效）
        /// </summary>
        private readonly ISubjectTemplateService _subjectTemplateService;

        /// <summary>
        /// 用户会话服务，用于获取当前用户 ID
        /// </summary>
        private readonly IUserSessionService? _userSessionService;

        /// <summary>
        /// 脏标记，标识当前数据是否有未保存的更改
        /// </summary>
        private bool _isDirty = false;



        /// <summary>
        /// 构造函数，初始化ContentEditorPresenter
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="view">视图接口</param>
        /// <param name="contentLoaderService">内容加载服务</param>
        /// <param name="subjectTemplateService">科目模板服务</param>
        /// <exception cref="ArgumentNullException">当任一参数为null时抛出</exception>
        public ContentEditorPresenter(
            ILogger<ContentEditorPresenter> logger,
            IContentEditorView view,
            IContentLoaderService contentLoaderService,
            ISubjectTemplateService subjectTemplateService,
            IUserSessionService? userSessionService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _contentLoaderService = contentLoaderService ?? throw new ArgumentNullException(nameof(contentLoaderService));
            _subjectTemplateService = subjectTemplateService ?? throw new ArgumentNullException(nameof(subjectTemplateService));
            _userSessionService = userSessionService;

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

        /// <summary>
        /// 获取当前用户 ID，优先使用 IUserSessionService，回退到默认用户。
        /// </summary>
        private string GetCurrentUserId()
        {
            return _userSessionService?.CurrentUserId ?? Constants.DefaultUserId;
        }

        private void LoadItems()
        {
            var subject = _view.SelectedSubject;
            var subCategory = _view.SelectedSubCategory;
            var context = new LearningContext(GetCurrentUserId(), subject, subCategory);
            var items = _contentLoaderService.LoadItems(context);
            _view.ItemData = ConvertToDataTable(items, subCategory);
            _isDirty = false;
            _view.UpdateDirtyStatus(false);
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

        /// <summary>
        /// 获取学习项的去重键。优先使用 MainContent；为空时从 ExtendedProperties 提取主字段（如 Name/Formula/Question/Concept）。
        /// 解决数学/物理等类别 MainContent 为空导致所有项被视为重复的问题。
        /// </summary>
        private string GetDedupKey(LearningItem item, SubCategoryType category)
        {
            var mainContent = item.GetMainContent().Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(mainContent))
                return mainContent;

            // MainContent 为空时，从模板主字段提取去重键
            var primaryField = GetPrimaryFieldName(category);
            if (!string.IsNullOrEmpty(primaryField))
            {
                var value = item.GetExtendedProperty<string>(primaryField, "");
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim().ToLowerInvariant();
            }

            // 兜底：使用 ExtendedProperties 整体作为去重键
            return (item.ExtendedProperties ?? "{}").Trim().ToLowerInvariant();
        }

        /// <summary>
        /// 获取类别的模板主字段名（模板字段列表的第一个字段）。
        /// </summary>
        private string? GetPrimaryFieldName(SubCategoryType category)
        {
            var template = GetTemplateDictionary(category);
            return template.Keys.FirstOrDefault();
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
            var context = new LearningContext(GetCurrentUserId(), subject, category);
            var itemsOld = _contentLoaderService.LoadItems(context);

            foreach (var newItem in items)
            {
                newItem.Subject = subject;
                newItem.SubCategory = category;

                var dedupKey = GetDedupKey(newItem, category);
                var existingIndex = itemsOld.FindIndex(item =>
                    GetDedupKey(item, category) == dedupKey);

                if (existingIndex >= 0)
                {
                    itemsOld[existingIndex] = newItem;
                    _logger.LogInformation("覆盖重复项: {DedupKey}", dedupKey);
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
                using var memoryStream = new MemoryStream();
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
                var context = new LearningContext(GetCurrentUserId(), subject, category);
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
        /// 系统列名，不应序列化到编辑JSON中（避免空Id/CreatedAt/UpdatedAt 覆盖真实数据）
        /// </summary>
        private static readonly HashSet<string> SystemColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "Id", "CreatedAt", "UpdatedAt"
        };

        /// <summary>
        /// 从网格数据更新JSON内容（过滤系统列，避免空Id被序列化保存）
        /// </summary>
        private void UpdateJsonFromGrid()
        {
            if (_view.GridDataSource is DataTable dataTable)
            {
                var rows = dataTable.Rows.Cast<DataRow>()
                    .Select(row => dataTable.Columns.Cast<DataColumn>()
                        .Where(col => !SystemColumns.Contains(col.ColumnName))
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
                    var context = new LearningContext(GetCurrentUserId(), subject, subCategory);
                    var existingItems = _contentLoaderService.LoadItems(context);

                    foreach (var newItem in importedItems)
                    {
                        var dedupKey = GetDedupKey(newItem, subCategory);
                        var existingIndex = existingItems.FindIndex(item =>
                            GetDedupKey(item, subCategory) == dedupKey);

                        if (existingIndex >= 0)
                        {
                            existingItems[existingIndex] = newItem;
                            _logger.LogInformation("导入时覆盖重复项: {DedupKey}", dedupKey);
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
                var context = new LearningContext(GetCurrentUserId(), subject, subCategory);
                var items = _contentLoaderService.LoadItems(context);

                if (items.Count == 0)
                {
                    _view.ShowMessage("没有可导出的内容");
                    return;
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new LearningItemJsonConverter() }
                };
                var json = System.Text.Json.JsonSerializer.Serialize(items, options);
                File.WriteAllText(dialog.FileName, json);
                _view.ShowMessage("导出成功");
                _logger.LogInformation("Successfully exported {Count} items to {FilePath}", items.Count, dialog.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export items to {FilePath}", dialog.FileName);
                _view.ShowMessage("导出失败");
            }
        }

        /// <summary>
        /// 获取指定类别的模板字段字典，从 SubjectTemplates.json 动态加载。
        /// </summary>
        private Dictionary<string, object> GetTemplateDictionary(SubCategoryType category)
        {
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
        /// 检查并保存未保存的更改。询问用户是否保存（保存/放弃/取消）。
        /// </summary>
        /// <returns>如果允许继续操作返回true，否则返回false</returns>
        private bool CheckAndSaveUnsavedChanges()
        {
            if (!_isDirty) return true;

            var result = _view.ShowConfirm("有未保存的更改，是否保存？", "确认保存");
            if (result == DialogResult.Yes)
                return SaveChanges();
            if (result == DialogResult.No)
            {
                _isDirty = false;
                _view.UpdateDirtyStatus(false);
                return true;
            }
            return false; // Cancel
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
