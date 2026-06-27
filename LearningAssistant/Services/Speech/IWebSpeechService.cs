namespace LearningAssistant.Services.Speech
{
    /// <summary>
    /// Web Speech API 服务接口
    /// 基于WebView2实现语音识别和语音合成
    /// </summary>
    public interface IWebSpeechService
    {
        /// <summary>
        /// 语音合成 - 朗读文本
        /// </summary>
        Task SpeakAsync(string text, string language = "zh-CN", float rate = 1.0f);

        /// <summary>
        /// 停止语音合成
        /// </summary>
        void StopSpeaking();

        /// <summary>
        /// 单次语音识别
        /// </summary>
        Task<SpeechRecognitionResult> RecognizeOnceAsync(string language = "zh-CN");

        /// <summary>
        /// 开始连续语音识别
        /// </summary>
        Task StartContinuousRecognitionAsync(Action<string> onResult, string language = "zh-CN");

        /// <summary>
        /// 停止连续语音识别
        /// </summary>
        Task StopContinuousRecognitionAsync();

        /// <summary>
        /// 检查是否支持语音识别
        /// </summary>
        bool IsRecognitionSupported { get; }

        /// <summary>
        /// 检查是否正在录音
        /// </summary>
        bool IsListening { get; }

        /// <summary>
        /// 事件：语音识别结果
        /// </summary>
        event EventHandler<SpeechRecognitionResult>? RecognitionResult;

        /// <summary>
        /// 事件：语音识别开始
        /// </summary>
        event EventHandler? RecognitionStarted;

        /// <summary>
        /// 事件：语音识别结束
        /// </summary>
        event EventHandler? RecognitionEnded;
    }

    /// <summary>
    /// 语音识别结果
    /// </summary>
    public class SpeechRecognitionResult
    {
        /// <summary>
        /// 识别的文本
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 置信度（0-1）
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess => !string.IsNullOrEmpty(Text);

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? Error { get; set; }
    }
}
