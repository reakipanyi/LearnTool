using LearningAssistant.Common.UI;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Presenters;

using LearningAssistant.Services.Pdf;

namespace LearningAssistant.Managers
{
    public interface IPdfReaderFormAccess
    {
        string CurrentPdfPath { get; set; }
        int CurrentPageIndex { get; set; }
        Bitmap? CurrentPageImage { get; set; }
        Bitmap? SecondPageImage { get; set; }
        bool IsDualPage { get; }
        bool IsTranslationEnabled { get; set; }
        bool IsNightMode { get; }
        bool AutoSpeakAfterOcr { get; }
        bool AutoTranslateAfterOcr { get; }

        PictureBox PictureBoxPdf { get; }
        PdfPresenter? Presenter { get; set; }
        TextBox TextBoxOriginal { get; }
        TextBox TextBoxPage { get; }
        Label LabelZoom { get; }
        PdfToolbarSlider TrackBarZoom { get; }

        Button? ButtonNightMode { get; }
        Button? ButtonLanguage { get; }
        Button? ButtonAskAi { get; }

        TabPage? TabPageTranslate { get; }
        TextBox? TextBoxTranslation { get; }

        TabPage? TabPageBookmarksAndHighlights { get; }

        ListBox? ListBoxHighlights { get; }
        Button? ButtonRemoveHighlight { get; }
        Button? ButtonBatchRemoveHighlight { get; }

        Panel? PanelPdf { get; }
        Panel? PanelNavigation { get; }
        Panel? PanelLeftContainer { get; }
        TreeView? TreeViewFiles { get; }
        TabControl? TabControlLeft { get; }

        /// <summary>重新应用侧栏主题（四个页签面板与右侧音频抽屉）</summary>
        void ApplySidebarTheme();
        Panel? PanelThumbnails { get; }
        FlowLayoutPanel? FlowLayoutPanelThumbnails { get; }

        Panel? PageTransitionOverlay { get; }
        System.Windows.Forms.Timer? PageTransitionTimer { get; }
        Button? ButtonLockView { get; }

        /// <summary>刷新工具栏“锁定视图”按钮的开启态配色（锁定 = 品牌柔底）</summary>
        void ApplyLockViewButtonState(bool isLocked);

        Color BackColor { get; set; }

        Rectangle GetImageDisplayRect();

        /// <summary>重新加载当前页标注图层并刷新画面（不触发整页重渲染）</summary>
        void ReloadAnnotationLayer();
        (int pageIndex, Rectangle pageRect, Bitmap? pageImage) GetPageAtPoint(Point clientPoint);
        void DisplayImage(Bitmap bmp);
        void ShowWarning(string message);
        bool ShowConfirm(string message, string title);
        void ShowMessage(string message, string title);
        /// <summary>在窗口底部显示短暂提示</summary>
        void ShowToast(string message, int duration = 2000);

        event EventHandler? TranslateClicked;
        event EventHandler? SelectOcrClicked;

        void OnTranslateClicked();
        void OnSelectOcrClicked();

        Form Form { get; }

        /// <summary>高亮服务，用于 Select 模式调整高亮位置/大小后的持久化</summary>
        IHighlightService? HighlightService { get; }

        /// <summary>高亮控制器，用于删除选中/刷新图层</summary>
        PdfReaderHighlightManager? HighlightManager { get; }

        /// <summary>音频录制侧边栏标签页</summary>
        TabPage? TabPageAudio { get; }
        /// <summary>音频录制服务</summary>
        IAudioRecorderService? AudioRecorderService { get; }

        /// <summary>尝试读取选区翻译缓存；命中时返回本页该文本的译文。</summary>
        bool TryGetCachedSelectionTranslation(int pageIndex, string ocrText, out string translation);
        /// <summary>写入选区翻译缓存，供下次翻页回显。</summary>
        void StoreSelectionTranslation(int pageIndex, string ocrText, string translation);
        /// <summary>在顶部译条展示翻译加载态，返回本次请求代次。</summary>
        int ShowSelectionTranslationLoading(int pageIndex, string ocrText);
        /// <summary>展示翻译结果；过期代次会被丢弃。</summary>
        void CompleteSelectionTranslation(int requestId, int pageIndex, string ocrText, string translation, bool fromCache);
        /// <summary>展示翻译失败；过期代次会被丢弃。</summary>
        void FailSelectionTranslation(int requestId, int pageIndex, string ocrText, string message);

        /// <summary>
        /// 写入指定页面的原文；用于跨页高亮 OCR 时保留真实来源页，
        /// 避免翻译面板内容被误当成当前显示页的文本。
        /// </summary>
        void SetOriginalTextForPage(int pageIndex, string text);

        /// <summary>
        /// 把高亮送入翻译面板的「摘录」槽；用于显式翻译、自动框点击等一次性关联，
        /// 不改变整页槽内容。
        /// </summary>
        void ShowHighlightInTranslatePanel(PdfHighlight highlight);
    }
}
