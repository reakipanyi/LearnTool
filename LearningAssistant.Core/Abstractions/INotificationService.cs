namespace LearningAssistant.Abstractions
{
    /// <summary>
    /// 通知服务抽象：WinForms 端用 ToastNotification / MessageBox，
    /// MAUI 端用 LocalNotification。
    /// </summary>
    public interface INotificationService
    {
        void ShowToast(string title, string message);
        void ShowReminder(string title, string message, DateTime? scheduledTime = null);
    }
}