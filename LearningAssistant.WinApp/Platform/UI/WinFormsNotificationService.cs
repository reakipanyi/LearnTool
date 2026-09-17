using System.Windows.Forms;
using LearningAssistant.Abstractions;

namespace LearningAssistant.Platform
{
    /// <summary>
    /// WinForms 端 INotificationService 实现，用 MessageBox 作为简易通知。
    /// </summary>
    public class WinFormsNotificationService : INotificationService
    {
        public void ShowToast(string title, string message)
        {
            // WinForms 无原生 Toast，用 MessageBox 代替
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void ShowReminder(string title, string message, DateTime? scheduledTime = null)
        {
            // WinForms 无原生计划提醒，用即时 MessageBox 代替
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}