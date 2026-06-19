using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls
{
    /// <summary>
    /// 图表类型枚举
    /// </summary>
    public enum ChartType
    {
        Pie,
        Bar,
        Line
    }

    /// <summary>
    /// 图表控件，支持饼图、柱状图和折线图
    /// </summary>
    public class ChartControl : Control
    {
        private double[]? _values;
        private string[]? _labels;
        private Color[]? _colors;
        private ChartType _chartType = ChartType.Pie;
        private string _title = string.Empty;

        public ChartControl()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
            DoubleBuffered = true;
            BackColor = Color.White;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ChartType ChartType
        {
            get => _chartType;
            set
            {
                _chartType = value;
                Invalidate();
            }
        }

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

        public void SetData(double[] values, string[] labels, Color[] colors)
        {
            _values = values;
            _labels = labels;
            _colors = colors;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_values == null || _values.Length == 0)
                return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 绘制标题
            if (!string.IsNullOrEmpty(_title))
            {
                using var titleBrush = new SolidBrush(Color.FromArgb(50, 50, 50));
                using var titleFont = new Font(Font.FontFamily, 14f, FontStyle.Bold);
                var titleSize = g.MeasureString(_title, titleFont);
                g.DrawString(_title, titleFont, titleBrush, (Width - titleSize.Width) / 2, 10);
            }

            // 根据图表类型绘制
            switch (_chartType)
            {
                case ChartType.Pie:
                    DrawPieChart(g);
                    DrawLegend(g);
                    break;
                case ChartType.Bar:
                    DrawBarChart(g);
                    break;
                case ChartType.Line:
                    DrawLineChart(g);
                    break;
            }
        }

        private void DrawPieChart(Graphics g)
        {
            if (_values == null || _colors == null) return;

            var centerX = Width * 0.35f;
            var centerY = Height * 0.5f;
            var radius = Math.Min(centerX, centerY) - 40;

            var total = 0.0;
            foreach (var v in _values)
                total += v;

            if (total == 0) return;

            float startAngle = -90f;
            for (int i = 0; i < _values.Length; i++)
            {
                if (_values[i] <= 0) continue;

                var sweepAngle = (float)(_values[i] / total * 360);
                using var brush = new SolidBrush(_colors[i]);

                var rect = new RectangleF(
                    centerX - radius,
                    centerY - radius,
                    radius * 2,
                    radius * 2);

                g.FillPie(brush, rect, startAngle, sweepAngle);

                // 绘制边界
                using var pen = new Pen(Color.White, 2);
                g.DrawPie(pen, rect, startAngle, sweepAngle);

                startAngle += sweepAngle;
            }
        }

        private void DrawBarChart(Graphics g)
        {
            if (_values == null || _labels == null || _colors == null) return;

            var padding = 60;
            var chartWidth = Width - padding * 2;
            var chartHeight = Height - padding * 2;
            var maxValue = _values.Length > 0 ? _values.Max() : 1;
            var barWidth = Math.Max(20, chartWidth / _values.Length * 0.6);
            var gap = (chartWidth - barWidth * _values.Length) / (_values.Length + 1);

            // 绘制坐标轴
            using var axisPen = new Pen(Color.Gray, 2);
            g.DrawLine(axisPen, padding, Height - padding, padding, padding);
            g.DrawLine(axisPen, padding, Height - padding, Width - padding, Height - padding);

            // 绘制柱状图
            for (int i = 0; i < _values.Length; i++)
            {
                var barHeight = (float)(_values[i] / maxValue * (chartHeight - 30));
                var x = padding + gap + (barWidth + gap) * i;
                var y = Height - padding - barHeight;

                // 绘制柱状
                using var brush = new SolidBrush(_colors[i]);
                g.FillRectangle(brush, (float)x, (float)y, (float)barWidth, (float)barHeight);

                // 绘制边框
                using var pen = new Pen(Color.FromArgb(100, _colors[i].R, _colors[i].G, _colors[i].B), 1);
                g.DrawRectangle(pen, (float)x, (float)y, (float)barWidth, (float)barHeight);
                // 绘制数值标签
                using var textBrush = new SolidBrush(Color.Black);
                var valueText = _values[i].ToString();
                var textSize = g.MeasureString(valueText, Font);
                g.DrawString(valueText, Font, textBrush,
              (float)(x + (barWidth - textSize.Width) / 2),
                    y - textSize.Height - 5);

                // 绘制x轴标签
                var labelSize = g.MeasureString(_labels[i], Font);
                g.DrawString(_labels[i], Font, textBrush,
                (float)(x + (barWidth - labelSize.Width) / 2),
                    Height - padding + 10);
            }
        }

        private void DrawLineChart(Graphics g)
        {
            if (_values == null || _labels == null || _colors == null) return;

            var padding = 60;
            var chartWidth = Width - padding * 2;
            var chartHeight = Height - padding * 2;
            var maxValue = _values.Length > 0 ? _values.Max() : 1;
            var stepX = chartWidth / Math.Max(1, _values.Length - 1);

            // 绘制坐标轴
            using var axisPen = new Pen(Color.Gray, 2);
            g.DrawLine(axisPen, padding, Height - padding, padding, padding);
            g.DrawLine(axisPen, padding, Height - padding, Width - padding, Height - padding);

            // 计算点的位置
            var points = new PointF[_values.Length];
            for (int i = 0; i < _values.Length; i++)
            {
                var x = padding + i * stepX;
                var y = Height - padding - (float)(_values[i] / maxValue * (chartHeight - 30));
                points[i] = new PointF(x, y);
            }

            // 绘制折线
            if (points.Length >= 2)
            {
                using var linePen = new Pen(_colors[0], 3);
                g.DrawLines(linePen, points);
            }

            // 绘制数据点和标签
            for (int i = 0; i < points.Length; i++)
            {
                // 绘制数据点
                using var brush = new SolidBrush(_colors[i % _colors.Length]);
                g.FillEllipse(brush, points[i].X - 5, points[i].Y - 5, 10, 10);
                using var pen = new Pen(Color.White, 2);
                g.DrawEllipse(pen, points[i].X - 5, points[i].Y - 5, 10, 10);

                // 绘制数值标签
                using var textBrush = new SolidBrush(Color.Black);
                var valueText = _values[i].ToString();
                var textSize = g.MeasureString(valueText, Font);
                g.DrawString(valueText, Font, textBrush,
                    points[i].X - textSize.Width / 2,
                    points[i].Y - textSize.Height - 10);

                // 绘制x轴标签
                if (i < _labels.Length)
                {
                    var labelSize = g.MeasureString(_labels[i], Font);
                    g.DrawString(_labels[i], Font, textBrush,
                        points[i].X - labelSize.Width / 2,
                        Height - padding + 10);
                }
            }
        }

        private void DrawLegend(Graphics g)
        {
            if (_labels == null || _values == null || _colors == null) return;

            var startX = Width * 0.65f;
            var startY = 60f;
            var lineHeight = 35f;

            for (int i = 0; i < _labels.Length && i < _values.Length; i++)
            {
                // 绘制图例方块
                using var brush = new SolidBrush(_colors[i]);
                g.FillRectangle(brush, startX, startY + i * lineHeight, 25, 25);
                using var pen = new Pen(Color.FromArgb(100, _colors[i].R, _colors[i].G, _colors[i].B), 1);
                g.DrawRectangle(pen, startX, startY + i * lineHeight, 25, 25);

                // 计算百分比
                var percentage = 0.0;
                var total = 0.0;
                foreach (var v in _values) total += v;
                if (total > 0) percentage = _values[i] / total * 100;

                // 绘制文本
                using var textBrush = new SolidBrush(Color.Black);
                var text = $"{_labels[i]}: {_values[i]} ({percentage:F0}%)";
                g.DrawString(text, Font, textBrush, startX + 35, startY + i * lineHeight + 3);
            }
        }
    }
}
