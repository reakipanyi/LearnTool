using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

/// <summary>「音频」侧栏容器：分段切换条 + 配套音频 / 整页录音 / 句子跟读三个子页。</summary>
public partial class PdfAudioSidebarPanel : UserControl
{
    public PdfAudioSidebarPanel()
    {
        InitializeComponent();

        // 三个子页仅作为内容宿主，页签条由上方的 SegmentedTabs 承载
        segments.SetItems("配套音频", "整页录音", "句子跟读");
        segments.SelectedIndexChanged += (_, _) =>
        {
            if (tabs.SelectedIndex != segments.SelectedIndex)
                tabs.SelectedIndex = segments.SelectedIndex;
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    /// <summary>分段切换后触发，宿主据此刷新当前子页内容。</summary>
    public event EventHandler? SelectedIndexChanged;

    /// <summary>当前选中的子页索引（0 配套音频 / 1 整页录音 / 2 句子跟读）。</summary>
    public int SelectedIndex => segments.SelectedIndex;

    /// <summary>分段切换条。</summary>
    public SegmentedTabs Segments => segments;

    /// <summary>配套音频子页，宿主在此挂载音频面板。</summary>
    public TabPage CompanionPage => companionPage;

    /// <summary>整页录音子页，录音管理器在此挂载 UI。</summary>
    public TabPage RecordingPage => recordingPage;

    /// <summary>句子跟读子页，宿主在此挂载跟读面板。</summary>
    public TabPage ShadowingPage => shadowingPage;
}
