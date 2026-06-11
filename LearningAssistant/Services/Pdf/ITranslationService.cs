namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// 翻译服务接口 - 提供文本翻译功能
    /// </summary>
    public interface ITranslationService
    {
        /// <summary>
        /// 翻译文本
        /// </summary>
        /// <param name="text">要翻译的文本</param>
        /// <param name="from">源语言（默认auto自动检测）</param>
        /// <param name="to">目标语言（默认zh中文）</param>
        /// <returns>翻译结果，失败返回null</returns>
        Task<string?> TranslateAsync(string text, string from = "auto", string to = "zh");

        /// <summary>
        /// 翻译服务是否可用
        /// </summary>
        bool IsAvailable { get; }
    }
}
