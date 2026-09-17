using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using ScottPlot;

namespace LearningAssistant.Forms.UserControls.Charts
{
    /// <summary>
    /// 统一图表基类：所有 ScottPlot 图表继承此基类，实现 IThemeable（明暗主题联动），
    /// 统一配色/背景/字体/空态占位，避免各图表硬编码颜色导致主题不一致（03 方案 3.4/3.6）。
    /// </summary>
    public abstract class LearningChartBase : FormsPlot, IThemeable
    {
        /// <summary>主文字色</summary>
        protected Color TextColor = Color.FromArgb(33, 33, 33);
        /// <summary>次级文字色</summary>
        protected Color TextSecondaryColor = Color.FromArgb(120, 120, 120);
        /// <summary>图表背景（跟随主题 Surface）</summary>
        protected Color ChartBackground = Color.White;
        /// <summary>网格线颜色</summary>
        protected Color GridColor = Color.FromArgb(230, 230, 230);
        /// <summary>系列主色</summary>
        protected Color Series1 = Color.FromArgb(63, 81, 181);
        /// <summary>系列次色</summary>
        protected Color Series2 = Color.FromArgb(156, 39, 176);
        /// <summary>系列成功色</summary>
        protected Color Series3 = Color.FromArgb(76, 175, 80);
        /// <summary>强调/警示色</summary>
        protected Color AccentColor = Color.FromArgb(255, 152, 0);
        /// <summary>错误色</summary>
        protected Color ErrorColor = Color.FromArgb(244, 67, 54);

        /// <summary>当前主题颜色配置</summary>
        protected ThemeColors? Theme;

        protected LearningChartBase()
        {
            Dock = DockStyle.Fill;
            // 默认采用浅色主题配色，后续由 ThemeService.ApplyTheme 覆盖
            ApplyChartTheme(ThemeService.GetColors(ThemeMode.Light));
            Font = new Font("微软雅黑", 9F);
        }

        /// <inheritdoc/>
        public virtual void ApplyTheme(ThemeColors colors)
        {
            Theme = colors;
            TextColor = colors.TextPrimary;
            TextSecondaryColor = colors.TextSecondary;
            ChartBackground = colors.Surface;
            GridColor = colors.Divider;
            Series1 = colors.Primary;
            Series2 = colors.Accent;
            Series3 = colors.Success;
            AccentColor = colors.Warning;
            ErrorColor = colors.Error;
            ApplyChartTheme(colors);
            ApplyAxisStyle();
        }

        private void ApplyChartTheme(ThemeColors colors)
        {
            BackColor = colors.Surface;
            Plot.Style(figureBackground: colors.Surface, dataBackground: colors.Surface);
            Plot.Grid(enable: true, color: colors.Divider);
        }

        /// <summary>应用轴与字体样式（子类可在数据刷新时调用以保持主题一致）</summary>
        protected void ApplyAxisStyle(string? title = null, string? xLabel = null, string? yLabel = null)
        {
            ApplyChartTheme(Theme ?? ThemeService.GetColors(ThemeMode.Light));
            if (title != null) Plot.Title(title, fontName: "微软雅黑", color: TextColor);
            if (xLabel != null) Plot.XLabel(xLabel);
            if (yLabel != null) Plot.YLabel(yLabel);
            Refresh();
        }

        /// <summary>
        /// 空数据占位：清空图形并显示引导提示，不展示假数值（03 方案 3.4 空态）。
        /// </summary>
        protected void ShowEmptyState(string message, string? hint = null)
        {
            Plot.Clear();
            Plot.Grid(false);
            Plot.Title(message ?? "暂无数据", fontName: "微软雅黑", size: 15f, color: TextSecondaryColor);
            if (!string.IsNullOrEmpty(hint))
            {
                Plot.XLabel(hint);
            }
            Refresh();
        }
    }

