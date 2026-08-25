using LearningAssistant.Common;
using LearningAssistant.Models.Config;
using LearningAssistant.Models.Learning;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using LearningAssistant.Abstractions;

namespace LearningAssistant.Services
{
    /// <summary>
    /// 科目模板服务接口
    /// </summary>
    public interface ISubjectTemplateService
    {
        List<string> GetAllSubjects();
        SubjectTemplate? GetSubjectTemplate(string subjectKey);
        string GetSubjectName(string subjectKey);
        string GetSubjectIcon(string subjectKey);
        List<string> GetCategories(string subjectKey);
        CategoryTemplate? GetCategoryTemplate(string subjectKey, string categoryKey);
        List<string> GetFields(string subjectKey, string categoryKey);
        string GetFieldDisplayName(string subjectKey, string categoryKey, string fieldKey);
        Dictionary<string, string> GetFieldDisplayNames(string subjectKey, string categoryKey);
        Dictionary<string, object> GetSampleData(string subjectKey, string categoryKey);
        bool HasSubject(string subjectKey);
        bool HasCategory(string subjectKey, string categoryKey);
        void ReloadConfig();
        List<(string Key, string Name, string Icon)> GetSubjectList();
        string GetCategoryDisplayName(string subjectKey, string categoryKey);
    }

    /// <summary>
    /// 科目模板服务 - 从JSON配置加载和管理科目模板
    /// </summary>
    public class SubjectTemplateService : ISubjectTemplateService
    {
        private SubjectTemplateConfig _config;
        private readonly ILogger<SubjectTemplateService>? _logger;
        private readonly object _lock = new object();
        private readonly IAppPaths _appPaths;

        public SubjectTemplateService(ILogger<SubjectTemplateService>? logger = null, IAppPaths appPaths = null)
        {
            _logger = logger;
            _appPaths = appPaths ?? throw new ArgumentNullException(nameof(appPaths));
            _config = LoadConfig();
            InitializeCategoryConfigFieldNames();
            InitializeCommonFieldNames();
        }

        private SubjectTemplateConfig LoadConfig()
        {
            try
            {
                // 尝试从多个位置加载配置
                string[] configPaths = new[]
                {
                    _appPaths.SubjectTemplatesPath
                };

                foreach (var configPath in configPaths)
                {
                    if (File.Exists(configPath))
                    {
                        var json = File.ReadAllText(configPath);
                        var config = JsonConvert.DeserializeObject<SubjectTemplateConfig>(json);
                        if (config != null)
                        {
                            _logger?.LogInformation("成功加载科目模板配置: {Path}", configPath);
                            return config;
                        }
                    }
                }

                _logger?.LogWarning("未找到科目模板配置文件，使用默认配置");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载科目模板配置失败");
            }

            return new SubjectTemplateConfig();
        }

        /// <summary>
        /// 将 SubjectTemplates.json 的 fieldNames 按 SubCategoryType 枚举名推送到 CategoryConfig，
        /// 使列标题等字段显示名以 JSON 为权威来源。
        /// </summary>
        private void InitializeCategoryConfigFieldNames()
        {
            var map = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var sub in Enum.GetValues<SubCategoryType>())
            {
                var enumName = sub.ToString();
                // JSON category 键 = Constants.FileName（去 .json），与 SubjectTemplates.json 的模板键一致。
                var fileField = typeof(Constants.FileName).GetField(enumName);
                var file = fileField?.GetValue(null) as string;
                var categoryKey = file?.Replace(".json", "");
                if (string.IsNullOrEmpty(categoryKey))
                    continue;

                var subjectKey = SubjectSubCategoryMapping.GetSubjectDisplayName(SubjectSubCategoryMapping.GetSubject(sub));
                var template = GetCategoryTemplate(subjectKey, categoryKey);
                if (template?.FieldNames != null && template.FieldNames.Count > 0)
                    map[enumName] = new Dictionary<string, string>(template.FieldNames, StringComparer.OrdinalIgnoreCase);
            }
            CategoryConfig.InitializeJsonFieldNames(map);
        }

