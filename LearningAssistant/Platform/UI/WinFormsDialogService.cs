using System.Windows.Forms;
using LearningAssistant.Abstractions;

namespace LearningAssistant.Platform
{
    /// <summary>
    /// WinForms 端 IDialogService 实现，用 MessageBox + OpenFileDialog/SaveFileDialog。
    /// </summary>
    public class WinFormsDialogService : IDialogService
    {
        public Task<bool> ConfirmAsync(string title, string message)
        {
            var result = MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            return Task.FromResult(result == DialogResult.Yes);
        }

        public Task ShowMessageAsync(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return Task.CompletedTask;
        }

        public Task<string?> PromptAsync(string title, string defaultValue)
        {
            using var dialog = new InputDialog(title, defaultValue);
            var result = dialog.ShowDialog();
            return Task.FromResult(result == DialogResult.OK ? dialog.Value : null);
        }

        public Task<string?> PickFileOpenAsync(string filter)
        {
            using var dialog = new OpenFileDialog { Filter = filter };
            var result = dialog.ShowDialog();
            return Task.FromResult(result == DialogResult.OK ? dialog.FileName : null);
        }

        public Task<string?> PickFileSaveAsync(string filter, string defaultName)
        {
            using var dialog = new SaveFileDialog { Filter = filter, FileName = defaultName };
            var result = dialog.ShowDialog();
            return Task.FromResult(result == DialogResult.OK ? dialog.FileName : null);
        }
    }

    /// <summary>
    /// 简易输入对话框，替代 VB.NET InputBox
    /// </summary>
    internal class InputDialog : Form
    {
        private readonly TextBox _textBox;
        public string Value => _textBox.Text;

        public InputDialog(string title, string defaultValue)
        {
            Text = title;
            Width = 400;
            Height = 150;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            _textBox = new TextBox
            {
                Left = 10, Top = 10, Width = 370, Text = defaultValue
            };

            var okButton = new Button
            {
                Text = "确定", Left = 200, Width = 80, Top = 50, DialogResult = DialogResult.OK
            };
            var cancelButton = new Button
            {
                Text = "取消", Left = 290, Width = 80, Top = 50, DialogResult = DialogResult.Cancel
            };

            Controls.Add(_textBox);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
            AcceptButton = okButton;
            CancelButton = cancelButton;
        }
    }
}