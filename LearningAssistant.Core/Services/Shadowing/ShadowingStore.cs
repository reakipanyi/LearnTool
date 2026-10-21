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

        public bool Delete(string pdfPath, int pageIndex)
        {
            string path = GetManifestPath(pdfPath, pageIndex);
            if (!File.Exists(path))
                return false;

            try
            {
                File.Delete(path);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }
    }
}
