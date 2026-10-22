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
                var pageText = card.PageNumber > 0 ? $"（第 {card.PageNumber} 页）" : string.Empty;
                listCards.Items.Add($"{card.Front}　→　{card.Back}{pageText}", true);
            }
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
