using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 单词消消乐服务：负责将 LearningItem 映射为游戏数据，并把游戏结果回写学习状态与错题本。
    /// </summary>
    public class WordMatchGameService
    {
        private readonly IContentLoaderService _contentLoaderService;
        private readonly IWrongAnswerService _wrongAnswerService;
        private readonly ILogger<WordMatchGameService> _logger;

        public WordMatchGameService(
            IContentLoaderService contentLoaderService,
            IWrongAnswerService wrongAnswerService,
            ILogger<WordMatchGameService> logger)
        {
            _contentLoaderService = contentLoaderService;
            _wrongAnswerService = wrongAnswerService;
            _logger = logger;
        }

        /// <summary>
        /// 从词库构建游戏数据（仅保留单词与释义均非空的条目，抽取 maxCount 个）。
        /// </summary>
        /// <param name="context">学习上下文。</param>
        /// <param name="maxCount">抽取条数。</param>
        /// <param name="selection">配牌策略，默认 <see cref="WordSelection.Random"/> 向后兼容。</param>
        /// <param name="excludeIds">需排除的词条 Id（本局已答对、换一组不再出现）；排除后无剩余则回退全量。</param>
        public List<WordMatchItemDto> BuildItems(
            LearningContext context,
            int maxCount,
            WordSelection selection = WordSelection.Random,
            IReadOnlyCollection<string>? excludeIds = null)
        {
            try
            {
                var items = _contentLoaderService.LoadItems(context);

                // 按选定策略排序：Random 随机；WrongFirst 优先低掌握度（错题优先）；ReviewDue 优先临近复习
                IEnumerable<Models.Learning.LearningItem> ordered = selection switch
                {
                    WordSelection.WrongFirst => items
                        .OrderBy(i => (int)i.Status)
                        .ThenBy(i => i.ReviewCount)
                        .ThenBy(_ => Guid.NewGuid()),
                    WordSelection.ReviewDue => items
                        .OrderBy(i => i.LastReviewedAt ?? DateTime.MinValue)
                        .ThenBy(_ => Guid.NewGuid()),
                    _ => items.OrderBy(_ => Guid.NewGuid())
                };

                List<Models.Learning.LearningItem> valid = ordered
                    .Where(i => !string.IsNullOrWhiteSpace(i.MainContent) && i.Meaning != null && !string.IsNullOrWhiteSpace(i.Meaning.Content))
                    .ToList();

                // 排除「本局已答对」的词条（换一组不再出现）；若排除后无剩余，则回退全量避免误报词库为空
                if (excludeIds != null && excludeIds.Count > 0)
                {
                    var remaining = valid.Where(i => !excludeIds.Contains(i.Id)).ToList();
                    if (remaining.Count > 0) valid = remaining;
                }

                var result = valid
                    .Take(Math.Max(1, maxCount))
                    .Select(i => new WordMatchItemDto
                    {
                        Id = i.Id,
                        Word = i.MainContent,
                        Meaning = i.Meaning!.Content,
                        Phonetic = i.GetPronunciation() ?? string.Empty,
                        Example = i.Example?.Content ?? string.Empty
                    })
                    .ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "构建消消乐游戏数据失败: {Category}", context.SubCategory);
                return new List<WordMatchItemDto>();
            }
        }

        /// <summary>
        /// 将游戏结果回写：更新每个单词的复习状态（Review），答错的写入错题本。
        /// </summary>
        public void ApplyResults(string userId, LearningContext context, IReadOnlyList<WordMatchResult> results)
        {
            if (results == null || results.Count == 0) return;

            try
            {
                var items = _contentLoaderService.LoadItems(context);
                bool changed = false;

                foreach (var result in results)
                {
                    var item = items.FirstOrDefault(i => i.Id == result.Id);
                    if (item == null) continue;

                    item.Review(result.Correct);
                    changed = true;

                    if (!result.Correct)
                    {
                        _wrongAnswerService.AddWrongAnswer(userId, new WrongAnswerItem
                        {
                            Question = item.MainContent,
                            CorrectAnswer = item.Meaning?.Content ?? string.Empty,
                            UserAnswer = string.Empty,
                            Subject = context.Subject,
                            Category = context.SubCategory,
                            Explanation = item.Example?.Content ?? string.Empty
                        });
                    }
                }

                if (changed)
                {
                    _contentLoaderService.SaveItems(context, items);
                    _logger.LogInformation("单词消消乐回写完成: userId={UserId}, 结果数={Count}", userId, results.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "单词消消乐回写失败: userId={UserId}", userId);
            }
        }
    }

    /// <summary>
    /// 游戏配牌策略。
    /// </summary>
    public enum WordSelection
    {
        /// <summary>完全随机抽取。</summary>
        Random = 0,
        /// <summary>优先低掌握度/复习次数少的词条（错题优先）。</summary>
        WrongFirst = 1,
        /// <summary>优先临近复习（长时间未复习）的词条。</summary>
        ReviewDue = 2
    }

    /// <summary>
    /// 游戏数据项（前端展示用）。
    /// </summary>
    public sealed class WordMatchItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Word { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public string Phonetic { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;
    }

    /// <summary>
    /// 游戏结果项（前端上报用）。
    /// </summary>
    public sealed class WordMatchResult
    {
        public string Id { get; set; } = string.Empty;
        public bool Correct { get; set; }
    }
}