using LearningAssistant.Abstractions;

namespace LearningAssistant.Platform
{
    /// <summary>
    /// WinForms 端 IResourceLoader 实现，从 AppDomain.BaseDirectory 读取文件。
    /// </summary>
    public class WinFormsResourceLoader : IResourceLoader
    {
        public Task<Stream> OpenReadAsync(string relativePath)
        {
            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            return Task.FromResult<Stream>(File.OpenRead(fullPath));
        }

        public async Task<string> ReadAllTextAsync(string relativePath)
        {
            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            return await File.ReadAllTextAsync(fullPath);
        }
    }
}