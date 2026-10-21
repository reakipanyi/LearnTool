namespace LearningAssistant.Forms.Pdf
{
    /// <summary>AI 闪卡生成的页码范围选项</summary>
    public sealed class PdfAiFlashcardOptions
    {
        /// <summary>起始页（0 基），-1 表示第一页</summary>
        public int StartPageIndex { get; init; } = -1;
        /// <summary>结束页（0 基），-1 表示最后一页</summary>
        public int EndPageIndex { get; init; } = -1;
    }

    /// <summary>单张 AI 闪卡</summary>
    public sealed class PdfAiFlashcardCard
    {
        /// <summary>正面问题或概念</summary>
        public string Front { get; init; } = string.Empty;
        /// <summary>背面答案或解释</summary>
        public string Back { get; init; } = string.Empty;
        /// <summary>来源页码（从 1 开始，0 表示未标注）</summary>
        public int PageNumber { get; init; }
    }

    /// <summary>
    /// AI 闪卡生成设置：选择页码范围。纯代码构建，避免设计器文件。
    /// </summary>
    public sealed class PdfAiFlashcardDialog : Form
    {
        private static readonly Color AccentColor = Color.FromArgb(139, 92, 246);
        private static readonly Color TextColor = Color.FromArgb(50, 50, 60);

        private readonly TextBox _textRange;

        public PdfAiFlashcardOptions Options { get; private set; } = new();

        public PdfAiFlashcardDialog(int pageCount)
        {
            Text = "AI 闪卡生成";
            Font = new Font("Microsoft YaHei UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 216);
            BackColor = Color.White;

            var accentBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 4,
                BackColor = AccentColor
            };

            var labelTitle = new Label
            {
                Text = "把文档内容转成问答闪卡",
                Location = new Point(20, 22),
                Size = new Size(420, 26),
                ForeColor = TextColor,
                Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold)
            };

            var labelRange = new Label
            {
                Text = $"页码范围（共 {pageCount} 页，留空表示全部页面）",
                Location = new Point(20, 58),
                Size = new Size(420, 22),
                ForeColor = TextColor
            };

            _textRange = new TextBox
            {
                Location = new Point(20, 82),
                Size = new Size(200, 27),
                PlaceholderText = "例如：1-5"
            };

            var labelRangeHint = new Label
            {
                Text = "支持单页或区间，超出页数时按实际页数截断",
                Location = new Point(230, 86),
                Size = new Size(210, 22),
                ForeColor = Color.FromArgb(150, 150, 160)
            };

            var labelHint = new Label
            {
                Text = "生成后可勾选要加入闪卡复习队列的卡片。",
                Location = new Point(20, 120),
                Size = new Size(420, 22),
                ForeColor = Color.FromArgb(120, 120, 132)
            };

            var buttonOk = new Button
            {
                Text = "生成闪卡",
                Location = new Point(236, 164),
                Size = new Size(104, 34),
                BackColor = AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            buttonOk.FlatAppearance.BorderSize = 0;
            buttonOk.Click += ButtonOk_Click;

            var buttonCancel = new Button
            {
                Text = "取消",
                Location = new Point(350, 164),
                Size = new Size(90, 34),
                BackColor = Color.White,
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            buttonCancel.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 225);
            buttonCancel.Click += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.AddRange(new Control[]
            {
                accentBar, labelTitle, labelRange, _textRange, labelRangeHint, labelHint, buttonOk, buttonCancel
            });

            AcceptButton = buttonOk;
            CancelButton = buttonCancel;
        }

        private void ButtonOk_Click(object? sender, EventArgs e)
        {
            if (!PdfAiNotesExportDialog.TryParsePageRange(_textRange.Text, out var startIndex, out var endIndex))
            {
                MessageBox.Show(this, "页码范围格式不正确，请填写如 1-5，或留空表示全部页面。", "AI 闪卡生成",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Options = new PdfAiFlashcardOptions
            {
                StartPageIndex = startIndex,
                EndPageIndex = endIndex
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }

    /// <summary>
    /// AI 闪卡预览：勾选要加入闪卡复习队列的卡片。
    /// </summary>
    public sealed class PdfAiFlashcardPreviewDialog : Form
    {
        private static readonly Color AccentColor = Color.FromArgb(139, 92, 246);
        private static readonly Color TextColor = Color.FromArgb(50, 50, 60);

        private readonly CheckedListBox _listCards;
        private readonly IReadOnlyList<PdfAiFlashcardCard> _cards;

        /// <summary>用户勾选的卡片</summary>
        public List<PdfAiFlashcardCard> SelectedCards { get; } = new();

        public PdfAiFlashcardPreviewDialog(IReadOnlyList<PdfAiFlashcardCard> cards, string documentName)
        {
            _cards = cards ?? Array.Empty<PdfAiFlashcardCard>();

            Text = "AI 闪卡预览";
            Font = new Font("Microsoft YaHei UI", 9F);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(640, 520);
            MinimumSize = new Size(520, 400);
            BackColor = Color.White;

            var accentBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 4,
                BackColor = AccentColor
            };

            var labelTitle = new Label
            {
                Text = $"AI 闪卡预览（{_cards.Count} 张）",
                Location = new Point(20, 16),
                Size = new Size(420, 26),
                ForeColor = TextColor,
                Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold)
            };

            var labelDoc = new Label
            {
                Text = documentName,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(380, 20),
                Size = new Size(240, 22),
                TextAlign = ContentAlignment.MiddleRight,
                AutoEllipsis = true,
                ForeColor = Color.FromArgb(140, 140, 152)
            };

            _listCards = new CheckedListBox
            {
                Location = new Point(20, 52),
                Size = new Size(600, 382),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                CheckOnClick = true,
                IntegralHeight = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            foreach (var card in _cards)
            {
                var pageText = card.PageNumber > 0 ? $"（第 {card.PageNumber} 页）" : string.Empty;
                _listCards.Items.Add($"{card.Front}　→　{card.Back}{pageText}", true);
            }

            var buttonSelectAll = new Button
            {
                Text = "全选",
                Location = new Point(20, 448),
                Size = new Size(80, 32),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            buttonSelectAll.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 225);
            buttonSelectAll.Click += (_, _) => SetAllChecked(true);

            var buttonSelectNone = new Button
            {
                Text = "全不选",
                Location = new Point(108, 448),
                Size = new Size(80, 32),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            buttonSelectNone.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 225);
            buttonSelectNone.Click += (_, _) => SetAllChecked(false);

            var buttonOk = new Button
            {
                Text = "加入闪卡复习",
                Location = new Point(408, 448),
                Size = new Size(126, 32),
                BackColor = AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            buttonOk.FlatAppearance.BorderSize = 0;
            buttonOk.Click += (_, _) => ConfirmSelection();

            var buttonCancel = new Button
            {
                Text = "取消",
                Location = new Point(540, 448),
                Size = new Size(80, 32),
                BackColor = Color.White,
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            buttonCancel.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 225);
            buttonCancel.Click += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.AddRange(new Control[]
            {
                accentBar, labelTitle, labelDoc, _listCards,
                buttonSelectAll, buttonSelectNone, buttonOk, buttonCancel
            });

            AcceptButton = buttonOk;
            CancelButton = buttonCancel;
        }

        private void SetAllChecked(bool isChecked)
        {
            for (int i = 0; i < _listCards.Items.Count; i++)
            {
                _listCards.SetItemChecked(i, isChecked);
            }
        }

        private void ConfirmSelection()
        {
            SelectedCards.Clear();
            foreach (int index in _listCards.CheckedIndices)
            {
                if (index >= 0 && index < _cards.Count)
                    SelectedCards.Add(_cards[index]);
            }

            if (SelectedCards.Count == 0)
            {
                MessageBox.Show(this, "请至少勾选一张闪卡。", "AI 闪卡预览",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
