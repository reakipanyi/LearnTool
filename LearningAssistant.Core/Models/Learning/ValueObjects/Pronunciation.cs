using LearningAssistant.Models.ValueObjects;

namespace LearningAssistant.Models.Learning.ValueObjects
{
    public sealed class Pronunciation : ValueObject
    {
        public string Main { get; set; } = string.Empty;
        public string? UkPhonetic { get; set; }
        public string? UsPhonetic { get; set; }

        public Pronunciation() { }

        public Pronunciation(string main, string? ukPhonetic = null, string? usPhonetic = null)
        {
            Main = main ?? throw new ArgumentNullException(nameof(main));
            UkPhonetic = ukPhonetic;
            UsPhonetic = usPhonetic;
        }

        public static Pronunciation Create(string main, string? ukPhonetic = null, string? usPhonetic = null)
        {
            if (string.IsNullOrWhiteSpace(main))
                throw new ArgumentException("发音不能为空", nameof(main));
            return new Pronunciation(main, ukPhonetic, usPhonetic);
        }

        public override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Main;
            yield return UkPhonetic ?? string.Empty;
            yield return UsPhonetic ?? string.Empty;
        }
    }
}