using Microsoft.Extensions.Logging;
using System.Windows.Forms;

namespace LearningAssistant.Services.SystemTray
{
    /// <summary>
    /// 系统托盘服务实现
    /// </summary>
    public class TrayIconService : ITrayIconService
    {
        private readonly ILogger<TrayIconService>? _logger;
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private Form? _mainWindow;
        private bool _showTrayIcon = true;
        private bool _minimizeToTray = true;
        private bool _closeToTray = true;
        private bool _isInitialized;

        public bool ShowTrayIcon
        {
            get => _showTrayIcon;
            set
            {
                _showTrayIcon = value;
                if (_notifyIcon != null)
                    _notifyIcon.Visible = value;
            }
        }

        public bool MinimizeToTray
        {
            get => _minimizeToTray;
            set => _minimizeToTray = value;
        }

        public bool CloseToTray
        {
            get => _closeToTray;
            set => _closeToTray = value;
        }

        public event EventHandler? TrayDoubleClick;
        public event EventHandler<bool>? VisibilityChanged;

        public TrayIconService(ILogger<TrayIconService>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public void Initialize(Form mainWindow)
        {
            if (_isInitialized)
                return;

            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));

            _contextMenu = new ContextMenuStrip();

            var showItem = new ToolStripMenuItem("显示主窗口");
            showItem.Click += OnShowMainWindowClick;
            _contextMenu.Items.Add(showItem);

            var hideItem = new ToolStripMenuItem("隐藏到托盘");
            hideItem.Click += OnHideToTrayClick;
            _contextMenu.Items.Add(hideItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += OnExitClick;
            _contextMenu.Items.Add(exitItem);

            _notifyIcon = new NotifyIcon
            {
                Visible = _showTrayIcon,
                ContextMenuStrip = _contextMenu,
                Text = Application.ProductName ?? "学习助手"
            };

            try
            {
                _notifyIcon.Icon = _mainWindow.Icon;
            }
            catch
            {
            }

            _notifyIcon.DoubleClick += OnNotifyIconDoubleClick;
            _notifyIcon.MouseClick += OnNotifyIconMouseClick;

            _mainWindow.Resize += OnMainWindowResize;
            _mainWindow.FormClosing += OnMainWindowFormClosing;

            _isInitialized = true;

            _logger?.LogInformation("系统托盘已初始化");
        }

        /// <inheritdoc/>
        public void Show()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
                _showTrayIcon = true;
            }
        }

        /// <inheritdoc/>
        public void Hide()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _showTrayIcon = false;
            }
        }

        /// <inheritdoc/>
        public void ShowMainWindow()
        {
            if (_mainWindow == null)
                return;

            _mainWindow.Show();
            _mainWindow.WindowState = FormWindowState.Normal;
            _mainWindow.Activate();
            _mainWindow.BringToFront();

            VisibilityChanged?.Invoke(this, true);
            _logger?.LogDebug("主窗口已显示");
        }

        /// <inheritdoc/>
        public void HideToTray()
        {
            if (_mainWindow == null)
                return;

            if (_notifyIcon != null && _showTrayIcon)
            {
                _mainWindow.Hide();
                VisibilityChanged?.Invoke(this, false);
                _logger?.LogDebug("主窗口已隐藏到托盘");
            }
        }

        /// <inheritdoc/>
        public void ShowNotification(string title, string message, int timeout = 3000)
        {
            if (_notifyIcon == null)
                return;

            try
            {
                _notifyIcon.ShowBalloonTip(timeout, title, message, ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "显示托盘通知失败");
            }
        }

        /// <inheritdoc/>
        public void AddMenuItem(string text, EventHandler onClick, int index = -1)
        {
            if (_contextMenu == null)
                return;

            var item = new ToolStripMenuItem(text);
            item.Click += onClick;

            if (index < 0 || index >= _contextMenu.Items.Count)
            {
                _contextMenu.Items.Insert(_contextMenu.Items.Count - 1, item);
            }
            else
            {
                _contextMenu.Items.Insert(index, item);
            }
        }

        /// <inheritdoc/>
        public void AddSeparator(int index = -1)
        {
            if (_contextMenu == null)
                return;

            var separator = new ToolStripSeparator();

            if (index < 0 || index >= _contextMenu.Items.Count)
            {
                _contextMenu.Items.Insert(_contextMenu.Items.Count - 1, separator);
            }
            else
            {
                _contextMenu.Items.Insert(index, separator);
            }
        }

        /// <inheritdoc/>
        public void RemoveMenuItem(string text)
        {
            if (_contextMenu == null)
                return;

            for (int i = 0; i < _contextMenu.Items.Count; i++)
            {
                if (_contextMenu.Items[i] is ToolStripMenuItem item && item.Text == text)
                {
                    _contextMenu.Items.RemoveAt(i);
                    break;
                }
            }
        }

        /// <inheritdoc/>
        public void SetIcon(Icon icon)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Icon = icon;
            }
        }

        /// <inheritdoc/>
        public void SetTooltip(string text)
        {
            if (_notifyIcon != null)
            {
                if (text.Length > 63)
                    text = text.Substring(0, 63);
                _notifyIcon.Text = text;
            }
        }

        /// <inheritdoc/>
        public ContextMenuStrip? GetContextMenu()
        {
            return _contextMenu;
        }

        /// <inheritdoc/>
        public void UpdateMenuItem(string oldText, string newText, bool enabled = true)
        {
            if (_contextMenu == null)
                return;

            for (int i = 0; i < _contextMenu.Items.Count; i++)
            {
                if (_contextMenu.Items[i] is ToolStripMenuItem item && item.Text == oldText)
                {
                    item.Text = newText;
                    item.Enabled = enabled;
                    break;
                }
            }
        }

        /// <inheritdoc/>
        public void Cleanup()
        {
            try
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                }

                if (_contextMenu != null)
                {
                    _contextMenu.Dispose();
                    _contextMenu = null;
                }

                if (_mainWindow != null)
                {
                    _mainWindow.Resize -= OnMainWindowResize;
                    _mainWindow.FormClosing -= OnMainWindowFormClosing;
                }

                _isInitialized = false;
                _logger?.LogInformation("系统托盘已清理");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "清理系统托盘失败");
            }
        }

        #region 私有方法 - 事件处理

        private void OnNotifyIconDoubleClick(object? sender, EventArgs e)
        {
            TrayDoubleClick?.Invoke(this, e);

            if (_mainWindow != null)
            {
                if (_mainWindow.Visible)
                {
                    HideToTray();
                }
                else
                {
                    ShowMainWindow();
                }
            }
        }

        private void OnNotifyIconMouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
            }
        }

        private void OnMainWindowResize(object? sender, EventArgs e)
        {
            if (_mainWindow == null)
                return;

            if (_mainWindow.WindowState == FormWindowState.Minimized && _minimizeToTray && _showTrayIcon)
            {
                HideToTray();
            }
        }

        private void OnMainWindowFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && _closeToTray && _showTrayIcon)
            {
                e.Cancel = true;
                HideToTray();
                ShowNotification("程序已最小化到托盘", "程序正在后台运行，点击托盘图标可重新打开");
            }
        }

        private void OnShowMainWindowClick(object? sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void OnHideToTrayClick(object? sender, EventArgs e)
        {
            HideToTray();
        }

        private void OnExitClick(object? sender, EventArgs e)
        {
            try
            {
                Cleanup();
                Application.Exit();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "退出应用失败");
            }
        }

        #endregion
    }
}
