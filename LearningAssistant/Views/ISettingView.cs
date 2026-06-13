namespace LearningAssistant.Views
{
    /// <summary>
    /// 设置视图接口 - 提供设置界面的显示和交互功能
    /// </summary>
    public interface ISettingView
    {
        /// <summary>
        /// AI服务提供商
        /// </summary>
        string Provider { get; set; }

        /// <summary>
        /// API密钥
        /// </summary>
        string ApiKey { get; set; }

        /// <summary>
        /// API端点
        /// </summary>
        string ApiEndpoint { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        string Model { get; set; }

        /// <summary>
        /// 是否启用TTS
        /// </summary>
        bool TTSEnabled { get; set; }

        /// <summary>
        /// TTS API密钥
        /// </summary>
        string TtsApiKey { get; set; }

        /// <summary>
        /// TTS语音名称
        /// </summary>
        string TtsVoice { get; set; }

        /// <summary>
        /// TTS语速（1-100）
        /// </summary>
        int TTSSpeed { get; set; }

        /// <summary>
        /// TTS音量（0-100）
        /// </summary>
        int TTSVolume { get; set; }

        /// <summary>
        /// 字体大小
        /// </summary>
        int FontSize { get; set; }

        /// <summary>
        /// 主题名称
        /// </summary>
        string Theme { get; set; }

        /// <summary>
        /// 百度应用ID
        /// </summary>
        string BaiduAppId { get; set; }

        /// <summary>
        /// 百度密钥
        /// </summary>
        string BaiduSecret { get; set; }

        /// <summary>
        /// 百度网盘客户端ID
        /// </summary>
        string BaiduNetdiskClientId { get; set; }

        /// <summary>
        /// 百度网盘客户端密钥
        /// </summary>
        string BaiduNetdiskClientSecret { get; set; }

        /// <summary>
        /// 是否启用语音
        /// </summary>
        bool IsVoiceEnabled { get; set; }

        /// <summary>
        /// 发音范围（0:原文, 1:解释, 2:两者）
        /// </summary>
        int PronunciationScope { get; set; }

        /// <summary>
        /// 是否启用AI解释
        /// </summary>
        bool IsAIExplanationEnabled { get; set; }

        /// <summary>
        /// 保存按钮点击事件
        /// </summary>
        event EventHandler? SaveClicked;

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        event EventHandler? CancelClicked;

        /// <summary>
        /// 打开网页版AI按钮点击事件
        /// </summary>
        event EventHandler? OpenWebViewClicked;

        /// <summary>
        /// 显示消息
        /// </summary>
        /// <param name="msg">消息内容</param>
        void ShowMessage(string msg);

        /// <summary>
        /// 关闭设置视图
        /// </summary>
        void CloseView();
    }
}
