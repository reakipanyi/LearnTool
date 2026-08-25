using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Forms.UserControls;
using System.Collections.Concurrent;
using System.Windows.Forms;

namespace LearningAssistant.Services
{
    /// <summary>
    /// AI面板弹窗服务 - 统一管理AIAbilityPanel的弹出逻辑
    /// </summary>
    public interface IAIPanelPopupService
    {
        /// <summary>
        /// 在指定父窗体上显示AI面板
        /// </summary>
        /// <param name="parent">父窗体</param>
        /// <param name="prompt">初始提示词</param>
        /// <param name="aiUrl">AI URL（可选）</param>
        /// <param name="context">上下文文本（可选，如PDF选中的内容）</param>
        void ShowAIAbilityPanel(Form parent, string? prompt = null, string? aiUrl = null, string? context = null);

        /// <summary>
        /// 关闭指定父窗体上的AI面板
        /// </summary>
        void HideAIAbilityPanel(Form parent);

        /// <summary>
        /// 清理并释放指定父窗体上的AI面板资源
        /// </summary>
        void CleanupPanel(Form parent);
    }

    public class AIPanelPopupService : IAIPanelPopupService
    {
        #region 字段

        private readonly ConcurrentDictionary<Form, Panel> _panelContainers = new();
        private readonly ConcurrentDictionary<Form, Button> _closeButtons = new();
        private readonly Color _closeButtonNormalColor = Color.FromArgb(240, 240, 240);
        private readonly Color _closeButtonHoverColor = Color.FromArgb(220, 220, 220);
        private readonly IDialogService? _dialogService;

        public AIPanelPopupService(IDialogService? dialogService = null)
        {
            _dialogService = dialogService;
        }

        #endregion

        #region 公共方法

        public void ShowAIAbilityPanel(Form parent, string? prompt = null, string? aiUrl = null, string? context = null)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            if (_panelContainers.TryGetValue(parent, out var existingContainer))
            {
                // 内部实现为 Task，自行捕获异常并提示，避免 async void 吞掉异常
                _ = OpenExistingPanelCoreAsync(parent, existingContainer, prompt, aiUrl, context);
                return;
            }

            _ = OpenNewPanelCoreAsync(parent, prompt, aiUrl, context);
        }

        public void HideAIAbilityPanel(Form parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            if (_panelContainers.TryGetValue(parent, out var container))
            {
                container.Visible = false;
            }
        }

        public void CleanupPanel(Form parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            DisposePanel(parent);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 打开/复用父窗体上已存在的 AI 面板。
        /// </summary>
        private async Task OpenExistingPanelCoreAsync(Form parent, Panel container, string? prompt, string? aiUrl, string? context)
        {
            try
            {
                container.Visible = true;
                container.BringToFront();

                var aiPanel = container.Controls.OfType<AIAbilityPanel>().FirstOrDefault();
                if (aiPanel != null)
                {
                    if (!string.IsNullOrEmpty(context))
                        aiPanel.ContextText = context;

                    if (!string.IsNullOrEmpty(prompt))
                        aiPanel.PromptText = prompt;

                    var urlToUse = !string.IsNullOrEmpty(aiUrl) ? aiUrl : aiPanel.CurrentAIUrl;
                    await aiPanel.OpenWebViewAsync(urlToUse, prompt);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AI面板WebView加载失败: {ex.Message}");
                ShowPanelError(parent, "AI 面板加载失败", ex);
            }
        }

        /// <summary>
        /// 创建并打开新的 AI 面板。
        /// </summary>
        private async Task OpenNewPanelCoreAsync(Form parent, string? prompt, string? aiUrl, string? context)
        {
            try
            {
                var aiAbilityPanel = CreateAIAbilityPanel(prompt, context);
                var containerPanel = CreateContainerPanel(aiAbilityPanel, parent);

                parent.Controls.Add(containerPanel);
                containerPanel.BringToFront();
                _panelContainers[parent] = containerPanel;

                var finalUrl = !string.IsNullOrEmpty(aiUrl) ? aiUrl : aiAbilityPanel.CurrentAIUrl;
                await aiAbilityPanel.OpenWebViewAsync(finalUrl, prompt);

                parent.FormClosed += ParentFormClosedHandler;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AI面板WebView加载失败: {ex.Message}");
                // 创建失败时清理已添加的容器，避免残留控件与事件泄漏
                DisposePanel(parent);
                ShowPanelError(parent, "AI 面板打开失败", ex);
            }
        }

        private AIAbilityPanel CreateAIAbilityPanel(string? prompt, string? context)
        {
            var panel = new AIAbilityPanel
            {
                Dock = DockStyle.Fill
            };

            if (!string.IsNullOrEmpty(context))
                panel.ContextText = context;

            if (!string.IsNullOrEmpty(prompt))
                panel.PromptText = prompt;

            return panel;
        }

        private Panel CreateContainerPanel(AIAbilityPanel aiPanel, Form parent)
        {
            var containerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Name = "AIAbilityContainer",
                BackColor = Color.White
            };

            containerPanel.Controls.Add(aiPanel);

            var closeButton = new Button
            {
                Text = "✕ 关闭面板",
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = _closeButtonNormalColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 15, 0)
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.MouseEnter += (s, args) => closeButton.BackColor = _closeButtonHoverColor;
            closeButton.MouseLeave += (s, args) => closeButton.BackColor = _closeButtonNormalColor;
            closeButton.Click += (s, args) => HideAIAbilityPanel(parent);
            containerPanel.Controls.Add(closeButton);

            _closeButtons[parent] = closeButton;

            return containerPanel;
        }

        /// <summary>
        /// 向用户展示 AI 面板错误提示（提示失败时静默，不影响主流程）。
        /// </summary>
        private void ShowPanelError(Form parent, string title, Exception ex)
        {
            try
            {
                if (parent != null && !parent.IsDisposed)
                {
                    if (_dialogService != null)
                        _dialogService.ShowMessageAsync("AI 面板", $"{title}：{ex.Message}").GetAwaiter().GetResult();
                    else
                        MessageBox.Show($"{title}：{ex.Message}", "AI 面板", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch
            {
                // 错误提示自身失败时不再抛出，避免影响主流程
            }
        }

        private void ParentFormClosedHandler(object? sender, FormClosedEventArgs e)
        {
            if (sender is Form parent)
            {
                DisposePanel(parent);
            }
        }

        private void DisposePanel(Form parent)
        {
            if (_panelContainers.TryRemove(parent, out var container))
            {
                try
                {
                    container.Dispose();
                }
                catch
                {
                    // 忽略 dispose 过程中的异常，确保后续清理继续执行
                }
                _closeButtons.TryRemove(parent, out _);
                parent.FormClosed -= ParentFormClosedHandler;
            }
        }

        #endregion
    }
}
