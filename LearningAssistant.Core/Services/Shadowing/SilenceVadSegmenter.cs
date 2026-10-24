using System;
using System.Collections.Generic;

namespace LearningAssistant.Services.Shadowing
{
    /// <summary>能量 VAD 断句参数。</summary>
    public sealed class SilenceVadOptions
    {
        /// <summary>分析帧长（毫秒），越小边界越细、计算量越大</summary>
        public int FrameMilliseconds { get; set; } = 20;

        /// <summary>短于该时长的语音片段会被丢弃</summary>
        public int MinSegmentMilliseconds { get; set; } = 250;

        /// <summary>静音持续该时长才判定句子结束</summary>
        public int MinSilenceMilliseconds { get; set; } = 280;

        /// <summary>切片前后各留白的时长，避免切掉起音/尾音</summary>
        public int PaddingMilliseconds { get; set; } = 120;

        /// <summary>语音阈值相对噪声底的最小倍数</summary>
        public double NoiseMultiplier { get; set; } = 2.5;

        /// <summary>绝对 RMS 下限，防止纯静音文件被噪声抖动触发</summary>
        public double MinAbsoluteRms { get; set; } = 0.0005;
    }

    /// <summary>VAD 给出的一段语音区间（毫秒，含留白）。</summary>
    public sealed class VadSegment
    {
        public VadSegment(int startMs, int endMs)
        {
            StartMs = startMs;
            EndMs = endMs;
        }

        public int StartMs { get; }
        public int EndMs { get; }
        public int DurationMs => EndMs - StartMs;
    }

    /// <summary>
    /// 流式能量 VAD：逐块喂入单声道采样，完成时按帧能量给出语音区间。
    /// 只保存每帧的 RMS，不保留整段音频，适合长音频与在线分析。
    /// </summary>
    public sealed class SilenceVadAnalyzer
    {
        private readonly SilenceVadOptions _options;
        private readonly int _frameMilliseconds;
        private readonly int _frameSize;
        private readonly float[] _frame;
        private readonly List<double> _frameRms = new();
        private int _fill;

        public SilenceVadAnalyzer(int sampleRate, SilenceVadOptions? options = null)
        {
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));

            _options = options ?? new SilenceVadOptions();
            _frameMilliseconds = Math.Max(5, _options.FrameMilliseconds);
            _frameSize = Math.Max(1, sampleRate * _frameMilliseconds / 1000);
            _frame = new float[_frameSize];
        }

        /// <summary>已分析的整帧数，用于估算进度。</summary>
        public int FrameCount => _frameRms.Count;

        /// <summary>喂入单声道采样；不足一帧的尾部数据保留到下次。</summary>
        public void Push(ReadOnlySpan<float> samples)
        {
            foreach (float sample in samples)
            {
                _frame[_fill++] = sample;
                if (_fill < _frameSize) continue;

                _frameRms.Add(ComputeRms(_frame));
                _fill = 0;
            }
        }

        /// <summary>结束分析并返回语音区间（已按参数留白、截断、去掉过短片段）。</summary>
        public IReadOnlyList<VadSegment> Complete()
        {
            if (_frameRms.Count == 0) return Array.Empty<VadSegment>();

            double enter = ComputeEnterThreshold();
            double exit = enter * 0.6;
            int minSilenceFrames = Math.Max(1, (int)Math.Ceiling(_options.MinSilenceMilliseconds / (double)_frameMilliseconds));
            int totalMs = _frameRms.Count * _frameMilliseconds;

            var segments = new List<VadSegment>();
            int? startFrame = null;
            int lastSpeechFrame = -1;
            int silenceRun = 0;

            for (int i = 0; i < _frameRms.Count; i++)
            {
                double threshold = startFrame == null ? enter : exit;
                if (_frameRms[i] >= threshold)
                {
                    startFrame ??= i;
                    lastSpeechFrame = i;
                    silenceRun = 0;
                    continue;
                }

                if (startFrame == null) continue;
                silenceRun++;
                if (silenceRun < minSilenceFrames) continue;

                AppendSegment(segments, startFrame.Value, lastSpeechFrame + 1, totalMs);
                startFrame = null;
                lastSpeechFrame = -1;
                silenceRun = 0;
            }

            if (startFrame != null)
                AppendSegment(segments, startFrame.Value, lastSpeechFrame + 1, totalMs);

            return segments;
        }

        private void AppendSegment(List<VadSegment> segments, int startFrame, int endFrame, int totalMs)
        {
            int rawStart = startFrame * _frameMilliseconds;
            int rawEnd = endFrame * _frameMilliseconds;
            if (rawEnd - rawStart < _options.MinSegmentMilliseconds) return;

            int padding = Math.Max(0, _options.PaddingMilliseconds);
            int start = Math.Max(0, rawStart - padding);
            int end = Math.Min(totalMs, rawEnd + padding);
            if (end <= start) return;

            segments.Add(new VadSegment(start, end));
        }

        private double ComputeEnterThreshold()
        {
            var sorted = new double[_frameRms.Count];
            _frameRms.CopyTo(sorted);
            Array.Sort(sorted);

            double noise = sorted[(int)(sorted.Length * 0.1)];
            double speechRef = sorted[(int)(sorted.Length * 0.9)];

            // 动态范围太小（整段都是人声或整段都是静音）时按噪声的一半判定
            if (speechRef <= noise * 1.5)
                return Math.Max(noise * 0.5, _options.MinAbsoluteRms);

            double relative = Math.Max(noise * _options.NoiseMultiplier, speechRef * 0.08);
            return Math.Max(relative, _options.MinAbsoluteRms);
        }

        private static double ComputeRms(float[] frame)
        {
            double sum = 0;
            foreach (float sample in frame)
                sum += sample * sample;
            return Math.Sqrt(sum / frame.Length);
        }
    }
}
