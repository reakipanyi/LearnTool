using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Persistence;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 笔记服务实现
    /// 使用IDataPersistenceService持久化笔记数据
    /// </summary>
    public class NoteService : INoteService
    {
        private readonly ILogger<NoteService> _logger;
        private readonly IEventBus? _eventBus;
        private readonly IDataPersistenceService _persistenceService;
        private readonly string _notesDir;
        private readonly object _lock = new object();

        public NoteService(
            ILogger<NoteService> logger,
            IDataPersistenceService persistenceService,
            IEventBus? eventBus = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _eventBus = eventBus;
            _notesDir = AppPaths.NotesDir;
            EnsureDirectoryExists();
            MigrateFromOldLocation();
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_notesDir))
            {
                Directory.CreateDirectory(_notesDir);
            }
        }

        private void MigrateFromOldLocation()
        {
            var oldDir = Path.Combine(AppPaths.UsersDir, "notes");
            if (!Directory.Exists(oldDir)) return;

            try
            {
                foreach (var file in Directory.EnumerateFiles(oldDir))
                {
                    var fileName = Path.GetFileName(file);
                    var newPath = Path.Combine(_notesDir, fileName);
                    if (!File.Exists(newPath))
                    {
                        File.Move(file, newPath);
                    }
                }

                Directory.Delete(oldDir);
                _logger.LogInformation("迁移笔记数据从旧位置完成");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "迁移笔记数据失败");
            }
        }

        private string GetUserNotesPath(string userId)
        {
            return Path.Combine(_notesDir, $"{userId}_notes.json");
        }

        private List<NoteItem> LoadNotes(string userId)
        {
            try
            {
                var path = GetUserNotesPath(userId);
                var result = _persistenceService.LoadJsonFile<List<NoteItem>>(path);
                return result ?? new List<NoteItem>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载笔记数据失败，返回空列表");
                return new List<NoteItem>();
            }
        }

        private void SaveNotes(string userId, List<NoteItem> notes)
        {
            try
            {
                var path = GetUserNotesPath(userId);
                _persistenceService.SaveJsonFile(path, notes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存笔记数据失败");
                throw;
            }
        }

        /// <inheritdoc/>
        public void AddNote(string userId, NoteItem note)
        {
            lock (_lock)
            {
                try
                {
                    var notes = LoadNotes(userId);
                    note.Id = Guid.NewGuid().ToString();
                    note.UserId = userId;
                    note.CreatedAt = DateTime.Now;
                    note.UpdatedAt = DateTime.Now;
                    notes.Add(note);
                    SaveNotes(userId, notes);
                    _logger.LogInformation("笔记已添加: {Title}", note.Title);

                    // 发布笔记添加事件
                    if (_eventBus != null)
                    {
                        _eventBus.Publish(new NoteAddedEvent
                        {
                            UserId = userId,
                            NoteId = note.Id,
                            NoteTitle = note.Title,
                            RelatedType = note.RelatedType,
                            RelatedItemId = note.RelatedItemId,
                            AddedAt = DateTime.Now
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "添加笔记失败");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void UpdateNote(string userId, NoteItem note)
        {
            lock (_lock)
            {
                try
                {
                    var notes = LoadNotes(userId);
                    var existing = notes.FirstOrDefault(n => n.Id == note.Id);
                    if (existing != null)
                    {
                        existing.Title = note.Title;
                        existing.Content = note.Content;
                        existing.Category = note.Category;
                        existing.Tags = note.Tags;
                        existing.Importance = note.Importance;
                        existing.Color = note.Color;
                        existing.UpdatedAt = DateTime.Now;
                        SaveNotes(userId, notes);
                        _logger.LogInformation($"笔记已更新: {note.Title}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "更新笔记失败");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void DeleteNote(string userId, string noteId)
        {
            lock (_lock)
            {
                try
                {
                    var notes = LoadNotes(userId);
                    var noteToRemove = notes.FirstOrDefault(n => n.Id == noteId);
                    if (noteToRemove != null)
                    {
                        notes.Remove(noteToRemove);
                        SaveNotes(userId, notes);
                        _logger.LogInformation($"笔记已删除: {noteToRemove.Title}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "删除笔记失败");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public NoteItem? GetNote(string userId, string noteId)
        {
            try
            {
                var notes = LoadNotes(userId);
                return notes.FirstOrDefault(n => n.Id == noteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取笔记失败");
                return null;
            }
        }

        /// <inheritdoc/>
        public List<NoteItem> GetNotes(string userId, string category = "", string tag = "")
        {
            try
            {
                var notes = LoadNotes(userId);

                if (!string.IsNullOrWhiteSpace(category))
                {
                    notes = notes.Where(n => n.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(tag))
                {
                    notes = notes.Where(n => n.Tags != null &&
                        n.Tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase))).ToList();
                }

                return notes.OrderByDescending(n => n.UpdatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取笔记列表失败");
                return new List<NoteItem>();
            }
        }

        /// <inheritdoc/>
        public List<NoteItem> SearchNotes(string userId, string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return new List<NoteItem>();

                var notes = LoadNotes(userId);
                keyword = keyword.Trim().ToLower();

                return notes.Where(n =>
                    (!string.IsNullOrWhiteSpace(n.Title) && n.Title.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(n.Content) && n.Content.ToLower().Contains(keyword)) ||
                    (n.Tags != null && n.Tags.Any(t => t.ToLower().Contains(keyword)))
                ).OrderByDescending(n => n.UpdatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索笔记失败");
                return new List<NoteItem>();
            }
        }

        /// <inheritdoc/>
        public List<NoteItem> GetRelatedNotes(string userId, string relatedType, string relatedItemId)
        {
            try
            {
                var notes = LoadNotes(userId);
                return notes.Where(n =>
                    n.RelatedType == relatedType && n.RelatedItemId == relatedItemId
                ).OrderByDescending(n => n.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取关联笔记失败");
                return new List<NoteItem>();
            }
        }

        /// <inheritdoc/>
        public void SetFavorite(string userId, string noteId, bool isFavorite)
        {
            lock (_lock)
            {
                try
                {
                    var notes = LoadNotes(userId);
                    var note = notes.FirstOrDefault(n => n.Id == noteId);
                    if (note != null)
                    {
                        note.IsFavorite = isFavorite;
                        note.UpdatedAt = DateTime.Now;
                        SaveNotes(userId, notes);
                        _logger.LogInformation($"笔记收藏状态已更新: {isFavorite}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "更新收藏状态失败");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public List<NoteItem> GetFavoriteNotes(string userId)
        {
            try
            {
                var notes = LoadNotes(userId);
                return notes.Where(n => n.IsFavorite).OrderByDescending(n => n.UpdatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取收藏笔记失败");
                return new List<NoteItem>();
            }
        }

        /// <inheritdoc/>
        public List<string> GetAllCategories(string userId)
        {
            try
            {
                var notes = LoadNotes(userId);
                return notes
                    .Where(n => !string.IsNullOrWhiteSpace(n.Category))
                    .Select(n => n.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取分类列表失败");
                return new List<string>();
            }
        }

        /// <inheritdoc/>
        public List<string> GetAllTags(string userId)
        {
            try
            {
                var notes = LoadNotes(userId);
                var allTags = new HashSet<string>();
                foreach (var note in notes)
                {
                    if (note.Tags != null)
                    {
                        foreach (var tag in note.Tags)
                        {
                            if (!string.IsNullOrWhiteSpace(tag))
                            {
                                allTags.Add(tag);
                            }
                        }
                    }
                }
                return allTags.OrderBy(t => t).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取标签列表失败");
                return new List<string>();
            }
        }

        /// <inheritdoc/>
        public void MarkAsReviewed(string userId, string noteId)
        {
            lock (_lock)
            {
                try
                {
                    var notes = LoadNotes(userId);
                    var note = notes.FirstOrDefault(n => n.Id == noteId);
                    if (note != null)
                    {
                        note.LastReviewedAt = DateTime.Now;
                        note.ReviewCount++;
                        note.UpdatedAt = DateTime.Now;
                        SaveNotes(userId, notes);
                        _logger.LogInformation($"笔记已标记为已复习: {note.Title}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "标记复习失败");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public List<NoteItem> GetNotesForReview(string userId, int days = 7)
        {
            try
            {
                var notes = LoadNotes(userId);
                var cutoffDate = DateTime.Now.AddDays(-days);
                return notes.Where(n =>
                    !n.LastReviewedAt.HasValue || n.LastReviewedAt < cutoffDate
                ).OrderBy(n => n.LastReviewedAt ?? DateTime.MinValue).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取待复习笔记失败");
                return new List<NoteItem>();
            }
        }

        /// <inheritdoc/>
        public int GetNoteCount(string userId)
        {
            try
            {
                var notes = LoadNotes(userId);
                return notes.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取笔记数量失败");
                return 0;
            }
        }

        /// <inheritdoc/>
        public void ExportNotes(string userId, string filePath, string format = "txt")
        {
            try
            {
                var notes = LoadNotes(userId);
                string content;

                switch (format.ToLower())
                {
                    case "md":
                    case "markdown":
                        content = ExportAsMarkdown(notes);
                        break;
                    default:
                        content = ExportAsPlainText(notes);
                        break;
                }

                File.WriteAllText(filePath, content);
                _logger.LogInformation("笔记已导出: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出笔记失败");
                throw;
            }
        }

        /// <inheritdoc/>
        public (List<NoteItem> items, int total) GetNotesPaged(string userId, int page, int pageSize, string category = "", string tag = "")
        {
            try
            {
                var allNotes = LoadNotes(userId);

                // 应用筛选
                if (!string.IsNullOrWhiteSpace(category))
                {
                    allNotes = allNotes.Where(n => n.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(tag))
                {
                    allNotes = allNotes.Where(n => n.Tags != null && n.Tags.Contains(tag)).ToList();
                }

                int total = allNotes.Count;
                var pagedNotes = allNotes
                    .OrderByDescending(n => n.UpdatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return (pagedNotes, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页获取笔记失败");
                return (new List<NoteItem>(), 0);
            }
        }

        /// <inheritdoc/>
        public void BatchDelete(string userId, List<string> noteIds)
        {
            if (string.IsNullOrWhiteSpace(userId) || noteIds == null || noteIds.Count == 0)
                return;

            lock (_lock)
            {
                try
                {
                    var notes = LoadNotes(userId);
                    var toRemove = notes.Where(n => noteIds.Contains(n.Id)).ToList();
                    notes.RemoveAll(n => noteIds.Contains(n.Id));
                    SaveNotes(userId, notes);
                    _logger.LogInformation("批量删除笔记: {Count} 条", toRemove.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量删除笔记失败");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void BatchMove(string userId, List<string> noteIds, string targetCategory)
        {
            if (string.IsNullOrWhiteSpace(userId) || noteIds == null || noteIds.Count == 0)
                return;

            lock (_lock)
            {
                try
                {
                    var notes = LoadNotes(userId);
                    bool changed = false;

                    foreach (var note in notes)
                    {
                        if (noteIds.Contains(note.Id))
                        {
                            note.Category = targetCategory;
                            note.UpdatedAt = DateTime.Now;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        SaveNotes(userId, notes);
                        _logger.LogInformation("批量移动笔记到分类 {Category}: {Count} 条", targetCategory, noteIds.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量移动笔记失败");
                    throw;
                }
            }
        }

        #region 私有方法

        private string ExportAsPlainText(List<NoteItem> notes)
        {
            var lines = new List<string>
            {
                "=== 学习笔记导出 ===",
                $"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"笔记总数: {notes.Count}",
                "",
                new string('=', 50),
                ""
            };

            foreach (var note in notes.OrderByDescending(n => n.UpdatedAt))
            {
                lines.Add($"【标题】{note.Title}");
                if (!string.IsNullOrWhiteSpace(note.Category))
                    lines.Add($"【分类】{note.Category}");
                if (note.Tags != null && note.Tags.Count > 0)
                    lines.Add($"【标签】{string.Join(", ", note.Tags)}");
                lines.Add($"【创建时间】{note.CreatedAt:yyyy-MM-dd HH:mm}");
                lines.Add($"【更新时间】{note.UpdatedAt:yyyy-MM-dd HH:mm}");
                if (note.IsFavorite)
                    lines.Add("【收藏】⭐");
                lines.Add("");
                lines.Add(note.Content);
                lines.Add("");
                lines.Add(new string('-', 50));
                lines.Add("");
            }

            return string.Join("\n", lines);
        }

        private string ExportAsMarkdown(List<NoteItem> notes)
        {
            var lines = new List<string>
            {
                "# 学习笔记导出",
                "",
                $"> 导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"> 笔记总数: {notes.Count}",
                "",
                "---",
                ""
            };

            foreach (var note in notes.OrderByDescending(n => n.UpdatedAt))
            {
                lines.Add($"## {note.Title}");
                lines.Add("");

                if (!string.IsNullOrWhiteSpace(note.Category))
                    lines.Add($"- **分类**: {note.Category}");
                if (note.Tags != null && note.Tags.Count > 0)
                    lines.Add($"- **标签**: {string.Join(", ", note.Tags)}");
                lines.Add($"- **创建时间**: {note.CreatedAt:yyyy-MM-dd HH:mm}");
                lines.Add($"- **更新时间**: {note.UpdatedAt:yyyy-MM-dd HH:mm}");
                if (note.IsFavorite)
                    lines.Add("- **收藏**: ⭐");

                lines.Add("");
                lines.Add("### 内容");
                lines.Add("");
                lines.Add(note.Content);
                lines.Add("");
                lines.Add("---");
                lines.Add("");
            }

            return string.Join("\n", lines);
        }

        #endregion
    }
}
