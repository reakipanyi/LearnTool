namespace LearningAssistant.Services.Shadowing
{
    /// <summary>
    /// 发音参考评分器。实现位于平台层（System.Speech），Core 仅定义契约。
    /// </summary>
    public interface IPronunciationScorer
    {
        /// <summary>
        /// 当前设备是否具备可用的英语识别引擎。
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 不可用原因，用于在界面提示用户。
        /// </summary>
        string? UnavailableReason { get; }

        /// <summary>
        /// 对一段录音按参考文本打分；不可用或录音无效时返回 null。
        /// </summary>
        Task<PronunciationScoreResult?> ScoreAsync(
            string expectedText,
            string wavFilePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 仅识别录音文本，不评分；用于无字幕/无选区时从本页文字中匹配用户实际跟读的句子。
        /// 不可用或录音无效时返回 null。
        /// </summary>
        Task<string?> RecognizeTextAsync(
            string wavFilePath,
            CancellationToken cancellationToken = default);
    }
}
