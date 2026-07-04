using Microsoft.Extensions.Logging;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Persistence;

namespace LearningAssistant.Services.Learning
{
    public class UserSessionService : IUserSessionService
    {
        private readonly ILogger<UserSessionService> _logger;
        private readonly IDataPersistenceService _persistenceService;
        private string _currentUserId = "Default";

        public string CurrentUserId => _currentUserId;

        public UserSessionService(ILogger<UserSessionService> logger, IDataPersistenceService persistenceService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
        }

        public string LoadSession()
        {
            var session = _persistenceService.LoadSession();
            if (!string.IsNullOrWhiteSpace(session.CurrentUserId))
            {
                _currentUserId = session.CurrentUserId;
            }
            return _currentUserId;
        }

        public void SaveSession(string userId)
        {
            _currentUserId = userId;
            var session = _persistenceService.LoadSession();
            session.CurrentUserId = userId;
            session.LastAccessTime = DateTime.Now;
            _persistenceService.SaveSession(session);
            _logger.LogInformation($"Session saved for user: {userId}");
        }

        public List<string> GetUserList()
        {
            var users = _persistenceService.GetUserIds();
            if (!users.Any())
            {
                _persistenceService.CreateUserProfile("Default", "访客");
                users = new List<string> { "Default" };
            }
            return users.ToList();
        }

        public UserProfile LoadUserProfile(string userId)
        {
            return _persistenceService.LoadUserProfile(userId);
        }

        public void SaveLearningConfig(LearningConfig config)
        {
            var session = _persistenceService.LoadSession();
            session.Language = config.Language;
            session.SubCategory = config.SubCategory;
            session.Mode = config.Mode;
            session.WordBankFile = config.WordBankFile;
            session.SortOrder = config.SortOrder;
            session.LastAccessTime = DateTime.Now;
            _persistenceService.SaveSession(session);
            _logger.LogInformation($"Learning config saved: Language={config.Language}, SubCategory={config.SubCategory}, Mode={config.Mode}");
        }

        public LearningConfig LoadLearningConfig()
        {
            var session = _persistenceService.LoadSession();
            return new LearningConfig
            {
                Language = session.Language,
                SubCategory = session.SubCategory,
                Mode = session.Mode,
                WordBankFile = session.WordBankFile,
                SortOrder = session.SortOrder
            };
        }
    }
}