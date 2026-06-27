using System.Collections.Generic;
using System.Windows.Forms;
using ScottPlot;

namespace LearningAssistant.Forms.UserControls.Charts
{
    public class LearningTrendChart : FormsPlot
    {
        public LearningTrendChart()
        {
            InitializeChart();
        }

        private void InitializeChart()
        {
            BackColor = System.Drawing.Color.White;
            Size = new System.Drawing.Size(520, 200);
            Dock = DockStyle.Fill;

            Plot.Title("📈 学习趋势", size: 12, color: System.Drawing.Color.FromArgb(33, 33, 33));
            Plot.XLabel("日期");
            Plot.YLabel("学习数量");
            Plot.Grid(true);
            Plot.Background.Color = System.Drawing.Color.White;
            Plot.Style(Style.Gray1);

            Font = new System.Drawing.Font("微软雅黑", 9F);
        }

        public void UpdateData(List<double> xValues, List<double> yValues, List<double>? accuracyValues = null)
        {
            Plot.Clear();

            var scatter = Plot.Add.Scatter(xValues.ToArray(), yValues.ToArray());
            scatter.Color = System.Drawing.Color.FromArgb(63, 81, 181);
            scatter.LineWidth = 2;
            scatter.MarkerSize = 5;
            scatter.MarkerShape = MarkerShape.FilledCircle;

            if (accuracyValues != null && accuracyValues.Count > 0)
            {
                var accuracyScatter = Plot.Add.Scatter(xValues.ToArray(), accuracyValues.ToArray());
                accuracyScatter.Color = System.Drawing.Color.FromArgb(76, 175, 80);
                accuracyScatter.LineWidth = 2;
                accuracyScatter.MarkerSize = 4;
                accuracyScatter.MarkerShape = MarkerShape.FilledSquare;
            }

            Plot.Axes.AutoScale();
            Plot.Axes.SetLimitsY(0, null);
            Refresh();
        }

        public void UpdateDataWithLabels(List<string> labels, List<double> yValues, List<double>? accuracyValues = null)
        {
            var xValues = new List<double>();
            for (int i = 0; i < labels.Count; i++)
            {
                xValues.Add(i);
            }

            UpdateData(xValues, yValues, accuracyValues);

            Plot.XTicks(labels.ToArray());
            Plot.Axes.AutoScale();
            Refresh();
        }
    }

    public class ForgettingCurveChart : FormsPlot
    {
        public ForgettingCurveChart()
        {
            InitializeChart();
        }

        private void InitializeChart()
        {
            BackColor = System.Drawing.Color.White;
            Size = new System.Drawing.Size(600, 160);
            Dock = DockStyle.Fill;

            Plot.Title("📉 遗忘曲线", size: 12, color: System.Drawing.Color.FromArgb(33, 33, 33));
            Plot.XLabel("天数");
            Plot.YLabel("记忆保留率 (%)");
            Plot.Grid(true);
            Plot.Background.Color = System.Drawing.Color.White;
            Plot.Style(Style.Gray1);

            Font = new System.Drawing.Font("微软雅黑", 9F);
        }

        public void UpdateCurve(Dictionary<int, double> curveData)
        {
            Plot.Clear();

            var xValues = new List<double>();
            var yValues = new List<double>();

            foreach (var kvp in curveData.OrderBy(k => k.Key))
            {
                xValues.Add(kvp.Key);
                yValues.Add(kvp.Value * 100);
            }

            var scatter = Plot.Add.Scatter(xValues.ToArray(), yValues.ToArray());
            scatter.Color = System.Drawing.Color.FromArgb(156, 39, 176);
            scatter.LineWidth = 2;
            scatter.MarkerSize = 3;
            scatter.MarkerShape = MarkerShape.FilledCircle;

            Plot.Axes.SetLimitsY(0, 100);
            Plot.Axes.AutoScaleX();
            Plot.Axes.Tighten();
            Refresh();
        }
    }

    public class CategoryProgressChart : FormsPlot
    {
        public CategoryProgressChart()
        {
            InitializeChart();
        }

        private void InitializeChart()
        {
            BackColor = System.Drawing.Color.White;
            Size = new System.Drawing.Size(520, 180);
            Dock = DockStyle.Fill;

            Plot.Title("📊 分类进度", size: 12, color: System.Drawing.Color.FromArgb(33, 33, 33));
            Plot.Grid(true);
            Plot.Background.Color = System.Drawing.Color.White;
            Plot.Style(Style.Gray1);

            Font = new System.Drawing.Font("微软雅黑", 9F);
        }

        public void UpdateData(List<string> categories, List<double> progressValues)
        {
            Plot.Clear();

            var colors = new System.Drawing.Color[]
            {
                System.Drawing.Color.FromArgb(63, 81, 181),
                System.Drawing.Color.FromArgb(255, 152, 0),
                System.Drawing.Color.FromArgb(76, 175, 80),
                System.Drawing.Color.FromArgb(244, 67, 54),
                System.Drawing.Color.FromArgb(156, 39, 176),
                System.Drawing.Color.FromArgb(33, 150, 243)
            };

            var xValues = new List<double>();
            for (int i = 0; i < categories.Count; i++)
            {
                xValues.Add(i);
            }

            var bar = Plot.Add.Bar(xValues.ToArray(), progressValues.ToArray());
            for (int i = 0; i < bar.Bars.Count; i++)
            {
                bar.Bars[i].FillColor = colors[i % colors.Length];
            }

            Plot.XTicks(categories.ToArray());
            Plot.Axes.SetLimitsY(0, 100);
            Plot.Axes.Tighten();
            Refresh();
        }
    }

    public class ReviewDistributionChart : FormsPlot
    {
        public ReviewDistributionChart()
        {
            InitializeChart();
        }

        private void InitializeChart()
        {
            BackColor = System.Drawing.Color.White;
            Size = new System.Drawing.Size(300, 160);
            Dock = DockStyle.Fill;

            Plot.Title("⭐ 评分分布", size: 12, color: System.Drawing.Color.FromArgb(33, 33, 33));
            Plot.Grid(true);
            Plot.Background.Color = System.Drawing.Color.White;
            Plot.Style(Style.Gray1);

            Font = new System.Drawing.Font("微软雅黑", 9F);
        }

        public void UpdateData(Dictionary<int, int> ratingDistribution)
        {
            Plot.Clear();

            var labels = new string[] { "1分", "2分", "3分", "4分", "5分" };
            var colors = new System.Drawing.Color[]
            {
                System.Drawing.Color.FromArgb(244, 67, 54),
                System.Drawing.Color.FromArgb(255, 152, 0),
                System.Drawing.Color.FromArgb(255, 193, 7),
                System.Drawing.Color.FromArgb(76, 175, 80),
                System.Drawing.Color.FromArgb(33, 150, 243)
            };

            var xValues = new List<double>();
            var yValues = new List<double>();

            for (int i = 1; i <= 5; i++)
            {
                xValues.Add(i);
                yValues.Add(ratingDistribution.GetValueOrDefault(i, 0));
            }

            var bar = Plot.Add.Bar(xValues.ToArray(), yValues.ToArray());
            for (int i = 0; i < bar.Bars.Count; i++)
            {
                bar.Bars[i].FillColor = colors[i];
            }

            Plot.XTicks(labels);
            Plot.Axes.SetLimitsY(0, null);
            Plot.Axes.Tighten();
            Refresh();
        }
    }
}
