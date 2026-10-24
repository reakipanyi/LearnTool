using LearningAssistant.Models.Pdf;
namespace LearningAssistant.Services.Pdf;

public interface IPdfAudioProfileStore
{
    /// <summary>按 PDF 路径加载所在目录的档案；文件不存在时 Profile 为 null。</summary>
    PdfAudioProfileLoadResult Load(string pdfPath);
    void Save(PdfAudioProfile profile);
    /// <summary>目录档案文件路径（dir-*.json），用于展示与排查。</summary>
    string GetProfilePath(string pdfPath);
}
