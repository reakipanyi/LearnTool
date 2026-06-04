using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Common;
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

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            services
                .AddConfigurationServices(configuration)
                .AddLoggingServices()
                .AddCoreServices(configuration)
                .AddAIServices()
                .AddPdfServices()
                .AddLearningServices()
                .AddDatabaseServices()
                .AddFormServices();

            return services.BuildServiceProvider();
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
                // 释放高级语音服务
                var advancedSpeechService = ServiceProvider?.GetService<Services.Learning.IAdvancedSpeechService>();
                if (advancedSpeechService is IDisposable disposableAdvancedSpeech)
                {
                    disposableAdvancedSpeech.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"释放高级语音服务失败: {ex.Message}");
            }

            try
            {
                // 释放增强提醒服务
                var enhancedReminderService = ServiceProvider?.GetService<Services.Learning.IEnhancedReminderService>();
                if (enhancedReminderService is IDisposable disposableEnhancedReminder)
                {
                    disposableEnhancedReminder.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"释放增强提醒服务失败: {ex.Message}");
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
