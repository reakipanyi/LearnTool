using LearningAssistant.Managers;

namespace LearningAssistant.Views
{
    /// <summary>
    /// 发音范围枚举 - 控制哪些内容需要发音
    /// </summary>
    public enum PronunciationScope
    {
        /// <summary>
        /// 仅原文发音
        /// </summary>
        Original,

        /// <summary>
        /// 仅解释发音
        /// </summary>
        Explanation,

        /// <summary>
        /// 两者都发音
        /// </summary>
        Both
    }

    /// <summary>
    /// 学习视图接口 - 提供学习界面的显示和交互功能
    /// </summary>
    public interface ILearningView
    {
        /// <summary>
        /// 当前学习内容
        /// </summary>
        string CurrentContent { set; }

        /// <summary>
        /// 当前显示文本（带格式）
        /// </summary>
        string CurrentDisplayText { set; }
        string CurrentDisplayStruct { set; }

        /// <summary>
        /// 当前学习项
        /// </summary>
        Models.Learning.LearningItem? CurrentItem { set; }


        /// <summary>
        /// 统计信息文本
        /// </summary>
        string Statistics { set; }

        /// <summary>
        /// 当前进度值
        /// </summary>
        int ProgressValue { set; }

        /// <summary>
        /// 进度最大值
        /// </summary>
        int ProgressMax { set; }

        /// <summary>
        /// 是否启用语音
        /// </summary>
        bool IsVoiceEnabled { get; set; }


        /// <summary>
        /// 发音范围
        /// </summary>
        PronunciationScope PronunciationScope { get; set; }

        /// <summary>
        /// 当前学习模式
        /// </summary>
        string CurrentMode { get; }

        /// <summary>
        /// 学习模式（Study/Test）
        /// </summary>
        string LearningMode { get; }

        /// <summary>
        /// 排序方式
        /// </summary>
        string SortOrder { get; }

        /// <summary>
        /// 学习语言（兼容旧版）
        /// </summary>
        string Language { get; }

        /// <summary>
        /// 学习学科
        /// </summary>
        string Subject { get; }

        /// <summary>
        /// 当前子类别
        /// </summary>
        string SubCategory { get; set; }

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
        void PlayPronunciation(string text, string language);


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

        /// <summary>
        /// 刷新子类别列表
        /// </summary>
        /// <param name="subCategories">子类别列表</param>
        void RefreshSubCategories(List<string> subCategories);

        /// <summary>
        /// 设置加载状态
        /// </summary>
        /// <param name="isLoading">是否正在加载</param>
        /// <param name="message">加载提示文本</param>
        void SetLoadingState(bool isLoading, string message = "加载中...");
    }
}
