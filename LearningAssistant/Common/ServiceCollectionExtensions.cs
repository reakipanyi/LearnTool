using LearningAssistant.Common.Events;
using LearningAssistant.Common.Themes;
using LearningAssistant.Data.Database;
using LearningAssistant.Forms;
using LearningAssistant.Models.Config;
using LearningAssistant.Presenters;
using LearningAssistant.Services;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Cache;
using LearningAssistant.Services.Cloud;
using LearningAssistant.Services.Feedback;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Migration;
using LearningAssistant.Services.Pdf;
using LearningAssistant.Services.Persistence;
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
            services.AddLogging(builder =>
            {
                builder.AddConsole();
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
            services.AddSingleton<IDataPersistenceService, DataPersistenceService>();
            services.AddSingleton<ICacheService>(sp =>
            {
                var cacheDir = GetCacheDirectorySafely();
                var cachePath = Path.Combine(cacheDir, "cache.json");
                var logger = sp.GetService<ILogger<CacheService>>();
                return new CacheService(cachePath, logger);
            });
            services.AddTransient<ITTSService>(sp =>
            {
                var ttsConfig = sp.GetRequiredService<TtsConfig>();
                string decryptedApiKey = Services.Utils.SecureConfigManager.Decrypt(ttsConfig.ApiKey);
                return new QwenTtsService(decryptedApiKey, ttsConfig.BaseUrl);
            });
            services.AddTransient<IAiQuestionService, AiQuestionService>();
            services.AddSingleton<IContentLoaderService, ContentLoaderService>();
            services.AddSingleton<IUserSessionService, UserSessionService>();
            // IProgressService 已移除，功能合并到 IStudyEngine
            services.AddSingleton<IExportService, ExportService>();
            services.AddSingleton<QuoteService>();

            services.AddSingleton<SpeechService>();
            services.AddSingleton<LearningReportService>();
            services.AddSingleton<IPdfContentLinkService, PdfContentLinkService>();
            services.AddSingleton<BaiduNetdiskService>(sp =>
            {
                var config = sp.GetRequiredService<CloudStorageConfig>();
                var logger = sp.GetService<ILogger<BaiduNetdiskService>>();
                var persistenceService = sp.GetService<IDataPersistenceService>();
                return new BaiduNetdiskService(config, logger, persistenceService);
            });
            services.AddSingleton<ICloudStorageService>(sp => sp.GetRequiredService<BaiduNetdiskService>());

            return services;
        }

        /// <summary>
        /// 添加 AI 相关服务
        /// </summary>
        public static IServiceCollection AddAIServices(this IServiceCollection services)
        {
            services.AddSingleton<IAIServiceFactory, AIServiceFactory>();
            services.AddSingleton<IAIService, FallbackAIService>();

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
            // IPdfAiService 已移除，PdfPresenter 直接使用 IAIService
            services.AddScoped<IPdfTtsService, PdfTtsService>();
            services.AddScoped<IPdfStudyIntegration, PdfStudyIntegration>();
            services.AddScoped<IPdfExportService, PdfExportService>();

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

            services.AddSingleton<ILearningAnalyticsService, LearningAnalyticsService>();
            services.AddSingleton<ILearningReminderService, LearningReminderService>();
            services.AddSingleton<IPendingContentService, PendingContentService>();
            services.AddSingleton<DataMigrationService>();
            services.AddSingleton<ISpacedRepetitionService, SpacedRepetitionService>();
            services.AddSingleton<IHighlightSyncService, HighlightSyncService>();
            services.AddSingleton<ILearningChartService, LearningChartService>();
            services.AddSingleton<Services.Web.IWebBookmarkService, Services.Web.WebBookmarkService>();
            services.AddSingleton<ISoundService>(sp => new SoundService(sp.GetService<ITTSService>()));
            services.AddSingleton<IAdvancedSpeechService, AdvancedSpeechService>();
            services.AddSingleton<IEnhancedReminderService, EnhancedReminderService>();
            services.AddSingleton<IEncouragementService, EncouragementService>();

            services.AddScoped<ILearningSettingsManager, LearningSettingsManager>();
            services.AddScoped<ILearningEventMediator, LearningEventMediator>();
            services.AddScoped<ILearningFlowHandler>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<LearningFlowHandler>>();
                var studyEngine = sp.GetRequiredService<IStudyEngine>();
                var aiService = sp.GetRequiredService<IAIService>();
                var ttsService = sp.GetRequiredService<ITTSService>();
                var contentLoaderService = sp.GetRequiredService<IContentLoaderService>();
                var exportService = sp.GetRequiredService<IExportService>();
                var windowManager = sp.GetRequiredService<IWindowManager>();
                var settingsManager = sp.GetRequiredService<ILearningSettingsManager>();
                var view = sp.GetRequiredService<ILearningView>();
                return new LearningFlowHandler(logger, studyEngine, aiService, ttsService, contentLoaderService,
                    exportService, windowManager, settingsManager, view);
            });

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
                var aiService = sp.GetRequiredService<IAIService>();
                var pdfTtsService = sp.GetRequiredService<IPdfTtsService>();
                var pdfStudyIntegration = sp.GetRequiredService<IPdfStudyIntegration>();
                var pdfExportService = sp.GetRequiredService<IPdfExportService>();
                var annotationService = sp.GetRequiredService<IAnnotationService>();
                var highlightService = sp.GetRequiredService<IHighlightService>();
                var pdfService = sp.GetRequiredService<IPdfService>();
                return new PdfPresenter(logger, pdfRenderer, pdfFileManager, pdfOcrService, pdfTranslationService,
                    aiService, pdfTtsService, pdfStudyIntegration, pdfExportService,
                    annotationService, highlightService, pdfService);
            });

            services.AddScoped<MainForm>(sp =>
            {
                var presenter = sp.GetRequiredService<MainPresenter>();
                var windowManager = sp.GetRequiredService<IWindowManager>();
                var appConfig = sp.GetRequiredService<AppConfig>();
                var cloudStorageService = sp.GetRequiredService<ICloudStorageService>();
                var themeService = sp.GetRequiredService<IThemeService>();
                var logger = sp.GetRequiredService<ILogger<MainForm>>();
                var webBookmarkService = sp.GetRequiredService<Services.Web.IWebBookmarkService>();
                return new MainForm(presenter, windowManager, appConfig, cloudStorageService, themeService, logger, webBookmarkService);
            });
            services.AddScoped<SettingForm>();
            services.AddScoped<LearningForm>();
            services.AddScoped<PdfReaderForm>();
            services.AddScoped<ResultForm>();
            services.AddScoped<ContentEditorForm>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ContentEditorForm>>();
                var appConfig = sp.GetRequiredService<AppConfig>();
                var aiPanelPopupService = sp.GetRequiredService<IAIPanelPopupService>();
                return new ContentEditorForm(logger, appConfig, aiPanelPopupService);
            });
            services.AddScoped<LearningManagementForm>(sp =>
            {
                var analyticsService = sp.GetRequiredService<ILearningAnalyticsService>();
                var reminderService = sp.GetRequiredService<ILearningReminderService>();
                var reportService = sp.GetRequiredService<LearningReportService>();
                var quoteService = sp.GetRequiredService<QuoteService>();
                var logger = sp.GetService<ILogger<LearningManagementForm>>();
                return new LearningManagementForm(analyticsService, reminderService, reportService, quoteService, logger);
            });

            services.AddScoped<ISettingView>(sp => sp.GetRequiredService<SettingForm>());
            services.AddScoped<ILearningView>(sp => sp.GetRequiredService<LearningForm>());
            services.AddScoped<IPdfView>(sp => sp.GetRequiredService<PdfReaderForm>());
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
