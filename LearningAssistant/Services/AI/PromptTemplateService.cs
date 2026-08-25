using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Models.AI;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace LearningAssistant.Services.AI
{
    /// <summary>
    /// 提示词模板管理服务实现
    /// </summary>
    public class PromptTemplateService : IPromptTemplateService
    {
        private readonly ILogger<PromptTemplateService>? _logger;
        private ConcurrentBag<PromptTemplateCategory> _categories = new();
        private readonly object _lock = new();
        private readonly IAppPaths _appPaths;

        public PromptTemplateService(ILogger<PromptTemplateService>? logger = null, IAppPaths appPaths = null!)
        {
            _logger = logger;
            _appPaths = appPaths ?? throw new ArgumentNullException(nameof(appPaths));
            LoadFromFile();
        }

        /// <inheritdoc/>
        public List<PromptTemplateCategory> GetAllCategories()
        {
            return _categories.OrderBy(c => c.Order).ToList();
        }

        /// <inheritdoc/>
        public List<PromptTemplate> GetAllTemplates()
        {
            var allTemplates = new List<PromptTemplate>();
            foreach (var category in _categories)
            {
                allTemplates.AddRange(category.Templates);
            }
            return allTemplates.OrderBy(t => t.Order).ToList();
        }

        /// <inheritdoc/>
        public List<PromptTemplate> GetTemplatesByCategory(string category)
        {
            var cat = _categories.FirstOrDefault(c =>
                c.Name.Equals(category, StringComparison.OrdinalIgnoreCase));
            return cat?.Templates.OrderBy(t => t.Order).ToList() ?? new List<PromptTemplate>();
        }

        /// <inheritdoc/>
        public PromptTemplate? GetTemplate(string templateId)
        {
            foreach (var category in _categories)
            {
                var template = category.Templates.FirstOrDefault(t => t.Id == templateId);
                if (template != null)
                    return template;
            }
            return null;
        }

        /// <inheritdoc/>
        public List<PromptTemplate> SearchTemplates(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return GetAllTemplates();

            keyword = keyword.Trim().ToLower();
            var results = new List<PromptTemplate>();

            foreach (var category in _categories)
            {
                results.AddRange(category.Templates.Where(t =>
                    (!string.IsNullOrWhiteSpace(t.Name) && t.Name.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(t.Description) && t.Description.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(t.SystemPrompt) && t.SystemPrompt.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(t.UserPromptTemplate) && t.UserPromptTemplate.ToLower().Contains(keyword))
                ));
            }

            return results;
        }

        /// <inheritdoc/>
        public void AddTemplate(PromptTemplate template)
        {
            lock (_lock)
            {
                try
                {
                    var category = _categories.FirstOrDefault(c =>
                        c.Name.Equals(template.Category, StringComparison.OrdinalIgnoreCase));

                    if (category == null)
                    {
                        category = new PromptTemplateCategory
                        {
                            Name = template.Category,
                            Order = _categories.Count
                        };
                        _categories.Add(category);
                    }

                    template.Id = Guid.NewGuid().ToString();
                    template.CreatedAt = DateTime.Now;
                    template.UpdatedAt = DateTime.Now;
                    template.Order = category.Templates.Count;

                    category.Templates.Add(template);
                    SaveToFile();
                    _logger?.LogInformation($"提示词模板已添加: {template.Name}");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "添加提示词模板失败");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void UpdateTemplate(PromptTemplate template)
        {
            lock (_lock)
            {
                try
                {
                    foreach (var category in _categories)
                    {
                        var existing = category.Templates.FirstOrDefault(t => t.Id == template.Id);
                        if (existing != null)
                        {
                            existing.Name = template.Name;
                            existing.Description = template.Description;
                            existing.SystemPrompt = template.SystemPrompt;
                            existing.UserPromptTemplate = template.UserPromptTemplate;
                            existing.Variables = template.Variables;
                            existing.Icon = template.Icon;
                            existing.Color = template.Color;
                            existing.IsFavorite = template.IsFavorite;
                            existing.UpdatedAt = DateTime.Now;

                            if (existing.Category != template.Category)
                            {
                                category.Templates.Remove(existing);
                                var newCategory = _categories.FirstOrDefault(c =>
                                    c.Name.Equals(template.Category, StringComparison.OrdinalIgnoreCase));
                                if (newCategory == null)
                                {
                                    newCategory = new PromptTemplateCategory
                                    {
                                        Name = template.Category,
                                        Order = _categories.Count
                                    };
                                    _categories.Add(newCategory);
                                }
                                existing.Category = template.Category;
                                newCategory.Templates.Add(existing);
                            }

                            SaveToFile();
                            _logger?.LogInformation($"提示词模板已更新: {template.Name}");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "更新提示词模板失败");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void DeleteTemplate(string templateId)
        {
            lock (_lock)
            {
                try
                {
                    foreach (var category in _categories)
                    {
                        var template = category.Templates.FirstOrDefault(t => t.Id == templateId);
                        if (template != null)
                        {
                            if (template.IsBuiltIn)
                            {
                                _logger?.LogWarning("内置模板不能删除: {Name}", template.Name);
                                return;
                            }

                            category.Templates.Remove(template);
                            SaveToFile();
                            _logger?.LogInformation($"提示词模板已删除: {template.Name}");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "删除提示词模板失败");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void SetFavorite(string templateId, bool isFavorite)
        {
            lock (_lock)
            {
                try
                {
                    var template = GetTemplate(templateId);
                    if (template != null)
                    {
                        template.IsFavorite = isFavorite;
                        template.UpdatedAt = DateTime.Now;
                        SaveToFile();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "设置收藏状态失败");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public List<PromptTemplate> GetFavoriteTemplates()
        {
            return GetAllTemplates().Where(t => t.IsFavorite).ToList();
        }

        /// <inheritdoc/>
        public string RenderTemplate(string templateId, Dictionary<string, string> variables)
        {
            try
            {
                var template = GetTemplate(templateId);
                if (template == null)
                    return string.Empty;

                var result = template.UserPromptTemplate;

                foreach (var variable in template.Variables)
                {
                    var placeholder = "{{" + variable.Name + "}}";
                    var value = variables.ContainsKey(variable.Name)
                        ? variables[variable.Name]
                        : variable.DefaultValue;
                    result = result.Replace(placeholder, value);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "渲染提示词模板失败");
                return string.Empty;
            }
        }

        /// <inheritdoc/>
        public void RecordUsage(string templateId)
        {
            lock (_lock)
            {
                try
                {
                    var template = GetTemplate(templateId);
                    if (template != null)
                    {
                        template.UsageCount++;
                        template.LastUsedAt = DateTime.Now;
                        SaveToFile();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "记录使用次数失败");
                }
            }
        }

        /// <inheritdoc/>
        public List<PromptTemplate> GetFrequentlyUsed(int count = 10)
        {
            return GetAllTemplates()
                .OrderByDescending(t => t.UsageCount)
                .ThenByDescending(t => t.LastUsedAt)
                .Take(count)
                .ToList();
        }

        /// <inheritdoc/>
        public void ExportTemplates(string filePath, List<string>? templateIds = null)
        {
            try
            {
                var templatesToExport = templateIds != null && templateIds.Count > 0
                    ? GetAllTemplates().Where(t => templateIds.Contains(t.Id)).ToList()
                    : GetAllTemplates();

                var json = JsonSerializer.Serialize(templatesToExport, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(filePath, json);
                _logger?.LogInformation($"已导出 {templatesToExport.Count} 个提示词模板: {filePath}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出提示词模板失败");
                throw;
            }
        }

        /// <inheritdoc/>
        public int ImportTemplates(string filePath, bool overwrite = false)
        {
            lock (_lock)
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    var importedTemplates = JsonSerializer.Deserialize<List<PromptTemplate>>(json);

                    if (importedTemplates == null || importedTemplates.Count == 0)
                        return 0;

                    int importedCount = 0;

                    foreach (var template in importedTemplates)
                    {
                        var existing = GetTemplate(template.Id);

                        if (existing != null && overwrite)
                        {
                            existing.Name = template.Name;
                            existing.Description = template.Description;
                            existing.Category = template.Category;
                            existing.SystemPrompt = template.SystemPrompt;
                            existing.UserPromptTemplate = template.UserPromptTemplate;
                            existing.Variables = template.Variables;
                            existing.Icon = template.Icon;
                            existing.Color = template.Color;
                            existing.UpdatedAt = DateTime.Now;
                            importedCount++;
                        }
                        else if (existing == null)
                        {
                            AddTemplate(template);
                            importedCount++;
                        }
                    }

                    SaveToFile();
                    _logger?.LogInformation($"已导入 {importedCount} 个提示词模板");
                    return importedCount;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "导入提示词模板失败");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void AddCategory(string categoryName, string icon = "📁")
        {
            lock (_lock)
            {
                try
                {
                    if (!_categories.Any(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase)))
                    {
                        _categories.Add(new PromptTemplateCategory
                        {
                            Name = categoryName,
                            Icon = icon,
                            Order = _categories.Count
                        });
                        SaveToFile();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "添加分类失败");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void RemoveCategory(string categoryName)
        {
            lock (_lock)
            {
                try
                {
                    var category = _categories.FirstOrDefault(c =>
                        c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                    if (category != null)
                    {
                        _categories = new ConcurrentBag<PromptTemplateCategory>(
                            _categories.Where(c => c.Name != categoryName));
                        SaveToFile();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "删除分类失败");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void SaveToFile()
        {
            try
            {
                var data = new
                {
                    Categories = _categories.OrderBy(c => c.Order).ToList()
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var directory = Path.GetDirectoryName(_appPaths.PromptTemplatesPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(_appPaths.PromptTemplatesPath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存提示词模板失败");
            }
        }

        /// <inheritdoc/>
        public void LoadFromFile()
        {
            try
            {
                if (File.Exists(_appPaths.PromptTemplatesPath))
                {
                    var json = File.ReadAllText(_appPaths.PromptTemplatesPath);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("Categories", out var categoriesElement))
                    {
                        _categories = new ConcurrentBag<PromptTemplateCategory>();
                        var categories = JsonSerializer.Deserialize<List<PromptTemplateCategory>>(
                            categoriesElement.GetRawText());

                        if (categories != null)
                        {
                            foreach (var category in categories)
                            {
                                _categories.Add(category);
                            }
                            _logger?.LogInformation($"已加载 {_categories.Count} 个提示词模板分类");
                            return;
                        }
                    }
                }

                _logger?.LogInformation("提示词模板文件不存在，加载默认模板");
                LoadDefaultTemplates();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载提示词模板失败，使用默认模板");
                LoadDefaultTemplates();
            }
        }

        /// <inheritdoc/>
        public void ResetToDefault()
        {
            lock (_lock)
            {
                _categories = new ConcurrentBag<PromptTemplateCategory>();
                LoadDefaultTemplates();
                SaveToFile();
            }
        }

        private void LoadDefaultTemplates()
        {
            var defaultCategories = new List<PromptTemplateCategory>
            {
                new PromptTemplateCategory
                {
                    Name = "工具",
                    Icon = "📚",
                    Order = 0,
                    Templates = new List<PromptTemplate>
                    {
                        new PromptTemplate
                        {
                            Name = "知识点讲解",
                            Description = "用通俗易懂的方式讲解知识点",
                            Category = "学习助手",
                            Icon = "📖",
                            Color = "#2196F3",
                            IsBuiltIn = true,
                            IsFavorite = true,
                            SystemPrompt = "你是一位知识渊博、耐心细致的老师。请用通俗易懂的语言讲解知识点，适当举例帮助理解。",
                            UserPromptTemplate = "请帮我讲解一下这个知识点：{{知识点}}\n\n请从以下几个方面讲解：\n1. 基本概念\n2. 核心原理\n3. 常见用法\n4. 注意事项",
                            Variables = new List<PromptVariable>
                            {
                                new() { Name = "知识点", Description = "要讲解的知识点", IsRequired = true, Type = "textarea" }
                            },
                            Order = 0
                        },
                        new PromptTemplate
                        {
                            Name = "题目解析",
                            Description = "详细解析题目，给出解题思路",
                            Category = "学习助手",
                            Icon = "✏️",
                            Color = "#4CAF50",
                            IsBuiltIn = true,
                            SystemPrompt = "你是一位经验丰富的辅导老师。请详细分析题目，给出清晰的解题思路和步骤。",
                            UserPromptTemplate = "请帮我解析这道题：\n\n题目：{{题目}}\n\n我的答案：{{我的答案}}\n\n请分析：\n1. 正确答案\n2. 解题思路\n3. 我错在哪里（如果我答错了）\n4. 相关知识点",
                            Variables = new List<PromptVariable>
                            {
                                new() { Name = "题目", Description = "题目内容", IsRequired = true, Type = "textarea" },
                                new() { Name = "我的答案", Description = "你的答案（可选）", Type = "textarea" }
                            },
                            Order = 1
                        }
                    }
                },
                new PromptTemplateCategory
                {
                    Name = "写作辅助",
                    Icon = "✍️",
                    Order = 1,
                    Templates = new List<PromptTemplate>
                    {
                        new PromptTemplate
                        {
                            Name = "作文润色",
                            Description = "优化文章表达，提升文采",
                            Category = "写作辅助",
                            Icon = "✨",
                            Color = "#9C27B0",
                            IsBuiltIn = true,
                            SystemPrompt = "你是一位资深的写作指导老师。请在保持原意的基础上，优化文章的表达，提升文采和感染力。",
                            UserPromptTemplate = "请帮我润色这篇文章：\n\n{{文章}}\n\n请从以下方面优化：\n1. 语句通顺度\n2. 词汇丰富度\n3. 结构逻辑性\n4. 文采感染力",
                            Variables = new List<PromptVariable>
                            {
                                new() { Name = "文章", Description = "待润色的文章", IsRequired = true, Type = "textarea" }
                            },
                            Order = 0
                        },
                        new PromptTemplate
                        {
                            Name = "翻译润色",
                            Description = "翻译并优化表达",
                            Category = "写作辅助",
                            Icon = "🌐",
                            Color = "#FF9800",
                            IsBuiltIn = true,
                            SystemPrompt = "你是一位专业的翻译。请准确翻译原文，并使译文自然流畅。",
                            UserPromptTemplate = "请将以下内容翻译成{{目标语言}}：\n\n{{原文}}\n\n要求：\n1. 准确传达原意\n2. 译文自然流畅\n3. 符合目标语言表达习惯",
                            Variables = new List<PromptVariable>
                            {
                                new() { Name = "原文", Description = "待翻译的原文", IsRequired = true, Type = "textarea" },
                                new() { Name = "目标语言", Description = "目标语言", IsRequired = true, DefaultValue = "英语",
                                    Type = "select", Options = new List<string> { "英语", "日语", "韩语", "法语", "德语", "西班牙语", "中文" } }
                            },
                            Order = 1
                        }
                    }
                },
                new PromptTemplateCategory
                {
                    Name = "创意灵感",
                    Icon = "💡",
                    Order = 2,
                    Templates = new List<PromptTemplate>
                    {
                        new PromptTemplate
                        {
                            Name = "头脑风暴",
                            Description = "激发创意，生成多种想法",
                            Category = "创意灵感",
                            Icon = "🌪️",
                            Color = "#E91E63",
                            IsBuiltIn = true,
                            SystemPrompt = "你是一位创意大师。请围绕主题展开头脑风暴，提供多样化的创意想法。",
                            UserPromptTemplate = "请围绕「{{主题}}」进行头脑风暴：\n\n请给出至少10个不同方向的创意想法，包括：\n1. 实用方向\n2. 有趣方向\n3. 创新方向\n4. 跨界方向",
                            Variables = new List<PromptVariable>
                            {
                                new() { Name = "主题", Description = "头脑风暴的主题", IsRequired = true }
                            },
                            Order = 0
                        }
                    }
                }
            };

            _categories = new ConcurrentBag<PromptTemplateCategory>(defaultCategories);
        }
    }
}