        /// <summary>
        /// 将 SubjectTemplates.json 的 commonFieldNames 推送到 CategoryConfig，
        /// 作为类别特定 fieldNames 未命中时的跨类别通用字段翻译兜底。
        /// </summary>
        private void InitializeCommonFieldNames()
        {
            var common = _config.CommonFieldNames != null && _config.CommonFieldNames.Count > 0
                ? new Dictionary<string, string>(_config.CommonFieldNames, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            CategoryConfig.InitializeCommonFieldNames(common);
        }

        /// <summary>
        /// 获取所有科目
        /// </summary>
        public List<string> GetAllSubjects()
        {
            var subjects = new List<string>();
            foreach (var key in _config.Subjects.Keys)
            {
                subjects.Add(key);
            }
            return subjects;
        }

        /// <summary>
        /// 获取科目模板
        /// </summary>
        public SubjectTemplate? GetSubjectTemplate(string subjectKey)
        {
            if (_config.Subjects.TryGetValue(subjectKey, out var template))
            {
                return template;
            }
            return null;
        }

        /// <summary>
        /// 获取科目名称
        /// </summary>
        public string GetSubjectName(string subjectKey)
        {
            if (_config.Subjects.TryGetValue(subjectKey, out var template))
            {
                return template.Name;
            }
            return subjectKey;
        }

        /// <summary>
        /// 获取科目图标
        /// </summary>
        public string GetSubjectIcon(string subjectKey)
        {
            if (_config.Subjects.TryGetValue(subjectKey, out var template))
            {
                return template.Icon;
            }
            return "📖";
        }

        /// <summary>
        /// 获取科目的所有类别
        /// </summary>
        public List<string> GetCategories(string subjectKey)
        {
            if (_config.Subjects.TryGetValue(subjectKey, out var template))
            {
                return template.Categories;
            }
            return new List<string>();
        }

        /// <summary>
        /// 获取类别模板
        /// </summary>
        public CategoryTemplate? GetCategoryTemplate(string subjectKey, string categoryKey)
        {
            if (_config.Subjects.TryGetValue(subjectKey, out var subject))
            {
                if (subject.Templates.TryGetValue(categoryKey, out var template))
                {
                    return template;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取类别的所有字段
        /// </summary>
        public List<string> GetFields(string subjectKey, string categoryKey)
        {
            var template = GetCategoryTemplate(subjectKey, categoryKey);
            return template?.Fields ?? new List<string>();
        }

        /// <summary>
        /// 获取字段显示名称
        /// </summary>
        public string GetFieldDisplayName(string subjectKey, string categoryKey, string fieldKey)
        {
            var template = GetCategoryTemplate(subjectKey, categoryKey);
            if (template != null && template.FieldNames.TryGetValue(fieldKey, out var name))
            {
                return name;
            }
            return fieldKey;
        }

        /// <summary>
        /// 获取字段显示名称字典
        /// </summary>
        public Dictionary<string, string> GetFieldDisplayNames(string subjectKey, string categoryKey)
        {
            var template = GetCategoryTemplate(subjectKey, categoryKey);
            return template?.FieldNames ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// 获取示例数据
        /// </summary>
        public Dictionary<string, object> GetSampleData(string subjectKey, string categoryKey)
        {
            var template = GetCategoryTemplate(subjectKey, categoryKey);
            return template?.Sample ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// 检查是否存在指定科目
        /// </summary>
        public bool HasSubject(string subjectKey)
        {
            return _config.Subjects.ContainsKey(subjectKey);
        }

        /// <summary>
        /// 检查是否存在指定类别
        /// </summary>
        public bool HasCategory(string subjectKey, string categoryKey)
        {
            if (_config.Subjects.TryGetValue(subjectKey, out var subject))
            {
                return subject.Templates.ContainsKey(categoryKey);
            }
            return false;
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public void ReloadConfig()
        {
            lock (_lock)
            {
                _config = LoadConfig();
                InitializeCategoryConfigFieldNames();
                InitializeCommonFieldNames();
            }
        }

        /// <summary>
        /// 获取带图标的科目列表
        /// </summary>
        public List<(string Key, string Name, string Icon)> GetSubjectList()
        {
            var result = new List<(string Key, string Name, string Icon)>();
            foreach (var kvp in _config.Subjects)
            {
                result.Add((kvp.Key, kvp.Value.Name, kvp.Value.Icon));
            }
            return result;
        }

        /// <summary>
        /// 获取类别的显示名称
        /// </summary>
        public string GetCategoryDisplayName(string subjectKey, string categoryKey)
        {
            var template = GetCategoryTemplate(subjectKey, categoryKey);
            return template?.Name ?? categoryKey;
        }
    }
}