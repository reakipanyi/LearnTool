using LearningAssistant.Common;
using LearningAssistant.Services.Persistence;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习状态缓存服务实现
    /// 提供学习项已知/未知状态的缓存管理
    /// 从 LearningForm 中提取的状态缓存逻辑
    /// </summary>
    public class LearningStateCacheService : ILearningStateCacheService
    {
        private readonly IDataPersistenceService? _persistenceService;
        private readonly ILogger<LearningStateCacheService> _logger;

        private HashSet<string>? _cachedKnownItems;
        private HashSet<string>? _cachedUnknownItems;
        private string? _cachedUserId;
        private SubCategoryType _cachedSubCategory;

        public LearningStateCacheService(
            IDataPersistenceService? persistenceService,
            ILogger<LearningStateCacheService> logger)
        {
            _persistenceService = persistenceService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(HashSet<string> KnownItems, HashSet<string> UnknownItems)> LoadCacheAsync(string userId, SubCategoryType subCategory)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("加载学习状态缓存失败: 用户ID为空");
                return (new HashSet<string>(), new HashSet<string>());
            }

            try
            {
                // 检查缓存是否有效
                if (_cachedKnownItems != null && _cachedUnknownItems != null &&
                    _cachedUserId == userId && _cachedSubCategory == subCategory)
                {
                    return (_cachedKnownItems, _cachedUnknownItems);
                }

                // 从持久化服务加载数据
                if (_persistenceService != null)
                {
                    _cachedKnownItems = new HashSet<string>(_persistenceService.GetKnownItems(userId, subCategory));
                    _cachedUnknownItems = new HashSet<string>(_persistenceService.GetUnknownItems(userId, subCategory));
                }
                else
                {
                    _cachedKnownItems = new HashSet<string>();
                    _cachedUnknownItems = new HashSet<string>();
                }

                _cachedUserId = userId;
                _cachedSubCategory = subCategory;

                _logger.LogDebug("成功加载学习状态缓存: UserId={UserId}, SubCategory={SubCategory}, Known={KnownCount}, Unknown={UnknownCount}",
                    userId, subCategory, _cachedKnownItems.Count, _cachedUnknownItems.Count);

                return (_cachedKnownItems, _cachedUnknownItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载学习状态缓存失败: UserId={UserId}, SubCategory={SubCategory}", userId, subCategory);
                return (new HashSet<string>(), new HashSet<string>());
            }
        }

        public bool IsItemKnown(string itemText)
        {
            if (string.IsNullOrWhiteSpace(itemText))
                return false;

            EnsureCacheLoaded();
            return _cachedKnownItems?.Contains(itemText) ?? false;
        }

        public bool IsItemUnknown(string itemText)
        {
            if (string.IsNullOrWhiteSpace(itemText))
                return false;

            EnsureCacheLoaded();
            return _cachedUnknownItems?.Contains(itemText) ?? false;
        }

        public void UpdateItemStateImmediately(string itemText, bool isKnown)
        {
            if (string.IsNullOrWhiteSpace(itemText))
                return;

            EnsureCacheLoaded();

            if (isKnown)
            {
                _cachedKnownItems?.Add(itemText);
                _cachedUnknownItems?.Remove(itemText);
            }
            else
            {
                _cachedUnknownItems?.Add(itemText);
                _cachedKnownItems?.Remove(itemText);
            }

            _logger.LogDebug("立即更新项状态: Item={Item}, IsKnown={IsKnown}", itemText, isKnown);
        }

        public void InvalidateCache()
        {
            _cachedKnownItems = null;
            _cachedUnknownItems = null;
            _cachedUserId = null;

            _logger.LogDebug("学习状态缓存已失效");
        }

        public HashSet<string>? GetCachedKnownItems()
        {
            return _cachedKnownItems;
        }

        public HashSet<string>? GetCachedUnknownItems()
        {
            return _cachedUnknownItems;
        }

        #region === 私有方法 ===

        /// <summary>
        /// 确保缓存已加载
        /// </summary>
        private void EnsureCacheLoaded()
        {
            if (_cachedKnownItems == null || _cachedUnknownItems == null)
            {
                if (_persistenceService != null &&
                    !string.IsNullOrWhiteSpace(_cachedUserId))
                {
                    _cachedKnownItems = new HashSet<string>(_persistenceService.GetKnownItems(_cachedUserId, _cachedSubCategory));
                    _cachedUnknownItems = new HashSet<string>(_persistenceService.GetUnknownItems(_cachedUserId, _cachedSubCategory));
                }
                else
                {
                    _cachedKnownItems = new HashSet<string>();
                    _cachedUnknownItems = new HashSet<string>();
                }
            }
        }

        #endregion
    }
}