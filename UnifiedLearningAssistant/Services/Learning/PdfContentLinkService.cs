using System;
using System.Collections.Generic;
using System.Linq;
using UnifiedLearningAssistant.Models.Pdf;
using UnifiedLearningAssistant.Services.Pdf;

namespace UnifiedLearningAssistant.Services.Learning
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
        public string Type { get; set; }
        public string Content { get; set; }
        public string PageLabel { get; set; }
        public int PageNumber { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public string Color { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsSelected { get; set; } = false;
    }

    public class StudyMaterial
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; }
        public string Content { get; set; }
        public string Type { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public int RelevanceScore { get; set; }
    }

    public class PdfContentLinkService : IPdfContentLinkService
    {
        private readonly IPdfService _pdfService;
        private readonly IHighlightService _highlightService;
        private readonly IBookmarkService _bookmarkService;

        public PdfContentLinkService(IPdfService pdfService, 
            IHighlightService highlightService, 
            IBookmarkService bookmarkService)
        {
            _pdfService = pdfService;
            _highlightService = highlightService;
            _bookmarkService = bookmarkService;
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
            var highlights = _highlightService.GetAllHighlights(pdfPath);
            return highlights.Select(h => new ExtractedContent
            {
                Type = "Highlight",
                Content = h.Text,
                PageNumber = h.PageNumber,
                PageLabel = $"第 {h.PageNumber} 页",
                PositionX = h.Rect.Left,
                PositionY = h.Rect.Top,
                Color = h.Color.ToHexString(),
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
                PageNumber = b.PageNumber,
                PageLabel = $"第 {b.PageNumber} 页",
                CreatedAt = DateTime.Now
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
        }

        public void DeleteContent(ExtractedContent content)
        {
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
        }
    }
}