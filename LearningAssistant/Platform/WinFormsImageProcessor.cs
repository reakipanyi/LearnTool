using System.Drawing;
using System.Drawing.Imaging;
using LearningAssistant.Abstractions;
using LearningAssistant.Services.Pdf;

namespace LearningAssistant.Platform
{
    public class WinFormsImageProcessor : IImageProcessor
    {
        public byte[]? CropImage(byte[] imageData, RectInt region)
        {
            try
            {
                using var bmp = BytesToBitmap(imageData);
                if (bmp == null) return null;

                var rect = new Rectangle(region.X, region.Y, region.Width, region.Height);
                using var cropped = new Bitmap(rect.Width, rect.Height);
                using (var g = Graphics.FromImage(cropped))
                {
                    g.DrawImage(bmp, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
                }
                return BitmapToBytes(cropped);
            }
            catch
            {
                return null;
            }
        }

        public (int Width, int Height) GetImageSize(byte[] imageData)
        {
            try
            {
                using var bmp = BytesToBitmap(imageData);
                if (bmp == null) return (0, 0);
                return (bmp.Width, bmp.Height);
            }
            catch
            {
                return (0, 0);
            }
        }

        public float GetDpiScaleFactor()
        {
            try
            {
                using var g = Graphics.FromHwnd(IntPtr.Zero);
                return g.DpiX / 96f;
            }
            catch
            {
                return 1.5f;
            }
        }

        private static byte[]? BitmapToBytes(Bitmap? bmp)
        {
            if (bmp == null) return null;
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }

        private static Bitmap? BytesToBitmap(byte[]? data)
        {
            if (data == null || data.Length == 0) return null;
            using var ms = new MemoryStream(data);
            return new Bitmap(ms);
        }
    }
}
