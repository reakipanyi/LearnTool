namespace LearningAssistant.Models.Learning
{
    /// <summary>
    /// 古诗词学习项
    /// </summary>
    public class ChinesePoem : LearningItem
    {
        /// <summary>
        /// 诗题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 作者
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// 朝代
        /// </summary>
        public string Dynasty { get; set; } = string.Empty;

        /// <summary>
        /// 诗句列表
        /// </summary>
        public List<string> Verses { get; set; } = new List<string>();

        /// <summary>
        /// 注释
        /// </summary>
        public string Annotation { get; set; } = string.Empty;

        /// <summary>
        /// 译文
        /// </summary>
        public string Translation { get; set; } = string.Empty;

        /// <summary>
        /// 赏析/鉴赏
        /// </summary>
        public string Appreciation { get; set; } = string.Empty;

        /// <summary>
        /// 创作背景
        /// </summary>
        public string CreationBackground { get; set; } = string.Empty;

        /// <summary>
        /// 名句
        /// </summary>
        public string FamousLines { get; set; } = string.Empty;

        /// <summary>
        /// 修辞手法
        /// </summary>
        public string RhetoricalDevices { get; set; } = string.Empty;

        /// <summary>
        /// 主题思想
        /// </summary>
        public string Theme { get; set; } = string.Empty;

        /// <summary>
        /// 作者简介
        /// </summary>
        public string AuthorIntro { get; set; } = string.Empty;

        /// <summary>
        /// 诗歌类型（唐诗、宋词、元曲等）
        /// </summary>
        public string PoemType { get; set; } = string.Empty;

        /// <summary>
        /// 相关诗词推荐
        /// </summary>
        public string RelatedPoems { get; set; } = string.Empty;

        /// <summary>
        /// 难度等级
        /// </summary>
        public int DifficultyLevel { get; set; } = 1;

        /// <inheritdoc/>
        public override string GetMainContent() => Title;

        /// <inheritdoc/>
        public override string GetDisplayText()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Author))
                parts.Add($"作者: {Author}");
            if (!string.IsNullOrWhiteSpace(Dynasty))
                parts.Add($"朝代: {Dynasty}");
            if (!string.IsNullOrWhiteSpace(PoemType))
                parts.Add($"类型: {PoemType}");
            if (Verses != null && Verses.Any())
                parts.Add($"诗句: {string.Join("，", Verses)}");
            if (!string.IsNullOrWhiteSpace(Translation))
                parts.Add($"译文: {Translation}");
            if (!string.IsNullOrWhiteSpace(Annotation))
                parts.Add($"注释: {Annotation}");
            if (!string.IsNullOrWhiteSpace(Appreciation))
                parts.Add($"赏析: {Appreciation}");
            if (!string.IsNullOrWhiteSpace(Theme))
                parts.Add($"主题: {Theme}");
            if (!string.IsNullOrWhiteSpace(FamousLines))
                parts.Add($"名句: {FamousLines}");
            if (!string.IsNullOrWhiteSpace(RhetoricalDevices))
                parts.Add($"修辞: {RhetoricalDevices}");
            if (!string.IsNullOrWhiteSpace(CreationBackground))
                parts.Add($"背景: {CreationBackground}");
            if (!string.IsNullOrWhiteSpace(AuthorIntro))
                parts.Add($"作者简介: {AuthorIntro}");
            return string.Join("\n", parts);
        }

        /// <inheritdoc/>
        public override string GetPronunciation() => string.Empty;

        /// <inheritdoc/>
        public override string GetDisplayStruct()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Author))
                parts.Add("作者:?");
            if (!string.IsNullOrWhiteSpace(Dynasty))
                parts.Add("朝代:?");
            if (!string.IsNullOrWhiteSpace(PoemType))
                parts.Add("类型:?");
            if (Verses != null && Verses.Any())
                parts.Add("诗句:?");
            if (!string.IsNullOrWhiteSpace(Translation))
                parts.Add("译文:?");
            if (!string.IsNullOrWhiteSpace(Annotation))
                parts.Add("注释:?");
            if (!string.IsNullOrWhiteSpace(Appreciation))
                parts.Add("赏析:?");
            if (!string.IsNullOrWhiteSpace(Theme))
                parts.Add("主题:?");
            if (!string.IsNullOrWhiteSpace(FamousLines))
                parts.Add("名句:?");
            if (!string.IsNullOrWhiteSpace(RhetoricalDevices))
                parts.Add("修辞:?");
            if (!string.IsNullOrWhiteSpace(CreationBackground))
                parts.Add("背景:?");
            if (!string.IsNullOrWhiteSpace(AuthorIntro))
                parts.Add("作者简介:?");
            return string.Join("\n", parts);
        }

    }
}
