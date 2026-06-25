using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LearningAssistant.Common
{
    /// <summary>
    /// 文件操作帮助类 - 已重构使用 AppPaths
    /// </summary>
    [Obsolete("建议使用 AppPaths 和 CachePaths 代替此类")]
    public static class FileHelper
    {
        [Obsolete("建议使用 AppPaths.GetAppDirectory()")]
        public static string GetAppDirectory()
        {
            return AppPaths.AssemblyDir;
        }

        [Obsolete("建议使用 AppPaths.DataDir")]
        public static string GetDataDirectory()
        {
            return AppPaths.DataDir;
        }

        [Obsolete("建议使用 AppPaths.UsersDir")]
        public static string GetUsersDirectory()
        {
            return AppPaths.UsersDir;
        }

        [Obsolete("建议使用 AppPaths.CacheDir 或 CachePaths")]
        public static string GetCacheDirectory()
        {
            return AppPaths.CacheDir;
        }

        [Obsolete("建议使用 AppPaths.AnnotationsDir")]
        public static string GetAnnotationsDirectory()
        {
            return AppPaths.AnnotationsDir;
        }

        [Obsolete("建议使用 AppPaths.TranslationsDir")]
        public static string GetTranslationsDirectory()
        {
            return AppPaths.TranslationsDir;
        }

        [Obsolete("建议使用 AppPaths.FileBookmarksDir")]
        public static string GetBookmarksDirectory()
        {
            return AppPaths.FileBookmarksDir;
        }

        [Obsolete("建议使用 AppPaths.HighlightsDir")]
        public static string GetHighlightsDirectory()
        {
            return AppPaths.HighlightsDir;
        }

        public static void EnsureDirectoryExists(string path)
        {
            AppPaths.EnsureDirectoryExists(path);
        }

        [Obsolete("建议使用 AppPaths.GetUserProgressPath()")]
        public static string GetUserProgressPath(string userName)
        {
            return AppPaths.GetUserProgressPath(userName);
        }

        [Obsolete("建议使用 AppPaths.GetAnnotationPath()")]
        public static string GetAnnotationPath(string pdfPath, int pageIndex)
        {
            return AppPaths.GetAnnotationPath(pdfPath, pageIndex);
        }

        [Obsolete("建议使用 AppPaths.LastSessionPath")]
        public static string GetSessionPath()
        {
            return AppPaths.LastSessionPath;
        }

        public static bool FileExists(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        }

        [Obsolete("建议使用 AppPaths.GetCacheFilePath() 或 CachePaths")]
        public static string GetCacheFilePath(string key)
        {
            return AppPaths.GetCacheFilePath(key);
        }

        public static IEnumerable<string> GetFilesByExtension(string directory, string extension)
        {
            if (!Directory.Exists(directory))
                return Enumerable.Empty<string>();

            return Directory.EnumerateFiles(directory, $"*.{extension}", SearchOption.TopDirectoryOnly)
                           .OrderBy(f => f, StringComparer.CurrentCultureIgnoreCase);
        }

        public static string GetUniqueFileName(string directory, string baseName, string extension)
        {
            string fileName = $"{baseName}.{extension}";
            string fullPath = Path.Combine(directory, fileName);
            int counter = 1;

            while (File.Exists(fullPath))
            {
                fileName = $"{baseName}_{counter}.{extension}";
                fullPath = Path.Combine(directory, fileName);
                counter++;
            }

            return fullPath;
        }
    }
}
