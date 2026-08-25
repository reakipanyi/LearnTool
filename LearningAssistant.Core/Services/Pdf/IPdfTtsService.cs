namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF TTS（文字转语音）服务接口 - 提供PDF文本的语音朗读功能
    /// </summary>
    public interface IPdfTtsService
    {
        /// <summary>
        /// TTS服务是否可用
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 使用指定语言朗读文本
        /// </summary>
        /// <param name="text">要朗读的文本</param>
        /// <param name="language">语言代码（如 en-US, zh-CN）</param>
        /// <param name="speed">语速（1.0为正常速度）</param>
        Task SpeakTextAsync(string text, string language, float speed);

        /// <summary>
        /// 使用默认语言朗读文本
        /// </summary>
        /// <param name="text">要朗读的文本</param>
        /// <param name="speed">语速（1.0为正常速度，-1表示使用配置值）</param>
        Task SpeakTextAsync(string text, float speed = -1f);
    }
}
