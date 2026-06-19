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
                { { "Character", "汉字" }, { "Pinyin", "拼音" }, { "Meaning", "释义" }, { "StrokeCount", "笔画数" }, { "Radical", "部首" }, { "StrokeOrder", "笔顺" }, { "Words", "组词" } } },
            { Constants.SubCategory.ChineseIdiom, new Dictionary<string, string>
                { { "Idiom", "成语" }, { "Pinyin", "拼音" }, { "Meaning", "释义" }, { "Origin", "出处" }, { "Example", "例句" } } },
            { Constants.SubCategory.ChinesePhrase, new Dictionary<string, string>
                { { "Phrase", "短语" }, { "Pinyin", "拼音" }, { "Meaning", "释义" }, { "Example", "例句" } } },
            { Constants.SubCategory.ChinesePoem, new Dictionary<string, string>
                { { "Title", "诗名" }, { "Author", "作者" }, { "Dynasty", "朝代" }, { "Verses", "诗句" }, { "Annotation", "注释" } } },
            { Constants.SubCategory.ChineseComprehensive, new Dictionary<string, string>
                { { "Title", "课文标题" }, { "Content", "课文内容" }, { "Questions", "课后习题" }, { "Question", "题目" }, { "Answer", "答案" }, { "Analysis", "解析" } } },
            { Constants.SubCategory.EnglishWord, new Dictionary<string, string>
                { { "Word", "单词" }, { "Phonetic", "音标" }, { "PartOfSpeech", "词性" }, { "SyllableBreakdown", "音节拼读" }, { "Meaning", "中文释义" }, { "Example", "例句" } } },
            { Constants.SubCategory.EnglishPhrase, new Dictionary<string, string>
                { { "Phrase", "短语" }, { "Meaning", "中文释义" }, { "Example", "例句" } } },
            { Constants.SubCategory.EnglishSentence, new Dictionary<string, string>
                { { "Sentence", "句子" }, { "Translation", "中文翻译" }, { "Grammar", "语法点" } } },
            { Constants.SubCategory.EnglishComprehensive, new Dictionary<string, string>
                { { "Title", "文章标题" }, { "Content", "文章内容" }, { "Questions", "阅读理解题" }, { "Question", "题目" }, { "Answer", "答案" }, { "Analysis", "解析" } } },
            { Constants.SubCategory.MathFormula, new Dictionary<string, string>
                { { "Name", "公式名称" }, { "Formula", "公式表达式" }, { "Description", "公式说明" }, { "Conditions", "适用条件" }, { "Example", "应用举例" }, { "Difficulty", "难度等级" }, { "Tags", "标签" } } },
            { Constants.SubCategory.MathExample, new Dictionary<string, string>
                { { "Title", "例题标题" }, { "Problem", "题目描述" }, { "Solution", "解答过程" }, { "KeySteps", "关键步骤" }, { "Analysis", "方法总结" }, { "Difficulty", "难度等级" }, { "Tags", "标签" } } },
            { Constants.SubCategory.MathConcept, new Dictionary<string, string>
                { { "Name", "概念名称" }, { "Definition", "定义" }, { "Properties", "性质" }, { "Example", "举例说明" }, { "Notes", "注意事项" }, { "Difficulty", "难度等级" }, { "Tags", "标签" } } },
            { Constants.SubCategory.MathComprehensive, new Dictionary<string, string>
                { { "Title", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" }, { "Example", "典型例题" }, { "Explanation", "答案解析" }, { "Difficulty", "难度等级" }, { "Tags", "标签" } } },
            { Constants.SubCategory.PhysicsLaw, new Dictionary<string, string>
                { { "Name", "定律名称" }, { "Statement", "定律内容" }, { "Formula", "公式" }, { "Conditions", "适用条件" }, { "Application", "应用场景" }, { "Difficulty", "难度等级" }, { "Tags", "标签" } } },
            { Constants.SubCategory.PhysicsExperiment, new Dictionary<string, string>
                { { "Name", "实验名称" }, { "Purpose", "实验目的" }, { "Equipment", "实验器材" }, { "Procedure", "实验步骤" }, { "Conclusion", "实验结论" }, { "Difficulty", "难度等级" }, { "Tags", "标签" } } },
            { Constants.SubCategory.PhysicsDerivation, new Dictionary<string, string>
                { { "Name", "公式名称" }, { "Formula", "推导结果" }, { "DerivationSteps", "推导步骤" }, { "Conditions", "前提条件" }, { "Example", "应用实例" }, { "Difficulty", "难度等级" }, { "Tags", "标签" } } },
            { Constants.SubCategory.PhysicsComprehensive, new Dictionary<string, string>
                { { "Title", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" }, { "Example", "典型例题" }, { "Explanation", "答案解析" }, { "Difficulty", "难度等级" }, { "Tags", "标签" } } },
            { Constants.SubCategory.ChemistryEquation, new Dictionary<string, string>
                { { "Name", "反应名称" }, { "Reactants", "反应物" }, { "Products", "生成物" }, { "Equation", "化学方程式" }, { "Conditions", "反应条件" }, { "Phenomenon", "反应现象" }, { "Tags", "标签" } } },
            { Constants.SubCategory.ChemistryElement, new Dictionary<string, string>
                { { "Name", "元素名称" }, { "Symbol", "元素符号" }, { "AtomicNumber", "原子序数" }, { "Properties", "元素性质" }, { "Uses", "主要用途" }, { "Difficulty", "难度等级" }, { "Tags", "标签" } } },
            { Constants.SubCategory.ChemistryExperiment, new Dictionary<string, string>
                { { "Name", "实验名称" }, { "Purpose", "实验目的" }, { "Equipment", "实验器材" }, { "Procedure", "操作步骤" }, { "Phenomenon", "实验现象" }, { "Conclusion", "实验结论" }, { "Tags", "标签" } } },
            { Constants.SubCategory.ChemistryComprehensive, new Dictionary<string, string>
                { { "Title", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" }, { "Example", "典型例题" }, { "Explanation", "答案解析" }, { "Difficulty", "难度等级" }, { "Tags", "标签" } } },
            { Constants.SubCategory.HistoryEvent, new Dictionary<string, string>
                { { "Name", "事件名称" }, { "Time", "发生时间" }, { "Location", "发生地点" }, { "Background", "历史背景" }, { "Process", "事件经过" }, { "Impact", "历史影响" }, { "Tags", "标签" } } },
            { Constants.SubCategory.HistoryPerson, new Dictionary<string, string>
                { { "Name", "人物姓名" }, { "Dynasty", "所处朝代" }, { "Lifetime", "生卒年月" }, { "Achievements", "主要成就" }, { "Evaluation", "历史评价" }, { "Works", "代表作品" }, { "Tags", "标签" } } },
            { Constants.SubCategory.HistoryTimeline, new Dictionary<string, string>
                { { "Era", "时代名称" }, { "TimePeriod", "时间范围" }, { "KeyEvents", "重要事件" }, { "Characteristics", "时代特征" }, { "ImportantFigures", "重要人物" }, { "Notes", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.HistoryComprehensive, new Dictionary<string, string>
                { { "Title", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" }, { "Example", "典型例题" }, { "Explanation", "答案解析" }, { "Difficulty", "难度等级" }, { "Tags", "标签" } } },
            { Constants.SubCategory.GeographyKnowledge, new Dictionary<string, string>
                { { "Name", "地理名称" }, { "Category", "地理分类" }, { "Description", "地理描述" }, { "Distribution", "分布地区" }, { "Features", "主要特征" }, { "Notes", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.GeographyMap, new Dictionary<string, string>
                { { "Name", "地图名称" }, { "Region", "所属地区" }, { "Features", "地理特征" }, { "KeyLocations", "重要地点" }, { "ReadingTips", "读图技巧" }, { "Notes", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.GeographyClimate, new Dictionary<string, string>
                { { "Type", "气候类型" }, { "Distribution", "分布地区" }, { "Characteristics", "气候特征" }, { "Causes", "形成原因" }, { "Vegetation", "植被类型" }, { "Tags", "标签" } } },
            { Constants.SubCategory.GeographyComprehensive, new Dictionary<string, string>
                { { "Title", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" }, { "Example", "典型例题" }, { "Explanation", "答案解析" }, { "Difficulty", "难度等级" }, { "Tags", "标签" } } },
            { Constants.SubCategory.BiologyConcept, new Dictionary<string, string>
                { { "Name", "概念名称" }, { "Definition", "定义" }, { "Classification", "分类" }, { "Features", "主要特征" }, { "Function", "功能作用" }, { "Example", "实例" }, { "Tags", "标签" } } },
            { Constants.SubCategory.BiologyExperiment, new Dictionary<string, string>
                { { "Name", "实验名称" }, { "Purpose", "实验目的" }, { "Materials", "实验材料" }, { "Steps", "实验步骤" }, { "Result", "实验结果" }, { "Conclusion", "实验结论" }, { "Tags", "标签" } } },
            { Constants.SubCategory.BiologyPhenomenon, new Dictionary<string, string>
                { { "Name", "现象名称" }, { "Description", "现象描述" }, { "Type", "现象类型" }, { "Causes", "产生原因" }, { "Examples", "常见实例" }, { "Significance", "生物意义" }, { "Tags", "标签" } } },
            { Constants.SubCategory.BiologyComprehensive, new Dictionary<string, string>
                { { "Title", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" }, { "Example", "典型例题" }, { "Explanation", "答案解析" }, { "Difficulty", "难度等级" }, { "Tags", "标签" } } }
        };

        /// <summary>
        /// 类别模板字典，定义每个类别对应的字段结构
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, object>> CategoryTemplates = new()
        {
            {
                Constants.SubCategory.ChineseCharacter, new Dictionary<string, object>
                {
                    { "Character", "" }, { "Pinyin", "" }, { "Meaning", "" }, { "StrokeCount", "" }, { "Radical", "" }, { "StrokeOrder", "" }, { "Words", new List<string> { "", "", "", "", "" } }
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
                    { "Title", "" }, { "Author", "" }, { "Dynasty", "" }, { "Verses", new List<string> { "", "", "", "" } }, { "Annotation", "" }
                }
            },
            {
                Constants.SubCategory.EnglishWord, new Dictionary<string, object>
                {
                    { "Word", "" }, { "Phonetic", "" }, { "PartOfSpeech", "" }, { "SyllableBreakdown", "" }, { "Meaning", "" }, { "Example", "" }
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
                    { "Name", "" }, { "Formula", "" }, { "Description", "" }, { "Conditions", "" }, { "Example", "" }, { "Difficulty", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.MathExample, new Dictionary<string, object>
                {
                    { "Title", "" }, { "Problem", "" }, { "Solution", "" }, { "KeySteps", new List<string> { "", "", "" } }, { "Analysis", "" }, { "Difficulty", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.MathConcept, new Dictionary<string, object>
                {
                    { "Name", "" }, { "Definition", "" }, { "Properties", new List<string> { "", "", "" } }, { "Example", "" }, { "Notes", "" }, { "Difficulty", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.MathComprehensive, new Dictionary<string, object>
                {
                    { "Title", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Explanation", "" }, { "Difficulty", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.PhysicsLaw, new Dictionary<string, object>
                {
                    { "Name", "" }, { "Statement", "" }, { "Formula", "" }, { "Conditions", "" }, { "Application", "" }, { "Difficulty", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.PhysicsExperiment, new Dictionary<string, object>
                {
                    { "Name", "" }, { "Purpose", "" }, { "Equipment", new List<string> { "", "", "" } }, { "Procedure", new List<string> { "", "", "" } }, { "Conclusion", "" }, { "Difficulty", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.PhysicsDerivation, new Dictionary<string, object>
                {
                    { "Name", "" }, { "Formula", "" }, { "DerivationSteps", new List<string> { "", "", "" } }, { "Conditions", "" }, { "Example", "" }, { "Difficulty", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.PhysicsComprehensive, new Dictionary<string, object>
                {
                    { "Title", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Explanation", "" }, { "Difficulty", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.ChemistryEquation, new Dictionary<string, object>
                {
                    { "Name", "" }, { "Reactants", "" }, { "Products", "" }, { "Equation", "" }, { "Conditions", "" }, { "Phenomenon", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.ChemistryElement, new Dictionary<string, object>
                {
                    { "Name", "" }, { "Symbol", "" }, { "AtomicNumber", "" }, { "Properties", new List<string> { "", "", "" } }, { "Uses", "" }, { "Difficulty", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.ChemistryExperiment, new Dictionary<string, object>
                {
                    { "Name", "" }, { "Purpose", "" }, { "Equipment", new List<string> { "", "", "" } }, { "Procedure", new List<string> { "", "", "" } }, { "Phenomenon", "" }, { "Conclusion", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.ChemistryComprehensive, new Dictionary<string, object>
                {
                    { "Title", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Explanation", "" }, { "Difficulty", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.HistoryEvent, new Dictionary<string, object>
                {
                    { "Name", "" }, { "Time", "" }, { "Location", "" }, { "Background", "" }, { "Process", "" }, { "Impact", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.HistoryPerson, new Dictionary<string, object>
                {
                    { "Name", "" }, { "Dynasty", "" }, { "Lifetime", "" }, { "Achievements", new List<string> { "", "", "" } }, { "Evaluation", "" }, { "Works", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.HistoryTimeline, new Dictionary<string, object>
                {
                    { "Era", "" }, { "TimePeriod", "" }, { "KeyEvents", new List<string> { "", "", "" } }, { "Characteristics", "" }, { "ImportantFigures", "" }, { "Notes", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.HistoryComprehensive, new Dictionary<string, object>
                {
                    { "Title", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Explanation", "" }, { "Difficulty", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.GeographyKnowledge, new Dictionary<string, object>
                {
                    { "Name", "" }, { "Category", "" }, { "Description", "" }, { "Distribution", "" }, { "Features", new List<string> { "", "", "" } }, { "Notes", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.GeographyMap, new Dictionary<string, object>
                {
                    { "Name", "" }, { "Region", "" }, { "Features", new List<string> { "", "", "" } }, { "KeyLocations", "" }, { "ReadingTips", "" }, { "Notes", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.GeographyClimate, new Dictionary<string, object>
                {
                    { "Type", "" }, { "Distribution", "" }, { "Characteristics", new List<string> { "", "", "" } }, { "Causes", "" }, { "Vegetation", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.GeographyComprehensive, new Dictionary<string, object>
                {
                    { "Title", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Explanation", "" }, { "Difficulty", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.BiologyConcept, new Dictionary<string, object>
                {
                    { "Name", "" }, { "Definition", "" }, { "Classification", "" }, { "Features", new List<string> { "", "", "" } }, { "Function", "" }, { "Example", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.BiologyExperiment, new Dictionary<string, object>
                {
                    { "Name", "" }, { "Purpose", "" }, { "Materials", new List<string> { "", "", "" } }, { "Steps", new List<string> { "", "", "" } }, { "Result", "" }, { "Conclusion", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.BiologyPhenomenon, new Dictionary<string, object>
                {
                    { "Name", "" }, { "Description", "" }, { "Type", "" }, { "Causes", new List<string> { "", "", "" } }, { "Examples", "" }, { "Significance", "" }, { "Tags", new List<string> { "", "", "" } }
                }
            },
            {
                Constants.SubCategory.BiologyComprehensive, new Dictionary<string, object>
                {
                    { "Title", "" }, { "Content", "" }, { "KeyPoints", new List<string> { "", "", "" } }, { "Example", "" }, { "Explanation", "" }, { "Difficulty", "" }, { "Tags", new List<string> { "", "", "" } }
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

            _view.SubjectChanged += (_, _) => OnSubjectChanged();
            _view.LanguageChanged += (_, _) => OnLanguageChanged();
            _view.SubCategoryChanged += (_, _) => OnSubCategoryChanged();
            _view.TemplateAddClicked += (_, _) => OnTemplateAddClicked();
            _view.TemplateSaveClicked += (_, _) => OnTemplateSaveClicked();
            _view.TemplateDeleteClicked += (_, _) => OnTemplateDeleteClicked();
            _view.ImportClicked += (_, _) => OnImportClicked();
            _view.ExportClicked += (_, _) => OnExportClicked();
            _view.GridCellEndEdit += (_, _) => OnGridValueChanged();
            _view.GridRowsAdded += (_, _) => OnGridValueChanged();

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
        private void OnSubjectChanged()
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
        private void OnLanguageChanged()
        {
            // 学科变更事件已经处理了，这里不做额外处理
        }

        /// <summary>
        /// 子类别切换事件处理方法
        /// </summary>
        private void OnSubCategoryChanged()
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
        private static string GetChineseColumnName(string columnName, string category)
        {
            if (!string.IsNullOrEmpty(category) &&
                CategoryColumnHeaders.TryGetValue(category, out var headers) &&
                headers.TryGetValue(columnName, out var chineseName))
            {
                return chineseName;
            }
            return columnName;
        }

        /// <summary>
        /// 将对象列表转换为DataTable，所有列均为string类型以避免类型推断问题
        /// </summary>
        /// <param name="items">对象列表</param>
        /// <param name="category">类别名称</param>
        /// <returns>转换后的DataTable</returns>
        private DataTable ConvertToDataTable(List<object> items, string category)
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

            var properties = items[0].GetType().GetProperties();
            foreach (var prop in properties)
            {
                var column = table.Columns.Add(prop.Name, typeof(string));
                column.Caption = GetChineseColumnName(prop.Name, category);
            }

            foreach (var item in items)
            {
                var row = table.NewRow();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(item);
                    row[prop.Name] = value switch
                    {
                        List<string> list => list.Count > 0 ? string.Join(", ", list) : "",
                        null => "",
                        _ => value.ToString() ?? ""
                    };
                }
                table.Rows.Add(row);
            }

            return table;
        }

        /// <summary>
        /// 添加模板事件处理方法，显示当前类别的JSON模板
        /// </summary>
        private void OnTemplateAddClicked()
        {
            if (!CheckAndSaveUnsavedChanges()) return;
            _view.CurrentEditItemJson = GetTemplateJson(_view.SelectedSubCategory);
        }

        /// <summary>
        /// 保存事件处理方法，将JSON内容保存到当前类别
        /// </summary>
        private void OnTemplateSaveClicked()
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
                if (newItem is LearningItem newLearningItem)
                {
                    var newMainContent = newLearningItem.GetMainContent().Trim().ToLower();
                    var existingIndex = itemsOld.FindIndex(item =>
                        item is LearningItem existingItem &&
                        existingItem.GetMainContent().Trim().ToLower() == newMainContent);

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
        private List<object> ParseJsonToItems(string json, string category)
        {
            var items = new List<object>();
            var itemType = _contentLoaderService.GetItemType(category);

            if (!json.TrimStart().StartsWith("[")) json = $"[{json}]";

            var listType = typeof(List<>).MakeGenericType(itemType);
            var data = System.Text.Json.JsonSerializer.Deserialize(json, listType,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            var importedItems = ((System.Collections.IList)data).Cast<object>().ToList();
            foreach (var item in importedItems)
            {
                items.Add(item);
            }

            return items;
        }

        /// <summary>
        /// 将DataTable转换为对象列表
        /// </summary>
        /// <param name="table">DataTable数据源</param>
        /// <param name="category">类别名称，用于确定对象类型</param>
        /// <returns>转换后的对象列表</returns>
        private List<object> ConvertDataTableToItems(DataTable table, string category)
        {
            var itemType = _contentLoaderService.GetItemType(category);
            return table.Rows.Cast<DataRow>().Select(row =>
            {
                var jsonObj = new JObject();
                foreach (DataColumn col in table.Columns)
                {
                    var value = row[col]?.ToString();
                    jsonObj[col.ColumnName] = TryParseAsList(value) ?? value ?? "";
                }
                return jsonObj.ToObject(itemType);
            }).Where(item => item != null).Cast<object>().ToList();
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
        private void OnTemplateDeleteClicked()
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
        private void OnGridValueChanged()
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
        private void OnImportClicked()
        {
            if (!CheckAndSaveUnsavedChanges()) return;

            using var dialog = new OpenFileDialog { Filter = "JSON文件 (*.json)|*.json" };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var content = File.ReadAllText(dialog.FileName);
                var importedItems = JsonConvert.DeserializeObject<List<object>>(content,
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

                if (importedItems?.Count > 0)
                {
                    var existingItems = _contentLoaderService.LoadItems(_view.SelectedSubCategory);

                    foreach (var newItem in importedItems)
                    {
                        if (newItem is LearningItem newLearningItem)
                        {
                            var newMainContent = newLearningItem.GetMainContent().Trim().ToLower();
                            var existingIndex = existingItems.FindIndex(item =>
                                item is LearningItem existingItem &&
                                existingItem.GetMainContent().Trim().ToLower() == newMainContent);

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
        private void OnExportClicked()
        {
            using var dialog = new SaveFileDialog { Filter = "JSON文件 (*.json)|*.json" };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var items = _contentLoaderService.LoadItems(_view.SelectedSubCategory);
                var json = JsonConvert.SerializeObject(items, Formatting.Indented,
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

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
            OnTemplateSaveClicked();
            return true;
        }

        /// <summary>
        /// 释放Presenter资源，在窗口关闭时调用
        /// </summary>
        public void Dispose()
        {

            OnTemplateSaveClicked();


            _view.SubjectChanged -= (_, _) => OnSubjectChanged();
            _view.LanguageChanged -= (_, _) => OnLanguageChanged();
            _view.SubCategoryChanged -= (_, _) => OnSubCategoryChanged();
            _view.TemplateAddClicked -= (_, _) => OnTemplateAddClicked();
            _view.TemplateSaveClicked -= (_, _) => OnTemplateSaveClicked();
            _view.TemplateDeleteClicked -= (_, _) => OnTemplateDeleteClicked();
            _view.ImportClicked -= (_, _) => OnImportClicked();
            _view.ExportClicked -= (_, _) => OnExportClicked();
            _view.GridCellEndEdit -= (_, _) => OnGridValueChanged();
            _view.GridRowsAdded -= (_, _) => OnGridValueChanged();

            _logger.LogInformation("ContentEditorPresenter disposed");
        }
    }
}
