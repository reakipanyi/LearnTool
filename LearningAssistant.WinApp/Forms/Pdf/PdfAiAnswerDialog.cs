using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Forms.Pdf
{
    /// <summary>
    /// PDF 站内 AI 问答结果窗口：展示答案，并将 [[pN]] 引用页码渲染为可点击跳转的页码按钮。
    /// 固定控件在设计器中创建，动态按钮与页码链接在运行时构建。
    /// </summary>
    public sealed partial class PdfAiAnswerDialog : Form
    {
        private readonly PdfAiResponse _response;
        private readonly Action<int>? _navigateToPage;
        private readonly Func<PdfAiResponse, Task>? _saveAsAnnotation;
        private readonly Action<PdfAiResponse>? _addToEditor;
        private readonly List<Button> _actionButtons = new();

        /// <summary>供 Visual Studio 设计器实例化使用。</summary>
        public PdfAiAnswerDialog() : this("AI 问答", new PdfAiResponse { Success = true })
        {
        }

        public PdfAiAnswerDialog(string title, PdfAiResponse response, Action<int>? navigateToPage = null,
            Func<PdfAiResponse, Task>? saveAsAnnotation = null, Action<PdfAiResponse>? addToEditor = null)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
            _navigateToPage = navigateToPage;
            _saveAsAnnotation = saveAsAnnotation;
            _addToEditor = addToEditor;

            InitializeComponent();

            Text = title;
            labelTitle.Text = _response.Success ? title : "AI 问答失败";
            labelTitle.ForeColor = _response.Success ? Color.FromArgb(60, 60, 60) : Color.FromArgb(200, 60, 60);

            textAnswer.ForeColor = _response.Success ? Color.FromArgb(40, 40, 40) : Color.FromArgb(200, 60, 60);
            textAnswer.Text = _response.Success
                ? HumanizeAnchors(_response.Content)
                : (_response.ErrorMessage ?? "未知错误");

            UpdateMetaLine();

            if (_addToEditor != null && _response.Success)
            {
                var editorButton = CreateSecondaryButton("添加到编辑器");
                editorButton.Click += (_, _) =>
                {
                    _addToEditor(_response);
                    editorButton.Text = "已添加";
                };
                _actionButtons.Add(editorButton);
                panelBottom.Controls.Add(editorButton);
            }

            if (_saveAsAnnotation != null && _response.Success)
            {
                var saveButton = CreateSecondaryButton("保存为标注");
                saveButton.Click += async (_, _) => await SaveAsAnnotationAsync(saveButton);
                _actionButtons.Add(saveButton);
                panelBottom.Controls.Add(saveButton);
            }

            panelBottom.Resize += (_, _) => LayoutBottom();

            BuildPageLinks();
            LayoutBottom();
        }

        private void ButtonClose_Click(object? sender, EventArgs e) => Close();

        /// <summary>
        /// 结果元信息：模型、本次覆盖页数与未覆盖提示，让用户知道答案基于多少内容生成。
        /// </summary>
        private void UpdateMetaLine()
        {
            if (!_response.Success)
            {
                labelMeta.Text = string.Empty;
                return;
            }

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(_response.ModelId))
                parts.Add($"模型 {_response.ModelId}");
            if (_response.TotalPages > 0)
                parts.Add($"覆盖 {_response.CoveredPages}/{_response.TotalPages} 页");

            if (!string.IsNullOrWhiteSpace(_response.CoverageWarning))
            {
                parts.Add(_response.CoverageWarning);
                labelMeta.ForeColor = Color.FromArgb(180, 95, 0);
            }

            if (!string.IsNullOrWhiteSpace(_response.CitationWarning))
            {
                parts.Add(_response.CitationWarning);
                labelMeta.ForeColor = Color.FromArgb(180, 95, 0);
            }

            labelMeta.Text = string.Join(" · ", parts);
        }

        private void PdfAiAnswerDialog_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Close();
        }

        /// <summary>右侧依次排布：动作按钮 → 关闭；页码区宽度随按钮区收缩</summary>
        private void LayoutBottom()
        {
            int right = panelBottom.ClientSize.Width - 12;
            buttonClose.Left = right - buttonClose.Width;

            int left = buttonClose.Left;
            for (int i = _actionButtons.Count - 1; i >= 0; i--)
            {
                left -= _actionButtons[i].Width + 8;
                _actionButtons[i].Left = left;
            }

            flowPages.Width = Math.Max(120, left - flowPages.Left - 8);
        }

        /// <summary>把 [[pN]] 锚点转成可读的“（第 N 页）”，点击跳转仍用底部页码链接</summary>
        private static string HumanizeAnchors(string? content)
        {
            if (string.IsNullOrEmpty(content)) return string.Empty;
            return System.Text.RegularExpressions.Regex.Replace(content, @"\[\[p(\d+)\]\]", "（第 $1 页）");
        }

        private static Button CreateSecondaryButton(string text)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(104, 32),
                Location = new Point(0, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(243, 240, 255),
                ForeColor = Color.FromArgb(109, 40, 217),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(196, 181, 253);
            return button;
        }

        private async Task SaveAsAnnotationAsync(Button source)
        {
            if (_saveAsAnnotation == null) return;

            try
            {
                source.Enabled = false;
                await _saveAsAnnotation(_response);
                source.Text = "已保存";
            }
            catch (Exception ex)
            {
                source.Enabled = true;
                MessageBox.Show(this, $"保存失败：{ex.Message}", "保存为标注",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BuildPageLinks()
        {
            flowPages.Controls.Clear();
            if (_response.ReferencedPages.Count == 0)
            {
                flowPages.Controls.Add(new Label
                {
                    Text = "无",
                    AutoSize = true,
                    ForeColor = Color.Gray,
                    Margin = new Padding(3, 8, 3, 3)
                });
                return;
            }

            foreach (int pageNumber in _response.ReferencedPages)
            {
                var link = new LinkLabel
                {
                    Text = $"第 {pageNumber} 页",
                    AutoSize = true,
                    LinkColor = Color.FromArgb(79, 70, 229),
                    ActiveLinkColor = Color.FromArgb(139, 92, 246),
                    VisitedLinkColor = Color.FromArgb(79, 70, 229),
                    Font = new Font("微软雅黑", 9F, FontStyle.Underline),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(3, 8, 10, 3)
                };
                int targetPage = pageNumber;
                link.LinkClicked += (_, _) =>
                {
                    _navigateToPage?.Invoke(targetPage);
                    Close();
                };
                flowPages.Controls.Add(link);
            }
        }
    }
}
