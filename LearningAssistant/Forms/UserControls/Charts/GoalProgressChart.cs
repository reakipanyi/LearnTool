using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using ScottPlot;

namespace LearningAssistant.Forms.UserControls.Charts
{
    /// <summary>
    /// 目标达成进度图（03 方案 3.2 新增）：展示"今日目标 / 周目标"完成率，
    /// 以进度条 + 目标参考线形式呈现，数据来自聚合 DTO（<see cref="Models.Learning.PeriodOverview"/> 或直接进度值）。
    /// </summary>
    public class GoalProgressChart : LearningChartBase
    {
        public GoalProgressChart()
        {
            ApplyAxisStyle("学习目标进度", null, "完成率 (%)");
        }

        /// <summary>
        /// 绘制目标达成进度条。
        /// </summary>
        /// <param name="title">图表标题，如 "今日目标"</param>
        /// <param name="completed">已完成量</param>
        /// <param name="target">目标量（&gt;0）</param>
        /// <param name="unit">单位，如 "项"/"分钟"</param>
        public void UpdateData(string title, double completed, double target, string unit = "项")
        {
            if (target <= 0)
            {
                ShowEmptyState("暂未设置目标", "在设置中设定每日/每周目标后展示达成进度");
                return;
            }

            var rate = Math.Max(0, Math.Min(100, completed / target * 100));

            Plot.Clear();
            ApplyAxisStyle(title ?? "学习目标进度", "目标", "完成率 (%)");

            // 目标参考线（100%）
            var targetLine = Plot.AddHorizontalLine(100);
            targetLine.Color = TextSecondaryColor;
            targetLine.LineStyle = LineStyle.Dash;

            // 已达成量：分成 已达成(绿) / 目标(灰底) 两段，直观看到缺口
            var achievedBar = Plot.AddBar(rate, 0);
            achievedBar.FillColor = Series3;
            achievedBar.BorderColor = Series3;

            var gapBar = Plot.AddBar(100 - rate, 0.15);
            gapBar.FillColor = ColorBlend(ChartBackground, GridColor, 0.6f);
            gapBar.BorderColor = Color.Transparent;

            Plot.XTicks(new[] { 0.075 }, new[] { $"{completed:F0}/{target:F0} {unit}" });
            Plot.SetAxisLimitsY(0, 130);

            // 顶部的完成率文本
            var valueText = Plot.AddText($"{rate:F0}%", 0.075, 112, 14, color: TextColor);
            _ = valueText;

            Refresh();
        }

        /// <summary>按周期概览 DTO 的进度口径绘制"今日目标"（ItemsStudied / 目标项数）</summary>
        public void UpdateData(Models.Learning.DailyOverview overview, int targetItems, string title = "今日目标")
        {
            if (overview == null)
            {
                ShowEmptyState("暂无目标进度数据");
                return;
            }
            UpdateData(title, overview.ItemsStudied, targetItems);
        }

        private static Color ColorBlend(Color a, Color b, float t)
        {
            int r = (int)(a.R + (b.R - a.R) * t);
            int g = (int)(a.G + (b.G - a.G) * t);
            int bl = (int)(a.B + (b.B - a.B) * t);
            return Color.FromArgb(r, g, bl);
        }
    }
}