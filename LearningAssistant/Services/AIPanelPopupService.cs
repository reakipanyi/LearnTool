using LearningAssistant.Forms.UserControls;
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

        private readonly Dictionary<Form, Panel> _panelContainers = new();

        #endregion

        #region 公共方法

        public void ShowAIAbilityPanel(Form parent, string? prompt = null, string? aiUrl = null, string? context = null)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            if (_panelContainers.TryGetValue(parent, out var existingContainer))
            {
                ShowExistingPanel(existingContainer, prompt, aiUrl, context);
                return;
            }

            CreateAndShowNewPanel(parent, prompt, aiUrl, context);
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

        private void ShowExistingPanel(Panel container, string? prompt, string? aiUrl, string? context)
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
                aiPanel.OpenWebView(urlToUse, prompt);
            }
        }

        private void CreateAndShowNewPanel(Form parent, string? prompt, string? aiUrl, string? context)
        {
            var aiAbilityPanel = CreateAIAbilityPanel(prompt, context);
            var containerPanel = CreateContainerPanel(aiAbilityPanel, parent);

            parent.Controls.Add(containerPanel);
            containerPanel.BringToFront();
            _panelContainers[parent] = containerPanel;

            var finalUrl = !string.IsNullOrEmpty(aiUrl) ? aiUrl : aiAbilityPanel.CurrentAIUrl;
            aiAbilityPanel.OpenWebView(finalUrl, prompt);

            parent.FormClosed += ParentFormClosedHandler;
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

            return containerPanel;
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
            if (_panelContainers.TryGetValue(parent, out var container))
            {
                try
                {
                    container.Dispose();
                }
                catch
                {
                    // 忽略 dispose 过程中的异常，确保后续清理继续执行
                }
                _panelContainers.Remove(parent);
                parent.FormClosed -= ParentFormClosedHandler;
            }
        }

        #endregion
    }
}