using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace LearningAssistant.Common
{
    public static class MarkdownParser
    {
        private static readonly Font H1Font = new Font("Microsoft YaHei", 16, FontStyle.Bold);
        private static readonly Font H2Font = new Font("Microsoft YaHei", 14, FontStyle.Bold);
        private static readonly Font H3Font = new Font("Microsoft YaHei", 12, FontStyle.Bold);
        private static readonly Font DefaultFont = new Font("Microsoft YaHei", 10, FontStyle.Regular);
        private static readonly Font BoldFont = new Font("Microsoft YaHei", 10, FontStyle.Bold);
        private static readonly Font ItalicFont = new Font("Microsoft YaHei", 10, FontStyle.Italic);
        private static readonly Font CodeFont = new Font("Consolas", 10, FontStyle.Regular);

        public static void ParseMarkdownToRichTextBox(RichTextBox richTextBox, string markdown)
        {
            if (richTextBox == null) throw new ArgumentNullException(nameof(richTextBox));
            if (markdown == null) markdown = string.Empty;

            richTextBox.Clear();
            richTextBox.SelectionStart = 0;

            var lines = markdown.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            
            foreach (var line in lines)
            {
                ParseLine(richTextBox, line);
                richTextBox.AppendText(Environment.NewLine);
            }
        }

        private static void ParseLine(RichTextBox richTextBox, string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            if (line.StartsWith("# "))
            {
                richTextBox.SelectionFont = H1Font;
                richTextBox.SelectionColor = Color.Black;
                richTextBox.AppendText(line.Substring(2));
                return;
            }

            if (line.StartsWith("## "))
            {
                richTextBox.SelectionFont = H2Font;
                richTextBox.SelectionColor = Color.Black;
                richTextBox.AppendText(line.Substring(3));
                return;
            }

            if (line.StartsWith("### "))
            {
                richTextBox.SelectionFont = H3Font;
                richTextBox.SelectionColor = Color.Black;
                richTextBox.AppendText(line.Substring(4));
                return;
            }

            if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                richTextBox.SelectionFont = DefaultFont;
                richTextBox.SelectionColor = Color.Black;
                richTextBox.AppendText("• ");
                ParseInlineMarkdown(richTextBox, line.Substring(2));
                return;
            }

            if (line.StartsWith("1. "))
            {
                richTextBox.SelectionFont = DefaultFont;
                richTextBox.SelectionColor = Color.Black;
                richTextBox.AppendText(line.Substring(0, 3));
                ParseInlineMarkdown(richTextBox, line.Substring(3));
                return;
            }

            ParseInlineMarkdown(richTextBox, line);
        }

        private static void ParseInlineMarkdown(RichTextBox richTextBox, string text)
        {
            var pattern = @"\*\*(.+?)\*\*|\*(.+?)\*|`(.+?)`|(.+?)(?=\*\*|\*|`|$)";
            var matches = Regex.Matches(text, pattern);

            foreach (Match match in matches)
            {
                if (match.Groups[1].Success)
                {
                    richTextBox.SelectionFont = BoldFont;
                    richTextBox.SelectionColor = Color.Black;
                    richTextBox.AppendText(match.Groups[1].Value);
                }
                else if (match.Groups[2].Success)
                {
                    richTextBox.SelectionFont = ItalicFont;
                    richTextBox.SelectionColor = Color.Black;
                    richTextBox.AppendText(match.Groups[2].Value);
                }
                else if (match.Groups[3].Success)
                {
                    richTextBox.SelectionFont = CodeFont;
                    richTextBox.SelectionColor = Color.DarkBlue;
                    richTextBox.SelectionBackColor = Color.LightGray;
                    richTextBox.AppendText(match.Groups[3].Value);
                    richTextBox.SelectionBackColor = Color.White;
                }
                else if (match.Groups[4].Success)
                {
                    richTextBox.SelectionFont = DefaultFont;
                    richTextBox.SelectionColor = Color.Black;
                    richTextBox.AppendText(match.Groups[4].Value);
                }
            }
        }
    }
}