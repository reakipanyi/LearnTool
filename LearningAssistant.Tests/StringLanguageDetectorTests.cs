using Xunit;
using FluentAssertions;
using KidWinApp.Services;

namespace LearningAssistant.Tests
{
    public class StringLanguageDetectorTests
    {
        [Fact]
        public void DetectLanguage_WithEmptyString_ShouldReturnUnknown()
        {
            var result = StringLanguageDetector.DetectLanguage(string.Empty);
            result.Should().Be(LanguageType.Unknown);
        }

        [Fact]
        public void DetectLanguage_WithNullString_ShouldReturnUnknown()
        {
            var result = StringLanguageDetector.DetectLanguage(null!);
            result.Should().Be(LanguageType.Unknown);
        }

        [Fact]
        public void DetectLanguage_WithPureChinese_ShouldReturnChinese()
        {
            var result = StringLanguageDetector.DetectLanguage("你好世界");
            result.Should().Be(LanguageType.Chinese);
        }

        [Fact]
        public void DetectLanguage_WithPureEnglish_ShouldReturnEnglish()
        {
            var result = StringLanguageDetector.DetectLanguage("Hello World");
            result.Should().Be(LanguageType.English);
        }

        [Fact]
        public void DetectLanguage_WithMixedChineseAndEnglish_ShouldReturnMixed()
        {
            var result = StringLanguageDetector.DetectLanguage("你好 World");
            result.Should().Be(LanguageType.Mixed);
        }

        [Fact]
        public void DetectLanguage_WithSpecialCharactersOnly_ShouldReturnUnknown()
        {
            var result = StringLanguageDetector.DetectLanguage("!@#$%^&*");
            result.Should().Be(LanguageType.Unknown);
        }

        [Fact]
        public void DetectLanguage_WithNumbersOnly_ShouldReturnUnknown()
        {
            var result = StringLanguageDetector.DetectLanguage("123456789");
            result.Should().Be(LanguageType.Unknown);
        }

        [Fact]
        public void DetectLanguage_WithChineseAndNumbers_ShouldReturnChinese()
        {
            var result = StringLanguageDetector.DetectLanguage("中文123");
            result.Should().Be(LanguageType.Chinese);
        }

        [Fact]
        public void DetectLanguage_WithEnglishAndNumbers_ShouldReturnEnglish()
        {
            var result = StringLanguageDetector.DetectLanguage("test123");
            result.Should().Be(LanguageType.English);
        }

        [Fact]
        public void DetectLanguageRegex_WithPureChinese_ShouldReturnChinese()
        {
            var result = StringLanguageDetector.DetectLanguageRegex("你好世界");
            result.Should().Be(LanguageType.Chinese);
        }

        [Fact]
        public void DetectLanguageRegex_WithPureEnglish_ShouldReturnEnglish()
        {
            var result = StringLanguageDetector.DetectLanguageRegex("Hello World");
            result.Should().Be(LanguageType.English);
        }

        [Fact]
        public void DetectLanguageRegex_WithMixedText_ShouldReturnMixed()
        {
            var result = StringLanguageDetector.DetectLanguageRegex("你好 World");
            result.Should().Be(LanguageType.Mixed);
        }

        [Fact]
        public void DetailedDetect_WithEmptyString_ShouldReturnZeroCounts()
        {
            var result = StringLanguageDetector.DetailedDetect(string.Empty);
            result.ChineseCount.Should().Be(0);
            result.EnglishCount.Should().Be(0);
            result.OtherCount.Should().Be(0);
            result.LanguageType.Should().Be(LanguageType.Unknown);
        }

        [Fact]
        public void DetailedDetect_WithMixedText_ShouldCountAllCharacters()
        {
            var result = StringLanguageDetector.DetailedDetect("你好 World!");
            result.ChineseCount.Should().Be(2);
            result.EnglishCount.Should().Be(6);
            result.OtherCount.Should().Be(2);
            result.LanguageType.Should().Be(LanguageType.Mixed);
        }

        [Fact]
        public void DetailedDetect_WithPureChinese_ShouldCountChineseCharacters()
        {
            var result = StringLanguageDetector.DetailedDetect("你好世界");
            result.ChineseCount.Should().Be(4);
            result.EnglishCount.Should().Be(0);
            result.OtherCount.Should().Be(0);
            result.LanguageType.Should().Be(LanguageType.Chinese);
        }
    }
}