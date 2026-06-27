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

            Plot.Title("学习趋势", fontName: null, color: System.Drawing.Color.FromArgb(33, 33, 33));
            Plot.XLabel("日期");
            Plot.YLabel("学习数量");
            Plot.Grid(enable: true);
            Plot.Style(figureBackground: System.Drawing.Color.White, dataBackground: System.Drawing.Color.White);

            Font = new System.Drawing.Font("微软雅黑", 9F);
        }

        public void UpdateData(List<double> xValues, List<double> yValues, List<double>? accuracyValues = null)
        {
            Plot.Clear();

            var scatter = Plot.AddScatter(xValues.ToArray(), yValues.ToArray());
            scatter.Color = System.Drawing.Color.FromArgb(63, 81, 181);
            scatter.LineWidth = 2;
            scatter.MarkerSize = 5;
            scatter.MarkerShape = MarkerShape.filledCircle;

            if (accuracyValues != null && accuracyValues.Count > 0)
            {
                var accuracyScatter = Plot.AddScatter(xValues.ToArray(), accuracyValues.ToArray());
                accuracyScatter.Color = System.Drawing.Color.FromArgb(76, 175, 80);
                accuracyScatter.LineWidth = 2;
                accuracyScatter.MarkerSize = 4;
                accuracyScatter.MarkerShape = MarkerShape.filledSquare;
            }

            Plot.AxisAuto();
            Plot.SetAxisLimitsY(0, double.MaxValue);
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

            Plot.XTicks(xValues.ToArray(), labels.ToArray());
            Plot.AxisAuto();
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

            Plot.Title("遗忘曲线", fontName: null, color: System.Drawing.Color.FromArgb(33, 33, 33));
            Plot.XLabel("天数");
            Plot.YLabel("记忆保留率 (%)");
            Plot.Grid(enable: true);
            Plot.Style(figureBackground: System.Drawing.Color.White, dataBackground: System.Drawing.Color.White);

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

            var scatter = Plot.AddScatter(xValues.ToArray(), yValues.ToArray());
            scatter.Color = System.Drawing.Color.FromArgb(156, 39, 176);
            scatter.LineWidth = 2;
            scatter.MarkerSize = 3;
            scatter.MarkerShape = MarkerShape.filledCircle;

            Plot.SetAxisLimitsY(0, 100);
            Plot.AxisAutoX();
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

            Plot.Title("分类进度", fontName: null, color: System.Drawing.Color.FromArgb(33, 33, 33));
            Plot.Grid(enable: true);
            Plot.Style(figureBackground: System.Drawing.Color.White, dataBackground: System.Drawing.Color.White);

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

            for (int i = 0; i < categories.Count && i < progressValues.Count; i++)
            {
                var bar = Plot.AddBar(progressValues[i], i);
                bar.FillColor = colors[i % colors.Length];
            }

            var xValues = new List<double>();
            for (int i = 0; i < categories.Count; i++)
            {
                xValues.Add(i);
            }

            Plot.XTicks(xValues.ToArray(), categories.ToArray());
            Plot.SetAxisLimitsY(0, 100);
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

            Plot.Title("评分分布", fontName: null, color: System.Drawing.Color.FromArgb(33, 33, 33));
            Plot.Grid(enable: true);
            Plot.Style(figureBackground: System.Drawing.Color.White, dataBackground: System.Drawing.Color.White);

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

            for (int i = 1; i <= 5; i++)
            {
                xValues.Add(i);
                var bar = Plot.AddBar(ratingDistribution.GetValueOrDefault(i, 0), i);
                bar.FillColor = colors[i - 1];
            }

            Plot.XTicks(xValues.ToArray(), labels);
            Plot.SetAxisLimitsY(0, double.MaxValue);
            Refresh();
        }
    }
}
