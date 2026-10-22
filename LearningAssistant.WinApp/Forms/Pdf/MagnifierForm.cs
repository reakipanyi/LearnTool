using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.Pdf
{
    /// <summary>
    /// 放大镜窗体，显示 PDF 页面指定区域的放大视图。控件在设计器中创建，便于可视化预览。
    /// </summary>
    public partial class MagnifierForm : Form
    {
        private readonly Bitmap _sourceImage;
        private readonly Point _sourcePoint;
        private readonly int _zoomFactor = 2;
        private readonly int _lensRadius = 80;
        private bool _isDragging;
        private Point _dragStart;

        /// <summary>供 Visual Studio 设计器实例化使用。</summary>
        public MagnifierForm() : this(new Bitmap(1, 1), Point.Empty)
        {
        }

        public MagnifierForm(Bitmap sourceImage, Point sourcePoint, int zoomFactor = 2)
        {
            _sourceImage = sourceImage;
            _sourcePoint = sourcePoint;
            _zoomFactor = zoomFactor;

            InitializeComponent();

            Size = new Size(_lensRadius * 2 * _zoomFactor + 16, _lensRadius * 2 * _zoomFactor + 40);

            UpdateMagnifiedImage();
        }

        private void UpdateMagnifiedImage()
        {
            int halfW = _lensRadius;
            int halfH = _lensRadius;

            int srcX = Math.Max(0, Math.Min(_sourcePoint.X - halfW, _sourceImage.Width - _lensRadius * 2));
            int srcY = Math.Max(0, Math.Min(_sourcePoint.Y - halfH, _sourceImage.Height - _lensRadius * 2));
            int srcW = Math.Min(_lensRadius * 2, _sourceImage.Width - srcX);
            int srcH = Math.Min(_lensRadius * 2, _sourceImage.Height - srcY);

            if (srcW <= 0 || srcH <= 0) return;

            using var cropped = _sourceImage.Clone(new Rectangle(srcX, srcY, srcW, srcH), _sourceImage.PixelFormat);
            var magnified = new Bitmap(srcW * _zoomFactor, srcH * _zoomFactor);
            using var g = Graphics.FromImage(magnified);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(cropped, 0, 0, magnified.Width, magnified.Height);

            // 绘制十字准星
            int centerX = magnified.Width / 2;
            int centerY = magnified.Height / 2;
            using var pen = new Pen(Color.Red, 1);
            g.DrawLine(pen, centerX - 10, centerY, centerX + 10, centerY);
            g.DrawLine(pen, centerX, centerY - 10, centerX, centerY + 10);

            // 绘制圆形边框
            using var circlePen = new Pen(Color.Black, 2);
            g.DrawEllipse(circlePen, 0, 0, magnified.Width - 1, magnified.Height - 1);

            pictureBox.Image?.Dispose();
            pictureBox.Image = magnified;
        }

        private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragStart = new Point(e.X, e.Y);
            }
        }

        private void PictureBox_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Left += e.X - _dragStart.X;
                Top += e.Y - _dragStart.Y;
            }
        }

        private void PictureBox_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = false;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
            base.OnKeyDown(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            pictureBox.Image?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
