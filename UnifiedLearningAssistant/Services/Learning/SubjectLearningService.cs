using System;
using System.Collections.Generic;
using System.Linq;

namespace UnifiedLearningAssistant.Services.Learning
{
    /// <summary>
    /// 学科学习服务
    /// 提供数学、科学、历史等学科的学习内容
    /// </summary>
    public class SubjectLearningService
    {
        private readonly List<MathProblem> _mathProblems;
        private readonly List<ScienceCard> _scienceCards;
        private readonly List<HistoricalEvent> _historicalEvents;

        public SubjectLearningService()
        {
            _mathProblems = LoadMathProblems();
            _scienceCards = LoadScienceCards();
            _historicalEvents = LoadHistoricalEvents();
        }

        #region 数学学习

        public MathProblem GetRandomMathProblem(MathTopic topic = MathTopic.Algebra)
        {
            var topicProblems = _mathProblems.Where(p => p.Topic == topic).ToList();
            if (topicProblems.Count == 0)
            {
                topicProblems = _mathProblems.ToList();
            }
            
            var random = new Random();
            return topicProblems[random.Next(topicProblems.Count)];
        }

        public List<MathProblem> GetMathProblemsByTopic(MathTopic topic, int count = 5)
        {
            var topicProblems = _mathProblems.Where(p => p.Topic == topic).ToList();
            return topicProblems.OrderBy(_ => new Random().Next()).Take(count).ToList();
        }

