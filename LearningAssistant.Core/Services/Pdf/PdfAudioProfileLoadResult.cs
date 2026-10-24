using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>档案加载结果：解析失败时 Profile 为 null，但仍带出可读错误。</summary>
    public sealed class PdfAudioProfileLoadResult
    {
        public PdfAudioProfile? Profile { get; init; }
        public string? Error { get; init; }
        public bool Migrated { get; init; }

        public static PdfAudioProfileLoadResult Empty { get; } = new();
        public static PdfAudioProfileLoadResult Fail(string error) => new() { Error = error };
        public static PdfAudioProfileLoadResult Ok(PdfAudioProfile profile, bool migrated = false)
            => new() { Profile = profile, Migrated = migrated };
    }
}
