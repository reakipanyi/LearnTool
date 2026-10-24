using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using LearningAssistant.Common;
using LearningAssistant.Models.Shadowing;

namespace LearningAssistant.Services.Shadowing
{
    /// <summary>
    /// 原声切片 / 跟读 take / 合成成品 / 练习进度的持久化存储。
    /// 目录布局：Shadowings/{user}/{pdfHash}/p{page}.segments.json + slices/ + takes/ + composed/ + progress.json。
    /// segments 与 progress 共用一把按 user + pdf 的进程内锁，读改写串行化。
    /// </summary>
    public sealed class ShadowingSegmentStore
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

        private readonly string _rootDir;
        private readonly string _userId;

        public ShadowingSegmentStore(string? rootDir = null, string? userId = null)
        {
            _rootDir = Path.GetFullPath(rootDir ?? AppPaths.ShadowingDir);
            _userId = string.IsNullOrWhiteSpace(userId) ? AppPaths.GetCurrentUserId() : userId;
        }

        /// <summary>该 PDF 的存储根目录（所有页共用）。</summary>
        public string GetDocumentDirectory(string pdfPath)
            => Path.Combine(_rootDir, _userId, GetDocumentHash(pdfPath));

        public string GetSegmentsPath(string pdfPath, int pageIndex)
            => Path.Combine(GetDocumentDirectory(pdfPath), $"p{pageIndex:000}.segments.json");

        public string GetProgressPath(string pdfPath)
            => Path.Combine(GetDocumentDirectory(pdfPath), "progress.json");

