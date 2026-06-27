using Moq;
using Xunit;
using FluentAssertions;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Persistence;
using LearningAssistant.Models.Learning;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Tests
{
    public class WrongAnswerServiceTests
    {
        private readonly Mock<IDataPersistenceService> _mockPersistence;
        private readonly Mock<ILogger<WrongAnswerService>> _mockLogger;
        private readonly WrongAnswerService _service;

        public WrongAnswerServiceTests()
        {
            _mockPersistence = new Mock<IDataPersistenceService>();
            _mockLogger = new Mock<ILogger<WrongAnswerService>>();
            _service = new WrongAnswerService(_mockPersistence.Object, _mockLogger.Object);
        }

        [Fact]
        public void AddWrongAnswer_WithValidItem_ShouldAddSuccessfully()
        {
            var item = new WrongAnswerItem
            {
                Question = "Test question",
                CorrectAnswer = "Correct answer",
                Subject = "Math",
                Category = "Algebra"
            };

            _service.AddWrongAnswer("test_user", item);

            item.Id.Should().NotBeNullOrEmpty();
            item.UserId.Should().Be("test_user");
            item.IsActive.Should().BeTrue();
        }

        [Fact]
        public void RemoveWrongAnswer_WithExistingItem_ShouldRemoveSuccessfully()
        {
            var item = new WrongAnswerItem
            {
                Id = "test_id",
                Question = "Test question",
                Subject = "Math"
            };
            _service.AddWrongAnswer("test_user", item);

            _service.RemoveWrongAnswer("test_user", "test_id");

            var remaining = _service.GetWrongAnswers("test_user");
            remaining.Should().BeEmpty();
        }

        [Fact]
        public void GetWrongAnswers_WithSubjectFilter_ShouldReturnFilteredResults()
        {
            _service.AddWrongAnswer("test_user", new WrongAnswerItem
            {
                Question = "Math question",
                Subject = "Math",
                Category = "Algebra"
            });
            _service.AddWrongAnswer("test_user", new WrongAnswerItem
            {
                Question = "English question",
                Subject = "English",
                Category = "Vocabulary"
            });

            var mathItems = _service.GetWrongAnswers("test_user", "Math");

            mathItems.Should().HaveCount(1);
            mathItems[0].Subject.Should().Be("Math");
        }

        [Fact]
        public void GetBySubjectCategory_ShouldReturnFilteredResults()
        {
            _service.AddWrongAnswer("test_user", new WrongAnswerItem
            {
                Question = "Algebra question",
                Subject = "Math",
                Category = "Algebra"
            });

            var items = _service.GetBySubjectCategory("test_user", "Math", "Algebra");

            items.Should().NotBeEmpty();
            items[0].Category.Should().Be("Algebra");
        }

        [Fact]
        public void MarkAsMastered_WithValidItem_ShouldUpdateMastery()
        {
            var item = new WrongAnswerItem
            {
                Question = "Test question",
                Subject = "Math"
            };
            _service.AddWrongAnswer("test_user", item);

            _service.MarkAsMastered("test_user", item.Id);

            var result = _service.GetWrongAnswers("test_user", "Math");
            result.Should().BeEmpty();
        }
    }

    public class LearningItemBaseTests
    {
        [Fact]
        public void LearningItem_BaseProperties_ShouldHaveDefaultValues()
        {
            var item = new TestLearningItem("Test", "Test", "");

            item.Id.Should().BeEmpty();
            item.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            item.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void LearningItem_CanSetId_ShouldUpdateId()
        {
            var item = new TestLearningItem("Test", "Test", "");
            var testId = "custom-id-123";

            item.Id = testId;

            item.Id.Should().Be(testId);
        }
    }

    public class TestLearningItem : LearningItem
    {
        private readonly string _content;
        private readonly string _meaning;
        private readonly string _pronunciation;

        public TestLearningItem(string content, string meaning, string pronunciation)
        {
            _content = content;
            _meaning = meaning;
            _pronunciation = pronunciation;
        }

        public override string GetMainContent() => _content;

        public override string GetDisplayText() => $"{_content}: {_meaning}";

        public override string GetPronunciation() => _pronunciation;

        public override string GetDisplayStruct() => "单词 | 音标 | 释义";
    }
}