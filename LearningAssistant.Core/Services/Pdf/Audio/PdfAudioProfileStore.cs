using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf;

/// <summary>
/// 目录级音频档案存储：同一目录的 PDF 共享规则与来源，书级配置按文件名分区。
/// 所有写入都在进程级锁内完成“读-改-写”，并用临时文件原子替换。
/// </summary>
public class PdfAudioProfileStore : IPdfAudioProfileStore
{
    private const string AudioSource = "audio";

    private static readonly object WriteLock = new();

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public PdfAudioProfileLoadResult Load(string pdfPath)
    {
        try
        {
            pdfPath = Path.GetFullPath(pdfPath);
            var directory = GetDirectory(pdfPath);
            var profilePath = GetProfilePath(pdfPath);

            PdfAudioDirectoryProfile? document;
            bool migrated = false;
            string? migrationError = null;

            lock (WriteLock)
            {
                if (File.Exists(profilePath))
                {
                    document = ReadDocument(profilePath, out var error);
                    if (error != null) return PdfAudioProfileLoadResult.Fail(error);
                }
                else
                {
                    (document, migrationError) = MigrateLegacy(directory, profilePath);
                    migrated = document != null;
                    if (document == null)
                        return migrationError == null
                            ? PdfAudioProfileLoadResult.Empty
                            : PdfAudioProfileLoadResult.Fail(migrationError);
                }
            }

            var fileName = Path.GetFileName(pdfPath);
            var (bookKey, book) = FindBook(document!, fileName);

            // 文件被改名：按内容快速指纹接管书级配置
            if (book == null)
            {
                var quick = ComputeQuickFingerprint(pdfPath);
                if (!string.IsNullOrEmpty(quick))
                {
                    var candidates = document!.Books
                        .Where(kv => string.Equals(kv.Value.QuickFingerprint, quick, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    if (candidates.Count == 1)
                    {
                        bookKey = candidates[0].Key;
                        book = candidates[0].Value;
                    }
                }
            }

            var profile = BuildProfile(pdfPath, document!, book);
            if (book != null && !string.Equals(bookKey, fileName, StringComparison.Ordinal))
                profile.PdfPath = pdfPath; // 真正改名落盘在下次 Save 时完成
            return PdfAudioProfileLoadResult.Ok(profile, migrated);
        }
        catch (Exception ex)
        {
            return PdfAudioProfileLoadResult.Fail($"加载音频档案失败：{ex.Message}");
        }
    }

    public void Save(PdfAudioProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.PdfPath))
            throw new InvalidOperationException("档案缺少 PDF 路径");

        var pdfPath = Path.GetFullPath(profile.PdfPath);
        var directory = GetDirectory(pdfPath);
        var fileName = Path.GetFileName(pdfPath);
        var profilePath = GetProfilePath(pdfPath);

        lock (WriteLock)
        {
            JsonObject root;
            if (File.Exists(profilePath))
            {
                var text = File.ReadAllText(profilePath);
                try
                {
                    root = JsonNode.Parse(text, null, new JsonDocumentOptions
                    {
                        CommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    }) as JsonObject ?? new JsonObject();
                }
                catch (JsonException ex)
                {
                    // 损坏文件绝不覆盖，交给上层提示用户修复
                    throw new InvalidOperationException(
                        $"档案解析失败（第 {ex.LineNumber + 1} 行）：{ex.Message}", ex);
                }
            }
            else
            {
                root = new JsonObject();
            }

            root["schemaVersion"] = 3;
            root["scope"] = BuildScopeNode(directory, pdfPath);
            root["sources"] = BuildSourcesNode(profile);
            root["defaults"] = BuildDefaultsNode(root["defaults"] as JsonObject, profile);

            var books = root["books"] as JsonObject ?? new JsonObject();
            // 规则是目录级共享数据：新书或空规则不覆盖已有规则；
            // 有书级补丁时不回写目录规则，避免覆盖别人的增量
            var existingBook = FindBookNode(books, fileName);
            var effectiveBook = existingBook.Value;
            if (effectiveBook?["rulePatch"] == null
                && (root["rules"] == null || profile.Rules.Count > 0))
            {
                root["rules"] = JsonSerializer.SerializeToNode(profile.Rules, WriteOptions);
            }
            if (root["rules"] == null)
                root["rules"] = new JsonArray();

            if (existingBook.Key != null && !string.Equals(existingBook.Key, fileName, StringComparison.Ordinal))
                books.Remove(existingBook.Key);

            books[fileName] = BuildBookNode(effectiveBook, profile, pdfPath);
            root["books"] = books;

            WriteAtomic(profilePath, root.ToJsonString(WriteOptions));
        }
    }

