using Xunit;
using FluentAssertions;
using LearningAssistant.Common;
using System.IO;

namespace LearningAssistant.Tests
{
    public class FileHelperTests : IDisposable
    {
        private readonly string _testDirectory;

        public FileHelperTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"filehelper_test_{Guid.NewGuid()}");
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
        public void GetAppDirectory_ShouldReturnBaseDirectory()
        {
            var result = FileHelper.GetAppDirectory();
            result.Should().NotBeNullOrEmpty();
            Directory.Exists(result).Should().BeTrue();
        }

        [Fact]
        public void EnsureDirectoryExists_WithNonExistentPath_ShouldCreateDirectory()
        {
            var newDir = Path.Combine(_testDirectory, "new_folder");
            FileHelper.EnsureDirectoryExists(newDir);
            Directory.Exists(newDir).Should().BeTrue();
        }

        [Fact]
        public void EnsureDirectoryExists_WithExistingPath_ShouldNotThrow()
        {
            var existingDir = Path.Combine(_testDirectory, "existing_folder");
            Directory.CreateDirectory(existingDir);
            
            Action act = () => FileHelper.EnsureDirectoryExists(existingDir);
            act.Should().NotThrow();
        }

        [Fact]
        public void FileExists_WithExistingFile_ShouldReturnTrue()
        {
            var testFile = Path.Combine(_testDirectory, "test.txt");
            File.Create(testFile).Close();
            
            var result = FileHelper.FileExists(testFile);
            result.Should().BeTrue();
        }

        [Fact]
        public void FileExists_WithNonExistingFile_ShouldReturnFalse()
        {
            var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.txt");
            var result = FileHelper.FileExists(nonExistentFile);
            result.Should().BeFalse();
        }

        [Fact]
        public void FileExists_WithNullPath_ShouldReturnFalse()
        {
            var result = FileHelper.FileExists(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void FileExists_WithEmptyPath_ShouldReturnFalse()
        {
            var result = FileHelper.FileExists(string.Empty);
            result.Should().BeFalse();
        }

        [Fact]
        public void GetUniqueFileName_WithNonExistingFile_ShouldReturnBaseName()
        {
            var baseName = "test";
            var extension = "txt";
            
            var result = FileHelper.GetUniqueFileName(_testDirectory, baseName, extension);
            result.Should().Be(Path.Combine(_testDirectory, "test.txt"));
        }

        [Fact]
        public void GetUniqueFileName_WithExistingFile_ShouldAppendCounter()
        {
            var baseName = "test";
            var extension = "txt";
            var existingFile = Path.Combine(_testDirectory, "test.txt");
            File.Create(existingFile).Close();

            var result = FileHelper.GetUniqueFileName(_testDirectory, baseName, extension);
            result.Should().Be(Path.Combine(_testDirectory, "test_1.txt"));
        }

        [Fact]
        public void GetUniqueFileName_WithMultipleExistingFiles_ShouldIncrementCounter()
        {
            var baseName = "test";
            var extension = "txt";
            
            for (int i = 1; i <= 3; i++)
            {
                var fileName = i == 1 ? "test.txt" : $"test_{i - 1}.txt";
                File.Create(Path.Combine(_testDirectory, fileName)).Close();
            }

            var result = FileHelper.GetUniqueFileName(_testDirectory, baseName, extension);
            result.Should().Be(Path.Combine(_testDirectory, "test_3.txt"));
        }

        [Fact]
        public void GetFilesByExtension_WithExistingFiles_ShouldReturnFiles()
        {
            File.Create(Path.Combine(_testDirectory, "file1.txt")).Close();
            File.Create(Path.Combine(_testDirectory, "file2.txt")).Close();
            File.Create(Path.Combine(_testDirectory, "file3.json")).Close();

            var result = FileHelper.GetFilesByExtension(_testDirectory, "txt").ToList();
            result.Should().HaveCount(2);
            result.All(f => f.EndsWith(".txt")).Should().BeTrue();
        }

        [Fact]
        public void GetFilesByExtension_WithNonExistingDirectory_ShouldReturnEmpty()
        {
            var nonExistentDir = Path.Combine(_testDirectory, "nonexistent");
            var result = FileHelper.GetFilesByExtension(nonExistentDir, "txt");
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetUserProgressPath_ShouldReturnCorrectPath()
        {
            var userName = "testuser";
            var result = FileHelper.GetUserProgressPath(userName);
            
            result.Should().EndWith($"Users{Path.DirectorySeparatorChar}{userName}.json");
        }

        [Fact]
        public void GetAnnotationPath_ShouldReturnCorrectPath()
        {
            var pdfPath = @"C:\test\document.pdf";
            var pageIndex = 5;
            var result = FileHelper.GetAnnotationPath(pdfPath, pageIndex);
            
            result.Should().EndWith($"Annotations{Path.DirectorySeparatorChar}document_page5.json");
        }

        [Fact]
        public void GetCacheFilePath_ShouldReturnCorrectPath()
        {
            var key = "mycachekey";
            var result = FileHelper.GetCacheFilePath(key);
            
            result.Should().EndWith($"Cache{Path.DirectorySeparatorChar}{key}.json");
        }
    }
}