using Xunit;
using FluentAssertions;
using System.IO;
using UnifiedLearningAssistant.Services.Cache;

namespace UnifiedLearningAssistant.Tests
{
    /// <summary>
    /// 测试 CacheService 服务
    /// </summary>
    public class CacheServiceTests : IDisposable
    {
        private readonly string _testCachePath;
        private readonly CacheService _cacheService;

        public CacheServiceTests()
        {
            // 创建临时测试文件
            _testCachePath = Path.Combine(Path.GetTempPath(), $"test_cache_{Guid.NewGuid()}.json");
            _cacheService = new CacheService(_testCachePath);
        }

        public void Dispose()
        {
            // 清理测试文件
            if (File.Exists(_testCachePath))
            {
                try { File.Delete(_testCachePath); } catch { }
            }
        }

        [Fact]
        public void SetAndGet_WithStringValue_ShouldReturnValue()
        {
            // Arrange
            var key = "test_key";
            var value = "test_value";

            // Act
            _cacheService.Set(key, value);
            var result = _cacheService.Get<string>(key);

            // Assert
            result.Should().Be(value);
        }

        [Fact]
        public void SetAndGet_WithComplexObject_ShouldReturnValue()
        {
            // Arrange
            var key = "test_object";
            var value = new TestObject { Name = "Test", Age = 25 };

            // Act
            _cacheService.Set(key, value);
            var result = _cacheService.Get<TestObject>(key);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be(value.Name);
            result.Age.Should().Be(value.Age);
        }

        [Fact]
        public void Get_WithNonExistentKey_ShouldReturnDefault()
        {
            // Arrange
            var key = "non_existent_key";

            // Act
            var result = _cacheService.Get<string>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Remove_WithExistingKey_ShouldRemoveValue()
        {
            // Arrange
            var key = "key_to_remove";
            _cacheService.Set(key, "value");

            // Act
            _cacheService.Remove(key);
            var result = _cacheService.Get<string>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Clear_ShouldRemoveAllValues()
        {
            // Arrange
            _cacheService.Set("key1", "value1");
            _cacheService.Set("key2", "value2");

            // Act
            _cacheService.Clear();
            var result1 = _cacheService.Get<string>("key1");
            var result2 = _cacheService.Get<string>("key2");

            // Assert
            result1.Should().BeNull();
            result2.Should().BeNull();
        }

        // 边缘情况测试

        [Fact]
        public void Set_WithNullKey_ShouldThrow()
        {
            // Act
            Action act = () => _cacheService.Set(null!, "value");

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Set_WithEmptyKey_ShouldThrow()
        {
            // Act
            Action act = () => _cacheService.Set("", "value");

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Set_WithNullValue_ShouldStoreNull()
        {
            // Arrange
            var key = "null_value_key";

            // Act
            _cacheService.Set<object?>(key, null);
            var result = _cacheService.Get<object?>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Get_WithNullKey_ShouldThrow()
        {
            // Act
            Action act = () => _cacheService.Get<string>(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Remove_WithNonExistentKey_ShouldNotThrow()
        {
            // Act
            Action act = () => _cacheService.Remove("non_existent_key");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Set_MultipleValues_ShouldAllBeRetrievable()
        {
            // Arrange
            var values = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }
            };

            // Act
            foreach (var kvp in values)
            {
                _cacheService.Set(kvp.Key, kvp.Value);
            }

            // Assert
            foreach (var kvp in values)
            {
                _cacheService.Get<string>(kvp.Key).Should().Be(kvp.Value);
            }
        }
    }

    public class TestObject
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}
