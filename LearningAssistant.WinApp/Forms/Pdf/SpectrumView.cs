using LearningAssistant.Common.UI;
using NAudio.Dsp;

namespace LearningAssistant.Forms.Pdf;

/// <summary>
/// 实时频谱视图：对最近 2048 个单声道采样做 FFT，绘制 64 段对数频率柱。
/// 只在可见期间启动刷新，隐藏后停止以节省 CPU。
/// </summary>
internal sealed class SpectrumView : Control
{
    private const int FftSize = 2048;
    private const int FftOrder = 11;
    private const int BarCount = 64;
    private const double MinFreq = 60;
    private const double MaxFreq = 16000;
    private const double DbFloor = -80;

    private readonly float[] _samples = new float[FftSize];
    private readonly Complex[] _fft = new Complex[FftSize];
    private readonly float[] _window = new float[FftSize];
    private readonly float[] _levels = new float[BarCount];
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 33 };
    private int _sampleRate;
    private Func<float[], int>? _captureSamples;

    /// <summary>设置取样回调：填充目标缓冲并返回采样率，0 表示暂无数据。</summary>
    public void SetSampleProvider(Func<float[], int> capture) => _captureSamples = capture;

    public SpectrumView()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        for (int i = 0; i < FftSize; i++)
            _window[i] = (float)(0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (FftSize - 1)));
        _timer.Tick += (_, _) => RefreshFrame();
    }

    public void Start()
    {
        if (!_timer.Enabled) _timer.Start();
        RefreshFrame();
    }

    public void Stop() => _timer.Stop();

    private void RefreshFrame()
    {
        int sampleRate = _captureSamples?.Invoke(_samples) ?? 0;
        if (sampleRate > 0)
        {
            _sampleRate = sampleRate;
            ComputeLevels(sampleRate);
        }
        else
        {
            for (int i = 0; i < BarCount; i++)
                _levels[i] = Math.Max(0f, _levels[i] - 0.08f);
        }
        Invalidate();
    }

    private void ComputeLevels(int sampleRate)
    {
        for (int i = 0; i < FftSize; i++)
        {
            _fft[i].X = _samples[i] * _window[i];
            _fft[i].Y = 0;
        }
        FastFourierTransform.FFT(true, FftOrder, _fft);

        double top = Math.Min(MaxFreq, sampleRate / 2.0);
        double binHz = sampleRate / (double)FftSize;
        double ratio = top / MinFreq;

        for (int b = 0; b < BarCount; b++)
        {
            // 对数分频：低频段更细，符合听感
            double lo = MinFreq * Math.Pow(ratio, b / (double)BarCount);
            double hi = MinFreq * Math.Pow(ratio, (b + 1) / (double)BarCount);
            int from = Math.Max(1, (int)Math.Floor(lo / binHz));
            int to = Math.Min(FftSize / 2, Math.Max(from + 1, (int)Math.Ceiling(hi / binHz)));

            double peak = 0;
            for (int k = from; k < to; k++)
            {
                double mag = Math.Sqrt(_fft[k].X * _fft[k].X + _fft[k].Y * _fft[k].Y);
                if (mag > peak) peak = mag;
            }

            // 归一化到 0dB 满幅正弦，再映射到 -80dB..0dB
            double db = 20 * Math.Log10(peak / (FftSize / 2.0) + 1e-9);
            float level = (float)Math.Clamp((db - DbFloor) / -DbFloor, 0, 1);
            _levels[b] = Math.Max(level, _levels[b] - 0.07f);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var palette = PdfSidebarTheme.PaletteFor(this);
        e.Graphics.Clear(BackColor);

        float area = Height - 15;
        using var gridPen = new Pen(palette.Border);
        for (int i = 1; i <= 2; i++)
        {
            float y = area * i / 3f;
            e.Graphics.DrawLine(gridPen, 0, y, Width, y);
        }
        e.Graphics.DrawLine(gridPen, 0, area, Width, area);

        float gap = 2f;
        float barWidth = Math.Max(1f, (Width - gap * (BarCount - 1)) / BarCount);
        using var barBrush = new SolidBrush(PdfSidebarTheme.Brand);
        using var capBrush = new SolidBrush(palette.BrandSoft);
        for (int b = 0; b < BarCount; b++)
        {
            float height = Math.Max(1f, _levels[b] * area);
            float x = b * (barWidth + gap);
            e.Graphics.FillRectangle(barBrush, x, area - height, barWidth, height);
            e.Graphics.FillRectangle(capBrush, x, area - height, barWidth, Math.Min(2f, height));
        }

        using var font = new Font("Segoe UI", 7F);
        double topFreq = _sampleRate > 0 ? Math.Min(MaxFreq, _sampleRate / 2.0) : MaxFreq;
        TextRenderer.DrawText(e.Graphics, "60Hz", font, new Point(0, (int)area + 1), palette.Muted);
        TextRenderer.DrawText(e.Graphics, $"{FormatFreq(topFreq / 2)}", font,
            new Point(Width / 2 - 16, (int)area + 1), palette.Muted);
        string right = FormatFreq(topFreq);
        int rightWidth = TextRenderer.MeasureText(right, font).Width;
        TextRenderer.DrawText(e.Graphics, right, font, new Point(Width - rightWidth, (int)area + 1), palette.Muted);
    }

    private static string FormatFreq(double freq)
        => freq >= 1000 ? $"{freq / 1000:0.#}k" : $"{freq:0}Hz";

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Stop();
            _timer.Dispose();
        }
        base.Dispose(disposing);
    }
}
