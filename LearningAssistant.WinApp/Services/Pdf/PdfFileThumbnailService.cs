using LearningAssistant.Common;
using PdfiumViewer;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;

namespace LearningAssistant.Services.Pdf;

/// <summary>
/// PDF 文件首帧缩略图：取每个 PDF 的第一页，等比缩放后绘制到固定画布，
/// 并按「路径 + 文件大小 + 修改时间」的指纹缓存为 PNG，目录每次打开都能直接复用。
/// </summary>
public static class PdfFileThumbnailService
{
    /// <summary>缩略图画布宽度（与目录树 ImageList 尺寸保持一致）。</summary>
    public const int ThumbnailWidth = 44;
    /// <summary>缩略图画布高度（与目录树 ImageList 尺寸保持一致）。</summary>
    public const int ThumbnailHeight = 58;

    private static readonly object RenderLock = new();

    /// <summary>缩略图缓存目录：AppData/cache/pdf_render/file_thumbs。</summary>
    public static string CacheDirectory => Path.Combine(CachePaths.PdfRender, "file_thumbs");

    /// <summary>
    /// 计算缩略图缓存路径；文件不存在或无法读取元数据时返回 null。
    /// 文件名包含文件指纹，PDF 被替换或修改后会自动生成新的缓存，不会命中旧图。
    /// </summary>
    public static string? GetCachePath(string pdfPath)
    {
        var info = TryGetFileInfo(pdfPath);
        if (info == null) return null;

        string fingerprint = $"{Path.GetFullPath(pdfPath).ToUpperInvariant()}|{info.Length}|{info.LastWriteTimeUtc.Ticks}";
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)));
        return Path.Combine(CacheDirectory, hash + ".png");
    }

    /// <summary>读取已缓存的缩略图；缓存不存在或损坏时返回 null。</summary>
    public static Bitmap? LoadCachedThumbnail(string cachePath)
    {
        if (string.IsNullOrEmpty(cachePath) || !File.Exists(cachePath)) return null;

        try
        {
            using var stream = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var source = Image.FromStream(stream);
            return new Bitmap(source);
        }
        catch
        {
            // 缓存损坏时删除，下一次重新渲染
            try { File.Delete(cachePath); } catch { }
            return null;
        }
    }

    /// <summary>
    /// 确保缩略图可用：优先读缓存，缺失时渲染第一页并写回缓存。
    /// 渲染与缓存读写是阻塞操作，请在后台线程调用；返回的位图由调用方负责释放。
    /// </summary>
    public static Bitmap? EnsureThumbnail(string pdfPath)
    {
        string? cachePath = GetCachePath(pdfPath);
        if (cachePath == null) return null;

        var cached = LoadCachedThumbnail(cachePath);
        if (cached != null) return cached;

        lock (RenderLock)
        {
            // 并发请求同一文件时，等待锁的线程直接复用刚写好的缓存
            cached = LoadCachedThumbnail(cachePath);
            if (cached != null) return cached;

            return RenderAndCache(pdfPath, cachePath);
        }
    }

    private static Bitmap? RenderAndCache(string pdfPath, string cachePath)
    {
        Bitmap? thumbnail = null;
        string? tempPath = null;

        try
        {
            // 共享读取：阅读器可能仍持有该 PDF 的文件句柄
            using var stream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using var document = PdfDocument.Load(stream);
            if (document.PageCount <= 0) return null;

            var contentSize = CalculateContentSize(document);
            using var rendered = document.Render(0, contentSize.Width, contentSize.Height,
                96, 96, PdfRenderFlags.Annotations);
            if (rendered == null) return null;

            thumbnail = ComposeThumbnail(rendered, contentSize.Width, contentSize.Height);

            AppPaths.EnsureDirectoryExists(CacheDirectory);
            tempPath = cachePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            thumbnail.Save(tempPath, ImageFormat.Png);
            File.Move(tempPath, cachePath, overwrite: true);
            tempPath = null;
        }
        catch
        {
            // 加密/损坏的 PDF 或写缓存失败时只影响预览图，不影响目录列表
            thumbnail?.Dispose();
            return null;
        }
        finally
        {
            if (tempPath != null)
            {
                try { File.Delete(tempPath); } catch { }
            }
        }

        return thumbnail;
    }

    /// <summary>按第一页宽高比计算缩略图内容区尺寸，保证不超出画布。</summary>
    private static Size CalculateContentSize(PdfDocument document)
    {
        float pageWidth = 0;
        float pageHeight = 0;
        try
        {
            if (document.PageSizes.Count > 0)
            {
                var size = document.PageSizes[0];
                pageWidth = size.Width;
                pageHeight = size.Height;
            }
        }
        catch
        {
            // 拿不到页面尺寸时按 A4 比例兜底
        }

        if (pageWidth <= 0 || pageHeight <= 0)
        {
            pageWidth = 210;
            pageHeight = 297;
        }

        float aspect = pageHeight / pageWidth;
        int width = ThumbnailWidth;
        int height = Math.Max(1, (int)Math.Round(width * aspect));
        if (height > ThumbnailHeight)
        {
            height = ThumbnailHeight;
            width = Math.Max(1, (int)Math.Round(height / aspect));
        }

        return new Size(width, height);
    }

    /// <summary>把渲染结果画到透明画布上，纸面留白并描边，浅色/深色主题下边界都清晰。</summary>
    private static Bitmap ComposeThumbnail(Image rendered, int contentWidth, int contentHeight)
    {
        var canvas = new Bitmap(ThumbnailWidth, ThumbnailHeight, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(canvas);
        graphics.Clear(Color.Transparent);

        int x = (ThumbnailWidth - contentWidth) / 2;
        int y = (ThumbnailHeight - contentHeight) / 2;
        var contentRect = new Rectangle(x, y, contentWidth, contentHeight);

        using (var paper = new SolidBrush(Color.White))
            graphics.FillRectangle(paper, contentRect);

        graphics.DrawImage(rendered, contentRect);

        using (var border = new Pen(Color.FromArgb(203, 213, 225)))
            graphics.DrawRectangle(border, contentRect.X, contentRect.Y, contentRect.Width - 1, contentRect.Height - 1);

        return canvas;
    }

    private static FileInfo? TryGetFileInfo(string pdfPath)
    {
        if (string.IsNullOrWhiteSpace(pdfPath)) return null;

        try
        {
            var info = new FileInfo(pdfPath);
            return info.Exists ? info : null;
        }
        catch
        {
            return null;
        }
    }
}
