using LearningAssistant.Common;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Services.Pdf
{
    public class PdfFileManager : IPdfFileManager
    {
        private readonly ILogger<PdfFileManager> _logger;
        private readonly IPdfService _pdfService;

        private string _currentPdfPath = string.Empty;
        private int _currentPageIndex = 0;
        private string _lastFolderPath = "";
        private bool _isImageMode = false;
        private readonly List<string> _imageFiles = new List<string>();
        private readonly Dictionary<string, int> _filePageMap = new Dictionary<string, int>();


        public event EventHandler<FileLoadedEventArgs>? FileLoaded;
        public event EventHandler<FolderLoadedEventArgs>? FolderLoaded;

        public PdfFileManager(ILogger<PdfFileManager> logger, IPdfService pdfService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
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
                    SaveSession();
                }
            }
        }

        public bool IsImageMode => _isImageMode;

        public List<string> ImageFiles => _imageFiles;

        private record SessionData(
            string? Folder,
            string? FilePath,
            Dictionary<string, int> FilePageMap
        );

        public void SaveSession()
        {
            try
            {
                var data = new SessionData(_lastFolderPath, _currentPdfPath, new Dictionary<string, int>(_filePageMap));
                var json = JsonSerializer.Serialize(data);
                File.WriteAllText(AppPaths.LastSessionPath, json);
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
                if (!File.Exists(AppPaths.LastSessionPath)) return (null, null, null);
                var json = File.ReadAllText(AppPaths.LastSessionPath);
                var data = JsonSerializer.Deserialize<SessionData>(json);
                if (data == null) return (null, null, null);
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
                                   .Select(f => Path.GetFileName(f))
                                   .ToList();

            var imageFiles = Directory.EnumerateFiles(folderPath, "*.jpg", SearchOption.AllDirectories)
                                     .Concat(Directory.EnumerateFiles(folderPath, "*.jpeg", SearchOption.AllDirectories))
                                     .Concat(Directory.EnumerateFiles(folderPath, "*.png", SearchOption.AllDirectories))
                                     .Concat(Directory.EnumerateFiles(folderPath, "*.bmp", SearchOption.AllDirectories))
                                     .Concat(Directory.EnumerateFiles(folderPath, "*.gif", SearchOption.AllDirectories))
                                     .Select(f => Path.GetFileName(f))
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

            foreach (var ext in imageExtensions)
            {
                _imageFiles.AddRange(Directory.EnumerateFiles(folder, "*" + ext));
            }

            _imageFiles.Sort();

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
    }
}