    public class LearningTrendChart : LearningChartBase
    {
        public LearningTrendChart()
        {
            ApplyAxisStyle("学习趋势", "日期", "学习数量");
        }

        /// <summary>旧版数据接口（兼容调用点）：xx轴为序号，附正确率副线</summary>
        public void UpdateData(List<double> xValues, List<double> yValues, List<double>? accuracyValues = null)
        {
            Plot.Clear();
            ApplyAxisStyle("学习趋势", "日期", "学习数量");

            var scatter = Plot.AddScatter(xValues.ToArray(), yValues.ToArray());
            scatter.Color = Series1;
            scatter.LineWidth = 2;
            scatter.MarkerSize = 5;
            scatter.MarkerShape = MarkerShape.filledCircle;

            if (accuracyValues != null && accuracyValues.Count > 0)
            {
                var accuracyScatter = Plot.AddScatter(xValues.ToArray(), accuracyValues.ToArray());
                accuracyScatter.Color = Series3;
                accuracyScatter.LineWidth = 2;
                accuracyScatter.MarkerSize = 4;
                accuracyScatter.MarkerShape = MarkerShape.filledSquare;
            }

            Plot.AxisAuto();
            var yMax = 0.0;
            if (yValues != null && yValues.Count > 0) yMax = yValues.Max();
            if (accuracyValues != null && accuracyValues.Count > 0) yMax = Math.Max(yMax, accuracyValues.Max());
            Plot.SetAxisLimitsY(0, Math.Max(1, yMax * 1.15));
            Refresh();
        }

        public void UpdateDataWithLabels(List<string> labels, List<double> yValues, List<double>? accuracyValues = null)
        {
            var xValues = Enumerable.Range(0, labels.Count).Select(i => (double)i).ToList();
            UpdateData(xValues, yValues, accuracyValues);

            if (labels.Count > 0)
            {
                Plot.XTicks(xValues.ToArray(), labels.ToArray());
                Plot.AxisAuto();
                Refresh();
            }
        }

        /// <summary>
        /// 聚合数据源重载（03 方案 3.4）：直接消费统计底座的 <see cref="TrendSeries"/>，
        /// studySeries 绘制学习量、accuracySeries 绘制正确率（0-100）。
        /// </summary>
        public void UpdateData(TrendSeries studySeries, TrendSeries? accuracySeries = null)
        {
            if (studySeries == null || studySeries.Points.Count == 0)
            {
                ShowEmptyState("暂无趋势数据", "开始学习后这里将展示每日学习趋势");
                return;
            }

            var xs = studySeries.Points.Select(p => (double)p.Date.Subtract(studySeries.Points[0].Date).Days).ToArray();
            var ys = studySeries.Points.Select(p => p.Value).ToArray();
            var labels = studySeries.Points.Select(p => p.Date.ToString("M/d")).ToArray();

            UpdateData(xs.ToList(), ys.ToList(), null);
            Plot.XTicks(xs, labels);

            if (accuracySeries != null && accuracySeries.Points.Count > 0)
            {
                var axs = accuracySeries.Points.Select(p => (double)p.Date.Subtract(studySeries.Points[0].Date).Days).ToArray();
                var ays = accuracySeries.Points.Select(p => p.Value).ToArray();
                var line = Plot.AddScatter(axs, ays);
                line.Color = Series3;
                line.LineWidth = 2;
                line.MarkerSize = 4;
                line.MarkerShape = MarkerShape.filledSquare;
            }

            var trendMax = ys.DefaultIfEmpty(0).Max();
            if (accuracySeries != null && accuracySeries.Points.Count > 0)
                trendMax = Math.Max(trendMax, accuracySeries.Points.Select(p => p.Value).Max());
            Plot.AxisAuto();
            Plot.SetAxisLimitsY(0, Math.Max(1, trendMax * 1.15));
            Refresh();
        }
    }

