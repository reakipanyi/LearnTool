namespace LearningAssistant.Common.Events
{
    public class ContextChangedEventArgs : EventArgs
    {
        public SubjectType? OldSubject { get; }
        public SubjectType? NewSubject { get; }
        public SubCategoryType? OldSubCategory { get; }
        public SubCategoryType? NewSubCategory { get; }

        public ContextChangedEventArgs(SubjectType? oldSubject, SubjectType? newSubject,
            SubCategoryType? oldSubCategory, SubCategoryType? newSubCategory)
        {
            OldSubject = oldSubject;
            NewSubject = newSubject;
            OldSubCategory = oldSubCategory;
            NewSubCategory = newSubCategory;
        }
    }
}