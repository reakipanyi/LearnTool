using System.Windows.Forms;

namespace LearningAssistant.Services.Gamification
{
    /// <summary>
    /// WinForms 专属的游戏化 UI 绑定接口。
    /// Core 的 IGamificationService 不含 WinForms 类型；
    /// 需要直接操作 Label/FlowLayoutPanel 等控件时通过此接口访问。
    /// </summary>
    public interface IGamificationUIBinding
    {
        void SetStatsUI(Label studyTime, Label score, Label todayCount, Label streak,
            Label? level, Label? xp, ProgressBar? progressXp);

        void SetBadgeUI(FlowLayoutPanel panel, ToolTip toolTip);

        void SetChallengeUI(FlowLayoutPanel panel, object? soundService = null);
    }
}
