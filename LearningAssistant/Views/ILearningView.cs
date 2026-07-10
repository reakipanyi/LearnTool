using LearningAssistant.Common;
using LearningAssistant.Managers;

namespace LearningAssistant.Views
{
    public enum PronunciationScope
    {
        Original,
        Explanation,
        Both
    }

    public interface ILearningView
    {
        string CurrentContent { set; }
        string CurrentDisplayText { set; }
        string CurrentDisplayStruct { set; }
        Models.Learning.LearningItem? CurrentItem { set; }
        string Statistics { set; }
        int ProgressValue { set; }
        int ProgressMax { set; }
        bool IsVoiceEnabled { get; set; }
        PronunciationScope PronunciationScope { get; set; }

        LearningModeType CurrentMode { get; }
        LearningModeType LearningMode { get; }
        SortOrderType SortOrder { get; }
        SubjectType Subject { get; }
        SubCategoryType SubCategory { get; set; }

        /// <summary>
        /// 标记为已知点击事件
        /// </summary>
        event EventHandler? MarkAsKnownClicked;

        /// <summary>
        /// 标记为未知点击事件
        /// </summary>
        event EventHandler? MarkAsUnknownClicked;

        /// <summary>
        /// 发音按钮点击事件
        /// </summary>
        event EventHandler? PronounceClicked;

        /// <summary>
        /// 下一个点击事件
        /// </summary>
        event EventHandler? NextClicked;

        /// <summary>
        /// 退出点击事件
        /// </summary>
        event EventHandler? ExitClicked;

        /// <summary>
        /// 添加到PDF问题点击事件
        /// </summary>
        event EventHandler? AddToPdfQuestionClicked;

        /// <summary>
        /// 设置变更事件
        /// </summary>
        event EventHandler? SettingsChanged;

        /// <summary>
        /// 打开统计点击事件
        /// </summary>
        event EventHandler? OpenStatisticsClicked;

        /// <summary>
        /// 导出错题本点击事件
        /// </summary>
        event EventHandler? ExportErrorBookClicked;

        /// <summary>
        /// 从列表中选择项事件
        /// </summary>
        event EventHandler<ItemSelectedEventArgs>? ItemSelectedFromList;

        /// <summary>
        /// 显示消息
        /// </summary>
        /// <param name="msg">消息内容</param>
        void ShowMessage(string msg);

        /// <summary>
        /// 启用/禁用按钮
        /// </summary>
        /// <param name="enabled">是否启用</param>
        void EnableButtons(bool enabled);

        /// <summary>
        /// 播放发音
        /// </summary>
        /// <param name="text">要发音的文本</param>
        /// <param name="language">语言代码</param>
        Task PlayPronunciationAsync(string text, string language);


        /// <summary>
        /// 更新学习列表显示
        /// </summary>
        /// <param name="items">学习项内容列表</param>
        /// <param name="currentIndex">当前索引</param>
        void UpdateLearningList(List<string> items, int currentIndex);

        /// <summary>
        /// 更新学习列表选中项
        /// </summary>
        /// <param name="currentIndex">当前索引</param>
        void UpdateLearningListSelection(int currentIndex);

        void RefreshSubCategories(List<SubCategoryType> subCategories);

        string SearchText { get; set; }
        event EventHandler? SearchTextChanged;

        /// <summary>
        /// 设置加载状态
        /// </summary>
        /// <param name="isLoading">是否正在加载</param>
        /// <param name="message">加载提示文本</param>
        void SetLoadingState(bool isLoading, string message = "加载中...");
    }
}
