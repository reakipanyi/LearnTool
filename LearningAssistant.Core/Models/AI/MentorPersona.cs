namespace LearningAssistant.Models.AI
{
    /// <summary>
    /// 导师角色类型
    /// </summary>
    public enum MentorPersonaType
    {
        /// <summary>
        /// 普通答疑导师 - 回答问题、解释概念
        /// </summary>
        Tutor,

        /// <summary>
        /// 苏格拉底式引导 - 通过提问引导思考
        /// </summary>
        Socratic,

        /// <summary>
        /// 费曼检验 - 检验用户的解释是否清晰易懂
        /// </summary>
        Feynman,

        /// <summary>
        /// 薄弱点诊断 - 分析学习数据找出薄弱环节
        /// </summary>
        Diagnostician
    }

    /// <summary>
    /// 导师角色配置
    /// </summary>
    public class MentorPersona
    {
        public MentorPersonaType Type { get; set; } = MentorPersonaType.Tutor;

        public string Name => Type switch
        {
            MentorPersonaType.Tutor => "答疑导师",
            MentorPersonaType.Socratic => "苏格拉底",
            MentorPersonaType.Feynman => "费曼教练",
            MentorPersonaType.Diagnostician => "诊断专家",
            _ => "导师"
        };

        public string Icon => Type switch
        {
            MentorPersonaType.Tutor => "📖",
            MentorPersonaType.Socratic => "💭",
            MentorPersonaType.Feynman => "🎓",
            MentorPersonaType.Diagnostician => "🔍",
            _ => "🤖"
        };

        /// <summary>
        /// 获取该角色的系统提示词
        /// </summary>
        public string GetSystemPrompt()
        {
            return Type switch
            {
                MentorPersonaType.Tutor =>
                    "你是一位耐心的学习导师，用简洁清晰的语言回答用户的问题。" +
                    "解释时尽量结合具体例子，帮助用户真正理解概念。",

                MentorPersonaType.Socratic =>
                    "你是一位苏格拉底式的导师，通过提问引导用户自己思考答案。" +
                    "不要直接给出答案，而是用一系列问题帮助用户逐步深入理解。" +
                    "多用'为什么'、'你怎么想到的'、'还有呢'这类引导性提问。",

                MentorPersonaType.Feynman =>
                    "你是一位费曼学习法的教练。" +
                    "要求用户用自己的话解释概念，如果你发现用户的解释不够清晰，" +
                    "用简单的比喻和例子帮助用户把复杂概念讲简单。" +
                    "记住：真正理解一个概念，就是能用简单的语言解释它。",

                MentorPersonaType.Diagnostician =>
                    "你是一位学习诊断专家，分析用户的学习状态并给出改进建议。" +
                    "基于用户提供的信息，找出薄弱环节并给出针对性的学习方案。",

                _ => "你是一位学习助手，帮助用户解答问题。"
            };
        }
    }
}