        /// <summary>把相对 Shadowings 根目录的路径解析为绝对路径；已是绝对路径时原样返回。</summary>
        public string ResolvePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return string.Empty;
            return Path.IsPathRooted(relativePath)
                ? relativePath
                : Path.Combine(_rootDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>把绝对路径转为相对 Shadowings 根目录的路径，便于随存储整体迁移。</summary>
        public string ToRelativePath(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
                return string.Empty;
            string full = Path.GetFullPath(absolutePath);
            return Path.GetRelativePath(_rootDir, full).Replace('\\', '/');
        }

        public static string CreateSegmentId(int pageIndex)
            => $"sg_{pageIndex:000}_{Guid.NewGuid().ToString("N")[..8]}";

        public static string CreateSessionId(DateTime localTime)
            => $"ss_{localTime:yyyyMMdd_HHmm}";

        public static string CreateTakeId(int pageIndex)
            => $"tk_{pageIndex:000}_{Guid.NewGuid().ToString("N")[..8]}";

        /// <summary>切片相对路径：slices/{segmentId}.wav。</summary>
        public string GetSliceRelativePath(string segmentId)
            => $"slices/{segmentId}.wav";

        /// <summary>take 相对路径：takes/{segmentId}_{yyyyMMdd_HHmmss}.wav，重复时追加 _2、_3。</summary>
        public string AllocateTakeRelativePath(string segmentId, DateTime localTime)
        {
            string name = $"{segmentId}_{localTime:yyyyMMdd_HHmmss}";
            string relative = $"takes/{name}.wav";
            int suffix = 2;
            while (File.Exists(ResolvePath(relative)))
                relative = $"takes/{name}_{suffix++}.wav";
            return relative;
        }

        /// <summary>成品相对路径：composed/P{页码3位}_{yyyyMMdd_HHmmss}.wav，重复时追加 _2、_3。</summary>
        public string AllocateComposedRelativePath(int pageIndex, DateTime localTime)
        {
            string name = $"P{pageIndex + 1:000}_{localTime:yyyyMMdd_HHmmss}";
            string relative = $"composed/{name}.wav";
            int suffix = 2;
            while (File.Exists(ResolvePath(relative)))
                relative = $"composed/{name}_{suffix++}.wav";
            return relative;
        }

        /// <summary>源音频指纹：大小 + 修改时间 + 时长；变化时提示“源已变更，切片可能偏移”。</summary>
        public static string BuildFingerprint(string filePath, int durationMs)
        {
            try
            {
                var info = new FileInfo(filePath);
                string raw = $"{info.Length}|{info.LastWriteTimeUtc.Ticks}|{durationMs}";
                return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..16];
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>同步读取本页切片清单（窗口构造期使用，文件很小）。</summary>
        public ShadowingSegmentManifest? LoadSegments(string pdfPath, int pageIndex)
        {
            var gate = GetLock(pdfPath);
            gate.Wait();
            try
            {
                return JsonHelper.LoadFromFile<ShadowingSegmentManifest>(GetSegmentsPath(pdfPath, pageIndex));
            }
            finally
            {
                gate.Release();
            }
        }

        /// <summary>同步读取练习进度（窗口构造期使用，文件很小）。</summary>
        public ShadowingProgress? LoadProgress(string pdfPath)
        {
            var gate = GetLock(pdfPath);
            gate.Wait();
            try
            {
                return JsonHelper.LoadFromFile<ShadowingProgress>(GetProgressPath(pdfPath));
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task<ShadowingSegmentManifest?> LoadSegmentsAsync(
            string pdfPath, int pageIndex, CancellationToken cancellationToken = default)
        {
            var gate = GetLock(pdfPath);
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return JsonHelper.LoadFromFile<ShadowingSegmentManifest>(GetSegmentsPath(pdfPath, pageIndex));
            }
            finally
            {
                gate.Release();
            }
        }

        /// <summary>
        /// 保存本页切片清单：与磁盘上的旧清单按 segment.id 合并（后写覆盖同 id），
        /// 未出现在本次保存里的旧段与旧成品保留，避免多窗口互删。
        /// </summary>
        public async Task<ShadowingSegmentManifest> SaveSegmentsAsync(
            string pdfPath, ShadowingSegmentManifest manifest, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(manifest);

            var gate = GetLock(pdfPath);
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                string path = GetSegmentsPath(pdfPath, manifest.PageIndex);
                var existing = JsonHelper.LoadFromFile<ShadowingSegmentManifest>(path);
                var merged = existing == null ? manifest : Merge(existing, manifest);
                merged.SchemaVersion = 2;
                merged.PageIndex = manifest.PageIndex;
                JsonHelper.SaveToFile(path, merged);
                return merged;
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task<ShadowingProgress?> LoadProgressAsync(
            string pdfPath, CancellationToken cancellationToken = default)
        {
            var gate = GetLock(pdfPath);
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return JsonHelper.LoadFromFile<ShadowingProgress>(GetProgressPath(pdfPath));
            }
            finally
            {
                gate.Release();
            }
        }

        /// <summary>按页合并写入练习进度，其他页记录保持不变。</summary>
        public async Task SaveProgressPageAsync(
            string pdfPath, int pageIndex, string? lastSegmentId, string? mode,
            CancellationToken cancellationToken = default)
        {
            var gate = GetLock(pdfPath);
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                string path = GetProgressPath(pdfPath);
                var progress = JsonHelper.LoadFromFile<ShadowingProgress>(path) ?? new ShadowingProgress();
                progress.Pages[pageIndex.ToString()] = new ShadowingPageProgress
                {
                    LastSegmentId = lastSegmentId,
                    Mode = mode,
                    UpdatedAt = DateTime.Now
                };
                JsonHelper.SaveToFile(path, progress);
            }
            finally
            {
                gate.Release();
            }
        }

        /// <summary>取该 PDF 的目录锁；segments.json 与 progress.json 共用。</summary>
        public SemaphoreSlim GetLock(string pdfPath)
            => _locks.GetOrAdd($"{_rootDir}|{_userId}|{GetDocumentHash(pdfPath)}", _ => new SemaphoreSlim(1, 1));

        private static ShadowingSegmentManifest Merge(ShadowingSegmentManifest existing, ShadowingSegmentManifest incoming)
        {
            var segments = new List<ShadowingSegment>(existing.Segments);
            foreach (var segment in incoming.Segments)
            {
                int index = segments.FindIndex(s => string.Equals(s.Id, segment.Id, StringComparison.Ordinal));
                if (index >= 0)
                    segments[index] = segment;
                else
                    segments.Add(segment);
            }

            var composed = new List<ShadowingComposedAudio>(existing.Composed);
            foreach (var item in incoming.Composed)
            {
                int index = composed.FindIndex(c => string.Equals(c.Path, item.Path, StringComparison.Ordinal));
                if (index >= 0)
                    composed[index] = item;
                else
                    composed.Add(item);
            }

            return new ShadowingSegmentManifest
            {
                SchemaVersion = 2,
                PageIndex = incoming.PageIndex,
                Source = incoming.Source,
                Session = incoming.Session,
                Segments = segments,
                Composed = composed
            };
        }

        private static string GetDocumentHash(string pdfPath)
        {
            string key = Path.GetFullPath(pdfPath);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..16];
        }
    }
}
