using LearningAssistant.Common;
using LearningAssistant.Data.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

using LearningAssistant.Abstractions;

namespace LearningAssistant.Services.Pdf
{
    public class PdfFileManager : IPdfFileManager
    {
        private readonly ILogger<PdfFileManager> _logger;
        private readonly IPdfService _pdfService;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        private string _currentPdfPath = string.Empty;
        private int _currentPageIndex = 0;
        private string _lastFolderPath = "";
        private bool _isImageMode = false;
        private readonly List<string> _imageFiles = new List<string>();
        private readonly Dictionary<string, int> _filePageMap = new Dictionary<string, int>();
        private readonly List<string> _recentFiles = new List<string>();
        // 目录 -> 上次打开的文件：重新打开同一目录时用于在文件树中定位
        private readonly Dictionary<string, string> _folderFileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private const int MaxRecentFiles = 10;


        public event EventHandler<FileLoadedEventArgs>? FileLoaded;
        public event EventHandler<FolderLoadedEventArgs>? FolderLoaded;

        private const string PdfSessionKey = "pdf_session";

        // 会话保存防抖：翻页会频繁触发 SetCurrentPageIndex，若每次同步写 SQLite 会阻塞 UI 线程
        private const int SessionSaveDebounceMs = 500;
        private readonly object _sessionSaveLock = new();
        private System.Threading.Timer? _sessionSaveTimer;
        private bool _sessionSavePending;

        public PdfFileManager(
            ILogger<PdfFileManager> logger,
            IPdfService pdfService,
            IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }

        public string CurrentFilePath => _currentPdfPath;

        public int CurrentPageIndex
        {
            get => _currentPageIndex;
            set
            {
                _currentPageIndex = value;
                if (!string.IsNullOrEmpty(_currentPdfPath))
                {
                    _filePageMap[_currentPdfPath] = value;
                }
            }
        }

        public void SetCurrentPageIndex(int pageIndex)
        {
            CurrentPageIndex = pageIndex;
            ScheduleSaveSession();
        }

        public bool IsImageMode => _isImageMode;

        public List<string> ImageFiles => _imageFiles;

        public List<string> RecentFiles => _recentFiles;

        private record SessionData(
            string? Folder,
            string? FilePath,
            Dictionary<string, int> FilePageMap,
            List<string>? RecentFiles,
            // 目录 -> 上次打开的文件；旧版本会话没有该字段时反序列化为 null
            Dictionary<string, string>? FolderFileMap = null
        );

        public void SaveSession()
        {
            lock (_sessionSaveLock)
            {
                // 直接保存视为一次强制落盘：取消防抖计时器并清除挂起标记，
                // 覆盖任何尚未执行的延迟写入（快照包含最新状态）
                _sessionSaveTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _sessionSavePending = false;
                SaveSessionCore();
            }
        }

        // 翻页触发的会话保存走防抖：连翻 N 页只落盘一次，且写库在后台线程执行，
        // 避免 EF Core 同步写 SQLite 阻塞 UI 线程造成翻页卡顿
        private void ScheduleSaveSession()
        {
            lock (_sessionSaveLock)
            {
                _sessionSavePending = true;
                _sessionSaveTimer ??= new System.Threading.Timer(_ => OnSessionSaveTimer(), null, Timeout.Infinite, Timeout.Infinite);
                _sessionSaveTimer.Change(SessionSaveDebounceMs, Timeout.Infinite);
            }
        }

        private void OnSessionSaveTimer()
        {
            lock (_sessionSaveLock)
            {
                if (!_sessionSavePending) return;
                _sessionSavePending = false;
                SaveSessionCore();
            }
        }

        /// <summary>
        /// 立即落盘挂起的会话保存（切换文件、窗体关闭等场景调用），无挂起时为无操作。
        /// </summary>
        public void FlushSessionSave()
        {
            lock (_sessionSaveLock)
            {
                _sessionSaveTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                if (!_sessionSavePending) return;
                _sessionSavePending = false;
                SaveSessionCore();
            }
        }

