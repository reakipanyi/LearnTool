using LearningAssistant.Abstractions;
using LearningAssistant.Models.Pdf;
using Microsoft.Extensions.Logging;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Managers
{
    public class AnnotationToolHandler
    {
        private readonly ILogger _logger;
        private readonly IPdfReaderFormAccess _form;
        private readonly AnnotationLayerManager _layerManager;

        // 画笔状态
        public Color PenColor { get; set; } = Color.Black;
        public float PenWidth { get; set; } = 3f;
        public bool IsDashed { get; set; }
        public string PenType { get; set; } = "Pen"; // Pencil, Pen, Marker
        public string StrokeStyle { get; set; } = "Solid"; // Solid, DotLine, ArrowLine

        // 绘制状态
        public bool IsDrawing { get; set; }
        public bool IsDrawingShape { get; set; }
        public List<PointF>? CurrentStrokePoints { get; set; }
        public PointF? ShapeStartPoint { get; set; }
        public PointF? ShapeEndPoint { get; set; }
        public int DrawingPageIndex { get; set; } = -1;

        private Pen? _drawingPen;

        // 回调委托
        public Action<AnnotationStroke>? PushStrokeToUndoStack { get; set; }
        public Func<long?>? GetCurrentAudioTimestampMs { get; set; }
        public Action<int>? SaveAnnotationForPage { get; set; }
        public Action<float[], int, float, int, int, string?, int, string>? AddAnnotationStroke { get; set; }
        public Func<Point, PointF>? ClientToImage { get; set; }

        public AnnotationToolHandler(ILogger logger, IPdfReaderFormAccess form, AnnotationLayerManager layerManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _layerManager = layerManager ?? throw new ArgumentNullException(nameof(layerManager));
            _drawingPen = CreatePen(PenColor, PenWidth);
        }

        #region 画笔管理

        public void SetPenColor(Color color)
        {
            PenColor = color;
            _drawingPen?.Dispose();
            _drawingPen = CreatePen(PenColor, PenWidth);
        }

        public void SetPenWidth(float width)
        {
            PenWidth = Math.Max(1f, Math.Min(20f, width));
            _drawingPen?.Dispose();
            _drawingPen = CreatePen(PenColor, PenWidth);
        }

        public void SetDashStyle(bool dashed)
        {
            IsDashed = dashed;
        }

        public void SetPenType(string penType)
        {
            PenType = penType switch
            {
                "Pencil" => "Pencil",
                "Marker" => "Marker",
                _ => "Pen"
            };
        }

        public void SetStrokeStyle(string style)
        {
            StrokeStyle = style switch
            {
                "DotLine" => "DotLine",
                "ArrowLine" => "ArrowLine",
                _ => "Solid"
            };
        }

        public Pen CreatePen(Color color, float width)
        {
            var pen = PenType switch
            {
                "Pencil" => CreatePencilPen(color, width),
                "Marker" => CreateMarkerPen(color, width),
                _ => CreatePenPen(color, width)
            };
            return pen;
        }

        /// <summary>创建钢笔（水笔）画笔：完全不透明，线条干净利落，连接处圆润</summary>
        private static Pen CreatePenPen(Color color, float width)
        {
            var pen = new Pen(Color.FromArgb(255, color.R, color.G, color.B), width);
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            pen.Alignment = PenAlignment.Center;
            return pen;
        }

        /// <summary>创建铅笔画笔：半透明 + 轻微锯齿纹理，模拟铅笔素描质感</summary>
        private static Pen CreatePencilPen(Color color, float width)
        {
            var pen = new Pen(Color.FromArgb(200, color.R, color.G, color.B), Math.Max(1, width - 0.5f));
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            pen.DashPattern = new float[] { 4, 1, 4, 1, 3, 2, 5, 1 };
            pen.Alignment = PenAlignment.Center;
            return pen;
        }

        /// <summary>创建马克笔画笔：高度透明 + 较粗笔触，模拟荧光笔/马克笔效果</summary>
        private static Pen CreateMarkerPen(Color color, float width)
        {
            var pen = new Pen(Color.FromArgb(100, color.R, color.G, color.B), width + 2f);
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            pen.Alignment = PenAlignment.Center;
            return pen;
        }

        /// <summary>
        /// 使用 Cardinal 样条曲线绘制平滑曲线，并保持直线段依然笔直。
        /// 2点 → 直线（DrawLines），3点以上 → 张力曲线（DrawCurve）。
        /// 张力值 0.5 在平滑度和直线保持之间取得良好平衡。
        /// </summary>
        public static void DrawSmoothCurve(Graphics g, Pen pen, PointF[] points)
        {
            if (points == null || points.Length < 2) return;

            if (points.Length == 2)
            {
                g.DrawLine(pen, points[0], points[1]);
                return;
            }

            g.DrawCurve(pen, points, 0.5f);
        }

        /// <summary>Point[] 重载，自动转换为 PointF[]</summary>
        public static void DrawSmoothCurve(Graphics g, Pen pen, Point[] points)
        {
            if (points == null || points.Length < 2) return;
            var pts = Array.ConvertAll(points, p => (PointF)p);
            DrawSmoothCurve(g, pen, pts);
        }

        #endregion

        #region 绘制控制

        /// <summary>初始化自由绘制笔划</summary>
        public void BeginStroke(Point clientPoint, int pageIndex)
        {
            IsDrawing = true;
            DrawingPageIndex = pageIndex;
            var imgPt = ClientToImage!(clientPoint);
            CurrentStrokePoints = new List<PointF> { imgPt };
        }

        /// <summary>初始化形状绘制</summary>
        public void BeginShape(Point clientPoint, int pageIndex)
        {
            IsDrawingShape = true;
            DrawingPageIndex = pageIndex;
            ShapeStartPoint = ClientToImage!(clientPoint);
            ShapeEndPoint = ShapeStartPoint;
        }

        /// <summary>追加笔划点</summary>
        public void AddStrokePoint(PointF imgPoint)
        {
            CurrentStrokePoints?.Add(imgPoint);
        }

        /// <summary>更新形状终点（支持 Shift 约束）</summary>
        public void UpdateShapeEnd(PointF imgEnd, PointF? imgStart, AnnotationToolMode mode, bool shiftHeld)
        {
            if (shiftHeld && imgStart.HasValue)
            {
                var startPt = imgStart.Value;
                var dx = imgEnd.X - startPt.X;
                var dy = imgEnd.Y - startPt.Y;

                if (mode is AnnotationToolMode.Rectangle or AnnotationToolMode.Ellipse or AnnotationToolMode.Mosaic)
                {
                    var maxSide = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    imgEnd = new PointF(
                        startPt.X + Math.Sign(dx) * maxSide,
                        startPt.Y + Math.Sign(dy) * maxSide);
                }
                else if (mode == AnnotationToolMode.Arrow)
                {
                    imgEnd = Math.Abs(dx) > Math.Abs(dy)
                        ? new PointF(imgEnd.X, startPt.Y)
                        : new PointF(startPt.X, imgEnd.Y);
                }
            }

            ShapeEndPoint = imgEnd;
        }

        /// <summary>重置绘制状态</summary>
        public void ResetDrawingState()
        {
            IsDrawing = false;
            IsDrawingShape = false;
            CurrentStrokePoints = null;
            ShapeStartPoint = null;
            ShapeEndPoint = null;
            DrawingPageIndex = -1;
        }

        #endregion

        #region 绘制完成（写入标注层）

        /// <summary>完成自由绘制笔划，写入标注层并返回 Stroke 数据</summary>
        public AnnotationStroke? FinalizeStroke(AnnotationToolMode mode)
        {
            var bmp = _layerManager.AnnotationBitmap;
            if (bmp == null || CurrentStrokePoints == null || CurrentStrokePoints.Count < 2)
            {
                ResetDrawingState();
                return null;
            }

            bool isSecondPage = DrawingPageIndex > _form.CurrentPageIndex;
            Graphics activeGfx = isSecondPage ? _layerManager.SecondAnnotationGraphics! : _layerManager.AnnotationGraphics!;
            Bitmap activeBmp = isSecondPage ? _layerManager.SecondAnnotationBitmap! : _layerManager.AnnotationBitmap!;

            activeGfx.SmoothingMode = SmoothingMode.AntiAlias;

            Color drawColor = mode == AnnotationToolMode.Strikethrough ? Color.Red : PenColor;
            float drawWidth = mode == AnnotationToolMode.Strikethrough ? 6f : PenWidth;

            using var drawPen = CreatePen(drawColor, drawWidth);
            DrawSmoothCurve(activeGfx, drawPen, CurrentStrokePoints.ToArray());

            SaveAnnotationForPage?.Invoke(DrawingPageIndex);

            var imgW = activeBmp.Width;
            var imgH = activeBmp.Height;
            var pts = new List<float>();
            foreach (var pt in CurrentStrokePoints)
            {
                pts.Add(pt.X / imgW);
                pts.Add(pt.Y / imgH);
            }

            var stroke = new AnnotationStroke
            {
                Points = pts.ToArray(),
                ColorArgb = drawColor.ToArgb(),
                Thickness = drawWidth,
                PenType = PenType,
                StrokeStyle = StrokeStyle,
                AudioTimestampMs = GetCurrentAudioTimestampMs?.Invoke(),
                CreatedAt = DateTime.Now
            };

            PushStrokeToUndoStack?.Invoke(stroke);
            AddAnnotationStroke?.Invoke(pts.ToArray(), drawColor.ToArgb(), drawWidth, imgW, imgH, null, DrawingPageIndex, StrokeStyle);

            ResetDrawingState();
            return stroke;
        }

        /// <summary>完成形状绘制，写入标注层并返回 Stroke 数据</summary>
        public AnnotationStroke? FinalizeShape(AnnotationToolMode mode)
        {
            var bmp = _layerManager.AnnotationBitmap;
            if (bmp == null || !ShapeStartPoint.HasValue || !ShapeEndPoint.HasValue)
            {
                ResetDrawingState();
                return null;
            }

            var startPt = ShapeStartPoint.Value;
            var endPt = ShapeEndPoint.Value;
            var rect = new RectangleF(
                Math.Min(startPt.X, endPt.X),
                Math.Min(startPt.Y, endPt.Y),
                Math.Abs(endPt.X - startPt.X),
                Math.Abs(endPt.Y - startPt.Y));

            if (mode == AnnotationToolMode.Highlight)
            {
                if (rect.Width > 0 && rect.Height > 0)
                {
                    // 高亮由外部回调处理，只返回空 Stroke
                    ResetDrawingState();
                    return null;
                }
                ResetDrawingState();
                return null;
            }

            bool isSecondPage = DrawingPageIndex > _form.CurrentPageIndex;
            Graphics activeGfx = isSecondPage ? _layerManager.SecondAnnotationGraphics! : _layerManager.AnnotationGraphics!;
            Bitmap activeBmp = isSecondPage ? _layerManager.SecondAnnotationBitmap! : _layerManager.AnnotationBitmap!;

            activeGfx.SmoothingMode = SmoothingMode.AntiAlias;

            DrawShapeOnLayer(activeGfx, activeBmp, mode, startPt, endPt, rect);

            SaveAnnotationForPage?.Invoke(DrawingPageIndex);

            var imgW = activeBmp.Width;
            var imgH = activeBmp.Height;

            var strokePts = new List<float>
            {
                startPt.X / imgW, startPt.Y / imgH,
                endPt.X / imgW, endPt.Y / imgH
            };

            var stroke = new AnnotationStroke
            {
                Points = strokePts.ToArray(),
                ColorArgb = PenColor.ToArgb(),
                Thickness = PenWidth,
                ShapeType = mode.ToString(),
                DashStyle = IsDashed ? "Dash" : "Solid",
                PenType = PenType,
                StrokeStyle = StrokeStyle,
                AudioTimestampMs = GetCurrentAudioTimestampMs?.Invoke(),
                CreatedAt = DateTime.Now
            };

            PushStrokeToUndoStack?.Invoke(stroke);
            AddAnnotationStroke?.Invoke(strokePts.ToArray(), PenColor.ToArgb(), PenWidth, imgW, imgH, mode.ToString(), DrawingPageIndex, StrokeStyle);

            ResetDrawingState();
            return stroke;
        }

        /// <summary>在标注层上绘制形状，按工具模式分发到对应绘制方法</summary>
        private void DrawShapeOnLayer(Graphics activeGfx, Bitmap activeBmp, AnnotationToolMode mode, PointF startPt, PointF endPt, RectangleF rect)
        {
            using var drawPen = new Pen(PenColor, PenWidth);
            drawPen.StartCap = LineCap.Round;
            drawPen.EndCap = LineCap.Round;

            switch (mode)
            {
                case AnnotationToolMode.Rectangle:
                    DrawRectangleShape(activeGfx, drawPen, rect);
                    break;
                case AnnotationToolMode.Ellipse:
                    DrawEllipseShape(activeGfx, drawPen, rect);
                    break;
                case AnnotationToolMode.Arrow:
                    DrawArrowShape(activeGfx, drawPen, startPt, endPt);
                    break;
                case AnnotationToolMode.Mosaic:
                    ApplyMosaic(rect, 10, activeGfx, activeBmp);
                    break;
                case AnnotationToolMode.Checklist:
                    DrawChecklistShape(activeGfx, rect);
                    break;
                case AnnotationToolMode.ImageEmbed:
                    DrawImageEmbedShape(activeGfx, rect);
                    break;
            }
        }

        private void DrawRectangleShape(Graphics g, Pen pen, RectangleF rect)
        {
            if (IsDashed) pen.DashStyle = DashStyle.Dash;
            if (StrokeStyle == "DotLine") pen.DashStyle = DashStyle.Dot;
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        private void DrawEllipseShape(Graphics g, Pen pen, RectangleF rect)
        {
            if (IsDashed) pen.DashStyle = DashStyle.Dash;
            if (StrokeStyle == "DotLine") pen.DashStyle = DashStyle.Dot;
            g.DrawEllipse(pen, rect);
        }

        private void DrawArrowShape(Graphics g, Pen pen, PointF startPt, PointF endPt)
        {
            if (StrokeStyle == "ArrowLine") pen.EndCap = LineCap.ArrowAnchor;
            if (StrokeStyle == "DotLine") pen.DashStyle = DashStyle.Dot;
            g.DrawLine(pen, startPt, endPt);
        }

        private static void DrawChecklistShape(Graphics g, RectangleF rect)
        {
            var checkSize = Math.Min(rect.Width, rect.Height);
            if (checkSize < 10) checkSize = 20;
            using var checkBrush = new SolidBrush(Color.FromArgb(60, 0, 150, 0));
            g.FillRectangle(checkBrush, (int)rect.X, (int)rect.Y, (int)checkSize, (int)checkSize);
            using var checkPen = new Pen(Color.FromArgb(0, 150, 0), 2);
            g.DrawRectangle(checkPen, (int)rect.X, (int)rect.Y, (int)checkSize, (int)checkSize);
        }

        private void DrawImageEmbedShape(Graphics g, RectangleF rect)
        {
            using var ofd = new OpenFileDialog();
            ofd.Filter = "图片文件|*.png;*.jpg;*.jpeg;*.gif;*.bmp";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using var embedImg = Image.FromFile(ofd.FileName);
                    g.DrawImage(embedImg, (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to embed image");
                }
            }
        }

        #endregion

        #region 实时预览绘制

        /// <summary>绘制自由笔划的实时预览</summary>
        public void DrawStrokePreview(Graphics g, Rectangle imgRect, int pageIndex, Bitmap? srcImage, AnnotationToolMode mode)
        {
            if (CurrentStrokePoints == null || CurrentStrokePoints.Count < 2) return;
            if (pageIndex >= 0 && pageIndex != DrawingPageIndex) return;

            if (srcImage == null) return;

            var scaleX = (float)imgRect.Width / srcImage.Width;
            var scaleY = (float)imgRect.Height / srcImage.Height;

            var screenPoints = new List<Point>();
            foreach (var pt in CurrentStrokePoints)
            {
                screenPoints.Add(new Point(
                    (int)(pt.X * scaleX + imgRect.X),
                    (int)(pt.Y * scaleY + imgRect.Y)));
            }

            Color drawColor = mode == AnnotationToolMode.Strikethrough ? Color.Red : PenColor;
            float drawWidth = mode == AnnotationToolMode.Strikethrough ? 4f : PenWidth;

            using var pen = new Pen(drawColor, drawWidth);
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            if (StrokeStyle == "DotLine")
                pen.DashStyle = DashStyle.Dot;
            DrawSmoothCurve(g, pen, screenPoints.ToArray());
        }

        /// <summary>绘制形状的实时预览</summary>
        public void DrawShapePreview(Graphics g, Rectangle imgRect, int pageIndex, Bitmap? srcImage, AnnotationToolMode mode)
        {
            if (!ShapeStartPoint.HasValue || !ShapeEndPoint.HasValue) return;
            if (pageIndex >= 0 && pageIndex != DrawingPageIndex) return;

            if (srcImage == null) return;

            var scaleX = (float)imgRect.Width / srcImage.Width;
            var scaleY = (float)imgRect.Height / srcImage.Height;

            var startPt = ShapeStartPoint.Value;
            var endPt = ShapeEndPoint.Value;
            var screenStart = new Point(
                (int)(startPt.X * scaleX + imgRect.X),
                (int)(startPt.Y * scaleY + imgRect.Y));
            var screenEnd = new Point(
                (int)(endPt.X * scaleX + imgRect.X),
                (int)(endPt.Y * scaleY + imgRect.Y));

            var rect = new Rectangle(
                Math.Min(screenStart.X, screenEnd.X),
                Math.Min(screenStart.Y, screenEnd.Y),
                Math.Abs(screenEnd.X - screenStart.X),
                Math.Abs(screenEnd.Y - screenStart.Y));

            using var pen = new Pen(PenColor, PenWidth);
            if (IsDashed) pen.DashStyle = DashStyle.Dash;
            if (StrokeStyle == "DotLine") pen.DashStyle = DashStyle.Dot;
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;

            switch (mode)
            {
                case AnnotationToolMode.Highlight:
                    {
                        var highlightColor = Color.FromArgb(120, 255, 255, 0);
                        using var brush = new SolidBrush(highlightColor);
                        g.FillRectangle(brush, rect);
                        using var highlightPen = new Pen(Color.FromArgb(180, 255, 150, 0), 2);
                        g.DrawRectangle(highlightPen, rect);
                        break;
                    }
                case AnnotationToolMode.Rectangle:
                    g.DrawRectangle(pen, rect);
                    break;
                case AnnotationToolMode.Ellipse:
                    g.DrawEllipse(pen, rect);
                    break;
                case AnnotationToolMode.Arrow:
                    pen.EndCap = LineCap.ArrowAnchor;
                    g.DrawLine(pen, screenStart, screenEnd);
                    break;
                case AnnotationToolMode.Checklist:
                    {
                        var checkSize = Math.Min(rect.Width, rect.Height);
                        if (checkSize < 10) checkSize = 20;
                        using var checkBrush = new SolidBrush(Color.FromArgb(60, 0, 150, 0));
                        g.FillRectangle(checkBrush, new Rectangle(rect.Left, rect.Top, checkSize, checkSize));
                        using var checkPen = new Pen(Color.FromArgb(0, 150, 0), 2);
                        g.DrawRectangle(checkPen, new Rectangle(rect.Left, rect.Top, checkSize, checkSize));
                        break;
                    }
                case AnnotationToolMode.Mosaic:
                    {
                        using var mosaicBrush = new SolidBrush(Color.FromArgb(80, 255, 255, 255));
                        g.FillRectangle(mosaicBrush, rect);
                        if (IsDashed) pen.DashStyle = DashStyle.Dash;
                        g.DrawRectangle(pen, rect);
                        break;
                    }
            }
        }

        #endregion

        #region 马赛克效果

        public void ApplyMosaic(RectangleF rect, int blockSize)
        {
            if (_layerManager.AnnotationGraphics == null || _layerManager.AnnotationBitmap == null) return;
            ApplyMosaic(rect, blockSize, _layerManager.AnnotationGraphics, _layerManager.AnnotationBitmap);
        }

        private void ApplyMosaic(RectangleF rect, int blockSize, Graphics g, Bitmap bmp)
        {
            if (bmp == null || g == null) return;
            if (rect.Width <= 0 || rect.Height <= 0) return;

            try
            {
                int x = (int)Math.Max(0, rect.X);
                int y = (int)Math.Max(0, rect.Y);
                int w = (int)Math.Min(bmp.Width - x, rect.Width);
                int h = (int)Math.Min(bmp.Height - y, rect.Height);

                if (w <= 0 || h <= 0) return;

                using var mosaicPen = new SolidBrush(Color.FromArgb(220, 255, 255, 255));
                for (int blockY = y; blockY < y + h; blockY += blockSize)
                {
                    for (int blockX = x; blockX < x + w; blockX += blockSize)
                    {
                        int bw = Math.Min(blockSize, x + w - blockX);
                        int bh = Math.Min(blockSize, y + h - blockY);
                        g.FillRectangle(mosaicPen, blockX, blockY, bw, bh);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying mosaic");
            }
        }

        #endregion

        public void Dispose()
        {
            _drawingPen?.Dispose();
            _drawingPen = null;
        }
    }
}