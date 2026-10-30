using LearningAssistant.Common;
using System.Drawing.Drawing2D;
using System.Net;
using System.Text;
using System.Text.Json;

namespace LearningAssistant.Forms.Pdf
{
    /// <summary>大纲中的一个章节条目：标题、起始页（1 起，0 表示未知）与要点。</summary>
    public sealed class PdfOutlineEntry
    {
        public PdfOutlineEntry(string title, int pageNumber, IReadOnlyList<string> points)
        {
            Title = title;
            PageNumber = pageNumber;
            Points = points;
        }

        public string Title { get; }
        public int PageNumber { get; }
        public IReadOnlyList<string> Points { get; }

        /// <summary>树节点与导出使用的显示文本</summary>
        public string DisplayText => PageNumber > 0 ? $"{Title}（第 {PageNumber} 页）" : Title;
    }

    /// <summary>解析后的文档大纲，供树形视图、Markdown/OPML/PNG 导出共用。</summary>
    public sealed class PdfOutlineModel
    {
        public PdfOutlineModel(string title, IReadOnlyList<PdfOutlineEntry> entries)
        {
            Title = title;
            Entries = entries;
        }

        public string Title { get; }
        public IReadOnlyList<PdfOutlineEntry> Entries { get; }

        /// <summary>
        /// 解析 AI 返回的大纲 JSON；结构不符时返回 null，由调用方回退到纯文本展示。
        /// </summary>
        public static PdfOutlineModel? Parse(string content, int pageCount)
        {
            var json = StripCodeFence(content);
            try
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind != JsonValueKind.Object) return null;

                var title = document.RootElement.TryGetProperty("title", out var titleNode)
                    ? (titleNode.GetString() ?? string.Empty).Trim()
                    : string.Empty;
                if (!document.RootElement.TryGetProperty("sections", out var sections) ||
                    sections.ValueKind != JsonValueKind.Array)
                    return null;

                var entries = new List<PdfOutlineEntry>();
                int index = 1;
                foreach (var section in sections.EnumerateArray())
                {
                    if (section.ValueKind != JsonValueKind.Object) continue;

                    var sectionTitle = section.TryGetProperty("title", out var sectionTitleNode)
                        ? (sectionTitleNode.GetString() ?? string.Empty).Trim()
                        : string.Empty;
                    if (string.IsNullOrWhiteSpace(sectionTitle))
                        sectionTitle = $"第 {index} 部分";

                    int pageNumber = section.TryGetProperty("page", out var pageNode)
                        && pageNode.TryGetInt32(out var parsedPage)
                            ? Math.Clamp(parsedPage, 0, Math.Max(0, pageCount))
                            : 0;

                    var points = new List<string>();
                    if (section.TryGetProperty("points", out var pointsNode) &&
                        pointsNode.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var point in pointsNode.EnumerateArray())
                        {
                            var text = (point.GetString() ?? string.Empty).Trim();
                            if (!string.IsNullOrWhiteSpace(text)) points.Add(text);
                        }
                    }

                    entries.Add(new PdfOutlineEntry(sectionTitle, pageNumber, points));
                    index++;
                }

