using System;
using System.Drawing;
using System.Windows.Forms;

namespace UnifiedLearningAssistant.Views.UI
{
    // 新增功能：中等级 - 学习统计可视化
    public class ChartControl : Control
    {
        private double[]? _values;
        private string[]? _labels;
        private Color[]? _colors;
        private readonly Random _random = new Random();

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

            DrawPieChart(g);
            DrawLegend(g);
        }

        private void DrawPieChart(Graphics g)
        {
            if (_values == null || _colors == null) return;

            var centerX = Width * 0.35f;
            var centerY = Height * 0.5f;
            var radius = Math.Min(centerX, centerY) - 20;

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
                startAngle += sweepAngle;
            }
        }

        private void DrawLegend(Graphics g)
        {
            if (_labels == null || _values == null || _colors == null) return;

            var startX = Width * 0.65f;
            var startY = 50f;
            var lineHeight = 30f;

            for (int i = 0; i < _labels.Length && i < _values.Length; i++)
            {
                using var brush = new SolidBrush(_colors[i]);
                g.FillRectangle(brush, startX, startY + i * lineHeight, 20, 20);

                var percentage = 0.0;
                var total = 0.0;
                foreach (var v in _values) total += v;
                if (total > 0) percentage = _values[i] / total * 100;

                using var textBrush = new SolidBrush(Color.Black);
                var text = $"{_labels[i]}: {_values[i]} ({percentage:F0}%)";
                g.DrawString(text, Font, textBrush, startX + 30, startY + i * lineHeight);
            }
        }
    }
}
