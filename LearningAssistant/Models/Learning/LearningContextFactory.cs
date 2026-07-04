using LearningAssistant.Common;

namespace LearningAssistant.Models.Learning
{
    public static class LearningContextFactory
    {
        public static LearningContext FromUiSelection(string userId, SubjectType subject,
            SubCategoryType subCategory, string wordBankFile = "")
        {
            return new LearningContext(
                UserId: userId,
                Subject: subject,
                SubCategory: subCategory,
                WordBankFile: wordBankFile
            );
        }

        public static LearningContext WithWordBankFile(this LearningContext context, string wordBankFile)
        {
            return context with { WordBankFile = wordBankFile };
        }

        public static LearningContext WithMode(this LearningContext context, LearningModeType mode)
        {
            return context with { Mode = mode };
        }

        public static LearningContext WithSortOrder(this LearningContext context, SortOrderType sortOrder)
        {
            return context with { SortOrder = sortOrder };
        }
    }
}