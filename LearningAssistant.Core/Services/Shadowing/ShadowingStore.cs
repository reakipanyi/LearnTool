using System.Security.Cryptography;
using System.Text;
using LearningAssistant.Common;
using LearningAssistant.Models.Shadowing;

namespace LearningAssistant.Services.Shadowing
{
    public class ShadowingStore : IShadowingStore
    {
        private readonly string _rootDir;
        private readonly string _userId;

        /// <summary>
        /// </summary>
        /// <param name="rootDir">存储根目录，默认 AppPaths.ShadowingDir；单元测试可传入临时目录。</param>
        /// <param name="userId">用户标识，默认取当前登录用户；单元测试可显式指定。</param>
        public ShadowingStore(string? rootDir = null, string? userId = null)
        {
            _rootDir = rootDir ?? AppPaths.ShadowingDir;
            _userId = string.IsNullOrWhiteSpace(userId) ? AppPaths.GetCurrentUserId() : userId;
        }

        public ShadowingManifest? Load(string pdfPath, int pageIndex)
        {
            return JsonHelper.LoadFromFile<ShadowingManifest>(GetManifestPath(pdfPath, pageIndex));
        }

        public void Save(ShadowingManifest manifest)
        {
            manifest.PdfPath = Path.GetFullPath(manifest.PdfPath);
            JsonHelper.SaveToFile(GetManifestPath(manifest.PdfPath, manifest.PageIndex), manifest);
        }

        public string GetManifestPath(string pdfPath, int pageIndex)
        {
            string key = Path.GetFullPath(pdfPath) + "#p" + pageIndex;
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..16];
            return Path.Combine(_rootDir, _userId, hash + ".json");
        }

        public IReadOnlyDictionary<string, string> LoadSentenceRecordings(string pdfPath, int pageIndex)
        {
            return JsonHelper.LoadFromFile<Dictionary<string, string>>(GetRecordingsPath(pdfPath, pageIndex))
                   ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }

        public void SaveSentenceRecordings(string pdfPath, int pageIndex, IReadOnlyDictionary<string, string> recordings)
        {
            ArgumentNullException.ThrowIfNull(recordings);
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in recordings)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                    map[pair.Key] = pair.Value;
            }

            JsonHelper.SaveToFile(GetRecordingsPath(pdfPath, pageIndex), map);
        }

        private string GetRecordingsPath(string pdfPath, int pageIndex)
            => GetManifestPath(pdfPath, pageIndex) + ".recordings.json";

        public IReadOnlyDictionary<string, int> LoadSentenceScores(string pdfPath, int pageIndex)
        {
            return JsonHelper.LoadFromFile<Dictionary<string, int>>(GetScoresPath(pdfPath, pageIndex))
                   ?? new Dictionary<string, int>(StringComparer.Ordinal);
        }

        public void SaveSentenceScores(string pdfPath, int pageIndex, IReadOnlyDictionary<string, int> scores)
        {
            ArgumentNullException.ThrowIfNull(scores);
            var map = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in scores)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value >= 0 && pair.Value <= 100)
                    map[pair.Key] = pair.Value;
            }

            JsonHelper.SaveToFile(GetScoresPath(pdfPath, pageIndex), map);
        }

        private string GetScoresPath(string pdfPath, int pageIndex)
            => GetManifestPath(pdfPath, pageIndex) + ".scores.json";

        public bool Delete(string pdfPath, int pageIndex)
        {
            string path = GetManifestPath(pdfPath, pageIndex);
            string recordingsPath = GetRecordingsPath(pdfPath, pageIndex);
            string scoresPath = GetScoresPath(pdfPath, pageIndex);
            if (!File.Exists(path) && !File.Exists(recordingsPath) && !File.Exists(scoresPath))
                return false;

            bool deleted = false;
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    deleted = true;
                }

                if (File.Exists(recordingsPath))
                {
                    File.Delete(recordingsPath);
                    deleted = true;
                }

                if (File.Exists(scoresPath))
                {
                    File.Delete(scoresPath);
                    deleted = true;
                }

                return deleted;
            }
            catch (IOException)
            {
                return false;
            }
        }
    }
}
