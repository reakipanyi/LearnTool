using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;

namespace LearningAssistant.Services.Learning
{
    public interface IHighlightSyncService
    {
        void SyncToMarkdown(string pdfPath, string outputPath);
        void SyncToNotebook(string pdfPath);
        void ExportHighlightsWithTags(string pdfPath, string outputPath);
        void ImportFromMarkdown(string markdownPath, string pdfPath);
        List<HighlightTag> GetAvailableTags();
        void AddTagToHighlight(string pdfPath, string highlightId, string tagName);
        void RemoveTagFromHighlight(string pdfPath, string highlightId, string tagName);
        List<PdfHighlight> GetHighlightsByTag(string pdfPath, string tagName);
    }

    public class HighlightTag
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#FFFF00";
        public int Count { get; set; } = 0;
    }

    public class HighlightSyncService : IHighlightSyncService
    {
        private readonly IHighlightService _highlightService;
        private readonly IBookmarkService _bookmarkService;
        private readonly ILogger<HighlightSyncService>? _logger;
        private readonly List<HighlightTag> _defaultTags = new List<HighlightTag>
        {
            new HighlightTag { Name = "重要", Color = "#FF0000", Count = 0 },
            new HighlightTag { Name = "疑问", Color = "#0000FF", Count = 0 },
            new HighlightTag { Name = "待复习", Color = "#FFA500", Count = 0 },
            new HighlightTag { Name = "笔记", Color = "#00FF00", Count = 0 },
            new HighlightTag { Name = "例子", Color = "#FFC0CB", Count = 0 }
        };

        public HighlightSyncService(IHighlightService highlightService, IBookmarkService bookmarkService, ILogger<HighlightSyncService>? logger = null)
        {
            _highlightService = highlightService;
            _bookmarkService = bookmarkService;
            _logger = logger;
        }

        public void SyncToMarkdown(string pdfPath, string outputPath)
        {
            try
            {
                var highlights = _highlightService.GetHighlights(pdfPath);
                var bookmarks = _bookmarkService.GetBookmarks(pdfPath);

                var markdown = new System.Text.StringBuilder();
                markdown.AppendLine($"# {Path.GetFileName(pdfPath)}");
                markdown.AppendLine();
                markdown.AppendLine($"> 导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                markdown.AppendLine();

                if (bookmarks.Any())
                {
                    markdown.AppendLine("## 📚 书签");
                    markdown.AppendLine();
                    foreach (var bookmark in bookmarks.OrderBy(b => b.PageIndex))
                    {
                        markdown.AppendLine($"- [{bookmark.Title}](#page-{bookmark.PageIndex + 1})");
                    }
                    markdown.AppendLine();
                }

                if (highlights.Any())
                {
                    markdown.AppendLine("## 🔖 高亮笔记");
                    markdown.AppendLine();

                    var highlightsByPage = highlights.GroupBy(h => h.PageIndex).OrderBy(g => g.Key);
                    foreach (var pageGroup in highlightsByPage)
                    {
                        markdown.AppendLine($"### 第 {pageGroup.Key + 1} 页");
                        markdown.AppendLine();

                        foreach (var highlight in pageGroup)
                        {
                            var colorTag = GetColorTag(highlight.Color);
                            markdown.AppendLine($"{colorTag} {highlight.Text}");
                            
                            if (!string.IsNullOrEmpty(highlight.Note))
                            {
                                markdown.AppendLine();
                                markdown.AppendLine($"> {highlight.Note}");
                            }
                            markdown.AppendLine();
                        }
                    }
                }

                File.WriteAllText(outputPath, markdown.ToString());
                _logger?.LogInformation("高亮导出到 Markdown 成功: {OutputPath}", outputPath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出高亮到 Markdown 失败: {PdfPath}", pdfPath);
                throw;
            }
        }

        public void SyncToNotebook(string pdfPath)
        {
            try
            {
                var tempPath = Path.GetTempFileName() + ".md";
                SyncToMarkdown(pdfPath, tempPath);

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);

                _logger?.LogInformation("打开笔记文件: {Path}", tempPath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "同步到笔记失败: {PdfPath}", pdfPath);
                throw;
            }
        }

        public void ExportHighlightsWithTags(string pdfPath, string outputPath)
        {
            try
            {
                var highlights = _highlightService.GetHighlights(pdfPath);
                
                var groupedByColor = highlights.GroupBy(h => h.Color);
                var markdown = new System.Text.StringBuilder();
                markdown.AppendLine($"# {Path.GetFileName(pdfPath)} - 按标签分类");
                markdown.AppendLine();

                foreach (var group in groupedByColor)
                {
                    var tagName = GetTagNameByColor(group.Key);
                    markdown.AppendLine($"## {tagName}");
                    markdown.AppendLine();

                    foreach (var highlight in group.OrderBy(h => h.PageIndex))
                    {
                        markdown.AppendLine($"- 第 {highlight.PageIndex + 1} 页: {highlight.Text}");
                    }
                    markdown.AppendLine();
                }

                File.WriteAllText(outputPath, markdown.ToString());
                _logger?.LogInformation("按标签导出高亮成功: {OutputPath}", outputPath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "按标签导出高亮失败: {PdfPath}", pdfPath);
                throw;
            }
        }

        public void ImportFromMarkdown(string markdownPath, string pdfPath)
        {
            try
            {
                var content = File.ReadAllText(markdownPath);
                var lines = content.Split('\n');
                int currentPage = 1;

                foreach (var line in lines)
                {
                    if (line.StartsWith("### 第"))
                    {
                        int.TryParse(line.Replace("### 第", "").Replace("页", ""), out currentPage);
                    }
                    else if (line.StartsWith("- ") || line.StartsWith("* "))
                    {
                        var text = line.Substring(2).Trim();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            _highlightService.AddHighlight(pdfPath, currentPage - 1, 0, 0, 0, 0, text);
                        }
                    }
                }

                _logger?.LogInformation("从 Markdown 导入高亮成功: {MarkdownPath}", markdownPath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "从 Markdown 导入高亮失败: {MarkdownPath}", markdownPath);
                throw;
            }
        }

        public List<HighlightTag> GetAvailableTags()
        {
            return _defaultTags.ToList();
        }

        public void AddTagToHighlight(string pdfPath, string highlightId, string tagName)
        {
            var highlights = _highlightService.GetHighlights(pdfPath);
            var highlight = highlights.FirstOrDefault(h => h.Id == highlightId);
            if (highlight != null)
            {
                var tag = _defaultTags.FirstOrDefault(t => t.Name == tagName);
                if (tag != null)
                {
                    highlight.Color = GetColorByTagName(tagName);
                    tag.Count++;
                    _logger?.LogInformation("为高亮添加标签: {HighlightId}, 标签: {TagName}", highlightId, tagName);
                }
            }
        }

        public void RemoveTagFromHighlight(string pdfPath, string highlightId, string tagName)
        {
            var highlights = _highlightService.GetHighlights(pdfPath);
            var highlight = highlights.FirstOrDefault(h => h.Id == highlightId);
            if (highlight != null)
            {
                var tag = _defaultTags.FirstOrDefault(t => t.Name == tagName);
                if (tag != null && tag.Count > 0)
                {
                    tag.Count--;
                    _logger?.LogInformation("从高亮移除标签: {HighlightId}, 标签: {TagName}", highlightId, tagName);
                }
            }
        }

        public List<PdfHighlight> GetHighlightsByTag(string pdfPath, string tagName)
        {
            var color = GetColorByTagName(tagName);
            var highlights = _highlightService.GetHighlights(pdfPath);
            return highlights.Where(h => h.Color == color).ToList();
        }

        private string GetColorTag(HighlightColor color)
        {
            return color switch
            {
                HighlightColor.Red => "🔴",
                HighlightColor.Yellow => "🟡",
                HighlightColor.Green => "🟢",
                HighlightColor.Blue => "🔵",
                HighlightColor.Pink => "💗",
                HighlightColor.Orange => "🟠",
                _ => "📌"
            };
        }

        private string GetTagNameByColor(HighlightColor color)
        {
            return color switch
            {
                HighlightColor.Red => "🔴 重要",
                HighlightColor.Yellow => "🟡 一般",
                HighlightColor.Green => "🟢 笔记",
                HighlightColor.Blue => "🔵 疑问",
                HighlightColor.Pink => "💗 例子",
                HighlightColor.Orange => "🟠 待复习",
                _ => "📌 其他"
            };
        }

        private HighlightColor GetColorByTagName(string tagName)
        {
            return tagName switch
            {
                "重要" => HighlightColor.Red,
                "疑问" => HighlightColor.Blue,
                "待复习" => HighlightColor.Orange,
                "笔记" => HighlightColor.Green,
                "例子" => HighlightColor.Pink,
                _ => HighlightColor.Yellow
            };
        }
    }
}