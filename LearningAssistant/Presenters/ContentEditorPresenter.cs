using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Learning;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;

namespace LearningAssistant.Presenters
{
    /// <summary>
    /// 内容编辑器Presenter，负责管理学习内容的编辑、导入、导出和AI生成功能
    /// </summary>
    public class ContentEditorPresenter : IDisposable
    {
        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger<ContentEditorPresenter> _logger;

        /// <summary>
        /// 视图接口，用于与UI层交互
        /// </summary>
        private readonly IContentEditorView _view;

        /// <summary>
        /// 内容加载服务，用于数据的持久化操作
        /// </summary>
        private readonly IContentLoaderService _contentLoaderService;

        /// <summary>
        /// AI问答服务，用于生成学习内容
        /// </summary>
        private readonly IAiQuestionService _aiQuestionService;

        /// <summary>
        /// 脏标记，标识当前数据是否有未保存的更改
        /// </summary>
        private bool _isDirty = false;

        /// <summary>
        /// 类别类型名称映射字典，将类别常量映射为中文显示名称
        /// </summary>
        private static readonly Dictionary<string, string> CategoryTypeNames = new()
        {
            { Constants.SubCategory.ChineseCharacter, "识字" },
            { Constants.SubCategory.ChineseIdiom, "成语" },
            { Constants.SubCategory.ChinesePhrase, "短语" },
            { Constants.SubCategory.ChinesePoem, "诗词" },
            { Constants.SubCategory.ChineseComprehensive, "语文综合" },
            { Constants.SubCategory.EnglishWord, "英语单词" },
            { Constants.SubCategory.EnglishPhrase, "英语短语" },
            { Constants.SubCategory.EnglishSentence, "英语句子" },
            { Constants.SubCategory.EnglishComprehensive, "英语综合" }
        };

