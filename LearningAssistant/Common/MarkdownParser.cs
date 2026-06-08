using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace LearningAssistant.Common
{
    public static class MarkdownParser
    {
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
                richTextBox.SelectionFont = new System.Drawing.Font("Microsoft YaHei", 16, System.Drawing.FontStyle.Bold);
                richTextBox.SelectionColor = System.Drawing.Color.Black;
                richTextBox.AppendText(line.Substring(2));
                return;
            }

            if (line.StartsWith("## "))
            {
                richTextBox.SelectionFont = new System.Drawing.Font("Microsoft YaHei", 14, System.Drawing.FontStyle.Bold);
                richTextBox.SelectionColor = System.Drawing.Color.Black;
                richTextBox.AppendText(line.Substring(3));
                return;
            }

            if (line.StartsWith("### "))
            {
                richTextBox.SelectionFont = new System.Drawing.Font("Microsoft YaHei", 12, System.Drawing.FontStyle.Bold);
                richTextBox.SelectionColor = System.Drawing.Color.Black;
                richTextBox.AppendText(line.Substring(4));
                return;
            }

            if (line.StartsWith("- "))
            {
                richTextBox.SelectionFont = new System.Drawing.Font("Microsoft YaHei", 10, System.Drawing.FontStyle.Regular);
                richTextBox.SelectionColor = System.Drawing.Color.Black;
                richTextBox.AppendText("• ");
                ParseInlineMarkdown(richTextBox, line.Substring(2));
                return;
            }

            if (line.StartsWith("* "))
            {
                richTextBox.SelectionFont = new System.Drawing.Font("Microsoft YaHei", 10, System.Drawing.FontStyle.Regular);
                richTextBox.SelectionColor = System.Drawing.Color.Black;
                richTextBox.AppendText("• ");
                ParseInlineMarkdown(richTextBox, line.Substring(2));
                return;
            }

            if (line.StartsWith("1. "))
            {
                richTextBox.SelectionFont = new System.Drawing.Font("Microsoft YaHei", 10, System.Drawing.FontStyle.Regular);
                richTextBox.SelectionColor = System.Drawing.Color.Black;
                richTextBox.AppendText(line.Substring(0, 3));
                ParseInlineMarkdown(richTextBox, line.Substring(3));
                return;
            }

            ParseInlineMarkdown(richTextBox, line);
        }

        private static void ParseInlineMarkdown(RichTextBox richTextBox, string text)
        {
            var defaultFont = new System.Drawing.Font("Microsoft YaHei", 10, System.Drawing.FontStyle.Regular);
            var boldFont = new System.Drawing.Font("Microsoft YaHei", 10, System.Drawing.FontStyle.Bold);
            var italicFont = new System.Drawing.Font("Microsoft YaHei", 10, System.Drawing.FontStyle.Italic);
            var codeFont = new System.Drawing.Font("Consolas", 10, System.Drawing.FontStyle.Regular);

            var pattern = @"\*\*(.+?)\*\*|\*(.+?)\*|`(.+?)`|(.+?)(?=\*\*|\*|`|$)";
            var matches = Regex.Matches(text, pattern);

            foreach (Match match in matches)
            {
                if (match.Groups[1].Success)
                {
                    richTextBox.SelectionFont = boldFont;
                    richTextBox.SelectionColor = System.Drawing.Color.Black;
                    richTextBox.AppendText(match.Groups[1].Value);
                }
                else if (match.Groups[2].Success)
                {
                    richTextBox.SelectionFont = italicFont;
                    richTextBox.SelectionColor = System.Drawing.Color.Black;
                    richTextBox.AppendText(match.Groups[2].Value);
                }
                else if (match.Groups[3].Success)
                {
                    richTextBox.SelectionFont = codeFont;
                    richTextBox.SelectionColor = System.Drawing.Color.DarkBlue;
                    richTextBox.SelectionBackColor = System.Drawing.Color.LightGray;
                    richTextBox.AppendText(match.Groups[3].Value);
                    richTextBox.SelectionBackColor = System.Drawing.Color.White;
                }
                else if (match.Groups[4].Success)
                {
                    richTextBox.SelectionFont = defaultFont;
                    richTextBox.SelectionColor = System.Drawing.Color.Black;
                    richTextBox.AppendText(match.Groups[4].Value);
                }
            }
        }
    }
}