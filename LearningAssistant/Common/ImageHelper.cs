using System.Drawing.Drawing2D;

namespace LearningAssistant.Common
{
    public static class ImageHelper
    {
        public static Bitmap CropImage(Bitmap source, Rectangle region)
        {
            var cropped = new Bitmap(region.Width, region.Height);
            using var graphics = Graphics.FromImage(cropped);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.DrawImage(source, new Rectangle(0, 0, region.Width, region.Height),
                              region, GraphicsUnit.Pixel);
            return cropped;
        }
    }
}
