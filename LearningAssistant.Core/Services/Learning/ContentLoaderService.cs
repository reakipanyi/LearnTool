using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Abstractions;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    public class ContentLoaderService : IContentLoaderService
    {
        private readonly ILogger<ContentLoaderService> _logger;

        private readonly Dictionary<SubCategoryType, string> _categoryFileMap = new Dictionary<SubCategoryType, string>
        {
            { SubCategoryType.ChineseCharacter, Constants.FileName.ChineseCharacter },
            { SubCategoryType.ChineseIdiom, Constants.FileName.ChineseIdiom },
            { SubCategoryType.ChinesePhrase, Constants.FileName.ChinesePhrase },
            { SubCategoryType.ChinesePoem, Constants.FileName.ChinesePoem },
            { SubCategoryType.ChineseComprehensive, Constants.FileName.ChineseComprehensive },
            { SubCategoryType.EnglishWord, Constants.FileName.EnglishWord },
            { SubCategoryType.EnglishPhrase, Constants.FileName.EnglishPhrase },
            { SubCategoryType.EnglishSentence, Constants.FileName.EnglishSentence },
            { SubCategoryType.EnglishComprehensive, Constants.FileName.EnglishComprehensive },
            { SubCategoryType.MathFormula, Constants.FileName.MathFormula },
            { SubCategoryType.MathExample, Constants.FileName.MathExample },
            { SubCategoryType.MathConcept, Constants.FileName.MathConcept },
            { SubCategoryType.MathComprehensive, Constants.FileName.MathComprehensive },
            { SubCategoryType.PhysicsLaw, Constants.FileName.PhysicsLaw },
            { SubCategoryType.PhysicsExperiment, Constants.FileName.PhysicsExperiment },
            { SubCategoryType.PhysicsDerivation, Constants.FileName.PhysicsDerivation },
            { SubCategoryType.PhysicsComprehensive, Constants.FileName.PhysicsComprehensive },
            { SubCategoryType.ChemistryEquation, Constants.FileName.ChemistryEquation },
            { SubCategoryType.ChemistryElement, Constants.FileName.ChemistryElement },
            { SubCategoryType.ChemistryExperiment, Constants.FileName.ChemistryExperiment },
            { SubCategoryType.ChemistryComprehensive, Constants.FileName.ChemistryComprehensive },
            { SubCategoryType.HistoryEvent, Constants.FileName.HistoryEvent },
            { SubCategoryType.HistoryPerson, Constants.FileName.HistoryPerson },
            { SubCategoryType.HistoryTimeline, Constants.FileName.HistoryTimeline },
            { SubCategoryType.HistoryComprehensive, Constants.FileName.HistoryComprehensive },
            { SubCategoryType.GeographyKnowledge, Constants.FileName.GeographyKnowledge },
            { SubCategoryType.GeographyMap, Constants.FileName.GeographyMap },
            { SubCategoryType.GeographyClimate, Constants.FileName.GeographyClimate },
            { SubCategoryType.GeographyComprehensive, Constants.FileName.GeographyComprehensive },
            { SubCategoryType.BiologyConcept, Constants.FileName.BiologyConcept },
            { SubCategoryType.BiologyExperiment, Constants.FileName.BiologyExperiment },
            { SubCategoryType.BiologyPhenomenon, Constants.FileName.BiologyPhenomenon },
            { SubCategoryType.BiologyComprehensive, Constants.FileName.BiologyComprehensive }
        };

        private const int MaxCacheEntries = 20;
        private const int CacheSlidingExpirationMinutes = 30;

        private readonly Dictionary<string, CacheEntry> _fileCache = new Dictionary<string, CacheEntry>();
        private readonly object _cacheLock = new object();
        private readonly IAppPaths _appPaths;
        private readonly ILearningTaxonomyService? _taxonomyService;

        public ContentLoaderService(
            ILogger<ContentLoaderService> logger,
            IAppPaths appPaths,
            ILearningTaxonomyService? taxonomyService = null)
        {
            _logger = logger;
            _appPaths = appPaths;
            _taxonomyService = taxonomyService;
        }

        public List<LearningItem> LoadItems(LearningContext context)
        {
            try
            {
                string filePath = GetFilePath(context.SubCategory, context.WordBankFile);

                if (!IsPathSafe(filePath))
                {
                    _logger.LogWarning("Path traversal detected: {FilePath}", filePath);
                    return new List<LearningItem>();
                }

                var items = new List<LearningItem>();

                if (File.Exists(filePath))
                {
                    items.AddRange(LoadFromCacheOrFile(filePath, context.SubCategory));
                }
                else
                {
                    _logger.LogDebug("System word bank not found, continue with personal content: {FilePath}", filePath);
                }

                // 个人内容合并：按规范化键去重，个人内容优先覆盖系统词库中的同名项。
                var personalItems = LoadPersonalItems(context);
                if (personalItems.Count > 0)
                {
                    var personalKeys = new HashSet<string>(
                        personalItems.Select(LearningItemIdentity.GetCanonicalKey), StringComparer.Ordinal);
                    items.RemoveAll(item => personalKeys.Contains(LearningItemIdentity.GetCanonicalKey(item)));
                    items.AddRange(personalItems);
                }

                return ApplyClassificationFilters(items, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load items for subCategory: {SubCategory}", context.SubCategory);
                return new List<LearningItem>();
            }
        }

        /// <summary>
        /// 按知识点/适龄段/重要等级/来源过滤队列。未启用筛选时直接返回原集合，保持历史行为。
        /// 知识点以关联表为权威来源，旧数据回退到学习项自身的 KnowledgePointId。
        /// </summary>
        private List<LearningItem> ApplyClassificationFilters(List<LearningItem> items, LearningContext context)
        {
            if (!context.HasClassificationFilter || items.Count == 0)
                return items;

            IReadOnlyDictionary<string, string>? assignments = null;
            HashSet<string>? allowedKnowledgePointIds = null;

            if (!string.IsNullOrWhiteSpace(context.KnowledgePointId))
            {
                assignments = _taxonomyService?.GetAssignmentMap(context.UserId);

                if (context.KnowledgePointId == LearningTaxonomyService.UnclassifiedId)
                {
                    // 未分类筛选不需要子树集合
                }
                else if (_taxonomyService != null)
                {
                    allowedKnowledgePointIds = CollectKnowledgePointSubtree(
                        context.Subject, context.UserId, context.KnowledgePointId);
                }
                else
                {
                    allowedKnowledgePointIds = new HashSet<string>(StringComparer.Ordinal)
                    {
                        context.KnowledgePointId
                    };
                }
            }

            return items.Where(item =>
            {
                if (context.AgeRange is { } ageFilter && ageFilter != AgeRange.All
                    && item.AgeRange != ageFilter && item.AgeRange != AgeRange.All)
                {
                    return false;
                }

                if (context.Importance is { } importanceFilter && item.Importance != importanceFilter)
                    return false;

                if (context.SourceType is { } sourceFilter
                    && LearningItemClassification.GetEffectiveSourceType(item) != sourceFilter)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(context.KnowledgePointId))
                    return true;

                var itemPointId = ResolveItemKnowledgePointId(item, assignments);

                if (context.KnowledgePointId == LearningTaxonomyService.UnclassifiedId)
                    return string.IsNullOrWhiteSpace(itemPointId);

                return !string.IsNullOrWhiteSpace(itemPointId)
                    && allowedKnowledgePointIds != null
                    && allowedKnowledgePointIds.Contains(itemPointId);
            }).ToList();
        }

        private static string? ResolveItemKnowledgePointId(
            LearningItem item,
            IReadOnlyDictionary<string, string>? assignments)
        {
            var key = string.IsNullOrWhiteSpace(item.Id)
                ? LearningItemIdentity.GetCanonicalKey(item)
                : item.Id;

            if (assignments != null && assignments.TryGetValue(key, out var assigned))
                return assigned;

            return string.IsNullOrWhiteSpace(item.KnowledgePointId) ? null : item.KnowledgePointId;
        }

        private HashSet<string> CollectKnowledgePointSubtree(
            SubjectType subject,
            string userId,
            string rootId)
        {
            var result = new HashSet<string>(StringComparer.Ordinal) { rootId };
            var tree = _taxonomyService?.GetTree(subject, userId);
            if (tree == null || tree.Count == 0)
                return result;

            KnowledgePointNode? FindNode(IEnumerable<KnowledgePointNode> nodes)
            {
                foreach (var node in nodes)
                {
                    if (node.Id == rootId) return node;
                    var found = FindNode(node.Children);
                    if (found != null) return found;
                }
                return null;
            }

            void Collect(KnowledgePointNode node)
            {
                result.Add(node.Id);
                foreach (var child in node.Children)
                    Collect(child);
            }

            var root = FindNode(tree);
            if (root != null)
                Collect(root);

            return result;
        }

        public List<LearningItem> LoadItemsPaged(LearningContext context, int pageIndex, int pageSize)
        {
            if (pageIndex < 0) pageIndex = 0;
            if (pageSize <= 0) pageSize = 100;

            try
            {
                var allItems = LoadItems(context);
                int start = pageIndex * pageSize;
                if (start >= allItems.Count)
                    return new List<LearningItem>();

                int count = Math.Min(pageSize, allItems.Count - start);
                return allItems.Skip(start).Take(count).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load paged items for subCategory: {SubCategory}", context.SubCategory);
                return new List<LearningItem>();
            }
        }

        public int GetItemCount(LearningContext context)
        {
            try
            {
                return LoadItems(context).Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get item count for subCategory: {SubCategory}", context.SubCategory);
                return 0;
            }
        }

        /// <summary>加载当前用户的个人内容，缺失或损坏时返回空集合。</summary>
        private List<LearningItem> LoadPersonalItems(LearningContext context)
        {
            var personalPath = GetPersonalFilePath(context);
            if (personalPath == null || !File.Exists(personalPath)) return new List<LearningItem>();

            try
            {
                return LoadFromCacheOrFile(personalPath, context.SubCategory);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load personal content: {FilePath}", personalPath);
                return new List<LearningItem>();
            }
        }

        private string? GetPersonalFilePath(LearningContext context)
        {
            try
            {
                return PersonalContentStore.GetFilePath(
                    _appPaths, context.UserId, context.Subject, context.SubCategory);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve personal content path");
                return null;
            }
        }

        /// <summary>
        /// 为旧词库补齐缺失的稳定 Id。逐文件处理：有变更才备份并写回，单文件失败不阻断其他文件。
        /// </summary>
        public int EnsureAllItemsHaveIds(DateTime? modifiedSince = null)
        {
            var fixedCount = 0;
            var fileCount = 0;
            var failedCount = 0;
            var startedAt = DateTime.Now;

            foreach (var filePath in EnumerateWordBankFiles(modifiedSince))
            {
                fileCount++;
                try
                {
                    fixedCount += EnsureFileIds(filePath);
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogWarning(ex, "Failed to backfill learning item ids: {FilePath}", filePath);
                }
            }

            _logger.LogInformation(
                "Learning item id backfill finished: {FileCount} files, {FixedCount} items fixed, {FailedCount} failed, elapsed {ElapsedMs}ms",
                fileCount, fixedCount, failedCount, (int)(DateTime.Now - startedAt).TotalMilliseconds);

            return fixedCount;
        }

        /// <summary>显式失效单个文件缓存（个人内容写入后调用）。</summary>
        public void InvalidateFileCache(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            lock (_cacheLock)
            {
                _fileCache.Remove(filePath);
            }
        }

        private IEnumerable<string> EnumerateWordBankFiles(DateTime? modifiedSince)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var dataDir = _appPaths.DataDir;
                if (Directory.Exists(dataDir))
                {
                    foreach (var file in Directory.EnumerateFiles(dataDir, "*.json"))
                    {
                        var fileName = Path.GetFileName(file);
                        if (Constants.ExcludedFileKeywords.WordBankExclusions.Any(
                                keyword => fileName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            continue;
                        }

                        if (_categoryFileMap.Values.Contains(fileName, StringComparer.OrdinalIgnoreCase) ||
                            _categoryFileMap.Keys.Any(category =>
                                fileName.StartsWith(GetCategoryFilePrefix(category), StringComparison.OrdinalIgnoreCase)))
                        {
                            result.Add(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enumerate system word bank files");
            }

            try
            {
                var usersDir = _appPaths.UsersDir;
                if (Directory.Exists(usersDir))
                {
                    foreach (var userDir in Directory.EnumerateDirectories(usersDir))
                    {
                        var contentDir = Path.Combine(userDir, "content");
                        if (!Directory.Exists(contentDir)) continue;

                        foreach (var file in Directory.EnumerateFiles(contentDir, "*.json", SearchOption.AllDirectories))
                        {
                            result.Add(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enumerate personal content files");
            }

            if (modifiedSince.HasValue)
            {
                result.RemoveWhere(file =>
                {
                    try
                    {
                        return File.GetLastWriteTime(file) <= modifiedSince.Value;
                    }
                    catch
                    {
                        return true;
                    }
                });
            }

            return result;
        }

        private int EnsureFileIds(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var items = JsonHelper.DeserializeLearningItems(json);
            if (items.Count == 0) return 0;

            var missing = items.Where(item => string.IsNullOrWhiteSpace(item.Id)).ToList();
            if (missing.Count == 0) return 0;

            var backupPath = $"{filePath}.bak-{DateTime.Now:yyyyMMddHHmmss}";
            File.Copy(filePath, backupPath, overwrite: true);

            foreach (var item in missing)
            {
                LearningItemIdentity.EnsureKey(item);
            }

            JsonHelper.SaveToFile(filePath, items);
            InvalidateFileCache(filePath);

            _logger.LogInformation(
                "Backfilled {Count} learning item ids in {FilePath} (backup: {BackupPath})",
                missing.Count, filePath, backupPath);
            return missing.Count;
        }

        private List<LearningItem> LoadFromCacheOrFile(string filePath, SubCategoryType subCategory)
        {
            lock (_cacheLock)
            {
                if (_fileCache.TryGetValue(filePath, out var cached))
                {
                    if (cached.IsValid && !cached.IsExpired)
                    {
                        cached.LastAccessTime = DateTime.Now;
                        _logger.LogDebug("Items loaded from cache: {FilePath}", filePath);
                        return cached.Items.ToList();
                    }

                    if (!cached.IsValid)
                    {
                        _fileCache.Remove(filePath);
                        _logger.LogDebug("Cache invalidated (file changed): {FilePath}", filePath);
                    }
                    else if (cached.IsExpired)
                    {
                        _fileCache.Remove(filePath);
                        _logger.LogDebug("Cache expired: {FilePath}", filePath);
                    }
                }

                if (_fileCache.Count >= MaxCacheEntries)
                {
                    var oldestKey = _fileCache.OrderBy(kvp => kvp.Value.LastAccessTime)
                        .FirstOrDefault().Key;
                    if (oldestKey != null)
                    {
                        _fileCache.Remove(oldestKey);
                        _logger.LogDebug("Evicted oldest cache entry: {FilePath}", oldestKey);
                    }
                }
            }

            var fileInfo = new FileInfo(filePath);
            var json = File.ReadAllText(filePath);
            var items = JsonHelper.DeserializeLearningItems(json);

            foreach (var item in items)
            {
                if (item.SubCategory == 0)
                {
                    item.SubCategory = subCategory;
                }
            }

            lock (_cacheLock)
            {
                _fileCache[filePath] = new CacheEntry
                {
                    Items = items,
                    FilePath = filePath,
                    FileLastWriteTime = fileInfo.LastWriteTime,
                    LoadTime = DateTime.Now,
                    LastAccessTime = DateTime.Now
                };
            }

            _logger.LogDebug("Items loaded from file and cached: {FilePath}, count: {Count}", filePath, items.Count);
            return items.ToList();
        }

        public void SaveItems(LearningContext context, List<LearningItem> items)
        {
            try
            {
                string filePath = GetFilePath(context.SubCategory, context.WordBankFile);
                JsonHelper.SaveToFile(filePath, items);

                lock (_cacheLock)
                {
                    _fileCache.Remove(filePath);
                }

                _logger.LogInformation("Saved {Count} items to {FilePath}", items.Count, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save items for subCategory: {SubCategory}", context.SubCategory);
            }
        }

        public List<SubCategoryType> GetSubCategories(SubjectType subject)
        {
            return SubjectSubCategoryMapping.GetSubCategories(subject);
        }

        public List<string> GetAllSubjects()
        {
            return new List<string>
            {
                Constants.Subject.Chinese,
                Constants.Subject.English,
                Constants.Subject.Math,
                Constants.Subject.Physics,
                Constants.Subject.Chemistry,
                Constants.Subject.History,
                Constants.Subject.Geography,
                Constants.Subject.Biology
            };
        }

        public List<string> GetWordBankFiles(SubCategoryType subCategory)
        {
            try
            {
                var dataDir = _appPaths.DataDir;
                var defaultFile = _categoryFileMap.GetValueOrDefault(subCategory, "");

                var categoryPrefix = GetCategoryFilePrefix(subCategory);

                var files = Directory.EnumerateFiles(dataDir, "*.json")
                                   .Select(Path.GetFileName)
                                   .Where(file => file.StartsWith(categoryPrefix, StringComparison.OrdinalIgnoreCase)
                                                  && !Constants.ExcludedFileKeywords.WordBankExclusions.Any(keyword => file.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
                                   .ToList();

                if (!string.IsNullOrWhiteSpace(defaultFile) && !files.Contains(defaultFile, StringComparer.OrdinalIgnoreCase))
                {
                    files.Add(defaultFile);
                }

                files.Sort();
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get word bank files for subCategory: {SubCategory}", subCategory);
                return new List<string>();
            }
        }

        private string GetCategoryFilePrefix(SubCategoryType subCategory)
        {
            return subCategory switch
            {
                SubCategoryType.ChineseCharacter => "识字",
                SubCategoryType.ChinesePhrase => "短语",
                SubCategoryType.ChineseIdiom => "成语",
                SubCategoryType.ChinesePoem => "诗词",
                SubCategoryType.ChineseComprehensive => "语文综合",
                SubCategoryType.EnglishWord => "英语单词",
                SubCategoryType.EnglishPhrase => "英语短语",
                SubCategoryType.EnglishSentence => "英语句子",
                SubCategoryType.EnglishComprehensive => "英语综合",
                SubCategoryType.MathFormula => "数学公式",
                SubCategoryType.MathExample => "数学例题",
                SubCategoryType.MathConcept => "数学概念",
                SubCategoryType.MathComprehensive => "数学综合",
                SubCategoryType.PhysicsLaw => "物理定律",
                SubCategoryType.PhysicsExperiment => "物理实验",
                SubCategoryType.PhysicsDerivation => "物理推导",
                SubCategoryType.PhysicsComprehensive => "物理综合",
                SubCategoryType.ChemistryEquation => "化学方程式",
                SubCategoryType.ChemistryElement => "化学元素",
                SubCategoryType.ChemistryExperiment => "化学实验",
                SubCategoryType.ChemistryComprehensive => "化学综合",
                SubCategoryType.HistoryEvent => "历史事件",
                SubCategoryType.HistoryPerson => "历史人物",
                SubCategoryType.HistoryTimeline => "历史时间线",
                SubCategoryType.HistoryComprehensive => "历史综合",
                SubCategoryType.GeographyKnowledge => "地理知识",
                SubCategoryType.GeographyMap => "地理地图",
                SubCategoryType.GeographyClimate => "地理气候",
                SubCategoryType.GeographyComprehensive => "地理综合",
                SubCategoryType.BiologyConcept => "生物概念",
                SubCategoryType.BiologyExperiment => "生物实验",
                SubCategoryType.BiologyPhenomenon => "生物现象",
                SubCategoryType.BiologyComprehensive => "生物综合",
                _ => ""
            };
        }

        public string GetDefaultWordBankFile(SubCategoryType subCategory)
        {
            return _categoryFileMap.GetValueOrDefault(subCategory, "");
        }

        public void SaveUserContent(UserContent content)
        {
            try
            {
                var userContentDir = Path.Combine(_appPaths.DataDir, "UserContent");
                Directory.CreateDirectory(userContentDir);

                var safeUserId = SanitizeFileName(content.UserId);
                var safeId = SanitizeFileName(content.Id.ToString());
                var filePath = Path.Combine(userContentDir, $"{safeUserId}_{safeId}.json");
                JsonHelper.SaveToFile(filePath, content);
                _logger.LogInformation("Saved user content: {Title}", content.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save user content");
            }
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unnamed";

            var invalid = Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }

        private string GetFilePath(SubCategoryType subCategory, string wordBankFile)
        {
            if (!string.IsNullOrWhiteSpace(wordBankFile))
            {
                return Path.Combine(_appPaths.DataDir, wordBankFile);
            }
            return Path.Combine(_appPaths.DataDir, _categoryFileMap.GetValueOrDefault(subCategory, "data.json"));
        }

        private bool IsPathSafe(string filePath)
        {
            try
            {
                var dataDir = new DirectoryInfo(_appPaths.DataDir).FullName;
                var fullPath = new FileInfo(filePath).FullName;
                return fullPath.Equals(dataDir, StringComparison.OrdinalIgnoreCase)
                    || fullPath.StartsWith(dataDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private class CacheEntry
        {
            public List<LearningItem> Items { get; set; } = new List<LearningItem>();
            public string FilePath { get; set; } = string.Empty;
            public DateTime FileLastWriteTime { get; set; }
            public DateTime LoadTime { get; set; }
            public DateTime LastAccessTime { get; set; }

            public bool IsValid
            {
                get
                {
                    try
                    {
                        if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
                            return false;

                        var currentLastWrite = File.GetLastWriteTime(FilePath);
                        return currentLastWrite <= FileLastWriteTime;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            public bool IsExpired => (DateTime.Now - LastAccessTime).TotalMinutes > CacheSlidingExpirationMinutes;
        }
    }
}