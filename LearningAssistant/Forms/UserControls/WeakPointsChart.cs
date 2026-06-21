using LearningAssistant.Common.UI;
using LearningAssistant.Services.Learning;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls
{
    public class WeakPointsChart : UserControl
    {
        private List<WeakPointAnalysis> _weakPoints = new List<WeakPointAnalysis>();
        private int _borderRadius = 12;
        private string _title = "薄弱点分析";

        [Category("数据")]
        [Description("薄弱点数据")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<WeakPointAnalysis> WeakPoints
        {
            get => _weakPoints;
            set
            {
                _weakPoints = value ?? new List<WeakPointAnalysis>();
                Invalidate();
            }
        }

        [Category("外观")]
        [Description("图表标题")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                Invalidate();
            }
        }

        [Category("外观")]
        [Description("圆角半径")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int BorderRadius
        {
            get => _borderRadius;
            set
            {
                _borderRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        public WeakPointsChart()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint
                   | ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Size = new Size(400, 300);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            var rect = ClientRectangle;
            var padding = 10;
            var contentRect = new Rectangle(
                rect.X + padding,
                rect.Y + padding,
                rect.Width - padding * 2,
                rect.Height - padding * 2
            );

            using var cardPath = GdiHelper.CreateRoundedRectPath(contentRect, _borderRadius);
            using var bgBrush = new SolidBrush(Color.White);
            g.FillPath(bgBrush, cardPath);

            using var borderPen = new Pen(Color.FromArgb(30, 0, 0, 0), 1);
            g.DrawPath(borderPen, cardPath);

            var titleRect = new RectangleF(
                contentRect.X + 20,
                contentRect.Y + 15,
                contentRect.Width - 40,
                28
            );
            using var titleFont = new Font("微软雅黑", 12F, FontStyle.Bold);
            g.DrawString(_title, titleFont,
                new SolidBrush(Color.FromArgb(33, 33, 33)),
                titleRect);

            var chartArea = new Rectangle(
                contentRect.X + 110,
                contentRect.Y + 55,
                Math.Min(contentRect.Width - 130, contentRect.Height - 80),
                Math.Min(contentRect.Width - 130, contentRect.Height - 80)
            );

            if (_weakPoints.Count == 0)
            {
                DrawEmptyState(g, chartArea);
            }
            else
            {
                DrawRadarChart(g, chartArea);
            }

            DrawLegend(g, contentRect);
        }

        private void DrawRadarChart(Graphics g, Rectangle chartArea)
        {
            var centerX = chartArea.X + chartArea.Width / 2f;
            var centerY = chartArea.Y + chartArea.Height / 2f;
            var radius = Math.Min(chartArea.Width, chartArea.Height) / 2f - 10;

            var displayPoints = _weakPoints.Take(6).ToList();
            int count = displayPoints.Count;
            if (count < 3)
            {
                var fillers = new[] { "拓展", "综合", "应用" };
                for (int i = count; i < 3; i++)
                {
                    displayPoints.Add(new WeakPointAnalysis
                    {
                        Category = fillers[i - count],
                        CategoryName = fillers[i - count],
                        Severity = 0.1,
                        Icon = "📌"
                    });
                }
                count = displayPoints.Count;
            }

            for (int level = 1; level <= 4; level++)
            {
                var levelRadius = radius * level / 4;
                var levelPoints = new List<PointF>();

                for (int i = 0; i < count; i++)
                {
                    var angle = 2 * Math.PI * i / count - Math.PI / 2;
                    var x = centerX + levelRadius * (float)Math.Cos(angle);
                    var y = centerY + levelRadius * (float)Math.Sin(angle);
                    levelPoints.Add(new PointF(x, y));
                }

                if (levelPoints.Count >= 3)
                {
                    using var levelPen = new Pen(Color.FromArgb(25, 0, 0, 0), 1);
                    g.DrawPolygon(levelPen, levelPoints.ToArray());
                }
            }

            for (int i = 0; i < count; i++)
            {
                var angle = 2 * Math.PI * i / count - Math.PI / 2;
                var x = centerX + radius * (float)Math.Cos(angle);
                var y = centerY + radius * (float)Math.Sin(angle);

                using var axisPen = new Pen(Color.FromArgb(20, 0, 0, 0), 1);
                g.DrawLine(axisPen, centerX, centerY, x, y);

                var labelRadius = radius + 20;
                var labelX = centerX + labelRadius * (float)Math.Cos(angle);
                var labelY = centerY + labelRadius * (float)Math.Sin(angle);

                var point = displayPoints[i];
                var labelText = point.CategoryName;
                using var labelFont = new Font("微软雅黑", 8F);
                var labelSize = g.MeasureString(labelText, labelFont);

                float drawX = labelX - labelSize.Width / 2;
                float drawY = labelY - labelSize.Height / 2;

                g.DrawString(labelText, labelFont,
                    new SolidBrush(Color.FromArgb(100, 100, 100)),
                    drawX, drawY);
            }

            var dataPoints = new List<PointF>();
            for (int i = 0; i < count; i++)
            {
                var angle = 2 * Math.PI * i / count - Math.PI / 2;
                var severity = (float)Math.Clamp(displayPoints[i].Severity, 0, 1);
                var dataRadius = radius * severity;
                var x = centerX + dataRadius * (float)Math.Cos(angle);
                var y = centerY + dataRadius * (float)Math.Sin(angle);
                dataPoints.Add(new PointF(x, y));
            }

            if (dataPoints.Count >= 3)
            {
                using var fillBrush = new SolidBrush(Color.FromArgb(60, 255, 87, 34));
                g.FillPolygon(fillBrush, dataPoints.ToArray());

                using var linePen = new Pen(Color.FromArgb(255, 87, 34), 2);
                g.DrawPolygon(linePen, dataPoints.ToArray());

                foreach (var point in dataPoints)
                {
                    using var dotBrush = new SolidBrush(Color.White);
                    g.FillEllipse(dotBrush, point.X - 4, point.Y - 4, 8, 8);
                    using var dotPen = new Pen(Color.FromArgb(255, 87, 34), 2);
                    g.DrawEllipse(dotPen, point.X - 4, point.Y - 4, 8, 8);
                }
            }
        }

        private void DrawLegend(Graphics g, Rectangle contentRect)
        {
            var legendX = contentRect.X + 20;
            var legendY = contentRect.Y + 55;
            var displayPoints = _weakPoints.Take(5).ToList();

            using var legendTitleFont = new Font("微软雅黑", 9F, FontStyle.Bold);
            g.DrawString("TOP 薄弱点", legendTitleFont,
                new SolidBrush(Color.FromArgb(80, 80, 80)),
                legendX, legendY);

            legendY += 25;

            using var itemFont = new Font("微软雅黑", 8.5F);
            for (int i = 0; i < displayPoints.Count && i < 5; i++)
            {
                var point = displayPoints[i];
                var percentText = $"{(int)(point.Severity * 100)}%";
                var barWidth = 80;
                var barHeight = 6;
                var barX = legendX + 70;
                var barY = legendY + 5;

                using var bgBarBrush = new SolidBrush(Color.FromArgb(20, 0, 0, 0));
                g.FillRectangle(bgBarBrush, barX, barY, barWidth, barHeight);

                var fillWidth = (int)(barWidth * Math.Clamp(point.Severity, 0, 1));
                var barColor = point.Severity >= 0.7 ? Color.FromArgb(255, 87, 34) :
                               point.Severity >= 0.4 ? Color.FromArgb(255, 152, 0) :
                               Color.FromArgb(76, 175, 80);
                using var fillBrush = new SolidBrush(barColor);
                g.FillRectangle(fillBrush, barX, barY, fillWidth, barHeight);

                var iconAndName = $"{point.Icon} {point.CategoryName}";
                g.DrawString(iconAndName, itemFont,
                    new SolidBrush(Color.FromArgb(60, 60, 60)),
                    legendX, legendY);

                g.DrawString(percentText, itemFont,
                    new SolidBrush(barColor),
                    barX + barWidth + 5, legendY - 1);

                legendY += 28;
            }
        }

        private void DrawEmptyState(Graphics g, Rectangle chartArea)
        {
            var centerX = chartArea.X + chartArea.Width / 2f;
            var centerY = chartArea.Y + chartArea.Height / 2f;

            using var iconFont = new Font("Segoe UI Emoji", 36F);
            var icon = "📊";
            var iconSize = g.MeasureString(icon, iconFont);
            g.DrawString(icon, iconFont,
                new SolidBrush(Color.FromArgb(180, 180, 180)),
                centerX - iconSize.Width / 2,
                centerY - iconSize.Height / 2 - 15);

            using var textFont = new Font("微软雅黑", 10F);
            var text = "暂无数据";
            var textSize = g.MeasureString(text, textFont);
            g.DrawString(text, textFont,
                new SolidBrush(Color.FromArgb(150, 150, 150)),
                centerX - textSize.Width / 2,
                centerY + iconSize.Height / 2 - 5);
        }
    }
}