    public string GetProfilePath(string pdfPath)
    {
        var directory = GetDirectory(Path.GetFullPath(pdfPath));
        var key = ComputePathKey(directory);
        return Path.Combine(AppPaths.AudioProfilesDir, "dir-" + key + ".json");
    }

    #region 读取与映射

    private PdfAudioDirectoryProfile? ReadDocument(string path, out string? error)
    {
        error = null;
        try
        {
            var text = File.ReadAllText(path);
            var doc = JsonSerializer.Deserialize<PdfAudioDirectoryProfile>(text, ReadOptions);
            return doc ?? new PdfAudioDirectoryProfile();
        }
        catch (JsonException ex)
        {
            error = $"档案解析失败（第 {ex.LineNumber + 1} 行）：{ex.Message}";
            return null;
        }
        catch (IOException ex)
        {
            error = $"档案读取失败：{ex.Message}";
            return null;
        }
    }

    private static (string? Key, PdfAudioBookProfile? Book) FindBook(PdfAudioDirectoryProfile doc, string fileName)
    {
        foreach (var kv in doc.Books)
        {
            if (string.Equals(kv.Key, fileName, StringComparison.OrdinalIgnoreCase))
                return (kv.Key, kv.Value);
        }
        return (null, null);
    }

    private static PdfAudioProfile BuildProfile(
        string pdfPath, PdfAudioDirectoryProfile doc, PdfAudioBookProfile? book)
    {
        var singleAudio = book?.SingleAudio;
        var playbackScope = book?.PlaybackScope ?? doc.Defaults.PlaybackScope;
        if (!string.IsNullOrWhiteSpace(singleAudio))
            playbackScope = PlaybackScope.WholeBook;
        else if (playbackScope == PlaybackScope.WholeBook)
            playbackScope = PlaybackScope.FollowPage;

        var profile = new PdfAudioProfile
        {
            PdfPath = pdfPath,
            MatchMode = string.IsNullOrWhiteSpace(singleAudio)
                ? PdfAudioMatchMode.PerPage
                : PdfAudioMatchMode.SingleAudio,
            SingleAudioFile = singleAudio,
            PlaybackScope = playbackScope,
            IncludeSubDirectories = doc.Defaults.IncludeSubDirectories,
            ExternalAudioDirectories = doc.Sources
                .Where(s => !string.Equals(s, AudioSource, StringComparison.OrdinalIgnoreCase))
                .ToList(),
            PageCount = book?.PageCount ?? 0,
            ScanFingerprint = book?.Cache?.ScanFingerprint ?? "",
            PageIndex = book?.Cache?.PageIndex ?? new Dictionary<int, string>(),
            ManualOverrides = book?.Overrides.ToDictionary(kv => kv.Key, kv => kv.Value.Path)
                               ?? new Dictionary<int, string>(),
            FollowPage = book?.FollowPage ?? true,
            SyncPageOnTrackChange = book?.SyncPageOnTrackChange ?? true,
            Rules = BuildEffectiveRules(doc.Rules, book?.RulePatch)
        };
        return profile;
    }

    private static List<AudioMatchRule> BuildEffectiveRules(List<AudioMatchRule> directoryRules, PdfAudioRulePatch? patch)
    {
        if (patch == null) return directoryRules.ToList();

        if (patch.ReplaceAll)
            return patch.ExtraRules.ToList();

        var disabled = patch.DisabledRuleIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var effective = directoryRules
            .Where(r => !disabled.Contains(r.Id))
            .Select(CloneRule)
            .ToList();

        foreach (var extra in patch.ExtraRules)
        {
            int existing = effective.FindIndex(r => string.Equals(r.Id, extra.Id, StringComparison.OrdinalIgnoreCase));
            if (existing >= 0) effective[existing] = CloneRule(extra);
            else effective.Add(CloneRule(extra));
        }
        return effective;
    }

