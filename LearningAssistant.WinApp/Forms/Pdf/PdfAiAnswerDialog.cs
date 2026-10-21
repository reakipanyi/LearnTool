using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Forms.Pdf
{
    /// <summary>
    /// PDF 站内 AI 问答结果窗口：展示答案，并将 [[pN]] 引用页码渲染为可点击跳转的页码按钮。
    /// </summary>
    public sealed class PdfAiAnswerDialog : Form
    {
        private readonly PdfAiResponse _response;
        private readonly Action<int>? _navigateToPage;
        private readonly Func<PdfAiResponse, Task>? _saveAsAnnotation;
        private readonly Action<PdfAiResponse>? _addToEditor;
        private readonly RichTextBox _textAnswer;
        private readonly FlowLayoutPanel _flowPages;

        public PdfAiAnswerDialog(string title, PdfAiResponse response, Action<int>? navigateToPage = null,
            Func<PdfAiResponse, Task>? saveAsAnnotation = null, Action<PdfAiResponse>? addToEditor = null)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
            _navigateToPage = navigateToPage;
            _saveAsAnnotation = saveAsAnnotation;
            _addToEditor = addToEditor;

            Text = title;
            Size = new Size(680, 560);
            MinimumSize = new Size(520, 420);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("微软雅黑", 9F);
            KeyPreview = true;

            var panelBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                BackColor = Color.FromArgb(248, 248, 248),
                Padding = new Padding(12, 10, 12, 10)
            };

            var labelPages = new Label
            {
                Text = "引用页码：",
                AutoSize = true,
                Location = new Point(12, 18),
                ForeColor = Color.FromArgb(90, 90, 90)
            };

            _flowPages = new FlowLayoutPanel
            {
                Location = new Point(86, 12),
                Size = new Size(440, 36),
                WrapContents = false,
                AutoScroll = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };

            var buttonClose = new Button
            {
                Text = "关闭",
                Size = new Size(84, 32),
                Location = new Point(0, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(139, 92, 246),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            buttonClose.FlatAppearance.BorderSize = 0;
            buttonClose.Click += (_, _) => Close();

            var actionButtons = new List<Button>();

            if (_addToEditor != null && _response.Success)
            {
                var editorButton = CreateSecondaryButton("添加到编辑器");
                editorButton.Click += (_, _) =>
                {
                    _addToEditor(_response);
                    editorButton.Text = "已添加";
                };
                actionButtons.Add(editorButton);
            }

            if (_saveAsAnnotation != null && _response.Success)
            {
                var saveButton = CreateSecondaryButton("保存为标注");
                saveButton.Click += async (_, _) => await SaveAsAnnotationAsync(saveButton);
                actionButtons.Add(saveButton);
            }

            // 右侧依次排布：动作按钮 → 关闭；页码区宽度随按钮区收缩
            void LayoutBottom()
            {
                int right = panelBottom.ClientSize.Width - 12;
                buttonClose.Left = right - buttonClose.Width;

                int left = buttonClose.Left;
                for (int i = actionButtons.Count - 1; i >= 0; i--)
                {
                    left -= actionButtons[i].Width + 8;
                    actionButtons[i].Left = left;
                }

                _flowPages.Width = Math.Max(120, left - _flowPages.Left - 8);
            }

            panelBottom.Resize += (_, _) => LayoutBottom();

            panelBottom.Controls.Add(labelPages);
            panelBottom.Controls.Add(_flowPages);
            foreach (var button in actionButtons) panelBottom.Controls.Add(button);
            panelBottom.Controls.Add(buttonClose);

            var labelTitle = new Label
            {
                Text = _response.Success ? title : "AI 问答失败",
                Dock = DockStyle.Top,
                Height = 46,
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                ForeColor = _response.Success ? Color.FromArgb(60, 60, 60) : Color.FromArgb(200, 60, 60),
                Padding = new Padding(14, 12, 14, 6)
            };

            _textAnswer = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("微软雅黑", 10F),
                BackColor = Color.White,
                ForeColor = _response.Success ? Color.FromArgb(40, 40, 40) : Color.FromArgb(200, 60, 60),
                ScrollBars = RichTextBoxScrollBars.Vertical,
                DetectUrls = false,
                Text = _response.Success
                    ? _response.Content
                    : (_response.ErrorMessage ?? "未知错误")
            };

            var panelAnswer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 4, 14, 10)
            };
            panelAnswer.Controls.Add(_textAnswer);

            Controls.Add(panelAnswer);
            Controls.Add(panelBottom);
            Controls.Add(labelTitle);

            BuildPageLinks();
            LayoutBottom();

            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape) Close();
            };
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
            _flowPages.Controls.Clear();
            if (_response.ReferencedPages.Count == 0)
            {
                _flowPages.Controls.Add(new Label
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
                _flowPages.Controls.Add(link);
            }
        }
    }
}
