using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Models
{
    public class HighlightUndoAction
    {
        public HighlightActionType ActionType { get; set; }
        public PdfHighlight Highlight { get; set; }
    }
}
