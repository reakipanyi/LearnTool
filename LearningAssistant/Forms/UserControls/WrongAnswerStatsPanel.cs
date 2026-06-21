using LearningAssistant.Common.UI;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls
{
    /// <summary>
    /// 错题统计面板
    /// </summary>
    public class WrongAnswerStatsPanel : UserControl
    {
        private IWrongAnswerService? _wrongAnswerService;
        private string? _currentUserId;
        private WrongAnswerStats? _stats;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IWrongAnswerService? WrongAnswerService
        {
            get => _wrongAnswerService;
            set
            {
                _wrongAnswerService = value;
                RefreshStats();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? CurrentUserId
        {
            get => _currentUserId;
            set
            {
                _currentUserId = value;
                RefreshStats();
            }
        }

        public WrongAnswerStatsPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint, true);

            BackColor = Color.White;
            Padding = new Padding(15);
            AutoScroll = true;
        }

        public void RefreshStats()
        {
            if (_wrongAnswerService == null || string.IsNullOrEmpty(_currentUserId))
            {
                _stats = null;
                Invalidate();
                return;
            }

            _stats = _wrongAnswerService.GetStatistics(_currentUserId);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            if (_stats == null) return;

            var rect = ClientRectangle;
            int y = 0;

            var titleLabelRect = new Rectangle(rect.X + 15, y, rect.Width - 30, 30);
            TextRenderer.DrawText(g, "📊 错题统计",
                new Font("微软雅黑", 14F, FontStyle.Bold),
                titleLabelRect,
                Color.FromArgb(33, 33, 33),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            y += 40;

            var summaryCards = new[]
            {
                ("总错题数", _stats.TotalCount.ToString(), Color.FromArgb(66, 165, 245)),
                ("未掌握", _stats.NotMasteredCount.ToString(), Color.FromArgb(239, 83, 80)),
                ("模糊", _stats.FuzzyCount.ToString(), Color.FromArgb(255, 152, 0)),
                ("已掌握", _stats.MasteredCount.ToString(), Color.FromArgb(76, 175, 80))
            };

            int cardWidth = (rect.Width - 30 - 30) / 4;
            int cardHeight = 70;
            for (int i = 0; i < summaryCards.Length; i++)
            {
                var cardRect = new Rectangle(
                    rect.X + 15 + i * (cardWidth + 10),
                    y,
                    cardWidth,
                    cardHeight
                );
                DrawStatCard(g, cardRect, summaryCards[i].Item1, summaryCards[i].Item2, summaryCards[i].Item3);
            }
            y += cardHeight + 15;

            var masteryTitleRect = new Rectangle(rect.X + 15, y, rect.Width - 30, 25);
            TextRenderer.DrawText(g, "📈 掌握程度分布",
                new Font("微软雅黑", 11F, FontStyle.Bold),
                masteryTitleRect,
                Color.FromArgb(60, 60, 60),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            y += 30;

            var barRect = new Rectangle(rect.X + 15, y, rect.Width - 30, 24);
            DrawMasteryBar(g, barRect, _stats);
            y += 35;

            var legendY = y;
            var legends = new[]
            {
                ("未掌握", _stats.NotMasteredCount, Color.FromArgb(239, 83, 80)),
                ("模糊", _stats.FuzzyCount, Color.FromArgb(255, 152, 0)),
                ("已掌握", _stats.MasteredCount, Color.FromArgb(76, 175, 80))
            };

            int legendX = rect.X + 15;
            foreach (var (name, count, color) in legends)
            {
                var colorRect = new Rectangle(legendX, y + 2, 12, 12);
                using var colorBrush = new SolidBrush(color);
                g.FillRectangle(colorBrush, colorRect);

                var textRect = new Rectangle(legendX + 18, y, 100, 16);
                TextRenderer.DrawText(g, $"{name}: {count}",
                    new Font("微软雅黑", 8F),
                    textRect,
                    Color.FromArgb(100, 100, 100),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                legendX += 110;
            }
            y += 25;

            if (_stats.SubjectStats.Count > 0)
            {
                y += 10;
                var subjectTitleRect = new Rectangle(rect.X + 15, y, rect.Width - 30, 25);
                TextRenderer.DrawText(g, "📚 学科分布",
                    new Font("微软雅黑", 11F, FontStyle.Bold),
                    subjectTitleRect,
                    Color.FromArgb(60, 60, 60),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                y += 30;

                int barMaxWidth = rect.Width - 140;
                var maxCount = _stats.SubjectStats.Values.DefaultIfEmpty(1).Max();

                foreach (var (subject, count) in _stats.SubjectStats.Take(6))
                {
                    var nameRect = new Rectangle(rect.X + 15, y, 110, 20);
                    TextRenderer.DrawText(g, subject,
                        new Font("微软雅黑", 8.5F),
                        nameRect,
                        Color.FromArgb(80, 80, 80),
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                    int barWidth = (int)((double)count / maxCount * barMaxWidth);
                    var barBgRect = new Rectangle(rect.X + 130, y + 4, barMaxWidth, 14);
                    using var bgBrush = new SolidBrush(Color.FromArgb(240, 240, 240));
                    g.FillRoundedRectangle(bgBrush, barBgRect, new Size(4, 4));

                    if (barWidth > 0)
                    {
                        var barFillRect = new Rectangle(rect.X + 130, y + 4, barWidth, 14);
                        using var fillBrush = new LinearGradientBrush(
                            barFillRect,
                            Color.FromArgb(129, 199, 132),
                            Color.FromArgb(76, 175, 80),
                            LinearGradientMode.Horizontal);
                        g.FillRoundedRectangle(fillBrush, barFillRect, new Size(4, 4));
                    }

                    var countRect = new Rectangle(rect.X + 130 + barMaxWidth + 8, y, 40, 20);
                    TextRenderer.DrawText(g, count.ToString(),
                        new Font("微软雅黑", 8.5F, FontStyle.Bold),
                        countRect,
                        Color.FromArgb(60, 60, 60),
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                    y += 22;
                }
            }

            y += 15;
            var masteryRateText = $"掌握率: {_stats.MasteryRate:0.#}%";
            var masteryRateRect = new Rectangle(rect.X + 15, y, rect.Width - 30, 22);
            TextRenderer.DrawText(g, masteryRateText,
                new Font("微软雅黑", 10F, FontStyle.Bold),
                masteryRateRect,
                Color.FromArgb(46, 125, 50),
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        }

        private static void DrawStatCard(Graphics g, Rectangle rect, string label, string value, Color color)
        {
            using var path = GdiHelper.CreateRoundedRectPath(rect, 8);
            using var bgBrush = new SolidBrush(Color.FromArgb(248, 249, 250));
            g.FillPath(bgBrush, path);

            var valueRect = new Rectangle(rect.X + 12, rect.Y + 10, rect.Width - 24, 30);
            TextRenderer.DrawText(g, value,
                new Font("微软雅黑", 18F, FontStyle.Bold),
                valueRect,
                color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            var labelRect = new Rectangle(rect.X + 12, rect.Y + 40, rect.Width - 24, 20);
            TextRenderer.DrawText(g, label,
                new Font("微软雅黑", 8.5F),
                labelRect,
                Color.FromArgb(120, 120, 120),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        private static void DrawMasteryBar(Graphics g, Rectangle rect, WrongAnswerStats stats)
        {
            if (stats.TotalCount == 0)
            {
                using var emptyBrush = new SolidBrush(Color.FromArgb(240, 240, 240));
                g.FillRoundedRectangle(emptyBrush, rect, new Size(6, 6));
                return;
            }

            double notMasteredRatio = (double)stats.NotMasteredCount / stats.TotalCount;
            double fuzzyRatio = (double)stats.FuzzyCount / stats.TotalCount;
            double masteredRatio = (double)stats.MasteredCount / stats.TotalCount;

            int x = rect.X;
            int notMasteredWidth = (int)(notMasteredRatio * rect.Width);
            int fuzzyWidth = (int)(fuzzyRatio * rect.Width);
            int masteredWidth = rect.Width - notMasteredWidth - fuzzyWidth;

            if (notMasteredWidth > 0)
            {
                var notRect = new Rectangle(x, rect.Y, notMasteredWidth, rect.Height);
                using var brush = new SolidBrush(Color.FromArgb(239, 83, 80));
                g.FillRoundedRectangle(brush, notRect, new Size(6, 6));
                x += notMasteredWidth;
            }

            if (fuzzyWidth > 0)
            {
                var fuzzyRect = new Rectangle(x, rect.Y, fuzzyWidth, rect.Height);
                using var brush = new SolidBrush(Color.FromArgb(255, 152, 0));
                g.FillRectangle(brush, fuzzyRect);
                x += fuzzyWidth;
            }

            if (masteredWidth > 0)
            {
                var masteredRect = new Rectangle(x, rect.Y, masteredWidth, rect.Height);
                using var brush = new SolidBrush(Color.FromArgb(76, 175, 80));
                g.FillRoundedRectangle(brush, masteredRect, new Size(6, 6));
            }
        }
    }
}
