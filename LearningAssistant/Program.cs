using LearningAssistant.Common;
using LearningAssistant.Forms;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LearningAssistant
{
    internal static class Program
    {
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
                using var appScope = ServiceProvider.CreateScope();
                var scopedProvider = appScope.ServiceProvider;

                var mainForm = scopedProvider.GetRequiredService<MainForm>();

                logger.LogInformation("主窗体创建成功，启动应用程序");
                Application.ApplicationExit += Application_ApplicationExit;
                Application.Run(mainForm);
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
            DisposeService<ILearningReminderService>("提醒服务");
            DisposeService<SpeechService>("语音服务");
            DisposeService<IAdvancedSpeechService>("高级语音服务");
            DisposeService<IEnhancedReminderService>("增强提醒服务");
            DisposeService<Services.Cloud.BaiduNetdiskService>("百度网盘服务");

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

        private static void DisposeService<T>(string serviceName) where T : class
        {
            try
            {
                var service = ServiceProvider?.GetService<T>();
                if (service is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"释放{serviceName}失败: {ex.Message}");
            }
        }

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
