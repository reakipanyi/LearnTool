namespace LearningAssistant.Abstractions
{
    /// <summary>
    /// 对话框服务抽象，替代 MessageBox.Show / InputBox / OpenFileDialog / SaveFileDialog。
    /// </summary>
    public interface IDialogService
    {
        Task<bool> ConfirmAsync(string title, string message);
        Task ShowMessageAsync(string title, string message);
        Task<string?> PromptAsync(string title, string defaultValue);
        Task<string?> PickFileOpenAsync(string filter);
        Task<string?> PickFileSaveAsync(string filter, string defaultName);
    }
}