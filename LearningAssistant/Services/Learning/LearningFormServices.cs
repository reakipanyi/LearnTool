using LearningAssistant.Services.Feedback;
using LearningAssistant.Services.Gamification;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Persistence;
using LearningAssistant.Services.TTS;
using LearningAssistant.Common.Events;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Config;

namespace LearningAssistant.Services.Learning
{
    public class AudioServices
    {
        public ITTSService? TTSService { get; }
        public TtsConfig TtsConfig { get; }

        public AudioServices(ITTSService? ttsService, TtsConfig ttsConfig)
        {
            TTSService = ttsService;
            TtsConfig = ttsConfig ?? new TtsConfig();
        }
    }

    public class GamificationServices
    {
        public IGamificationService GamificationService { get; }
        public IEncouragementService EncouragementService { get; }

        public GamificationServices(
            IGamificationService? gamificationService,
            IEncouragementService encouragementService)
        {
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

        public NotificationServices(
            IEventBus? eventBus,
            ISoundService? soundService)
        {
            EventBus = eventBus;
            SoundService = soundService;
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
        public ISpeechCoordinator? SpeechCoordinator { get; }
        public IDataPersistenceService? PersistenceService { get; }

        public LearningFormServices(
            AudioServices audioServices,
            GamificationServices gamificationServices,
            NotificationServices notificationServices,
            ISpacedRepetitionService? spacedRepetitionService,
            IUserSessionService? userSessionService,
            IPomodoroService? pomodoroService,
            IThemeService themeService,
            ISpeechCoordinator? speechCoordinator,
            IDataPersistenceService? persistenceService)
        {
            AudioServices = audioServices;
            GamificationServices = gamificationServices;
            NotificationServices = notificationServices;
            SpacedRepetitionService = spacedRepetitionService;
            UserSessionService = userSessionService;
            PomodoroService = pomodoroService;
            ThemeService = themeService;
            SpeechCoordinator = speechCoordinator;
            PersistenceService = persistenceService;
        }
    }
}