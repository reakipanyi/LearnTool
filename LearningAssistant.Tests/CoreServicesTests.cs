using Moq;
using Xunit;
using FluentAssertions;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Persistence;
using LearningAssistant.Models.Learning;
using LearningAssistant.Common;
using LearningAssistant.Data.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Tests
{
    public class WrongAnswerServiceTests
    {
        private readonly Mock<IDbContextFactory<AppDbContext>> _mockDbContextFactory;
        private readonly Mock<IDataPersistenceService> _mockPersistence;
        private readonly Mock<ILogger<WrongAnswerService>> _mockLogger;
        private readonly WrongAnswerService _service;

        public WrongAnswerServiceTests()
        {
            _mockDbContextFactory = new Mock<IDbContextFactory<AppDbContext>>();
            _mockPersistence = new Mock<IDataPersistenceService>();
            _mockLogger = new Mock<ILogger<WrongAnswerService>>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _mockDbContextFactory.Setup(f => f.CreateDbContext())
                .Returns(() => new AppDbContext(options));

            _service = new WrongAnswerService(_mockDbContextFactory.Object, _mockPersistence.Object, _mockLogger.Object);
        }

        [Fact]
        public void AddWrongAnswer_WithValidItem_ShouldAddSuccessfully()
        {
            var item = new WrongAnswerItem
            {
                Question = "Test question",
                CorrectAnswer = "Correct answer",
                Subject = SubjectType.Math,
                Category = SubCategoryType.MathFormula
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
                Subject = SubjectType.Math
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
                Subject = SubjectType.Math,
                Category = SubCategoryType.MathFormula
            });
            _service.AddWrongAnswer("test_user", new WrongAnswerItem
            {
                Question = "English question",
                Subject = SubjectType.English,
                Category = SubCategoryType.EnglishWord
            });

            var mathItems = _service.GetWrongAnswers("test_user", SubjectType.Math);

            mathItems.Should().HaveCount(1);
            mathItems[0].Subject.Should().Be(SubjectType.Math);
        }

        [Fact]
        public void GetBySubjectCategory_ShouldReturnFilteredResults()
        {
            _service.AddWrongAnswer("test_user", new WrongAnswerItem
            {
                Question = "Algebra question",
                Subject = SubjectType.Math,
                Category = SubCategoryType.MathFormula
            });

            var items = _service.GetBySubjectCategory("test_user", SubjectType.Math, SubCategoryType.MathFormula);

            items.Should().NotBeEmpty();
            items[0].Category.Should().Be(SubCategoryType.MathFormula);
        }

        [Fact]
        public void MarkAsMastered_WithValidItem_ShouldUpdateMastery()
        {
            var item = new WrongAnswerItem
            {
                Question = "Test question",
                Subject = SubjectType.Math
            };
            _service.AddWrongAnswer("test_user", item);

            _service.MarkAsMastered("test_user", item.Id);

            var result = _service.GetWrongAnswers("test_user", SubjectType.Math);
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
}