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

        /// <summary>
        /// 读取该页各句子最近一次跟读录音的文件路径（键为句子 Id）。
        /// </summary>
        IReadOnlyDictionary<string, string> LoadSentenceRecordings(string pdfPath, int pageIndex);

        /// <summary>
        /// 保存该页各句子的跟读录音路径，便于重新打开窗口后回放。
        /// </summary>
        void SaveSentenceRecordings(string pdfPath, int pageIndex, IReadOnlyDictionary<string, string> recordings);

        /// <summary>
        /// 读取该页各句子最近一次跟读的参考分（键为句子 Id，值 0-100）。
        /// </summary>
        IReadOnlyDictionary<string, int> LoadSentenceScores(string pdfPath, int pageIndex);

        /// <summary>
        /// 保存该页各句子的跟读参考分，便于重新打开窗口后继续展示。
        /// </summary>
        void SaveSentenceScores(string pdfPath, int pageIndex, IReadOnlyDictionary<string, int> scores);

        bool Delete(string pdfPath, int pageIndex);
    }
}
