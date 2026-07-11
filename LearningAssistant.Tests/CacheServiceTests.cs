using Xunit;
using FluentAssertions;
using System.IO;
using LearningAssistant.Services.Cache;

namespace LearningAssistant.Tests
{
    public class CacheServiceTests : IDisposable
    {
        private readonly string _testCachePath;
        private readonly CacheService _cacheService;

        public CacheServiceTests()
        {
            _testCachePath = Path.Combine(Path.GetTempPath(), $"test_cache_{Guid.NewGuid()}.json");
            _cacheService = new CacheService(_testCachePath);
        }

        public void Dispose()
        {
            if (File.Exists(_testCachePath))
            {
                try { File.Delete(_testCachePath); } catch { }
            }
        }

        [Fact]
        public void SetAndGet_WithStringValue_ShouldReturnValue()
        {
            var key = "test_key";
            var value = "test_value";

            _cacheService.Set(key, value);
            _cacheService.TryGet(key, out string result);

            result.Should().Be(value);
        }

        [Fact]
        public void SetAndGet_WithComplexObject_ShouldReturnValue()
        {
            var key = "test_object";
            var value = new TestObject { Name = "Test", Age = 25 };

            _cacheService.Set(key, value);
            _cacheService.TryGet(key, out TestObject result);

            result.Should().NotBeNull();
            result!.Name.Should().Be(value.Name);
            result.Age.Should().Be(value.Age);
        }

        [Fact]
        public void Get_WithNonExistentKey_ShouldReturnDefault()
        {
            var key = "non_existent_key";

            _cacheService.TryGet(key, out string result);

            result.Should().BeNull();
        }

        [Fact]
        public void Remove_WithExistingKey_ShouldRemoveValue()
        {
            var key = "key_to_remove";
            _cacheService.Set(key, "value");

            _cacheService.Remove(key);
            _cacheService.TryGet(key, out string result);

            result.Should().BeNull();
        }

        [Fact]
        public void Clear_ShouldRemoveAllValues()
        {
            _cacheService.Set("key1", "value1");
            _cacheService.Set("key2", "value2");

            _cacheService.Clear();
            _cacheService.TryGet("key1", out string result1);
            _cacheService.TryGet("key2", out string result2);

            result1.Should().BeNull();
            result2.Should().BeNull();
        }

        [Fact]
        public void Set_WithNullKey_ShouldThrow()
        {
            Action act = () => _cacheService.Set(null!, "value");

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Set_WithEmptyKey_ShouldThrow()
        {
            Action act = () => _cacheService.Set("", "value");

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Set_WithNullValue_ShouldStoreNull()
        {
            var key = "null_value_key";

            _cacheService.Set<object?>(key, null);
            _cacheService.TryGet(key, out object? result);

            result.Should().BeNull();
        }

        [Fact]
        public void Remove_WithNonExistentKey_ShouldNotThrow()
        {
            Action act = () => _cacheService.Remove("non_existent_key");

            act.Should().NotThrow();
        }

        [Fact]
        public void Set_MultipleValues_ShouldAllBeRetrievable()
        {
            var values = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }
            };

            foreach (var kvp in values)
            {
                _cacheService.Set(kvp.Key, kvp.Value);
            }

            foreach (var kvp in values)
            {
                _cacheService.TryGet(kvp.Key, out string result);
                result.Should().Be(kvp.Value);
            }
        }
    }

    public class TestObject
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}