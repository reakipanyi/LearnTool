using LearningAssistant.Models.Pdf;
using System.Text;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>标注摘要条目：仅保留页码、时间戳与文字，不含音频/位图</summary>
    public sealed class PdfAnnotationDigestItem
    {
        public int PageIndex { get; init; }
        /// <summary>录音时间戳（毫秒），无时间戳时为 null</summary>
        public long? AudioTimestampMs { get; init; }
        public string Kind { get; init; } = "标注";
        public string Text { get; init; } = string.Empty;
    }

    /// <summary>
    /// 把页面标注整理为紧凑文本，供 AI 归纳为要点/待办/疑问清单。
    /// </summary>
    public static class PdfAnnotationDigestBuilder
    {
        /// <summary>最多采集的标注条目数，避免提示词过长</summary>
        public const int MaxItems = 200;

        /// <summary>单条文字最大长度</summary>
        public const int MaxTextLength = 200;

        /// <summary>采集一页标注（含文字与形状），按时间戳排序</summary>
        public static List<PdfAnnotationDigestItem> Collect(PdfAnnotation? annotation, int pageIndex)
        {
            var items = new List<PdfAnnotationDigestItem>();
            if (annotation == null) return items;

            foreach (var text in annotation.Texts)
            {
                var content = (text.Content ?? string.Empty).Trim();
                if (content.Length > MaxTextLength) content = content[..MaxTextLength] + "…";
                items.Add(new PdfAnnotationDigestItem
                {
                    PageIndex = pageIndex,
                    AudioTimestampMs = text.AudioTimestampMs,
                    Kind = "文字",
                    Text = content
                });
            }

            foreach (var stroke in annotation.Strokes)
            {
                var kind = string.IsNullOrWhiteSpace(stroke.ShapeType)
                    ? "笔划"
                    : $"形状-{GetShapeName(stroke.ShapeType)}";
                items.Add(new PdfAnnotationDigestItem
                {
                    PageIndex = pageIndex,
                    AudioTimestampMs = stroke.AudioTimestampMs,
                    Kind = kind,
                    Text = string.Empty
                });
            }

            return items
                .OrderBy(i => i.PageIndex)
                .ThenBy(i => i.AudioTimestampMs ?? long.MaxValue)
                .ToList();
        }

        /// <summary>构建提示词上下文：每行带页码锚点，有时间戳时附 mm:ss</summary>
        public static string Build(IEnumerable<PdfAnnotationDigestItem> items)
        {
            var list = items?.Where(i => i != null).Take(MaxItems).ToList() ?? new List<PdfAnnotationDigestItem>();
            var builder = new StringBuilder();

            foreach (var item in list.OrderBy(i => i.PageIndex).ThenBy(i => i.AudioTimestampMs ?? long.MaxValue))
            {
                builder.Append("[[p").Append(item.PageIndex + 1).Append("]]");
                if (item.AudioTimestampMs.HasValue)
                {
                    builder.Append(" [").Append(FormatTimestamp(item.AudioTimestampMs.Value)).Append(']');
                }
                builder.Append(' ').Append(item.Kind);
                if (!string.IsNullOrWhiteSpace(item.Text))
                {
                    builder.Append(": ").Append(item.Text);
                }
                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        /// <summary>毫秒转 mm:ss（超过一小时用 h:mm:ss）</summary>
        public static string FormatTimestamp(long milliseconds)
        {
            if (milliseconds < 0) milliseconds = 0;
            var time = TimeSpan.FromMilliseconds(milliseconds);
            return time.TotalHours >= 1
                ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}"
                : $"{time.Minutes:00}:{time.Seconds:00}";
        }

        private static string GetShapeName(string shapeType)
        {
            return shapeType switch
            {
                "Rectangle" => "矩形",
                "Ellipse" => "椭圆",
                "Arrow" => "箭头",
                "Mosaic" => "马赛克",
                "Strikethrough" => "删除线",
                "Pen" => "笔划",
                _ => shapeType
            };
        }
    }
}
