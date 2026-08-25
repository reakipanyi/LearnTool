using LearningAssistant.Common;
using LearningAssistant.Forms;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Migration;
using LearningAssistant.Services.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

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
                .AddJsonFile(Path.Combine(AppPaths.ConfigDir, "appsettings.json"), optional: true, reloadOnChange: true)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            services
                .AddConfigurationServices(configuration)
                .AddLoggingServices()
                .AddCoreServices(configuration)
                .AddAIServices()
                .AddPdfServices()
                .AddLearningServices()
                .AddLearningEnhancementServices()
                .AddDatabaseServices()
                .AddPanAnalysisServices()
                .AddFormServices()
                .AddWindowsFormsPlatform();

            return services.BuildServiceProvider();
        }

        [STAThread]
        static void Main(string[] args)
        {
            ExcelPackage.License.SetNonCommercialPersonal("LearningAssistant");

            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0 && args[0].ToLower() == "--preview")
            {
                Application.Run(new FormPreviewTool());
                return;
            }

            ILogger<MainForm>? logger = null;

            try
            {
                ServiceProvider = BuildServiceProvider();
                logger = ServiceProvider.GetRequiredService<ILogger<MainForm>>();
                logger.LogInformation("服务容器初始化成功");

                // 初始化数据库（确保表已创建）
                var persistenceService = ServiceProvider.GetService<IDataPersistenceService>();
                persistenceService?.Initialize();
                logger.LogInformation("数据库初始化完成");

                // 后台执行数据迁移（从 JSON 到 SQLite，含 analytics→DailyRollup），不阻塞主窗体打开
                var migrationService = ServiceProvider.GetService<IDataMigrationService>();
                if (migrationService != null)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(500);
                            if (migrationService.NeedsMigration())
                            {
                                logger?.LogInformation("检测到需要迁移的数据，开始迁移...");
                                migrationService.BackupBeforeMigration();
                                var result = migrationService.PerformMigration();
                                if (result.Success)
                                {
                                    logger?.LogInformation("数据迁移成功: {Count} 个用户", result.SuccessfulMigrations);
                                }
                                else
                                {
                                    logger?.LogWarning("数据迁移部分失败: 成功 {Success}, 失败 {Failed}",
                                        result.SuccessfulMigrations, result.FailedMigrations);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError(ex, "后台数据迁移失败");
                        }
                    });
                }
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

                WireDailyReportReminder(mainForm, logger);

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

        /// <summary>
        /// 接线日报自动提醒（05 方案 3.4）：
        /// - 订阅“昨日学习小结”提醒触发，构建小结并经由主窗体线程弹出通知；
        /// - 为当前用户幂等创建默认日报提醒（默认 08:00）。
        /// </summary>
        private static void WireDailyReportReminder(MainForm mainForm, ILogger<MainForm>? logger)
        {
            try
            {
                var dailyReportService = ServiceProvider.GetService<ILearningDailyReportReminderService>();
                if (dailyReportService == null) return;

                dailyReportService.Start();
                dailyReportService.DailyReportReady += (s, e) =>
                {
                    try
                    {
                        if (mainForm == null || !mainForm.IsHandleCreated) return;
                        var title = e.Title;
                        var summary = e.Summary;
                        mainForm.BeginInvoke((Action)(() => ShowDailyReportPopup(title, summary)));
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "显示日报小结提醒失败");
                    }
                };

                var userSession = ServiceProvider.GetService<IUserSessionService>();
                var userId = userSession?.CurrentUserId;
                if (!string.IsNullOrEmpty(userId))
                {
                    dailyReportService.EnsureDefaultSummaryReminder(userId);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "日报自动提醒初始化失败");
            }
        }

        /// <summary>以右下角弹出通知形式展示“昨日学习小结”。</summary>
        private static void ShowDailyReportPopup(string title, string summary)
        {
            var popup = new ReminderNotificationForm
            {
                ReminderTitle = title,
                ReminderMessage = summary,
                ReminderTime = "学习统计",
                ReminderIcon = "📋",
                ReminderType = ReminderType.Study
            };
            popup.Show();
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