    public class ForgettingCurveChart : LearningChartBase
    {
        public ForgettingCurveChart()
        {
            ApplyAxisStyle("遗忘曲线", "天数", "记忆保留率 (%)");
        }

        public void UpdateCurve(Dictionary<int, double> curveData)
        {
            if (curveData == null || curveData.Count == 0)
            {
                ShowEmptyState("暂无遗忘曲线数据", "完成复习后这里将生成记忆遗忘曲线");
                return;
            }

            Plot.Clear();
            ApplyAxisStyle("遗忘曲线", "天数", "记忆保留率 (%)");

            var ordered = curveData.OrderBy(k => k.Key).ToList();
            var xValues = ordered.Select(k => (double)k.Key).ToArray();
            var yValues = ordered.Select(k => k.Value * 100).ToArray();

            var scatter = Plot.AddScatter(xValues, yValues);
            scatter.Color = Series2;
            scatter.LineWidth = 2;
            scatter.MarkerSize = 3;
            scatter.MarkerShape = MarkerShape.filledCircle;

            Plot.SetAxisLimitsY(0, 100);
            Plot.AxisAutoX();
            Refresh();
        }

        /// <summary>聚合数据源重载：消费 <see cref="MemoryInsights.ForgettingCurve"/></summary>
        public void UpdateCurve(MemoryInsights insights)
            => UpdateCurve(insights?.ForgettingCurve ?? new Dictionary<int, double>());
    }

    public class CategoryProgressChart : LearningChartBase
    {
        public CategoryProgressChart()
        {
            ApplyAxisStyle("分类进度", null, "学习数量");
        }

        public void UpdateData(List<string> categories, List<double> progressValues)
        {
            if ((categories == null || categories.Count == 0) || (progressValues == null || progressValues.Count == 0))
            {
                ShowEmptyState("暂无分类数据", "学习不同分类内容后将显示进度分布");
                return;
            }

            Plot.Clear();
            ApplyAxisStyle("分类进度", null, "学习数量");

            var palette = new[]
            {
                Series1, AccentColor, Series3, ErrorColor, Series2, Color.FromArgb(33, 150, 243)
            };

            for (int i = 0; i < categories.Count && i < progressValues.Count; i++)
            {
                var bar = Plot.AddBar(progressValues[i], i);
                bar.FillColor = palette[i % palette.Length];
            }

            var xValues = Enumerable.Range(0, categories.Count).Select(i => (double)i).ToArray();
            Plot.XTicks(xValues, categories.ToArray());
            var catMax = progressValues.Count > 0 ? progressValues.Max() : 0.0;
            Plot.SetAxisLimitsY(0, Math.Max(1, catMax * 1.15));
            Refresh();
        }

        /// <summary>聚合数据源重载：消费 <see cref="CategoryBreakdown"/>，按学习项数绘制</summary>
        public void UpdateData(List<CategoryBreakdown> breakdowns)
        {
            if (breakdowns == null || breakdowns.Count == 0)
            {
                ShowEmptyState("暂无分类数据", "学习不同分类内容后将显示进度分布");
                return;
            }

            var top = breakdowns.OrderByDescending(b => b.ItemsStudied).Take(6).ToList();
            var categories = top.Select(b => b.Category).ToList();
            var values = top.Select(b => (double)b.ItemsStudied).ToList();
            UpdateData(categories, values);
        }
    }

    public class ReviewDistributionChart : LearningChartBase
    {
        public ReviewDistributionChart()
        {
            ApplyAxisStyle("评分分布", "评分", "次数");
        }

