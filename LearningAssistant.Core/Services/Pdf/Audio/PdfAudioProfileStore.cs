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
/// 所有读写都在进程内 SemaphoreSlim 内完成"读-改-写"，并用临时文件原子替换。
/// </summary>
public class PdfAudioProfileStore : IPdfAudioProfileStore
{
    private const string AudioSource = "audio";

    /// <summary>进程内串行化档案读写；同步实现，SemaphoreSlim 便于后续异步化。</summary>
    private static readonly SemaphoreSlim WriteGate = new(1, 1);

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

    public PdfAudioProfileLoadResult Load(string pdfPath, int pageCount = 0)
    {
        try
        {
            pdfPath = Path.GetFullPath(pdfPath);
            var directory = GetDirectory(pdfPath);
            var profilePath = GetProfilePath(pdfPath);

            PdfAudioDirectoryProfile? document;
            bool migrated = false;
            string? migrationError = null;

            WriteGate.Wait();
            try
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
            finally
            {
                WriteGate.Release();
            }

            var fileName = Path.GetFileName(pdfPath);
            var (bookKey, book) = FindBook(document!, fileName);

            // 文件被改名：quickFingerprint 先筛候选，再用 fullFingerprint（含页数）确认
            if (book == null)
            {
                var quick = ComputeQuickFingerprint(pdfPath);
                if (!string.IsNullOrEmpty(quick))
                {
                    var candidates = document!.Books
                        .Where(kv => string.Equals(kv.Value.QuickFingerprint, quick, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    if (candidates.Count > 1 && pageCount > 0)
                    {
                        var full = ComputeFullFingerprint(pdfPath, pageCount);
                        var confirmed = candidates
                            .Where(kv => string.Equals(kv.Value.FullFingerprint, full, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        if (confirmed.Count > 0) candidates = confirmed;
                    }
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

        WriteGate.Wait();
        try
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

            // 旧迁移档案由 JsonHelper 写出 PascalCase 键，这里统一按不区分大小写读写，
            // 避免只认 camelCase 时把其它书的分区整块丢掉
            SetNode(root, "schemaVersion", 3);
            SetNode(root, "scope", BuildScopeNode(directory, pdfPath));
            SetNode(root, "sources", BuildSourcesNode(profile));
            SetNode(root, "defaults", BuildDefaultsNode(GetObject(root, "defaults"), profile));

            var books = GetObject(root, "books") ?? new JsonObject();
            // 有书级补丁时 Rules 是合并结果，不能回写目录规则；无补丁时 Rules 即目录规则
            if (profile.RulePatch == null)
                SetNode(root, "rules", JsonSerializer.SerializeToNode(profile.Rules, WriteOptions) ?? new JsonArray());
            else if (GetNode(root, "rules") == null)
                SetNode(root, "rules", new JsonArray());

            var existingBook = FindBookNode(books, fileName);
            var effectiveBook = existingBook.Value;

            if (existingBook.Key != null && !string.Equals(existingBook.Key, fileName, StringComparison.Ordinal))
                books.Remove(existingBook.Key);

            books[fileName] = BuildBookNode(effectiveBook, profile, pdfPath);
            SetNode(root, "books", books);

            WriteAtomic(profilePath, root.ToJsonString(WriteOptions));
        }
        finally
        {
            WriteGate.Release();
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
            PlaybackSpeed = Math.Clamp(book?.PlaybackSpeed ?? doc.Defaults.PlaybackSpeed, 0.6, 2.0),
            LastPositionSeconds = Math.Max(0, book?.PositionSeconds ?? 0),
            LastTrackPath = book?.LastTrack,
            DirectoryRules = doc.Rules.ToList(),
            RulePatch = book?.RulePatch,
            Rules = PdfAudioRuleMerger.Merge(doc.Rules, book?.RulePatch)
        };
        return profile;
    }

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

        // 迁移档案是 PascalCase，统一用大小写无关写入，避免同名字段重复
        SetNode(book, "quickFingerprint", ComputeQuickFingerprint(pdfPath));
        SetNode(book, "fullFingerprint", ComputeFullFingerprint(pdfPath, profile.PageCount));
        SetNode(book, "pageCount", profile.PageCount);
        SetNode(book, "singleAudio", profile.SingleAudioFile);
        SetNode(book, "playbackScope", profile.PlaybackScope.ToString());
        SetNode(book, "followPage", profile.FollowPage);
        SetNode(book, "syncPageOnTrackChange", profile.SyncPageOnTrackChange);
        // 书级用户状态：倍速与上次播放位置，按书记忆（与机器缓存分离）
        SetNode(book, "playbackSpeed", Math.Clamp(profile.PlaybackSpeed, 0.6, 2.0));
        SetNode(book, "positionSeconds", Math.Max(0, profile.LastPositionSeconds));
        SetNode(book, "lastTrack", profile.LastTrackPath);

        // 覆盖项合并：保留原有 source/createdAt，仅更新路径
        var existingOverrides = GetObject(book, "overrides");
        var overrides = new JsonObject();
        foreach (var kv in profile.ManualOverrides)
        {
            var key = kv.Key.ToString();
            var old = existingOverrides?[key] as JsonObject;
            overrides[key] = new JsonObject
            {
                ["path"] = kv.Value,
                ["source"] = old == null ? "manual" : GetString(old, "source") ?? "manual",
                ["createdAt"] = old == null ? DateTime.Now.ToString("O") : GetString(old, "createdAt") ?? DateTime.Now.ToString("O")
            };
        }
        SetNode(book, "overrides", overrides);

        var pageIndex = new JsonObject();
        foreach (var kv in profile.PageIndex.OrderBy(kv => kv.Key))
            pageIndex[kv.Key.ToString()] = kv.Value;

        SetNode(book, "cache", new JsonObject
        {
            ["pageIndex"] = pageIndex,
            ["scanFingerprint"] = profile.ScanFingerprint,
            ["updatedAt"] = DateTime.Now.ToString("O")
        });

        SetNode(book, "rulePatch", profile.RulePatch == null ? null : BuildRulePatchNode(profile.RulePatch));
        return book;
    }

    private static JsonObject BuildRulePatchNode(PdfAudioRulePatch patch)
    {
        var disabled = new JsonArray();
        foreach (var id in patch.DisabledRuleIds.Where(id => !string.IsNullOrWhiteSpace(id)))
            disabled.Add(id);

        return new JsonObject
        {
            ["disabledRuleIds"] = disabled,
            ["extraRules"] = JsonSerializer.SerializeToNode(patch.ExtraRules, WriteOptions) ?? new JsonArray(),
            ["replaceAll"] = patch.ReplaceAll
        };
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

    #region 导入导出

    /// <summary>导出整个目录档案：保留用户字段，剔除 cache。</summary>
    public PdfAudioExportResult ExportDirectory(string pdfPath)
    {
        try
        {
            pdfPath = Path.GetFullPath(pdfPath);
            var profilePath = GetProfilePath(pdfPath);

            JsonObject root;
            WriteGate.Wait();
            try
            {
                if (!File.Exists(profilePath))
                    return PdfAudioExportResult.Fail("当前目录还没有可导出的匹配档案");
                root = ParseRoot(File.ReadAllText(profilePath), out var error)
                       ?? throw new InvalidOperationException(error);
            }
            finally
            {
                WriteGate.Release();
            }

            int version = GetInt(root, "schemaVersion") ?? 3;
            if (version > 3)
                return PdfAudioExportResult.Fail($"档案版本 {version} 高于当前支持版本 3，无法导出");

            var directory = GetDirectory(pdfPath);
            var name = SanitizeFileName(new DirectoryInfo(directory).Name) + ".audio.json";
            var export = BuildExportRoot(root, "directory", GetObject(root, "books") ?? new JsonObject());
            return PdfAudioExportResult.Ok(export.ToJsonString(WriteOptions), name);
        }
        catch (Exception ex)
        {
            return PdfAudioExportResult.Fail($"导出目录档案失败：{ex.Message}");
        }
    }

    /// <summary>导出当前书：目录规则快照 + 一个书级分区，导入时只影响同名书。</summary>
    public PdfAudioExportResult ExportBook(string pdfPath)
    {
        try
        {
            pdfPath = Path.GetFullPath(pdfPath);
            var profilePath = GetProfilePath(pdfPath);
            var fileName = Path.GetFileName(pdfPath);

            JsonObject root;
            WriteGate.Wait();
            try
            {
                if (!File.Exists(profilePath))
                    return PdfAudioExportResult.Fail("当前书籍还没有可导出的匹配配置");
                root = ParseRoot(File.ReadAllText(profilePath), out var error)
                       ?? throw new InvalidOperationException(error);
            }
            finally
            {
                WriteGate.Release();
            }

            var books = GetObject(root, "books") ?? new JsonObject();
            var book = FindBookNode(books, fileName).Value;
            if (book == null)
            {
                // 文件改名后按快速指纹找回分区
                var quick = ComputeQuickFingerprint(pdfPath);
                if (!string.IsNullOrEmpty(quick))
                {
                    var candidates = new List<JsonObject>();
                    foreach (var kv in books)
                    {
                        if (kv.Value is JsonObject candidate
                            && string.Equals(GetString(candidate, "quickFingerprint"), quick,
                                StringComparison.OrdinalIgnoreCase))
                            candidates.Add(candidate);
                    }
                    if (candidates.Count == 1) book = candidates[0];
                }
            }

            if (book == null)
                return PdfAudioExportResult.Fail("当前书籍还没有可导出的匹配配置");

            var single = new JsonObject { [fileName] = book.DeepClone() };
            var export = BuildExportRoot(root, "book", single);
            var suggested = SanitizeFileName(Path.GetFileNameWithoutExtension(pdfPath)) + ".audio.json";
            return PdfAudioExportResult.Ok(export.ToJsonString(WriteOptions), suggested);
        }
        catch (Exception ex)
        {
            return PdfAudioExportResult.Fail($"导出当前书失败：{ex.Message}");
        }
    }

    /// <summary>解析并校验导入文件，生成差异摘要；不写盘。</summary>
    public PdfAudioImportPreview PreviewImport(string pdfPath, string json)
    {
        try
        {
            var root = ParseRoot(json, out var parseError);
            if (root == null) return PdfAudioImportPreview.Fail(parseError!);

            int? version = GetInt(root, "schemaVersion");
            if (version == null) return PdfAudioImportPreview.Fail("档案缺少 schemaVersion");
            if (version > 3)
                return PdfAudioImportPreview.Fail($"档案版本 {version} 高于当前支持版本 3，请升级程序后再导入");

            var kind = (GetString(root, "kind") ?? "").Trim().ToLowerInvariant();
            if (kind != "directory" && kind != "book")
                return PdfAudioImportPreview.Fail("无法识别档案类型：kind 必须是 directory 或 book");

            var books = GetObject(root, "books");
            if (books == null || books.Count == 0)
                return PdfAudioImportPreview.Fail("档案中没有可导入的书籍分区");

            var fullPath = Path.GetFullPath(pdfPath);
            var fileName = Path.GetFileName(fullPath);
            if (kind == "book")
            {
                if (books.Count != 1)
                    return PdfAudioImportPreview.Fail("单书档案只能包含一个书籍分区");
                var key = books.First().Key;
                if (!string.Equals(key, fileName, StringComparison.OrdinalIgnoreCase))
                    return PdfAudioImportPreview.Fail(
                        $"单书档案属于「{key}」，与当前打开的「{fileName}」不一致；请打开对应 PDF 后再导入");
            }

            var errors = new List<string>();
            var warnings = new List<string>();

            ValidatePlaybackScope(GetObject(root, "defaults"), "目录默认值", errors);
            ValidateRules(GetArray(root, "rules"), "目录规则", errors, warnings);

            var items = new List<string>();
            var directory = GetDirectory(fullPath);
            foreach (var kv in books)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                {
                    errors.Add("书籍分区存在空文件名");
                    continue;
                }
                if (kv.Value is not JsonObject book)
                {
                    errors.Add($"书籍「{kv.Key}」的分区不是 JSON 对象");
                    continue;
                }

                ValidatePlaybackScope(book, $"「{kv.Key}」", errors);
                ValidateRules(GetArray(GetObject(book, "rulePatch"), "extraRules"),
                    $"「{kv.Key}」附加规则", errors, warnings);
                ValidateOverrides(book, kv.Key, errors);

                var overrides = GetObject(book, "overrides")?.Count ?? 0;
                var single = GetString(book, "singleAudio");
                var patch = GetObject(book, "rulePatch");
                var patchText = patch == null
                    ? "继承目录规则"
                    : GetBool(patch, "replaceAll") == true
                        ? "规则完全独立"
                        : $"禁用 {(GetArray(patch, "disabledRuleIds")?.Count ?? 0)} 条 · 追加 {(GetArray(patch, "extraRules")?.Count ?? 0)} 条";
                var scopeText = string.IsNullOrWhiteSpace(single) ? "按页匹配" : "整篇音频";
                var missing = kind == "directory" && !File.Exists(Path.Combine(directory, kv.Key));
                items.Add($"{(missing ? "[本目录无此文件] " : "")}{kv.Key}：覆盖 {overrides} 页 · {scopeText} · {patchText}");
                if (missing)
                    warnings.Add($"本目录没有「{kv.Key}」，将只保存其配置，打开该书时生效");
            }

            if (errors.Count > 0)
                return PdfAudioImportPreview.Fail(string.Join(Environment.NewLine, errors.Take(5)));

            int ruleCount = GetArray(root, "rules")?.Count ?? 0;
            int sourceCount = GetArray(root, "sources")?.Count ?? 0;
            return new PdfAudioImportPreview
            {
                Success = true,
                Kind = kind,
                Title = kind == "directory" ? "目录档案" : "单书档案",
                Summary = kind == "directory"
                    ? $"目录规则 {ruleCount} 条 · 来源 {sourceCount} 个 · 书籍 {books.Count} 本"
                    : $"本书 {fileName} · 目录规则 {ruleCount} 条",
                Items = items,
                Warnings = warnings,
                Root = root
            };
        }
        catch (Exception ex)
        {
            return PdfAudioImportPreview.Fail($"导入校验失败：{ex.Message}");
        }
    }

    /// <summary>按预览结果写盘：规则按 id 合并、来源并集、默认值覆盖、按书合并并保留本机缓存。</summary>
    public void ApplyImport(string pdfPath, PdfAudioImportPreview preview)
    {
        if (!preview.Success || preview.Root == null)
            throw new InvalidOperationException(preview.Error ?? "导入预览无效");

        var fullPath = Path.GetFullPath(pdfPath);
        var directory = GetDirectory(fullPath);
        var profilePath = GetProfilePath(fullPath);

        WriteGate.Wait();
        try
        {
            JsonObject target;
            if (File.Exists(profilePath))
            {
                target = ParseRoot(File.ReadAllText(profilePath), out var error)
                         ?? throw new InvalidOperationException(error ?? "档案解析失败");
            }
            else
            {
                target = new JsonObject();
            }

            SetNode(target, "schemaVersion", 3);
            if (GetObject(target, "scope") == null)
                SetNode(target, "scope", BuildScopeNode(directory, fullPath));

            SetNode(target, "sources", MergeSources(GetNode(target, "sources"), GetNode(preview.Root, "sources")));
            SetNode(target, "rules", MergeRules(GetNode(target, "rules"), GetNode(preview.Root, "rules")));

            var defaults = GetObject(target, "defaults") ?? new JsonObject();
            var importedDefaults = GetObject(preview.Root, "defaults");
            if (importedDefaults != null)
                foreach (var kv in importedDefaults) defaults[kv.Key] = kv.Value?.DeepClone();
            SetNode(target, "defaults", defaults);

            var targetBooks = GetObject(target, "books") ?? new JsonObject();
            var importedBooks = GetObject(preview.Root, "books")!;
            foreach (var kv in importedBooks)
            {
                if (kv.Value is not JsonObject importedBook) continue;
                var key = FindExistingBookKey(targetBooks, kv.Key) ?? kv.Key;
                var existing = targetBooks[key] as JsonObject;
                targetBooks[key] = MergeBookNode(existing, importedBook, directory, key);
            }
            SetNode(target, "books", targetBooks);

            WriteAtomic(profilePath, target.ToJsonString(WriteOptions));
        }
        finally
        {
            WriteGate.Release();
        }
    }

    private static JsonObject BuildExportRoot(JsonObject root, string kind, JsonObject books)
        => new()
        {
            ["schemaVersion"] = 3,
            ["kind"] = kind,
            ["exportedAt"] = DateTime.Now.ToString("O"),
            ["scope"] = GetObject(root, "scope")?.DeepClone() ?? new JsonObject(),
            ["defaults"] = GetObject(root, "defaults")?.DeepClone() ?? new JsonObject(),
            ["sources"] = GetArray(root, "sources")?.DeepClone() ?? new JsonArray(),
            ["rules"] = GetArray(root, "rules")?.DeepClone() ?? new JsonArray(),
            ["books"] = StripCaches(books)
        };

    /// <summary>导出剔除缓存：缓存只留 AppData，不随档案迁移。</summary>
    private static JsonObject StripCaches(JsonObject books)
    {
        var result = new JsonObject();
        foreach (var kv in books)
        {
            if (kv.Value is JsonObject book)
            {
                var clone = (JsonObject)book.DeepClone();
                RemoveIgnoreCase(clone, "cache");
                result[kv.Key] = clone;
            }
            else
            {
                result[kv.Key] = kv.Value?.DeepClone();
            }
        }
        return result;
    }

    private static JsonObject MergeBookNode(JsonObject? existing, JsonObject imported,
        string directory, string key)
    {
        var book = existing != null ? (JsonObject)existing.DeepClone() : new JsonObject();

        foreach (var field in new[]
                 {
                     "singleAudio", "playbackScope", "followPage", "syncPageOnTrackChange",
                     "playbackSpeed", "positionSeconds", "lastTrack"
                 })
        {
            if (TryGetNode(imported, field, out var node))
                SetNode(book, field, node?.DeepClone());
        }
        if (TryGetNode(imported, "rulePatch", out var patchNode))
            SetNode(book, "rulePatch", NormalizeRulePatch(patchNode));

        var overrides = GetObject(book, "overrides") ?? new JsonObject();
        var importedOverrides = GetObject(imported, "overrides");
        if (importedOverrides != null)
        {
            foreach (var kv in importedOverrides)
                if (kv.Value != null) overrides[kv.Key] = kv.Value.DeepClone();
        }
        SetNode(book, "overrides", overrides);

        // 已有书：指纹、页数与缓存都是本机机器字段，保持原值
        if (existing == null)
        {
            int pageCount = GetInt(imported, "pageCount") ?? 0;
            SetNode(book, "pageCount", pageCount);
            var pdfPath = Path.Combine(directory, key);
            var quick = ComputeQuickFingerprint(pdfPath);
            SetNode(book, "quickFingerprint", quick);
            SetNode(book, "fullFingerprint", quick == null ? null : ComputeFullFingerprint(pdfPath, pageCount));
        }

        return book;
    }

    private static JsonNode? NormalizeRulePatch(JsonNode? node)
    {
        if (node is not JsonObject patch) return null;
        return new JsonObject
        {
            ["disabledRuleIds"] = GetArray(patch, "disabledRuleIds")?.DeepClone() ?? new JsonArray(),
            ["extraRules"] = GetArray(patch, "extraRules")?.DeepClone() ?? new JsonArray(),
            ["replaceAll"] = GetBool(patch, "replaceAll") ?? false
        };
    }

    private static JsonArray MergeSources(JsonNode? existing, JsonNode? imported)
    {
        var result = new JsonArray();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        seen.Add(AudioSource);
        result.Add(AudioSource);

        void AddAll(JsonNode? node)
        {
            if (node is not JsonArray array) return;
            foreach (var item in array)
            {
                if (item is JsonValue value && value.TryGetValue<string>(out var text)
                    && !string.IsNullOrWhiteSpace(text) && seen.Add(text))
                    result.Add(text);
            }
        }

        AddAll(existing);
        AddAll(imported);
        return result;
    }

    /// <summary>规则按 id 合并：已存在同 id 时导入内容覆盖，其余保留。</summary>
    private static JsonArray MergeRules(JsonNode? existing, JsonNode? imported)
    {
        var order = new List<string>();
        var byId = new Dictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
        var idless = new List<JsonObject>();

        void AddAll(JsonNode? node, bool overwrite)
        {
            if (node is not JsonArray array) return;
            foreach (var item in array)
            {
                if (item is not JsonObject rule) continue;
                var id = GetString(rule, "id");
                if (string.IsNullOrWhiteSpace(id))
                {
                    if (overwrite) idless.Add((JsonObject)rule.DeepClone());
                    continue;
                }
                if (!byId.ContainsKey(id)) order.Add(id);
                if (overwrite || !byId.ContainsKey(id))
                    byId[id] = (JsonObject)rule.DeepClone();
            }
        }

        AddAll(existing, overwrite: false);
        AddAll(imported, overwrite: true);

        var result = new JsonArray();
        foreach (var id in order) result.Add(byId[id]);
        foreach (var rule in idless) result.Add(rule);
        return result;
    }

    private static void ValidatePlaybackScope(JsonObject? owner, string label, List<string> errors)
    {
        if (owner == null) return;
        var scope = GetString(owner, "playbackScope");
        if (!string.IsNullOrWhiteSpace(scope)
            && !Enum.TryParse<PlaybackScope>(scope, ignoreCase: true, out _))
            errors.Add($"{label}的 playbackScope「{scope}」无效");
    }

    private static void ValidateRules(JsonArray? rules, string label, List<string> errors, List<string> warnings)
    {
        if (rules == null) return;
        int index = 0;
        foreach (var node in rules)
        {
            index++;
            if (node is not JsonObject ruleNode)
            {
                errors.Add($"{label}第 {index} 条不是 JSON 对象");
                continue;
            }

            AudioMatchRule? rule;
            try
            {
                rule = JsonSerializer.Deserialize<AudioMatchRule>(ruleNode.ToJsonString(), ReadOptions);
            }
            catch (JsonException ex)
            {
                errors.Add($"{label}第 {index} 条格式错误：{ex.Message}");
                continue;
            }
            if (rule == null)
            {
                errors.Add($"{label}第 {index} 条内容为空");
                continue;
            }
            if (!rule.Enabled) continue;
            if (string.IsNullOrWhiteSpace(rule.Name)) rule.Name = $"{label}第 {index} 条";

            var regex = PdfAudioPatternCompiler.TryCompile(rule, out var diagnostic);
            if (regex == null) errors.Add($"{label}「{rule.Name}」：{diagnostic}");
            else if (!string.IsNullOrWhiteSpace(diagnostic)) warnings.Add($"{label}「{rule.Name}」：{diagnostic}");
        }
    }

    private static void ValidateOverrides(JsonObject book, string bookKey, List<string> errors)
    {
        var overrides = GetObject(book, "overrides");
        if (overrides == null) return;
        int pageCount = GetInt(book, "pageCount") ?? 0;
        foreach (var kv in overrides)
        {
            if (!int.TryParse(kv.Key, out var page) || page < 0)
            {
                errors.Add($"「{bookKey}」的覆盖项页码「{kv.Key}」无效");
                continue;
            }
            if (pageCount > 0 && page >= pageCount)
                errors.Add($"「{bookKey}」的覆盖项页码 {page + 1} 超出页数 {pageCount}");
            if (kv.Value is not JsonObject)
                errors.Add($"「{bookKey}」第 {page + 1} 页的覆盖项不是 JSON 对象");
        }
    }

    private static JsonObject? ParseRoot(string json, out string? error)
    {
        error = null;
        try
        {
            var node = JsonNode.Parse(json, null, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
            if (node is JsonObject root) return root;
            error = "档案根节点必须是 JSON 对象";
            return null;
        }
        catch (JsonException ex)
        {
            error = $"档案解析失败（第 {ex.LineNumber + 1} 行）：{ex.Message}";
            return null;
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder();
        foreach (var ch in name)
            builder.Append(invalid.Contains(ch) ? '_' : ch);
        var result = builder.ToString().Trim();
        return string.IsNullOrEmpty(result) ? "audio" : result;
    }

    #endregion

    #region JsonNode 大小写无关读写

    private static JsonNode? GetNode(JsonObject obj, string name)
        => TryGetNode(obj, name, out var node) ? node : null;

    private static bool TryGetNode(JsonObject obj, string name, out JsonNode? node)
    {
        if (obj.TryGetPropertyValue(name, out node)) return true;
        foreach (var kv in obj)
        {
            if (!string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase)) continue;
            node = kv.Value;
            return true;
        }
        node = null;
        return false;
    }

    private static JsonObject? GetObject(JsonObject? obj, string name)
        => obj != null && TryGetNode(obj, name, out var node) ? node as JsonObject : null;

    private static JsonArray? GetArray(JsonObject? obj, string name)
        => obj != null && TryGetNode(obj, name, out var node) ? node as JsonArray : null;

    private static string? GetString(JsonObject obj, string name)
        => TryGetNode(obj, name, out var node)
           && node is JsonValue value
           && value.TryGetValue<string>(out var text)
            ? text
            : null;

    private static int? GetInt(JsonObject obj, string name)
    {
        if (!TryGetNode(obj, name, out var node) || node is not JsonValue value) return null;
        if (value.TryGetValue<int>(out var number)) return number;
        return value.TryGetValue<string>(out var text) && int.TryParse(text, out var parsed) ? parsed : null;
    }

    private static bool? GetBool(JsonObject obj, string name)
    {
        if (!TryGetNode(obj, name, out var node) || node is not JsonValue value) return null;
        if (value.TryGetValue<bool>(out var flag)) return flag;
        return value.TryGetValue<string>(out var text) && bool.TryParse(text, out var parsed) ? parsed : null;
    }

    private static void SetNode(JsonObject obj, string name, JsonNode? node)
    {
        RemoveIgnoreCase(obj, name);
        obj[name] = node;
    }

    private static void RemoveIgnoreCase(JsonObject obj, string name)
    {
        var keys = obj
            .Where(kv => string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Key)
            .ToList();
        foreach (var key in keys) obj.Remove(key);
    }

    private static string? FindExistingBookKey(JsonObject books, string fileName)
    {
        foreach (var kv in books)
            if (string.Equals(kv.Key, fileName, StringComparison.OrdinalIgnoreCase)) return kv.Key;
        return null;
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
