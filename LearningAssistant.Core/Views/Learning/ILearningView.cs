using LearningAssistant.Common;
using LearningAssistant.Managers;
using LearningAssistant.Models.Learning;
using System.Collections.Generic;

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

        LearningContext CurrentContext { get; }

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

        /// <summary>
        /// 更新学习项的已知/未知状态，用于列表对勾显示同步
        /// </summary>
        /// <param name="knownItems">已知项内容集合</param>
        /// <param name="unknownItems">未知项内容集合</param>
        void UpdateLearningItemStates(HashSet<string> knownItems, HashSet<string> unknownItems);

        void RefreshSubCategories(List<SubCategoryType> subCategories);

        string SearchText { get; set; }
        event EventHandler? SearchTextChanged;

        /// <summary>
        /// 设置加载状态
        /// </summary>
        /// <param name="isLoading">是否正在加载</param>
        /// <param name="message">加载提示文本</param>
        void SetLoadingState(bool isLoading, string message = "加载中...");

        /// <summary>
        /// 字段发音请求事件
        /// </summary>
        event EventHandler<FieldSpeakEventArgs>? FieldSpeakRequested;

        /// <summary>
        /// 字段停止发音请求事件
        /// </summary>
        event EventHandler<FieldSpeakEventArgs>? FieldStopRequested;

        /// <summary>
        /// 字段复制请求事件
        /// </summary>
        event EventHandler<FieldCopyEventArgs>? FieldCopyRequested;

        /// <summary>
        /// 将文本复制到剪贴板
        /// </summary>
        void CopyToClipboard(string text);
    }

    public class FieldSpeakEventArgs : EventArgs
    {
        public string SpeakText { get; }
        public string Language { get; }
        public string? SpeakKey { get; }

        public FieldSpeakEventArgs(string speakText, string language, string? speakKey = null)
        {
            SpeakText = speakText;
            Language = language;
            SpeakKey = speakKey;
        }
    }

    public class FieldCopyEventArgs : EventArgs
    {
        public string Value { get; }

        public FieldCopyEventArgs(string value)
        {
            Value = value;
        }
    }
}
