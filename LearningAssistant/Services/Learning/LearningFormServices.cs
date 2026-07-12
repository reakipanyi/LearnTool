using LearningAssistant.Services.AI;
using LearningAssistant.Services.Feedback;
using LearningAssistant.Services.Gamification;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.TTS;
using LearningAssistant.Common.Events;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Config;

namespace LearningAssistant.Services.Learning
{
    public class AudioServices
    {
        public ITTSService? TTSService { get; }
        public SpeechService? SpeechService { get; }
        public TtsConfig TtsConfig { get; }

        public AudioServices(ITTSService? ttsService, TtsConfig ttsConfig)
        {
            TTSService = ttsService;
            TtsConfig = ttsConfig ?? new TtsConfig();
        }
    }

    public class GamificationServices
    {
        public IAchievementService? AchievementService { get; }
        public IGamificationService GamificationService { get; }
        public IEncouragementService EncouragementService { get; }

        public GamificationServices(
            IAchievementService? achievementService,
            IGamificationService? gamificationService,
            IEncouragementService encouragementService)
        {
            AchievementService = achievementService;
            GamificationService = gamificationService ?? new GamificationService(
                null,
                null);
            EncouragementService = encouragementService;
        }
    }

    public class NotificationServices
    {
        public IEventBus? EventBus { get; }
        public ISoundService? SoundService { get; }
        public IAIPanelPopupService? AIPanelPopupService { get; }

        public NotificationServices(
            IEventBus? eventBus,
            ISoundService? soundService,
            IAIPanelPopupService? aiPanelPopupService)
        {
            EventBus = eventBus;
            SoundService = soundService;
            AIPanelPopupService = aiPanelPopupService;
        }
    }

    public class LearningFormServices
    {
        public AudioServices AudioServices { get; }
        public GamificationServices GamificationServices { get; }
        public NotificationServices NotificationServices { get; }
        public ISpacedRepetitionService? SpacedRepetitionService { get; }
        public IUserSessionService? UserSessionService { get; }
        public IPomodoroService? PomodoroService { get; }
        public IThemeService ThemeService { get; }
        public IAiQuestionService? AiQuestionService { get; }
        public ISpeechCoordinator? SpeechCoordinator { get; }

        public LearningFormServices(
            AudioServices audioServices,
            GamificationServices gamificationServices,
            NotificationServices notificationServices,
            ISpacedRepetitionService? spacedRepetitionService,
            IUserSessionService? userSessionService,
            IPomodoroService? pomodoroService,
            IThemeService themeService,
            IAiQuestionService? aiQuestionService,
            ISpeechCoordinator? speechCoordinator)
        {
            AudioServices = audioServices;
            GamificationServices = gamificationServices;
            NotificationServices = notificationServices;
            SpacedRepetitionService = spacedRepetitionService;
            UserSessionService = userSessionService;
            PomodoroService = pomodoroService;
            ThemeService = themeService;
            AiQuestionService = aiQuestionService;
            SpeechCoordinator = speechCoordinator;
        }
    }
}