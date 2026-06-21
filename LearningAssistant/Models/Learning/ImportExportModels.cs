namespace LearningAssistant.Models.Learning
{
    /// <summary>
    /// 数据导入结果
    /// </summary>
    public class ImportResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 导入的总数量
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 成功导入的数量
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败的数量
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// 跳过的数量（重复等）
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// 错误信息列表
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// 警告信息列表
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// 导入的内容类型
        /// </summary>
        public string ContentType { get; set; } = string.Empty;
    }

    /// <summary>
    /// 导入选项
    /// </summary>
    public class ImportOptions
    {
        /// <summary>
        /// 内容类型（word、character、poem、grammar、general等）
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// 分类
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 遇到重复时的处理方式（skip、overwrite、duplicate）
        /// </summary>
        public string DuplicateHandling { get; set; } = "skip";

        /// <summary>
        /// 编码（UTF-8、GBK等）
        /// </summary>
        public string Encoding { get; set; } = "UTF-8";

        /// <summary>
        /// CSV分隔符
        /// </summary>
        public string CsvDelimiter { get; set; } = ",";

        /// <summary>
        /// 是否有标题行
        /// </summary>
        public bool HasHeaderRow { get; set; } = true;

        /// <summary>
        /// 列映射（字段名 -> 列索引或列名）
        /// </summary>
        public Dictionary<string, string> ColumnMapping { get; set; } = new();

        /// <summary>
        /// 科目
        /// </summary>
        public string Subject { get; set; } = string.Empty;
    }

    /// <summary>
    /// 导出选项
    /// </summary>
    public class ExportOptions
    {
        /// <summary>
        /// 导出格式（csv、txt、json）
        /// </summary>
        public string Format { get; set; } = "csv";

        /// <summary>
        /// 内容类型
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// 分类筛选
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 编码
        /// </summary>
        public string Encoding { get; set; } = "UTF-8";

        /// <summary>
        /// CSV分隔符
        /// </summary>
        public string CsvDelimiter { get; set; } = ",";

        /// <summary>
        /// 是否包含标题行
        /// </summary>
        public bool IncludeHeader { get; set; } = true;
    }
}
