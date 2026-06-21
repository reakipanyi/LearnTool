using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO;

namespace LearningAssistant.Common
{
    public class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _logDirectory;
        private readonly LogLevel _minLevel;
        private static readonly ConcurrentDictionary<string, object> _fileLocks = new();
        private const int MaxFileSizeBytes = 10 * 1024 * 1024;
        private const int MaxRetainedFiles = 30;

        public FileLogger(string categoryName, string logDirectory, LogLevel minLevel)
        {
            _categoryName = categoryName;
            _logDirectory = logDirectory;
            _minLevel = minLevel;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _minLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            try
            {
                var message = formatter(state, exception);
                if (string.IsNullOrEmpty(message) && exception == null)
                    return;

                var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{GetLevelString(logLevel)}] [{_categoryName}] {message}";
                if (exception != null)
                {
                    logLine += Environment.NewLine + exception.ToString();
                }

                var logFilePath = GetLogFilePath();
                var fileLock = _fileLocks.GetOrAdd(logFilePath, _ => new object());

                lock (fileLock)
                {
                    EnsureDirectoryExists();
                    RotateFilesIfNeeded(logFilePath);
                    File.AppendAllText(logFilePath, logLine + Environment.NewLine);
                }
            }
            catch
            {
                // 日志失败时不抛出异常，避免影响应用程序
            }
        }

        private string GetLogFilePath()
        {
            var dateStr = DateTime.Now.ToString("yyyyMMdd");
            return Path.Combine(_logDirectory, $"log-{dateStr}.txt");
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        private static void RotateFilesIfNeeded(string currentLogPath)
        {
            if (!File.Exists(currentLogPath))
                return;

            var fileInfo = new FileInfo(currentLogPath);
            if (fileInfo.Length < MaxFileSizeBytes)
                return;

            var directory = Path.GetDirectoryName(currentLogPath);
            if (string.IsNullOrEmpty(directory))
                return;

            var baseFileName = Path.GetFileNameWithoutExtension(currentLogPath);
            var extension = Path.GetExtension(currentLogPath);
            var timestamp = DateTime.Now.ToString("HHmmss");

            var newFileName = $"{baseFileName}-{timestamp}{extension}";
            var newFilePath = Path.Combine(directory, newFileName);
            File.Move(currentLogPath, newFilePath);

            CleanOldFiles(directory);
        }

        private static void CleanOldFiles(string directory)
        {
            try
            {
                var files = Directory.GetFiles(directory, "log-*.txt")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                if (files.Count <= MaxRetainedFiles)
                    return;

                foreach (var file in files.Skip(MaxRetainedFiles))
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {
                        // 忽略删除失败
                    }
                }
            }
            catch
            {
                // 忽略清理失败
            }
        }

        private static string GetLevelString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "TRC",
                LogLevel.Debug => "DBG",
                LogLevel.Information => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                LogLevel.Critical => "CRT",
                LogLevel.None => "NON",
                _ => "???"
            };
        }
    }

    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _logDirectory;
        private readonly LogLevel _minLevel;

        public FileLoggerProvider(string logDirectory, LogLevel minLevel = LogLevel.Information)
        {
            _logDirectory = logDirectory;
            _minLevel = minLevel;
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName, _logDirectory, _minLevel);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    public static class FileLoggerExtensions
    {
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string logDirectory, LogLevel minLevel = LogLevel.Information)
        {
            builder.AddProvider(new FileLoggerProvider(logDirectory, minLevel));
            return builder;
        }
    }
}
