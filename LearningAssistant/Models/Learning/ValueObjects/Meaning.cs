namespace LearningAssistant.Models.Learning.ValueObjects
{
    public sealed class Meaning : ValueObject
    {
        public string Content { get; set; } = string.Empty;

        public Meaning() { }

        public Meaning(string content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public static Meaning Create(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("释义不能为空", nameof(content));
            return new Meaning(content);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Content;
        }
    }
}