        public void UpdateData(Dictionary<int, int> ratingDistribution)
        {
            Plot.Clear();
            ApplyAxisStyle("评分分布", "评分", "次数");

            var labels = new[] { "1分", "2分", "3分", "4分", "5分" };
            var palette = new[]
            {
                ErrorColor, AccentColor, Color.FromArgb(255, 193, 7), Series3, Series2
            };

            int maxCount = 0;
            var xValues = new List<double>();
            var anyData = false;
            for (int i = 1; i <= 5; i++)
            {
                var value = ratingDistribution.GetValueOrDefault(i) + ratingDistribution.GetValueOrDefault(i * 10);
                var bar = Plot.AddBar(value, i);
                bar.FillColor = palette[i - 1];
                xValues.Add(i);
                if (value > 0) { anyData = true; }
                if (value > maxCount) maxCount = value;
            }

            Plot.XTicks(xValues.ToArray(), labels);
            Plot.SetAxisLimitsY(0, Math.Max(1, maxCount));

            if (!anyData)
            {
                Plot.Clear();
                ShowEmptyState("暂无评分分布", "完成评分作答后将显示分布");
                return;
            }

            Refresh();
        }

        /// <summary>聚合数据源重载：消费 <see cref="MemoryInsights.ReviewDistribution"/>（距上次复习天数分布）</summary>
        public void UpdateReviewInterval(MemoryInsights insights)
        {
            var dist = insights?.ReviewDistribution ?? new Dictionary<int, int>();
            if (dist.Count == 0 || dist.Values.All(v => v == 0))
            {
                ShowEmptyState("暂无复习间隔分布", "完成复习后将显示距上次复习的天数分布");
                return;
            }

            Plot.Clear();
            ApplyAxisStyle("复习间隔分布", "距上次复习(天)", "项数");

            var buckets = new Dictionary<string, int>();
            foreach (var kv in dist)
            {
                var bucket = kv.Key <= 0 ? "当天" : kv.Key <= 1 ? "1天" : kv.Key <= 3 ? "2-3天" : kv.Key <= 7 ? "4-7天" : ">7天";
                buckets[bucket] = buckets.GetValueOrDefault(bucket) + kv.Value;
            }

            var labels = buckets.Keys.ToList();
            var values = buckets.Values.ToList();
            var xVals = new List<double>();
            for (int i = 0; i < labels.Count; i++)
            {
                var bar = Plot.AddBar(values[i], i);
                bar.FillColor = Series1;
                bar.BorderColor = Series1;
                xVals.Add(i);
            }

            Plot.XTicks(xVals.ToArray(), labels.ToArray());
            Plot.SetAxisLimitsY(0, Math.Max(1, values.DefaultIfEmpty(0).Max()));
            Refresh();
        }
    }

    /// <summary>
    /// 多用户数据对比图（04 方案 3.1 对比 Tab / 3.4）：按所选指标为每位用户绘制一根柱。
    /// </summary>
    public class UserComparisonChart : LearningChartBase
    {
        public UserComparisonChart()
        {
            ApplyAxisStyle("多用户对比", "用户", "指标值");
        }

        public void UpdateData(List<string> users, List<double> values, string metricLabel)
        {
            Plot.Clear();
            ApplyAxisStyle("多用户对比", "用户", metricLabel);

            if (users == null || users.Count == 0 || values == null || users.Count != values.Count)
            {
                ShowEmptyState("暂无对比数据", "至少需要两位用户");
                return;
            }

            var palette = new[]
            {
                Series1, Series2, Series3, AccentColor, ErrorColor,
                Color.FromArgb(33, 150, 243), Color.FromArgb(0, 137, 123), Color.FromArgb(255, 152, 0)
            };

            var xs = new List<double>();
            for (int i = 0; i < users.Count; i++)
            {
                var bar = Plot.AddBar(values[i], i);
                bar.FillColor = palette[i % palette.Length];
                bar.BorderColor = palette[i % palette.Length];
                xs.Add(i);
            }

            Plot.XTicks(xs.ToArray(), users.ToArray());
            var cmax = values.Count > 0 ? values.Max() : 0.0;
            Plot.SetAxisLimitsY(0, Math.Max(1, cmax * 1.15));
            Refresh();
        }
    }
}