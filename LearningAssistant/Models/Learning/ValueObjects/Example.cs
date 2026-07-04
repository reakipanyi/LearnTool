namespace LearningAssistant.Models.Learning.ValueObjects
{
    public sealed class Example : ValueObject
    {
        public string Content { get; set; } = string.Empty;
        public string? Translation { get; set; }

        public Example() { }

        public Example(string content, string? translation = null)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Translation = translation;
        }

        public static Example Create(string content, string? translation = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("例句不能为空", nameof(content));
            return new Example(content, translation);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Content;
            yield return Translation ?? string.Empty;
        }
    }
}