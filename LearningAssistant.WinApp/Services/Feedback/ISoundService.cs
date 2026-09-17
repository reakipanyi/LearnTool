namespace LearningAssistant.Services.Feedback
{
    /// <summary>
    /// 音效服务接口 - 提供各种场景的音效播放功能
    /// </summary>
    public interface ISoundService
    {
        /// <summary>
        /// 播放成功音效（如答对题目时）
        /// </summary>
        void PlaySuccess();

        /// <summary>
        /// 播放错误音效（如答错题目时）
        /// </summary>
        void PlayError();

        /// <summary>
        /// 播放导航音效（如切换页面时）
        /// </summary>
        void PlayNavigation();

        /// <summary>
        /// 播放成就解锁音效
        /// </summary>
        void PlayAchievement();

        /// <summary>
        /// 播放点击音效（如按钮点击时）
        /// </summary>
        void PlayClick();
    }
}
