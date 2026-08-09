using Microsoft.Extensions.Logging;
using LearningAssistant.Common;
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
            // 启动恢复上次用户时，同步文件路径层的当前用户标识，
            // 确保 CurrentUserDir/书签/收藏/设置等路径解析到正确用户。
            AppPaths.SetCurrentUserId(_currentUserId);
            return _currentUserId;
        }

        public void SaveSession(string userId)
        {
            _currentUserId = userId;
            // 切换/保存用户时同步文件路径层，避免 AppPaths._currentUserId 与实际当前用户脱节
            // 造成用户专属文件数据读写到错误用户目录（跨用户数据污染）。
            AppPaths.SetCurrentUserId(userId);
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
            session.LastSubject = config.Subject;
            session.LastSubCategory = config.SubCategory;
            session.LastMode = config.Mode;
            session.WordBankFile = config.WordBankFile;
            session.LastSortOrder = config.SortOrder;
            session.LastAccessTime = DateTime.Now;
            _persistenceService.SaveSession(session);
            _logger.LogInformation($"Learning config saved: Subject={config.Subject}, SubCategory={config.SubCategory}, Mode={config.Mode}");
        }

        public LearningConfig LoadLearningConfig()
        {
            var session = _persistenceService.LoadSession();
            return new LearningConfig
            {
                Subject = session.LastSubject,
                SubCategory = session.LastSubCategory,
                Mode = session.LastMode,
                WordBankFile = session.WordBankFile,
                SortOrder = session.LastSortOrder
            };
        }
    }
}