        // 调用方须已持有 _sessionSaveLock（lock 可重入）
        private void SaveSessionCore()
        {
            try
            {
                var data = new SessionData(_lastFolderPath, _currentPdfPath,
                    new Dictionary<string, int>(_filePageMap),
                    new List<string>(_recentFiles),
                    new Dictionary<string, string>(_folderFileMap));
                var json = JsonSerializer.Serialize(data);

                using var db = _dbContextFactory.CreateDbContext();
                var entity = db.AppSessions.FirstOrDefault(s => s.SessionKey == PdfSessionKey);
                if (entity == null)
                {
                    db.AppSessions.Add(new AppSessionEntity
                    {
                        SessionKey = PdfSessionKey,
                        SessionDataJson = json,
                        LastAccessTime = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }
                else
                {
                    entity.SessionDataJson = json;
                    entity.LastAccessTime = DateTime.Now;
                    entity.UpdatedAt = DateTime.Now;
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save session");
            }
        }

        public (string? Folder, string? FilePath, Dictionary<string, int>? FilePageMap) LoadSession()
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var entity = db.AppSessions.FirstOrDefault(s => s.SessionKey == PdfSessionKey);
                if (entity == null || string.IsNullOrWhiteSpace(entity.SessionDataJson)) return (null, null, null);

                var data = JsonSerializer.Deserialize<SessionData>(entity.SessionDataJson);
                if (data == null) return (null, null, null);

                if (data.RecentFiles != null)
                {
                    _recentFiles.Clear();
                    foreach (var f in data.RecentFiles)
                    {
                        if (File.Exists(f))
                            _recentFiles.Add(f);
                    }
                }

                // 恢复目录 -> 上次文件映射；目录已移动/文件已删除的记录直接丢弃
                _folderFileMap.Clear();
                if (data.FolderFileMap != null)
                {
                    foreach (var kvp in data.FolderFileMap)
                    {
                        if (string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value))
                            continue;
                        if (!File.Exists(kvp.Value))
                            continue;

                        try
                        {
                            _folderFileMap[NormalizeFolder(kvp.Key)] = kvp.Value;
                        }
                        catch
                        {
                            // 非法路径不参与定位，跳过即可
                        }
                    }
                }

                return (data.Folder, data.FilePath, data.FilePageMap);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load session");
                return (null, null, null);
            }
        }

        public void LoadLastSessionAndRestore()
        {
            try
            {
                var (folder, filePath, filePageMap) = LoadSession();
                if (filePageMap != null)
                {
                    foreach (var kvp in filePageMap)
                    {
                        _filePageMap[kvp.Key] = kvp.Value;
                    }
                }

                if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                {
                    _lastFolderPath = folder;
                    LoadFolder(folder);

                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        _currentPdfPath = filePath;

                        if (_filePageMap.TryGetValue(filePath, out int savedPage))
                        {
                            _currentPageIndex = savedPage;
                        }
                        else
                        {
                            _currentPageIndex = 0;
                        }

                        string extension = Path.GetExtension(filePath).ToLower();
                        if (extension == ".pdf")
                        {
                            _isImageMode = false;
                            _pdfService.Load(filePath);

                            FileLoaded?.Invoke(this, new FileLoadedEventArgs
                            {
                                FilePath = filePath,
                                IsImageMode = false,
                                PageCount = _pdfService.PageCount,
                                InitialPageIndex = _currentPageIndex
                            });
                        }
                        else
                        {
                            _isImageMode = true;
                            LoadImageFolder(filePath);

                            FileLoaded?.Invoke(this, new FileLoadedEventArgs
                            {
                                FilePath = filePath,
                                IsImageMode = true,
                                PageCount = _imageFiles.Count,
                                InitialPageIndex = _currentPageIndex
                            });
                        }

                        SaveSession();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore last session");
            }
        }

        public void LoadFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return;

            _lastFolderPath = folderPath;
            var pdfFiles = Directory.EnumerateFiles(folderPath, "*.pdf", SearchOption.AllDirectories)
                                   .ToList();

            var imageFiles = Directory.EnumerateFiles(folderPath, "*.jpg", SearchOption.AllDirectories)
                                     .Concat(Directory.EnumerateFiles(folderPath, "*.jpeg", SearchOption.AllDirectories))
                                     .Concat(Directory.EnumerateFiles(folderPath, "*.png", SearchOption.AllDirectories))
                                     .Concat(Directory.EnumerateFiles(folderPath, "*.bmp", SearchOption.AllDirectories))
                                     .Concat(Directory.EnumerateFiles(folderPath, "*.gif", SearchOption.AllDirectories))
                                     .ToList();

            var allFiles = pdfFiles.Concat(imageFiles).ToList();
            FolderLoaded?.Invoke(this, new FolderLoadedEventArgs
            {
                FolderPath = folderPath,
                Files = allFiles
            });
            SaveSession();
        }

