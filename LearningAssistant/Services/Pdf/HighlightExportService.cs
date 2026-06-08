using System.Drawing.Imaging;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Pdf
{
    public class HighlightExportService
    {
        private readonly ILogger<HighlightExportService> _logger;

        public HighlightExportService(ILogger<HighlightExportService> logger)
        {
            _logger = logger;
            // 设置 EPPlus 许可证
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// 导出高亮到 Excel
        /// </summary>
        public async Task<bool> ExportHighlightsToExcelAsync(
            List<PdfHighlight> highlights,
            string pdfPath,
            string outputPath,
            IPdfService? pdfService = null,
            List<string>? imageFiles = null)
        {
            try
            {
                if (highlights.Count == 0)
                {
                    _logger.LogWarning("No highlights to export");
                    return false;
                }

                _logger.LogInformation($"Exporting {highlights.Count} highlights to {outputPath}");

                // 确保目录存在
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("高亮错题集");

                // 设置标题
                worksheet.Cells[1, 1].Value = "序号";
                worksheet.Cells[1, 2].Value = "页码";
                worksheet.Cells[1, 3].Value = "高亮文本";
                worksheet.Cells[1, 4].Value = "备注";
                worksheet.Cells[1, 5].Value = "图片";

                // 设置标题样式
                using (var range = worksheet.Cells[1, 1, 1, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                // 调整列宽
                worksheet.Column(1).Width = 8;  // 序号
                worksheet.Column(2).Width = 10;  // 页码
                worksheet.Column(3).Width = 40;  // 高亮文本
                worksheet.Column(4).Width = 25;  // 备注
                worksheet.Column(5).Width = 50;  // 图片

                // 按页码排序
                var sortedHighlights = highlights.OrderBy(h => h.PageIndex).ToList();

                // 检测是否为图片模式
                bool isImageMode = imageFiles != null && imageFiles.Count > 0 && pdfService == null;

                for (int i = 0; i < sortedHighlights.Count; i++)
                {
                    var highlight = sortedHighlights[i];
                    int row = i + 2;

                    // 序号
                    worksheet.Cells[row, 1].Value = i + 1;

                    // 页码
                    worksheet.Cells[row, 2].Value = highlight.PageIndex + 1;

                    // 高亮文本
                    worksheet.Cells[row, 3].Value = highlight.Text;
                    worksheet.Cells[row, 3].Style.WrapText = true;

                    // 备注
                    worksheet.Cells[row, 4].Value = highlight.Note;
                    worksheet.Cells[row, 4].Style.WrapText = true;

                    // 调整行高
                    worksheet.Row(row).Height = 150;

                    // 截取高亮区域图片并插入到 Excel
                    try
                    {
                        Bitmap? image = null;
                        if (isImageMode && imageFiles != null)
                        {
                            // 图片模式：从图片文件截取
                            image = await CaptureHighlightFromImageAsync(highlight, imageFiles);
                        }
                        else if (pdfService != null)
                        {
                            // PDF模式：从PDF渲染截取
                            image = await CaptureHighlightImageAsync(highlight, pdfPath, pdfService);
                        }

                        if (image != null)
                        {
                            // 添加图片到 Excel - 使用内存流方式
                            using (var ms = new MemoryStream())
                            {
                                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                ms.Position = 0;
                                
                                var picture = worksheet.Drawings.AddPicture($"highlight_{i}", ms);
                                picture.SetPosition(row - 1, 5, 4, 0);
                                picture.SetSize(450, 130);
                            }

                            // 释放图片资源
                            image.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to capture highlight image for index {i}");
                        worksheet.Cells[row, 5].Value = "截图失败";
                    }
                }

                // 保存 Excel 文件
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

        /// <summary>
        /// 截取高亮区域的图片
        /// </summary>
        private async Task<Bitmap?> CaptureHighlightImageAsync(
            PdfHighlight highlight,
            string pdfPath,
            IPdfService pdfService)
        {
            try
            {
                // 渲染页面
                var pageSize = pdfService.GetPageSize(highlight.PageIndex);
                if (pageSize.Width <= 0 || pageSize.Height <= 0)
                {
                    _logger.LogWarning($"Invalid page size for page {highlight.PageIndex}");
                    return null;
                }

                // 渲染完整页面 (高分辨率)
                var renderWidth = (int)(pageSize.Width * 2);
                var renderHeight = (int)(pageSize.Height * 2);
                var pageBitmap = pdfService.RenderPage(highlight.PageIndex, renderWidth, renderHeight);
                
                if (pageBitmap == null)
                {
                    _logger.LogWarning($"Failed to render page {highlight.PageIndex}");
                    return null;
                }

                // 计算高亮区域在图片上的位置
                var cropX = (int)(highlight.NormalizedX * renderWidth);
                var cropY = (int)(highlight.NormalizedY * renderHeight);
                var cropWidth = (int)(highlight.NormalizedWidth * renderWidth);
                var cropHeight = (int)(highlight.NormalizedHeight * renderHeight);

                // 添加边距
                var margin = 50;
                cropX = Math.Max(0, cropX - margin);
                cropY = Math.Max(0, cropY - margin);
                cropWidth = Math.Min(pageBitmap.Width - cropX, cropWidth + margin * 2);
                cropHeight = Math.Min(pageBitmap.Height - cropY, cropHeight + margin * 2);

                // 截取高亮区域
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

        /// <summary>
        /// 从图片文件截取高亮区域
        /// </summary>
        private async Task<Bitmap?> CaptureHighlightFromImageAsync(
            PdfHighlight highlight,
            List<string> imageFiles)
        {
            try
            {
                if (highlight.PageIndex < 0 || highlight.PageIndex >= imageFiles.Count)
                {
                    _logger.LogWarning($"Invalid page index {highlight.PageIndex} for image files count {imageFiles.Count}");
                    return null;
                }

                string imagePath = imageFiles[highlight.PageIndex];
                if (!File.Exists(imagePath))
                {
                    _logger.LogWarning($"Image file not found: {imagePath}");
                    return null;
                }

                // 加载图片
                using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                {
                    using (var originalImage = new Bitmap(stream))
                    {
                        int renderWidth = originalImage.Width;
                        int renderHeight = originalImage.Height;

                        // 计算高亮区域在图片上的位置
                        var cropX = (int)(highlight.NormalizedX * renderWidth);
                        var cropY = (int)(highlight.NormalizedY * renderHeight);
                        var cropWidth = (int)(highlight.NormalizedWidth * renderWidth);
                        var cropHeight = (int)(highlight.NormalizedHeight * renderHeight);

                        // 添加边距
                        var margin = 50;
                        cropX = Math.Max(0, cropX - margin);
                        cropY = Math.Max(0, cropY - margin);
                        cropWidth = Math.Min(originalImage.Width - cropX, cropWidth + margin * 2);
                        cropHeight = Math.Min(originalImage.Height - cropY, cropHeight + margin * 2);

                        // 截取高亮区域
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
    }
}
