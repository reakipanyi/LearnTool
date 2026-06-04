using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace UnifiedLearningAssistant.Services.Learning
{
    public interface ILearningChartService
    {
        Bitmap GenerateDailyTrendChart(List<DailyStatistics> data, int width = 800, int height = 400);
        Bitmap GenerateCategoryPieChart(Dictionary<string, int> categoryData, int width = 400, int height = 400);
        Bitmap GenerateProgressBar(double progress, string label, int width = 400, int height = 60);
        Bitmap GenerateStreakVisualization(int streak, int width = 500, int height = 80);
        Bitmap GenerateWeeklyHeatmap(int[,] weeklyData, int width = 500, int height = 100);
    }

    public class LearningChartService : ILearningChartService
    {
        private readonly ILogger<LearningChartService>? _logger;
        private readonly Color[] _chartColors = new[]
        {
            Color.FromArgb(66, 133, 244), // Blue
            Color.FromArgb(52, 168, 83),  // Green
            Color.FromArgb(219, 68, 55),  // Red
            Color.FromArgb(244, 180, 0),  // Yellow
            Color.FromArgb(156, 39, 176), // Purple
            Color.FromArgb(255, 152, 0),  // Orange
            Color.FromArgb(0, 188, 212),  // Cyan
            Color.FromArgb(233, 30, 99)   // Pink
        };

        public LearningChartService(ILogger<LearningChartService>? logger = null)
        {
            _logger = logger;
        }

        public Bitmap GenerateDailyTrendChart(List<DailyStatistics> data, int width, int height)
        {
            var bitmap = new Bitmap(width, height);
            using var g = Graphics.FromImage(bitmap);
            
            try
            {
                g.Clear(Color.White);
                
                if (data == null || data.Count == 0)
                {
                    DrawNoData(g, width, height);
                    return bitmap;
                }

                var padding = 50;
                var chartWidth = width - padding * 2;
                var chartHeight = height - padding * 2;

                var maxMinutes = data.Max(d => d.TotalMinutes);
                var maxItems = data.Max(d => d.TotalItems);
                
                if (maxMinutes == 0) maxMinutes = 1;
                if (maxItems == 0) maxItems = 1;

                DrawGrid(g, data.Count, maxMinutes, padding, chartWidth, chartHeight);

                var pointsMinutes = new List<PointF>();
                var pointsItems = new List<PointF>();

                for (int i = 0; i < data.Count; i++)
                {
                    var x = padding + (i * chartWidth) / (data.Count - 1);
                    var yMinutes = padding + chartHeight - (data[i].TotalMinutes * chartHeight) / maxMinutes;
                    var yItems = padding + chartHeight - (data[i].TotalItems * chartHeight) / maxItems;

                    pointsMinutes.Add(new PointF(x, yMinutes));
                    pointsItems.Add(new PointF(x, yItems));

                    g.DrawString(data[i].Date.Day.ToString(), 
                        new Font("Arial", 8), Brushes.Gray, x - 8, height - 30);
                }

                DrawLine(g, pointsMinutes, _chartColors[0], "学习时长");
                DrawLine(g, pointsItems, _chartColors[1], "学习数量");

                DrawLegend(g, new[] { "学习时长", "学习数量" }, 
                    new[] { _chartColors[0], _chartColors[1] }, width - 150, 10);

                _logger?.LogDebug("生成每日趋势图成功");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成每日趋势图失败");
                DrawError(g, width, height, ex.Message);
            }

            return bitmap;
        }

        public Bitmap GenerateCategoryPieChart(Dictionary<string, int> categoryData, int width, int height)
        {
            var bitmap = new Bitmap(width, height);
            using var g = Graphics.FromImage(bitmap);
            
            try
            {
                g.Clear(Color.White);
                
                if (categoryData == null || categoryData.Count == 0)
                {
                    DrawNoData(g, width, height);
                    return bitmap;
                }

                var total = categoryData.Sum(kv => kv.Value);
                if (total == 0)
                {
                    DrawNoData(g, width, height);
                    return bitmap;
                }

                var centerX = width / 2;
                var centerY = height / 2;
                var radius = Math.Min(width, height) / 2 - 30;

                float startAngle = 0;
                int colorIndex = 0;

                foreach (var kvp in categoryData)
                {
                    var angle = (float)(kvp.Value * 360.0 / total);
                    var brush = new SolidBrush(_chartColors[colorIndex % _chartColors.Length]);
                    
                    g.FillPie(brush, centerX - radius, centerY - radius, 
                        radius * 2, radius * 2, startAngle, angle);

                    startAngle += angle;
                    colorIndex++;
                }

                DrawPieLegend(g, categoryData, width - 120, 20);

                _logger?.LogDebug("生成分类饼图成功");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成分类饼图失败");
                DrawError(g, width, height, ex.Message);
            }

            return bitmap;
        }

        public Bitmap GenerateProgressBar(double progress, string label, int width, int height)
        {
            var bitmap = new Bitmap(width, height);
            using var g = Graphics.FromImage(bitmap);
            
            try
            {
                g.Clear(Color.White);

                var barHeight = height - 20;
                var barWidth = width - 40;

                g.DrawRectangle(Pens.LightGray, 10, 10, barWidth, barHeight);

                var fillWidth = (int)(barWidth * Math.Min(progress, 1.0));
                var gradient = new LinearGradientBrush(
                    new Point(10, 10), 
                    new Point(10 + fillWidth, 10),
                    _chartColors[0], 
                    _chartColors[1]);
                g.FillRectangle(gradient, 10, 10, fillWidth, barHeight);

                var text = $"{label}: {Math.Round(progress * 100)}%";
                var font = new Font("Arial", 12, FontStyle.Bold);
                var textSize = g.MeasureString(text, font);
                g.DrawString(text, font, Brushes.Black, (width - textSize.Width) / 2, barHeight + 12);

                _logger?.LogDebug("生成进度条成功: {Progress}%", progress * 100);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成进度条失败");
                DrawError(g, width, height, ex.Message);
            }

            return bitmap;
        }

        public Bitmap GenerateStreakVisualization(int streak, int width, int height)
        {
            var bitmap = new Bitmap(width, height);
            using var g = Graphics.FromImage(bitmap);
            
            try
            {
                g.Clear(Color.White);

                var cellSize = (width - 40) / Math.Max(streak, 7);
                var gap = 5;
                var startX = 20;
                var startY = 10;

                for (int i = 0; i < Math.Max(streak, 7); i++)
                {
                    var x = startX + i * (cellSize + gap);
                    var isActive = i < streak;

                    var brush = isActive ? new SolidBrush(_chartColors[1]) : Brushes.LightGray;
                    g.FillRectangle(brush, x, startY, cellSize, cellSize);
                    g.DrawRectangle(Pens.Gray, x, startY, cellSize, cellSize);
                }

                var font = new Font("Arial", 14, FontStyle.Bold);
                var text = streak > 0 ? $"🔥 {streak} 天连续学习" : "💪 开始你的学习之旅吧！";
                var textSize = g.MeasureString(text, font);
                g.DrawString(text, font, Brushes.Black, (width - textSize.Width) / 2, startY + cellSize + 10);

                _logger?.LogDebug("生成连续学习可视化成功: {Streak} 天", streak);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成连续学习可视化失败");
                DrawError(g, width, height, ex.Message);
            }

            return bitmap;
        }

        public Bitmap GenerateWeeklyHeatmap(int[,] weeklyData, int width, int height)
        {
            var bitmap = new Bitmap(width, height);
            using var g = Graphics.FromImage(bitmap);
            
            try
            {
                g.Clear(Color.White);

                var cellWidth = (width - 60) / 7;
                var cellHeight = (height - 40) / 24;

                for (int hour = 0; hour < 24; hour++)
                {
                    for (int day = 0; day < 7; day++)
                    {
                        var x = 30 + day * cellWidth;
                        var y = 20 + hour * cellHeight;
                        var value = weeklyData[hour, day];

                        var intensity = Math.Min(value / 10.0, 1.0);
                        var color = GetHeatmapColor(intensity);

                        g.FillRectangle(new SolidBrush(color), x, y, cellWidth - 1, cellHeight - 1);
                    }
                }

                DrawHeatmapLabels(g, width, height, cellWidth, cellHeight);
                DrawHeatmapLegend(g, width - 50, height - 40);

                _logger?.LogDebug("生成周学习热力图成功");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成周学习热力图失败");
                DrawError(g, width, height, ex.Message);
            }

            return bitmap;
        }

        private void DrawGrid(Graphics g, int count, int maxValue, int padding, int chartWidth, int chartHeight)
        {
            var pen = new Pen(Color.LightGray);
            
            for (int i = 0; i <= 4; i++)
            {
                var y = padding + (i * chartHeight) / 4;
                g.DrawLine(pen, padding, y, padding + chartWidth, y);
                
                var label = ((maxValue * (4 - i)) / 4).ToString();
                g.DrawString(label, new Font("Arial", 8), Brushes.Gray, 5, y - 8);
            }

            for (int i = 0; i < count; i++)
            {
                var x = padding + (i * chartWidth) / (count - 1);
                g.DrawLine(pen, x, padding, x, padding + chartHeight);
            }
        }

        private void DrawLine(Graphics g, List<PointF> points, Color color, string label)
        {
            var pen = new Pen(color, 2);
            g.DrawLines(pen, points.ToArray());

            foreach (var point in points)
            {
                g.FillEllipse(new SolidBrush(color), point.X - 3, point.Y - 3, 6, 6);
            }
        }

        private void DrawLegend(Graphics g, string[] labels, Color[] colors, int x, int y)
        {
            for (int i = 0; i < labels.Length; i++)
            {
                g.FillRectangle(new SolidBrush(colors[i]), x, y + i * 20, 15, 10);
                g.DrawString(labels[i], new Font("Arial", 10), Brushes.Black, x + 20, y + i * 20);
            }
        }

        private void DrawPieLegend(Graphics g, Dictionary<string, int> data, int x, int y)
        {
            var total = data.Sum(kv => kv.Value);
            int i = 0;

            foreach (var kvp in data)
            {
                var percentage = Math.Round(kvp.Value * 100.0 / total, 1);
                g.FillRectangle(new SolidBrush(_chartColors[i % _chartColors.Length]), x, y + i * 25, 15, 10);
                g.DrawString($"{kvp.Key}: {percentage}%", new Font("Arial", 9), Brushes.Black, x + 20, y + i * 25);
                i++;
            }
        }

        private void DrawHeatmapLabels(Graphics g, int width, int height, int cellWidth, int cellHeight)
        {
            var days = new[] { "日", "一", "二", "三", "四", "五", "六" };
            var hours = new[] { "0", "6", "12", "18", "24" };

            for (int i = 0; i < 7; i++)
            {
                g.DrawString(days[i], new Font("Arial", 8), Brushes.Gray, 30 + i * cellWidth, height - 20);
            }

            for (int i = 0; i < 5; i++)
            {
                var hour = i * 6;
                g.DrawString(hour.ToString(), new Font("Arial", 8), Brushes.Gray, 5, 20 + hour * cellHeight);
            }
        }

        private void DrawHeatmapLegend(Graphics g, int x, int y)
        {
            for (int i = 0; i <= 4; i++)
            {
                var intensity = i / 4.0;
                var color = GetHeatmapColor(intensity);
                g.FillRectangle(new SolidBrush(color), x, y + i * 8, 15, 8);
            }
            
            g.DrawString("少", new Font("Arial", 8), Brushes.Gray, x + 20, y);
            g.DrawString("多", new Font("Arial", 8), Brushes.Gray, x + 20, y + 32);
        }

        private Color GetHeatmapColor(double intensity)
        {
            if (intensity < 0.2)
                return Color.LightGray;
            if (intensity < 0.4)
                return Color.LightBlue;
            if (intensity < 0.6)
                return Color.Yellow;
            if (intensity < 0.8)
                return Color.Orange;
            return Color.Red;
        }

        private void DrawNoData(Graphics g, int width, int height)
        {
            var font = new Font("Arial", 14);
            var text = "暂无数据";
            var size = g.MeasureString(text, font);
            g.DrawString(text, font, Brushes.Gray, (width - size.Width) / 2, (height - size.Height) / 2);
        }

        private void DrawError(Graphics g, int width, int height, string message)
        {
            g.Clear(Color.White);
            var font = new Font("Arial", 10);
            g.DrawString($"错误: {message}", font, Brushes.Red, 10, 10);
        }
    }
}