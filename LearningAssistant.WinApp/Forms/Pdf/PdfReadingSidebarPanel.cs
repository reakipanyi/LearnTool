using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

/// <summary>「阅读」侧栏容器：分段切换「文件/页面」与「缩略图」两个子页。</summary>
public partial class PdfReadingSidebarPanel : UserControl
{
    public PdfReadingSidebarPanel()
    {
        InitializeComponent();

        segments.SetItems("文件 / 页面", "缩略图");
        segments.SelectedIndexChanged += (_, _) =>
        {
            if (tabs.SelectedIndex != segments.SelectedIndex)
                tabs.SelectedIndex = segments.SelectedIndex;
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        };
        // 直接点击内容宿主时也同步分段条，保证子页与页签不会脱节
        tabs.SelectedIndexChanged += (_, _) =>
        {
            if (segments.SelectedIndex != tabs.SelectedIndex)
                segments.SelectedIndex = tabs.SelectedIndex;
        };
    }

    /// <summary>分段切换后触发，宿主据此按需加载缩略图。</summary>
    public event EventHandler? SelectedIndexChanged;

    /// <summary>当前子页索引（0 文件/页面，1 缩略图）。</summary>
    public int SelectedIndex => segments.SelectedIndex;

    /// <summary>文件树子页，宿主挂载文件筛选框与目录树。</summary>
    public TabPage FilesPage => filesPage;

    /// <summary>缩略图子页，宿主挂载缩略图面板。</summary>
    public TabPage ThumbnailsPage => thumbnailsPage;
}
