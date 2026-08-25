using LearningAssistant.Models.ValueObjects;

namespace LearningAssistant.Models.Learning.ValueObjects
{
    public sealed class WordFeatures : ValueObject
    {
        public string PartOfSpeech { get; set; } = string.Empty;
        public string WordForms { get; set; } = string.Empty;
        public string Collocations { get; set; } = string.Empty;
        public string SyllableBreakdown { get; set; } = string.Empty;

        public WordFeatures() { }

        public WordFeatures(string partOfSpeech, string wordForms, string collocations, string syllableBreakdown)
        {
            PartOfSpeech = partOfSpeech;
            WordForms = wordForms;
            Collocations = collocations;
            SyllableBreakdown = syllableBreakdown;
        }

        public static WordFeatures Create(string partOfSpeech = "", string wordForms = "", 
                                          string collocations = "", string syllableBreakdown = "")
            => new(partOfSpeech, wordForms, collocations, syllableBreakdown);

        public override IEnumerable<object?> GetEqualityComponents()
        {
            yield return PartOfSpeech;
            yield return WordForms;
            yield return Collocations;
            yield return SyllableBreakdown;
        }
    }
}