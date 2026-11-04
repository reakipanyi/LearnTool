using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using LearningAssistant.Common;
using LearningAssistant.Models.Audio;
using LearningAssistant.Services.Shadowing;

namespace LearningAssistant.Services.Audio;

/// <summary>
/// 手动断句修正与手动页码锚点的持久化（用户数据）。
/// 按「书 + 曲目」存放，源音频 mtime 变化不会删除修正；指纹不一致只提示可能偏移。
/// </summary>
public sealed class AudioBoundaryOverlayStore
{
    public const int CurrentSchemaVersion = 1;

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.Ordinal);

    private readonly string _rootDir;

    public AudioBoundaryOverlayStore(string? rootDir = null)
    {
        _rootDir = Path.GetFullPath(rootDir ?? AppPaths.AudioBoundariesDir);
    }

    public string GetOverlayPath(string pdfPath, string trackPath)
    {
        string documentKey = ShadowingSegmentStore.ComputeDocumentKey(pdfPath);
        string trackKey = ComputeTrackKey(trackPath);
        return Path.Combine(_rootDir, documentKey, $"{trackKey}.manual.json");
    }

    public AudioBoundaryOverlay? Load(string pdfPath, string trackPath)
    {
        if (string.IsNullOrWhiteSpace(pdfPath) || string.IsNullOrWhiteSpace(trackPath))
            return null;

        var gate = GetLock(pdfPath, trackPath);
        gate.Wait();
        try
        {
            var overlay = JsonHelper.LoadFromFile<AudioBoundaryOverlay>(GetOverlayPath(pdfPath, trackPath));
            if (overlay == null || overlay.SchemaVersion != CurrentSchemaVersion)
                return null;

            overlay.Edits ??= new List<AudioSentenceEdit>();
            overlay.PageAnchors ??= new List<AudioPageAnchor>();
            return overlay;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            gate.Release();
        }
    }

    public void Save(string pdfPath, string trackPath, AudioBoundaryOverlay overlay)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        if (string.IsNullOrWhiteSpace(pdfPath) || string.IsNullOrWhiteSpace(trackPath)) return;

        var gate = GetLock(pdfPath, trackPath);
        gate.Wait();
        try
        {
            overlay.SchemaVersion = CurrentSchemaVersion;
            overlay.Edits ??= new List<AudioSentenceEdit>();
            overlay.PageAnchors ??= new List<AudioPageAnchor>();
            JsonHelper.SaveToFile(GetOverlayPath(pdfPath, trackPath), overlay);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>清除本曲目手动修正，不动自动断句缓存。</summary>
    public void Delete(string pdfPath, string trackPath)
    {
        if (string.IsNullOrWhiteSpace(pdfPath) || string.IsNullOrWhiteSpace(trackPath)) return;

        var gate = GetLock(pdfPath, trackPath);
        gate.Wait();
        try
        {
            string path = GetOverlayPath(pdfPath, trackPath);
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception)
        {
            // 删除失败不阻断 UI，下次保存会覆盖
        }
        finally
        {
            gate.Release();
        }
    }

    public static bool IsFingerprintStale(AudioBoundaryOverlay? overlay, string fingerprint)
    {
        if (overlay == null || string.IsNullOrWhiteSpace(overlay.Fingerprint)) return false;
        if (string.IsNullOrWhiteSpace(fingerprint)) return false;
        return !string.Equals(overlay.Fingerprint, fingerprint, StringComparison.OrdinalIgnoreCase);
    }

    private SemaphoreSlim GetLock(string pdfPath, string trackPath)
    {
        string key = $"{_rootDir}|{ShadowingSegmentStore.ComputeDocumentKey(pdfPath)}|{ComputeTrackKey(trackPath)}";
        return Locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
    }

    private static string ComputeTrackKey(string trackPath)
    {
        string key = Path.GetFullPath(trackPath);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..16];
    }
}
