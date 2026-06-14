using System.Collections.Generic;

namespace LearningAssistant.Models.Config
{
    /// <summary>
    /// 科目模板配置模型
    /// </summary>
    public class SubjectTemplateConfig
    {
        public Dictionary<string, SubjectTemplate> Subjects { get; set; } = new();
    }

    /// <summary>
    /// 科目模板
    /// </summary>
    public class SubjectTemplate
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public List<string> Categories { get; set; } = new();
        public Dictionary<string, CategoryTemplate> Templates { get; set; } = new();
    }

    /// <summary>
    /// 类别模板
    /// </summary>
    public class CategoryTemplate
    {
        public string Name { get; set; } = "";
        public List<string> Fields { get; set; } = new();
        public Dictionary<string, string> FieldNames { get; set; } = new();
        public Dictionary<string, object> Sample { get; set; } = new();
    }
}