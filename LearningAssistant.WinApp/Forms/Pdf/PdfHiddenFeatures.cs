namespace LearningAssistant.Forms.Pdf
{
    /// <summary>
    /// 已下线功能的入口开关：代码与历史数据全部保留，仅隐藏界面入口，便于后续恢复。
    /// </summary>
    internal static class PdfHiddenFeatures
    {
        /// <summary>跟读打点（播放器中打断录音并切片）。</summary>
        public const bool EnableShadowingMark = false;

        /// <summary>整页录音（音频抽屉、学习页与命令面板入口）。</summary>
        public const bool EnablePageRecording = false;
    }
}
