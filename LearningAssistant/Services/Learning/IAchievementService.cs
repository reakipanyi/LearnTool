
using LearningAssistant.Models.User;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 成就服务接口 - 管理学习成就的加载、检查和解锁
    /// </summary>
    public interface IAchievementService
    {
        /// <summary>
        /// 加载用户成就进度
        /// </summary>
        /// <param name="profile">用户 Profile 对象</param>
        void LoadProgress(UserProfile profile);

        /// <summary>
        /// 检查并解锁达成的成就
        /// 根据学习进度判断是否满足成就条件，满足则触发解锁事件
        /// </summary>
        /// <param name="profile">用户 Profile</param>
        /// <param name="progress">当前学习进度</param>
        void CheckAndUnlockAchievements(UserProfile profile, LearningProgress progress);

        /// <summary>
        /// 获取所有成就列表（包括已解锁和未解锁）
        /// </summary>
        /// <returns>成就列表</returns>
        List<Achievement> GetAllAchievements();

        /// <summary>
        /// 获取已解锁的成就列表
        /// </summary>
        /// <returns>已解锁成就列表</returns>
        List<Achievement> GetUnlockedAchievements();

        /// <summary>
        /// 获取未解锁的成就列表
        /// </summary>
        /// <returns>未解锁成就列表</returns>
        List<Achievement> GetLockedAchievements();

        /// <summary>
        /// 成就解锁事件 - 当成就被解锁时触发
        /// </summary>
        event EventHandler<AchievementUnlockedEventArgs>? AchievementUnlocked;
    }
}
