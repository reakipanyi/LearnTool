using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using UnifiedLearningAssistant.Models.Learning;
using UnifiedLearningAssistant.Services.AI;
using UnifiedLearningAssistant.Services.Learning;
using UnifiedLearningAssistant.Views;

namespace UnifiedLearningAssistant.Presenters
{
    public class ContentEditorPresenter : IDisposable
    {
        private readonly ILogger<ContentEditorPresenter> _logger;
        private readonly IContentEditorView _view;
        private readonly IContentLoaderService _contentLoaderService;
        private readonly IAiQuestionService _aiQuestionService;

        public ContentEditorPresenter(ILogger<ContentEditorPresenter> logger, IContentEditorView view,
            IContentLoaderService contentLoaderService, IAiQuestionService aiQuestionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _contentLoaderService = contentLoaderService ?? throw new ArgumentNullException(nameof(contentLoaderService));
            _aiQuestionService = aiQuestionService ?? throw new ArgumentNullException(nameof(aiQuestionService));

            _view.CategoryChanged += OnCategoryChanged;
            _view.TemplateAddClicked += OnTemplateAddClicked;
            _view.TemplateSaveClicked += OnTemplateSaveClicked;
            _view.TemplateDeleteClicked += OnTemplateDeleteClicked;
            _view.ImportClicked += OnImportClicked;
            _view.ExportClicked += OnExportClicked;
            _view.InsertTemplateClicked += OnInsertTemplateClicked;
            _view.GenerateWithAIClicked += OnGenerateWithAIClicked;
            _view.GridCellEndEdit += OnGridCellEndEdit;
            _view.GridRowsAdded += OnGridRowsAdded;
            _logger.LogInformation("ContentEditorPresenter initialized");
        }

        public void Initialize()
        {
        }

        private void OnCategoryChanged(object? sender, EventArgs e)
        {
        }

        private void OnTemplateAddClicked(object? sender, EventArgs e)
        {
            _view.ClearEditForm();
        }

