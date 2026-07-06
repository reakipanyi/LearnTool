using LearningAssistant.Models.Learning;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;

namespace LearningAssistant.Services.Learning
{
    public interface IPdfContentLinkService
    {
        List<ExtractedContent> ExtractAllContent(string pdfPath);
        List<ExtractedContent> ExtractHighlights(string pdfPath);
        List<ExtractedContent> ExtractBookmarks(string pdfPath);
        List<ExtractedContent> ExtractTextByPage(string pdfPath, int pageNumber);
        void EditContent(ExtractedContent content);
        void DeleteContent(ExtractedContent content);
        List<StudyMaterial> GenerateStudyMaterials(List<ExtractedContent> contents);
        void ExportToLearningLibrary(List<ExtractedContent> contents, string userId);
    }

    public class ExtractedContent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string PageLabel { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public string Color { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsSelected { get; set; } = false;
    }

    public class StudyMaterial
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public int RelevanceScore { get; set; }
    }

    public class PdfContentLinkService : IPdfContentLinkService
    {
        private readonly IPdfService _pdfService;
        private readonly IHighlightService _highlightService;
        private readonly IBookmarkService _bookmarkService;
        private readonly IContentLoaderService? _contentLoaderService;
        private readonly List<ExtractedContent> _editedContents = new List<ExtractedContent>();

        public PdfContentLinkService(IPdfService pdfService,
            IHighlightService highlightService,
            IBookmarkService bookmarkService,
            IContentLoaderService? contentLoaderService = null)
        {
            _pdfService = pdfService;
            _highlightService = highlightService;
            _bookmarkService = bookmarkService;
            _contentLoaderService = contentLoaderService;
        }

        public List<ExtractedContent> ExtractAllContent(string pdfPath)
        {
            var contents = new List<ExtractedContent>();

            contents.AddRange(ExtractTextByPage(pdfPath, 0));
            contents.AddRange(ExtractHighlights(pdfPath));
            contents.AddRange(ExtractBookmarks(pdfPath));

            return contents;
        }

        public List<ExtractedContent> ExtractHighlights(string pdfPath)
        {
            var highlights = _highlightService.GetHighlights(pdfPath);
            return highlights.Select(h => new ExtractedContent
            {
                Type = "Highlight",
                Content = h.Text,
                PageNumber = h.PageIndex + 1,
                PageLabel = $"第 {h.PageIndex + 1} 页",
                PositionX = h.NormalizedX,
                PositionY = h.NormalizedY,
                Color = GetColorHexString(h.Color),
                CreatedAt = h.CreatedAt
            }).ToList();
        }

        public List<ExtractedContent> ExtractBookmarks(string pdfPath)
        {
            var bookmarks = _bookmarkService.GetBookmarks(pdfPath);
            return bookmarks.Select(b => new ExtractedContent
            {
                Type = "Bookmark",
                Content = b.Title,
                PageNumber = b.PageIndex + 1,
                PageLabel = $"第 {b.PageIndex + 1} 页",
                CreatedAt = b.CreatedAt
            }).ToList();
        }

