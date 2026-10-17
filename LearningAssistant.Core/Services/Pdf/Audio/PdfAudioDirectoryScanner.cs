using LearningAssistant.Models.Pdf;
using System.Security.Cryptography;
using System.Text;

namespace LearningAssistant.Services.Pdf;

public class PdfAudioDirectoryScanner : IPdfAudioDirectoryScanner
{
    private static readonly string[] AudioExts =
        { ".mp3", ".wav", ".m4a", ".aac", ".flac", ".ogg", ".wma" };
    private static readonly string[] SubtitleExts = { ".srt", ".lrc" };

    public List<AudioTrack> Scan(string pdfDirectoryPath, bool includeSubDirectories)
    {
        var tracks = new List<AudioTrack>();
        if (!Directory.Exists(pdfDirectoryPath)) return tracks;

        var option = includeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        foreach (var ext in AudioExts)
        {
            foreach (var file in Directory.EnumerateFiles(pdfDirectoryPath, "*" + ext, option))
            {
                var relative = Path.GetRelativePath(pdfDirectoryPath, file);
                tracks.Add(new AudioTrack
                {
                    FilePath = file,
                    FileName = Path.GetFileName(file),
                    RelativePath = relative,
                    SubtitlePath = FindSubtitle(file)
                });
            }
        }
        return tracks.OrderBy(t => t.RelativePath, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string? FindSubtitle(string audioPath)
    {
        var baseName = Path.Combine(Path.GetDirectoryName(audioPath)!, Path.GetFileNameWithoutExtension(audioPath));
        foreach (var ext in SubtitleExts)
            if (File.Exists(baseName + ext)) return baseName + ext;
        return null;
    }

    public static string ComputeScanFingerprint(List<AudioTrack> tracks)
    {
        var sb = new StringBuilder();
        foreach (var t in tracks.OrderBy(t => t.RelativePath, StringComparer.OrdinalIgnoreCase))
        {
            long size = 0; DateTime m; try { var fi = new FileInfo(t.FilePath); size = fi.Length; m = fi.LastWriteTimeUtc; }
            catch { m = default; }
            sb.Append(t.RelativePath).Append('|').Append(size).Append('|').Append(m.Ticks).Append('\n');
        }
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes)[..16];
    }
}