                return entries.Count == 0 ? null : new PdfOutlineModel(title, entries);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>去掉模型偶发输出的 Markdown 代码块围栏</summary>
        private static string StripCodeFence(string content)
        {
            var text = content?.Trim() ?? string.Empty;
            if (!text.StartsWith("```", StringComparison.Ordinal)) return text;

            int firstLineEnd = text.IndexOf('\n');
            if (firstLineEnd < 0) return text;

            text = text[(firstLineEnd + 1)..];
            if (text.EndsWith("```", StringComparison.Ordinal))
                text = text[..^3];
            return text.Trim();
        }
    }

    /// <summary>
    /// 大纲树形视图：章节展开为要点，双击章节跳回原文页；支持导出 Markdown / OPML / 思维导图 PNG。
    /// </summary>
    public sealed class PdfOutlineTreeForm : Form
    {
        private readonly PdfOutlineModel _model;
        private readonly string _documentName;
        private readonly Action<int>? _navigateToPage;
        private readonly TreeView _tree;

        public PdfOutlineTreeForm(PdfOutlineModel model, string documentName, Action<int>? navigateToPage)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _documentName = documentName ?? string.Empty;
            _navigateToPage = navigateToPage;

            Text = $"文档大纲 - {documentName}";
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(620, 520);
            MinimumSize = new Size(460, 360);
            MinimizeBox = false;
            ShowInTaskbar = false;

            _tree = new TreeView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                HideSelection = false,
                ShowLines = true,
                ShowRootLines = true,
                FullRowSelect = false,
                Font = new Font("Microsoft YaHei UI", 10F)
            };
            BuildTree();
            _tree.NodeMouseDoubleClick += Tree_NodeMouseDoubleClick;

            var hint = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                Text = "双击带页码的章节可跳回原文；导出按钮在底部",
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.DimGray,
                Padding = new Padding(6, 0, 0, 0)
            };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(6, 7, 6, 7),
                WrapContents = false
            };
            buttons.Controls.Add(CreateButton("关闭", (_, _) => Close()));
            buttons.Controls.Add(CreateButton("导出思维导图 PNG…", (_, _) => ExportMindMap()));
            buttons.Controls.Add(CreateButton("导出 OPML…", (_, _) => ExportOpml()));
            buttons.Controls.Add(CreateButton("导出 Markdown…", (_, _) => ExportMarkdown()));

            var header = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                Text = string.IsNullOrWhiteSpace(_model.Title) ? documentName : _model.Title,
                Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };

            Controls.Add(_tree);
            Controls.Add(hint);
            Controls.Add(buttons);
            Controls.Add(header);
        }

        private static Button CreateButton(string text, EventHandler onClick)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                MinimumSize = new Size(96, 30),
                Margin = new Padding(6, 0, 0, 0)
            };
            button.Click += onClick;
            return button;
        }

        private void BuildTree()
        {
            var root = new TreeNode(_model.Title.Length == 0 ? "文档大纲" : _model.Title)
            {
                Tag = 0
            };

            foreach (var entry in _model.Entries)
            {
                var node = new TreeNode(entry.DisplayText) { Tag = entry.PageNumber };
                foreach (var point in entry.Points)
                    node.Nodes.Add(new TreeNode(point));
                root.Nodes.Add(node);
            }

            _tree.BeginUpdate();
            _tree.Nodes.Add(root);
            _tree.EndUpdate();
            root.Expand();
        }

        private void Tree_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node?.Tag is not int pageNumber || pageNumber <= 0) return;
            _navigateToPage?.Invoke(pageNumber);
        }

        private void ExportMarkdown()
        {
            using var dialog = CreateSaveDialog("Markdown 文件|*.md", ".md");
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                File.WriteAllText(dialog.FileName, BuildMarkdown(), Encoding.UTF8);
                MessageBox.Show(this, "已导出 Markdown 大纲。", "导出成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"导出失败：{ex.Message}", "导出失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ExportOpml()
        {
            using var dialog = CreateSaveDialog("OPML 文件|*.opml", ".opml");
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                File.WriteAllText(dialog.FileName, BuildOpml(), Encoding.UTF8);
                MessageBox.Show(this, "已导出 OPML，可在思维导图工具中打开。", "导出成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"导出失败：{ex.Message}", "导出失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ExportMindMap()
        {
            using var dialog = CreateSaveDialog("PNG 图片|*.png", ".png");
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                using var bitmap = RenderMindMap();
                bitmap.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                MessageBox.Show(this, "已导出思维导图 PNG。", "导出成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"导出失败：{ex.Message}", "导出失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private SaveFileDialog CreateSaveDialog(string filter, string extension)
        {
            var safeName = string.IsNullOrWhiteSpace(_documentName) ? "文档大纲" : _documentName + " 大纲";
            return new SaveFileDialog
            {
                Filter = filter,
                DefaultExt = extension,
                FileName = safeName,
                InitialDirectory = AppPaths.ExportsDir,
                AddExtension = true,
                OverwritePrompt = true
            };
        }

        private string BuildMarkdown()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"# {(_model.Title.Length == 0 ? _documentName : _model.Title)}").AppendLine();

            int index = 1;
            foreach (var entry in _model.Entries)
            {
                var pageSuffix = entry.PageNumber > 0 ? $"（第 {entry.PageNumber} 页）" : string.Empty;
                builder.AppendLine($"## {index}. {entry.Title}{pageSuffix}");
                foreach (var point in entry.Points)
                    builder.AppendLine($"- {point}");
                builder.AppendLine();
                index++;
            }

            return builder.ToString().TrimEnd();
        }

        private string BuildOpml()
        {
            var builder = new StringBuilder();
            builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            builder.AppendLine("<opml version=\"2.0\">");
            builder.Append("  <head><title>");
            builder.Append(WebUtility.HtmlEncode(_model.Title.Length == 0 ? _documentName : _model.Title));
            builder.AppendLine("</title></head>");
            builder.AppendLine("  <body>");
            builder.Append("    <outline text=\"");
            builder.Append(WebUtility.HtmlEncode(_model.Title.Length == 0 ? _documentName : _model.Title));
            builder.AppendLine("\">");

            foreach (var entry in _model.Entries)
            {
                builder.Append("      <outline text=\"");
                builder.Append(WebUtility.HtmlEncode(entry.DisplayText));
                builder.Append('"');
                if (entry.PageNumber > 0)
                {
                    builder.Append(" page=\"");
                    builder.Append(entry.PageNumber);
                    builder.Append('"');
                }

                if (entry.Points.Count == 0)
                {
                    builder.AppendLine("/>");
                    continue;
                }

                builder.AppendLine(">");
                foreach (var point in entry.Points)
                {
                    builder.Append("        <outline text=\"");
                    builder.Append(WebUtility.HtmlEncode(point));
                    builder.AppendLine("\"/>");
                }
                builder.AppendLine("      </outline>");
            }

            builder.AppendLine("    </outline>");
            builder.AppendLine("  </body>");
            builder.AppendLine("</opml>");
            return builder.ToString();
        }

        /// <summary>把大纲画成左根右叶的思维导图：根 → 章节 → 要点，连线用折线。</summary>
        private Bitmap RenderMindMap()
        {
            const int RootWidth = 190;
            const int SectionWidth = 230;
            const int PointWidth = 260;
            const int RootX = 24;
            const int SectionX = RootX + RootWidth + 56;
            const int PointX = SectionX + SectionWidth + 48;
            const int Margin = 28;
            const int SectionGap = 18;

            using var measureSurface = new Bitmap(1, 1);
            using var measureGraphics = Graphics.FromImage(measureSurface);
            var titleFont = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
            var bodyFont = new Font("Microsoft YaHei UI", 9.5F);
            try
            {
                // 章节高度按要点数量累加，保证每个要点都有独立一行
                int sectionHeights = _model.Entries
                    .Sum(e => 34 + e.Points.Count * 24);
                int contentHeight = sectionHeights + Math.Max(0, _model.Entries.Count - 1) * SectionGap;
                int height = Math.Max(320, contentHeight + Margin * 2);
                int width = PointX + PointWidth + Margin;

                var bitmap = new Bitmap(width, height);
                using var graphics = Graphics.FromImage(bitmap);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                graphics.Clear(Color.White);

                int rootCenterY = height / 2;
                var rootRect = new Rectangle(RootX, rootCenterY - 34, RootWidth, 68);
                DrawNode(graphics, rootRect, _model.Title.Length == 0 ? _documentName : _model.Title,
                    Color.FromArgb(79, 70, 229), Color.White, titleFont);

                int cursorY = Margin;
                var rootPoint = new Point(rootRect.Right, rootCenterY);
                foreach (var entry in _model.Entries)
                {
                    int sectionHeight = 34 + entry.Points.Count * 24;
                    var sectionRect = new Rectangle(SectionX, cursorY, SectionWidth, 30);
                    DrawNode(graphics, sectionRect, entry.DisplayText,
                        Color.FromArgb(224, 231, 255), Color.FromArgb(30, 41, 59), bodyFont);

                    var sectionAnchor = new Point(sectionRect.Left, sectionRect.Top + sectionRect.Height / 2);
                    using var sectionPen = new Pen(Color.FromArgb(129, 140, 248), 1.5f);
                    graphics.DrawLine(sectionPen,
                        rootPoint.X + 8, rootPoint.Y,
                        sectionAnchor.X - 8, sectionAnchor.Y);

                    int pointY = sectionRect.Bottom + 4;
                    foreach (var point in entry.Points)
                    {
                        var pointRect = new Rectangle(PointX, pointY, PointWidth, 22);
                        using var pointBrush = new SolidBrush(Color.FromArgb(248, 250, 252));
                        graphics.FillRectangle(pointBrush, pointRect);
                        using var pointPen = new Pen(Color.FromArgb(203, 213, 225), 1f);
                        graphics.DrawRectangle(pointPen, pointRect);
                        using var pointFormat = new StringFormat
                        {
                            Trimming = StringTrimming.EllipsisCharacter,
                            FormatFlags = StringFormatFlags.NoWrap
                        };
                        graphics.DrawString(point, bodyFont, Brushes.DimGray,
                            new RectangleF(pointRect.X + 6, pointRect.Y + 3, pointRect.Width - 12, pointRect.Height - 4),
                            pointFormat);

                        using var leafPen = new Pen(Color.FromArgb(203, 213, 225), 1f);
                        graphics.DrawLine(leafPen,
                            sectionAnchor.X + 12, sectionAnchor.Y,
                            pointRect.Left - 6, pointRect.Top + pointRect.Height / 2);
                        pointY += 24;
                    }

                    // 章节折线：从根节点水平到章节列，保证长列表不遮挡
                    using var elbowPen = new Pen(Color.FromArgb(129, 140, 248), 1f);
                    int elbowY = sectionAnchor.Y;
                    graphics.DrawLine(elbowPen, rootRect.Right - 8, rootCenterY, rootRect.Right + 8, rootCenterY);
                    graphics.DrawLine(elbowPen, rootRect.Right + 8, rootCenterY, rootRect.Right + 8, elbowY);

                    cursorY = sectionRect.Top + Math.Max(sectionHeight, 30) + SectionGap;
                }

                return bitmap;
            }
            finally
            {
                titleFont.Dispose();
                bodyFont.Dispose();
            }
        }

        private static void DrawNode(Graphics graphics, Rectangle rect, string text,
            Color background, Color foreground, Font font)
        {
            using var path = CreateRoundedRect(rect, 8);
            using var brush = new SolidBrush(background);
            graphics.FillPath(brush, path);
            using var format = new StringFormat
            {
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap,
                LineAlignment = StringAlignment.Center
            };
            using var textBrush = new SolidBrush(foreground);
            graphics.DrawString(text, font, textBrush,
                new RectangleF(rect.X + 8, rect.Y + 2, rect.Width - 16, rect.Height - 4), format);
        }

        private static GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
