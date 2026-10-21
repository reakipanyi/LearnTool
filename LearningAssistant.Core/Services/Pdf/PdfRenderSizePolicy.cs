using System;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF 渲染尺寸策略：尺寸量化 + 硬上限。
    /// 缩放路径传入的尺寸可能极大（400% 时 ClientSize×4 可达 6000×9600，ARGB 约 245MB），
    /// 这里统一收敛到安全区间，避免单张位图过大导致内存尖峰。
    /// 纯静态逻辑，便于单元测试。
    /// </summary>
    public static class PdfRenderSizePolicy
    {
        /// <summary>单张渲染位图宽度硬上限（像素）</summary>
        public const int MaxRenderWidth = 2400;

        /// <summary>单张渲染位图高度硬上限（像素）</summary>
        public const int MaxRenderHeight = 3200;

        // 尺寸量化档位：向上对齐到 64 像素整数倍。
        // 渲染缓存 key 含尺寸，客户区或 DPI 的 1px 抖动不应让整组缓存失效。
        private const int QuantizeStep = 64;

        // 缩略图等小尺寸不参与量化，避免把 100×140 的缩略图放大
        private const int QuantizeMinSize = 512;

        /// <summary>
        /// 对请求的渲染尺寸执行量化与硬上限约束，保持宽高比不变形。
        /// </summary>
        public static (int Width, int Height) Apply(int width, int height)
        {
            width = Math.Max(1, width);
            height = Math.Max(1, height);

            width = Quantize(width);
            height = Quantize(height);

            // 硬上限：按较小比例等比缩小，保证页面不变形
            double scale = 1.0;
            if (width > MaxRenderWidth)
                scale = Math.Min(scale, (double)MaxRenderWidth / width);
            if (height > MaxRenderHeight)
                scale = Math.Min(scale, (double)MaxRenderHeight / height);

            if (scale < 1.0)
            {
                width = Math.Max(1, (int)Math.Floor(width * scale));
                height = Math.Max(1, (int)Math.Floor(height * scale));
            }

            return (width, height);
        }

        private static int Quantize(int value)
        {
            if (value < QuantizeMinSize)
                return value;

            return (value + QuantizeStep - 1) / QuantizeStep * QuantizeStep;
        }
    }
}
