namespace LearningAssistant.Views
{
    /// <summary>
    /// 设置视图接口 - 提供设置界面的显示和交互功能
    /// </summary>
    public interface ISettingView
    {
        /// <summary>
        /// 是否启用TTS
        /// </summary>
        bool TTSEnabled { get; set; }

        /// <summary>
        /// TTS提供商
        /// </summary>
        string TtsProvider { get; set; }

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
        string BaiduAppId { get; set; }
        string BaiduSecret { get; set; }

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
        /// 当前选中的用户ID
        /// </summary>
        string SelectedUserId { get; set; }

        /// <summary>
        /// 设置用户列表显示
        /// </summary>
        void SetUserList(IList<string> userIds);

        /// <summary>
        /// 保存按钮点击事件
        /// </summary>
        event EventHandler? SaveClicked;

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        event EventHandler? CancelClicked;

        /// <summary>
        /// 添加用户按钮点击事件
        /// </summary>
        event EventHandler? AddUserClicked;

        /// <summary>
        /// 删除用户按钮点击事件
        /// </summary>
        event EventHandler? DeleteUserClicked;

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
