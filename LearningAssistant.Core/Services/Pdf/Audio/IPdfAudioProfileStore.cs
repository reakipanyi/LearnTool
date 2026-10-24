using LearningAssistant.Models.Pdf;
namespace LearningAssistant.Services.Pdf;

public interface IPdfAudioProfileStore
{
    /// <summary>
    /// 按 PDF 路径加载所在目录的档案；文件不存在时 Profile 为 null。
    /// pageCount 用于改名找回时以 fullFingerprint 复核候选。
    /// </summary>
    PdfAudioProfileLoadResult Load(string pdfPath, int pageCount = 0);

    /// <summary>找出与当前目录 PDF 文件名重叠率 &gt;80% 的旧档案，供用户确认重挂。</summary>
    IReadOnlyList<PdfAudioDirectoryReattachCandidate> FindReattachCandidates(string pdfPath);

    /// <summary>把候选档案重挂到当前目录（更新 scope 并搬到当前目录档案位置）。</summary>
    bool ReattachDirectory(string pdfPath, string candidateProfilePath);
    void Save(PdfAudioProfile profile);
    /// <summary>目录档案文件路径（dir-*.json），用于展示与排查。</summary>
    string GetProfilePath(string pdfPath);

    /// <summary>导出整个目录档案（不含缓存）。</summary>
    PdfAudioExportResult ExportDirectory(string pdfPath);

    /// <summary>导出当前书档案（目录规则快照 + 单书分区，不含缓存）。</summary>
    PdfAudioExportResult ExportBook(string pdfPath);

    /// <summary>解析并校验导入内容，生成差异摘要；不写盘。</summary>
    PdfAudioImportPreview PreviewImport(string pdfPath, string json);

    /// <summary>按预览结果合并写入，保留本机缓存与其它书的分区。</summary>
    void ApplyImport(string pdfPath, PdfAudioImportPreview preview);
}
