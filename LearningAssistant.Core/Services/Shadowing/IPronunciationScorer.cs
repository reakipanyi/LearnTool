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
    }
}
