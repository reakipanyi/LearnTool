using LearningAssistant.Common.UI;
using NAudio.Wave;

namespace LearningAssistant.Forms.Pdf
{
    /// <summary>
    /// 跟读 take 的静态波形缩略图：整段峰值降采样后绘制成柱状，不做实时滚动。
    /// </summary>
    public sealed class WaveformThumb : Control
    {
        private const int BucketCount = 64;
        private float[] _peaks = Array.Empty<float>();

        public WaveformThumb()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                     | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Height = 42;
            Visible = false;
        }

        public bool HasData => _peaks.Length > 0;
        public string? LoadedPath { get; private set; }

        public void LoadFromFile(string? path)
        {
            LoadedPath = path;
            _peaks = string.IsNullOrWhiteSpace(path) || !File.Exists(path)
                ? Array.Empty<float>()
                : ComputePeaks(path);
            Visible = _peaks.Length > 0;
            Invalidate();
        }

        public void Clear() => LoadFromFile(null);

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_peaks.Length == 0)
                return;

            var g = e.Graphics;
            int width = Math.Max(1, ClientSize.Width - 4);
            int height = Math.Max(1, ClientSize.Height - 6);
            float barWidth = Math.Max(1f, width / (float)_peaks.Length);
            using var brush = new SolidBrush(PdfSidebarTheme.Brand);
            float mid = 3 + height / 2f;

            for (int i = 0; i < _peaks.Length; i++)
            {
                float amplitude = Math.Max(0.04f, _peaks[i]) * (height / 2f);
                float x = 2 + i * barWidth;
                float barHeight = Math.Max(1.5f, amplitude * 2f);
                g.FillRectangle(brush, x, mid - barHeight / 2f, Math.Max(1f, barWidth - 1f), barHeight);
            }
        }

        /// <summary>读取 16bit PCM 录音，按桶取最大绝对值作为峰值；格式不符时返回空。</summary>
        private static float[] ComputePeaks(string path)
        {
            try
            {
                using var reader = new WaveFileReader(path);
                if (reader.WaveFormat.BitsPerSample != 16 || reader.WaveFormat.Channels != 1)
                {
                    // 立体声/非 16bit：先转单声道 16bit 再取峰值，避免直接按字节解析出错
                    using var converted = new WaveFormatConversionStream(
                        new WaveFormat(44100, 16, 1), reader);
                    return ComputePeaks(converted);
                }
                return ComputePeaks(reader);
            }
            catch
            {
                return Array.Empty<float>();
            }
        }

        private static float[] ComputePeaks(WaveStream stream)
        {
            long samples = stream.Length / Math.Max(1, stream.WaveFormat.BlockAlign);
            if (samples <= 0)
                return Array.Empty<float>();

            int buckets = BucketCount;
            var peaks = new float[buckets];
            long perBucket = Math.Max(1, samples / buckets);
            var buffer = new byte[perBucket * 2];
            long bucket = 0;
            while (bucket < buckets)
            {
                int bytesPerBucket = (int)Math.Min(perBucket, samples - bucket * perBucket) * 2;
                if (bytesPerBucket <= 0)
                    break;
                int read = stream.Read(buffer, 0, bytesPerBucket);
                if (read <= 0)
                    break;

                short max = 0;
                for (int i = 0; i + 1 < read; i += 2)
                {
                    short value = (short)(buffer[i] | (buffer[i + 1] << 8));
                    short abs = value == short.MinValue ? short.MaxValue : Math.Abs(value);
                    if (abs > max) max = abs;
                }
                peaks[bucket] = max / (float)short.MaxValue;
                bucket++;
            }
            return peaks;
        }
    }
}
