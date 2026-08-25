
namespace LearningAssistant.Common.Events
{
    // ==========================================
    // 学习相关事件
    // ==========================================

    /// <summary>
    /// 学习会话开始事件
    /// </summary>
    public class LearningSessionStartedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string WordBankFile { get; set; } = string.Empty;
    }

    /// <summary>
    /// 学习项已完成事件 - ItemLearned
    /// 触发源：学习页点击"会了"
    /// 下游动作：
    /// 1. 每日挑战进度+1
    /// 2. XP+10
    /// 3. 检查成就条件
    /// 4. 加入间隔重复队列
    /// </summary>
    public class ItemLearnedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public string ItemContent { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string LearningType { get; set; } = string.Empty;
        public DateTime LearnedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 学习项答错事件 - ItemWrong
    /// 触发源：学习页点击"不会"
    /// 下游动作：
    /// 1. 记入错题本
    /// 2. 每日挑战进度暂停
    /// 3. 错题达10道 → 解锁"屡败屡战"成就
    /// </summary>
    public class ItemWrongEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public string ItemContent { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public string UserAnswer { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string LearningType { get; set; } = string.Empty;
        public DateTime WrongAt { get; set; } = DateTime.Now;
    }
    public class LearningItemCompletedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
    }
    /// <summary>
    /// 学习会话完成事件
    /// </summary>
    public class LearningSessionCompletedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public int CorrectCount { get; set; }
        public double Accuracy { get; set; }
        public string SubCategory { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
    }

    // ==========================================
    // 复习相关事件
    // ==========================================

    /// <summary>
    /// 复习完成事件 - ReviewDone
    /// 触发源：复习模块完成1项
    /// 下游动作：
    /// 1. 更新记忆保持率趋势
    /// 2. 错题本中对应项自动移除
    /// </summary>
    public class ReviewDoneEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public string ItemContent { get; set; } = string.Empty;
        public bool WasCorrect { get; set; }
        public DateTime ReviewedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 复习会话开始事件
    /// </summary>
    public class ReviewSessionStartedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public int DueCount { get; set; }
    }

    /// <summary>
    /// 复习会话完成事件
    /// </summary>
    public class ReviewSessionCompletedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public int TotalReviewed { get; set; }
        public int CorrectCount { get; set; }
        public double RetentionRate { get; set; }
    }

    // ==========================================
    // PDF/高亮相关事件
    // ==========================================

    /// <summary>
    /// PDF创建高亮事件 - PDFHighlight
    /// 触发源：PDF阅读器创建高亮
    /// 下游动作：
    /// 1. 弹出"是否生成复习卡？"
    /// 2. 确认后自动创建学习项
    /// 3. 加入间隔重复队列
    /// </summary>
    public class PDFHighlightEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public string PdfFileName { get; set; } = string.Empty;
        public string HighlightedText { get; set; } = string.Empty;
        public string SourceUrl { get; set; } = string.Empty;
        public string? SelectedCategory { get; set; }
        public string? Tags { get; set; }
        public DateTime HighlightedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 发送文本到PDF搜索事件 - SendToPdfSearch
    /// 触发源：学习页点击"发送到PDF问题"
    /// 下游动作：
    /// 1. PDF阅读器搜索并高亮匹配文本
    /// 2. 自动定位到匹配页面
    /// </summary>
    public class SendToPdfSearchEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public string SearchText { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
    }

    // ==========================================
    // 挑战相关事件
    // ==========================================

    /// <summary>
    /// 挑战完成事件 - ChallengeCompleted
    /// 触发源：完成1项挑战
    /// 下游动作：
    /// 1. XP飘字动画
    /// 2. 检查全部完成 → "本周达人"成就进度+1
    /// </summary>
    public class ChallengeCompletedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public string ChallengeId { get; set; } = string.Empty;
        public string ChallengeName { get; set; } = string.Empty;
        public int XpReward { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 每日挑战刷新事件
    /// </summary>
    public class DailyChallengesRefreshedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public DateTime RefreshedAt { get; set; } = DateTime.Now;
        public DateTime NextRefreshAt { get; set; }
    }

    // ==========================================
    // 成就相关事件
    // ==========================================

    /// <summary>
    /// 成就解锁事件 - AchievementUnlocked
    /// </summary>
    public class AchievementUnlockedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public string AchievementId { get; set; } = string.Empty;
        public string AchievementName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsHidden { get; set; }
        public DateTime UnlockedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 成就进度更新事件
    /// </summary>
    public class AchievementProgressUpdatedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public string AchievementId { get; set; } = string.Empty;
        public int CurrentProgress { get; set; }
        public int TargetProgress { get; set; }
    }

    // ==========================================
    // 费曼学习法相关事件
    // ==========================================

    /// <summary>
    /// 费曼学习完成事件 - FeynmanCompleted
    /// </summary>
    public class FeynmanCompletedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public string ItemContent { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string SimplifiedText { get; set; } = string.Empty;
        public double SimplificationRate { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.Now;
    }

    // ==========================================
    // XP/经验值相关事件
    // ==========================================

    /// <summary>
    /// XP增加事件
    /// </summary>
    public class XPGainedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 用户等级提升事件
    /// </summary>
    public class LevelUpEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public int OldLevel { get; set; }
        public int NewLevel { get; set; }
    }

    // ==========================================
    // 收藏相关事件
    // ==========================================

    /// <summary>
    /// 收藏添加事件
    /// </summary>
    public class FavoriteAddedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public string ItemContent { get; set; } = string.Empty;
    }

    /// <summary>
    /// 收藏移除事件
    /// </summary>
    public class FavoriteRemovedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
    }

    // ==========================================
    // 主题相关事件
    // ==========================================

    /// <summary>
    /// 主题切换事件
    /// </summary>
    public class ThemeChangedEvent : ApplicationEventBase
    {
        public ThemeMode NewTheme { get; set; }
        public ThemeMode OldTheme { get; set; }
    }

    // ==========================================
    // 用户相关事件
    // ==========================================

    /// <summary>
    /// 用户资料更新事件
    /// </summary>
    public class UserProfileUpdatedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
    }

    // ==========================================
    // 番茄钟相关事件
    // ==========================================

    /// <summary>
    /// 番茄钟完成事件 - PomodoroCompleted
    /// 触发源：番茄钟完成1个工作周期
    /// 下游动作：
    /// 1. 更新学习时长目标进度
    /// 2. XP+25（完成一个番茄钟）
    /// 3. 检查"专注达人"成就
    /// </summary>
    public class PomodoroCompletedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string? TaskName { get; set; }
        public int CompletedCount { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.Now;
    }

    // ==========================================
    // 笔记相关事件
    // ==========================================

    /// <summary>
    /// 笔记添加事件 - NoteAdded
    /// 触发源：用户添加新笔记
    /// 下游动作：
    /// 1. XP+15（记录笔记）
    /// 2. 更新学习目标进度
    /// </summary>
    public class NoteAddedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public string NoteId { get; set; } = string.Empty;
        public string NoteTitle { get; set; } = string.Empty;
        public string? RelatedType { get; set; }
        public string? RelatedItemId { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;
    }
}