    private static AudioMatchRule CloneRule(AudioMatchRule rule) => new()
    {
        Id = rule.Id,
        Name = rule.Name,
        FileNamePattern = rule.FileNamePattern,
        PatternMode = rule.PatternMode,
        CaptureGroupIndex = rule.CaptureGroupIndex,
        CaptureGroupName = rule.CaptureGroupName,
        MapMode = rule.MapMode,
        PageOffset = rule.PageOffset,
        PagesPerUnit = rule.PagesPerUnit,
        Enabled = rule.Enabled,
        Priority = rule.Priority
    };

    #endregion

    #region 写入节点

    private static JsonObject BuildScopeNode(string directory, string pdfPath)
    {
        var key = ComputePathKey(directory);
        return new JsonObject
        {
            ["kind"] = "directory",
            ["id"] = "dir_" + key[..8],
            ["path"] = directory,
            ["pathKey"] = key,
            ["signature"] = ComputeDirectorySignature(directory)
        };
    }

    private static JsonArray BuildSourcesNode(PdfAudioProfile profile)
    {
        var sources = new JsonArray { AudioSource };
        foreach (var dir in profile.ExternalAudioDirectories
                     .Where(d => !string.IsNullOrWhiteSpace(d))
                     .Select(Path.GetFullPath)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            sources.Add(dir);
        }
        return sources;
    }

    private static JsonObject BuildDefaultsNode(JsonObject? existing, PdfAudioProfile profile)
    {
        var node = existing != null ? (JsonObject)existing.DeepClone() : new JsonObject();
        node["includeSubDirectories"] = profile.IncludeSubDirectories;
        if (node["playbackScope"] == null)
            node["playbackScope"] = profile.PlaybackScope.ToString();
        if (node["playbackSpeed"] == null)
            node["playbackSpeed"] = 1.0;
        return node;
    }

    private static JsonObject BuildBookNode(JsonObject? existing, PdfAudioProfile profile, string pdfPath)
    {
        var book = existing != null ? (JsonObject)existing.DeepClone() : new JsonObject();

        book["quickFingerprint"] = ComputeQuickFingerprint(pdfPath);
        book["fullFingerprint"] = ComputeFullFingerprint(pdfPath, profile.PageCount);
        book["pageCount"] = profile.PageCount;
        book["singleAudio"] = profile.SingleAudioFile;
        book["playbackScope"] = profile.PlaybackScope.ToString();
        book["followPage"] = profile.FollowPage;
        book["syncPageOnTrackChange"] = profile.SyncPageOnTrackChange;

        // 覆盖项合并：保留原有 source/createdAt，仅更新路径
        var existingOverrides = book["overrides"] as JsonObject;
        var overrides = new JsonObject();
        foreach (var kv in profile.ManualOverrides)
        {
            var key = kv.Key.ToString();
            var old = existingOverrides?[key] as JsonObject;
            overrides[key] = new JsonObject
            {
                ["path"] = kv.Value,
                ["source"] = old?["source"]?.GetValue<string>() ?? "manual",
                ["createdAt"] = old?["createdAt"]?.GetValue<string>() ?? DateTime.Now.ToString("O")
            };
        }
        book["overrides"] = overrides;

        var pageIndex = new JsonObject();
        foreach (var kv in profile.PageIndex.OrderBy(kv => kv.Key))
            pageIndex[kv.Key.ToString()] = kv.Value;

        book["cache"] = new JsonObject
        {
            ["pageIndex"] = pageIndex,
            ["scanFingerprint"] = profile.ScanFingerprint,
            ["updatedAt"] = DateTime.Now.ToString("O")
        };

        // rulePatch 属于用户/导入数据，程序不生成；存在时保持原样
        if (book["rulePatch"] == null) book["rulePatch"] = null;
        return book;
    }

    private static (string? Key, JsonObject? Value) FindBookNode(JsonObject books, string fileName)
    {
        foreach (var kv in books)
        {
            if (string.Equals(kv.Key, fileName, StringComparison.OrdinalIgnoreCase))
                return (kv.Key, kv.Value as JsonObject);
        }
        return (null, null);
    }

