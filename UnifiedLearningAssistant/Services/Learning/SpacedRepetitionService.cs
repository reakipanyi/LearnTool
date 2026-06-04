using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Models;

namespace UnifiedLearningAssistant.Services.Learning
{
    public interface ISpacedRepetitionService
    {
        ReviewResult CalculateNextReview(ReviewItem item, int quality);
        ReviewItem CreateNewItem(string content, string answer = "");
        List<ReviewItem> GetItemsDueForReview(string userId, DateTime? date = null);
        void UpdateItem(ReviewItem item);
        List<ReviewItem> GetAllItems(string userId);
        void DeleteItem(Guid itemId);
        int GetTodayReviewCount(string userId);
        double CalculateRetentionRate(string userId);
    }

    public class ReviewItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public int Interval { get; set; } = 0;
        public int Repetitions { get; set; } = 0;
        public double EFactor { get; set; } = 2.5;
        public DateTime NextReviewDate { get; set; } = DateTime.Now;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public int WrongCount { get; set; } = 0;
        public int CorrectCount { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        
        public double Difficulty => EFactor < 2.3 ? 3 : EFactor < 2.5 ? 2 : 1;
    }

    public class ReviewResult
    {
        public bool ShouldReview { get; set; }
        public int NewInterval { get; set; }
        public int NewRepetitions { get; set; }
        public double NewEFactor { get; set; }
        public DateTime NextReviewDate { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SpacedRepetitionService : ISpacedRepetitionService
    {
        private readonly Dictionary<string, List<ReviewItem>> _userItems = new Dictionary<string, List<ReviewItem>>();
        private readonly ILogger<SpacedRepetitionService>? _logger;

        public SpacedRepetitionService(ILogger<SpacedRepetitionService>? logger = null)
        {
            _logger = logger;
        }

        public ReviewResult CalculateNextReview(ReviewItem item, int quality)
        {
            var result = new ReviewResult();
            
            if (quality < 0 || quality > 5)
            {
                result.ShouldReview = true;
                result.Message = "质量评分无效，需要重新学习";
                _logger?.LogWarning("无效的质量评分: {Quality}", quality);
                return result;
            }

            double newEFactor = Math.Max(1.3, item.EFactor + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02)));
            
            int newInterval;
            int newRepetitions;

            if (quality < 3)
            {
                newRepetitions = 0;
                newInterval = 1;
                result.Message = "需要重新学习";
            }
            else
            {
                if (item.Repetitions == 0)
                {
                    newInterval = 1;
                }
                else if (item.Repetitions == 1)
                {
                    newInterval = 6;
                }
                else
                {
                    newInterval = (int)Math.Round(item.Interval * newEFactor);
                }
                
                newRepetitions = item.Repetitions + 1;
                result.Message = quality == 5 ? "完美！下次复习将在 {0} 天后" : "继续加油！下次复习将在 {0} 天后";
            }

            var nextReview = DateTime.Today.AddDays(newInterval);
            
            result.ShouldReview = false;
            result.NewInterval = newInterval;
            result.NewRepetitions = newRepetitions;
            result.NewEFactor = newEFactor;
            result.NextReviewDate = nextReview;
            
            item.Interval = newInterval;
            item.Repetitions = newRepetitions;
            item.EFactor = newEFactor;
            item.NextReviewDate = nextReview;
            item.UpdatedAt = DateTime.Now;

            if (quality >= 3)
            {
                item.CorrectCount++;
            }
            else
            {
                item.WrongCount++;
            }

            UpdateItem(item);
            _logger?.LogInformation("计算复习结果: 用户 {UserId}, 间隔 {Interval} 天, 重复次数 {Repetitions}, EF {EFactor}", 
                item.UserId, newInterval, newRepetitions, newEFactor);

            return result;
        }

        public ReviewItem CreateNewItem(string content, string answer = "")
        {
            var item = new ReviewItem
            {
                Content = content,
                Answer = answer,
                Interval = 0,
                Repetitions = 0,
                EFactor = 2.5,
                NextReviewDate = DateTime.Today,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            var logContent = content.Length > 30 ? content.Substring(0, 30) + "..." : content;
            _logger?.LogInformation("创建新复习项: {Content}", logContent);
            return item;
        }

        public List<ReviewItem> GetItemsDueForReview(string userId, DateTime? date = null)
        {
            var targetDate = date ?? DateTime.Today;
            
            if (!_userItems.TryGetValue(userId, out var items))
            {
                return new List<ReviewItem>();
            }

            var dueItems = items.Where(i => i.IsActive && i.NextReviewDate <= targetDate.Date)
                               .OrderBy(i => i.NextReviewDate)
                               .ToList();
            
            _logger?.LogDebug("获取待复习项: 用户 {UserId}, 数量 {Count}", userId, dueItems.Count);
            return dueItems;
        }

        public void UpdateItem(ReviewItem item)
        {
            if (!_userItems.ContainsKey(item.UserId))
            {
                _userItems[item.UserId] = new List<ReviewItem>();
            }

            var items = _userItems[item.UserId];
            var existingIndex = items.FindIndex(i => i.Id == item.Id);
            
            if (existingIndex >= 0)
            {
                items[existingIndex] = item;
            }
            else
            {
                items.Add(item);
            }
            
            _logger?.LogDebug("更新复习项: {Id}", item.Id);
        }

        public List<ReviewItem> GetAllItems(string userId)
        {
            if (_userItems.TryGetValue(userId, out var items))
            {
                return items.Where(i => i.IsActive).ToList();
            }
            return new List<ReviewItem>();
        }

        public void DeleteItem(Guid itemId)
        {
            foreach (var kvp in _userItems)
            {
                var item = kvp.Value.FirstOrDefault(i => i.Id == itemId);
                if (item != null)
                {
                    item.IsActive = false;
                    _logger?.LogInformation("标记复习项为删除: {Id}", itemId);
                    return;
                }
            }
        }

        public int GetTodayReviewCount(string userId)
        {
            if (!_userItems.TryGetValue(userId, out var items))
            {
                return 0;
            }

            var today = DateTime.Today;
            return items.Count(i => i.IsActive && 
                                   i.NextReviewDate.Date <= today && 
                                   i.UpdatedAt.Date == today && 
                                   i.CorrectCount > 0);
        }

        public double CalculateRetentionRate(string userId)
        {
            if (!_userItems.TryGetValue(userId, out var items))
            {
                return 0;
            }

            var activeItems = items.Where(i => i.IsActive).ToList();
            if (activeItems.Count == 0)
            {
                return 0;
            }

            var totalReviews = activeItems.Sum(i => i.CorrectCount + i.WrongCount);
            if (totalReviews == 0)
            {
                return 0;
            }

            var correctReviews = activeItems.Sum(i => i.CorrectCount);
            var retentionRate = (double)correctReviews / totalReviews * 100;
            
            _logger?.LogDebug("计算保持率: 用户 {UserId}, 保持率 {Rate}%", userId, retentionRate);
            return retentionRate;
        }
    }
}