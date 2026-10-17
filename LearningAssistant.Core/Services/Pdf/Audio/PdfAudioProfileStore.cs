using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;
using System.Security.Cryptography;
using System.Text;

namespace LearningAssistant.Services.Pdf;

public class PdfAudioProfileStore : IPdfAudioProfileStore
{
    public PdfAudioProfile? Load(string pdfPath)
    {
        var path = GetProfilePath(pdfPath);
        return JsonHelper.LoadFromFile<PdfAudioProfile>(path);
    }

    public void Save(PdfAudioProfile profile)
    {
        profile.PdfPath = Path.GetFullPath(profile.PdfPath);
        JsonHelper.SaveToFile(GetProfilePath(profile.PdfPath), profile);
    }

    public string GetProfilePath(string pdfPath)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(pdfPath))))[..16];
        return Path.Combine(AppPaths.AudioProfilesDir, hash + ".json");
    }
}