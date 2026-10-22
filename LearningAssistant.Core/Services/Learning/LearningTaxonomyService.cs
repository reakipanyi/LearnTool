using LearningAssistant.Common;
using LearningAssistant.Data.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    /// <summary>知识点树节点（按用户隔离）。</summary>
    public sealed class KnowledgePointNode
    {
        public string Id { get; init; } = string.Empty;
        public string? ParentId { get; init; }
        public string Name { get; init; } = string.Empty;
        public SubjectType Subject { get; init; }
        public AgeRange AgeRange { get; init; }
        public int SortOrder { get; init; }
        public List<KnowledgePointNode> Children { get; } = new List<KnowledgePointNode>();
    }

    public interface ILearningTaxonomyService
    {
        IReadOnlyList<KnowledgePointNode> GetTree(SubjectType subject, string userId);
        KnowledgePointNode Create(string userId, string? parentId, string name, SubjectType subject, AgeRange ageRange = AgeRange.All);
        bool Rename(string id, string newName);
        bool Merge(string sourceId, string targetId);
        void Assign(string userId, string itemKey, string? knowledgePointId);
        IReadOnlyDictionary<string, string> GetAssignmentMap(string userId);
        string? GetPath(string knowledgePointId);
        event EventHandler? TaxonomyChanged;
    }

    /// <summary>
    /// 知识点树与学习项分配的 SQLite 实现。
    /// 读取走内存缓存（按用户），写操作完成后整体失效，避免每次加载队列都查库。
    /// </summary>
    public sealed class LearningTaxonomyService : ILearningTaxonomyService
    {
        /// <summary>虚拟知识点：用于筛选“未分类”学习项，不落库。</summary>
        public const string UnclassifiedId = "__unclassified__";

        private sealed class CacheEntry
        {
            public List<KnowledgePointEntity> Nodes { get; } = new List<KnowledgePointEntity>();
            public Dictionary<string, string> Assignments { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<LearningTaxonomyService>? _logger;
        private readonly object _cacheLock = new object();
        private readonly Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>(StringComparer.Ordinal);

        public LearningTaxonomyService(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<LearningTaxonomyService>? logger = null)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public event EventHandler? TaxonomyChanged;

        public IReadOnlyList<KnowledgePointNode> GetTree(SubjectType subject, string userId)
        {
            var entry = GetCache(userId);
            var nodes = entry.Nodes
                .Where(n => string.Equals(n.Subject, subject.ToString(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n.SortOrder)
                .ThenBy(n => n.Name, StringComparer.Ordinal)
                .ToList();

            var map = nodes.ToDictionary(
                n => n.Id,
                n => new KnowledgePointNode
                {
                    Id = n.Id,
                    ParentId = n.ParentId,
                    Name = n.Name,
                    Subject = subject,
                    AgeRange = (AgeRange)n.AgeRange,
                    SortOrder = n.SortOrder
                },
                StringComparer.Ordinal);

            var roots = new List<KnowledgePointNode>();
            foreach (var entity in nodes)
            {
                var node = map[entity.Id];
                if (!string.IsNullOrEmpty(entity.ParentId) && map.TryGetValue(entity.ParentId, out var parent))
                    parent.Children.Add(node);
                else
                    roots.Add(node);
            }

            return roots;
        }

        public KnowledgePointNode Create(string userId, string? parentId, string name, SubjectType subject, AgeRange ageRange = AgeRange.All)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("用户不能为空", nameof(userId));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("知识点名称不能为空", nameof(name));

            var trimmedName = name.Trim();
            using var db = _dbContextFactory.CreateDbContext();

            if (!string.IsNullOrWhiteSpace(parentId))
            {
                var parent = db.KnowledgePoints.FirstOrDefault(k => k.Id == parentId && k.UserId == userId && k.IsActive);
                if (parent == null)
                    throw new ArgumentException("父知识点不存在或不属于当前用户", nameof(parentId));
            }

            var siblingMaxOrder = db.KnowledgePoints
                .Where(k => k.UserId == userId && k.ParentId == parentId && k.IsActive)
                .Select(k => (int?)k.SortOrder)
                .Max() ?? 0;

            var entity = new KnowledgePointEntity
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = userId,
                ParentId = string.IsNullOrWhiteSpace(parentId) ? null : parentId,
                Name = trimmedName,
                Subject = subject.ToString(),
                AgeRange = (int)ageRange,
                SortOrder = siblingMaxOrder + 1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsActive = true,
                RowVersion = 1
            };

            db.KnowledgePoints.Add(entity);
            db.SaveChanges();
            Invalidate(userId);

            return new KnowledgePointNode
            {
                Id = entity.Id,
                ParentId = entity.ParentId,
                Name = entity.Name,
                Subject = subject,
                AgeRange = ageRange,
                SortOrder = entity.SortOrder
            };
        }

        public bool Rename(string id, string newName)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(newName)) return false;

            using var db = _dbContextFactory.CreateDbContext();
            var entity = db.KnowledgePoints.FirstOrDefault(k => k.Id == id && k.IsActive);
            if (entity == null) return false;

            entity.Name = newName.Trim();
            entity.UpdatedAt = DateTime.Now;
            entity.RowVersion++;
            db.SaveChanges();

            Invalidate(entity.UserId);
            return true;
        }

        public bool Merge(string sourceId, string targetId)
        {
            if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(targetId) || sourceId == targetId)
                return false;

            using var db = _dbContextFactory.CreateDbContext();
            var source = db.KnowledgePoints.FirstOrDefault(k => k.Id == sourceId && k.IsActive);
            var target = db.KnowledgePoints.FirstOrDefault(k => k.Id == targetId && k.IsActive);
            if (source == null || target == null || source.UserId != target.UserId) return false;

            using var transaction = db.Database.BeginTransaction();
            try
            {
                // 子节点挂到目标节点下，保持树结构不断裂
                var children = db.KnowledgePoints
                    .Where(k => k.UserId == source.UserId && k.ParentId == sourceId && k.IsActive)
                    .ToList();
                foreach (var child in children)
                {
                    child.ParentId = targetId;
                    child.UpdatedAt = DateTime.Now;
                    child.RowVersion++;
                }

                // 学习项分配去重：已有目标分配的源分配直接删除，其余改挂目标
                var targetItemKeys = db.KnowledgePointAssignments
                    .Where(a => a.UserId == source.UserId && a.KnowledgePointId == targetId && a.IsActive)
                    .Select(a => a.ItemKey)
                    .ToHashSet(StringComparer.Ordinal);

                var sourceAssignments = db.KnowledgePointAssignments
                    .Where(a => a.UserId == source.UserId && a.KnowledgePointId == sourceId && a.IsActive)
                    .ToList();

                foreach (var assignment in sourceAssignments)
                {
                    if (targetItemKeys.Contains(assignment.ItemKey))
                    {
                        db.KnowledgePointAssignments.Remove(assignment);
                        continue;
                    }

                    assignment.KnowledgePointId = targetId;
                    assignment.UpdatedAt = DateTime.Now;
                    assignment.RowVersion++;
                }

                source.IsActive = false;
                source.UpdatedAt = DateTime.Now;
                source.RowVersion++;

                db.SaveChanges();
                transaction.Commit();

                Invalidate(source.UserId);
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger?.LogError(ex, "合并知识点失败: {Source} -> {Target}", sourceId, targetId);
                return false;
            }
        }

        public void Assign(string userId, string itemKey, string? knowledgePointId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(itemKey)) return;

            using var db = _dbContextFactory.CreateDbContext();
            var existing = db.KnowledgePointAssignments
                .FirstOrDefault(a => a.UserId == userId && a.ItemKey == itemKey);

            var normalizedKnowledgePointId = string.IsNullOrWhiteSpace(knowledgePointId)
                || string.Equals(knowledgePointId, UnclassifiedId, StringComparison.Ordinal)
                ? null
                : knowledgePointId.Trim();

            if (normalizedKnowledgePointId == null)
            {
                if (existing != null)
                {
                    db.KnowledgePointAssignments.Remove(existing);
                    db.SaveChanges();
                    Invalidate(userId);
                }
                return;
            }

            var pointExists = db.KnowledgePoints.Any(k =>
                k.Id == normalizedKnowledgePointId && k.UserId == userId && k.IsActive);
            if (!pointExists)
                throw new ArgumentException("知识点不存在或不属于当前用户", nameof(knowledgePointId));

            if (existing == null)
            {
                db.KnowledgePointAssignments.Add(new KnowledgePointAssignmentEntity
                {
                    UserId = userId,
                    ItemKey = itemKey,
                    KnowledgePointId = normalizedKnowledgePointId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsActive = true,
                    RowVersion = 1
                });
            }
            else
            {
                existing.KnowledgePointId = normalizedKnowledgePointId;
                existing.UpdatedAt = DateTime.Now;
                existing.IsActive = true;
                existing.RowVersion++;
            }

            db.SaveChanges();
            Invalidate(userId);
        }

        public IReadOnlyDictionary<string, string> GetAssignmentMap(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new Dictionary<string, string>(StringComparer.Ordinal);

            return GetCache(userId).Assignments;
        }

        public string? GetPath(string knowledgePointId)
        {
            if (string.IsNullOrWhiteSpace(knowledgePointId) || knowledgePointId == UnclassifiedId)
                return null;

            using var db = _dbContextFactory.CreateDbContext();
            var current = db.KnowledgePoints.FirstOrDefault(k => k.Id == knowledgePointId && k.IsActive);
            if (current == null) return null;

            var names = new List<string>();
            var guard = 0;
            while (current != null && guard++ < 32)
            {
                names.Add(current.Name);
                if (string.IsNullOrEmpty(current.ParentId)) break;
                var parentId = current.ParentId;
                current = db.KnowledgePoints.FirstOrDefault(k => k.Id == parentId && k.IsActive);
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private CacheEntry GetCache(string userId)
        {
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(userId, out var cached))
                    return cached;
            }

            var loaded = LoadFromDatabase(userId);

            lock (_cacheLock)
            {
                if (_cache.TryGetValue(userId, out var existing))
                    return existing;

                _cache[userId] = loaded;
                return loaded;
            }
        }

        private CacheEntry LoadFromDatabase(string userId)
        {
            var entry = new CacheEntry();
            try
            {
                using var db = _dbContextFactory.CreateDbContext();

                entry.Nodes.AddRange(db.KnowledgePoints
                    .Where(k => k.UserId == userId && k.IsActive)
                    .ToList());

                var activeNodeIds = entry.Nodes.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
                var assignments = db.KnowledgePointAssignments
                    .Where(a => a.UserId == userId && a.IsActive)
                    .ToList();

                foreach (var assignment in assignments)
                {
                    if (activeNodeIds.Contains(assignment.KnowledgePointId))
                        entry.Assignments[assignment.ItemKey] = assignment.KnowledgePointId;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "加载知识点缓存失败: {UserId}", userId);
            }

            return entry;
        }

        private void Invalidate(string userId)
        {
            lock (_cacheLock)
            {
                _cache.Remove(userId);
            }

            TaxonomyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
