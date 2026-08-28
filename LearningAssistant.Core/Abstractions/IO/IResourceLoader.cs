namespace LearningAssistant.Abstractions
{
    /// <summary>
    /// 只读资源加载抽象：WinForms 端从 AppDomain.BaseDirectory 读取；
    /// MAUI 端从 EmbeddedResource / Raw / Assets 读取。
    /// </summary>
    public interface IResourceLoader
    {
        Task<Stream> OpenReadAsync(string relativePath);
        Task<string> ReadAllTextAsync(string relativePath);
    }
}