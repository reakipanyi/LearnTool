using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Models.Favorites;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Services.Favorites
{
    /// <summary>
    /// 收藏夹服务实现
    /// </summary>
    public class FavoritesService : IFavoritesService
    {
        private readonly ILogger<FavoritesService>? _logger;
        private readonly IEventBus? _eventBus;
        private List<FavoriteFolder> _folders = new();
        private List<FavoriteItem> _items = new();
        private string _dataFilePath => AppPaths.GetUserFavoritesPath();

        public FavoritesService(ILogger<FavoritesService>? logger = null, IEventBus? eventBus = null)
        {
            _logger = logger;
            _eventBus = eventBus;
            LoadData();
        }

        #region 文件夹操作

        public List<FavoriteFolder> GetAllFolders()
        {
            return _folders.OrderBy(f => f.OrderIndex).ThenBy(f => f.Name).ToList();
        }

        public List<FavoriteFolder> GetSubFolders(string? parentId = null)
        {
            var folders = _folders.Where(f => f.ParentId == parentId)
                .OrderBy(f => f.OrderIndex)
                .ThenBy(f => f.Name)
                .Select(f => new FavoriteFolder
                {
                    Id = f.Id,
                    Name = f.Name,
                    ParentId = f.ParentId,
                    OrderIndex = f.OrderIndex,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt,
                    Icon = f.Icon,
                    SubFolderCount = _folders.Count(sf => sf.ParentId == f.Id),
                    ItemCount = _items.Count(i => i.FolderId == f.Id)
                })
                .ToList();

            return folders;
        }

        public FavoriteFolder? GetFolder(string folderId)
        {
            return _folders.FirstOrDefault(f => f.Id == folderId);
        }

        public FavoriteFolder CreateFolder(string name, string? parentId = null)
        {
            var folder = new FavoriteFolder
            {
                Name = name,
                ParentId = parentId,
                OrderIndex = _folders.Where(f => f.ParentId == parentId).Count()
            };

            _folders.Add(folder);
            SaveChanges();
            _logger?.LogInformation("创建收藏夹文件夹: {Name}", name);
            return folder;
        }

        public void UpdateFolder(string folderId, string name, string? icon = null)
        {
            var folder = GetFolder(folderId);
            if (folder == null) return;

            folder.Name = name;
            folder.Icon = icon;
            folder.UpdatedAt = DateTime.Now;
            SaveChanges();
        }

        public bool DeleteFolder(string folderId, bool deleteItems = false)
        {
            var folder = GetFolder(folderId);
            if (folder == null) return false;

            var subFolderIds = GetAllSubFolderIds(folderId);

            if (deleteItems)
            {
                _items.RemoveAll(i => i.FolderId == folderId || subFolderIds.Contains(i.FolderId));
            }
            else
            {
                foreach (var item in _items.Where(i => i.FolderId == folderId || subFolderIds.Contains(i.FolderId)))
                {
                    item.FolderId = "root";
                }
            }

            _folders.RemoveAll(f => f.Id == folderId || subFolderIds.Contains(f.Id));
            SaveChanges();
            _logger?.LogInformation("删除收藏夹文件夹: {FolderId}", folderId);
            return true;
        }

        public void MoveFolder(string folderId, string? targetParentId)
        {
            var folder = GetFolder(folderId);
            if (folder == null) return;

            folder.ParentId = targetParentId;
            folder.UpdatedAt = DateTime.Now;
            SaveChanges();
        }

        public void RenameFolder(string folderId, string newName)
        {
            var folder = GetFolder(folderId);
            if (folder == null) return;

            folder.Name = newName;
            folder.UpdatedAt = DateTime.Now;
            SaveChanges();
        }

        #endregion

        #region 收藏项操作

        public FavoriteItem AddItem(FavoriteItemType type, string title, string? content = null,
            string? description = null, string? folderId = null, List<string>? tags = null)
        {
            var item = new FavoriteItem
            {
                Type = type,
                Title = title,
                Content = content,
                Description = description,
                FolderId = folderId ?? "root",
                Tags = tags ?? new List<string>(),
                OrderIndex = _items.Count(i => i.FolderId == (folderId ?? "root"))
            };

            _items.Add(item);
            SaveChanges();
            _logger?.LogDebug("添加收藏: {Title}", title);

            if (_eventBus != null && !string.IsNullOrEmpty(content))
            {
                _eventBus.Publish(new FavoriteAddedEvent
                {
                    UserId = AppPaths.GetCurrentUserId(),
                    ItemId = item.Id,
                    ItemContent = content
                });
            }

            return item;
        }

        public FavoriteItem AddItem(FavoriteItem item)
        {
            if (string.IsNullOrEmpty(item.Id))
                item.Id = Guid.NewGuid().ToString();

            item.CreatedAt = DateTime.Now;
            item.UpdatedAt = DateTime.Now;

            _items.Add(item);
            SaveChanges();
            return item;
        }

        public FavoriteItem? GetItem(string itemId)
        {
            return _items.FirstOrDefault(i => i.Id == itemId);
        }

        public void UpdateItem(string itemId, Action<FavoriteItem> updateAction)
        {
            var item = GetItem(itemId);
            if (item == null) return;

            updateAction(item);
            item.UpdatedAt = DateTime.Now;
            SaveChanges();
        }

        public bool DeleteItem(string itemId)
        {
            var item = GetItem(itemId);
            if (item == null) return false;

            _items.Remove(item);
            SaveChanges();
            return true;
        }

        public void MoveItem(string itemId, string targetFolderId)
        {
            var item = GetItem(itemId);
            if (item == null) return;

            item.FolderId = targetFolderId;
            item.UpdatedAt = DateTime.Now;
            SaveChanges();
        }

        public void MoveItems(List<string> itemIds, string targetFolderId)
        {
            foreach (var itemId in itemIds)
            {
                var item = GetItem(itemId);
                if (item != null)
                {
                    item.FolderId = targetFolderId;
                    item.UpdatedAt = DateTime.Now;
                }
            }
            SaveChanges();
        }

        public void DeleteItems(List<string> itemIds)
        {
            _items.RemoveAll(i => itemIds.Contains(i.Id));
            SaveChanges();
        }

        #endregion

        #region 搜索和查询

        public PagedResult<FavoriteItem> SearchItems(FavoriteSearchParams searchParams)
        {
            var query = _items.AsEnumerable();

            if (!string.IsNullOrEmpty(searchParams.FolderId) && searchParams.FolderId != "all")
            {
                query = query.Where(i => i.FolderId == searchParams.FolderId);
            }

            if (!string.IsNullOrEmpty(searchParams.Keyword))
            {
                var keyword = searchParams.Keyword.ToLower();
                query = query.Where(i =>
                    i.Title.ToLower().Contains(keyword) ||
                    (i.Description != null && i.Description.ToLower().Contains(keyword)) ||
                    (i.Content != null && i.Content.ToLower().Contains(keyword)) ||
                    i.Tags.Any(t => t.ToLower().Contains(keyword))
                );
            }

            if (searchParams.Types != null && searchParams.Types.Count > 0)
            {
                query = query.Where(i => searchParams.Types.Contains(i.Type));
            }

            if (searchParams.Tags != null && searchParams.Tags.Count > 0)
            {
                query = query.Where(i => i.Tags.Any(t => searchParams.Tags.Contains(t)));
            }

            if (searchParams.PinnedOnly)
            {
                query = query.Where(i => i.IsPinned);
            }

            query = searchParams.SortOrder switch
            {
                FavoriteSortOrder.CreatedDesc => query.OrderByDescending(i => i.IsPinned).ThenByDescending(i => i.CreatedAt),
                FavoriteSortOrder.CreatedAsc => query.OrderByDescending(i => i.IsPinned).ThenBy(i => i.CreatedAt),
                FavoriteSortOrder.UpdatedDesc => query.OrderByDescending(i => i.IsPinned).ThenByDescending(i => i.UpdatedAt),
                FavoriteSortOrder.UpdatedAsc => query.OrderByDescending(i => i.IsPinned).ThenBy(i => i.UpdatedAt),
                FavoriteSortOrder.NameAsc => query.OrderByDescending(i => i.IsPinned).ThenBy(i => i.Title),
                FavoriteSortOrder.NameDesc => query.OrderByDescending(i => i.IsPinned).ThenByDescending(i => i.Title),
                FavoriteSortOrder.Custom => query.OrderByDescending(i => i.IsPinned).ThenBy(i => i.OrderIndex),
                FavoriteSortOrder.VisitCountDesc => query.OrderByDescending(i => i.IsPinned).ThenByDescending(i => i.VisitCount),
                FavoriteSortOrder.LastVisitedDesc => query.OrderByDescending(i => i.IsPinned).ThenByDescending(i => i.LastVisitedAt ?? DateTime.MinValue),
                _ => query.OrderByDescending(i => i.IsPinned).ThenByDescending(i => i.CreatedAt)
            };

            var totalCount = query.Count();
            var items = query
                .Skip((searchParams.Page - 1) * searchParams.PageSize)
                .Take(searchParams.PageSize)
                .ToList();

            return new PagedResult<FavoriteItem>
            {
                Items = items,
                TotalCount = totalCount,
                Page = searchParams.Page,
                PageSize = searchParams.PageSize
            };
        }

        public PagedResult<FavoriteItem> GetItemsByFolder(string folderId, int page = 1, int pageSize = 50,
            FavoriteSortOrder sortOrder = FavoriteSortOrder.CreatedDesc)
        {
            return SearchItems(new FavoriteSearchParams
            {
                FolderId = folderId,
                Page = page,
                PageSize = pageSize,
                SortOrder = sortOrder
            });
        }

        #endregion

        #region 标签操作

        public List<string> GetAllTags()
        {
            return _items.SelectMany(i => i.Tags)
                .Distinct()
                .OrderBy(t => t)
                .ToList();
        }

        public Dictionary<string, int> GetTagsWithCount()
        {
            return _items.SelectMany(i => i.Tags)
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public void AddTag(string itemId, string tag)
        {
            var item = GetItem(itemId);
            if (item == null) return;

            if (!item.Tags.Contains(tag))
            {
                item.Tags.Add(tag);
                item.UpdatedAt = DateTime.Now;
                SaveChanges();
            }
        }

        public void RemoveTag(string itemId, string tag)
        {
            var item = GetItem(itemId);
            if (item == null) return;

            if (item.Tags.Remove(tag))
            {
                item.UpdatedAt = DateTime.Now;
                SaveChanges();
            }
        }

        public void AddTags(List<string> itemIds, List<string> tags)
        {
            if (itemIds == null || tags == null || itemIds.Count == 0 || tags.Count == 0) return;

            bool changed = false;
            foreach (var itemId in itemIds)
            {
                var item = GetItem(itemId);
                if (item == null) continue;

                foreach (var tag in tags)
                {
                    if (!item.Tags.Contains(tag))
                    {
                        item.Tags.Add(tag);
                        item.UpdatedAt = DateTime.Now;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                SaveChanges();
                _logger?.LogInformation("批量添加标签: {Count} 个收藏项，{TagCount} 个标签", itemIds.Count, tags.Count);
            }
        }

        public void SetMarkedForReview(string itemId, bool isMarked)
        {
            var item = GetItem(itemId);
            if (item == null) return;

            if (item.IsMarkedForReview != isMarked)
            {
                item.IsMarkedForReview = isMarked;
                item.UpdatedAt = DateTime.Now;
                SaveChanges();
                _logger?.LogInformation("收藏项复习标记: {ItemId}, {IsMarked}", itemId, isMarked);
            }
        }

        public void SetMarkedForReviewBatch(List<string> itemIds, bool isMarked)
        {
            if (itemIds == null || itemIds.Count == 0) return;

            bool changed = false;
            foreach (var itemId in itemIds)
            {
                var item = GetItem(itemId);
                if (item == null) continue;

                if (item.IsMarkedForReview != isMarked)
                {
                    item.IsMarkedForReview = isMarked;
                    item.UpdatedAt = DateTime.Now;
                    changed = true;
                }
            }

            if (changed)
            {
                SaveChanges();
                _logger?.LogInformation("批量标记复习: {Count} 个收藏项, {IsMarked}", itemIds.Count, isMarked);
            }
        }

        public List<FavoriteItem> GetItemsForReview(int count = 20)
        {
            return _items
                .Where(i => i.IsMarkedForReview)
                .OrderBy(i => i.LastReviewedAt ?? DateTime.MinValue)
                .ThenByDescending(i => i.CreatedAt)
                .Take(count)
                .ToList();
        }

        public void RecordReview(string itemId, bool remembered)
        {
            var item = GetItem(itemId);
            if (item == null) return;

            item.LastReviewedAt = DateTime.Now;
            item.ReviewCount++;
            item.UpdatedAt = DateTime.Now;
            SaveChanges();
        }

        #endregion

        #region 其他操作

        public void SetPinned(string itemId, bool isPinned)
        {
            var item = GetItem(itemId);
            if (item == null) return;

            item.IsPinned = isPinned;
            item.UpdatedAt = DateTime.Now;
            SaveChanges();
        }

        public void RecordVisit(string itemId)
        {
            var item = GetItem(itemId);
            if (item == null) return;

            item.VisitCount++;
            item.LastVisitedAt = DateTime.Now;
            SaveChanges();
        }

        public bool IsFavorited(FavoriteItemType type, string content)
        {
            return _items.Any(i => i.Type == type && i.Content == content);
        }

        public bool ToggleFavorite(FavoriteItemType type, string title, string content, string? folderId = null)
        {
            var existing = _items.FirstOrDefault(i => i.Type == type && i.Content == content);
            if (existing != null)
            {
                DeleteItem(existing.Id);
                return false;
            }

            AddItem(type, title, content, null, folderId);
            return true;
        }

        public bool ExportFavorites(string filePath, List<string>? folderIds = null)
        {
            try
            {
                var foldersToExport = folderIds != null && folderIds.Count > 0
                    ? _folders.Where(f => folderIds.Contains(f.Id)).ToList()
                    : _folders;

                var itemsToExport = folderIds != null && folderIds.Count > 0
                    ? _items.Where(i => folderIds.Contains(i.FolderId)).ToList()
                    : _items;

                var exportData = new
                {
                    Version = "1.0",
                    ExportedAt = DateTime.Now,
                    UserId = AppPaths.GetCurrentUserId(),
                    Folders = foldersToExport,
                    Items = itemsToExport
                };

                var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
                _logger?.LogInformation("收藏夹导出成功: {Path}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "收藏夹导出失败");
                return false;
            }
        }

        public bool ExportToMarkdown(string filePath, List<string>? folderIds = null)
        {
            try
            {
                var itemsToExport = folderIds != null && folderIds.Count > 0
                    ? _items.Where(i => folderIds.Contains(i.FolderId)).ToList()
                    : _items;

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("# 收藏夹导出");
                sb.AppendLine();
                sb.AppendLine($"> 导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"> 共 {itemsToExport.Count} 条收藏");
                sb.AppendLine();

                var foldersDict = _folders.ToDictionary(f => f.Id, f => f);

                var groupedByFolder = itemsToExport.GroupBy(i => i.FolderId);
                foreach (var group in groupedByFolder.OrderBy(g => g.Key))
                {
                    var folderName = foldersDict.TryGetValue(group.Key, out var folder)
                        ? folder.Name
                        : "未分类";

                    sb.AppendLine($"## 📁 {folderName}");
                    sb.AppendLine();

                    foreach (var item in group.OrderByDescending(i => i.CreatedAt))
                    {
                        sb.AppendLine($"### {item.Title}");
                        sb.AppendLine();

                        if (!string.IsNullOrEmpty(item.Description))
                        {
                            sb.AppendLine($"> {item.Description}");
                            sb.AppendLine();
                        }

                        if (!string.IsNullOrEmpty(item.Content))
                        {
                            sb.AppendLine(item.Content);
                            sb.AppendLine();
                        }

                        if (!string.IsNullOrEmpty(item.Answer))
                        {
                            sb.AppendLine("**答案/解释：**");
                            sb.AppendLine();
                            sb.AppendLine(item.Answer);
                            sb.AppendLine();
                        }

                        if (item.Tags.Count > 0)
                        {
                            sb.AppendLine($"标签: {string.Join(", ", item.Tags.Select(t => $"`{t}`"))}");
                            sb.AppendLine();
                        }

                        sb.AppendLine($"*类型: {item.TypeDisplayName} | 创建: {item.CreatedAt:yyyy-MM-dd}*");
                        sb.AppendLine();
                        sb.AppendLine("---");
                        sb.AppendLine();
                    }
                }

                File.WriteAllText(filePath, sb.ToString(), System.Text.Encoding.UTF8);
                _logger?.LogInformation("收藏夹导出 Markdown 成功: {Path}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "收藏夹导出 Markdown 失败");
                return false;
            }
        }

        public bool ExportToTextCards(string filePath, List<string>? folderIds = null)
        {
            try
            {
                var itemsToExport = folderIds != null && folderIds.Count > 0
                    ? _items.Where(i => folderIds.Contains(i.FolderId)).ToList()
                    : _items;

                var sb = new System.Text.StringBuilder();
                int cardNumber = 1;

                foreach (var item in itemsToExport.OrderByDescending(i => i.CreatedAt))
                {
                    string front = !string.IsNullOrEmpty(item.Title) ? item.Title : item.Content ?? "";
                    string back = !string.IsNullOrEmpty(item.Answer)
                        ? item.Answer
                        : !string.IsNullOrEmpty(item.Description)
                            ? item.Description
                            : item.Content ?? "";

                    sb.AppendLine($"===== 卡片 {cardNumber:D3} =====");
                    sb.AppendLine();
                    sb.AppendLine("【正面】");
                    sb.AppendLine(front);
                    sb.AppendLine();
                    sb.AppendLine("【背面】");
                    sb.AppendLine(back);
                    sb.AppendLine();
                    if (item.Tags.Count > 0)
                    {
                        sb.AppendLine($"标签: {string.Join(", ", item.Tags)}");
                        sb.AppendLine();
                    }
                    sb.AppendLine();

                    cardNumber++;
                }

                File.WriteAllText(filePath, sb.ToString(), System.Text.Encoding.UTF8);
                _logger?.LogInformation("收藏夹导出文本卡片成功: {Path}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "收藏夹导出文本卡片失败");
                return false;
            }
        }

        public int ImportFavorites(string filePath, string? targetFolderId = null, string mode = "merge")
        {
            try
            {
                if (!File.Exists(filePath)) return 0;

                var json = File.ReadAllText(filePath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var importedCount = 0;

                if (root.TryGetProperty("Items", out var itemsElement))
                {
                    var idMapping = new Dictionary<string, string>();

                    if (root.TryGetProperty("Folders", out var foldersElement))
                    {
                        foreach (var folderElement in foldersElement.EnumerateArray())
                        {
                            var folder = JsonSerializer.Deserialize<FavoriteFolder>(folderElement.GetRawText());
                            if (folder == null) continue;

                            var newFolder = CreateFolder(folder.Name, targetFolderId);
                            idMapping[folder.Id] = newFolder.Id;
                        }
                    }

                    foreach (var itemElement in itemsElement.EnumerateArray())
                    {
                        var item = JsonSerializer.Deserialize<FavoriteItem>(itemElement.GetRawText());
                        if (item == null) continue;

                        item.Id = Guid.NewGuid().ToString();
                        item.FolderId = idMapping.TryGetValue(item.FolderId, out var newFolderId)
                            ? newFolderId
                            : (targetFolderId ?? "root");
                        item.CreatedAt = DateTime.Now;
                        item.UpdatedAt = DateTime.Now;

                        if (mode == "overwrite")
                        {
                            var existing = _items.FirstOrDefault(i => i.Type == item.Type && i.Content == item.Content);
                            if (existing != null)
                            {
                                _items.Remove(existing);
                            }
                        }

                        _items.Add(item);
                        importedCount++;
                    }
                }

                SaveChanges();
                _logger?.LogInformation("收藏夹导入成功: {Count} 项", importedCount);
                return importedCount;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "收藏夹导入失败");
                return 0;
            }
        }

        public (int TotalItems, int TotalFolders, int TotalTags) GetStatistics()
        {
            var totalTags = _items.SelectMany(i => i.Tags).Distinct().Count();
            return (_items.Count, _folders.Count, totalTags);
        }

        public (int TotalItems, int TotalFolders, int TotalTags) GetStats()
        {
            return GetStatistics();
        }

        public (int TotalItems, int TotalFolders, int TotalTags) GetStats(string userId)
        {
            return GetStatistics();
        }

        public List<FavoriteItem> GetItems()
        {
            return _items.ToList();
        }

        public List<FavoriteItem> GetItems(string userId)
        {
            return _items.ToList();
        }

        public List<FavoriteFolder> GetFolders()
        {
            return GetAllFolders();
        }

        public List<FavoriteFolder> GetFolders(string userId)
        {
            return GetAllFolders();
        }

        public FavoriteFolder AddFolder(string name, string? parentId = null)
        {
            return CreateFolder(name, parentId);
        }

        public List<FavoriteItem> GetRecentItems(int count = 10)
        {
            return _items
                .Where(i => i.LastVisitedAt.HasValue)
                .OrderByDescending(i => i.LastVisitedAt)
                .Take(count)
                .ToList();
        }

        #endregion

        #region 私有方法

        private void LoadData()
        {
            try
            {
                if (File.Exists(_dataFilePath))
                {
                    var json = File.ReadAllText(_dataFilePath);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("Folders", out var foldersElement))
                    {
                        _folders = JsonSerializer.Deserialize<List<FavoriteFolder>>(foldersElement.GetRawText()) ?? new List<FavoriteFolder>();
                    }

                    if (root.TryGetProperty("Items", out var itemsElement))
                    {
                        _items = JsonSerializer.Deserialize<List<FavoriteItem>>(itemsElement.GetRawText()) ?? new List<FavoriteItem>();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载收藏夹数据失败");
                _folders = new List<FavoriteFolder>();
                _items = new List<FavoriteItem>();
            }
        }

        public void SaveChanges()
        {
            try
            {
                var directory = Path.GetDirectoryName(_dataFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var data = new
                {
                    Version = "1.0",
                    UpdatedAt = DateTime.Now,
                    Folders = _folders,
                    Items = _items
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_dataFilePath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存收藏夹数据失败");
            }
        }

        private List<string> GetAllSubFolderIds(string parentId)
        {
            var result = new List<string>();
            var subFolders = _folders.Where(f => f.ParentId == parentId).ToList();
            foreach (var sub in subFolders)
            {
                result.Add(sub.Id);
                result.AddRange(GetAllSubFolderIds(sub.Id));
            }
            return result;
        }

        #endregion
    }
}
