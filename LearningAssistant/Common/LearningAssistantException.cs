namespace LearningAssistant.Common
{
    /// <summary>
    /// 应用程序基础异常 - 所有自定义异常的基类
    /// 用于统一异常处理和日志记录
    /// </summary>
    public class LearningAssistantException : Exception
    {
        /// <summary>
        /// 错误代码（可选），用于程序化判断错误类型
        /// </summary>
        public string? ErrorCode { get; }

        public LearningAssistantException() : base() { }

        public LearningAssistantException(string message) : base(message) { }

        public LearningAssistantException(string message, Exception innerException)
            : base(message, innerException) { }

        public LearningAssistantException(string message, string errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public LearningAssistantException(string message, string errorCode, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// 持久化相关异常 - 文件读写、JSON 序列化等失败时抛出
    /// </summary>
    public class PersistenceException : LearningAssistantException
    {
        public PersistenceException(string message) : base(message) { }

        public PersistenceException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// <summary>
    /// 配置相关异常 - 加载或保存配置失败时抛出
    /// </summary>
    public class ConfigurationException : LearningAssistantException
    {
        public ConfigurationException(string message) : base(message) { }

        public ConfigurationException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// <summary>
    /// 学习内容相关异常 - 词库加载、内容解析等失败时抛出
    /// </summary>
    public class LearningContentException : LearningAssistantException
    {
        public LearningContentException(string message) : base(message) { }

        public LearningContentException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
