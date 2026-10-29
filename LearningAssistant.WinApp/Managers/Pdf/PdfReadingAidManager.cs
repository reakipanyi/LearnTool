using System.Drawing;

namespace LearningAssistant.Managers
{
    /// <summary>护眼/色层预设。</summary>
    public enum ReadingOverlayPreset
    {
        /// <summary>关闭色层。</summary>
        None,
        /// <summary>暖黄护眼。</summary>
        Warm,
        /// <summary>豆绿护眼。</summary>
        Green,
        /// <summary>米色纸张。</summary>
        Sepia,
        /// <summary>冷蓝滤光。</summary>
        Cool
    }

    /// <summary>
    /// 阅读辅助（纯视图层）：护眼色层与阅读引导线。
    /// 只参与画布叠加绘制，不写入标注、不影响导出、OCR 与朗读取词。
    /// </summary>
    public sealed class PdfReadingAidManager
    {
        /// <summary>引导线亮带高度（像素）。</summary>
        public const int GuideBandHeight = 64;

        private static readonly (ReadingOverlayPreset Preset, string Name, Color Tint)[] Presets =
        {
            (ReadingOverlayPreset.None, "关闭", Color.Empty),
            (ReadingOverlayPreset.Warm, "暖黄护眼", Color.FromArgb(52, 255, 214, 140)),
            (ReadingOverlayPreset.Green, "豆绿护眼", Color.FromArgb(48, 199, 233, 192)),
            (ReadingOverlayPreset.Sepia, "米色纸张", Color.FromArgb(46, 238, 222, 184)),
            (ReadingOverlayPreset.Cool, "冷蓝滤光", Color.FromArgb(42, 205, 225, 255))
        };

        /// <summary>可选预设列表（供菜单构建）。</summary>
        public static IReadOnlyList<(ReadingOverlayPreset Preset, string Name)> Options { get; } =
            Presets.Select(p => (p.Preset, p.Name)).ToList();

        /// <summary>当前色层预设。</summary>
        public ReadingOverlayPreset Preset { get; private set; } = ReadingOverlayPreset.None;

        /// <summary>是否开启阅读引导线。</summary>
        public bool IsGuideEnabled { get; private set; }

        /// <summary>引导线亮带中心 Y（控件坐标）；未移动过时为 -1。</summary>
        public int GuideCenterY { get; private set; } = -1;

        /// <summary>是否会产生可见的叠加效果。</summary>
        public bool HasVisibleEffect => Preset != ReadingOverlayPreset.None || IsGuideEnabled;

        /// <summary>切换色层预设；返回是否发生变化。</summary>
        public bool SetPreset(ReadingOverlayPreset preset)
        {
            if (Preset == preset)
                return false;

            Preset = preset;
            return true;
        }

        /// <summary>开关阅读引导线；返回开启后的状态。</summary>
        public bool ToggleGuide()
        {
            IsGuideEnabled = !IsGuideEnabled;
            if (!IsGuideEnabled)
                GuideCenterY = -1;
            return IsGuideEnabled;
        }

        /// <summary>
        /// 按鼠标位置更新引导线中心；位置会限制在当前可见页面区域内。
        /// 返回 false 表示位置没有变化（无需重绘）。
        /// </summary>
        public bool UpdateGuidePosition(Point location, Rectangle pageArea)
        {
            if (!IsGuideEnabled || pageArea.Width <= 0 || pageArea.Height <= 0)
                return false;

            int half = GuideBandHeight / 2;
            int min = pageArea.Top + half;
            int max = pageArea.Bottom - half;
            if (max < min)
            {
                // 页面高度不足一个亮带时固定在中线
                min = max = (pageArea.Top + pageArea.Bottom) / 2;
            }

            int center = Math.Clamp(location.Y, min, max);
            if (center == GuideCenterY)
                return false;

            GuideCenterY = center;
            return true;
        }

        /// <summary>在指定页面矩形内绘制色层与引导线。</summary>
        public void Draw(Graphics g, Rectangle pageRect)
        {
            if (pageRect.Width <= 0 || pageRect.Height <= 0)
                return;

            if (Preset != ReadingOverlayPreset.None)
            {
                var tint = GetTint(Preset);
                if (tint.A > 0)
                {
                    using var brush = new SolidBrush(tint);
                    g.FillRectangle(brush, pageRect);
                }
            }

            if (!IsGuideEnabled || GuideCenterY < 0)
                return;

            int half = GuideBandHeight / 2;
            int top = GuideCenterY - half;
            int height = GuideBandHeight;

            // 亮带可能超出页面矩形：裁剪到页面内，避免压到页面外的空白区
            if (top < pageRect.Top)
            {
                height -= pageRect.Top - top;
                top = pageRect.Top;
            }
            if (top + height > pageRect.Bottom)
                height = pageRect.Bottom - top;
            if (height <= 0)
                return;

            int bandBottom = top + height;

            // 亮带外压暗，形成阅读尺效果
            using var dim = new SolidBrush(Color.FromArgb(76, 0, 0, 0));
            if (top > pageRect.Top)
                g.FillRectangle(dim, pageRect.Left, pageRect.Top, pageRect.Width, top - pageRect.Top);
            if (bandBottom < pageRect.Bottom)
                g.FillRectangle(dim, pageRect.Left, bandBottom, pageRect.Width, pageRect.Bottom - bandBottom);

            using var edge = new Pen(Color.FromArgb(150, 255, 196, 0), 1f);
            g.DrawLine(edge, pageRect.Left, top, pageRect.Right - 1, top);
            g.DrawLine(edge, pageRect.Left, bandBottom - 1, pageRect.Right - 1, bandBottom - 1);
        }

        private static Color GetTint(ReadingOverlayPreset preset)
        {
            foreach (var item in Presets)
            {
                if (item.Preset == preset)
                    return item.Tint;
            }
            return Color.Empty;
        }
    }
}
