using LearningAssistant.Common.Events;
using LearningAssistant.Common.Themes;
using LearningAssistant.Data.Database;
using LearningAssistant.Forms;
using LearningAssistant.Managers;
using LearningAssistant.Models.Config;
using LearningAssistant.Presenters;
using LearningAssistant.Services;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Backup;
using LearningAssistant.Services.Backup.Providers;
using LearningAssistant.Services.Cache;
using LearningAssistant.Services.DragDrop;
using LearningAssistant.Services.Favorites;
using LearningAssistant.Services.Feedback;
using LearningAssistant.Services.Gamification;
using LearningAssistant.Services.Hotkeys;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Migration;
using LearningAssistant.Services.Pdf;
using LearningAssistant.Services.Persistence;
using LearningAssistant.Services.Recovery;
using LearningAssistant.Services.SystemTray;
using LearningAssistant.Services.TTS;
using LearningAssistant.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Common
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加配置相关服务
        /// </summary>
        public static IServiceCollection AddConfigurationServices(this IServiceCollection services, IConfiguration configuration)
        {
            var appConfig = configuration.Get<AppConfig>() ?? new AppConfig();
            appConfig.TtsConfig ??= new TtsConfig();
            appConfig.AiConfig ??= new AiConfig();
            appConfig.TranslationConfig ??= new TranslationConfig();
            appConfig.OcrConfig ??= new OcrConfig();
            appConfig.CloudStorageConfig ??= new CloudStorageConfig();

            services.AddSingleton(appConfig);
            services.AddSingleton(appConfig.TtsConfig);
            services.AddSingleton(appConfig.AiConfig);
            services.AddSingleton(appConfig.TranslationConfig);
            services.AddSingleton(appConfig.OcrConfig);
            services.AddSingleton(appConfig.CloudStorageConfig);

            // 添加共享的 HttpClient
            services.AddSingleton<HttpClient>(sp => new HttpClient { Timeout = TimeSpan.FromSeconds(appConfig.AiConfig.TimeoutSeconds) });

            return services;
        }

        /// <summary>
        /// 添加日志服务
        /// </summary>
        public static IServiceCollection AddLoggingServices(this IServiceCollection services)
        {
            var logDir = AppPaths.LogsDir;
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddFile(logDir, LogLevel.Information);
                builder.SetMinimumLevel(LogLevel.Information);
            });

            return services;
        }

        /// <summary>
        /// 添加核心业务服务
        /// </summary>
        public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IEventBus, EventBus>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IDataPersistenceService, SqliteDataPersistenceService>();
            services.AddSingleton<ICacheService>(sp =>
            {
                var cacheDir = GetCacheDirectorySafely();
                var cachePath = Path.Combine(cacheDir, "cache.json");
                var logger = sp.GetService<ILogger<CacheService>>();
                return new CacheService(cachePath, logger);
            });
            services.AddSingleton<IContentLoaderService, ContentLoaderService>();
            services.AddSingleton<IUserSessionService, UserSessionService>();
            services.AddSingleton<ISubjectTemplateService, SubjectTemplateService>();
            services.AddSingleton<IDataMigrationService, DataMigrationService>();

            services.AddSingleton<ITTSService>(sp =>
            {
                var ttsConfig = sp.GetRequiredService<TtsConfig>();
                var logger = sp.GetService<ILogger<QwenTtsService>>();
                return new QwenTtsService(ttsConfig.ApiKey, ttsConfig.BaseUrl, logger);
            });

            services.AddSingleton<ExportService>();
            services.AddSingleton<QuoteService>();
            services.AddSingleton<LearningReportService>();
            services.AddSingleton<IPdfContentLinkService, PdfContentLinkService>();

            return services;
        }

        /// <summary>
        /// 添加 AI 相关服务（已移除，按需启用）
        /// </summary>
        public static IServiceCollection AddAIServices(this IServiceCollection services)
        {
            return services;
        }

        /// <summary>
        /// 添加 PDF 相关服务
        /// </summary>
        public static IServiceCollection AddPdfServices(this IServiceCollection services)
        {
            services.AddSingleton<IPdfService, PdfiumPdfService>();
            services.AddSingleton<IOcrService, TesseractOcrService>();
            services.AddSingleton<ITranslationService, BaiduTranslationService>();
            services.AddSingleton<IAnnotationService, FileAnnotationService>();
            services.AddSingleton<IHighlightService, HighlightService>();
            services.AddSingleton<IBookmarkService, BookmarkService>();

            services.AddScoped<IPdfRenderer, PdfRenderer>();
            services.AddScoped<IPdfFileManager, PdfFileManager>();
            services.AddScoped<IPdfOcrService, PdfOcrService>();
            services.AddScoped<IPdfTranslationService, PdfTranslationService>();
            services.AddScoped<IPdfTtsService, PdfTtsService>();
            services.AddScoped<IPdfStudyIntegration, PdfStudyIntegration>();
            services.AddSingleton<IExportService>(sp => sp.GetRequiredService<ExportService>());

            return services;
        }

        /// <summary>
        /// 添加学习相关服务
        /// </summary>
        public static IServiceCollection AddLearningServices(this IServiceCollection services)
        {
            services.AddSingleton<Services.Learning.SortStrategies.SortStrategyFactory>();
            services.AddSingleton<IStudyListProcessor, StudyListProcessor>();
            services.AddSingleton<IProgressManager, ProgressManager>();

            services.AddSingleton<IStudyEngine>(sp =>
            {
                var contentLoaderService = sp.GetRequiredService<IContentLoaderService>();
                var progressManager = sp.GetRequiredService<IProgressManager>();
                var studyListProcessor = sp.GetRequiredService<IStudyListProcessor>();
                var analyticsService = sp.GetService<ILearningAnalyticsService>();
                var persistenceService = sp.GetRequiredService<IDataPersistenceService>();
                return new StudyEngine(contentLoaderService, progressManager, studyListProcessor, analyticsService, persistenceService);
            });

            services.AddSingleton<ILearningAnalyticsService>(sp =>
            {
                var logger = sp.GetService<ILogger<LearningAnalyticsService>>();
                var persistenceService = sp.GetService<IDataPersistenceService>();
                return new LearningAnalyticsService(logger, persistenceService);
            });
            services.AddSingleton<ILearningReminderService>(sp =>
            {
                var dbFactory = sp.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<Data.Database.AppDbContext>>();
                var analyticsService = sp.GetService<ILearningAnalyticsService>();
                var logger = sp.GetService<ILogger<SqliteLearningReminderService>>();
                return new SqliteLearningReminderService(dbFactory, analyticsService, logger);
            });
            services.AddSingleton<IPendingContentService, PendingContentService>();
            services.AddSingleton<ISpacedRepetitionService, SqliteSpacedRepetitionService>();
            services.AddSingleton<LearningDataExportService>();
            services.AddSingleton<IHighlightSyncService, HighlightSyncService>();
            services.AddSingleton<ILearningChartService, LearningChartService>();
            services.AddSingleton<Services.Web.IWebBookmarkService, Services.Web.WebBookmarkService>();
            services.AddSingleton<IEncouragementService, EncouragementService>();
            services.AddSingleton<IAchievementService, AchievementService>();
            services.AddSingleton<IGamificationService, GamificationService>();
            services.AddSingleton<ILearningGoalService>(sp =>
            {
                var persistenceService = sp.GetRequiredService<IDataPersistenceService>();
                var logger = sp.GetRequiredService<ILogger<LearningGoalService>>();
                var eventBus = sp.GetService<IEventBus>();
                return new LearningGoalService(persistenceService, logger, eventBus);
            });
            services.AddSingleton<FavoritesBackupProvider>();
            services.AddSingleton<StudyStatsBackupProvider>();
            services.AddSingleton<LearningGoalsBackupProvider>();
            services.AddSingleton<IBackupService>(sp =>
            {
                var logger = sp.GetService<ILogger<BackupService>>();
                var backupService = new BackupService(logger);

                var favoritesProvider = sp.GetRequiredService<FavoritesBackupProvider>();
                var statsProvider = sp.GetRequiredService<StudyStatsBackupProvider>();
                var goalsProvider = sp.GetRequiredService<LearningGoalsBackupProvider>();

                backupService.RegisterProvider(favoritesProvider);
                backupService.RegisterProvider(statsProvider);
                backupService.RegisterProvider(goalsProvider);

                return backupService;
            });
            services.AddSingleton<IWrongAnswerService, WrongAnswerService>();
            services.AddSingleton<IFavoritesService>(sp =>
            {
                var logger = sp.GetService<ILogger<FavoritesService>>();
                var eventBus = sp.GetService<IEventBus>();
                return new FavoritesService(logger, eventBus);
            });
            services.AddSingleton<ILearningPathService, LearningPathService>();
            services.AddSingleton<ILearningRecommendationService>(sp =>
            {
                var spacedRepetitionService = sp.GetRequiredService<ISpacedRepetitionService>();
                var wrongAnswerService = sp.GetRequiredService<IWrongAnswerService>();
                var analyticsService = sp.GetRequiredService<ILearningAnalyticsService>();
                var learningPathService = sp.GetRequiredService<ILearningPathService>();
                var logger = sp.GetService<ILogger<LearningRecommendationService>>();
                var pomodoroService = sp.GetService<IPomodoroService>();
                return new LearningRecommendationService(spacedRepetitionService, wrongAnswerService, analyticsService,
                    learningPathService, logger, pomodoroService);
            });
            services.AddSingleton<IPomodoroService>(sp =>
            {
                var persistenceService = sp.GetRequiredService<IDataPersistenceService>();
                var logger = sp.GetService<ILogger<PomodoroService>>();
                var eventBus = sp.GetService<IEventBus>();
                return new PomodoroService(persistenceService, logger, eventBus);
            });
            services.AddSingleton<IDataImportService, DataImportService>();

            services.AddScoped<ILearningSettingsManager, LearningSettingsManager>();
            services.AddScoped<ILearningEventMediator>(sp =>
            {
                var eventBus = sp.GetService<IEventBus>();
                return new LearningEventMediator(eventBus);
            });
            services.AddScoped<ILearningFlowHandler>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<LearningFlowHandler>>();
                var studyEngine = sp.GetRequiredService<IStudyEngine>();
                var contentLoaderService = sp.GetRequiredService<IContentLoaderService>();
                var exportService = sp.GetRequiredService<IExportService>();
                var windowManager = sp.GetRequiredService<IWindowManager>();
                var settingsManager = sp.GetRequiredService<ILearningSettingsManager>();
                var view = sp.GetRequiredService<ILearningView>();
                var eventBus = sp.GetService<IEventBus>();
                var spacedRepetitionService = sp.GetService<ISpacedRepetitionService>();
                return new LearningFlowHandler(logger, studyEngine, null, null, contentLoaderService,
                    exportService, windowManager, settingsManager, view, eventBus, spacedRepetitionService);
            });

            return services;
        }

        /// <summary>
        /// 添加学习增强服务（已移除：测验、语音回忆、知识图谱）
        /// </summary>
        public static IServiceCollection AddLearningEnhancementServices(this IServiceCollection services)
        {
            services.AddSingleton<FavoritesEventSubscriber>();

            return services;
        }

        /// <summary>
        /// 添加缓存相关服务
        /// </summary>
        public static IServiceCollection AddCacheServices(this IServiceCollection services)
        {
            services.AddSingleton<ICacheManagerService, CacheManagerService>();

            return services;
        }

        /// <summary>
        /// 添加数据库相关服务
        /// </summary>
        public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
        {
            services.AddDbContextFactory<AppDbContext>();

            return services;
        }

        /// <summary>
        /// 添加窗体和 Presenter 服务
        /// </summary>
        public static IServiceCollection AddFormServices(this IServiceCollection services)
        {
            services.AddSingleton<IAIPanelPopupService, AIPanelPopupService>();
            services.AddSingleton<IWindowManager, WindowManager>();
            services.AddSingleton<IHotkeyService, HotkeyService>();
            services.AddSingleton<IDragDropService, DragDropService>();
            services.AddSingleton<ICrashRecoveryService, CrashRecoveryService>();
            services.AddSingleton<ITrayIconService, TrayIconService>();
            services.AddScoped<MainPresenter>();
            services.AddScoped<SettingPresenter>();
            services.AddScoped<LearningPresenter>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<LearningPresenter>>();
                var view = sp.GetRequiredService<ILearningView>();
                var flowHandler = sp.GetRequiredService<ILearningFlowHandler>();
                var eventMediator = sp.GetRequiredService<ILearningEventMediator>();
                var settingsManager = sp.GetRequiredService<ILearningSettingsManager>();
                return new LearningPresenter(logger, view, flowHandler, eventMediator, settingsManager);
            });
            services.AddScoped<ResultPresenter>();
            services.AddScoped<ContentEditorPresenter>();
            services.AddScoped<PdfPresenter>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<PdfPresenter>>();
                var pdfRenderer = sp.GetRequiredService<IPdfRenderer>();
                var pdfFileManager = sp.GetRequiredService<IPdfFileManager>();
                var pdfOcrService = sp.GetRequiredService<IPdfOcrService>();
                var pdfTranslationService = sp.GetRequiredService<IPdfTranslationService>();
                var aiService = sp.GetService<IAIService>();
                var pdfTtsService = sp.GetService<IPdfTtsService>();
                var pdfStudyIntegration = sp.GetRequiredService<IPdfStudyIntegration>();
                var exportService = sp.GetRequiredService<IExportService>();
                var annotationService = sp.GetRequiredService<IAnnotationService>();
                var highlightService = sp.GetRequiredService<IHighlightService>();
                var pdfService = sp.GetRequiredService<IPdfService>();
                var eventBus = sp.GetService<IEventBus>();
                return new PdfPresenter(logger, pdfRenderer, pdfFileManager, pdfOcrService, pdfTranslationService,
                    aiService, pdfTtsService, pdfStudyIntegration, exportService,
                    annotationService, highlightService, pdfService, eventBus);
            });

            services.AddScoped<MainForm>(sp =>
            {
                var presenter = sp.GetRequiredService<MainPresenter>();
                var windowManager = sp.GetRequiredService<IWindowManager>();
                var appConfig = sp.GetRequiredService<AppConfig>();
                var themeService = sp.GetRequiredService<IThemeService>();
                var logger = sp.GetRequiredService<ILogger<MainForm>>();
                var webBookmarkService = sp.GetRequiredService<Services.Web.IWebBookmarkService>();
                var trayIconService = sp.GetRequiredService<ITrayIconService>();
                var hotkeyService = sp.GetRequiredService<IHotkeyService>();
                var pomodoroService = sp.GetRequiredService<IPomodoroService>();
                var spacedRepetitionService = sp.GetService<ISpacedRepetitionService>();
                var userSessionService = sp.GetService<IUserSessionService>();
                return new MainForm(presenter, windowManager, appConfig, themeService, logger, webBookmarkService, trayIconService, hotkeyService, pomodoroService, spacedRepetitionService, userSessionService);
            });
            services.AddScoped<SettingForm>();
            services.AddScoped<LearningForm>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<LearningForm>>();
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var themeService = sp.GetRequiredService<IThemeService>();
                var aiPanelPopupService = sp.GetService<IAIPanelPopupService>();
                var encouragementService = sp.GetRequiredService<IEncouragementService>();
                var achievementService = sp.GetService<IAchievementService>();
                var spacedRepetitionService = sp.GetService<ISpacedRepetitionService>();
                var gamificationService = sp.GetService<IGamificationService>();
                var eventBus = sp.GetService<IEventBus>();
                var userSessionService = sp.GetService<IUserSessionService>();
                var pomodoroService = sp.GetService<IPomodoroService>();
                return new LearningForm(null, null, logger, loggerFactory, null,
                    themeService, aiPanelPopupService, encouragementService,
                    achievementService, spacedRepetitionService, gamificationService,
                    eventBus, userSessionService, pomodoroService);
            });
            services.AddScoped<LearningHubForm>(sp =>
            {
                var spacedRepetitionService = sp.GetService<ISpacedRepetitionService>();
                var userSessionService = sp.GetService<IUserSessionService>();
                var learningAnalyticsService = sp.GetService<ILearningAnalyticsService>();
                var logger = sp.GetService<ILogger<LearningHubForm>>();
                return new LearningHubForm(spacedRepetitionService, userSessionService, learningAnalyticsService, logger);
            });
            //services.AddScoped<PdfReaderForm>();
            services.AddScoped<PdfReaderFormV2>();
            services.AddScoped<ResultForm>(sp =>
            {
                var logger = sp.GetService<ILogger<ResultForm>>();
                var themeService = sp.GetService<IThemeService>();
                return new ResultForm(logger, themeService);
            });
            services.AddScoped<ContentEditorForm>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ContentEditorForm>>();
                var appConfig = sp.GetRequiredService<AppConfig>();
                var aiPanelPopupService = sp.GetRequiredService<IAIPanelPopupService>();
                var themeService = sp.GetRequiredService<IThemeService>();
                return new ContentEditorForm(logger, appConfig, aiPanelPopupService, themeService);
            });
            services.AddScoped<LearningManagementForm>(sp =>
            {
                var analyticsService = sp.GetRequiredService<ILearningAnalyticsService>();
                var reminderService = sp.GetRequiredService<ILearningReminderService>();
                var reportService = sp.GetRequiredService<LearningReportService>();
                var quoteService = sp.GetRequiredService<QuoteService>();
                var goalService = sp.GetRequiredService<ILearningGoalService>();
                var wrongAnswerService = sp.GetRequiredService<IWrongAnswerService>();
                var spacedRepetitionService = sp.GetService<ISpacedRepetitionService>();
                var logger = sp.GetService<ILogger<LearningManagementForm>>();
                var themeService = sp.GetService<IThemeService>();
                var userSessionService = sp.GetService<IUserSessionService>();
                return new LearningManagementForm(analyticsService, reminderService, reportService, quoteService, goalService, wrongAnswerService, spacedRepetitionService, logger, themeService, userSessionService);
            });

            services.AddScoped<WrongAnswerForm>(sp =>
            {
                var wrongAnswerService = sp.GetRequiredService<IWrongAnswerService>();
                var logger = sp.GetService<ILogger<WrongAnswerForm>>();
                var themeService = sp.GetService<IThemeService>();
                var userSessionService = sp.GetService<IUserSessionService>();
                return new WrongAnswerForm(wrongAnswerService, logger, themeService, userSessionService);
            });

            services.AddScoped<ChallengeForm>(sp =>
            {
                var gamificationService = sp.GetRequiredService<IGamificationService>();
                var analyticsService = sp.GetService<ILearningAnalyticsService>();
                var wrongAnswerService = sp.GetService<IWrongAnswerService>();
                var logger = sp.GetService<ILogger<ChallengeForm>>();
                var themeService = sp.GetService<IThemeService>();
                var userSessionService = sp.GetService<IUserSessionService>();
                return new ChallengeForm(gamificationService, analyticsService, null, wrongAnswerService, logger, themeService, userSessionService);
            });

            services.AddScoped<ISettingView>(sp => sp.GetRequiredService<SettingForm>());
            services.AddScoped<ILearningView>(sp => sp.GetRequiredService<LearningForm>());
            services.AddScoped<IPdfView>(sp => sp.GetRequiredService<PdfReaderFormV2>());
            services.AddScoped<IMainView>(sp => sp.GetRequiredService<MainForm>());
            services.AddScoped<IResultView>(sp => sp.GetRequiredService<ResultForm>());
            services.AddScoped<IContentEditorView>(sp => sp.GetRequiredService<ContentEditorForm>());

            return services;
        }

        /// <summary>
        /// 安全获取缓存目录
        /// </summary>
        private static string GetCacheDirectorySafely()
        {
            string cacheDir = AppPaths.CacheDir;
            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);
            return cacheDir;
        }
    }
}
