using System;
using System.IO;

namespace LearningAssistant.Common
{
    /// <summary>
    /// 缓存路径管理器 - 统一管理各类缓存目录
    /// 缓存文件存储在 AppPaths.CacheDir 下，便于清理
    /// </summary>
    public static class CachePaths
    {
        /// <summary>
        /// WebView2 浏览器缓存
        /// </summary>
        public static string WebView2 => Path.Combine(AppPaths.CacheDir, "webview2");

        /// <summary>
        /// TTS 语音合成缓存
        /// </summary>
        public static string Tts => Path.Combine(AppPaths.CacheDir, "tts");

        /// <summary>
        /// 声音效果缓存
        /// </summary>
        public static string Sound => Path.Combine(AppPaths.CacheDir, "sound");

        /// <summary>
        /// 通用缓存
        /// </summary>
        public static string General => Path.Combine(AppPaths.CacheDir, "general");

        /// <summary>
        /// 翻译缓存
        /// </summary>
        public static string Translation => Path.Combine(AppPaths.CacheDir, "translation");

        /// <summary>
        /// 图片缓存
        /// </summary>
        public static string Images => Path.Combine(AppPaths.CacheDir, "images");

        /// <summary>
        /// AI 响应缓存
        /// </summary>
        public static string AiResponse => Path.Combine(AppPaths.CacheDir, "ai_response");

        /// <summary>
        /// PDF 渲染缓存
        /// </summary>
        public static string PdfRender => Path.Combine(AppPaths.CacheDir, "pdf_render");

        /// <summary>
        /// OCR 临时文件
        /// </summary>
        public static string OcrTemp => Path.Combine(AppPaths.CacheDir, "ocr_temp");

        /// <summary>
        /// 临时文件目录
        /// </summary>
        public static string Temp => Path.Combine(AppPaths.CacheDir, "temp");

        /// <summary>
        /// 确保所有缓存目录存在
        /// </summary>
        public static void EnsureAllDirectoriesExist()
        {
            AppPaths.EnsureDirectoryExists(WebView2);
            AppPaths.EnsureDirectoryExists(Tts);
            AppPaths.EnsureDirectoryExists(Sound);
            AppPaths.EnsureDirectoryExists(General);
            AppPaths.EnsureDirectoryExists(Translation);
            AppPaths.EnsureDirectoryExists(Images);
            AppPaths.EnsureDirectoryExists(AiResponse);
            AppPaths.EnsureDirectoryExists(PdfRender);
            AppPaths.EnsureDirectoryExists(OcrTemp);
            AppPaths.EnsureDirectoryExists(Temp);
        }

        /// <summary>
        /// 获取缓存文件的完整路径
        /// </summary>
        /// <param name="cacheType">缓存类型</param>
        /// <param name="fileName">文件名</param>
        /// <returns>完整路径</returns>
        public static string GetCacheFilePath(CacheType cacheType, string fileName)
        {
            var baseDir = GetCacheDirectory(cacheType);
            var sanitizedName = SanitizeFileName(fileName);
            return Path.Combine(baseDir, sanitizedName);
        }

        /// <summary>
        /// 获取缓存类型的目录
        /// </summary>
        public static string GetCacheDirectory(CacheType cacheType)
        {
            return cacheType switch
            {
                CacheType.WebView2 => WebView2,
                CacheType.Tts => Tts,
                CacheType.Sound => Sound,
                CacheType.General => General,
                CacheType.Translation => Translation,
                CacheType.Images => Images,
                CacheType.AiResponse => AiResponse,
                CacheType.PdfRender => PdfRender,
                CacheType.OcrTemp => OcrTemp,
                CacheType.Temp => Temp,
                _ => General
            };
        }

        /// <summary>
        /// 获取缓存目录大小
        /// </summary>
        public static long GetCacheSize()
        {
            return GetDirectorySize(AppPaths.CacheDir);
        }

        /// <summary>
        /// 获取指定缓存类型的大小
        /// </summary>
        public static long GetCacheSize(CacheType cacheType)
        {
            return GetDirectorySize(GetCacheDirectory(cacheType));
        }

        /// <summary>
        /// 清理所有缓存
        /// </summary>
        public static void ClearAllCache()
        {
            ClearCache(CacheType.WebView2);
            ClearCache(CacheType.Tts);
            ClearCache(CacheType.Sound);
            ClearCache(CacheType.General);
            ClearCache(CacheType.Translation);
            ClearCache(CacheType.Images);
            ClearCache(CacheType.AiResponse);
            ClearCache(CacheType.PdfRender);
            ClearCache(CacheType.OcrTemp);
            ClearCache(CacheType.Temp);
        }

        /// <summary>
        /// 清理指定类型的缓存
        /// </summary>
        public static void ClearCache(CacheType cacheType)
        {
            var dir = GetCacheDirectory(cacheType);
            if (Directory.Exists(dir))
            {
                try
                {
                    foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                    // 忽略清理错误
                }
            }
        }

        /// <summary>
        /// 计算目录大小
        /// </summary>
        private static long GetDirectorySize(string path)
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

        /// <summary>
        /// 清理文件名中的非法字符
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return Guid.NewGuid().ToString();

            var invalid = Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }
    }

    /// <summary>
    /// 缓存类型枚举
    /// </summary>
    public enum CacheType
    {
        /// <summary>WebView2 浏览器缓存</summary>
        WebView2,
        
        /// <summary>TTS 语音合成缓存</summary>
        Tts,
        
        /// <summary>声音效果缓存</summary>
        Sound,
        
        /// <summary>通用缓存</summary>
        General,
        
        /// <summary>翻译缓存</summary>
        Translation,
        
        /// <summary>图片缓存</summary>
        Images,
        
        /// <summary>AI 响应缓存</summary>
        AiResponse,
        
        /// <summary>PDF 渲染缓存</summary>
        PdfRender,
        
        /// <summary>OCR 临时文件</summary>
        OcrTemp,
        
        /// <summary>临时文件</summary>
        Temp
    }
}
