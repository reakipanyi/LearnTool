 
        private void CloseButton_Click(object? sender, EventArgs e)
        {
            HideFeynmanPanel();
        }

        /// <summary>
        /// 隐藏费曼学习面板
        /// </summary>
        private void HideFeynmanPanel()
        {
            if (_feynmanContainerPanel != null)
            {
                _feynmanContainerPanel.Visible = false;
                _isFeynmanPanelVisible = false;
            }
        }

        private void CreateFeynmanPanel()
        {
            _feynmanPanel = new FeynmanLearningPanel
            {
                Dock = DockStyle.Fill
            };
            _feynmanPanel.CloseClicked += FeynmanPanel_CloseClicked;
            _feynmanPanel.Completed += FeynmanPanel_Completed;
            _feynmanPanel.AIFeedbackRequested += FeynmanPanel_AIFeedbackRequested;
            _feynmanPanel.GenerateSimplifiedRequested += FeynmanPanel_GenerateSimplifiedRequested;
            _feynmanPanel.GenerateAnalogyRequested += FeynmanPanel_GenerateAnalogyRequested;
            _feynmanPanel.VoiceInputRequested += FeynmanPanel_VoiceInputRequested;

            _feynmanContainerPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 420,
                Name = "FeynmanPanelContainer",
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(0, 0, 0, 0)
            };

            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.FromArgb(147, 112, 219)
            };

            var titleLabel = new Label
            {
                Text = "🧠 费曼学习法",
                Dock = DockStyle.Left,
                ForeColor = Color.White,
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                Padding = new Padding(20, 10, 0, 0),
                AutoSize = true
            };

            var closeButton = new Button
            {
                Text = "✕",
                Dock = DockStyle.Right,
                Width = 45,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("微软雅黑", 14F, FontStyle.Bold),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(127, 92, 199);
            closeButton.Click += CloseButton_Click;

            var gradientPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 8,
                BackColor = Color.White
            };
            gradientPanel.Paint += (sender, e) =>
            {
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, gradientPanel.Width, gradientPanel.Height),
                    Color.FromArgb(147, 112, 219),
                    Color.FromArgb(76, 175, 80),
                    System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(brush, e.ClipRectangle);
                }
            };

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(closeButton);

            _feynmanContainerPanel.Controls.Add(_feynmanPanel);
            _feynmanContainerPanel.Controls.Add(gradientPanel);
            _feynmanContainerPanel.Controls.Add(headerPanel);

            Controls.Add(_feynmanContainerPanel);
        }

        private void FeynmanPanel_CloseClicked(object? sender, EventArgs e)
        {
            HideFeynmanPanel();
        }

        private void FeynmanPanel_Completed(object? sender, EventArgs e)
        {
            _soundService?.PlaySuccess();

            if (_currentItem != null)
            {
                var record = new Models.Learning.FeynmanHistoryRecord
                {
                    ContentId = _currentItem.GetDisplayText(),
                    ContentTitle = _currentItem.GetDisplayText(),
                    TeachAnswer = _feynmanPanel?.TeachAnswer ?? string.Empty,
                    AIFeedback = _feynmanPanel?.AIFeedbackText,
                    SimplifiedText = _feynmanPanel?.SimplifiedText,
                    AnalogyText = _feynmanPanel?.AnalogyText,
                    IsCompleted = true
                };
                _feynmanHistoryService.SaveRecord(record);

                if (_eventBus != null)
                {
                    _eventBus.Publish(eventData: new FeynmanCompletedEvent
                    {
                        UserId = GetCurrentUserId(),
                        ItemContent = _currentItem.GetDisplayText(),
                        SubCategory = _settings.SubCategory,
                        SimplifiedText = record.SimplifiedText ?? string.Empty
                    });
                }
            }

            _gamificationService.Save();
            MessageBox.Show("🎉 恭喜完成费曼学习法四步流程！\n\n获得 50 XP 和 100 分！\n你的理解会更加深刻！", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            HideFeynmanPanel();
        }

        private async void FeynmanPanel_AIFeedbackRequested(object? sender, string userExplanation)
        {
            if (_currentItem == null || _aiQuestionService == null || _feynmanPanel == null)
                return;

            try
            {
                _feynmanPanel.SetAIFeedbackLoading(true);

                var content = _currentItem.GetMainContent();
                var displayText = _currentItem.GetDisplayText();

                var prompt = $"请评估以下用户对知识点的解释是否准确，并给出改进建议。\n\n" +
                             $"知识点：{displayText}\n" +
                             $"参考内容：{content}\n\n" +
                             $"用户的解释：{userExplanation}\n\n" +
                             $"请从以下几个方面评估：\n" +
                             $"1. 准确性：解释是否正确\n" +
                             $"2. 清晰度：是否容易理解\n" +
                             $"3. 完整性：是否涵盖了关键点\n" +
                             $"4. 改进建议：如何更好地解释";

                var feedback = await _aiQuestionService.AskAsync(prompt, content);
                _feynmanPanel.SetAIFeedback(feedback);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取AI反馈失败");
                _feynmanPanel?.SetAIFeedback($"❌ 获取AI反馈失败：{ex.Message}");
            }
        }

        private async void FeynmanPanel_GenerateSimplifiedRequested(object? sender, EventArgs e)
        {
            if (_currentItem == null || _aiQuestionService == null || _feynmanPanel == null)
                return;

            try
            {
                _feynmanPanel.SetSimplifiedLoading(true);

                var content = _currentItem.GetMainContent();
                var prompt = $"请用一句话（不超过30个字）总结以下知识点的核心内容：\n\n{content}";

                var result = await _aiQuestionService.AskAsync(prompt, content);
                result = result.Trim().Trim('"', '。', '.');
                _feynmanPanel.SetSimplifiedText(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成简化总结失败");
                _feynmanPanel?.SetSimplifiedText($"❌ 生成失败：{ex.Message}");
            }
        }

        private async void FeynmanPanel_GenerateAnalogyRequested(object? sender, EventArgs e)
        {
            if (_currentItem == null || _aiQuestionService == null || _feynmanPanel == null)
                return;

            try
            {
                _feynmanPanel.SetAnalogyLoading(true);

                var content = _currentItem.GetMainContent();
                var displayText = _currentItem.GetDisplayText();
                var prompt = $"请用一个生动形象的比喻/类比来解释\"{displayText}\"这个概念，让初学者也能轻松理解：\n\n参考内容：{content}";

                var result = await _aiQuestionService.AskAsync(prompt, content);
                result = result.Trim();
                _feynmanPanel.SetAnalogyText(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成类比失败");
                _feynmanPanel?.SetAnalogyText($"❌ 生成失败：{ex.Message}");
            }
        }

        private void FeynmanPanel_VoiceInputRequested(object? sender, EventArgs e)
        {
            try
            {
                if (_isDictationActive)
                {
                    _speechService?.StopDictation();
                    _isDictationActive = false;
                    return;
                }

                _speechService ??= Program.GetService<SpeechService>();
                if (_speechService == null)
                {
                    MessageBox.Show("语音服务未初始化", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _speechService.DictationCompleted -= OnDictationCompleted;
                _speechService.DictationCompleted += OnDictationCompleted;
                _speechService.DictationError -= OnDictationError;
                _speechService.DictationError += OnDictationError;

                _speechService.StartDictation();
                _isDictationActive = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动语音输入失败");
                MessageBox.Show($"启动语音输入失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _isDictationActive = false;
            }
        }

        private void OnDictationCompleted(object? sender, Services.Learning.DictationResultEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnDictationCompleted(sender, e)));
                return;
            }

            if (e.Success && !string.IsNullOrWhiteSpace(e.Text))
            {
                _feynmanPanel?.AppendVoiceText(e.Text);
            }
        }

        private void OnDictationError(object? sender, string e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnDictationError(sender, e)));
                return;
            }

            _logger.LogWarning("语音输入错误：{Error}", e);
        }


 
        private void InitializeAIHistoryPanel()
        {
            _aiHistoryPanel = new AIHistoryPanel
            {
                Dock = DockStyle.Right,
                Width = 300,
                Visible = false
            };
            _aiHistoryPanel.HistoryItemSelected += AIHistoryPanel_HistoryItemSelected;

            Controls.Add(_aiHistoryPanel);
        }

        private void AIHistoryPanel_HistoryItemSelected(object? sender, AIHistoryEventArgs e)
        {
            ShowToast($"问题: {e.Item.Question}", ToastType.Info);
        }

        private void InitializeNoteEnhancements()
        {
            _noteWordCountLabel = new Label
            {
                Text = "字数: 0",
                Font = new Font("微软雅黑", 8F),
                ForeColor = Color.Gray,
                Dock = DockStyle.Bottom,
                Padding = new Padding(5),
                TextAlign = ContentAlignment.MiddleRight
            };

            _noteFormattingToolbar = new ToolStrip
            {
                Dock = DockStyle.Top,
                Height = 25,
                BackColor = Color.White
            };

            var boldBtn = new ToolStripButton("B")
            {
                ToolTipText = "加粗",
                Font = new Font("微软雅黑", 9F, FontStyle.Bold)
            };
            boldBtn.Click += BoldBtn_Click;

            var italicBtn = new ToolStripButton("I")
            {
                ToolTipText = "斜体",
                Font = new Font("微软雅黑", 9F, FontStyle.Italic)
            };
            italicBtn.Click += ItalicBtn_Click;

            var underlineBtn = new ToolStripButton("U")
            {
                ToolTipText = "下划线",
                Font = new Font("微软雅黑", 9F, FontStyle.Underline)
            };
            underlineBtn.Click += UnderlineBtn_Click;

            _noteFormattingToolbar.Items.Add(boldBtn);
            _noteFormattingToolbar.Items.Add(italicBtn);
            _noteFormattingToolbar.Items.Add(underlineBtn);
            _noteFormattingToolbar.Items.Add(new ToolStripSeparator());

            var fontColorBtn = new ToolStripButton("A")
            {
                ToolTipText = "字体颜色"
            };
            fontColorBtn.Click += FontColorBtn_Click;
            _noteFormattingToolbar.Items.Add(fontColorBtn);

            if (panelNotes != null)
            {
                panelNotes.Controls.Add(_noteWordCountLabel);
                panelNotes.Controls.Add(_noteFormattingToolbar);
            }

            if (richTextBoxNotes != null)
            {
                richTextBoxNotes.TextChanged += RichTextBoxNotes_TextChangedEnhanced;
            }
        }

        private void BoldBtn_Click(object? sender, EventArgs e)
        {
            ApplyNoteFormat(FontStyle.Bold);
        }

        private void ItalicBtn_Click(object? sender, EventArgs e)
        {
            ApplyNoteFormat(FontStyle.Italic);
        }

        private void UnderlineBtn_Click(object? sender, EventArgs e)
        {
            ApplyNoteFormat(FontStyle.Underline);
        }

        private void FontColorBtn_Click(object? sender, EventArgs e)
        {
            ChangeNoteFontColor();
        }

        private void ApplyNoteFormat(FontStyle style)
        {
            if (richTextBoxNotes == null) return;

            var currentFont = richTextBoxNotes.SelectionFont;
            if (currentFont != null)
            {
                var newFont = new Font(currentFont, currentFont.Style ^ style);
                richTextBoxNotes.SelectionFont = newFont;
            }
        }

        private void ChangeNoteFontColor()
        {
            if (richTextBoxNotes == null) return;

            using (var colorDialog = new ColorDialog())
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    richTextBoxNotes.SelectionColor = colorDialog.Color;
                }
            }
        }

        private void RichTextBoxNotes_TextChangedEnhanced(object? sender, EventArgs e)
        {
            if (_noteWordCountLabel != null && richTextBoxNotes != null)
            {
                int wordCount = richTextBoxNotes.Text.Length;
                _noteWordCountLabel.Text = $"字数: {wordCount}";
            }
        }
		
		 
        #region === 费曼学习面板 ===
        private FeynmanLearningPanel? _feynmanPanel;
        private Panel? _feynmanContainerPanel;
        private bool _isFeynmanPanelVisible = false;
        private readonly FeynmanHistoryService _feynmanHistoryService = new();
        private SpeechService? _speechService;
        private bool _isDictationActive = false;
        private LevelBadge _levelBadge;
        #endregion