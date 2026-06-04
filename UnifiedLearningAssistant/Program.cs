using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Data.Database;
using UnifiedLearningAssistant.Forms;
using UnifiedLearningAssistant.Models.Config;
using UnifiedLearningAssistant.Presenters;
using UnifiedLearningAssistant.Services;
using UnifiedLearningAssistant.Views;

namespace UnifiedLearningAssistant
{
    internal static class Program
    {
        // 新增功能：优化依赖注入 - 添加全局服务提供程序
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        private static IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            // 1. 配置加载 + 默认值防护
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true) // 新增：开发环境配置
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            // 加载配置，若缺失则使用空对象（避免后续注册 null）
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

            // 2. 日志
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
                // Note: Debug logger provider can be added via Microsoft.Extensions.Logging.Debug package if desired
            });

            // 3. 核心服务（统一生命周期管理）
            services.AddSingleton<Services.Persistence.IDataPersistenceService, Services.Persistence.DataPersistenceService>();
            services.AddSingleton<Services.Cache.ICacheService>(sp =>
            {
                var cacheDir = GetCacheDirectorySafely();
                var cachePath = Path.Combine(cacheDir, "cache.json");
                return new Services.Cache.CacheService(cachePath);
            });
            services.AddTransient<Services.TTS.ITTSService>(sp =>
            {
                var ttsConfig = sp.GetRequiredService<TtsConfig>();
                return new Services.TTS.QwenTtsService(ttsConfig.ApiKey, ttsConfig.BaseUrl);
            });
            services.AddTransient<Services.AI.IAiQuestionService, Services.AI.AiQuestionService>();
            services.AddSingleton<Services.Learning.IContentLoaderService, Services.Learning.ContentLoaderService>();
            services.AddSingleton<Services.Learning.IUserSessionService, Services.Learning.UserSessionService>();
            services.AddSingleton<Services.Learning.IProgressService, Services.Learning.ProgressService>();
            services.AddSingleton<Services.Learning.IExportService, Services.Learning.ExportService>();
            services.AddSingleton<Services.AI.IAIServiceFactory, Services.AI.AIServiceFactory>();
            services.AddSingleton<Services.AI.IAIService, Services.AI.AIServiceProvider>();
            services.AddSingleton<Services.Pdf.IPdfService, Services.Pdf.PdfiumPdfService>();
            services.AddSingleton<Services.Pdf.IOcrService, Services.Pdf.TesseractOcrService>();
            services.AddSingleton<Services.Pdf.ITranslationService, Services.Pdf.BaiduTranslationService>();
            services.AddSingleton<Services.Pdf.IAnnotationService, Services.Pdf.FileAnnotationService>();
            services.AddSingleton<Services.Pdf.IHighlightService, Services.Pdf.HighlightService>();
            services.AddSingleton<Services.Pdf.IBookmarkService, Services.Pdf.BookmarkService>();
            services.AddSingleton<Services.Learning.IStudyEngine, Services.Learning.StudyEngine>();
            services.AddSingleton<Services.Learning.ILearningAnalyticsService, Services.Learning.LearningAnalyticsService>();
            services.AddSingleton<Services.Learning.QuoteService>();
            services.AddSingleton<Services.Learning.SubjectLearningService>();
            services.AddSingleton<Services.Learning.SpeechService>();
            services.AddSingleton<Services.Learning.LearningReportService>();
            services.AddSingleton<Services.Learning.IPdfContentLinkService, Services.Learning.PdfContentLinkService>();
            services.AddSingleton<Services.Cloud.BaiduNetdiskService>();
            
            // 数据库相关服务
            services.AddDbContextFactory<AppDbContext>();
            // 可选：如果你想使用 SQLite 版本的提醒服务，可以取消下面这行注释，并注释掉上面的原始提醒服务
            // services.AddSingleton<Services.Learning.ILearningReminderService, Services.Learning.SqliteLearningReminderService>();
            services.AddSingleton<Services.Learning.ILearningReminderService, Services.Learning.LearningReminderService>();
            
            // 新增：数据迁移服务
            services.AddSingleton<Services.Migration.DataMigrationService>();
            
            // 新增：云存储服务（占位符实现）
            services.AddSingleton<Services.Cloud.ICloudStorageService, Services.Cloud.PlaceholderCloudStorageService>();

            // 4. 窗体与 Presenter
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
                return new LearningManagementForm(analyticsService, reminderService, reportService, quoteService);
            });
            services.AddScoped<BrowserForm>(sp =>
            {
                var contentLoaderService = sp.GetRequiredService<IContentLoaderService>();
                return new BrowserForm(contentLoaderService);
            });

            // 视图接口映射
            services.AddScoped<ISettingView>(sp => sp.GetRequiredService<SettingForm>());
            services.AddScoped<ILearningView>(sp => sp.GetRequiredService<LearningForm>());
            services.AddScoped<IPdfView>(sp => sp.GetRequiredService<PdfReaderForm>());
            services.AddScoped<IMainView>(sp => sp.GetRequiredService<MainForm>());
            services.AddScoped<IResultView>(sp => sp.GetRequiredService<ResultForm>());
            services.AddScoped<IContentEditorView>(sp => sp.GetRequiredService<ContentEditorForm>());

            return services.BuildServiceProvider();
        }

        // 安全获取缓存目录，确保目录存在
        private static string GetCacheDirectorySafely()
        {
            string cacheDir;
            try
            {
                cacheDir = Common.FileHelper.GetCacheDirectory();
            }
            catch
            {
                cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache");
            }

            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);

            return cacheDir;
        }

        [STAThread]
        static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 检查是否是预览模式
            if (args.Length > 0 && args[0].ToLower() == "--preview")
            {
                Application.Run(new Forms.FormPreviewTool());
                return;
            }

            ILogger<MainForm> logger = null!;

            try
            {
                ServiceProvider = BuildServiceProvider();
                logger = ServiceProvider.GetRequiredService<ILogger<MainForm>>();
                logger.LogInformation("服务容器初始化成功");
                
                // 启动提醒服务
                var reminderService = ServiceProvider.GetService<ILearningReminderService>();
                if (reminderService != null)
                {
                    reminderService.Start();
                    logger.LogInformation("提醒服务已启动");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"致命错误: 服务容器初始化失败 - {ex.Message}");
                MessageBox.Show($"程序初始化失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 注册全局异常处理
            Application.ThreadException += (s, e) =>
            {
                logger?.LogError(e.Exception, "UI 线程未处理异常");
                if (logger == null) Console.WriteLine($"UI Exception: {e.Exception}");
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                logger?.LogError(ex, "应用程序域未处理异常");
                if (logger == null && ex != null) Console.WriteLine($"Domain Exception: {ex}");
            };

            try
            {
                // 使用 Scope 来解析 Scoped 生命周期的 Presenter/Form
                // 保持 scope 存活直到 Application.Run 结束，以便 Scoped 服务在主窗体生命周期内有效
                using var appScope = ServiceProvider.CreateScope();
                var scopedProvider = appScope.ServiceProvider;

                var mainForm = scopedProvider.GetRequiredService<MainForm>();
                var pdfPresenter = scopedProvider.GetRequiredService<PdfPresenter>();
                var pdfReaderForm = scopedProvider.GetRequiredService<PdfReaderForm>();

                // 设置 Presenter 与 View 关系
                pdfReaderForm.SetPresenter(pdfPresenter);
                mainForm.SetPdfPresenter(pdfPresenter);

                logger.LogInformation("主窗体创建成功，启动应用程序");
                Application.ApplicationExit += Application_ApplicationExit;
                Application.Run(mainForm);
                // 在 Application.Run 返回后，appScope.Dispose() 会自动释放 Scoped 服务
            }
            catch (Exception ex)
            {
                string msg = $"{ex.Message}\n{ex.StackTrace}";
                Console.WriteLine(msg);
                logger?.LogError(ex, "无法创建主窗体");
                MessageBox.Show($"无法启动主窗体: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void Application_ApplicationExit(object? sender, EventArgs e)
        {
            try
            {
                // 停止提醒服务
                var reminderService = ServiceProvider?.GetService<ILearningReminderService>();
                if (reminderService is IDisposable disposableReminder)
                {
                    disposableReminder.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"释放提醒服务失败: {ex.Message}");
            }

            try
            {
                // 释放语音服务
                var speechService = ServiceProvider?.GetService<Services.Learning.SpeechService>();
                if (speechService is IDisposable disposableSpeech)
                {
                    disposableSpeech.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"释放语音服务失败: {ex.Message}");
            }

            try
            {
                // 释放百度网盘服务
                var baiduService = ServiceProvider?.GetService<Services.Cloud.BaiduNetdiskService>();
                if (baiduService is IDisposable disposableBaidu)
                {
                    disposableBaidu.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"释放百度网盘服务失败: {ex.Message}");
            }

            try
            {
                ThemeHelper.DisposeFonts();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"释放字体资源失败: {ex.Message}");
            }

            try
            {
                if (ServiceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                ServiceProvider = null!;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"释放服务容器失败: {ex.Message}");
            }
        }

        // 新增功能：全局服务访问辅助方法
        public static T GetRequiredService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        public static T? GetService<T>()
        {
            return ServiceProvider.GetService<T>();
        }
    }
}
