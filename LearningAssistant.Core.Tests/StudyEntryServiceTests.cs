using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Learning;
using Xunit;

namespace LearningAssistant.Core.Tests;

public class StudyEntryServiceTests
{
    private static StudyEntryService CreateService(FakeStore store, string userId = "user-1")
        => new(store, new FakeSession { CurrentUserId = userId });

    [Fact]
    public async Task AddAsync_EnglishWord_IsClassifiedAsWord()
    {
        var store = new FakeStore();
        var result = await CreateService(store).AddAsync(new StudyEntryRequest
        {
            Text = "hello",
            SourceType = "PdfHighlight"
        });

        Assert.True(result.Created);
        Assert.Equal(SubjectType.English, result.Item!.Subject);
        Assert.Equal(SubCategoryType.EnglishWord, result.Item.SubCategory);
        Assert.Equal("PdfHighlight", result.Item.GetExtendedProperty<string>("SourceType"));
    }

    [Fact]
    public async Task AddAsync_Duplicate_ReturnsExistingId()
    {
        var store = new FakeStore();
        var service = CreateService(store);

        var first = await service.AddAsync(new StudyEntryRequest { Text = "hello" });
        var second = await service.AddAsync(new StudyEntryRequest { Text = "  HELLO  " });

        Assert.True(first.Created);
        Assert.True(second.AlreadyExists);
        Assert.False(second.Created);
        Assert.Equal(first.ItemId, second.ItemId);
        Assert.Single(store.ItemsFor(SubjectType.English, SubCategoryType.EnglishWord));
    }

    [Theory]
    [InlineData("This is a full sentence.", SubCategoryType.EnglishSentence)]
    [InlineData("look forward to", SubCategoryType.EnglishPhrase)]
    [InlineData("霜", SubCategoryType.ChineseCharacter)]
    public async Task AddAsync_InfersSubCategory(string text, SubCategoryType expected)
    {
        var store = new FakeStore();
        var result = await CreateService(store).AddAsync(new StudyEntryRequest { Text = text });

        Assert.NotNull(result.Item);
        Assert.Equal(expected, result.Item!.SubCategory);
    }

    [Fact]
    public async Task AddAsync_CleansWrappingQuotesAndWhitespace()
    {
        var store = new FakeStore();
        var result = await CreateService(store).AddAsync(new StudyEntryRequest
        {
            Text = "\"  hello    world  \""
        });

        Assert.Equal("hello world", result.Item!.MainContent);
    }

    [Fact]
    public async Task AddAsync_KeepsTranslationAndNote()
    {
        var store = new FakeStore();
        var result = await CreateService(store).AddAsync(new StudyEntryRequest
        {
            Text = "hello",
            Translation = "你好",
            Note = "Greeting"
        });

        Assert.Equal("你好", result.Item!.Meaning?.Content);
        Assert.Equal("Greeting", result.Item.Example?.Content);
    }

    [Fact]
    public async Task AddAsync_TooLong_Throws()
    {
        var store = new FakeStore();
        var service = CreateService(store);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddAsync(new StudyEntryRequest { Text = new string('a', 2001) }));
    }

    private sealed class FakeSession : IUserSessionService
    {
        public string CurrentUserId { get; set; } = "user-1";
        public string LoadSession() => CurrentUserId;
        public void SaveSession(string userId) => CurrentUserId = userId;
        public List<string> GetUserList() => new();
        public UserProfile LoadUserProfile(string userId) => new();
        public void SaveLearningConfig(LearningConfig config) { }
        public LearningConfig LoadLearningConfig() => new();
    }

    private sealed class FakeStore : IPersonalContentStore
    {
        private readonly Dictionary<(SubjectType, SubCategoryType), List<LearningItem>> _items = new();

        public string GetFilePath(string userId, SubjectType subject, SubCategoryType subCategory)
            => $"{userId}/{subject}/{subCategory}.json";

        public IReadOnlyList<LearningItem> Load(string userId, SubjectType subject, SubCategoryType subCategory)
            => ItemsFor(subject, subCategory);

        public IReadOnlyList<LearningItem> LoadAll(string userId)
            => _items.Values.SelectMany(list => list).ToList();

        public void Upsert(string userId, SubjectType subject, SubCategoryType subCategory, LearningItem item)
            => ItemsFor(subject, subCategory).Add(item);

        public bool Remove(string userId, SubjectType subject, SubCategoryType subCategory, string canonicalKey)
        {
            var item = ItemsFor(subject, subCategory).FirstOrDefault(i =>
                LearningItemIdentity.GetCanonicalKey(i) == canonicalKey);
            if (item == null) return false;
            ItemsFor(subject, subCategory).Remove(item);
            return true;
        }

        public List<LearningItem> ItemsFor(SubjectType subject, SubCategoryType subCategory)
        {
            if (!_items.TryGetValue((subject, subCategory), out var list))
            {
                list = new List<LearningItem>();
                _items[(subject, subCategory)] = list;
            }
            return list;
        }
    }
}
