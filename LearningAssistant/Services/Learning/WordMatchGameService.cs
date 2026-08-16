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
        /// 从词库构建游戏数据（仅保留单词与释义均非空的条目，随机抽取 maxCount 个）。
        /// </summary>
        public List<WordMatchItemDto> BuildItems(LearningContext context, int maxCount)
        {
            try
            {
                var items = _contentLoaderService.LoadItems(context);

                var valid = items
                    .Where(i => !string.IsNullOrWhiteSpace(i.MainContent) && i.Meaning != null && !string.IsNullOrWhiteSpace(i.Meaning.Content))
                    .OrderBy(_ => Guid.NewGuid())
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

                return valid;
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