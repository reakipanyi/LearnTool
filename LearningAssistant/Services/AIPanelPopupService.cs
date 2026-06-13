using LearningAssistant.Views.UI;
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
        void ShowAIAbilityPanel(Form parent, string? prompt = null, string? aiUrl = null);

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
        private readonly Dictionary<Form, Panel> _panelContainers = new();

        public void ShowAIAbilityPanel(Form parent, string? prompt = null, string? aiUrl = null)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            // 检查是否已有面板
            if (_panelContainers.TryGetValue(parent, out var existingContainer))
            {
                existingContainer.Visible = true;
                existingContainer.BringToFront();

                // 更新提示词和URL
                var aiPanel = existingContainer.Controls.OfType<AIAbilityPanel>().FirstOrDefault();
                if (aiPanel != null)
                {
                    if (!string.IsNullOrEmpty(prompt))
                        aiPanel.PromptText = prompt;

                    var urlToUse = !string.IsNullOrEmpty(aiUrl) ? aiUrl : aiPanel.CurrentAIUrl;
                    aiPanel.OpenWebView(urlToUse, prompt);
                }
                return;
            }

            // 创建AI面板
            var aiAbilityPanel = new AIAbilityPanel
            {
                Dock = DockStyle.Fill
            };

            // 设置初始值
            if (!string.IsNullOrEmpty(prompt))
                aiAbilityPanel.PromptText = prompt;

            // 创建容器面板
            var containerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Name = "AIAbilityContainer",
                BackColor = Color.White
            };
            containerPanel.Controls.Add(aiAbilityPanel);

            // 创建关闭按钮
            var closeButton = new Button
            {
                Text = "✕ 关闭",
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(240, 240, 240),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, args) => HideAIAbilityPanel(parent);
            containerPanel.Controls.Add(closeButton);

            // 添加到父窗体
            parent.Controls.Add(containerPanel);
            containerPanel.BringToFront();

            // 记录容器
            _panelContainers[parent] = containerPanel;

            // 打开WebView
            var finalUrl = !string.IsNullOrEmpty(aiUrl) ? aiUrl : aiAbilityPanel.CurrentAIUrl;
            aiAbilityPanel.OpenWebView(finalUrl, prompt);

            // 监听父窗体关闭
            parent.FormClosed += ParentFormClosedHandler;
        }

        private void ParentFormClosedHandler(object? sender, FormClosedEventArgs e)
        {
            if (sender is Form parent)
            {
                if (_panelContainers.TryGetValue(parent, out var container))
                {
                    try
                    {
                        container.Dispose();
                    }
                    catch (Exception ex)
                    {
                        // 忽略 dispose 过程中的异常，确保后续清理继续执行
                    }
                    _panelContainers.Remove(parent);
                    parent.FormClosed -= ParentFormClosedHandler;
                }
            }
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

            if (_panelContainers.TryGetValue(parent, out var container))
            {
                try
                {
                    container.Dispose();
                }
                catch (Exception ex)
                {
                    // 忽略 dispose 过程中的异常
                }
                _panelContainers.Remove(parent);
                parent.FormClosed -= ParentFormClosedHandler;
            }
        }
    }
}