        public async Task LoadFileAsync(string fileName)
        {
            try
            {
                string filePath;
                if (Path.IsPathRooted(fileName))
                {
                    filePath = fileName;
                    // 文件仍在已加载目录内时保留原目录，避免会话把根目录改写成子目录，
                    // 导致下次启动只恢复子目录、左侧目录树被截断
                    if (string.IsNullOrEmpty(_lastFolderPath) || !IsUnderFolder(filePath, _lastFolderPath))
                        _lastFolderPath = Path.GetDirectoryName(filePath) ?? "";
                }
                else
                {
                    filePath = Path.Combine(_lastFolderPath, fileName);
                }

                if (!File.Exists(filePath))
                    return;

                if (!string.IsNullOrEmpty(_currentPdfPath))
                {
                    _filePageMap[_currentPdfPath] = _currentPageIndex;
                }

                string extension = Path.GetExtension(filePath).ToLower();
                if (extension == ".pdf")
                {
                    _isImageMode = false;
                    _currentPdfPath = filePath;
                    _pdfService.Load(filePath);

                    if (_filePageMap.TryGetValue(filePath, out int savedPage))
                    {
                        _currentPageIndex = savedPage;
                    }
                    else
                    {
                        _currentPageIndex = 0;
                    }

                    FileLoaded?.Invoke(this, new FileLoadedEventArgs
                    {
                        FilePath = filePath,
                        IsImageMode = false,
                        PageCount = _pdfService.PageCount,
                        InitialPageIndex = _currentPageIndex
                    });

                    _logger.LogInformation("Loaded PDF: {Path}", filePath);
                }
                else
                {
                    _isImageMode = true;
                    _currentPdfPath = filePath;
                    LoadImageFolder(filePath);

                    FileLoaded?.Invoke(this, new FileLoadedEventArgs
                    {
                        FilePath = filePath,
                        IsImageMode = true,
                        PageCount = _imageFiles.Count,
                        InitialPageIndex = _currentPageIndex
                    });

                    _logger.LogInformation("Loaded image: {Path}", filePath);
                }

                SaveSession();

                AddToRecentFiles(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load file: {Path}", fileName);
            }
        }

        private void LoadImageFolder(string firstImagePath)
        {
            string folder = Path.GetDirectoryName(firstImagePath);
            if (string.IsNullOrEmpty(folder)) return;

            _imageFiles.Clear();
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

            // 递归加载子目录图片，便于按目录分组展示缩略图
            foreach (var ext in imageExtensions)
            {
                _imageFiles.AddRange(Directory.EnumerateFiles(folder, "*" + ext, SearchOption.AllDirectories));
            }

            // 按目录（分组）+ 文件名排序，保证同目录图片相邻
            _imageFiles.Sort(StringComparer.OrdinalIgnoreCase);

            int initialIndex = _imageFiles.IndexOf(firstImagePath);
            if (initialIndex >= 0)
            {
                _currentPageIndex = initialIndex;
            }
            else
            {
                _currentPageIndex = 0;
            }

            if (_imageFiles.Count > 0)
            {
                _currentPdfPath = _imageFiles[_currentPageIndex];
            }
            else
            {
                _currentPdfPath = string.Empty;
                _logger.LogWarning("No image files found in folder: {Folder}", folder);
            }
        }

        private void AddToRecentFiles(string filePath)
        {
            _recentFiles.Remove(filePath);
            _recentFiles.Insert(0, filePath);
            if (_recentFiles.Count > MaxRecentFiles)
            {
                _recentFiles.RemoveAt(_recentFiles.Count - 1);
            }
        }

        /// <summary>
        /// 获取指定目录上次打开的文件；没有记录或文件已不存在时返回 null。
        /// </summary>
        public string? GetLastFileForFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath)) return null;

            try
            {
                if (_folderFileMap.TryGetValue(NormalizeFolder(folderPath), out var filePath) && File.Exists(filePath))
                    return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get last file for folder: {Folder}", folderPath);
            }

            return null;
        }

        /// <summary>
        /// 记录指定目录最后打开的文件，用于重新打开该目录时在文件树中定位。
        /// </summary>
        public void SetLastFileForFolder(string folderPath, string filePath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || string.IsNullOrWhiteSpace(filePath)) return;

            try
            {
                string folderKey = NormalizeFolder(folderPath);
                string fullPath = Path.GetFullPath(filePath);
                // 记录未变化时不写库，避免每次点开同一文件都触发一次会话保存
                if (_folderFileMap.TryGetValue(folderKey, out var existing)
                    && string.Equals(existing, fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                _folderFileMap[folderKey] = fullPath;
                SaveSession();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to set last file for folder: {Folder}", folderPath);
            }
        }

        /// <summary>
        /// 目录键统一为绝对路径并去掉末尾分隔符；盘符根目录（如 E:\）保持原样。
        /// </summary>
        private static string NormalizeFolder(string folderPath)
        {
            string full = Path.GetFullPath(folderPath);
            string root = Path.GetPathRoot(full) ?? string.Empty;
            return full.Length > root.Length
                ? full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                : full;
        }

        /// <summary>
        /// 判断文件是否位于指定目录（含子目录）内。
        /// </summary>
        private static bool IsUnderFolder(string filePath, string folderPath)
        {
            string file = Path.GetFullPath(filePath);
            string folder = NormalizeFolder(folderPath);
            string prefix = folder.EndsWith(Path.DirectorySeparatorChar) || folder.EndsWith(Path.AltDirectorySeparatorChar)
                ? folder
                : folder + Path.DirectorySeparatorChar;

            return file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                || string.Equals(file, folder, StringComparison.OrdinalIgnoreCase);
        }
    }
}