using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace UnifiedLearningAssistant.Services.Learning
{
    /// <summary>
    /// 名人名言服务
    /// 提供每日励志名言，支持自动更新
    /// </summary>
    public class QuoteService
    {
        private readonly ILogger<QuoteService>? _logger;
        private readonly List<Quote> _quotes;
        private Quote? _todayQuote;
        private DateTime _lastUpdateDate;

        public QuoteService(ILogger<QuoteService>? logger = null)
        {
            _logger = logger;
            _quotes = LoadQuotes();
        }

        /// <summary>
        /// 获取今日名言
        /// </summary>
        public Quote GetTodayQuote()
        {
            var today = DateTime.Today;
            
            // 如果日期变了或者还没有今日名言，重新选择
            if (_todayQuote == null || _lastUpdateDate.Date != today.Date)
            {
                _todayQuote = GetRandomQuote();
                _lastUpdateDate = today;
                _logger?.LogInformation("今日名言已更新: {Quote}", _todayQuote.Text);
            }

            return _todayQuote;
        }

        /// <summary>
        /// 获取随机名言
        /// </summary>
        public Quote GetRandomQuote()
        {
            if (_quotes.Count == 0)
            {
                return new Quote("学习是进步的阶梯", "未知", QuoteCategory.Motivation);
            }

            var random = new Random();
            var index = random.Next(_quotes.Count);
            return _quotes[index];
        }

        /// <summary>
        /// 根据分类获取名言
        /// </summary>
        public Quote GetQuoteByCategory(QuoteCategory category)
        {
            var categoryQuotes = _quotes.Where(q => q.Category == category).ToList();
            
            if (categoryQuotes.Count == 0)
            {
                return GetRandomQuote();
            }

            var random = new Random();
            var index = random.Next(categoryQuotes.Count);
            return categoryQuotes[index];
        }

        /// <summary>
        /// 获取所有分类
        /// </summary>
        public List<QuoteCategory> GetCategories()
        {
            return Enum.GetValues(typeof(QuoteCategory)).Cast<QuoteCategory>().ToList();
        }

        /// <summary>
        /// 获取指定数量的随机名言
        /// </summary>
        public List<Quote> GetRandomQuotes(int count)
        {
            var shuffled = _quotes.OrderBy(_ => new Random().Next()).ToList();
            return shuffled.Take(Math.Min(count, shuffled.Count)).ToList();
        }

        private List<Quote> LoadQuotes()
        {
            return new List<Quote>
            {
                // 励志类
                new Quote("成功不是将来才有的，而是从决定去做的那一刻起，持续累积而成。", "俞敏洪", QuoteCategory.Motivation),
                new Quote("人生最大的挑战是发现自己是谁，而第二大的挑战是对所发现的感到满意。", "罗杰·塞尔夫", QuoteCategory.Motivation),
                new Quote("不要等待机会，而要创造机会。", "林肯", QuoteCategory.Motivation),
                new Quote("成功的秘诀在于始终如一地坚持目标。", "弗洛伦斯·南丁格尔", QuoteCategory.Motivation),
                new Quote("每一个不曾起舞的日子，都是对生命的辜负。", "尼采", QuoteCategory.Motivation),
                new Quote("生活不是等待风暴过去，而是学会在雨中翩翩起舞。", "维维安·格林", QuoteCategory.Motivation),
                new Quote("只有那些敢于相信自己内心深处的人，才能改变世界。", "乔布斯", QuoteCategory.Motivation),
                new Quote("成功的路上并不拥挤，因为坚持的人不多。", "佚名", QuoteCategory.Motivation),
                
                // 学习类
                new Quote("读书破万卷，下笔如有神。", "杜甫", QuoteCategory.Learning),
                new Quote("学而不思则罔，思而不学则殆。", "孔子", QuoteCategory.Learning),
                new Quote("知识是心灵的眼睛。", "德雷克", QuoteCategory.Learning),
                new Quote("学习是终身的事业。", "陶行知", QuoteCategory.Learning),
                new Quote("书籍是人类进步的阶梯。", "高尔基", QuoteCategory.Learning),
                new Quote("博学之，审问之，慎思之，明辨之，笃行之。", "《中庸》", QuoteCategory.Learning),
                new Quote("路漫漫其修远兮，吾将上下而求索。", "屈原", QuoteCategory.Learning),
                new Quote("活到老，学到老。", "谚语", QuoteCategory.Learning),
                
                // 时间类
                new Quote("时间就是金钱。", "富兰克林", QuoteCategory.Time),
                new Quote("盛年不重来，一日难再晨。及时当勉励，岁月不待人。", "陶渊明", QuoteCategory.Time),
                new Quote("时间是最公平的资源，每个人每天都有24小时。", "佚名", QuoteCategory.Time),
                new Quote("浪费时间等于浪费生命。", "鲁迅", QuoteCategory.Time),
                new Quote("明日复明日，明日何其多。我生待明日，万事成蹉跎。", "钱鹤滩", QuoteCategory.Time),
                new Quote("一寸光阴一寸金，寸金难买寸光阴。", "谚语", QuoteCategory.Time),
                
                // 奋斗类
                new Quote("奋斗是万物之父。", "陶行知", QuoteCategory.Struggle),
                new Quote("宝剑锋从磨砺出，梅花香自苦寒来。", "谚语", QuoteCategory.Struggle),
                new Quote("千淘万漉虽辛苦，吹尽狂沙始到金。", "刘禹锡", QuoteCategory.Struggle),
                new Quote("有志者，事竟成，破釜沉舟，百二秦关终属楚。", "蒲松龄", QuoteCategory.Struggle),
                new Quote("世上无难事，只怕有心人。", "谚语", QuoteCategory.Struggle),
                new Quote("锲而不舍，金石可镂。", "荀子", QuoteCategory.Struggle),
                
                // 智慧类
                new Quote("知识是智慧的火炬。", "英国谚语", QuoteCategory.Wisdom),
                new Quote("智慧不是与生俱来的，而是通过学习和实践获得的。", "亚里士多德", QuoteCategory.Wisdom),
                new Quote("真正的智慧是知道自己的无知。", "苏格拉底", QuoteCategory.Wisdom),
                new Quote("知识可以带来智慧，但智慧不一定带来知识。", "佚名", QuoteCategory.Wisdom),
                
                // 梦想类
                new Quote("梦想是给人生一个期许。", "佚名", QuoteCategory.Dream),
                new Quote("梦想只要能持久，就能成为现实。", "居里夫人", QuoteCategory.Dream),
                new Quote("心有多大，舞台就有多大。", "谚语", QuoteCategory.Dream),
                new Quote("不要放弃你的梦想，迟早有一天它会实现。", "林肯", QuoteCategory.Dream)
            };
        }
    }

    /// <summary>
    /// 名言类
    /// </summary>
    public class Quote
    {
        public string Text { get; }
        public string Author { get; }
        public QuoteCategory Category { get; }

        public Quote(string text, string author, QuoteCategory category)
        {
            Text = text;
            Author = author;
            Category = category;
        }

        public override string ToString()
        {
            return $"\"{Text}\" —— {Author}";
        }
    }

    /// <summary>
    /// 名言分类
    /// </summary>
    public enum QuoteCategory
    {
        /// <summary>
        /// 励志
        /// </summary>
        Motivation,
        
        /// <summary>
        /// 学习
        /// </summary>
        Learning,
        
        /// <summary>
        /// 时间
        /// </summary>
        Time,
        
        /// <summary>
        /// 奋斗
        /// </summary>
        Struggle,
        
        /// <summary>
        /// 智慧
        /// </summary>
        Wisdom,
        
        /// <summary>
        /// 梦想
        /// </summary>
        Dream
    }
}
