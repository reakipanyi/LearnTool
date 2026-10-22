using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Data.Database;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LearningAssistant.Core.Tests;

public class ContentLoaderServicePersonalMergeTests : IDisposable
{
    private readonly string _root;

    public ContentLoaderServicePersonalMergeTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "LearningAssistantTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "data"));
    }

    [Fact]
    public void LoadItems_MergesPersonalAndRemovesDuplicate()
    {
        var systemItems = new List<LearningItem>
        {
            new() { Id = "sys-1", Subject = SubjectType.Chinese, SubCategory = SubCategoryType.ChineseCharacter, MainContent = "霜" },
            new() { Id = "sys-2", Subject = SubjectType.Chinese, SubCategory = SubCategoryType.ChineseCharacter, MainContent = "雪" }
        };
        JsonHelper.SaveToFile(Path.Combine(_root, "data", Constants.FileName.ChineseCharacter), systemItems);

        var personalFile = Path.Combine(_root, "users", "u1", "content", SubjectType.Chinese.ToString(), $"{SubCategoryType.ChineseCharacter}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(personalFile)!);
        JsonHelper.SaveToFile(personalFile, new List<LearningItem>
        {
            new() { Id = "p-1", Subject = SubjectType.Chinese, SubCategory = SubCategoryType.ChineseCharacter, MainContent = "霜", Meaning = LearningAssistant.Models.Learning.ValueObjects.Meaning.Create("frost") },
            new() { Id = "p-2", Subject = SubjectType.Chinese, SubCategory = SubCategoryType.ChineseCharacter, MainContent = "露" }
        });

        var service = new ContentLoaderService(NullLogger<ContentLoaderService>.Instance, new FakeAppPaths(_root));
        var context = new LearningContext("u1", SubjectType.Chinese, SubCategoryType.ChineseCharacter);

        var items = service.LoadItems(context);

        Assert.Equal(3, items.Count);
        Assert.Single(items, i => i.MainContent == "霜");
        Assert.Equal("p-1", items.Single(i => i.MainContent == "霜").Id);
        Assert.Contains(items, i => i.MainContent == "露");
        Assert.Equal(3, service.GetItemCount(context));
    }

    [Fact]
    public void LoadItems_ReturnsPersonalContentWhenSystemFileMissing()
    {
        var personalFile = Path.Combine(_root, "users", "u1", "content", SubjectType.English.ToString(), $"{SubCategoryType.EnglishWord}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(personalFile)!);
        JsonHelper.SaveToFile(personalFile, new List<LearningItem>
        {
            new() { Id = "p-1", Subject = SubjectType.English, SubCategory = SubCategoryType.EnglishWord, MainContent = "hello" }
        });

        var service = new ContentLoaderService(NullLogger<ContentLoaderService>.Instance, new FakeAppPaths(_root));
        var items = service.LoadItems(new LearningContext("u1", SubjectType.English, SubCategoryType.EnglishWord));

        Assert.Single(items);
        Assert.Equal("hello", items[0].MainContent);
    }

    [Fact]
    public void LoadItems_FiltersByAgeImportanceAndSource()
    {
        var items = new List<LearningItem>
        {
            new()
            {
                Id = "1", Subject = SubjectType.English, SubCategory = SubCategoryType.EnglishWord,
                MainContent = "apple", AgeRange = AgeRange.PrimaryLow, Importance = ImportanceLevel.Key,
                SourceType = StudyContentSource.PdfHighlight, ContentDifficulty = 2
            },
            new()
            {
                Id = "2", Subject = SubjectType.English, SubCategory = SubCategoryType.EnglishWord,
                MainContent = "banana", AgeRange = AgeRange.All, Importance = ImportanceLevel.Normal
            },
            new()
            {
                Id = "3", Subject = SubjectType.English, SubCategory = SubCategoryType.EnglishWord,
                MainContent = "cherry", AgeRange = AgeRange.PrimaryLow, Importance = ImportanceLevel.Key
            }
        };
        JsonHelper.SaveToFile(Path.Combine(_root, "data", Constants.FileName.EnglishWord), items);

        var service = new ContentLoaderService(NullLogger<ContentLoaderService>.Instance, new FakeAppPaths(_root));
        var filtered = service.LoadItems(new LearningContext(
            "u1", SubjectType.English, SubCategoryType.EnglishWord,
            AgeRange: AgeRange.PrimaryLow,
            Importance: ImportanceLevel.Key,
            SourceType: StudyContentSource.PdfHighlight));

        Assert.Single(filtered);
        Assert.Equal("apple", filtered[0].MainContent);
        Assert.Equal(3, service.GetItemCount(new LearningContext("u1", SubjectType.English, SubCategoryType.EnglishWord)));
    }

    [Fact]
    public void JsonRoundTrip_PreservesClassificationAndSourceRef()
    {
        var item = new LearningItem
        {
            Id = "a-1",
            Subject = SubjectType.English,
            SubCategory = SubCategoryType.EnglishWord,
            MainContent = "apple",
            KnowledgePointId = "kp-1",
            KnowledgePointPath = "英语/词汇/食物",
            AgeRange = AgeRange.PrimaryLow,
            Importance = ImportanceLevel.Core,
            ContentDifficulty = 4,
            SourceType = StudyContentSource.PdfHighlight,
            SourceRef = new LearningSourceRef
            {
                PdfPath = "book.pdf",
                PdfHash = "hash",
                PageIndex = 12,
                HighlightId = "hl-9"
            }
        };

        var back = JsonHelper.Deserialize<LearningItem>(JsonHelper.Serialize(item));

        Assert.NotNull(back);
        Assert.Equal("kp-1", back!.KnowledgePointId);
        Assert.Equal("英语/词汇/食物", back.KnowledgePointPath);
        Assert.Equal(AgeRange.PrimaryLow, back.AgeRange);
        Assert.Equal(ImportanceLevel.Core, back.Importance);
        Assert.Equal(4, back.ContentDifficulty);
        Assert.Equal(StudyContentSource.PdfHighlight, back.SourceType);
        Assert.NotNull(back.SourceRef);
        Assert.Equal(12, back.SourceRef!.PageIndex);
        Assert.Equal("hl-9", back.SourceRef.HighlightId);
    }

    [Fact]
    public void GetFilterKey_IsolatesDifferentClassificationFilters()
    {
        var baseContext = new LearningContext("u1", SubjectType.English, SubCategoryType.EnglishWord);
        var ageContext = baseContext with { AgeRange = AgeRange.PrimaryLow };
        var importanceContext = baseContext with { Importance = ImportanceLevel.Core };

        Assert.Equal(string.Empty, baseContext.GetFilterKey());
        Assert.NotEqual(baseContext.GetFilterKey(), ageContext.GetFilterKey());
        Assert.NotEqual(ageContext.GetFilterKey(), importanceContext.GetFilterKey());
        Assert.Equal(ageContext.GetFilterKey(), (baseContext with { AgeRange = AgeRange.PrimaryLow }).GetFilterKey());
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }
        catch
        {
            // 临时目录清理失败不影响测试结果。
        }
    }

    private sealed class FakeAppPaths : IAppPaths
    {
        private readonly string _root;
        public FakeAppPaths(string root) => _root = root;

        public string AssemblyDir => _root;
        public string DataRoot => _root;
        public string ConfigDir => Path.Combine(_root, "config");
        public string DataDir => Path.Combine(_root, "data");
        public string UsersDir => Path.Combine(_root, "users");
        public string CurrentUserDir => Path.Combine(UsersDir, "u1");
        public string LogsDir => Path.Combine(_root, "logs");
        public string CacheDir => Path.Combine(_root, "cache");
        public string DatabaseDir => Path.Combine(_root, "db");
        public string DatabasePath => Path.Combine(_root, "db", "db.sqlite");
        public string SessionDir => Path.Combine(_root, "session");
        public string ExportsDir => Path.Combine(_root, "exports");
        public string AnnotationsDir => Path.Combine(_root, "annotations");
        public string TranslationsDir => Path.Combine(_root, "translations");
        public string FileBookmarksDir => Path.Combine(_root, "bookmarks");
        public string HighlightsDir => Path.Combine(_root, "highlights");
        public string RecommendationFeedbackDir => Path.Combine(_root, "feedback");
        public string ThumbnailsDir => Path.Combine(_root, "thumbs");
        public string AudioDir => Path.Combine(_root, "audio");
        public string TTSCacheDir => Path.Combine(_root, "tts");
        public string TesseractDataDir => Path.Combine(_root, "tess");
        public string UserDataDir => CurrentUserDir;
        public string TempDir => Path.Combine(_root, "temp");

        public string AppSettingsPath => Path.Combine(_root, "appsettings.json");
        public string AiPromptsPath => Path.Combine(_root, "ai.json");
        public string PromptTemplatesPath => Path.Combine(_root, "prompts.json");
        public string WebBookmarksPath => Path.Combine(_root, "web.json");
        public string SubjectTemplatesPath => Path.Combine(_root, "subjects.json");
        public string EncouragementConfigPath => Path.Combine(_root, "encouragement.json");
        public string PendingContentPath => Path.Combine(_root, "pending.json");
        public string BrowserTabsPath => Path.Combine(_root, "tabs.json");
        public string UserBookmarksPath => Path.Combine(_root, "bookmarks.json");

        public void SetCurrentUserId(string userId) { }
        public string GetCurrentUserId() => "u1";
        public void EnsureDirectoriesExist() { }
        public void EnsureUserDirectoriesExist(string? userId = null) { }
        public void EnsureDirectoryExists(string path) => Directory.CreateDirectory(path);
        public string GetUserDir(string? userId = null) => Path.Combine(UsersDir, string.IsNullOrWhiteSpace(userId) ? "u1" : userId);
        public string GetUserAnnotationsDir(string? userId = null) => Path.Combine(GetUserDir(userId), "annotations");
        public string GetUserAnnotationPath(string pdfPath, int pageIndex, string? userId = null) => Path.Combine(GetUserAnnotationsDir(userId), $"{pageIndex}.json");
        public string GetUserBookmarksDir(string? userId = null) => Path.Combine(GetUserDir(userId), "bookmarks");
        public string GetUserPdfBookmarkPath(string pdfPath, string? userId = null) => Path.Combine(GetUserBookmarksDir(userId), "pdf.json");
        public string GetUserHighlightsDir(string? userId = null) => Path.Combine(GetUserDir(userId), "highlights");
        public string GetUserFolderHighlightPath(string folderPath, string? userId = null) => Path.Combine(GetUserHighlightsDir(userId), "folder.json");
        public string GetUserTranslationsDir(string? userId = null) => Path.Combine(GetUserDir(userId), "translations");
        public string GetTtsCacheDir() => TTSCacheDir;
        public string GetUserAutoSaveDir(string? userId = null) => Path.Combine(GetUserDir(userId), "autosave");
        public string GetUserBackupDir(string? userId = null) => Path.Combine(GetUserDir(userId), "backup");
        public string GetUserFavoritesPath(string? userId = null) => Path.Combine(GetUserDir(userId), "favorites.json");
        public string GetAnnotationPath(string pdfPath, int pageIndex) => Path.Combine(AnnotationsDir, $"{pageIndex}.json");
        public string GetPdfBookmarkPath(string pdfPath) => Path.Combine(FileBookmarksDir, "pdf.json");
        public string GetFolderHighlightPath(string folderPath) => Path.Combine(HighlightsDir, "folder.json");
        public string GetCacheFilePath(string key) => Path.Combine(CacheDir, $"{key}.json");
        public string GetDataFilePath(string fileName) => Path.Combine(DataDir, fileName);
        public string FindConfigFile(string fileName, string?[]? searchPaths = null) => Path.Combine(ConfigDir, fileName);
    }
}
