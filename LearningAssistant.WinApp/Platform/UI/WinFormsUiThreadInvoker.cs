using System.Windows.Forms;
using LearningAssistant.Abstractions;

namespace LearningAssistant.Platform
{
    /// <summary>
    /// WinForms 端 IUiThreadInvoker 实现，用 Application.OpenForms[0] 获取主窗体。
    /// </summary>
    public class WinFormsUiThreadInvoker : IUiThreadInvoker
    {
        private static Control? GetMainControl()
        {
            return Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
        }

        public void Invoke(Action action)
        {
            var ctrl = GetMainControl();
            if (ctrl != null && ctrl.IsHandleCreated && ctrl.InvokeRequired)
                ctrl.Invoke(action);
            else
                action();
        }

        public T Invoke<T>(Func<T> func)
        {
            var ctrl = GetMainControl();
            if (ctrl != null && ctrl.IsHandleCreated && ctrl.InvokeRequired)
                return (T)ctrl.Invoke(func);
            return func();
        }

        public void BeginInvoke(Action action)
        {
            var ctrl = GetMainControl();
            if (ctrl != null && ctrl.IsHandleCreated && ctrl.InvokeRequired)
                ctrl.BeginInvoke(action);
            else
                action();
        }
    }
}