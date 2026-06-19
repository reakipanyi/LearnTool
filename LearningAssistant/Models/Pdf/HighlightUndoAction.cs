namespace LearningAssistant.Models.Pdf
{
    public class HighlightUndoAction
    {
        public HighlightActionType ActionType { get; set; }
        public PdfHighlight Highlight { get; set; }
    }
}
