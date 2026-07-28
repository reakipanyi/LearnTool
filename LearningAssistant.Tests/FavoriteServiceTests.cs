using Moq;
using Xunit;
using FluentAssertions;
using LearningAssistant.Services.Learning;
using LearningAssistant.Common;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Tests
{
    public class FavoriteServiceTests : IDisposable
    {
        private readonly Mock<ILogger<FavoriteService>> _mockLogger;
        private readonly string _testUserId;
        private readonly string _testUserDir;
        private FavoriteService? _service;

        public FavoriteServiceTests()
        {
            _mockLogger = new Mock<ILogger<FavoriteService>>();
            _testUserId = $"test_user_{Guid.NewGuid():N}";
            _testUserDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "LearningAssistant", "test_users", _testUserId);
            Directory.CreateDirectory(_testUserDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testUserDir))
            {
                Directory.Delete(_testUserDir, recursive: true);
            }
        }

        private FavoriteService CreateService()
        {
            return new FavoriteService(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            Action action = () => new FavoriteService(null!);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task IsFavoriteAsync_WithEmptyUserId_ShouldReturnFalse()
        {
            _service = CreateService();

            var result = await _service.IsFavoriteAsync(string.Empty, SubCategoryType.EnglishWord, "test");

            result.Should().Be(false);
        }

        [Fact]
        public async Task IsFavoriteAsync_WithEmptyContent_ShouldReturnFalse()
        {
            _service = CreateService();

            var result = await _service.IsFavoriteAsync("test_user", SubCategoryType.EnglishWord, string.Empty);

            result.Should().Be(false);
        }

        [Fact]
        public async Task IsFavoriteAsync_WithNullUserId_ShouldReturnFalse()
        {
            _service = CreateService();

            var result = await _service.IsFavoriteAsync(null!, SubCategoryType.EnglishWord, "test");

            result.Should().Be(false);
        }

        [Fact]
        public async Task IsFavoriteAsync_WithNullContent_ShouldReturnFalse()
        {
            _service = CreateService();

            var result = await _service.IsFavoriteAsync("test_user", SubCategoryType.EnglishWord, null!);

            result.Should().Be(false);
        }

        [Fact]
        public async Task IsFavoriteAsync_WithNonExistentFavorite_ShouldReturnFalse()
        {
            _service = CreateService();

            var result = await _service.IsFavoriteAsync(_testUserId, SubCategoryType.EnglishWord, "apple");

            result.Should().Be(false);
        }

        [Fact]
        public async Task AddFavoriteAsync_ShouldAddFavorite()
        {
            _service = CreateService();

            await _service.AddFavoriteAsync(_testUserId, SubCategoryType.EnglishWord, "apple");

            var isFavorite = await _service.IsFavoriteAsync(_testUserId, SubCategoryType.EnglishWord, "apple");
            isFavorite.Should().Be(true);
        }

        [Fact]
        public async Task AddFavoriteAsync_WithEmptyUserId_ShouldNotThrow()
        {
            _service = CreateService();

            Func<Task> action = () => _service.AddFavoriteAsync(string.Empty, SubCategoryType.EnglishWord, "test");

            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task AddFavoriteAsync_WithEmptyContent_ShouldNotThrow()
        {
            _service = CreateService();

            Func<Task> action = () => _service.AddFavoriteAsync("test_user", SubCategoryType.EnglishWord, string.Empty);

            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task AddFavoriteAsync_WithSameItemTwice_ShouldNotDuplicate()
        {
            _service = CreateService();

            await _service.AddFavoriteAsync(_testUserId, SubCategoryType.EnglishWord, "apple");
            await _service.AddFavoriteAsync(_testUserId, SubCategoryType.EnglishWord, "apple");

            var favorites = await _service.GetUserFavoritesAsync(_testUserId);
            favorites.Count.Should().Be(1);
        }

        [Fact]
        public async Task RemoveFavoriteAsync_ShouldRemoveFavorite()
        {
            _service = CreateService();

            await _service.AddFavoriteAsync(_testUserId, SubCategoryType.EnglishWord, "apple");
            var isFavoriteBefore = await _service.IsFavoriteAsync(_testUserId, SubCategoryType.EnglishWord, "apple");
            isFavoriteBefore.Should().Be(true);

            await _service.RemoveFavoriteAsync(_testUserId, SubCategoryType.EnglishWord, "apple");

            var isFavoriteAfter = await _service.IsFavoriteAsync(_testUserId, SubCategoryType.EnglishWord, "apple");
            isFavoriteAfter.Should().Be(false);
        }

        [Fact]
        public async Task RemoveFavoriteAsync_WithEmptyUserId_ShouldNotThrow()
        {
            _service = CreateService();

            Func<Task> action = () => _service.RemoveFavoriteAsync(string.Empty, SubCategoryType.EnglishWord, "test");

            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task RemoveFavoriteAsync_WithEmptyContent_ShouldNotThrow()
        {
            _service = CreateService();

            Func<Task> action = () => _service.RemoveFavoriteAsync("test_user", SubCategoryType.EnglishWord, string.Empty);

            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task RemoveFavoriteAsync_WithNonExistentItem_ShouldNotThrow()
        {
            _service = CreateService();

            Func<Task> action = () => _service.RemoveFavoriteAsync(_testUserId, SubCategoryType.EnglishWord, "nonexistent");

            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task GetUserFavoritesAsync_WithEmptyUserId_ShouldReturnEmpty()
        {
            _service = CreateService();

            var favorites = await _service.GetUserFavoritesAsync(string.Empty);

            favorites.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserFavoritesAsync_WithNullUserId_ShouldReturnEmpty()
        {
            _service = CreateService();

            var favorites = await _service.GetUserFavoritesAsync(null!);

            favorites.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserFavoritesAsync_WithNoFavorites_ShouldReturnEmpty()
        {
            _service = CreateService();

            var favorites = await _service.GetUserFavoritesAsync(_testUserId);

            favorites.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserFavoritesAsync_WithMultipleFavorites_ShouldReturnAll()
        {
            _service = CreateService();

            await _service.AddFavoriteAsync(_testUserId, SubCategoryType.EnglishWord, "apple");
            await _service.AddFavoriteAsync(_testUserId, SubCategoryType.EnglishWord, "banana");
            await _service.AddFavoriteAsync(_testUserId, SubCategoryType.ChinesePoem, "静夜思");

            var favorites = await _service.GetUserFavoritesAsync(_testUserId);

            favorites.Count.Should().Be(3);
            favorites.Should().Contain("[EnglishWord]apple");
            favorites.Should().Contain("[EnglishWord]banana");
            favorites.Should().Contain("[ChinesePoem]静夜思");
        }

        [Fact]
        public async Task GetUserFavoritesAsync_ShouldCacheResults()
        {
            _service = CreateService();

            await _service.AddFavoriteAsync(_testUserId, SubCategoryType.EnglishWord, "apple");

            var favorites1 = await _service.GetUserFavoritesAsync(_testUserId);
            favorites1.Count.Should().Be(1);

            var favorites2 = await _service.GetUserFavoritesAsync(_testUserId);
            favorites2.Count.Should().Be(1);

            favorites1.Should().BeSameAs(favorites2);
        }

        [Fact]
        public async Task InvalidateCache_ShouldClearCache()
        {
            _service = CreateService();

            await _service.AddFavoriteAsync(_testUserId, SubCategoryType.EnglishWord, "apple");

            var favorites1 = await _service.GetUserFavoritesAsync(_testUserId);

            _service.InvalidateCache(_testUserId);

            var favorites2 = await _service.GetUserFavoritesAsync(_testUserId);

            favorites1.Should().NotBeSameAs(favorites2);
        }

        [Fact]
        public void InvalidateCache_WithEmptyUserId_ShouldNotThrow()
        {
            _service = CreateService();

            Action action = () => _service.InvalidateCache(string.Empty);

            action.Should().NotThrow();
        }

        [Fact]
        public void InvalidateCache_WithNullUserId_ShouldNotThrow()
        {
            _service = CreateService();

            Action action = () => _service.InvalidateCache(null!);

            action.Should().NotThrow();
        }

        [Fact]
        public async Task DifferentUsers_ShouldHaveSeparateFavorites()
        {
            _service = CreateService();

            await _service.AddFavoriteAsync(_testUserId, SubCategoryType.EnglishWord, "apple");

            var otherUserId = $"other_user_{Guid.NewGuid():N}";
            var isFavorite = await _service.IsFavoriteAsync(otherUserId, SubCategoryType.EnglishWord, "apple");

            isFavorite.Should().Be(false);
        }

        [Fact]
        public async Task DifferentSubCategories_ShouldHaveSeparateKeys()
        {
            _service = CreateService();

            await _service.AddFavoriteAsync(_testUserId, SubCategoryType.EnglishWord, "apple");

            var isFavorite = await _service.IsFavoriteAsync(_testUserId, SubCategoryType.EnglishPhrase, "apple");

            isFavorite.Should().Be(false);
        }
    }
}