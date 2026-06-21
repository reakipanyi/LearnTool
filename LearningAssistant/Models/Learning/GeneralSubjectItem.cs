
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

        /// <summary>
        /// 时间/时期（历史事件时间、科学发现时间等）
        /// </summary>
        public string TimePeriod { get; set; } = string.Empty;

        /// <summary>
        /// 相关人物
        /// </summary>
        public string RelatedPeople { get; set; } = string.Empty;

        /// <summary>
        /// 相关地点
        /// </summary>
        public string RelatedPlaces { get; set; } = string.Empty;

        /// <summary>
        /// 背景/原因
        /// </summary>
        public string Background { get; set; } = string.Empty;

        /// <summary>
        /// 影响/意义
        /// </summary>
        public string Impact { get; set; } = string.Empty;

        /// <summary>
        /// 原理说明（科学类）
        /// </summary>
        public string Principle { get; set; } = string.Empty;

        /// <summary>
        /// 实验步骤（科学类）
        /// </summary>
        public string ExperimentSteps { get; set; } = string.Empty;

        /// <summary>
        /// 应用场景
        /// </summary>
        public string Applications { get; set; } = string.Empty;

        /// <summary>
        /// 延伸阅读/相关知识
        /// </summary>
        public string FurtherReading { get; set; } = string.Empty;

        /// <summary>
        /// 趣味知识/冷知识
        /// </summary>
        public string FunFact { get; set; } = string.Empty;

        /// <summary>
        /// 图片/图示描述
        /// </summary>
        public string ImageDescription { get; set; } = string.Empty;

        /// <summary>
        /// 重要程度（1-5星）
        /// </summary>
        public int Importance { get; set; } = 3;

        /// <summary>
        /// 标签，多个用逗号分隔
        /// </summary>
        public string Tags { get; set; } = string.Empty;

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
            if (!string.IsNullOrWhiteSpace(Subject))
                parts.Add($"科目: {Subject}");
            if (!string.IsNullOrWhiteSpace(Category))
                parts.Add($"分类: {Category}");
            if (!string.IsNullOrWhiteSpace(TimePeriod))
                parts.Add($"时间: {TimePeriod}");
            if (!string.IsNullOrWhiteSpace(RelatedPeople))
                parts.Add($"人物: {RelatedPeople}");
            if (!string.IsNullOrWhiteSpace(RelatedPlaces))
                parts.Add($"地点: {RelatedPlaces}");
            if (!string.IsNullOrWhiteSpace(Content))
                parts.Add($"内容: {Content}");
            if (!string.IsNullOrWhiteSpace(Background))
                parts.Add($"背景: {Background}");
            if (!string.IsNullOrWhiteSpace(Principle))
                parts.Add($"原理: {Principle}");
            if (!string.IsNullOrWhiteSpace(KeyPoints))
                parts.Add($"要点: {KeyPoints}");
            if (!string.IsNullOrWhiteSpace(Example))
                parts.Add($"例题: {Example}");
            if (!string.IsNullOrWhiteSpace(ExperimentSteps))
                parts.Add($"实验步骤: {ExperimentSteps}");
            if (!string.IsNullOrWhiteSpace(Applications))
                parts.Add($"应用: {Applications}");
            if (!string.IsNullOrWhiteSpace(Impact))
                parts.Add($"影响: {Impact}");
            if (!string.IsNullOrWhiteSpace(Question))
                parts.Add($"问题: {Question}");
            if (!string.IsNullOrWhiteSpace(Answer))
                parts.Add($"答案: {Answer}");
            if (!string.IsNullOrWhiteSpace(Analysis))
                parts.Add($"解析: {Analysis}");
            if (!string.IsNullOrWhiteSpace(FunFact))
                parts.Add($"趣味知识: {FunFact}");
            if (!string.IsNullOrWhiteSpace(FurtherReading))
                parts.Add($"延伸阅读: {FurtherReading}");
            if (!string.IsNullOrWhiteSpace(Note))
                parts.Add($"备注: {Note}");
            if (!string.IsNullOrWhiteSpace(Tags))
                parts.Add($"标签: {Tags}");

            return string.Join("\n", parts);
        }

        public override string GetPronunciation() => string.Empty;

        public override string GetDisplayStruct()
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(Topic))
                parts.Add("主题:?");
            if (!string.IsNullOrWhiteSpace(Subject))
                parts.Add("科目:?");
            if (!string.IsNullOrWhiteSpace(Category))
                parts.Add("分类:?");
            if (!string.IsNullOrWhiteSpace(TimePeriod))
                parts.Add("时间:?");
            if (!string.IsNullOrWhiteSpace(RelatedPeople))
                parts.Add("人物:?");
            if (!string.IsNullOrWhiteSpace(RelatedPlaces))
                parts.Add("地点:?");
            if (!string.IsNullOrWhiteSpace(Content))
                parts.Add("内容:?");
            if (!string.IsNullOrWhiteSpace(Background))
                parts.Add("背景:?");
            if (!string.IsNullOrWhiteSpace(Principle))
                parts.Add("原理:?");
            if (!string.IsNullOrWhiteSpace(KeyPoints))
                parts.Add("要点:?");
            if (!string.IsNullOrWhiteSpace(Example))
                parts.Add("例题:?");
            if (!string.IsNullOrWhiteSpace(ExperimentSteps))
                parts.Add("实验步骤:?");
            if (!string.IsNullOrWhiteSpace(Applications))
                parts.Add("应用:?");
            if (!string.IsNullOrWhiteSpace(Impact))
                parts.Add("影响:?");
            if (!string.IsNullOrWhiteSpace(Question))
                parts.Add("问题:?");
            if (!string.IsNullOrWhiteSpace(Answer))
                parts.Add("答案:?");
            if (!string.IsNullOrWhiteSpace(Analysis))
                parts.Add("解析:?");
            if (!string.IsNullOrWhiteSpace(FunFact))
                parts.Add("趣味知识:?");
            if (!string.IsNullOrWhiteSpace(FurtherReading))
                parts.Add("延伸阅读:?");
            if (!string.IsNullOrWhiteSpace(Note))
                parts.Add("备注:?");
            if (!string.IsNullOrWhiteSpace(Tags))
                parts.Add("标签:?");

            return string.Join("\n", parts);
        }
    }
}
