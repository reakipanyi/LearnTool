using UnifiedLearningAssistant.Models.Pdf;

namespace UnifiedLearningAssistant.Forms
{
    public class HighlightUndoAction
    {
        public HighlightActionType ActionType { get; set; }
        public PdfHighlight Highlight { get; set; }
    }
}
