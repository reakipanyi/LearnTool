using LearningAssistant.Common;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Migration
{
    /// <summary>
    /// 数据迁移服务 - 将旧目录结构迁移到新的统一目录结构
    /// </summary>
    public class PathMigrationService
    {
        private readonly ILogger<PathMigrationService>? _logger;

        // 旧目录路径（相对于程序集目录）
        private readonly string _oldDataDir;
        private readonly string _oldUsersDir;
        private readonly string _oldCacheDir;
        private readonly string _oldAnnotationsDir;
        private readonly string _oldTranslationsDir;
        private readonly string _oldBookmarksDir;
        private readonly string _oldHighlightsDir;
        private readonly string _oldSessionFile;
        private readonly string _oldDatabaseDir;

        public PathMigrationService(ILogger<PathMigrationService>? logger = null)
        {
            _logger = logger;
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            _oldDataDir = Path.Combine(baseDir, "Data");
            _oldUsersDir = Path.Combine(baseDir, "Users");
            _oldCacheDir = Path.Combine(baseDir, "Cache");
            _oldAnnotationsDir = Path.Combine(baseDir, "Annotations");
            _oldTranslationsDir = Path.Combine(baseDir, "Translations");
            _oldBookmarksDir = Path.Combine(baseDir, "Bookmarks");
            _oldHighlightsDir = Path.Combine(baseDir, "Highlights");
            _oldSessionFile = Path.Combine(baseDir, "lastsession.json");
            _oldDatabaseDir = Path.Combine(baseDir, "learning_assistant.db");
        }

        /// <summary>
        /// 检查是否需要迁移（如果新目录为空且旧目录存在数据）
        /// </summary>
        public bool NeedsMigration()
        {
            // 检查旧目录是否存在数据
            return Directory.Exists(_oldUsersDir) && Directory.GetFiles(_oldUsersDir).Length > 0 ||
                   Directory.Exists(_oldCacheDir) && Directory.GetFiles(_oldCacheDir, "*", SearchOption.AllDirectories).Length > 0 ||
                   File.Exists(_oldSessionFile);
        }

        /// <summary>
        /// 执行迁移
        /// </summary>
        public async Task<bool> MigrateAsync(IProgress<MigrationProgress>? progress = null)
        {
            try
            {
                _logger?.LogInformation("开始数据迁移...");

                // 确保新目录结构存在
                AppPaths.EnsureDirectoriesExist();
                CachePaths.EnsureAllDirectoriesExist();

                var totalSteps = 8;
                var currentStep = 0;

                // 1. 迁移用户数据
                currentStep++;
                progress?.Report(new MigrationProgress(currentStep, totalSteps, "迁移用户数据..."));
                await MigrateDirectoryAsync(_oldUsersDir, AppPaths.UsersDir);
                _logger?.LogInformation("用户数据迁移完成");

                // 2. 迁移缓存
                currentStep++;
                progress?.Report(new MigrationProgress(currentStep, totalSteps, "迁移缓存..."));
                await MigrateDirectoryAsync(_oldCacheDir, AppPaths.CacheDir);
                _logger?.LogInformation("缓存迁移完成");

                // 3. 迁移标注
                currentStep++;
                progress?.Report(new MigrationProgress(currentStep, totalSteps, "迁移PDF标注..."));
                await MigrateDirectoryAsync(_oldAnnotationsDir, AppPaths.AnnotationsDir);
                _logger?.LogInformation("PDF标注迁移完成");

                // 4. 迁移翻译
                currentStep++;
                progress?.Report(new MigrationProgress(currentStep, totalSteps, "迁移翻译缓存..."));
                await MigrateDirectoryAsync(_oldTranslationsDir, AppPaths.TranslationsDir);
                _logger?.LogInformation("翻译缓存迁移完成");

                // 5. 迁移书签
                currentStep++;
                progress?.Report(new MigrationProgress(currentStep, totalSteps, "迁移书签..."));
                await MigrateDirectoryAsync(_oldBookmarksDir, AppPaths.BookmarksDir);
                _logger?.LogInformation("书签迁移完成");

                // 6. 迁移高亮
                currentStep++;
                progress?.Report(new MigrationProgress(currentStep, totalSteps, "迁移高亮..."));
                await MigrateDirectoryAsync(_oldHighlightsDir, AppPaths.HighlightsDir);
                _logger?.LogInformation("高亮迁移完成");

                // 7. 迁移会话文件
                currentStep++;
                progress?.Report(new MigrationProgress(currentStep, totalSteps, "迁移会话文件..."));
                await MigrateFileAsync(_oldSessionFile, AppPaths.LastSessionPath);
                _logger?.LogInformation("会话文件迁移完成");

                // 8. 迁移数据库文件
                currentStep++;
                progress?.Report(new MigrationProgress(currentStep, totalSteps, "迁移数据库..."));
                await MigrateFileAsync(_oldDatabaseDir, AppPaths.DatabasePath);
                _logger?.LogInformation("数据库迁移完成");

                // 标记迁移完成
                MarkMigrationComplete();

                _logger?.LogInformation("数据迁移全部完成！");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "数据迁移失败");
                return false;
            }
        }

        /// <summary>
        /// 异步迁移目录
        /// </summary>
        private async Task MigrateDirectoryAsync(string sourceDir, string targetDir)
        {
            if (!Directory.Exists(sourceDir))
                return;

            await Task.Run(() =>
            {
                try
                {
                    // 确保目标目录存在
                    Directory.CreateDirectory(targetDir);

                    // 迁移所有文件
                    foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                    {
                        var relativePath = Path.GetRelativePath(sourceDir, file);
                        var targetPath = Path.Combine(targetDir, relativePath);
                        var targetFileDir = Path.GetDirectoryName(targetPath);

                        if (!string.IsNullOrEmpty(targetFileDir))
                        {
                            Directory.CreateDirectory(targetFileDir);
                        }

                        // 如果目标文件已存在，跳过
                        if (!File.Exists(targetPath))
                        {
                            File.Copy(file, targetPath, overwrite: false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "迁移目录失败: {Source}", sourceDir);
                }
            });
        }

        /// <summary>
        /// 异步迁移单个文件
        /// </summary>
        private async Task MigrateFileAsync(string sourceFile, string targetFile)
        {
            if (!File.Exists(sourceFile))
                return;

            await Task.Run(() =>
            {
                try
                {
                    var targetDir = Path.GetDirectoryName(targetFile);
                    if (!string.IsNullOrEmpty(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    // 如果目标文件已存在，跳过
                    if (!File.Exists(targetFile))
                    {
                        File.Copy(sourceFile, targetFile, overwrite: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "迁移文件失败: {Source}", sourceFile);
                }
            });
        }

        /// <summary>
        /// 标记迁移完成
        /// </summary>
        private void MarkMigrationComplete()
        {
            var markerFile = Path.Combine(AppPaths.ConfigDir, ".migration_complete");
            try
            {
                File.WriteAllText(markerFile, DateTime.Now.ToString("O"));
            }
            catch
            {
                // 忽略错误
            }
        }

        /// <summary>
        /// 获取旧缓存大小（用于提示用户是否清理）
        /// </summary>
        public long GetOldCacheSize()
        {
            if (!Directory.Exists(_oldCacheDir))
                return 0;

            return GetDirectorySize(_oldCacheDir);
        }

        /// <summary>
        /// 删除旧目录（可选，用户确认后执行）
        /// </summary>
        public async Task<bool> CleanOldDirectoriesAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    // 只清理数据目录，不清理程序集目录
                    var dirsToClean = new[] { _oldUsersDir, _oldCacheDir };

                    foreach (var dir in dirsToClean)
                    {
                        if (Directory.Exists(dir))
                        {
                            try
                            {
                                Directory.Delete(dir, recursive: true);
                            }
                            catch
                            {
                                // 忽略删除错误
                            }
                        }
                    }

                    // 删除旧会话文件
                    if (File.Exists(_oldSessionFile))
                    {
                        try
                        {
                            File.Delete(_oldSessionFile);
                        }
                        catch
                        {
                            // 忽略删除错误
                        }
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "清理旧目录失败");
                return false;
            }
        }

        private long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path))
                return 0;

            long size = 0;
            try
            {
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    var info = new FileInfo(file);
                    size += info.Length;
                }
            }
            catch
            {
                // 忽略访问错误
            }
            return size;
        }
    }

    /// <summary>
    /// 迁移进度报告
    /// </summary>
    public class MigrationProgress
    {
        public int CurrentStep { get; }
        public int TotalSteps { get; }
        public string Message { get; }
        public double PercentComplete => TotalSteps > 0 ? (double)CurrentStep / TotalSteps * 100 : 0;

        public MigrationProgress(int currentStep, int totalSteps, string message)
        {
            CurrentStep = currentStep;
            TotalSteps = totalSteps;
            Message = message;
        }
    }
}
