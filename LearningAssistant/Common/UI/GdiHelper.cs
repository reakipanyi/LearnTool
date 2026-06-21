using System.Drawing.Drawing2D;

namespace LearningAssistant.Common.UI
{
    /// <summary>
    /// GDI+ 绘制辅助类
    /// 提供常用的图形绘制扩展方法
    /// </summary>
    public static class GdiHelper
    {
        /// <summary>
        /// 创建圆角矩形路径
        /// </summary>
        public static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;

            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;

            Rectangle arcRect = new(rect.Location, new Size(diameter, diameter));

            path.AddArc(arcRect, 180, 90);

            arcRect.X = rect.Right - diameter;
            path.AddArc(arcRect, 270, 90);

            arcRect.Y = rect.Bottom - diameter;
            path.AddArc(arcRect, 0, 90);

            arcRect.X = rect.Left;
            path.AddArc(arcRect, 90, 90);

            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// 绘制圆角矩形
        /// </summary>
        public static void DrawRoundedRect(this Graphics g, Rectangle rect, int radius, Pen pen)
        {
            using var path = CreateRoundedRectPath(rect, radius);
            g.DrawPath(pen, path);
        }

        /// <summary>
        /// 填充圆角矩形
        /// </summary>
        public static void FillRoundedRect(this Graphics g, Rectangle rect, int radius, Brush brush)
        {
            using var path = CreateRoundedRectPath(rect, radius);
            g.FillPath(brush, path);
        }

        /// <summary>
        /// 绘制发光效果（多层透明叠加）
        /// </summary>
        public static void DrawGlow(this Graphics g, Rectangle rect, int radius, Color glowColor, int glowSize = 8, int layers = 4)
        {
            for (int i = layers; i > 0; i--)
            {
                int offset = glowSize * (layers - i + 1) / layers;
                int alpha = glowColor.A / (i + 1);
                var glowRect = new Rectangle(
                    rect.X - offset,
                    rect.Y - offset,
                    rect.Width + offset * 2,
                    rect.Height + offset * 2);

                using var path = CreateRoundedRectPath(glowRect, radius + offset);
                using var brush = new SolidBrush(Color.FromArgb(alpha, glowColor));
                g.FillPath(brush, path);
            }
        }

        /// <summary>
        /// 绘制带渐变的进度条
        /// </summary>
        public static void DrawGradientProgressBar(
            this Graphics g,
            Rectangle bgRect,
            int radius,
            double progressPercent,
            Color startColor,
            Color endColor,
            Color? bgColor = null)
        {
            var actualBgColor = bgColor ?? Color.FromArgb(240, 240, 245);

            using (var bgPath = CreateRoundedRectPath(bgRect, radius))
            using (var bgBrush = new SolidBrush(actualBgColor))
            {
                g.FillPath(bgBrush, bgPath);
            }

            if (progressPercent <= 0) return;

            int progressWidth = (int)(bgRect.Width * Math.Min(progressPercent, 1.0));
            if (progressWidth < radius * 2)
                progressWidth = radius * 2;

            var progressRect = new Rectangle(bgRect.X, bgRect.Y, progressWidth, bgRect.Height);

            using var progressPath = CreateRoundedRectPath(progressRect, radius);
            using var progressBrush = new LinearGradientBrush(
                progressRect, startColor, endColor, LinearGradientMode.Horizontal);
            g.FillPath(progressBrush, progressPath);
        }

        /// <summary>
        /// 绘制阴影
        /// </summary>
        public static void DrawShadow(this Graphics g, Rectangle rect, int radius, int shadowOffset = 4, int shadowAlpha = 25)
        {
            var shadowRect = new Rectangle(
                rect.X + shadowOffset,
                rect.Y + shadowOffset,
                rect.Width,
                rect.Height);

            using var shadowPath = CreateRoundedRectPath(shadowRect, radius);
            using var shadowBrush = new SolidBrush(Color.FromArgb(shadowAlpha, Color.Black));
            g.FillPath(shadowBrush, shadowPath);
        }

        /// <summary>
        /// 调整颜色的透明度
        /// </summary>
        public static Color WithAlpha(this Color color, int alpha)
        {
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        /// <summary>
        /// 调整颜色的亮度
        /// </summary>
        public static Color Lighten(this Color color, double amount)
        {
            int r = Math.Min(255, (int)(color.R + (255 - color.R) * amount));
            int g = Math.Min(255, (int)(color.G + (255 - color.G) * amount));
            int b = Math.Min(255, (int)(color.B + (255 - color.B) * amount));
            return Color.FromArgb(color.A, r, g, b);
        }

        /// <summary>
        /// 调整颜色的暗度
        /// </summary>
        public static Color Darken(this Color color, double amount)
        {
            int r = Math.Max(0, (int)(color.R * (1 - amount)));
            int g = Math.Max(0, (int)(color.G * (1 - amount)));
            int b = Math.Max(0, (int)(color.B * (1 - amount)));
            return Color.FromArgb(color.A, r, g, b);
        }

        /// <summary>
        /// 在指定区域绘制垂直居中文本
        /// </summary>
        public static void DrawVertCenteredString(
            this Graphics g,
            string text,
            Font font,
            Brush brush,
            Rectangle rect,
            StringAlignment alignment = StringAlignment.Near)
        {
            var sf = new StringFormat
            {
                LineAlignment = StringAlignment.Center,
                Alignment = alignment,
                Trimming = StringTrimming.EllipsisCharacter
            };
            g.DrawString(text, font, brush, rect, sf);
        }
    }
}
