using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习引擎接口 - 核心学习流程控制引擎
    /// 负责管理学习进度、记忆卡片流转、统计分析等功能
    /// </summary>
    public interface IStudyEngine
    {
        /// <summary>
        /// 初始化学习引擎
        /// </summary>
        /// <param name="userId">用户ID，用于标识学习记录归属</param>
        /// <param name="language">学习语言（如 en, ja, ko）</param>
        /// <param name="subCategory">学习子类别（如 CET4, CET6, GRE）</param>
        /// <param name="wordBankFile">词库文件路径</param>
        /// <param name="mode">学习模式（Study:学习模式, Test:测试模式）</param>
        /// <param name="sortOrder">排序方式（Sequential:顺序, Random:随机）</param>
        /// <param name="continueMode">是否继续上次进度</param>
        void Initialize(string userId, string language, string subCategory, string wordBankFile, string mode = "Study", string sortOrder = "Sequential", bool continueMode = true);

        /// <summary>
        /// 获取当前学习项
        /// </summary>
        /// <returns>当前学习的LearningItem，若无则返回null</returns>
        LearningItem? GetCurrentItem();

        /// <summary>
        /// 判断是否还有下一个学习项
        /// </summary>
        /// <returns>有下一个返回true</returns>
        bool HasNext();

        /// <summary>
        /// 移动到下一个学习项
        /// </summary>
        void MoveNext();

        /// <summary>
        /// 设置当前学习项索引
        /// </summary>
        /// <param name="index">目标索引位置</param>
        void SetCurrentIndex(int index);

        /// <summary>
        /// 标记当前项为已掌握
        /// 会将当前项从UnknownItems移除并添加到KnownItems
        /// </summary>
        void MarkCurrentAsKnown();

        /// <summary>
        /// 标记当前项为未掌握
        /// 会将当前项添加到UnknownItems用于后续复习
        /// </summary>
        void MarkCurrentAsUnknown();

        /// <summary>
        /// 批量标记多个内容为已掌握
        /// 一次性处理多个学习项，减少持久化IO次数
        /// </summary>
        /// <param name="contents">要标记为已掌握的学习项内容集合</param>
        /// <returns>实际标记的项数量</returns>
        int MarkItemsAsKnown(IEnumerable<string> contents);

        /// <summary>
        /// 批量标记多个内容为未掌握
        /// </summary>
        /// <param name="contents">要标记为未掌握的学习项内容集合</param>
        /// <returns>实际标记的项数量</returns>
        int MarkItemsAsUnknown(IEnumerable<string> contents);

        /// <summary>
        /// 获取学习统计数据
        /// </summary>
        /// <returns>包含测试次数、正确次数、准确率等统计信息</returns>
        StudyStatistics GetStatistics();

        /// <summary>
        /// 保存当前学习进度到持久化存储
        /// </summary>
        void SaveProgress();

        /// <summary>
        /// 重置学习进度
        /// 清空KnownItems和UnknownItems，将当前索引归零
        /// </summary>
        void ResetProgress();

        /// <summary>
        /// 获取所有未掌握的项列表
        /// </summary>
        /// <returns>UnknownItems中所有LearningItem的副本</returns>
        List<LearningItem> GetUnknownItems();

        /// <summary>
        /// PDF生词本联动 - 添加未掌握项
        /// 当用户在PDF阅读中标记生词时调用
        /// </summary>
        /// <param name="content">生词内容</param>
        /// <param name="subCategory">生词所属子类别</param>
        void AddUnknownItem(string content, string subCategory);

        /// <summary>
        /// 应用学习设置
        /// </summary>
        /// <param name="mode">学习模式</param>
        /// <param name="sortOrder">排序方式</param>
        void ApplySettings(string mode, string sortOrder);

        /// <summary>
        /// 当前学习项的索引位置（从0开始）
        /// </summary>
        int CurrentIndex { get; }

        /// <summary>
        /// 学习项总数
        /// </summary>
        int TotalCount { get; }

        /// <summary>
        /// 已掌握的学习项内容列表（只读）
        /// </summary>
        IReadOnlyList<string> KnownItems { get; }

        /// <summary>
        /// 未掌握的学习项内容列表（只读）
        /// </summary>
        IReadOnlyList<string> UnknownItems { get; }

        /// <summary>
        /// 当前学习模式
        /// </summary>
        string CurrentMode { get; }

        /// <summary>
        /// 当前排序方式
        /// </summary>
        string CurrentSortOrder { get; }

        /// <summary>
        /// 是否存在已保存的学习进度
        /// </summary>
        bool HasSavedProgress { get; }

        /// <summary>
        /// 获取所有学习项
        /// </summary>
        /// <returns>全部LearningItem列表</returns>
        List<LearningItem> GetAllItems();

        // ========== 进度查询方法（原IProgressService功能）==========

        /// <summary>
        /// 获取学习进度摘要
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="language">学习语言</param>
        /// <param name="subCategory">学习子类别</param>
        /// <returns>进度摘要文本</returns>
        string GetProgressSummary(string userId, string language, string subCategory);

        /// <summary>
        /// 获取已掌握的项数量
        /// </summary>
        int GetKnownCount(string userId, string subCategory);

        /// <summary>
        /// 获取未掌握的项数量
        /// </summary>
        int GetUnknownCount(string userId, string subCategory);

        /// <summary>
        /// 获取学习准确率
        /// </summary>
        double GetAccuracy(string userId, string subCategory);

        /// <summary>
        /// 获取用户的未掌握项列表（全局）
        /// </summary>
        List<string> GetUnknownItems(string userId);
    }

    /// <summary>
    /// 学习统计数据结构
    /// </summary>
    public class StudyStatistics
    {
        /// <summary>
        /// 总测试次数
        /// </summary>
        public int TotalTestCount { get; set; }

        /// <summary>
        /// 正确次数
        /// </summary>
        public int CorrectCount { get; set; }

        /// <summary>
        /// 最后测试日期时间
        /// </summary>
        public DateTime LastTestDate { get; set; }

        /// <summary>
        /// 计算属性：准确率（百分比）
        /// 计算公式：(正确次数 / 总次数) * 100
        /// 当总次数为0时返回0避免除零错误
        /// </summary>
        public double AccuracyRate => TotalTestCount > 0 ? (double)CorrectCount / TotalTestCount * 100 : 0;
    }
}
