using LearningAssistant.Models.Shadowing;

namespace LearningAssistant.Services.Shadowing
{
    /// <summary>
    /// 分句清单的持久化存储（按用户 + PDF 路径 + 页码隔离）。
    /// </summary>
    public interface IShadowingStore
    {
        ShadowingManifest? Load(string pdfPath, int pageIndex);

        void Save(ShadowingManifest manifest);

        string GetManifestPath(string pdfPath, int pageIndex);

        bool Delete(string pdfPath, int pageIndex);
    }
}
