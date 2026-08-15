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
        private readonly Mock<IDataPersistenceService> _mockPersistence;
        private readonly Mock<ILogger<WrongAnswerService>> _mockLogger;
        private readonly Mock<IDbContextFactory<AppDbContext>> _mockDbContextFactory;
        private readonly WrongAnswerService _service;

        public WrongAnswerServiceTests()
        {
            _mockPersistence = new Mock<IDataPersistenceService>();
            _mockLogger = new Mock<ILogger<WrongAnswerService>>();
            _mockDbContextFactory = new Mock<IDbContextFactory<AppDbContext>>();
            // 配置工厂返回已初始化 schema 的上下文，避免 CreateDbContext() 返回 null 导致 NRE。
            // 服务使用持久化 SQLite 文件，数据会跨测试运行累积，因此在每次构造时清空历史数据，
            // 保证每个测试从干净状态开始、断言结果可复现。
            _mockDbContextFactory.Setup(f => f.CreateDbContext())
                .Returns(CreateTestDbContext);

            _service = new WrongAnswerService(_mockDbContextFactory.Object, _mockPersistence.Object, _mockLogger.Object);
            ClearTestData();
        }

        private static AppDbContext CreateTestDbContext()
        {
            var context = new AppDbContext();
            context.EnsureDatabaseCreated();
            return context;
        }

        /// <summary>
        /// 清空测试用户的历史错题数据，避免多次运行间数据残留影响断言。
        /// </summary>
        private static void ClearTestData()
        {
            using var context = CreateTestDbContext();
            var existing = context.WrongAnswers
                .Where(e => e.UserId == "test_user")
                .ToList();
            if (existing.Count > 0)
            {
                context.WrongAnswers.RemoveRange(existing);
                context.SaveChanges();
            }
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
                Question = "Test question",
                Subject = SubjectType.Math
            };
            _service.AddWrongAnswer("test_user", item);
            // AddWrongAnswer 会生成新 GUID，使用生成后的 ID 进行删除
            var addedId = item.Id;

            _service.RemoveWrongAnswer("test_user", addedId);

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

            // 已掌握后，应从未掌握数量中排除，且掌握计数增加
            _service.GetWrongAnswerCount("test_user").Should().Be(0);
            _service.GetMasteredCount("test_user").Should().Be(1);
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