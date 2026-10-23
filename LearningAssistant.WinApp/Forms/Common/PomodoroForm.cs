using LearningAssistant.Forms.UserControls.Common;
using LearningAssistant.Services.Learning;

namespace LearningAssistant.Forms.Common;

/// <summary>番茄钟独立窗口：承载已实现的 PomodoroTimer 控件。</summary>
public sealed class PomodoroForm : Form
{
    private readonly PomodoroTimer _timerControl;

    public PomodoroForm(IPomodoroService pomodoroService)
    {
        Text = "番茄钟";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(304, 372);
        BackColor = Color.FromArgb(248, 250, 252);
        Font = new Font("Microsoft YaHei UI", 9F);

        _timerControl = new PomodoroTimer
        {
            Dock = DockStyle.Fill,
            PomodoroService = pomodoroService
        };
        _timerControl.StartClicked += (_, _) => pomodoroService.Start();
        _timerControl.PauseClicked += (_, _) => pomodoroService.Pause();
        _timerControl.ResetClicked += (_, _) => pomodoroService.Reset();
        _timerControl.SkipClicked += (_, _) => pomodoroService.Skip();

        Controls.Add(_timerControl);
    }
}
