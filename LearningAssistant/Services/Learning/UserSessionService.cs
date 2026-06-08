using Microsoft.Extensions.Logging;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Persistence;

namespace LearningAssistant.Services.Learning
{
    public class UserSessionService : IUserSessionService
    {
        private readonly ILogger<UserSessionService> _logger;
        private readonly IDataPersistenceService _persistenceService;
        private string _currentUserId = "Guest";

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
                _persistenceService.CreateUserProfile("Guest", "访客");
                users = new List<string> { "Guest" };
            }
            return users.ToList();
        }

        public UserProfile LoadUserProfile(string userId)
        {
            return _persistenceService.LoadUserProfile(userId);
        }

        public void SaveLearningConfig(string language, string subCategory, string mode, string wordBankFile, string sortOrder)
        {
            var session = _persistenceService.LoadSession();
            session.Language = language;
            session.SubCategory = subCategory;
            session.Mode = mode;
            session.WordBankFile = wordBankFile;
            session.SortOrder = sortOrder;
            session.LastAccessTime = DateTime.Now;
            _persistenceService.SaveSession(session);
            _logger.LogInformation($"Learning config saved: Language={language}, SubCategory={subCategory}, Mode={mode}");
        }

        public (string Language, string SubCategory, string Mode, string WordBankFile, string SortOrder) LoadLearningConfig()
        {
            var session = _persistenceService.LoadSession();
            return (session.Language, session.SubCategory, session.Mode, session.WordBankFile, session.SortOrder);
        }
    }
}