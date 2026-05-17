namespace UnifiedLearningAssistant.Common
{
    public static class FileHelper
    {
        public static string GetAppDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string GetDataDirectory()
        {
            var path = Path.Combine(GetAppDirectory(), Paths.DataDirectory);
            EnsureDirectoryExists(path);
            return path;
        }

        public static string GetUsersDirectory()
        {
            var path = Path.Combine(GetAppDirectory(), Paths.UsersDirectory);
            EnsureDirectoryExists(path);
            return path;
        }

        public static string GetCacheDirectory()
        {
            var path = Path.Combine(GetAppDirectory(), Paths.CacheDirectory);
            EnsureDirectoryExists(path);
            return path;
        }

        public static string GetAnnotationsDirectory()
        {
            var path = Path.Combine(GetAppDirectory(), Paths.AnnotationsDirectory);
            EnsureDirectoryExists(path);
            return path;
        }

        public static string GetTranslationsDirectory()
        {
            var path = Path.Combine(GetAppDirectory(), Paths.TranslationsDirectory);
            EnsureDirectoryExists(path);
            return path;
        }

        public static string GetBookmarksDirectory()
        {
            var path = Path.Combine(GetAppDirectory(), Paths.BookmarksDirectory);
            EnsureDirectoryExists(path);
            return path;
        }

        public static string GetHighlightsDirectory()
        {
            var path = Path.Combine(GetAppDirectory(), Paths.HighlightsDirectory);
            EnsureDirectoryExists(path);
            return path;
        }

        public static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static string GetUserProgressPath(string userName)
        {
            return Path.Combine(GetUsersDirectory(), $"{userName}.json");
        }

        public static string GetAnnotationPath(string pdfPath, int pageIndex)
        {
            var fileName = Path.GetFileNameWithoutExtension(pdfPath);
            return Path.Combine(GetAnnotationsDirectory(), $"{fileName}_page{pageIndex}.json");
        }

        public static string GetSessionPath()
        {
            return Path.Combine(GetAppDirectory(), Paths.SessionFile);
        }

        public static bool FileExists(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        }

        public static string GetCacheFilePath(string key)
        {
            return Path.Combine(GetCacheDirectory(), $"{key}.json");
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
