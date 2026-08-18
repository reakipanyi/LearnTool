using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using ScottPlot;

namespace LearningAssistant.Forms.UserControls.Charts
{
    /// <summary>
    /// 记忆成熟度分布图（03 方案 3.2 新增）：基于 FSRS <see cref="MaturityDistribution"/>，
    /// 展示"新生 / 学习中 / 已掌握"三段分布，比单一保留率更直观。
    /// </summary>
    public class MemoryMaturityChart : LearningChartBase
    {
        public MemoryMaturityChart()
        {
            ApplyAxisStyle("记忆成熟度分布", "学习阶段", "词条数量");
        }

        /// <summary>消费统计底座的 <see cref="MaturityDistribution"/> 绘制三段柱状分布</summary>
        public void UpdateData(MaturityDistribution distribution)
        {
            if (distribution == null
                || (distribution.NewCount + distribution.LearningCount + distribution.MasteredCount) <= 0)
            {
                ShowEmptyState("暂无记忆数据", "开始背诵后会按 FSRS 阶段展示记忆成熟度");
                return;
            }

            Plot.Clear();
            ApplyAxisStyle("记忆成熟度分布", "学习阶段", "词条数量");

            var labels = new[] { "新生", "学习中", "已掌握" };
            var colors = new[] { Series2, AccentColor, Series3 };
            var values = new[] { distribution.NewCount, distribution.LearningCount, distribution.MasteredCount };
            var maxCount = Math.Max(1, Math.Max(values[0], Math.Max(values[1], values[2])));

            for (int i = 0; i < values.Length; i++)
            {
                var bar = Plot.AddBar(values[i], i);
                bar.FillColor = colors[i];
                bar.BorderColor = colors[i];
            }

            Plot.XTicks(new double[] { 0, 1, 2 }, labels);
            Plot.SetAxisLimitsY(0, maxCount * 1.15);
            Refresh();
        }

        /// <summary>便利重载：直接消费 <see cref="MemoryInsights.Maturity"/></summary>
        public void UpdateData(MemoryInsights insights)
            => UpdateData(insights?.Maturity ?? new MaturityDistribution());
    }
}