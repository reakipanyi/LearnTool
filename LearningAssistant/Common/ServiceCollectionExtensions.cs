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
            services.AddSingleton<IDataPersistenceService, DataPersistenceService>();
            services.AddSingleton<ICacheService>(sp =>
            {
                var cacheDir = GetCacheDirectorySafely();
                var cachePath = Path.Combine(cacheDir, "cache.json");
                return new CacheService(cachePath);
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
            services.AddSingleton<IProgressService, ProgressService>();
            services.AddSingleton<IExportService, ExportService>();
            services.AddSingleton<QuoteService>();
            services.AddSingleton<SubjectLearningService>();
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
            services.AddScoped<IPdfAiService, PdfAiService>();
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
            services.AddSingleton<IStudyEngine, StudyEngine>();
            services.AddSingleton<ILearningAnalyticsService, LearningAnalyticsService>();
            services.AddSingleton<ILearningReminderService, LearningReminderService>();
            services.AddSingleton<DataMigrationService>();
            services.AddSingleton<ISpacedRepetitionService, SpacedRepetitionService>();
            services.AddSingleton<IHighlightSyncService, HighlightSyncService>();
            services.AddSingleton<ILearningChartService, LearningChartService>();
            services.AddSingleton<ISoundService, SoundService>();
            services.AddSingleton<IAdvancedSpeechService, AdvancedSpeechService>();
            services.AddSingleton<IEnhancedReminderService, EnhancedReminderService>();
            
            services.AddScoped<ILearningSettingsManager, LearningSettingsManager>();
            services.AddScoped<ILearningExportService, LearningExportService>();

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
            services.AddSingleton<IWindowManager, WindowManager>();
            services.AddScoped<MainPresenter>();
            services.AddScoped<SettingPresenter>();
            services.AddScoped<LearningPresenter>();
            services.AddScoped<ResultPresenter>();
            services.AddScoped<ContentEditorPresenter>();
            services.AddScoped<PdfPresenter>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<PdfPresenter>>();
                var pdfRenderer = sp.GetRequiredService<IPdfRenderer>();
                var pdfFileManager = sp.GetRequiredService<IPdfFileManager>();
                var pdfOcrService = sp.GetRequiredService<IPdfOcrService>();
                var pdfTranslationService = sp.GetRequiredService<IPdfTranslationService>();
                var pdfAiService = sp.GetRequiredService<IPdfAiService>();
                var pdfTtsService = sp.GetRequiredService<IPdfTtsService>();
                var pdfStudyIntegration = sp.GetRequiredService<IPdfStudyIntegration>();
                var pdfExportService = sp.GetRequiredService<IPdfExportService>();
                var annotationService = sp.GetRequiredService<IAnnotationService>();
                var highlightService = sp.GetRequiredService<IHighlightService>();
                var pdfService = sp.GetRequiredService<IPdfService>();
                return new PdfPresenter(logger, pdfRenderer, pdfFileManager, pdfOcrService, pdfTranslationService,
                    pdfAiService, pdfTtsService, pdfStudyIntegration, pdfExportService,
                    annotationService, highlightService, pdfService);
            });

            services.AddScoped<MainForm>(sp =>
            {
                var presenter = sp.GetRequiredService<MainPresenter>();
                var windowManager = sp.GetRequiredService<IWindowManager>();
                var appConfig = sp.GetRequiredService<AppConfig>();
                var cloudStorageService = sp.GetRequiredService<ICloudStorageService>();
                return new MainForm(presenter, windowManager, appConfig, cloudStorageService);
            });
            services.AddScoped<SettingForm>();
            services.AddScoped<LearningForm>();
            services.AddScoped<PdfReaderForm>();
            services.AddScoped<ResultForm>();
            services.AddScoped<ContentEditorForm>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ContentEditorForm>>();
                var appConfig = sp.GetRequiredService<AppConfig>();
                return new ContentEditorForm(logger, appConfig);
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

            services.AddScoped<BrowserForm>(sp =>
            {
                var contentLoaderService = sp.GetRequiredService<IContentLoaderService>();
                var cloudStorageService = sp.GetService<ICloudStorageService>();
                var logger = sp.GetService<ILogger<BrowserForm>>();
                return new BrowserForm(contentLoaderService, cloudStorageService, logger);
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
            string cacheDir;
            try
            {
                cacheDir = FileHelper.GetCacheDirectory();
            }
            catch (Exception ex)
            {
                // 使用默认目录作为后备
                cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache");
                System.Diagnostics.Debug.WriteLine($"获取缓存目录失败: {ex.Message}, 使用默认目录: {cacheDir}");
            }

            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);

            return cacheDir;
        }
    }
}