        private void OnTemplateSaveClicked(object? sender, EventArgs e)
        {
            string json = _view.CurrentEditItemJson;

            if (string.IsNullOrEmpty(json))
            {
                _view.ShowMessage("请先输入或生成JSON内容！");
                return;
            }

            string category = _view.SelectedSubCategory;
            if (string.IsNullOrEmpty(category))
            {
                _view.ShowMessage("请选择一个类别！");
                return;
            }

            try
            {
                if (json.TrimStart().StartsWith("["))
                {
                    var jsonArray = JArray.Parse(json);
                    List<LearningItem> items = new List<LearningItem>();

                    foreach (var jsonItem in jsonArray)
                    {
                        string typeName = jsonItem["_type"]?.ToString() ?? category;
                        Type itemType = _contentLoaderService.GetItemType(typeName);
                        LearningItem? item = jsonItem.ToObject(itemType) as LearningItem;
                        if (item != null)
                            items.Add(item);
                    }
                    if (items.Count > 0)
                    {
                        List<LearningItem> existingItems = _contentLoaderService.LoadItems(category);
                        existingItems.AddRange(items);
                        _contentLoaderService.SaveItems(category, existingItems);
                        _view.ShowMessage($"成功添加 {items.Count} 条数据！");
                        _view.ClearEditForm();
                        _logger.LogInformation("Successfully added {Count} items to category {Category}", items.Count, category);
                    }
                    else
                    {
                        _view.ShowMessage("JSON数组为空！");
                    }
                }
                else
                {
                    var jsonObj = JObject.Parse(json);
                    string typeName = jsonObj["_type"]?.ToString() ?? category;
                    Type itemType = _contentLoaderService.GetItemType(typeName);

                    LearningItem? item = jsonObj.ToObject(itemType) as LearningItem;
                    if (item != null)
                    {
                        List<LearningItem> existingItems = _contentLoaderService.LoadItems(category);
                        existingItems.Add(item);
                        _contentLoaderService.SaveItems(category, existingItems);
                        _view.ShowMessage("保存成功！");
                        _view.ClearEditForm();
                        _logger.LogInformation("Successfully saved item to category {Category}", category);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save items to category {Category}", category);
                _view.ShowMessage($"保存失败：{ex.Message}");
            }
        }

        private void OnTemplateDeleteClicked(object? sender, EventArgs e)
        {
            var dataTable = _view.GridDataSource as DataTable;
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                _view.ShowMessage("请先导入或加载数据后再进行删除操作");
                return;
            }

            int[] selectedIndices = _view.SelectedRowIndices;
            if (selectedIndices.Length == 0)
            {
                _view.ShowMessage("请选择要删除的行");
                return;
            }

            if (MessageBox.Show($"确定要删除选中的 {selectedIndices.Length} 行数据吗？", "确认删除",
                MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                for (int i = selectedIndices.Length - 1; i >= 0; i--)
                {
                    dataTable.Rows.RemoveAt(selectedIndices[i]);
                }
                UpdateJsonFromGrid();
                _view.ShowMessage($"已删除 {selectedIndices.Length} 行数据");
            }
        }

        private void OnGridCellEndEdit(object? sender, EventArgs e)
        {
            UpdateJsonFromGrid();
        }

        private void OnGridRowsAdded(object? sender, EventArgs e)
        {
            UpdateJsonFromGrid();
        }

        private void UpdateJsonFromGrid()
        {
            var dataTable = _view.GridDataSource as DataTable;
            if (dataTable != null)
            {
                string json = JsonConvert.SerializeObject(dataTable, Formatting.None);
                _view.CurrentEditItemJson = json;
            }
        }

        private void OnImportClicked(object? sender, EventArgs e)
        {
            using OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "JSON文件 (*.json)|*.json";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string content = File.ReadAllText(dialog.FileName);
                    List<LearningItem>? importedItems = JsonConvert.DeserializeObject<List<LearningItem>>(content, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    });

                    if (importedItems != null)
                    {
                        _view.CurrentEditItemJson = JsonConvert.SerializeObject(importedItems, Formatting.Indented);
                        _view.ShowMessage("导入成功，请点击保存按钮保存到词库");
                        _logger.LogInformation("Successfully imported items from {FilePath}", dialog.FileName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import items from {FilePath}", dialog.FileName);
                    _view.ShowMessage("导入失败");
                }
            }
        }

        private void OnExportClicked(object? sender, EventArgs e)
        {
            using SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "JSON文件 (*.json)|*.json";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string json = _view.CurrentEditItemJson;
                    if (!string.IsNullOrEmpty(json))
                    {
                        File.WriteAllText(dialog.FileName, json);
                        _view.ShowMessage("导出成功");
                        _logger.LogInformation("Successfully exported items to {FilePath}", dialog.FileName);
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
        }

        private void OnInsertTemplateClicked(object? sender, EventArgs e)
        {
            string template = GetTemplateForCategory(_view.SelectedSubCategory);
            _view.CurrentEditItemJson = template;
        }

        private string GetTemplateForCategory(string category)
        {
            object template = category switch
            {
                "汉字" => new Dictionary<string, object>
                {
                    { "Character", "" },
                    { "Pinyin", "" },
                    { "Meaning", "" },
                    { "StrokeCount", "" },
                    { "Radical", "" }
                },
                "组词" => new Dictionary<string, object>
                {
                    { "Character", "" },
                    { "Pinyin", "" },
                    { "Words", new List<string> { "", "", "", "", "" } }
                },
                "成语" => new Dictionary<string, object>
                {
                    { "Idiom", "" },
                    { "Pinyin", "" },
                    { "Meaning", "" },
                    { "Origin", "" },
                    { "Example", "" }
                },
                "短语" => new Dictionary<string, object>
                {
                    { "Phrase", "" },
                    { "Pinyin", "" },
                    { "Meaning", "" },
                    { "Example", "" }
                },
                "诗词" => new Dictionary<string, object>
                {
                    { "Title", "" },
                    { "Author", "" },
                    { "Dynasty", "" },
                    { "Verses", new List<string> { "", "", "", "" } },
                    { "Annotation", "" }
                },
                "英语单词" => new Dictionary<string, object>
                {
                    { "Word", "" },
                    { "Phonetic", "" },
                    { "PartOfSpeech", "" },
                    { "Meaning", "" },
                    { "Example", "" }
                },
                "英语短语" => new Dictionary<string, object>
                {
                    { "Phrase", "" },
                    { "Meaning", "" },
                    { "Example", "" }
                },
                "英语句子" => new Dictionary<string, object>
                {
                    { "Sentence", "" },
                    { "Translation", "" },
                    { "Grammar", "" }
                },
                _ => new Dictionary<string, object> { { "_type", "" } }
            };

            return JsonConvert.SerializeObject(template, Formatting.Indented);
        }

        private async void OnGenerateWithAIClicked(object? sender, EventArgs e)
        {
            string category = _view.SelectedSubCategory;

            if (string.IsNullOrEmpty(category))
            {
                _view.ShowMessage("请先选择一个类别！");
                return;
            }

            if (!int.TryParse(_view.GenerateCount, out int count))
            {
                _view.ShowMessage("请输入有效的生成数量！");
                return;
            }

            string range = _view.GenerateRange;
            if (string.IsNullOrWhiteSpace(range) || range == "请输入关键词或范围")
            {
                range = "常用";
            }

            try
            {
                _logger.LogInformation("Generating {Count} {Range} {Category} items with AI", count, range, category);
                string prompt = GetAIPrompt(category, count, range);
                string response = await _aiQuestionService.AskAsync(prompt);

                if (!string.IsNullOrEmpty(response))
                {
                    string jsonResult = CleanJsonResult(response);
                    _view.CurrentEditItemJson = jsonResult;
                    _view.ShowMessage($"AI已成功生成内容！");
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate items with AI for category {Category}", category);
                _view.ShowMessage($"生成失败：{ex.Message}");
            }
        }

        private string GetAIPrompt(string category, int count, string range)
        {
            string typeName = category switch
            {
                "汉字" => "汉字",
                "组词" => "组词",
                "成语" => "成语",
                "短语" => "短语",
                "诗词" => "诗词",
                "英语单词" => "英语单词",
                "英语短语" => "英语短语",
                "英语句子" => "英语句子",
                _ => "内容"
            };

            string format = GetJSONFormatHint(category);

            return $@"请生成{count}个{range}的{typeName}，每个包含详细信息，输出JSON数组格式：
{format}

注意：
1. 直接输出JSON数组，不要有其他解释文字
2. 确保JSON格式正确
3. 内容要丰富且实用";
        }

        private string GetJSONFormatHint(string category)
        {
            return category switch
            {
                "汉字" => @"[  {""Character"":"""",""Pinyin"":"""",""Meaning"":"""",""StrokeCount"":"""",""Radical"":""""}]",
                "组词" => @"[  {""Character"":"""",""Pinyin"":"""",""Words"":["""","""","""","""",""""]}]",
                "成语" => @"[  {""Idiom"":"""",""Pinyin"":"""",""Meaning"":"""",""Origin"":"""",""Example"":""""}]",
                "短语" => @"[  {""Phrase"":"""",""Pinyin"":"""",""Meaning"":"""",""Example"":""""}]",
                "诗词" => @"[  {""Title"":"""",""Author"":"""",""Dynasty"":"""",""Verses"":["""","""","""",""""],""Annotation"":""""}]",
                "英语单词" => @"[  {""Word"":"""",""Phonetic"":"""",""PartOfSpeech"":"""",""Meaning"":"""",""Example"":""""}]",
                "英语短语" => @"[  {""Phrase"":"""",""Meaning"":"""",""Example"":""""}]",
                "英语句子" => @"[  {""Sentence"":"""",""Translation"":"""",""Grammar"":""""}]",
                _ => "[]"
            };
        }

        private string CleanJsonResult(string result)
        {
            int startIndex = result.IndexOf('[');
            int endIndex = result.LastIndexOf(']');

            if (startIndex >= 0 && endIndex >= startIndex)
            {
                return result.Substring(startIndex, endIndex - startIndex + 1);
            }

            return result;
        }

        public void Dispose()
        {
            _view.CategoryChanged -= OnCategoryChanged;
            _view.TemplateAddClicked -= OnTemplateAddClicked;
            _view.TemplateSaveClicked -= OnTemplateSaveClicked;
            _view.TemplateDeleteClicked -= OnTemplateDeleteClicked;
            _view.ImportClicked -= OnImportClicked;
            _view.ExportClicked -= OnExportClicked;
            _view.InsertTemplateClicked -= OnInsertTemplateClicked;
            _view.GenerateWithAIClicked -= OnGenerateWithAIClicked;
            _view.GridCellEndEdit -= OnGridCellEndEdit;
            _view.GridRowsAdded -= OnGridRowsAdded;
            _logger.LogInformation("ContentEditorPresenter disposed");
        }
    }
}