using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LearningAssistant.Models.Audio;

namespace LearningAssistant.Services.Audio;

/// <summary>
/// 自动断句缓存（指纹键）：校验 SchemaVersion / Fingerprint / Options 签名，
/// 原子写、进程内按路径串行，损坏文件自愈，失败负缓存短 TTL。
/// </summary>
public sealed class AudioTimelineStore
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> WriteGates =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Func<string> _directoryProvider;

    /// <summary>按当前用户目录懒解析；切换用户后自动读写对应目录。</summary>
    public AudioTimelineStore(Func<string> directoryProvider)
    {
        _directoryProvider = directoryProvider ?? throw new ArgumentNullException(nameof(directoryProvider));
    }

    /// <summary>固定目录：测试与单用户场景使用。</summary>
    public AudioTimelineStore(string directoryPath)
        : this(CreateFixedProvider(directoryPath))
    {
    }

    private static Func<string> CreateFixedProvider(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentException("时间轴缓存目录不能为空", nameof(directoryPath));

        string fixedPath = directoryPath;
        return () => fixedPath;
    }

    /// <summary>音频指纹：大小|mtime|时长；同一文件跨书/跨目录复用同一缓存。</summary>
    public static string ComputeFingerprint(string audioPath, int durationMs)
    {
        long length = 0;
        long ticks = 0;
        try
        {
            var info = new FileInfo(audioPath);
            if (info.Exists)
            {
                length = info.Length;
                ticks = info.LastWriteTimeUtc.Ticks;
            }
        }
        catch (Exception)
        {
            // 取不到文件信息时退回 0，仍可与时长组合成可用键
        }

        return $"{length}|{ticks}|{durationMs}";
    }

    /// <summary>参数签名：任何阈值变化都视为缓存未命中。</summary>
    public static string ComputeOptionsSignature(AudioTimelineOptions? options)
    {
        options ??= new AudioTimelineOptions();
        string raw = string.Join("|",
            options.SentenceGapMs,
            options.FineGapMs,
            options.MinSpeechMs,
            options.MinSentenceMs,
            options.MaxSentenceMs,
            options.PaddingMs,
            options.NoiseMultiplier.ToString("R"),
            options.MinAbsoluteRms.ToString("R"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    public string GetFilePath(string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
            throw new ArgumentException("指纹不能为空", nameof(fingerprint));

        return Path.Combine(ResolveDirectory(), Sanitize(fingerprint) + ".timeline.json");
    }

    /// <summary>读取并校验缓存；不存在、损坏、版本/指纹/参数不符时返回 null。</summary>
    public AudioSentenceTimeline? Load(string fingerprint, AudioTimelineOptions? options)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
            return null;

        string file;
        try
        {
            file = GetFilePath(fingerprint);
        }
        catch (Exception)
        {
            return null;
        }

        if (!File.Exists(file))
            return null;

        try
        {
            var timeline = JsonSerializer.Deserialize<AudioSentenceTimeline>(File.ReadAllText(file), JsonOptions);
            if (timeline == null
                || timeline.SchemaVersion != CurrentSchemaVersion
                || !string.Equals(timeline.Fingerprint, fingerprint, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(
                    ComputeOptionsSignature(timeline.Analysis?.Options),
                    ComputeOptionsSignature(options),
                    StringComparison.Ordinal))
            {
                TryDelete(file);
                return null;
            }

            AssignIndexes(timeline.Sentences);
            TouchAccess(file);
            return timeline;
        }
        catch (Exception)
        {
            TryDelete(file);
            return null;
        }
    }

    /// <summary>
    /// 按“最后使用时间”清理自动缓存：命中读取时刷新文件写入时间，长期未用的条目在此删除。
    /// 只处理本目录下的 *.timeline.json 与 *.failed.json，不触碰手动覆盖层。
    /// </summary>
    public int PruneExpired(TimeSpan maxAge, DateTime? nowUtc = null)
    {
        if (maxAge <= TimeSpan.Zero) return 0;

        string directory;
        try
        {
            directory = ResolveDirectory();
        }
        catch (Exception)
        {
            return 0;
        }

        if (!Directory.Exists(directory)) return 0;

        DateTime now = (nowUtc ?? DateTime.UtcNow).ToUniversalTime();
        DateTime cutoff = now - maxAge;
        int removed = 0;

        foreach (var pattern in new[] { "*.timeline.json", "*.failed.json" })
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(directory, pattern);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (string file in files)
            {
                // 正在写入的 .part 不参与清理，避免与原子替换竞争
                if (file.EndsWith(".part", StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    var info = new FileInfo(file);
                    if (!info.Exists) continue;
                    if (info.LastWriteTimeUtc >= cutoff) continue;
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                        removed++;
                    }
                }
                catch (Exception)
                {
                    // 单个文件被占用时跳过，下次再清
                }
            }
        }

        return removed;
    }

    /// <summary>刷新“最后使用时间”，用于 LRU 清理；失败不影响读取。</summary>
    private static void TouchAccess(string file)
    {
        try
        {
            File.SetLastWriteTimeUtc(file, DateTime.UtcNow);
        }
        catch (Exception)
        {
            // 只读介质或权限不足时忽略
        }
    }

    /// <summary>写入缓存：先写 .part 再原子替换。</summary>
    public void Save(AudioSentenceTimeline timeline)
    {
        if (timeline == null) throw new ArgumentNullException(nameof(timeline));
        if (string.IsNullOrWhiteSpace(timeline.Fingerprint))
            throw new ArgumentException("时间轴缺少音频指纹", nameof(timeline));

        timeline.SchemaVersion = CurrentSchemaVersion;
        AssignIndexes(timeline.Sentences);

        string file = GetFilePath(timeline.Fingerprint);
        var gate = WriteGates.GetOrAdd(file, _ => new SemaphoreSlim(1, 1));
        gate.Wait();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            string temp = file + ".part";
            File.WriteAllText(temp, JsonSerializer.Serialize(timeline, JsonOptions), Encoding.UTF8);
            if (File.Exists(file))
                File.Replace(temp, file, null);
            else
                File.Move(temp, file);

            ClearFailure(timeline.Fingerprint);
        }
        finally
        {
            gate.Release();
        }
    }

    public void Delete(string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
            return;

        TryDelete(GetFilePath(fingerprint));
        ClearFailure(fingerprint);
    }

    #region 负缓存

    /// <summary>分析失败的短 TTL 负缓存：在冷却期内不再重试。</summary>
    public bool IsInFailureCooldown(string fingerprint, out DateTime retryAfterUtc)
    {
        retryAfterUtc = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(fingerprint))
            return false;

        string file = GetFailureFilePath(fingerprint);
        if (!File.Exists(file))
            return false;

        try
        {
            var entry = JsonSerializer.Deserialize<FailureEntry>(File.ReadAllText(file), JsonOptions);
            if (entry == null)
                return false;

            retryAfterUtc = entry.RetryAfterUtc;
            return entry.RetryAfterUtc > DateTime.UtcNow;
        }
        catch (Exception)
        {
            TryDelete(file);
            return false;
        }
    }

    public void SaveFailure(string fingerprint, TimeSpan ttl, string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
            return;

        try
        {
            string file = GetFailureFilePath(fingerprint);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var entry = new FailureEntry
            {
                Fingerprint = fingerprint,
                RetryAfterUtc = DateTime.UtcNow + ttl,
                Reason = reason
            };
            string temp = file + ".part";
            File.WriteAllText(temp, JsonSerializer.Serialize(entry, JsonOptions), Encoding.UTF8);
            if (File.Exists(file))
                File.Replace(temp, file, null);
            else
                File.Move(temp, file);
        }
        catch (Exception)
        {
            // 负缓存失败不应影响播放
        }
    }

    public void ClearFailure(string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
            return;

        TryDelete(GetFailureFilePath(fingerprint));
    }

    private sealed class FailureEntry
    {
        public string Fingerprint { get; set; } = "";

        public DateTime RetryAfterUtc { get; set; }

        public string? Reason { get; set; }
    }

    #endregion

    #region 辅助

    private string ResolveDirectory()
    {
        string directory = _directoryProvider();
        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException("时间轴缓存目录未配置");

        return directory;
    }

    private string GetFailureFilePath(string fingerprint)
        => Path.Combine(ResolveDirectory(), Sanitize(fingerprint) + ".failed.json");

    private static void AssignIndexes(List<AudioSentenceSpan>? sentences)
    {
        if (sentences == null) return;

        int index = 1;
        foreach (var span in sentences.OrderBy(span => span.StartMs).ThenBy(span => span.EndMs))
            span.Index = index++;
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (char ch in value)
            builder.Append(invalid.Contains(ch) ? '_' : ch);

        var result = builder.ToString().Trim();
        if (result.Length > 120) result = result[..120];
        return string.IsNullOrEmpty(result) ? "unnamed" : result;
    }

    private static void TryDelete(string file)
    {
        try
        {
            if (File.Exists(file)) File.Delete(file);
        }
        catch (Exception)
        {
            // 删除失败只影响本次自愈，下次仍会重试
        }
    }

    #endregion
}
