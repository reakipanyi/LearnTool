using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls
{
    public class MiniLineChart : Control
    {
        private List<double> _dataPoints = new();
        private Color _lineColor = Color.FromArgb(33, 150, 243);
        private Color _fillColor = Color.FromArgb(33, 150, 243);
        private Color _textColor = Color.FromArgb(102, 102, 102);
        private string _title = string.Empty;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<double> DataPoints
        {
            get => _dataPoints;
            set
            {
                _dataPoints = value ?? new List<double>();
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color LineColor
        {
            get => _lineColor;
            set
            {
                _lineColor = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color FillColor
        {
            get => _fillColor;
            set
            {
                _fillColor = value;
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

        public MiniLineChart()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Size = new Size(200, 60);
        }

        public void SetData(IEnumerable<double> data)
        {
            _dataPoints = data.ToList();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int paddingLeft = 5;
            int paddingRight = 5;
            int paddingTop = 5;
            int paddingBottom = 15;

            int chartWidth = Width - paddingLeft - paddingRight;
            int chartHeight = Height - paddingTop - paddingBottom;

            if (_dataPoints.Count < 2 || chartWidth <= 0 || chartHeight <= 0)
            {
                DrawEmptyState(g);
                return;
            }

            double minVal = _dataPoints.Min();
            double maxVal = _dataPoints.Max();
            double range = maxVal - minVal;
            if (range < 0.01) range = 0.01;

            double yFactor = chartHeight / range;
            double xStep = (double)chartWidth / (_dataPoints.Count - 1);

            var points = new List<PointF>();
            for (int i = 0; i < _dataPoints.Count; i++)
            {
                float x = paddingLeft + (float)(i * xStep);
                float y = paddingTop + chartHeight - (float)((_dataPoints[i] - minVal) * yFactor);
                points.Add(new PointF(x, y));
            }

            using (var fillPath = new System.Drawing.Drawing2D.GraphicsPath())
            {
                fillPath.AddLine(paddingLeft, Height - paddingBottom, points[0].X, points[0].Y);
                fillPath.AddLines(points.ToArray());
                fillPath.AddLine(points[^1].X, Height - paddingBottom, paddingLeft, Height - paddingBottom);
                fillPath.CloseAllFigures();

                using var fillBrush = new SolidBrush(Color.FromArgb(30, _fillColor));
                g.FillPath(fillBrush, fillPath);
            }

            using (var linePen = new Pen(_lineColor, 2))
            {
                linePen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                linePen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                g.DrawLines(linePen, points.ToArray());
            }

            for (int i = 0; i < points.Count; i++)
            {
                using var dotBrush = new SolidBrush(_lineColor);
                g.FillEllipse(dotBrush, points[i].X - 2, points[i].Y - 2, 4, 4);
            }

            if (!string.IsNullOrEmpty(_title))
            {
                using var titleFont = new Font("微软雅黑", 8F);
                using var titleBrush = new SolidBrush(_textColor);
                var titleSize = g.MeasureString(_title, titleFont);
                g.DrawString(_title, titleFont, titleBrush,
                    (Width - titleSize.Width) / 2, Height - paddingBottom + 2);
            }
        }

        private void DrawEmptyState(Graphics g)
        {
            using var textBrush = new SolidBrush(Color.FromArgb(180, 180, 180));
            using var textFont = new Font("微软雅黑", 9F);
            string text = "暂无数据";
            var textSize = g.MeasureString(text, textFont);
            g.DrawString(text, textFont, textBrush,
                (Width - textSize.Width) / 2, (Height - textSize.Height) / 2);
        }
    }
}