    private void WriteAtomic(string path, string json)
    {
        AppPaths.EnsureDirectoryExists(AppPaths.AudioProfilesDir);
        var temp = path + ".tmp";
        File.WriteAllText(temp, json, Encoding.UTF8);
        try
        {
            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);
        }
        catch
        {
            if (File.Exists(temp)) File.Delete(temp);
            throw;
        }
    }

    #endregion

    #region 旧版迁移

    /// <summary>
    /// 首次打开目录时，把同目录所有一书一档的旧档案合并为目录档案，并把旧文件移到 _legacy。
    /// </summary>
    private (PdfAudioDirectoryProfile? Document, string? Error) MigrateLegacy(string directory, string profilePath)
    {
        var legacyFiles = FindLegacyFiles(directory);
        if (legacyFiles.Count == 0) return (null, null);

        string? parseError = null;
        var loaded = new List<(PdfAudioProfile Profile, DateTime Modified)>();
        foreach (var file in legacyFiles)
        {
            try
            {
                var text = File.ReadAllText(file);
                var profile = JsonSerializer.Deserialize<PdfAudioProfile>(text, ReadOptions);
                if (profile == null || string.IsNullOrWhiteSpace(profile.PdfPath)) continue;
                loaded.Add((profile, File.GetLastWriteTimeUtc(file)));
            }
            catch (JsonException ex)
            {
                parseError ??= $"旧档案解析失败：{Path.GetFileName(file)}（第 {ex.LineNumber + 1} 行）";
            }
            catch (IOException)
            {
                // 读不到就跳过，不阻塞其它档案迁移
            }
        }

        if (loaded.Count == 0) return (null, parseError);

        loaded.Sort((a, b) => a.Modified.CompareTo(b.Modified));
        var latest = loaded[^1];

        var document = new PdfAudioDirectoryProfile
        {
            SchemaVersion = 3,
            MigratedFrom = "v2",
            Scope = new PdfAudioDirectoryScope
            {
                Path = directory,
                PathKey = ComputePathKey(directory),
                Signature = ComputeDirectorySignature(directory)
            },
            Defaults = new PdfAudioDirectoryDefaults
            {
                PlaybackScope = latest.Profile.PlaybackScope,
                IncludeSubDirectories = latest.Profile.IncludeSubDirectories,
                PlaybackSpeed = 1.0
            }
        };
        document.Scope.Id = "dir_" + document.Scope.PathKey[..8];

        // 规则按 Id 合并，后写入的（更晚修改的）覆盖同 Id 规则
        var rules = new Dictionary<string, AudioMatchRule>(StringComparer.OrdinalIgnoreCase);
        var sources = new List<string> { AudioSource };
        foreach (var (profile, modified) in loaded)
        {
            foreach (var rule in profile.Rules)
            {
                if (string.IsNullOrWhiteSpace(rule.Id)) rule.Id = Guid.NewGuid().ToString("N");
                rules[rule.Id] = rule;
            }
            foreach (var source in profile.ExternalAudioDirectories.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                if (!sources.Contains(source, StringComparer.OrdinalIgnoreCase))
                    sources.Add(source);
            }

            var fileName = Path.GetFileName(profile.PdfPath);
            if (string.IsNullOrWhiteSpace(fileName)) continue;

            var single = profile.SingleAudioFile;
            var book = new PdfAudioBookProfile
            {
                SingleAudio = single,
                PlaybackScope = !string.IsNullOrWhiteSpace(single)
                    ? PlaybackScope.WholeBook
                    : profile.PlaybackScope == PlaybackScope.WholeBook
                        ? PlaybackScope.FollowPage
                        : profile.PlaybackScope,
                PageCount = profile.PageCount,
                FollowPage = profile.FollowPage,
                SyncPageOnTrackChange = profile.SyncPageOnTrackChange,
                QuickFingerprint = ComputeQuickFingerprint(profile.PdfPath),
                FullFingerprint = ComputeFullFingerprint(profile.PdfPath, profile.PageCount),
                Cache = profile.PageIndex.Count == 0
                    ? null
                    : new PdfAudioBookCache
                    {
                        PageIndex = new Dictionary<int, string>(profile.PageIndex),
                        ScanFingerprint = profile.ScanFingerprint,
                        UpdatedAt = modified
                    }
            };
            foreach (var kv in profile.ManualOverrides)
            {
                book.Overrides[kv.Key] = new PdfAudioOverride
                {
                    Path = kv.Value,
                    Source = "manual",
                    CreatedAt = modified
                };
            }
            document.Books[fileName] = book;
        }

        document.Rules = rules.Values.ToList();
        document.Sources = sources;

        JsonHelper.SaveToFile(profilePath, document);
        MoveLegacyToArchive(legacyFiles);
        return (document, null);
    }

    private List<string> FindLegacyFiles(string directory)
    {
        var result = new List<string>();
        if (!Directory.Exists(AppPaths.AudioProfilesDir)) return result;

        foreach (var file in Directory.EnumerateFiles(AppPaths.AudioProfilesDir, "*.json"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (name.StartsWith("dir-", StringComparison.OrdinalIgnoreCase)) continue;
            if (name.Length != 16 || !name.All(Uri.IsHexDigit)) continue;

            PdfAudioProfile? profile;
            try
            {
                var text = File.ReadAllText(file);
                profile = JsonSerializer.Deserialize<PdfAudioProfile>(text, ReadOptions);
            }
            catch
            {
                // 无法解析的旧文件无法判断所属目录，保留原文件不迁移，避免误伤
                continue;
            }

            if (profile == null) continue;
            try
            {
                if (string.Equals(GetDirectory(profile.PdfPath), directory, StringComparison.OrdinalIgnoreCase))
                    result.Add(file);
            }
            catch
            {
                // 路径非法则忽略
            }
        }
        return result;
    }

    private void MoveLegacyToArchive(IEnumerable<string> legacyFiles)
    {
        var archive = Path.Combine(AppPaths.AudioProfilesDir, "_legacy");
        AppPaths.EnsureDirectoryExists(archive);
        foreach (var file in legacyFiles)
        {
            try
            {
                var target = Path.Combine(archive, Path.GetFileName(file));
                if (File.Exists(target)) File.Delete(target);
                File.Move(file, target);
            }
            catch
            {
                // 归档失败不影响新档案使用
            }
        }
    }

    #endregion

    #region 指纹与路径

    private static string GetDirectory(string pdfPath)
        => Path.GetFullPath(Path.GetDirectoryName(Path.GetFullPath(pdfPath)) ?? "").TrimEnd('\\', '/');

    private static string ComputePathKey(string directory)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(directory.ToUpperInvariant())))[..16];

    private static string ComputeDirectorySignature(string directory)
    {
        try
        {
            if (!Directory.Exists(directory)) return "";
            var builder = new StringBuilder();
            foreach (var file in Directory.EnumerateFiles(directory, "*.pdf")
                         .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                builder.Append(Path.GetFileName(file)).Append('|')
                    .Append(new FileInfo(file).Length).Append(';');
            }
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())))[..16];
        }
        catch
        {
            return "";
        }
    }

    /// <summary>快速指纹：文件大小 + 前 64KB 内容；用于改名恢复。</summary>
    private static string? ComputeQuickFingerprint(string pdfPath)
    {
        try
        {
            if (!File.Exists(pdfPath)) return null;
            using var stream = File.OpenRead(pdfPath);
            var buffer = new byte[64 * 1024];
            int read = stream.Read(buffer, 0, buffer.Length);
            using var sha = SHA256.Create();
            var header = Encoding.UTF8.GetBytes(stream.Length.ToString());
            sha.TransformBlock(header, 0, header.Length, null, 0);
            sha.TransformFinalBlock(buffer, 0, read);
            return "q_" + Convert.ToHexString(sha.Hash!)[..16];
        }
        catch
        {
            return null;
        }
    }

    private static string? ComputeFullFingerprint(string pdfPath, int pageCount)
    {
        var quick = ComputeQuickFingerprint(pdfPath);
        if (quick == null) return null;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(quick + "|" + pageCount));
        return "f_" + Convert.ToHexString(hash)[..16];
    }

    #endregion
}
