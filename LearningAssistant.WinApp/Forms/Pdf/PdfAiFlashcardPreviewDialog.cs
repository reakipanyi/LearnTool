namespace LearningAssistant.Forms.Pdf
{
    /// <summary>
    /// AI 闪卡预览：勾选要加入闪卡复习队列的卡片。控件均在设计器中创建，便于可视化预览。
    /// </summary>
    public sealed partial class PdfAiFlashcardPreviewDialog : Form
    {
        private readonly IReadOnlyList<PdfAiFlashcardCard> _cards;

        /// <summary>用户勾选的卡片</summary>
        public List<PdfAiFlashcardCard> SelectedCards { get; } = new();

        /// <summary>供 Visual Studio 设计器实例化使用。</summary>
        public PdfAiFlashcardPreviewDialog() : this(Array.Empty<PdfAiFlashcardCard>(), string.Empty)
        {
        }

        public PdfAiFlashcardPreviewDialog(IReadOnlyList<PdfAiFlashcardCard> cards, string documentName)
        {
            _cards = cards ?? Array.Empty<PdfAiFlashcardCard>();

            InitializeComponent();

            labelTitle.Text = $"AI 闪卡预览（{_cards.Count} 张）";
            labelDoc.Text = documentName;

            foreach (var card in _cards)
            {
                listCards.Items.Add(FormatCard(card), true);
            }

            // 双击进入编辑：导入前修正正面/背面/页码，避免把 AI 的啰嗦表述带进复习队列
            listCards.DoubleClick += (_, _) => EditSelectedCard();
        }

        private static string FormatCard(PdfAiFlashcardCard card)
        {
            var pageText = card.PageNumber > 0 ? $"（第 {card.PageNumber} 页）" : string.Empty;
            return $"{card.Front}　→　{card.Back}{pageText}";
        }

        /// <summary>编辑当前选中卡片的正面、背面与来源页码，确认后刷新列表行</summary>
        private void EditSelectedCard()
        {
            int index = listCards.SelectedIndex;
            if (index < 0 || index >= _cards.Count) return;

            var card = _cards[index];
            using var dialog = new Form
            {
                Text = "编辑闪卡",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(440, 300),
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                Font = Font
            };

            var frontLabel = new Label { Text = "正面", AutoSize = true, Location = new Point(14, 12) };
            var frontBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(14, 32),
                Size = new Size(412, 90),
                Text = card.Front
            };
            var backLabel = new Label { Text = "背面", AutoSize = true, Location = new Point(14, 130) };
            var backBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(14, 150),
                Size = new Size(412, 90),
                Text = card.Back
            };
            var pageLabel = new Label { Text = "来源页", AutoSize = true, Location = new Point(14, 252) };
            var pageBox = new NumericUpDown
            {
                Location = new Point(70, 248),
                Width = 80,
                Minimum = 0,
                Maximum = 9999,
                Value = Math.Clamp(card.PageNumber, 0, 9999)
            };
            var okButton = new Button { Text = "确定", Size = new Size(84, 30), Location = new Point(256, 248) };
            var cancelButton = new Button
            {
                Text = "取消", Size = new Size(84, 30), Location = new Point(344, 248),
                DialogResult = DialogResult.Cancel
            };
            okButton.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(frontBox.Text) || string.IsNullOrWhiteSpace(backBox.Text))
                {
                    MessageBox.Show(dialog, "正面和背面都不能为空。", "编辑闪卡",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                dialog.DialogResult = DialogResult.OK;
            };

            dialog.Controls.AddRange(new Control[]
            {
                frontLabel, frontBox, backLabel, backBox, pageLabel, pageBox, okButton, cancelButton
            });
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            card.Front = frontBox.Text.Trim();
            card.Back = backBox.Text.Trim();
            card.PageNumber = (int)pageBox.Value;

            bool wasChecked = listCards.GetItemChecked(index);
            listCards.Items[index] = FormatCard(card);
            listCards.SetItemChecked(index, wasChecked);
        }

        private void ButtonSelectAll_Click(object? sender, EventArgs e) => SetAllChecked(true);

        private void ButtonSelectNone_Click(object? sender, EventArgs e) => SetAllChecked(false);

        private void ButtonCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void ButtonOk_Click(object? sender, EventArgs e) => ConfirmSelection();

        private void SetAllChecked(bool isChecked)
        {
            for (int i = 0; i < listCards.Items.Count; i++)
            {
                listCards.SetItemChecked(i, isChecked);
            }
        }

        private void ConfirmSelection()
        {
            SelectedCards.Clear();
            foreach (int index in listCards.CheckedIndices)
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
