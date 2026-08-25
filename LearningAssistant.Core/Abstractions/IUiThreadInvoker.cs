namespace LearningAssistant.Abstractions
{
    /// <summary>
    /// UI 线程调用抽象：WinForms 端用 Control.Invoke/BeginInvoke，
    /// MAUI 端用 MainThread.BeginInvokeOnMainThread。
    /// </summary>
    public interface IUiThreadInvoker
    {
        void Invoke(Action action);
        T Invoke<T>(Func<T> func);
        void BeginInvoke(Action action);
    }
}