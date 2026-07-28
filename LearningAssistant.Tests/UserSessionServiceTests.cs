using Moq;
using Xunit;
using FluentAssertions;
using LearningAssistant.Services.Learning;
using LearningAssistant.Models.User;
using LearningAssistant.Common;
using LearningAssistant.Services.Persistence;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Tests
{
    public class UserSessionServiceTests
    {
        private readonly Mock<ILogger<UserSessionService>> _mockLogger;
        private readonly Mock<IDataPersistenceService> _mockPersistence;
        private readonly string _testUserId;

        public UserSessionServiceTests()
        {
            _mockLogger = new Mock<ILogger<UserSessionService>>();
            _mockPersistence = new Mock<IDataPersistenceService>();
            _testUserId = $"test_user_{Guid.NewGuid():N}";
        }

        private UserSessionService CreateService()
        {
            return new UserSessionService(_mockLogger.Object, _mockPersistence.Object);
        }

        [Fact]
        public void CurrentUserId_ShouldReturnDefaultByDefault()
        {
            var service = CreateService();

            service.CurrentUserId.Should().Be("Default");
        }

        [Fact]
        public void SaveSession_ShouldSaveUserId()
        {
            _mockPersistence.Setup(p => p.LoadSession())
                .Returns(new SessionData());

            var service = CreateService();
            service.SaveSession(_testUserId);

            service.CurrentUserId.Should().Be(_testUserId);
            _mockPersistence.Verify(p => p.SaveSession(It.IsAny<SessionData>()), Times.Once);
        }

        [Fact]
        public void LoadSession_ShouldReturnDefaultWhenNoSession()
        {
            _mockPersistence.Setup(p => p.LoadSession())
                .Returns(new SessionData());

            var service = CreateService();
            var userId = service.LoadSession();

            userId.Should().Be("Default");
        }

        [Fact]
        public void LoadSession_ShouldReturnSavedUserId()
        {
            _mockPersistence.Setup(p => p.LoadSession())
                .Returns(new SessionData { CurrentUserId = _testUserId });

            var service = CreateService();
            var loadedUserId = service.LoadSession();

            loadedUserId.Should().Be(_testUserId);
        }

        [Fact]
        public void GetUserList_ShouldReturnListOfUsers()
        {
            _mockPersistence.Setup(p => p.GetUserIds())
                .Returns(new List<string> { "Default" });

            var service = CreateService();
            var users = service.GetUserList();

            users.Should().NotBeNull();
            users.Should().Contain("Default");
        }

        [Fact]
        public void GetUserList_WithEmptyUserIds_ShouldCreateDefaultUser()
        {
            _mockPersistence.Setup(p => p.GetUserIds())
                .Returns(new List<string>());

            var service = CreateService();
            var users = service.GetUserList();

            users.Should().NotBeNull();
            users.Should().Contain("Default");
            _mockPersistence.Verify(p => p.CreateUserProfile("Default", "访客"), Times.Once);
        }

        [Fact]
        public void LoadUserProfile_ShouldReturnProfile()
        {
            _mockPersistence.Setup(p => p.LoadUserProfile(_testUserId))
                .Returns(new UserProfile { UserId = _testUserId });

            var service = CreateService();
            var profile = service.LoadUserProfile(_testUserId);

            profile.Should().NotBeNull();
            profile.UserId.Should().Be(_testUserId);
        }

        [Fact]
        public void SaveLearningConfig_ShouldSaveConfigToSession()
        {
            _mockPersistence.Setup(p => p.LoadSession())
                .Returns(new SessionData());

            var config = new LearningConfig
            {
                Subject = SubjectType.English,
                SubCategory = SubCategoryType.EnglishWord,
                Mode = LearningModeType.Quick
            };

            var service = CreateService();
            service.SaveLearningConfig(config);

            _mockPersistence.Verify(p => p.SaveSession(It.Is<SessionData>(s =>
                s.LastSubject == SubjectType.English &&
                s.LastSubCategory == SubCategoryType.EnglishWord &&
                s.LastMode == LearningModeType.Quick)), Times.Once);
        }

        [Fact]
        public void LoadLearningConfig_ShouldReturnConfigFromSession()
        {
            _mockPersistence.Setup(p => p.LoadSession())
                .Returns(new SessionData
                {
                    LastSubject = SubjectType.Math,
                    LastSubCategory = SubCategoryType.MathFormula,
                    LastMode = LearningModeType.QuickReview
                });

            var service = CreateService();
            var config = service.LoadLearningConfig();

            config.Should().NotBeNull();
            config.Subject.Should().Be(SubjectType.Math);
            config.SubCategory.Should().Be(SubCategoryType.MathFormula);
            config.Mode.Should().Be(LearningModeType.QuickReview);
        }

        [Fact]
        public void SaveLearningConfig_WithNull_ShouldThrow()
        {
            var service = CreateService();

            Action action = () => service.SaveLearningConfig(null!);

            action.Should().Throw<NullReferenceException>();
        }
    }
}