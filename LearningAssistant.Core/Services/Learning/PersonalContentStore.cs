using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 用户个人内容库。按用户 + 学科 + 子类别隔离存储，不修改共享系统词库。
    /// </summary>
    public interface IPersonalContentStore
    {
        string GetFilePath(string userId, SubjectType subject, SubCategoryType subCategory);
        IReadOnlyList<LearningItem> Load(string userId, SubjectType subject, SubCategoryType subCategory);
        void Upsert(string userId, SubjectType subject, SubCategoryType subCategory, LearningItem item);
        bool Remove(string userId, SubjectType subject, SubCategoryType subCategory, string canonicalKey);
    }

    public sealed class PersonalContentStore : IPersonalContentStore
    {
        private readonly IAppPaths _appPaths;
        private readonly IContentLoaderService _contentLoaderService;
        private readonly ILogger<PersonalContentStore>? _logger;

        private static readonly ConcurrentDictionary<string, Mutex> _pathMutexes =
            new(StringComparer.OrdinalIgnoreCase);

        public PersonalContentStore(
            IAppPaths appPaths,
            IContentLoaderService contentLoaderService,
            ILogger<PersonalContentStore>? logger = null)
        {
            _appPaths = appPaths ?? throw new ArgumentNullException(nameof(appPaths));
            _contentLoaderService = contentLoaderService ?? throw new ArgumentNullException(nameof(contentLoaderService));
            _logger = logger;
        }

        public string GetFilePath(string userId, SubjectType subject, SubCategoryType subCategory)
        {
            var safeUserId = Sanitize(userId);
            var userDir = _appPaths.GetUserDir(safeUserId);
            return Path.Combine(userDir, "content", subject.ToString(), $"{subCategory}.json");
        }

        public IReadOnlyList<LearningItem> Load(string userId, SubjectType subject, SubCategoryType subCategory)
        {
            var filePath = GetFilePath(userId, subject, subCategory);
            if (!File.Exists(filePath)) return Array.Empty<LearningItem>();

            try
            {
                var json = File.ReadAllText(filePath);
                return JsonHelper.DeserializeLearningItems(json);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load personal content: {FilePath}", filePath);
                return Array.Empty<LearningItem>();
            }
        }

        public void Upsert(string userId, SubjectType subject, SubCategoryType subCategory, LearningItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var filePath = GetFilePath(userId, subject, subCategory);
            WithFileLock(filePath, () =>
            {
                var items = File.Exists(filePath)
                    ? JsonHelper.DeserializeLearningItems(File.ReadAllText(filePath))
                    : new List<LearningItem>();

                var canonicalKey = LearningItemIdentity.GetCanonicalKey(item);
                var existing = items.FirstOrDefault(i =>
                    string.Equals(LearningItemIdentity.GetCanonicalKey(i), canonicalKey, StringComparison.Ordinal));

                if (existing != null)
                {
                    MergeInto(existing, item);
                    LearningItemIdentity.EnsureKey(existing);
                }
                else
                {
                    LearningItemIdentity.EnsureKey(item);
                    items.Add(item);
                }

                JsonHelper.SaveToFile(filePath, items);
                _contentLoaderService.InvalidateFileCache(filePath);
            });
        }

        public bool Remove(string userId, SubjectType subject, SubCategoryType subCategory, string canonicalKey)
        {
            if (string.IsNullOrEmpty(canonicalKey)) return false;

            var filePath = GetFilePath(userId, subject, subCategory);
            var removed = false;

            WithFileLock(filePath, () =>
            {
                if (!File.Exists(filePath)) return;

                var items = JsonHelper.DeserializeLearningItems(File.ReadAllText(filePath));
                var target = items.FirstOrDefault(i =>
                    string.Equals(LearningItemIdentity.GetCanonicalKey(i), canonicalKey, StringComparison.Ordinal));
                if (target == null) return;

                items.Remove(target);
                JsonHelper.SaveToFile(filePath, items);
                _contentLoaderService.InvalidateFileCache(filePath);
                removed = true;
            });

            return removed;
        }

        /// <summary>以新项补全旧项缺失字段，保留旧项 Id 与创建时间。</summary>
        private static void MergeInto(LearningItem target, LearningItem source)
        {
            if (string.IsNullOrWhiteSpace(target.Meaning?.Content) && !string.IsNullOrWhiteSpace(source.Meaning?.Content))
            {
                target.Meaning = source.Meaning;
            }

            if (string.IsNullOrWhiteSpace(target.Example?.Content) && !string.IsNullOrWhiteSpace(source.Example?.Content))
            {
                target.Example = source.Example;
            }

            target.Pronunciation ??= source.Pronunciation;
            target.CharacterFeatures ??= source.CharacterFeatures;
            target.WordFeatures ??= source.WordFeatures;

            var targetProps = ParseProperties(target.ExtendedProperties);
            var sourceProps = ParseProperties(source.ExtendedProperties);
            foreach (var pair in sourceProps)
            {
                if (string.IsNullOrWhiteSpace(pair.Value)) continue;
                if (!targetProps.TryGetValue(pair.Key, out var current) || string.IsNullOrWhiteSpace(current))
                {
                    targetProps[pair.Key] = pair.Value;
                }
            }

            target.ExtendedProperties = System.Text.Json.JsonSerializer.Serialize(targetProps);
            target.UpdatedAt = DateTime.Now;
        }

        private static Dictionary<string, string> ParseProperties(string? json)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(json)) return result;

            try
            {
                var props = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json);
                if (props == null) return result;

                foreach (var pair in props)
                {
                    result[pair.Key] = pair.Value.ValueKind == System.Text.Json.JsonValueKind.String
                        ? pair.Value.GetString() ?? string.Empty
                        : pair.Value.ToString();
                }
            }
            catch
            {
                // 旧数据可能不是合法 JSON，忽略即可，不影响主流程。
            }

            return result;
        }

        private static string Sanitize(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return Constants.DefaultUserId;

            var builder = new StringBuilder(userId.Length);
            foreach (var ch in userId)
            {
                if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_')
                {
                    builder.Append(ch);
                }
            }

            return builder.Length > 0 ? builder.ToString() : Constants.DefaultUserId;
        }

        private void WithFileLock(string filePath, Action action)
        {
            var mutex = _pathMutexes.GetOrAdd(filePath, path => new Mutex(false, BuildMutexName(path)));
            var acquired = false;

            try
            {
                try
                {
                    acquired = mutex.WaitOne(TimeSpan.FromSeconds(5));
                }
                catch (AbandonedMutexException)
                {
                    // 上一个进程异常退出，锁已归当前调用方所有。
                    acquired = true;
                }

                if (!acquired)
                {
                    throw new TimeoutException($"Timed out waiting for personal content lock: {filePath}");
                }

                action();
            }
            finally
            {
                if (acquired)
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        private static string BuildMutexName(string filePath)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(filePath.ToLowerInvariant()));
            return @"Global\LearningAssistantPersonalContent_" + Convert.ToHexString(hash, 0, 8);
        }
    }
}
