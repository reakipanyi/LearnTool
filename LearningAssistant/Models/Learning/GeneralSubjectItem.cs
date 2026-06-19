
namespace LearningAssistant.Models.Learning
{
    public class GeneralSubjectItem : LearningItem
    {
        public string Subject { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string Topic { get; set; } = string.Empty;

        public string KeyPoints { get; set; } = string.Empty;

        public string Example { get; set; } = string.Empty;

        public string Question { get; set; } = string.Empty;

        public string Answer { get; set; } = string.Empty;

        public string Analysis { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;

        public int DifficultyLevel { get; set; } = 1;

        public override string GetMainContent()
        {
            if (!string.IsNullOrWhiteSpace(Topic))
                return Topic;
            if (!string.IsNullOrWhiteSpace(Content))
                return Content.Length > 20 ? Content.Substring(0, 20) + "..." : Content;
            return "未命名";
        }

        public override string GetDisplayText()
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(Topic))
                parts.Add($"主题: {Topic}");
            if (!string.IsNullOrWhiteSpace(Content))
                parts.Add($"内容: {Content}");
            if (!string.IsNullOrWhiteSpace(KeyPoints))
                parts.Add($"要点: {KeyPoints}");
            if (!string.IsNullOrWhiteSpace(Example))
                parts.Add($"例题: {Example}");
            if (!string.IsNullOrWhiteSpace(Question))
                parts.Add($"问题: {Question}");
            if (!string.IsNullOrWhiteSpace(Answer))
                parts.Add($"答案: {Answer}");
            if (!string.IsNullOrWhiteSpace(Analysis))
                parts.Add($"解析: {Analysis}");
            if (!string.IsNullOrWhiteSpace(Note))
                parts.Add($"备注: {Note}");

            return string.Join("\n", parts);
        }

        public override string GetPronunciation() => string.Empty;

        public override string GetDisplayStruct()
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(Topic))
                parts.Add("主题:?");
            if (!string.IsNullOrWhiteSpace(Content))
                parts.Add("内容:?");
            if (!string.IsNullOrWhiteSpace(KeyPoints))
                parts.Add("要点:?");
            if (!string.IsNullOrWhiteSpace(Example))
                parts.Add("例题:?");
            if (!string.IsNullOrWhiteSpace(Question))
                parts.Add("问题:?");
            if (!string.IsNullOrWhiteSpace(Answer))
                parts.Add("答案:?");
            if (!string.IsNullOrWhiteSpace(Analysis))
                parts.Add("解析:?");
            if (!string.IsNullOrWhiteSpace(Note))
                parts.Add("备注:?");

            return string.Join("\n", parts);
        }
    }
}
