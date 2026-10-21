namespace LearningAssistant.Models.Pdf
{
    /// <summary>音频匹配规则的写法。</summary>
    public enum AudioPatternMode
    {
        /// <summary>正则表达式</summary>
        Regex = 0,

        /// <summary>通配符：* 匹配任意字符，? 匹配单个字符</summary>
        Wildcard = 1
    }
}
