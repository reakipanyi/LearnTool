using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Forms;
using UnifiedLearningAssistant.Models.Config;
using UnifiedLearningAssistant.Presenters;
using UnifiedLearningAssistant.Services;
using UnifiedLearningAssistant.Views;

namespace UnifiedLearningAssistant
{
    internal static class Program
    {
        private static IServiceProvider? _serviceProvider;

        private static IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            // 1. 配置加载 + 默认值防护
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
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
            });

            // 3. 核心服务（移除重复，统一生命周期）
            services.AddSingleton<Services.Persistence.IDataPersistenceService, Services.Persistence.DataPersistenceService>();
            services.AddSingleton<Services.Cache.ICacheService>(sp =>
            {
                var cacheDir = GetCacheDirectorySafely();
                var cachePath = Path.Combine(cacheDir, "cache.json");
                return new Services.Cache.CacheService(cachePath);
            });
            services.AddTransient<Services.TTS.ITTSService, Services.TTS.QwenTtsService>();
            services.AddTransient<Services.AI.IAiQuestionService, Services.AI.AiQuestionService>();
            services.AddSingleton<Services.Learning.IContentLoaderService, Services.Learning.ContentLoaderService>(); // 统一为 Singleton
            services.AddSingleton<Services.AI.IAIService, Services.AI.SiliconFlowAIService>();
            services.AddSingleton<Services.Pdf.IPdfService, Services.Pdf.PdfiumPdfService>();
            services.AddSingleton<Services.Pdf.IOcrService, Services.Pdf.TesseractOcrService>();
            services.AddSingleton<Services.Pdf.ITranslationService, Services.Pdf.BaiduTranslationService>();
            services.AddSingleton<Services.Pdf.IAnnotationService, Services.Pdf.FileAnnotationService>();
            services.AddSingleton<Services.Learning.IStudyEngine, Services.Learning.StudyEngine>();

            // 4. 窗体与 Presenter
            services.AddSingleton<IWindowManager, WindowManager>();
            services.AddScoped<MainPresenter>();
            services.AddScoped<SettingPresenter>();
            services.AddScoped<LearningPresenter>();
            services.AddScoped<ResultPresenter>();
            services.AddScoped<ContentEditorPresenter>();
            services.AddScoped<PdfPresenter>();

            services.AddScoped<MainForm>();
            services.AddScoped<SettingForm>();
            services.AddScoped<LearningForm>();
            services.AddScoped<PdfReaderForm>();
            services.AddScoped<ResultForm>();
            services.AddScoped<ContentEditorForm>();

            // 视图接口映射
            services.AddScoped<ISettingView>(sp => sp.GetRequiredService<SettingForm>());
            services.AddScoped<ILearningView>(sp => sp.GetRequiredService<LearningForm>());
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
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            IServiceProvider serviceProvider = null!;
            ILogger<MainForm> logger = null!;

            try
            {
                serviceProvider = BuildServiceProvider();
                _serviceProvider = serviceProvider;
                logger = serviceProvider.GetRequiredService<ILogger<MainForm>>();
            }
            catch (Exception ex)
            {
                // 若依赖注入容器构建失败，尝试使用控制台输出
                Console.WriteLine($"致命错误: 服务容器初始化失败 - {ex.Message}");
                MessageBox.Show($"程序初始化失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 注册全局异常处理（使用已获取的 logger，若为 null 则用控制台）
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

            MainForm mainForm;
            try
            {
                // 手动解决循环依赖问题
            var pdfReaderForm = serviceProvider.GetRequiredService<PdfReaderForm>();
            var pdfPresenter = serviceProvider.GetRequiredService<PdfPresenter>();
            pdfReaderForm.SetPresenter(pdfPresenter);

            // 现在构建MainForm，手动传递所需的依赖项
            var mainPresenter = serviceProvider.GetRequiredService<MainPresenter>();
            var windowManager = serviceProvider.GetRequiredService<IWindowManager>();
            
            mainForm = new MainForm(mainPresenter, pdfReaderForm, windowManager);
            // 新增功能：PDF生词本联动 - 将PDF Presenter传递给MainForm
            mainForm.SetPdfPresenter(pdfPresenter);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "无法创建主窗体");
                MessageBox.Show($"无法启动主窗体: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(mainForm);
        }
    }
}
