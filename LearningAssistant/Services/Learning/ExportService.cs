using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System.Drawing.Imaging;

namespace LearningAssistant.Services.Learning
{
    public class ExportService : IExportService
    {
        private readonly ILogger<ExportService> _logger;
        private readonly IHighlightService _highlightService;
        private readonly IAnnotationService? _annotationService;
        public ExportService(ILogger<ExportService> logger, IHighlightService highlightService, IAnnotationService? annotationService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ExcelPackage.License.SetNonCommercialPersonal("LearningAssistant");

            _highlightService = highlightService ?? throw new ArgumentNullException(nameof(highlightService));
            _annotationService = annotationService;
        }


        public async Task<bool> ExportHighlightsToExcelAsync(
            List<PdfHighlight> highlights,
            string pdfPath,
            string outputPath,
            IPdfService? pdfService = null,
            List<string>? imageFiles = null)
        {
            try
            {
                if (highlights == null || highlights.Count == 0)
                {
                    _logger.LogWarning("No highlights to export");
                    return false;
                }

                _logger.LogInformation($"Exporting {highlights.Count} highlights to {outputPath}");

                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("高亮错题集");

                worksheet.Cells[1, 1].Value = "序号";
                worksheet.Cells[1, 2].Value = "文件名";
                worksheet.Cells[1, 3].Value = "页码";
                worksheet.Cells[1, 4].Value = "高亮文本";
                worksheet.Cells[1, 5].Value = "备注";
                worksheet.Cells[1, 6].Value = "图片";

                using (var range = worksheet.Cells[1, 1, 1, 6])
                {
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                worksheet.Column(1).Width = 8;
                worksheet.Column(2).Width = 15;
                worksheet.Column(3).Width = 8;
                worksheet.Column(4).Width = 40;
                worksheet.Column(5).Width = 25;
                worksheet.Column(6).Width = 50;

                var sortedHighlights = highlights.OrderBy(h => h.PdfPath).ThenBy(h => h.PageIndex).ToList();

                bool isImageMode = imageFiles != null && imageFiles.Count > 0 && pdfService == null;

                for (int i = 0; i < sortedHighlights.Count; i++)
                {
                    var highlight = sortedHighlights[i];
                    int row = i + 2;

                    worksheet.Cells[row, 1].Value = i + 1;
                    worksheet.Cells[row, 2].Value = Path.GetFileName(highlight.PdfPath);
                    worksheet.Cells[row, 3].Value = highlight.PageIndex + 1;
                    worksheet.Cells[row, 4].Value = highlight.Text;
                    worksheet.Cells[row, 4].Style.WrapText = true;
                    worksheet.Cells[row, 5].Value = highlight.Note;
                    worksheet.Cells[row, 5].Style.WrapText = true;

                    try
                    {
                        Bitmap? image = null;
                        if (isImageMode && imageFiles != null)
                        {
                            image = await CaptureHighlightFromImageAsync(highlight, imageFiles);
                        }
                        else if (pdfService != null)
                        {
                            image = await CaptureHighlightImageAsync(highlight, pdfPath, pdfService);
                        }

                        if (image != null)
                        {
                            using (var ms = new MemoryStream())
                            {
                                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                ms.Position = 0;

                                var picture = worksheet.Drawings.AddPicture($"highlight_{i}", ms);
                                picture.SetPosition(row - 1, 5, 4, 0);
                                picture.SetSize(image.Width, image.Height);

                                int rowHeight = Math.Max(image.Height + 20, 100);
                                worksheet.Row(row).Height = rowHeight;
                            }

                            image.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to capture highlight image for index {i}, page {highlight.PageIndex}");
                        worksheet.Cells[row, 6].Value = $"截图失败: {ex.Message}";
                    }
                }

                await package.SaveAsAsync(new FileInfo(outputPath));

                _logger.LogInformation($"Successfully exported highlights to {outputPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export highlights to Excel");
                return false;
            }
        }

        private async Task<Bitmap?> CaptureHighlightImageAsync(
            PdfHighlight highlight,
            string pdfPath,
            IPdfService pdfService)
        {
            try
            {
                var pageSize = pdfService.GetPageSize(highlight.PageIndex);
                if (pageSize.Width <= 0 || pageSize.Height <= 0)
                {
                    _logger.LogWarning($"Invalid page size for page {highlight.PageIndex}");
                    return null;
                }

                var renderWidth = 1000;
                var renderHeight = (int)(renderWidth * pageSize.Height / pageSize.Width);
                var pageBitmap = pdfService.RenderPage(highlight.PageIndex, renderWidth, renderHeight);

                if (pageBitmap == null)
                {
                    _logger.LogWarning($"Failed to render page {highlight.PageIndex}");
                    return null;
                }

                var cropX = (int)(highlight.NormalizedX * renderWidth);
                var cropY = (int)(highlight.NormalizedY * renderHeight);
                var cropWidth = (int)(highlight.NormalizedWidth * renderWidth);
                var cropHeight = (int)(highlight.NormalizedHeight * renderHeight);

                cropX = Math.Max(0, cropX);
                cropY = Math.Max(0, cropY);
                cropWidth = Math.Min(renderWidth - cropX, Math.Max(1, cropWidth));
                cropHeight = Math.Min(renderHeight - cropY, Math.Max(1, cropHeight));

                using (pageBitmap)
                {
                    var croppedImage = new Bitmap(cropWidth, cropHeight, PixelFormat.Format24bppRgb);
                    using (var g = Graphics.FromImage(croppedImage))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(
                            pageBitmap,
                            new Rectangle(0, 0, cropWidth, cropHeight),
                            new Rectangle(cropX, cropY, cropWidth, cropHeight),
                            GraphicsUnit.Pixel);
                    }

                    return croppedImage;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to capture highlight image for highlight {highlight.Id}");
                return null;
            }
        }

        private async Task<Bitmap?> CaptureHighlightFromImageAsync(
            PdfHighlight highlight,
            List<string> imageFiles)
        {
            try
            {
                string? imagePath = imageFiles.FirstOrDefault(f =>
                    string.Equals(Path.GetFileName(f), Path.GetFileName(highlight.PdfPath),
                    StringComparison.OrdinalIgnoreCase));

                if (imagePath == null)
                {
                    _logger.LogWarning($"Image file not found for {highlight.PdfPath}");
                    return null;
                }

                if (!File.Exists(imagePath))
                {
                    _logger.LogWarning($"Image file not found: {imagePath}");
                    return null;
                }

                using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                {
                    using (var originalImage = new Bitmap(stream))
                    {
                        int renderWidth = originalImage.Width;
                        int renderHeight = originalImage.Height;

                        var cropX = (int)(highlight.NormalizedX * renderWidth);
                        var cropY = (int)(highlight.NormalizedY * renderHeight);
                        var cropWidth = (int)(highlight.NormalizedWidth * renderWidth);
                        var cropHeight = (int)(highlight.NormalizedHeight * renderHeight);

                        cropX = Math.Max(0, cropX);
                        cropY = Math.Max(0, cropY);
                        cropWidth = Math.Min(renderWidth - cropX, Math.Max(1, cropWidth));
                        cropHeight = Math.Min(renderHeight - cropY, Math.Max(1, cropHeight));

                        var croppedImage = new Bitmap(cropWidth, cropHeight, PixelFormat.Format24bppRgb);
                        using (var g = Graphics.FromImage(croppedImage))
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.DrawImage(
                                originalImage,
                                new Rectangle(0, 0, cropWidth, cropHeight),
                                new Rectangle(cropX, cropY, cropWidth, cropHeight),
                                GraphicsUnit.Pixel);
                        }

                        return croppedImage;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to capture highlight image from image file {highlight.PageIndex}");
                return null;
            }
        }



        public async Task<bool> ExportHighlightsToExcelAsync(string outputPath, List<PdfHighlight> highlights, string sourcePath, bool isImageMode, List<string>? imageFiles = null, IPdfService? pdfService = null)
        {
            if (highlights == null || highlights.Count == 0)
            {
                _logger.LogWarning("No highlights to export");
                return false;
            }

            try
            {
                if (isImageMode)
                {
                    var folderPath = Path.GetDirectoryName(sourcePath) ?? "";
                    return await ExportHighlightsToExcelAsync(highlights, folderPath, outputPath, null, imageFiles?.ToList() ?? new List<string>());
                }
                else
                {
                    return await ExportHighlightsToExcelAsync(highlights, sourcePath, outputPath, pdfService);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export highlights");
                return false;
            }
        }

        public async Task<bool> ExportHighlightsToExcelAsync(string outputPath, string sourcePath, bool isImageMode, List<string>? imageFiles = null, IPdfService? pdfService = null, bool includeAnnotations = false)
        {
            var folderPath = Path.GetDirectoryName(sourcePath) ?? "";
            List<PdfHighlight> highlights;

            if (isImageMode)
            {
                highlights = _highlightService.GetHighlightsForFolder(folderPath);
            }
            else
            {
                highlights = _highlightService.GetHighlights(sourcePath);
            }

            if (highlights == null || highlights.Count == 0)
            {
                _logger.LogWarning("No highlights to export");
                return false;
            }

            return await ExportHighlightsToExcelAsync(outputPath, highlights, sourcePath, isImageMode, imageFiles, pdfService);
        }

        public async Task<bool> ExportAnnotationsToExcelAsync(
            List<AnnotationStroke> strokes,
            string pdfPath,
            string outputPath,
            IPdfService? pdfService = null,
            int pageCount = 0)
        {
            try
            {
                if (strokes == null || strokes.Count == 0)
                {
                    _logger.LogWarning("No annotations to export");
                    return false;
                }

                _logger.LogInformation($"Exporting {strokes.Count} annotations to {outputPath}");

                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("标注");

                worksheet.Cells[1, 1].Value = "序号";
                worksheet.Cells[1, 2].Value = "文件名";
                worksheet.Cells[1, 3].Value = "页码";
                worksheet.Cells[1, 4].Value = "颜色";
                worksheet.Cells[1, 5].Value = "线宽";
                worksheet.Cells[1, 6].Value = "点数";
                worksheet.Cells[1, 7].Value = "图片";

                using (var range = worksheet.Cells[1, 1, 1, 7])
                {
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                }

                worksheet.Column(1).Width = 8;
                worksheet.Column(2).Width = 15;
                worksheet.Column(3).Width = 8;
                worksheet.Column(4).Width = 12;
                worksheet.Column(5).Width = 8;
                worksheet.Column(6).Width = 8;
                worksheet.Column(7).Width = 50;

                for (int i = 0; i < strokes.Count; i++)
                {
                    var stroke = strokes[i];
                    int row = i + 2;

                    worksheet.Cells[row, 1].Value = i + 1;
                    worksheet.Cells[row, 2].Value = Path.GetFileName(pdfPath);
                    worksheet.Cells[row, 3].Value = stroke.PageIndex + 1;
                    
                    var color = System.Drawing.Color.FromArgb(stroke.ColorArgb);
                    worksheet.Cells[row, 4].Value = color.Name;
                    worksheet.Cells[row, 5].Value = stroke.Thickness;
                    worksheet.Cells[row, 6].Value = stroke.Points.Length / 2;

                    try
                    {
                        if (pdfService != null && pageCount > 0)
                        {
                            var pageSize = pdfService.GetPageSize(stroke.PageIndex);
                            if (pageSize.Width > 0 && pageSize.Height > 0)
                            {
                                var renderWidth = 1000;
                                var renderHeight = (int)(renderWidth * pageSize.Height / pageSize.Width);
                                var pageBitmap = pdfService.RenderPage(stroke.PageIndex, renderWidth, renderHeight);

                                if (pageBitmap != null)
                                {
                                    using (pageBitmap)
                                    {
                                        float minX = float.MaxValue, minY = float.MaxValue;
                                        float maxX = float.MinValue, maxY = float.MinValue;

                                        for (int j = 0; j < stroke.Points.Length; j += 2)
                                        {
                                            float x = stroke.Points[j] * renderWidth;
                                            float y = stroke.Points[j + 1] * renderHeight;
                                            minX = Math.Min(minX, x);
                                            minY = Math.Min(minY, y);
                                            maxX = Math.Max(maxX, x);
                                            maxY = Math.Max(maxY, y);
                                        }

                                        int padding = 20;
                                        int cropX = Math.Max(0, (int)minX - padding);
                                        int cropY = Math.Max(0, (int)minY - padding);
                                        int cropWidth = Math.Min(renderWidth - cropX, Math.Max(1, (int)(maxX - minX) + padding * 2));
                                        int cropHeight = Math.Min(renderHeight - cropY, Math.Max(1, (int)(maxY - minY) + padding * 2));

                                        var croppedImage = new Bitmap(cropWidth, cropHeight, PixelFormat.Format24bppRgb);
                                        using (var g = Graphics.FromImage(croppedImage))
                                        {
                                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                            g.DrawImage(
                                                pageBitmap,
                                                new Rectangle(0, 0, cropWidth, cropHeight),
                                                new Rectangle(cropX, cropY, cropWidth, cropHeight),
                                                GraphicsUnit.Pixel);

                                            using (var pen = new System.Drawing.Pen(color, stroke.Thickness))
                                            {
                                                var points = new List<System.Drawing.Point>();
                                                for (int j = 0; j < stroke.Points.Length; j += 2)
                                                {
                                                    points.Add(new System.Drawing.Point(
                                                        (int)(stroke.Points[j] * renderWidth) - cropX,
                                                        (int)(stroke.Points[j + 1] * renderHeight) - cropY));
                                                }
                                                if (points.Count > 1)
                                                {
                                                    g.DrawLines(pen, points.ToArray());
                                                }
                                            }
                                        }

                                        using (var ms = new MemoryStream())
                                        {
                                            croppedImage.Save(ms, ImageFormat.Png);
                                            ms.Position = 0;

                                            var picture = worksheet.Drawings.AddPicture($"annotation_{i}", ms);
                                            picture.SetPosition(row - 1, 5, 5, 0);
                                            picture.SetSize(croppedImage.Width, croppedImage.Height);

                                            int rowHeight = Math.Max(croppedImage.Height + 20, 100);
                                            worksheet.Row(row).Height = rowHeight;
                                        }

                                        croppedImage.Dispose();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to capture annotation image for index {i}, page {stroke.PageIndex}");
                    }
                }

                await package.SaveAsAsync(new FileInfo(outputPath));

                _logger.LogInformation($"Successfully exported annotations to {outputPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export annotations to Excel");
                return false;
            }
        }
    }
}