using LearningAssistant.Models.ValueObjects;

namespace LearningAssistant.Models.Learning.ValueObjects
{
    public sealed class CharacterFeatures : ValueObject
    {
        public string StrokeCount { get; set; } = string.Empty;
        public string Radical { get; set; } = string.Empty;
        public string Structure { get; set; } = string.Empty;

        public CharacterFeatures() { }

        public CharacterFeatures(string strokeCount, string radical, string structure)
        {
            StrokeCount = strokeCount;
            Radical = radical;
            Structure = structure;
        }

        public static CharacterFeatures Create(string strokeCount, string radical, string structure)
            => new(strokeCount, radical, structure);

        public override IEnumerable<object?> GetEqualityComponents()
        {
            yield return StrokeCount;
            yield return Radical;
            yield return Structure;
        }
    }
}