        public List<ExtractedContent> ExtractTextByPage(string pdfPath, int pageNumber)
        {
            var contents = new List<ExtractedContent>();

            if (pageNumber <= 0)
            {
                var totalPages = _pdfService.GetPageCount(pdfPath);
                for (int i = 1; i <= totalPages; i++)
                {
                    var text = _pdfService.ExtractText(pdfPath, i);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        contents.Add(new ExtractedContent
                        {
                            Type = "Text",
                            Content = text,
                            PageNumber = i,
                            PageLabel = $"第 {i} 页",
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }
            else
            {
                var text = _pdfService.ExtractText(pdfPath, pageNumber);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    contents.Add(new ExtractedContent
                    {
                        Type = "Text",
                        Content = text,
                        PageNumber = pageNumber,
                        PageLabel = $"第 {pageNumber} 页",
                        CreatedAt = DateTime.Now
                    });
                }
            }

            return contents;
        }

        public void EditContent(ExtractedContent content)
        {
            var existingIndex = _editedContents.FindIndex(c => c.Id == content.Id);
            if (existingIndex >= 0)
            {
                _editedContents[existingIndex] = content;
            }
            else
            {
                _editedContents.Add(content);
            }

            System.Diagnostics.Trace.TraceInformation($"内容已编辑: {content.Type} - {content.Content.Substring(0, Math.Min(50, content.Content.Length))}...");
        }

        public void DeleteContent(ExtractedContent content)
        {
            _editedContents.RemoveAll(c => c.Id == content.Id);

            if (content.Type == "Highlight")
            {
                var highlights = _highlightService.GetHighlights(content.PageLabel);
                var highlightToRemove = highlights.FirstOrDefault(h => h.Text.Contains(content.Content.Substring(0, Math.Min(20, content.Content.Length))));
                if (highlightToRemove != null)
                {
                    _highlightService.RemoveHighlight(highlightToRemove.PdfPath, highlightToRemove.Id);
                }
            }
            else if (content.Type == "Bookmark")
            {
                _bookmarkService.RemoveBookmark(content.PageLabel, content.PageNumber - 1, content.Content);
            }

            System.Diagnostics.Trace.TraceInformation($"内容已删除: {content.Type} - {content.PageLabel}");
        }

        public List<StudyMaterial> GenerateStudyMaterials(List<ExtractedContent> contents)
        {
            var materials = new List<StudyMaterial>();

            var textContents = contents.Where(c => c.Type == "Text").ToList();
            var highlightContents = contents.Where(c => c.Type == "Highlight").ToList();
            var bookmarkContents = contents.Where(c => c.Type == "Bookmark").ToList();

            foreach (var highlight in highlightContents)
            {
                materials.Add(new StudyMaterial
                {
                    Title = $"重点内容 - {highlight.PageLabel}",
                    Content = highlight.Content,
                    Type = "Flashcard",
                    Tags = new List<string> { "重点", "高亮", highlight.PageLabel },
                    RelevanceScore = 90
                });
            }

            foreach (var bookmark in bookmarkContents)
            {
                materials.Add(new StudyMaterial
                {
                    Title = bookmark.Content,
                    Content = $"跳转到 {bookmark.PageLabel}",
                    Type = "Bookmark",
                    Tags = new List<string> { "书签", bookmark.PageLabel },
                    RelevanceScore = 70
                });
            }

            foreach (var text in textContents)
            {
                if (text.Content.Length > 100)
                {
                    materials.Add(new StudyMaterial
                    {
                        Title = $"阅读材料 - {text.PageLabel}",
                        Content = text.Content.Substring(0, Math.Min(500, text.Content.Length)) + "...",
                        Type = "Reading",
                        Tags = new List<string> { "阅读", text.PageLabel },
                        RelevanceScore = 60
                    });
                }
            }

            return materials.OrderByDescending(m => m.RelevanceScore).ToList();
        }

        public void ExportToLearningLibrary(List<ExtractedContent> contents, string userId)
        {
            if (contents == null || contents.Count == 0)
            {
                System.Diagnostics.Trace.TraceInformation("没有内容可导出到学习库");
                return;
            }

            var materials = GenerateStudyMaterials(contents);

            if (_contentLoaderService != null)
            {
                foreach (var material in materials)
                {
                    try
                    {
                        _contentLoaderService.SaveUserContent(new UserContent
                        {
                            UserId = userId,
                            Title = material.Title,
                            Content = material.Content,
                            Category = material.Type,
                            Tags = string.Join(",", material.Tags),
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        });
                        System.Diagnostics.Trace.TraceInformation($"已导出学习素材: {material.Title}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError($"导出学习素材失败: {material.Title} - {ex.Message}");
                    }
                }
            }
            else
            {
                System.Diagnostics.Trace.TraceWarning($"生成了 {materials.Count} 个学习素材，但无法保存到学习库（ContentLoaderService 未注入）");
            }
        }

        private string GetColorHexString(HighlightColor color)
        {
            return color switch
            {
                HighlightColor.Yellow => "#FFFF00",
                HighlightColor.Green => "#00FF00",
                HighlightColor.Blue => "#00BFFF",
                HighlightColor.Pink => "#FFC0CB",
                HighlightColor.Orange => "#FFA500",
                _ => "#FFFF00"
            };
        }
    }
}