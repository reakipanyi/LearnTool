using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using ScottPlot;

namespace LearningAssistant.Forms.UserControls.Charts
{
    /// <summary>
    /// 学习热力图（03 方案 3.2 新增）：以"周 × 星期"矩阵展示学习强度，
    /// 颜色深浅 = 当日学习量，替代旧的 Bitmap 热力图。数据来自聚合 DTO <see cref="TrendSeries"/>。
    /// </summary>
    public class WeeklyHeatmapChart : LearningChartBase
    {
        /// <summary>展示的周数（含本周）</summary>
        protected int Weeks = 6;

        /// <summary>低强度（空）对应的热力颜色——由主题派生，避免色盲下难分辨</summary>
        protected Color LowIntensityColor = Color.FromArgb(232, 237, 245);
        /// <summary>高强度对应的热力颜色</summary>
        protected Color HighIntensityColor = Color.FromArgb(41, 98, 255);

        public WeeklyHeatmapChart()
        {
            ApplyAxisStyle("学习热力图", null, null);
        }

        public override void ApplyTheme(ThemeColors colors)
        {
            base.ApplyTheme(colors);
            LowIntensityColor = ColorBlend(colors.Surface, colors.Primary, 0.12f);
            HighIntensityColor = colors.Primary;
        }

        private static Color ColorBlend(Color a, Color b, float t)
        {
            int r = (int)(a.R + (b.R - a.R) * t);
            int g = (int)(a.G + (b.G - a.G) * t);
            int bl = (int)(a.B + (b.B - a.B) * t);
            return Color.FromArgb(r, g, bl);
        }

        /// <summary>
        /// 用学习日志序列刷新热力图：构建 以 end 为末尾的 Weeks 周矩阵（周一~周日）。
        /// 聚合口径与 02 底座一致：学习量 = ItemsStudied。
        /// </summary>
        public void UpdateData(TrendSeries series, DateTime endDate, double maxValue = 0)
        {
            var counts = (series?.Points ?? new List<TrendPoint>())
                .ToDictionary(p => p.Date.Date, p => p.Value);

            var rows = Math.Max(1, Weeks);
            var matrix = new double[rows, 7];
            var hasData = false;

            double max = maxValue;
            foreach (var v in counts.Values) max = Math.Max(max, v);

            // 逐日回填到"周×星期"
            var monday = StartOfWeek(endDate);
            for (int row = 0; row < rows; row++)
            {
                var weekStart = monday.AddDays(-(rows - 1 - row) * 7);
                for (int day = 0; day < 7; day++)
                {
                    var date = weekStart.AddDays(day);
                    if (counts.TryGetValue(date, out var val) && val > 0)
                    {
                        matrix[row, day] = max > 0 ? val / max : 0;
                        hasData = true;
                    }
                }
            }

            if (!hasData)
            {
                ShowEmptyState("本周暂无学习记录", "颜色越深代表当天学习越多");
                return;
            }

            // 重新按当前主题刷新背景
            ApplyAxisStyle();
            Plot.Clear();
            Plot.Title("学习热力图", fontName: "微软雅黑", color: TextColor);

            var map = Plot.AddHeatmap(matrix);
            map.Opacity = 0.9;

            // 单元格中心：0..cols / 0..rows 坐标，刻度置于 (i+0.5, j+0.5)
            var dayLabels = new[] { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
            var dayTicks = Enumerable.Range(0, 7).Select(i => i + 0.5).ToArray();
            Plot.XTicks(dayTicks, dayLabels);

            var weekTicks = Enumerable.Range(0, rows).Select(i => i + 0.5).ToArray();
            var weekLabels = Enumerable.Range(0, rows)
                .Select(row => monday.AddDays(-(rows - 1 - row) * 7).ToString("M/d"))
                .ToArray();
            Plot.YTicks(weekTicks, weekLabels);

            Plot.SetAxisLimits(0, 7.5, 0, rows + 0.5);
            Plot.Grid(false);
            Refresh();
        }

        private static DateTime StartOfWeek(DateTime date) =>
            date.Date.AddDays(-((int)date.DayOfWeek + 6) % 7);
    }
}