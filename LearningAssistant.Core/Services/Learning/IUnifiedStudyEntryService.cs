using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Favorites;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习来源类型（P-001 统一学习入口）
    /// </summary>
    public enum StudySourceType
    {
        /// <summary>常规词库学习</summary>
        Normal,
        /// <summary>错题本复习</summary>
        WrongAnswer,
        /// <summary>收藏夹复习</summary>
        Favorite,
        /// <summary>学习路径</summary>
        LearningPath
    }

    /// <summary>
    /// 学习来源描述信息
    /// </summary>
    public class StudySourceInfo
    {
        /// <summary>来源唯一标识</summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>来源类型</summary>
        public StudySourceType SourceType { get; set; }
        /// <summary>显示名称</summary>
        public string DisplayName { get; set; } = string.Empty;
        /// <summary>内容数量</summary>
        public int ItemCount { get; set; }
        /// <summary>关联学科（如果适用）</summary>
        public SubjectType? Subject { get; set; }
        /// <summary>关联子分类（如果适用）</summary>
        public SubCategoryType? SubCategory { get; set; }
        /// <summary>关联词库文件（如果适用）</summary>
        public string? WordBankFile { get; set; }
        /// <summary>来源描述</summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 统一学习入口服务接口（P-001）
    /// <para>把错题本、收藏夹、学习路径等不同来源的学习内容统一接入 StudyEngine。</para>
    /// <para>安全重构原则：只聚合现有服务，不修改任何现有服务的代码。</para>
    /// </summary>
    public interface IUnifiedStudyEntryService
    {
        /// <summary>
        /// 获取用户所有可用的学习来源
        /// </summary>
        List<StudySourceInfo> GetAvailableSources(string userId);

        /// <summary>
        /// 获取指定来源的内容数量（用于分页/虚拟列表）
        /// </summary>
        int GetSourceItemCount(StudySourceInfo source, string userId);

        /// <summary>
        /// 从指定来源分页获取学习项
        /// </summary>
        List<LearningItem> GetItemsFromSource(StudySourceInfo source, string userId, int pageIndex = 0, int pageSize = 100);

        /// <summary>
        /// 尝试从来源创建标准 LearningContext（用于接入 StudyEngine 的常规学习流程）。
        /// 如果来源不支持标准 Context（如收藏夹内容跨分类），返回 null。
        /// </summary>
        LearningContext? TryCreateContext(StudySourceInfo source, string userId, LearningModeType mode = LearningModeType.Study);
    }
}