        public bool CheckMathAnswer(MathProblem problem, string userAnswer)
        {
            return string.Equals(problem.CorrectAnswer, userAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region 科学知识

        public ScienceCard GetRandomScienceCard(ScienceCategory category = ScienceCategory.Biology)
        {
            var categoryCards = _scienceCards.Where(c => c.Category == category).ToList();
            if (categoryCards.Count == 0)
            {
                categoryCards = _scienceCards.ToList();
            }
            
            var random = new Random();
            return categoryCards[random.Next(categoryCards.Count)];
        }

        public List<ScienceCard> GetScienceCardsByCategory(ScienceCategory category, int count = 5)
        {
            var categoryCards = _scienceCards.Where(c => c.Category == category).ToList();
            return categoryCards.OrderBy(_ => new Random().Next()).Take(count).ToList();
        }

        #endregion

        #region 历史事件

        public List<HistoricalEvent> GetHistoricalEvents(int count = 10)
        {
            return _historicalEvents.OrderBy(e => e.Year).Take(count).ToList();
        }

        public List<HistoricalEvent> GetHistoricalEventsByEra(HistoricalEra era)
        {
            return _historicalEvents.Where(e => e.Era == era).OrderBy(e => e.Year).ToList();
        }

        public List<HistoricalEvent> GetEventsInRange(int startYear, int endYear)
        {
            return _historicalEvents
                .Where(e => e.Year >= startYear && e.Year <= endYear)
                .OrderBy(e => e.Year)
                .ToList();
        }

        #endregion

        #region 数据加载

        private List<MathProblem> LoadMathProblems()
        {
            return new List<MathProblem>
            {
                // 代数
                new MathProblem("解方程：2x + 5 = 15", "x = 5", MathTopic.Algebra),
                new MathProblem("解方程：3(x - 2) = 12", "x = 6", MathTopic.Algebra),
                new MathProblem("化简：2x + 3x - 5x", "0", MathTopic.Algebra),
                new MathProblem("计算：(x + 2)(x - 3)", "x² - x - 6", MathTopic.Algebra),
                new MathProblem("解方程：x² - 4 = 0", "x = 2 或 x = -2", MathTopic.Algebra),
                
                // 几何
                new MathProblem("计算半径为5的圆的面积", "25π", MathTopic.Geometry),
                new MathProblem("计算边长为4的正方形的周长", "16", MathTopic.Geometry),
                new MathProblem("直角三角形两直角边分别为3和4，求斜边", "5", MathTopic.Geometry),
                new MathProblem("计算长5宽3的矩形面积", "15", MathTopic.Geometry),
                new MathProblem("计算直径为10的圆的周长", "10π", MathTopic.Geometry),
                
                // 概率统计
                new MathProblem("抛一枚硬币，正面朝上的概率", "1/2", MathTopic.Probability),
                new MathProblem("从1-10中随机选一个数，选到偶数的概率", "1/2", MathTopic.Probability),
                new MathProblem("数据：2, 4, 6, 8, 10 的平均数", "6", MathTopic.Probability),
                new MathProblem("数据：3, 5, 7 的中位数", "5", MathTopic.Probability),
                new MathProblem("掷骰子两次，两次都是6的概率", "1/36", MathTopic.Probability),
                
                // 函数
                new MathProblem("求函数 f(x) = 2x + 1 在 x=3 时的值", "7", MathTopic.Functions),
                new MathProblem("求函数 y = x² 在 x=-2 时的值", "4", MathTopic.Functions),
                new MathProblem("求函数 f(x) = x + 2 的斜率", "1", MathTopic.Functions),
                new MathProblem("函数 y = 3x - 6 与x轴的交点", "(2, 0)", MathTopic.Functions),
                new MathProblem("求函数 f(x) = |x| 在 x=-3 时的值", "3", MathTopic.Functions),
                
                // 微积分入门
                new MathProblem("求 f(x) = x² 的导数", "2x", MathTopic.Calculus),
                new MathProblem("求 f(x) = 3x 的导数", "3", MathTopic.Calculus),
                new MathProblem("求 f(x) = 5 的导数", "0", MathTopic.Calculus),
                new MathProblem("求 f(x) = x³ 的导数", "3x²", MathTopic.Calculus),
                new MathProblem("求 f(x) = 2x² + 3x 的导数", "4x + 3", MathTopic.Calculus)
            };
        }

        private List<ScienceCard> LoadScienceCards()
        {
            return new List<ScienceCard>
            {
                // 生物学
                new ScienceCard(
                    "细胞",
                    "细胞是生物体结构和功能的基本单位。所有生物都是由细胞构成的。",
                    "细胞膜、细胞质、细胞核",
                    ScienceCategory.Biology),
                new ScienceCard(
                    "光合作用",
                    "光合作用是植物利用光能将二氧化碳和水转化为葡萄糖和氧气的过程。",
                    "6CO₂ + 6H₂O + 光能 → C₆H₁₂O₆ + 6O₂",
                    ScienceCategory.Biology),
                new ScienceCard(
                    "DNA",
                    "DNA是脱氧核糖核酸的缩写，携带生物体的遗传信息。",
                    "双螺旋结构、基因、染色体",
                    ScienceCategory.Biology),
                new ScienceCard(
                    "进化论",
                    "进化论认为物种通过自然选择逐渐演变。",
                    "适者生存、自然选择、共同祖先",
                    ScienceCategory.Biology),
                new ScienceCard(
                    "食物链",
                    "食物链描述了生态系统中生物之间的食物关系。",
                    "生产者、消费者、分解者",
                    ScienceCategory.Biology),
                
                // 化学
                new ScienceCard(
                    "原子结构",
                    "原子由原子核和电子组成，原子核包含质子和中子。",
                    "质子(+)、中子(0)、电子(-)",
                    ScienceCategory.Chemistry),
                new ScienceCard(
                    "元素周期表",
                    "元素周期表按原子序数排列所有已知元素。",
                    "周期、族、原子序数",
                    ScienceCategory.Chemistry),
                new ScienceCard(
                    "化学反应",
                    "化学反应是物质发生变化形成新物质的过程。",
                    "反应物、生成物、催化剂",
                    ScienceCategory.Chemistry),
                new ScienceCard(
                    "化学键",
                    "化学键是原子之间的吸引力，形成分子。",
                    "离子键、共价键、金属键",
                    ScienceCategory.Chemistry),
                new ScienceCard(
                    "pH值",
                    "pH值表示溶液的酸碱度，范围从0到14。",
                    "酸性(<7)、碱性(>7)、中性(=7)",
                    ScienceCategory.Chemistry),
                
                // 物理
                new ScienceCard(
                    "牛顿定律",
                    "牛顿运动定律描述物体的运动规律。",
                    "惯性、加速度、作用力与反作用力",
                    ScienceCategory.Physics),
                new ScienceCard(
                    "能量守恒",
                    "能量既不能被创造也不能被消灭，只能转换形式。",
                    "动能、势能、热能",
                    ScienceCategory.Physics),
                new ScienceCard(
                    "万有引力",
                    "万有引力是物体之间相互吸引的力。",
                    "重力、质量、距离",
                    ScienceCategory.Physics),
                new ScienceCard(
                    "光的折射",
                    "光从一种介质进入另一种介质时会改变方向。",
                    "折射角、折射率、全反射",
                    ScienceCategory.Physics),
                new ScienceCard(
                    "电流",
                    "电流是电荷的流动。",
                    "电压、电阻、欧姆定律",
                    ScienceCategory.Physics),
                
                // 天文学
                new ScienceCard(
                    "太阳系",
                    "太阳系由太阳和围绕它运行的行星组成。",
                    "太阳、行星、卫星",
                    ScienceCategory.Astronomy),
                new ScienceCard(
                    "黑洞",
                    "黑洞是引力极强的天体，连光都无法逃脱。",
                    "事件视界、奇点、引力",
                    ScienceCategory.Astronomy),
                new ScienceCard(
                    "恒星",
                    "恒星是由炽热气体组成的发光天体。",
                    "核聚变、主序星、超新星",
                    ScienceCategory.Astronomy),
                new ScienceCard(
                    "星系",
                    "星系是由恒星、气体和暗物质组成的巨大系统。",
                    "银河系、仙女座星系、螺旋星系",
                    ScienceCategory.Astronomy),
                new ScienceCard(
                    "宇宙大爆炸",
                    "宇宙大爆炸是宇宙起源的理论。",
                    "奇点、膨胀、微波背景辐射",
                    ScienceCategory.Astronomy)
            };
        }

        private List<HistoricalEvent> LoadHistoricalEvents()
        {
            return new List<HistoricalEvent>
            {
                // 古代
                new HistoricalEvent(-221, "秦始皇统一中国", "中国", HistoricalEra.Ancient),
                new HistoricalEvent(-776, "第一届古代奥运会", "希腊", HistoricalEra.Ancient),
                new HistoricalEvent(-476, "西罗马帝国灭亡", "罗马", HistoricalEra.Ancient),
                new HistoricalEvent(551, "孔子诞生", "中国", HistoricalEra.Ancient),
                new HistoricalEvent(332, "亚历山大大帝征服埃及", "埃及", HistoricalEra.Ancient),
                
                // 中世纪
                new HistoricalEvent(1096, "第一次十字军东征", "欧洲", HistoricalEra.Medieval),
                new HistoricalEvent(1271, "马可·波罗开始中国之旅", "意大利/中国", HistoricalEra.Medieval),
                new HistoricalEvent(1453, "君士坦丁堡陷落", "拜占庭", HistoricalEra.Medieval),
                new HistoricalEvent(1348, "黑死病爆发", "欧洲", HistoricalEra.Medieval),
                new HistoricalEvent(1100, "活字印刷术发明", "中国", HistoricalEra.Medieval),
                
                // 近代
                new HistoricalEvent(1492, "哥伦布发现美洲", "西班牙", HistoricalEra.EarlyModern),
                new HistoricalEvent(1687, "牛顿发表《自然哲学的数学原理》", "英国", HistoricalEra.EarlyModern),
                new HistoricalEvent(1776, "美国独立宣言", "美国", HistoricalEra.EarlyModern),
                new HistoricalEvent(1789, "法国大革命", "法国", HistoricalEra.EarlyModern),
                new HistoricalEvent(1804, "拿破仑加冕为皇帝", "法国", HistoricalEra.EarlyModern),
                
                // 现代
                new HistoricalEvent(1903, "莱特兄弟首次飞行", "美国", HistoricalEra.Modern),
                new HistoricalEvent(1914, "第一次世界大战爆发", "全球", HistoricalEra.Modern),
                new HistoricalEvent(1929, "华尔街股市崩盘", "美国", HistoricalEra.Modern),
                new HistoricalEvent(1939, "第二次世界大战爆发", "全球", HistoricalEra.Modern),
                new HistoricalEvent(1945, "第二次世界大战结束", "全球", HistoricalEra.Modern),
                new HistoricalEvent(1953, "DNA双螺旋结构发现", "英国", HistoricalEra.Modern),
                new HistoricalEvent(1969, "阿波罗11号登月", "美国", HistoricalEra.Modern),
                new HistoricalEvent(1989, "柏林墙倒塌", "德国", HistoricalEra.Modern),
                new HistoricalEvent(2001, "中国加入世界贸易组织", "中国", HistoricalEra.Modern),
                new HistoricalEvent(2020, "COVID-19疫情全球爆发", "全球", HistoricalEra.Modern)
            };
        }

        #endregion
    }

    #region 数据模型

    public class MathProblem
    {
        public string Question { get; }
        public string CorrectAnswer { get; }
        public MathTopic Topic { get; }

        public MathProblem(string question, string correctAnswer, MathTopic topic)
        {
            Question = question;
            CorrectAnswer = correctAnswer;
            Topic = topic;
        }
    }

    public enum MathTopic
    {
        Algebra,
        Geometry,
        Probability,
        Functions,
        Calculus
    }

    public class ScienceCard
    {
        public string Title { get; }
        public string Description { get; }
        public string Keywords { get; }
        public ScienceCategory Category { get; }

        public ScienceCard(string title, string description, string keywords, ScienceCategory category)
        {
            Title = title;
            Description = description;
            Keywords = keywords;
            Category = category;
        }
    }

    public enum ScienceCategory
    {
        Biology,
        Chemistry,
        Physics,
        Astronomy
    }

    public class HistoricalEvent
    {
        public int Year { get; }
        public string Event { get; }
        public string Location { get; }
        public HistoricalEra Era { get; }

        public HistoricalEvent(int year, string @event, string location, HistoricalEra era)
        {
            Year = year;
            Event = @event;
            Location = location;
            Era = era;
        }
    }

    public enum HistoricalEra
    {
        Ancient,
        Medieval,
        EarlyModern,
        Modern
    }

    #endregion
}
