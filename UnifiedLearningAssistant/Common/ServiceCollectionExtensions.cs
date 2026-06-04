using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using UnifiedLearningAssistant.Data.Database;
using UnifiedLearningAssistant.Forms;
using UnifiedLearningAssistant.Models.Config;
using UnifiedLearningAssistant.Presenters;
using UnifiedLearningAssistant.Services;
using UnifiedLearningAssistant.Views;
using UnifiedLearningAssistant.Services.Learning;
using UnifiedLearningAssistant.Services.Pdf;
using UnifiedLearningAssistant.Services.AI;
using UnifiedLearningAssistant.Services.Cloud;
using UnifiedLearningAssistant.Services.Cache;
using UnifiedLearningAssistant.Services.Persistence;
using UnifiedLearningAssistant.Services.TTS;
using UnifiedLearningAssistant.Services.Migration;

namespace UnifiedLearningAssistant.Common
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

            services.AddSingleton(appConfig);
            services.AddSingleton(appConfig.TtsConfig);
            services.AddSingleton(appConfig.AiConfig);
            services.AddSingleton(appConfig.TranslationConfig);
            services.AddSingleton(appConfig.OcrConfig);

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
                return new QwenTtsService(ttsConfig.ApiKey, ttsConfig.BaseUrl);
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
            services.AddSingleton<BaiduNetdiskService>();
            services.AddSingleton<ICloudStorageService, PlaceholderCloudStorageService>();

            return services;
        }

        /// <summary>
        /// 添加 AI 相关服务
        /// </summary>
        public static IServiceCollection AddAIServices(this IServiceCollection services)
        {
            services.AddSingleton<IAIServiceFactory, AIServiceFactory>();
            services.AddSingleton<IAIService, AIServiceProvider>();

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
            services.AddSingleton<IAdvancedSpeechService, AdvancedSpeechService>();
            services.AddSingleton<IEnhancedReminderService, EnhancedReminderService>();

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
            services.AddScoped<PdfPresenter>();

            services.AddScoped<MainForm>(sp =>
            {
                var presenter = sp.GetRequiredService<MainPresenter>();
                var pdfView = sp.GetRequiredService<IPdfView>();
                var windowManager = sp.GetRequiredService<IWindowManager>();
                var appConfig = sp.GetRequiredService<AppConfig>();
                return new MainForm(presenter, pdfView, windowManager, appConfig);
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
                return new BrowserForm(contentLoaderService);
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
            catch
            {
                cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache");
            }

            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);

            return cacheDir;
        }
    }
}
