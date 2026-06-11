namespace LearningAssistant.Services.Pdf
{
    public interface IPdfFileManager
    {
        void LoadFolder(string folderPath);
        Task LoadFileAsync(string fileName);
        void SaveSession();
        (string? Folder, string? FilePath, Dictionary<string, int>? FilePageMap) LoadSession();
        void LoadLastSessionAndRestore();
        string CurrentFilePath { get; }
        int CurrentPageIndex { get; set; }
        bool IsImageMode { get; }
        List<string> ImageFiles { get; }
        event EventHandler<FileLoadedEventArgs>? FileLoaded;
        event EventHandler<FolderLoadedEventArgs>? FolderLoaded;
    }

    public class FileLoadedEventArgs : EventArgs
    {
        public string FilePath { get; set; } = string.Empty;
        public bool IsImageMode { get; set; }
        public int PageCount { get; set; }
        public int InitialPageIndex { get; set; }
    }

    public class FolderLoadedEventArgs : EventArgs
    {
        public string FolderPath { get; set; } = string.Empty;
        public List<string> Files { get; set; } = new List<string>();
    }
}