using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// AI 摘要条目的持久化存储（按 PDF 文件分别落盘）。
    /// </summary>
    public interface IPdfAiSummaryStore
    {
        /// <summary>读取该文档的全部摘要条目（无数据时返回空集合）</summary>
        IReadOnlyList<PdfAiSummaryEntry> GetEntries(string pdfPath);

        /// <summary>追加条目（按页码+文字+时间戳去重）</summary>
        void AddEntries(string pdfPath, IEnumerable<PdfAiSummaryEntry> entries);

        /// <summary>清空该文档的摘要条目</summary>
        void Clear(string pdfPath);

        /// <summary>存储文件路径（供测试/诊断使用）</summary>
        string GetStorePath(string pdfPath);
    }
}
