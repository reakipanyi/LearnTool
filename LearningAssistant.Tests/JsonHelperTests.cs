using Xunit;
using FluentAssertions;
using LearningAssistant.Common;
using System.IO;

namespace LearningAssistant.Tests
{
    public class JsonHelperTests : IDisposable
    {
        private readonly string _testDirectory;

        public JsonHelperTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"json_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public void Serialize_WithObject_ShouldReturnJsonString()
        {
            var testObj = new TestClass { Name = "Test", Value = 42 };
            var result = JsonHelper.Serialize(testObj);
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("\"Name\":");
            result.Should().Contain("\"Value\":");
        }

        [Fact]
        public void Deserialize_WithValidJson_ShouldReturnObject()
        {
            var json = "{\"Name\":\"Test\",\"Value\":42}";
            var result = JsonHelper.Deserialize<TestClass>(json);
            result.Should().NotBeNull();
            result!.Name.Should().Be("Test");
            result.Value.Should().Be(42);
        }

        [Fact]
        public void Deserialize_WithEmptyString_ShouldReturnDefault()
        {
            var result = JsonHelper.Deserialize<TestClass>(string.Empty);
            result.Should().BeNull();
        }

        [Fact]
        public void Deserialize_WithNullString_ShouldReturnDefault()
        {
            var result = JsonHelper.Deserialize<TestClass>(null!);
            result.Should().BeNull();
        }

        [Fact]
        public void Deserialize_WithInvalidJson_ShouldReturnDefault()
        {
            var result = JsonHelper.Deserialize<TestClass>("invalid json");
            result.Should().BeNull();
        }

        [Fact]
        public void LoadFromFile_WithExistingFile_ShouldReturnObject()
        {
            var filePath = Path.Combine(_testDirectory, "test.json");
            File.WriteAllText(filePath, "{\"Name\":\"Test\",\"Value\":42}");

            var result = JsonHelper.LoadFromFile<TestClass>(filePath);
            result.Should().NotBeNull();
            result!.Name.Should().Be("Test");
        }

        [Fact]
        public void LoadFromFile_WithNonExistingFile_ShouldReturnDefault()
        {
            var filePath = Path.Combine(_testDirectory, "nonexistent.json");
            var result = JsonHelper.LoadFromFile<TestClass>(filePath);
            result.Should().BeNull();
        }

        [Fact]
        public void SaveToFile_WithObject_ShouldCreateFile()
        {
            var filePath = Path.Combine(_testDirectory, "output.json");
            var testObj = new TestClass { Name = "SaveTest", Value = 100 };

            JsonHelper.SaveToFile(filePath, testObj);

            File.Exists(filePath).Should().BeTrue();
            var content = File.ReadAllText(filePath);
            content.Should().Contain("SaveTest");
        }

        [Fact]
        public void SaveToFile_WithNestedDirectory_ShouldCreateDirectory()
        {
            var nestedPath = Path.Combine(_testDirectory, "nested", "output.json");
            var testObj = new TestClass { Name = "NestedTest", Value = 200 };

            JsonHelper.SaveToFile(nestedPath, testObj);

            File.Exists(nestedPath).Should().BeTrue();
        }

        [Fact]
        public void SerializeToBytes_WithObject_ShouldReturnBytes()
        {
            var testObj = new TestClass { Name = "ByteTest", Value = 50 };
            var result = JsonHelper.SerializeToBytes(testObj);
            result.Should().NotBeNull();
            result.Length.Should().BeGreaterThan(0);
        }

        [Fact]
        public void DeserializeFromBytes_WithValidBytes_ShouldReturnObject()
        {
            var testObj = new TestClass { Name = "ByteTest", Value = 50 };
            var bytes = JsonHelper.SerializeToBytes(testObj);
            var result = JsonHelper.DeserializeFromBytes<TestClass>(bytes);
            
            result.Should().NotBeNull();
            result!.Name.Should().Be("ByteTest");
        }

        [Fact]
        public void DeserializeFromBytes_WithEmptyArray_ShouldReturnDefault()
        {
            var result = JsonHelper.DeserializeFromBytes<TestClass>(Array.Empty<byte>());
            result.Should().BeNull();
        }

        [Fact]
        public void DeserializeFromBytes_WithNull_ShouldReturnDefault()
        {
            var result = JsonHelper.DeserializeFromBytes<TestClass>(null!);
            result.Should().BeNull();
        }

        public class TestClass
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }
    }
}