        /// <summary>
        /// 按子分类分组的表头中英文映射字典
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, string>> CategoryColumnHeaders = new()
        {
            { Constants.SubCategory.ChineseCharacter, new Dictionary<string, string>
                { { "Character", "汉字" }, { "Pinyin", "拼音" }, { "Meaning", "释义" }, { "StrokeCount", "笔画数" }, { "Radical", "部首" }, { "StrokeOrder", "笔顺" }, { "Words", "组词" }, { "SimilarCharacters", "形近字" }, { "Synonyms", "近义词" }, { "Antonyms", "反义词" }, { "CommonMistakes", "易错点" }, { "ExampleSentence", "例句" }, { "CharacterLevel", "字级" }, { "Structure", "结构" }, { "CharacterFormation", "造字法" }, { "OtherPronunciations", "其他读音" }, { "Id", "ID" }, { "CreatedAt", "创建时间" }, { "UpdatedAt", "更新时间" } } },
            { Constants.SubCategory.ChineseIdiom, new Dictionary<string, string>
                { { "Idiom", "成语" }, { "Pinyin", "拼音" }, { "Meaning", "释义" }, { "Origin", "出处" }, { "Example", "例句" } } },
            { Constants.SubCategory.ChinesePhrase, new Dictionary<string, string>
                { { "Phrase", "短语" }, { "Pinyin", "拼音" }, { "Meaning", "释义" }, { "Example", "例句" } } },
            { Constants.SubCategory.ChinesePoem, new Dictionary<string, string>
                { { "Title", "诗名" }, { "Author", "作者" }, { "Dynasty", "朝代" }, { "Verses", "诗句" }, { "Annotation", "注释" } } },
            { Constants.SubCategory.ChineseComprehensive, new Dictionary<string, string>
                { { "Title", "课文标题" }, { "Content", "课文内容" }, { "Questions", "课后习题" }, { "Question", "题目" }, { "Answer", "答案" }, { "Analysis", "解析" } } },
            { Constants.SubCategory.EnglishWord, new Dictionary<string, string>
                { { "Word", "单词" }, { "Phonetic", "音标" }, { "PartOfSpeech", "词性" }, { "SyllableBreakdown", "音节拼读" }, { "Meaning", "中文释义" }, { "Example", "例句" }, { "Synonyms", "近义词" }, { "Antonyms", "反义词" }, { "Id", "ID" }, { "CreatedAt", "创建时间" }, { "UpdatedAt", "更新时间" } } },
            { Constants.SubCategory.EnglishPhrase, new Dictionary<string, string>
                { { "Phrase", "短语" }, { "Meaning", "中文释义" }, { "Example", "例句" } } },
            { Constants.SubCategory.EnglishSentence, new Dictionary<string, string>
                { { "Sentence", "句子" }, { "Translation", "中文翻译" }, { "Grammar", "语法点" } } },
            { Constants.SubCategory.EnglishComprehensive, new Dictionary<string, string>
                { { "Title", "文章标题" }, { "Content", "文章内容" }, { "Questions", "阅读理解题" }, { "Question", "题目" }, { "Answer", "答案" }, { "Analysis", "解析" } } },
            { Constants.SubCategory.MathFormula, new Dictionary<string, string>
                { { "Topic", "公式名称" }, { "Content", "公式表达式" }, { "KeyPoints", "公式说明" }, { "Principle", "适用条件" }, { "Example", "应用举例" }, { "Applications", "应用场景" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.MathExample, new Dictionary<string, string>
                { { "Topic", "例题标题" }, { "Content", "题目描述" }, { "Analysis", "解答过程" }, { "KeyPoints", "关键步骤" }, { "Example", "方法总结" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.MathConcept, new Dictionary<string, string>
                { { "Topic", "概念名称" }, { "Content", "定义" }, { "KeyPoints", "性质" }, { "Example", "举例说明" }, { "Note", "注意事项" }, { "Applications", "应用" }, { "Tags", "标签" } } },
            { Constants.SubCategory.MathComprehensive, new Dictionary<string, string>
                { { "Topic", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" }, { "Example", "典型例题" }, { "Analysis", "答案解析" }, { "Question", "问题" }, { "Answer", "答案" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.PhysicsLaw, new Dictionary<string, string>
                { { "Topic", "定律名称" }, { "Content", "定律内容" }, { "KeyPoints", "公式" }, { "Principle", "适用条件" }, { "Applications", "应用场景" }, { "Example", "实例" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.PhysicsExperiment, new Dictionary<string, string>
                { { "Topic", "实验名称" }, { "Content", "实验目的" }, { "KeyPoints", "实验器材" }, { "ExperimentSteps", "实验步骤" }, { "Analysis", "实验结论" }, { "Example", "实例" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.PhysicsDerivation, new Dictionary<string, string>
                { { "Topic", "公式名称" }, { "Content", "推导结果" }, { "KeyPoints", "推导步骤" }, { "Principle", "前提条件" }, { "Example", "应用实例" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.PhysicsComprehensive, new Dictionary<string, string>
                { { "Topic", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" }, { "Example", "典型例题" }, { "Analysis", "答案解析" }, { "Question", "问题" }, { "Answer", "答案" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.ChemistryEquation, new Dictionary<string, string>
                { { "Topic", "反应名称" }, { "Content", "化学方程式" }, { "KeyPoints", "反应条件" }, { "Principle", "反应原理" }, { "Example", "反应现象" }, { "Applications", "应用" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.ChemistryElement, new Dictionary<string, string>
                { { "Topic", "元素名称" }, { "Content", "元素符号" }, { "KeyPoints", "原子序数" }, { "Principle", "元素性质" }, { "Applications", "主要用途" }, { "Example", "实例" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.ChemistryExperiment, new Dictionary<string, string>
                { { "Topic", "实验名称" }, { "Content", "实验目的" }, { "KeyPoints", "实验器材" }, { "ExperimentSteps", "操作步骤" }, { "Analysis", "实验现象" }, { "Example", "实例" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.ChemistryComprehensive, new Dictionary<string, string>
                { { "Topic", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" }, { "Example", "典型例题" }, { "Analysis", "答案解析" }, { "Question", "问题" }, { "Answer", "答案" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.HistoryEvent, new Dictionary<string, string>
                { { "Topic", "事件名称" }, { "TimePeriod", "发生时间" }, { "RelatedPlaces", "发生地点" }, { "Background", "历史背景" }, { "Content", "事件经过" }, { "Impact", "历史影响" }, { "RelatedPeople", "相关人物" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.HistoryPerson, new Dictionary<string, string>
                { { "Topic", "人物姓名" }, { "TimePeriod", "所处朝代" }, { "Content", "生卒年月" }, { "KeyPoints", "主要成就" }, { "Analysis", "历史评价" }, { "Example", "代表作品" }, { "RelatedPlaces", "相关地点" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.HistoryTimeline, new Dictionary<string, string>
                { { "Topic", "时代名称" }, { "TimePeriod", "时间范围" }, { "KeyPoints", "重要事件" }, { "Content", "时代特征" }, { "RelatedPeople", "重要人物" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.HistoryComprehensive, new Dictionary<string, string>
                { { "Topic", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" }, { "Example", "典型例题" }, { "Analysis", "答案解析" }, { "Question", "问题" }, { "Answer", "答案" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.GeographyKnowledge, new Dictionary<string, string>
                { { "Topic", "地理名称" }, { "Category", "地理分类" }, { "Content", "地理描述" }, { "RelatedPlaces", "分布地区" }, { "KeyPoints", "主要特征" }, { "Example", "实例" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.GeographyMap, new Dictionary<string, string>
                { { "Topic", "地图名称" }, { "RelatedPlaces", "所属地区" }, { "Content", "地理特征" }, { "KeyPoints", "重要地点" }, { "Example", "读图技巧" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.GeographyClimate, new Dictionary<string, string>
                { { "Topic", "气候类型" }, { "RelatedPlaces", "分布地区" }, { "Content", "气候特征" }, { "Principle", "形成原因" }, { "KeyPoints", "植被类型" }, { "Example", "实例" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.GeographyComprehensive, new Dictionary<string, string>
                { { "Topic", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" }, { "Example", "典型例题" }, { "Analysis", "答案解析" }, { "Question", "问题" }, { "Answer", "答案" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.BiologyConcept, new Dictionary<string, string>
                { { "Topic", "概念名称" }, { "Content", "定义" }, { "Category", "分类" }, { "KeyPoints", "主要特征" }, { "Applications", "功能作用" }, { "Example", "实例" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.BiologyExperiment, new Dictionary<string, string>
                { { "Topic", "实验名称" }, { "Content", "实验目的" }, { "KeyPoints", "实验材料" }, { "ExperimentSteps", "实验步骤" }, { "Analysis", "实验结果" }, { "Example", "实例" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.BiologyPhenomenon, new Dictionary<string, string>
                { { "Topic", "现象名称" }, { "Content", "现象描述" }, { "Category", "现象类型" }, { "Principle", "产生原因" }, { "Example", "常见实例" }, { "Impact", "生物意义" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.BiologyComprehensive, new Dictionary<string, string>
                { { "Topic", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" }, { "Example", "典型例题" }, { "Analysis", "答案解析" }, { "Question", "问题" }, { "Answer", "答案" }, { "Note", "备注" }, { "Tags", "标签" } } }
        };

        /// <summary>
        /// 类别模板字典，定义每个类别对应的字段结构
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, object>> CategoryTemplates = new()
        {
            {
                Constants.SubCategory.ChineseCharacter, new Dictionary<string, object>
                {
                    { "Character", "" }, { "Pinyin", "" }, { "Meaning", "" }, { "StrokeCount", "" }, { "Radical", "" }, { "StrokeOrder", "" }, { "Words", "" }, { "SimilarCharacters", "" }, { "Synonyms", "" }, { "Antonyms", "" }, { "CommonMistakes", "" }, { "ExampleSentence", "" }, { "CharacterLevel", "" }, { "Structure", "" }, { "CharacterFormation", "" }, { "OtherPronunciations", "" }
                }
            },
            {
                Constants.SubCategory.ChineseIdiom, new Dictionary<string, object>
                {
                    { "Idiom", "" }, { "Pinyin", "" }, { "Meaning", "" }, { "Origin", "" }, { "Example", "" }
                }
            },
            {
                Constants.SubCategory.ChinesePhrase, new Dictionary<string, object>
                {
                    { "Phrase", "" }, { "Pinyin", "" }, { "Meaning", "" }, { "Example", "" }
                }
            },
            {
                Constants.SubCategory.ChinesePoem, new Dictionary<string, object>
                {
                    { "Title", "" }, { "Author", "" }, { "Dynasty", "" }, { "Verses", "" }, { "Annotation", "" }, { "Translation", "" }, { "Appreciation", "" }, { "CreationBackground", "" }, { "FamousLines", "" }, { "RhetoricalDevices", "" }, { "Theme", "" }, { "AuthorIntro", "" }, { "PoemType", "" }, { "RelatedPoems", "" }, { "DifficultyLevel", 1 }
                }
            },
            {
                Constants.SubCategory.EnglishWord, new Dictionary<string, object>
                {
                    { "Word", "" }, { "Phonetic", "" }, { "PartOfSpeech", "" }, { "SyllableBreakdown", "" }, { "Meaning", "" }, { "Example", "" }, { "Synonyms", "" }, { "Antonyms", "" }, { "WordForms", "" }, { "WordRootAffix", "" }, { "Collocations", "" }, { "Phrases", "" }, { "SynonymAnalysis", "" }, { "UkPhonetic", "" }, { "UsPhonetic", "" }, { "VocabularyLevel", "" }, { "Etymology", "" }, { "ConfusableWords", "" }
                }
            },
            {
                Constants.SubCategory.EnglishPhrase, new Dictionary<string, object>
                {
                    { "Phrase", "" }, { "Meaning", "" }, { "Example", "" }
                }
            },
            {
                Constants.SubCategory.EnglishSentence, new Dictionary<string, object>
                {
                    { "Sentence", "" }, { "Translation", "" }, { "Grammar", "" }
                }
            },
            {
                Constants.SubCategory.ChineseComprehensive, new Dictionary<string, object>
                {
                    { "Title", "" }, { "Content", "" }, { "Questions", new List<object> { new Dictionary<string, object> { { "Question", "" }, { "Answer", "" } } } }, { "Analysis", "" }
                }
            },
            {
                Constants.SubCategory.EnglishComprehensive, new Dictionary<string, object>
                {
                    { "Title", "" }, { "Content", "" }, { "Questions", new List<object> { new Dictionary<string, object> { { "Question", "" }, { "Answer", "" } } } }, { "Analysis", "" }
                }
            },
            {
                Constants.SubCategory.MathFormula, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", "" }, { "Principle", "" }, { "Example", "" }, { "Applications", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.MathExample, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "Analysis", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.MathConcept, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Note", "" }, { "Applications", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.MathComprehensive, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Analysis", "" }, { "Question", "" }, { "Answer", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.PhysicsLaw, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", "" }, { "Principle", "" }, { "Applications", "" }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.PhysicsExperiment, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "ExperimentSteps", new List<string> { "", "", "" } }, { "Analysis", "" }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.PhysicsDerivation, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Principle", "" }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.PhysicsComprehensive, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Analysis", "" }, { "Question", "" }, { "Answer", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.ChemistryEquation, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", "" }, { "Principle", "" }, { "Example", "" }, { "Applications", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.ChemistryElement, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", "" }, { "Principle", "" }, { "Applications", "" }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.ChemistryExperiment, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "ExperimentSteps", new List<string> { "", "", "" } }, { "Analysis", "" }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.ChemistryComprehensive, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Analysis", "" }, { "Question", "" }, { "Answer", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.HistoryEvent, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "TimePeriod", "" }, { "RelatedPlaces", "" }, { "Background", "" }, { "Content", "" }, { "Impact", "" }, { "RelatedPeople", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.HistoryPerson, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "TimePeriod", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Analysis", "" }, { "Example", "" }, { "RelatedPlaces", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.HistoryTimeline, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "TimePeriod", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Content", "" }, { "RelatedPeople", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.HistoryComprehensive, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Analysis", "" }, { "Question", "" }, { "Answer", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.GeographyKnowledge, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Category", "" }, { "Content", "" }, { "RelatedPlaces", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.GeographyMap, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "RelatedPlaces", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.GeographyClimate, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "RelatedPlaces", "" }, { "Content", "" }, { "Principle", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.GeographyComprehensive, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Analysis", "" }, { "Question", "" }, { "Answer", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.BiologyConcept, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "Category", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Applications", "" }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.BiologyExperiment, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "ExperimentSteps", new List<string> { "", "", "" } }, { "Analysis", "" }, { "Example", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.BiologyPhenomenon, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "Category", "" }, { "Principle", "" }, { "Example", "" }, { "Impact", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.BiologyComprehensive, new Dictionary<string, object>
                {
                    { "Topic", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Analysis", "" }, { "Question", "" }, { "Answer", "" }, { "Note", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            }
        };

        /// <summary>
        /// JSON格式提示字典，用于AI生成时指定输出格式
        /// </summary>
        private static readonly Dictionary<string, string> JsonFormatHints = new()
        {
            { Constants.SubCategory.ChineseCharacter, @"[  {""Character"":"""",""Pinyin"":"""",""Meaning"":"""",""StrokeCount"":"""",""Radical"":"""",""StrokeOrder"":"""",""Words"":""...,...""} ]" },
            { Constants.SubCategory.ChineseIdiom, @"[  {""Idiom"":"""",""Pinyin"":"""",""Meaning"":"""",""Origin"":"""",""Example"":""""} ]" },
            { Constants.SubCategory.ChinesePhrase, @"[  {""Phrase"":"""",""Pinyin"":"""",""Meaning"":"""",""Example"":""""} ]" },
            { Constants.SubCategory.ChinesePoem, @"[  {""Title"":"""",""Author"":"""",""Dynasty"":"""",""Verses"":["""","""","""",""""],""Annotation"":""""} ]" },
            { Constants.SubCategory.ChineseComprehensive, @"[  {""Title"":"""",""Content"":"""",""Questions"":[{""Question"":"""",""Answer"":""""}],""Analysis"":""""} ]" },
            { Constants.SubCategory.EnglishWord, @"[  {""Word"":"""",""Phonetic"":"""",""PartOfSpeech"":"""",""SyllableBreakdown"":"""",""Meaning"":"""",""Example"":""""} ]" },
            { Constants.SubCategory.EnglishPhrase, @"[  {""Phrase"":"""",""Meaning"":"""",""Example"":""""} ]" },
            { Constants.SubCategory.EnglishSentence, @"[  {""Sentence"":"""",""Translation"":"""",""Grammar"":""""} ]" },
            { Constants.SubCategory.EnglishComprehensive, @"[  {""Title"":"""",""Content"":"""",""Questions"":[{""Question"":"""",""Answer"":""""}],""Analysis"":""""} ]" }
        };

        /// <summary>
        /// 构造函数，初始化ContentEditorPresenter
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="view">视图接口</param>
        /// <param name="contentLoaderService">内容加载服务</param>
        /// <param name="aiQuestionService">AI问答服务</param>
        /// <exception cref="ArgumentNullException">当任一参数为null时抛出</exception>
        public ContentEditorPresenter(
            ILogger<ContentEditorPresenter> logger,
            IContentEditorView view,
            IContentLoaderService contentLoaderService,
            IAiQuestionService aiQuestionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _contentLoaderService = contentLoaderService ?? throw new ArgumentNullException(nameof(contentLoaderService));
            _aiQuestionService = aiQuestionService ?? throw new ArgumentNullException(nameof(aiQuestionService));

            _view.SubjectChanged += OnSubjectChanged;
            _view.LanguageChanged += OnLanguageChanged;
            _view.SubCategoryChanged += OnSubCategoryChanged;
            _view.TemplateAddClicked += OnTemplateAddClicked;
            _view.TemplateSaveClicked += OnTemplateSaveClicked;
            _view.TemplateDeleteClicked += OnTemplateDeleteClicked;
            _view.ImportClicked += OnImportClicked;
            _view.ExportClicked += OnExportClicked;
            _view.GridCellEndEdit += OnGridValueChanged;
            _view.GridRowsAdded += OnGridRowsAdded;

            _logger.LogInformation("ContentEditorPresenter initialized");
        }

        /// <summary>
        /// 初始化Presenter，加载子类别和数据
        /// </summary>
        public void Initialize()
        {
            LoadSubCategories();
            LoadItems();
            _isDirty = false;
        }

        /// <summary>
        /// 学科切换事件处理方法
        /// </summary>
        private void OnSubjectChanged(object? sender, EventArgs e)
        {
            if (CheckAndSaveUnsavedChanges())
            {
                LoadSubCategories();
                LoadItems();
            }
        }

        /// <summary>
        /// 语言切换事件处理方法（兼容旧版）
        /// </summary>
        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            // 学科变更事件已经处理了，这里不做额外处理
        }

        /// <summary>
        /// 子类别切换事件处理方法
        /// </summary>
        private void OnSubCategoryChanged(object? sender, EventArgs e)
        {
            if (CheckAndSaveUnsavedChanges())
            {
                LoadItems();
            }
        }

        /// <summary>
        /// 根据当前语言加载子类别列表
        /// </summary>
        private void LoadSubCategories()
        {
            var subject = _view.SelectedSubject;
            var subCategories = _contentLoaderService.GetSubCategoriesBySubject(subject);
            _view.RefreshSubCategories(subCategories);
        }

        /// <summary>
        /// 加载当前类别的数据项
        /// </summary>
        private void LoadItems()
        {
            var category = _view.SelectedSubCategory;
            var items = _contentLoaderService.LoadItems(category);
            _view.ItemData = ConvertToDataTable(items, category);
            _isDirty = false;
        }

        /// <summary>
        /// 获取列的中文名称
        /// </summary>
        /// <param name="columnName">英文列名</param>
        /// <returns>中文列名，如果没有映射则返回原名称</returns>
        private static readonly Dictionary<string, string> CommonColumnHeaders = new()
        {
            { "Id", "ID" }, { "CreatedAt", "创建时间" }, { "UpdatedAt", "更新时间" },
            { "Synonyms", "近义词" }, { "Antonyms", "反义词" }, { "CommonMistakes", "易错点" },
            { "ExampleSentence", "例句" }, { "OtherPronunciations", "其他读音" }
        };

        private static string GetChineseColumnName(string columnName, string category)
        {
            if (!string.IsNullOrEmpty(category) &&
                CategoryColumnHeaders.TryGetValue(category, out var headers) &&
                headers.TryGetValue(columnName, out var chineseName))
            {
                return chineseName;
            }
            if (CommonColumnHeaders.TryGetValue(columnName, out var commonName))
            {
                return commonName;
            }
            return columnName;
        }

        private static string GetEnglishColumnName(string columnName, string category)
        {
            if (!string.IsNullOrEmpty(category) &&
                CategoryColumnHeaders.TryGetValue(category, out var headers))
            {
                foreach (var pair in headers)
                {
                    if (pair.Value == columnName)
                    {
                        return pair.Key;
                    }
                }
            }
            return columnName;
        }

        /// <summary>
        /// 将对象列表转换为DataTable，所有列均为string类型以避免类型推断问题
        /// </summary>
        /// <param name="items">对象列表</param>
        /// <param name="category">类别名称</param>
        /// <returns>转换后的DataTable</returns>
        private DataTable ConvertToDataTable(List<LearningItem> items, string category)
        {
            var table = new DataTable();

            if (items.Count == 0)
            {
                if (CategoryTemplates.TryGetValue(category, out var template))
                {
                    foreach (var key in template.Keys)
                    {
                        var column = table.Columns.Add(key, typeof(string));
                        column.Caption = GetChineseColumnName(key, category);
                    }
                }
                return table;
            }

            var allColumns = new HashSet<string>();
            if (CategoryTemplates.TryGetValue(category, out var template2))
            {
                foreach (var key in template2.Keys)
                    allColumns.Add(key);
            }

            foreach (var item in items)
            {
                allColumns.Add("Id");
                allColumns.Add("CreatedAt");
                allColumns.Add("UpdatedAt");

                if (!string.IsNullOrWhiteSpace(item.MainContent))
                {
                    if (category.StartsWith("English"))
                        allColumns.Add("Word");
                    else if (category.StartsWith("Chinese"))
                        allColumns.Add(category.Contains("Character") ? "Character" : category.Contains("Idiom") ? "Idiom" : category.Contains("Poem") ? "Title" : "Phrase");
                }

                if (item.Meaning != null)
                    allColumns.Add("Meaning");
                if (item.Example != null)
                {
                    allColumns.Add("Example");
                    if (!string.IsNullOrWhiteSpace(item.Example.Translation))
                        allColumns.Add("ExampleTranslation");
                }
                if (item.Pronunciation != null)
                {
                    allColumns.Add("Phonetic");
                    if (!string.IsNullOrWhiteSpace(item.Pronunciation.UkPhonetic))
                        allColumns.Add("UkPhonetic");
                    if (!string.IsNullOrWhiteSpace(item.Pronunciation.UsPhonetic))
                        allColumns.Add("UsPhonetic");
                }
                if (item.CharacterFeatures != null)
                {
                    allColumns.Add("StrokeCount");
                    allColumns.Add("Radical");
                    allColumns.Add("Structure");
                }
                if (item.WordFeatures != null)
                {
                    allColumns.Add("PartOfSpeech");
                    allColumns.Add("WordForms");
                    allColumns.Add("Collocations");
                    allColumns.Add("SyllableBreakdown");
                }
            }

            foreach (var col in allColumns)
            {
                var column = table.Columns.Add(col, typeof(string));
                column.Caption = GetChineseColumnName(col, category);
            }

            foreach (var item in items)
            {
                var row = table.NewRow();
                row["Id"] = item.Id;
                row["CreatedAt"] = item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                row["UpdatedAt"] = item.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");

                if (category.StartsWith("English"))
                    row["Word"] = item.MainContent;
                else if (category.StartsWith("Chinese"))
                {
                    if (category.Contains("Character"))
                        row["Character"] = item.MainContent;
                    else if (category.Contains("Idiom"))
                        row["Idiom"] = item.MainContent;
                    else if (category.Contains("Poem"))
                        row["Title"] = item.MainContent;
                    else
                        row["Phrase"] = item.MainContent;
                }

                if (item.Meaning != null)
                    row["Meaning"] = item.Meaning.Content;
                if (item.Example != null)
                {
                    row["Example"] = item.Example.Content;
                    if (!string.IsNullOrWhiteSpace(item.Example.Translation))
                        row["ExampleTranslation"] = item.Example.Translation;
                }
                if (item.Pronunciation != null)
                {
                    row["Phonetic"] = item.Pronunciation.Main;
                    if (!string.IsNullOrWhiteSpace(item.Pronunciation.UkPhonetic))
                        row["UkPhonetic"] = item.Pronunciation.UkPhonetic;
                    if (!string.IsNullOrWhiteSpace(item.Pronunciation.UsPhonetic))
                        row["UsPhonetic"] = item.Pronunciation.UsPhonetic;
                }
                if (item.CharacterFeatures != null)
                {
                    row["StrokeCount"] = item.CharacterFeatures.StrokeCount;
                    row["Radical"] = item.CharacterFeatures.Radical;
                    row["Structure"] = item.CharacterFeatures.Structure;
                }
                if (item.WordFeatures != null)
                {
                    row["PartOfSpeech"] = item.WordFeatures.PartOfSpeech;
                    row["WordForms"] = item.WordFeatures.WordForms;
                    row["Collocations"] = item.WordFeatures.Collocations;
                    row["SyllableBreakdown"] = item.WordFeatures.SyllableBreakdown;
                }

                try
                {
                    var props = JsonConvert.DeserializeObject<Dictionary<string, object>>(item.ExtendedProperties);
                    if (props != null)
                    {
                        foreach (var prop in props)
                        {
                            if (table.Columns.Contains(prop.Key))
                                row[prop.Key] = prop.Value?.ToString() ?? "";
                        }
                    }
                }
                catch { }

                table.Rows.Add(row);
            }

            return table;
        }

        /// <summary>
        /// 添加模板事件处理方法，显示当前类别的JSON模板
        /// </summary>
        private void OnTemplateAddClicked(object? sender, EventArgs e)
        {
            if (!CheckAndSaveUnsavedChanges()) return;
            _view.CurrentEditItemJson = GetTemplateJson(_view.SelectedSubCategory);
        }

        /// <summary>
        /// 保存事件处理方法，将JSON内容保存到当前类别
        /// </summary>
        private void OnTemplateSaveClicked(object? sender, EventArgs e)
        {
            var json = _view.CurrentEditItemJson;
            var category = _view.SelectedSubCategory;

            if (string.IsNullOrEmpty(json))
            {
                _view.ShowMessage("请先输入或生成JSON内容！");
                return;
            }

            if (string.IsNullOrEmpty(category))
            {
                _view.ShowMessage("请选择一个类别！");
                return;
            }

            try
            {
                SaveFromJson(json, category);
                _view.ClearEditForm();
                LoadItems();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save items to category {Category}", category);
                _view.ShowMessage($"保存失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 从JSON字符串解析并保存数据项
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <param name="category">目标类别</param>
        private void SaveFromJson(string json, string category)
        {
            var items = ParseJsonToItems(json, category);
            if (items.Count == 0)
            {
                _view.ShowMessage("JSON为空或解析失败！");
                return;
            }
            var itemsOld = _contentLoaderService.LoadItems(category);

            foreach (var newItem in items)
            {
                newItem.Subject = category.StartsWith("English") ? SubjectType.English : SubjectType.Chinese;
                if (Enum.TryParse(category, out SubCategoryType subCategory))
                    newItem.SubCategory = subCategory;

                var newMainContent = newItem.GetMainContent().Trim().ToLower();
                var existingIndex = itemsOld.FindIndex(item =>
                    item.GetMainContent().Trim().ToLower() == newMainContent);

                if (existingIndex >= 0)
                {
                    itemsOld[existingIndex] = newItem;
                    _logger.LogInformation("覆盖重复项: {MainContent}", newMainContent);
                }
                else
                {
                    itemsOld.Add(newItem);
                }
            }

            _contentLoaderService.SaveItems(category, itemsOld);
            _logger.LogInformation("Successfully saved {Count} items to category {Category}", itemsOld.Count, category);
        }

        /// <summary>
        /// 将JSON字符串解析为对象列表
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <param name="category">类别名称，用于确定对象类型</param>
        /// <returns>解析后的对象列表</returns>
        private List<LearningItem> ParseJsonToItems(string json, string category)
        {
            if (!json.TrimStart().StartsWith("[")) json = $"[{json}]";

            try
            {
                var jsonArray = JsonConvert.DeserializeObject<JArray>(json);
                foreach (var obj in jsonArray.OfType<JObject>())
                {
                    var properties = obj.Properties().ToList();
                    foreach (var prop in properties)
                    {
                        var englishName = GetEnglishColumnName(prop.Name, category);
                        if (englishName != prop.Name)
                        {
                            obj[englishName] = prop.Value;
                            obj.Remove(prop.Name);
                        }
                    }
                }
                json = jsonArray.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert Chinese column names to English, proceeding with original JSON");
            }

            return JsonHelper.DeserializeLearningItems(json);
        }

        /// <summary>
        /// 将DataTable转换为对象列表
        /// </summary>
        /// <param name="table">DataTable数据源</param>
        /// <param name="category">类别名称，用于确定对象类型</param>
        /// <returns>转换后的对象列表</returns>
        private List<LearningItem> ConvertDataTableToItems(DataTable table, string category)
        {
            var items = new List<LearningItem>();

            foreach (DataRow row in table.Rows)
            {
                var item = new LearningItem
                {
                    Id = row["Id"]?.ToString() ?? Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.TryParse(row["CreatedAt"]?.ToString(), out var createdAt) ? createdAt : DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Subject = category.StartsWith("English") ? SubjectType.English : SubjectType.Chinese
                };

                if (Enum.TryParse(category, out SubCategoryType subCategory))
                    item.SubCategory = subCategory;

                if (category.StartsWith("English"))
                    item.MainContent = row["Word"]?.ToString() ?? "";
                else if (category.StartsWith("Chinese"))
                {
                    if (category.Contains("Character"))
                        item.MainContent = row["Character"]?.ToString() ?? "";
                    else if (category.Contains("Idiom"))
                        item.MainContent = row["Idiom"]?.ToString() ?? "";
                    else if (category.Contains("Poem"))
                        item.MainContent = row["Title"]?.ToString() ?? "";
                    else
                        item.MainContent = row["Phrase"]?.ToString() ?? "";
                }

                var meaning = row["Meaning"]?.ToString();
                if (!string.IsNullOrWhiteSpace(meaning))
                    item.Meaning = Models.Learning.ValueObjects.Meaning.Create(meaning);

                var example = row["Example"]?.ToString();
                var exampleTranslation = row["ExampleTranslation"]?.ToString();
                if (!string.IsNullOrWhiteSpace(example))
                    item.Example = Models.Learning.ValueObjects.Example.Create(example, exampleTranslation);

                var phonetic = row["Phonetic"]?.ToString();
                var ukPhonetic = row["UkPhonetic"]?.ToString();
                var usPhonetic = row["UsPhonetic"]?.ToString();
                if (!string.IsNullOrWhiteSpace(phonetic))
                    item.Pronunciation = Models.Learning.ValueObjects.Pronunciation.Create(phonetic, ukPhonetic, usPhonetic);

                var strokeCount = row["StrokeCount"]?.ToString();
                var radical = row["Radical"]?.ToString();
                var structure = row["Structure"]?.ToString();
                if (!string.IsNullOrWhiteSpace(strokeCount) || !string.IsNullOrWhiteSpace(radical))
                    item.CharacterFeatures = Models.Learning.ValueObjects.CharacterFeatures.Create(strokeCount, radical, structure);

                var partOfSpeech = row["PartOfSpeech"]?.ToString();
                var wordForms = row["WordForms"]?.ToString();
                var collocations = row["Collocations"]?.ToString();
                var syllableBreakdown = row["SyllableBreakdown"]?.ToString();
                if (!string.IsNullOrWhiteSpace(partOfSpeech) || !string.IsNullOrWhiteSpace(wordForms))
                    item.WordFeatures = Models.Learning.ValueObjects.WordFeatures.Create(partOfSpeech, wordForms, collocations, syllableBreakdown);

                var extendedProps = new Dictionary<string, object>();
                foreach (DataColumn col in table.Columns)
                {
                    var colName = col.ColumnName;
                    if (new[] { "Id", "CreatedAt", "UpdatedAt", "Word", "Character", "Idiom", "Phrase", "Title",
                        "Meaning", "Example", "ExampleTranslation", "Phonetic", "UkPhonetic", "UsPhonetic",
                        "StrokeCount", "Radical", "Structure", "PartOfSpeech", "WordForms", "Collocations",
                        "SyllableBreakdown" }.Contains(colName))
                        continue;

                    var value = row[col]?.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                        extendedProps[colName] = value;
                }
                item.ExtendedProperties = JsonConvert.SerializeObject(extendedProps);

                items.Add(item);
            }

            return items;
        }

        /// <summary>
        /// 尝试将逗号分隔的字符串解析为JSON数组
        /// </summary>
        /// <param name="value">待解析的字符串</param>
        /// <returns>如果解析成功返回JArray，否则返回null</returns>
        private JToken? TryParseAsList(string? value)
        {
            if (string.IsNullOrEmpty(value) || !value.Contains(',')) return null;
            var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(p => p.Trim())
                           .Where(p => !string.IsNullOrEmpty(p))
                           .ToList();
            return parts.Count > 1 ? JArray.FromObject(parts) : null;
        }

        /// <summary>
        /// 删除选中条目事件处理方法
        /// </summary>
        private void OnTemplateDeleteClicked(object? sender, EventArgs e)
        {
            var selectedIndices = _view.SelectedRowIndices;
            var category = _view.SelectedSubCategory;

            if (selectedIndices == null || selectedIndices.Count == 0)
            {
                _view.ShowMessage("请在列表中选择要删除的条目");
                return;
            }

            if (string.IsNullOrEmpty(category))
            {
                _view.ShowMessage("请选择一个类别！");
                return;
            }

            try
            {
                var items = _contentLoaderService.LoadItems(category);
                foreach (var index in selectedIndices.OrderByDescending(i => i).Where(i => i >= 0 && i < items.Count))
                    items.RemoveAt(index);

                _contentLoaderService.SaveItems(category, items);
                _view.ClearEditForm();
                LoadItems();
            }
            catch (Exception ex)
            {
                _view.ShowMessage($"删除失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 网格数据变更事件处理方法
        /// </summary>
        private void OnGridValueChanged(object? sender, EventArgs e)
        {
            _isDirty = true;
            UpdateJsonFromGrid();
        }

        private void OnGridRowsAdded(object? sender, EventArgs e)
        {
            _isDirty = true;
            UpdateJsonFromGrid();
        }

        /// <summary>
        /// 从网格数据更新JSON内容
        /// </summary>
        private void UpdateJsonFromGrid()
        {
            if (_view.GridDataSource is DataTable dataTable)
            {
                var rows = dataTable.Rows.Cast<DataRow>()
                    .Select(row => dataTable.Columns.Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName, col => row[col]?.ToString() ?? ""))
                    .ToList();
                _view.CurrentEditItemJson = JsonConvert.SerializeObject(rows, Formatting.Indented);
            }
        }

        /// <summary>
        /// 导入事件处理方法，从JSON文件导入数据
        /// </summary>
        private void OnImportClicked(object? sender, EventArgs e)
        {
            if (!CheckAndSaveUnsavedChanges()) return;

            using var dialog = new OpenFileDialog
            {
                Filter = "JSON文件 (*.json)|*.json",
                FileName = $"{_view.SelectedSubCategory}_学习内容.json",
                Title = "导入学习内容"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var content = File.ReadAllText(dialog.FileName);
                // 使用安全的自定义 Converter 替代 TypeNameHandling.Auto，防止 RCE 攻击
                var converter = new LearningItemJsonConverter();
                var importedItems = JsonConvert.DeserializeObject<List<LearningItem>>(content, converter);

                if (importedItems?.Count > 0)
                {
                    var existingItems = _contentLoaderService.LoadItems(_view.SelectedSubCategory);

                    foreach (var newItem in importedItems)
                    {
                        var newMainContent = newItem.GetMainContent().Trim().ToLower();
                        var existingIndex = existingItems.FindIndex(item =>
                            item.GetMainContent().Trim().ToLower() == newMainContent);

                        if (existingIndex >= 0)
                        {
                            existingItems[existingIndex] = newItem;
                            _logger.LogInformation("导入时覆盖重复项: {MainContent}", newMainContent);
                        }
                        else
                        {
                            existingItems.Add(newItem);
                        }
                    }

                    _contentLoaderService.SaveItems(_view.SelectedSubCategory, existingItems);
                    LoadItems();
                    _logger.LogInformation("Successfully imported {Count} items from {FilePath}", importedItems.Count, dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import items from {FilePath}", dialog.FileName);
                _view.ShowMessage("导入失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 导出事件处理方法，将数据导出为JSON文件
        /// </summary>
        private void OnExportClicked(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "JSON文件 (*.json)|*.json",
                FileName = $"{_view.SelectedSubCategory}_学习内容_{DateTime.Now:yyyyMMdd}.json",
                Title = "导出学习内容"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var items = _contentLoaderService.LoadItems(_view.SelectedSubCategory);
                // 使用安全的自定义 Converter 替代 TypeNameHandling.Auto
                var converter = new LearningItemListJsonConverter();
                var json = JsonConvert.SerializeObject(items, Formatting.Indented, converter);

                if (!string.IsNullOrEmpty(json))
                {
                    File.WriteAllText(dialog.FileName, json);
                    _view.ShowMessage("导出成功");
                    _logger.LogInformation("Successfully exported {Count} items to {FilePath}", items.Count, dialog.FileName);
                }
                else
                {
                    _view.ShowMessage("没有可导出的内容");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export items to {FilePath}", dialog.FileName);
                _view.ShowMessage("导出失败");
            }
        }

        /// <summary>
        /// 根据HTTP错误信息生成友好的中文错误提示
        /// </summary>
        /// <param name="errorMessage">原始错误信息</param>
        /// <returns>友好的中文错误提示</returns>
        private static string GetFriendlyErrorMessage(string errorMessage)
        {
            return errorMessage.Contains("401") ? "AI服务认证失败，请检查API密钥是否正确！" :
                   errorMessage.Contains("403") ? "AI服务访问被拒绝，请检查API密钥权限！" :
                   errorMessage.Contains("429") ? "AI服务请求过于频繁，请稍后再试！" :
                   errorMessage.Contains("500") || errorMessage.Contains("502") || errorMessage.Contains("503")
                       ? "AI服务暂时不可用，请稍后再试！" :
                       $"AI生成失败：{errorMessage}";
        }

        /// <summary>
        /// 生成AI请求提示词
        /// </summary>
        /// <param name="category">内容类别</param>
        /// <param name="count">生成数量</param>
        /// <param name="range">关键词或范围</param>
        /// <returns>格式化后的AI提示词</returns>
        private string GetAIPrompt(string category, int count, string range)
        {
            var typeName = CategoryTypeNames.GetValueOrDefault(category, "内容");
            var format = JsonFormatHints.GetValueOrDefault(category, "[]");

            if (category == Constants.SubCategory.ChineseComprehensive)
            {
                return $"生成{count}个语文综合练习题（{range}），包含标题、内容、3-5道题目及答案、解析。格式：{format}";
            }
            else if (category == Constants.SubCategory.EnglishComprehensive)
            {
                return $"Generate {count} English exercises ({range}) with title, content, 3-5 questions and answers, analysis. Format: {format}";
            }

            return $"生成{count}个{range}的{typeName}。格式：{format}";
        }

        /// <summary>
        /// 获取指定类别的JSON模板
        /// </summary>
        /// <param name="category">类别名称</param>
        /// <returns>JSON格式的模板字符串</returns>
        private static string GetTemplateJson(string category)
        {
            return CategoryTemplates.TryGetValue(category, out var template)
                ? JsonConvert.SerializeObject(template, Formatting.Indented)
                : "{}";
        }

        /// <summary>
        /// 清理AI返回的JSON结果，处理换行符和特殊字符
        /// </summary>
        /// <param name="result">AI返回的原始字符串</param>
        /// <returns>清理后的JSON字符串</returns>
        private static string CleanJsonResult(string result)
        {
            var startIndex = result.IndexOf('[');
            var endIndex = result.LastIndexOf(']');

            if (startIndex < 0 || endIndex < startIndex)
            {
                return result;
            }

            var jsonContent = result.Substring(startIndex, endIndex - startIndex + 1);

            jsonContent = jsonContent.Replace("\r\n", "\\n")
                                     .Replace("\r", "\\n")
                                     .Replace("\n", "\\n")
                                     .Replace("\"", "\\\"")
                                     .Replace("\t", "\\t");

            return jsonContent;
        }

        /// <summary>
        /// 检查并保存未保存的更改
        /// </summary>
        /// <returns>如果允许继续操作返回true，否则返回false</returns>
        private bool CheckAndSaveUnsavedChanges()
        {
            if (!_isDirty) return true;
            OnTemplateSaveClicked(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// 释放Presenter资源，在窗口关闭时调用
        /// </summary>
        public void Dispose()
        {
            OnTemplateSaveClicked(this, EventArgs.Empty);

            _view.SubjectChanged -= OnSubjectChanged;
            _view.LanguageChanged -= OnLanguageChanged;
            _view.SubCategoryChanged -= OnSubCategoryChanged;
            _view.TemplateAddClicked -= OnTemplateAddClicked;
            _view.TemplateSaveClicked -= OnTemplateSaveClicked;
            _view.TemplateDeleteClicked -= OnTemplateDeleteClicked;
            _view.ImportClicked -= OnImportClicked;
            _view.ExportClicked -= OnExportClicked;
            _view.GridCellEndEdit -= OnGridValueChanged;
            _view.GridRowsAdded -= OnGridRowsAdded;

            _logger.LogInformation("ContentEditorPresenter disposed");
        }
    }
}
