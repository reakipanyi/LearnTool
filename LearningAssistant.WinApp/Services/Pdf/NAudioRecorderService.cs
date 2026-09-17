using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using NAudio.Wave;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Pdf
{
    public class NAudioRecorderService : IAudioRecorderService, IDisposable
    {
        private readonly ILogger<NAudioRecorderService> _logger;
        private WaveInEvent? _waveIn;
        private WaveFileWriter? _writer;
        private string? _tempFilePath;
        private AudioRecording? _currentRecording;
        private readonly System.Diagnostics.Stopwatch _stopwatch = new();
        private System.Windows.Forms.Timer? _positionTimer;
        private bool _disposed;

        private const int SampleRate = 44100;
        private const int Channels = 1;
        private const int PositionIntervalMs = 100;

        public AudioRecorderState State { get; private set; } = AudioRecorderState.Idle;

        public event EventHandler<AudioRecording>? RecordingCompleted;
        public event EventHandler<TimeSpan>? PositionChanged;
        public event EventHandler<float>? AudioLevelChanged;

        public TimeSpan CurrentPosition => _stopwatch.Elapsed;
        public long CurrentPositionMs => (long)_stopwatch.Elapsed.TotalMilliseconds;

        public NAudioRecorderService(ILogger<NAudioRecorderService> logger)
        {
            _logger = logger;
        }

        public void StartRecording(string pdfPath, int pageIndex, string pdfFileName)
        {
            if (State == AudioRecorderState.Recording)
                StopRecording();

            try
            {
                var recordingsDir = Path.Combine(AppPaths.AudioDir, "Recordings");
                AppPaths.EnsureDirectoryExists(recordingsDir);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var safeName = SanitizeFileName(pdfFileName);
                _tempFilePath = Path.Combine(recordingsDir, $"{safeName}_{timestamp}.wav");

                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(SampleRate, 16, Channels),
                    BufferMilliseconds = 50
                };

                _writer = new WaveFileWriter(_tempFilePath, _waveIn.WaveFormat);
                _waveIn.DataAvailable += OnDataAvailable;
                _waveIn.RecordingStopped += OnRecordingStopped;

                _currentRecording = new AudioRecording
                {
                    PdfPath = pdfPath,
                    PdfFileName = pdfFileName,
                    PageIndex = pageIndex,
                    FilePath = _tempFilePath,
                    CreatedAt = DateTime.Now
                };

                _stopwatch.Reset();
                _stopwatch.Start();
                State = AudioRecorderState.Recording;
                _waveIn.StartRecording();

                _positionTimer = new System.Windows.Forms.Timer { Interval = PositionIntervalMs };
                _positionTimer.Tick += OnPositionTimerTick;
                _positionTimer.Start();

                _logger.LogInformation("Audio recording started: {Path}", _tempFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start audio recording");
                Cleanup();
                throw;
            }
        }

        public void StopRecording()
        {
            if (State != AudioRecorderState.Recording)
                return;

            try
            {
                _waveIn?.StopRecording();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping recording");
                FinalizeRecording();
            }
        }

        public AudioRecording? GetCurrentRecording()
        {
            return _currentRecording;
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            _writer?.Write(e.Buffer, 0, e.BytesRecorded);

            if (AudioLevelChanged != null)
            {
                float level = CalculateAudioLevel(e.Buffer, e.BytesRecorded);
                AudioLevelChanged?.Invoke(this, level);
            }
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            FinalizeRecording();
        }

        private void OnPositionTimerTick(object? sender, EventArgs e)
        {
            if (State == AudioRecorderState.Recording)
            {
                PositionChanged?.Invoke(this, _stopwatch.Elapsed);
            }
        }

        private void FinalizeRecording()
        {
            _positionTimer?.Stop();
            _positionTimer?.Dispose();
            _positionTimer = null;

            _stopwatch.Stop();
            var durationMs = (int)_stopwatch.Elapsed.TotalMilliseconds;

            _writer?.Dispose();
            _writer = null;

            if (_currentRecording != null)
            {
                _currentRecording.DurationMs = durationMs;
            }

            State = AudioRecorderState.Idle;

            if (_currentRecording != null)
            {
                _logger.LogInformation("Recording completed: {Path}, Duration: {Duration}ms",
                    _currentRecording.FilePath, durationMs);
                RecordingCompleted?.Invoke(this, _currentRecording);
            }

            _waveIn?.Dispose();
            _waveIn = null;
        }

        private void Cleanup()
        {
            _positionTimer?.Stop();
            _positionTimer?.Dispose();
            _positionTimer = null;

            _writer?.Dispose();
            _writer = null;

            _waveIn?.Dispose();
            _waveIn = null;

            _stopwatch.Reset();
            State = AudioRecorderState.Idle;
            _currentRecording = null;
        }

        private static float CalculateAudioLevel(byte[] buffer, int bytesRecorded)
        {
            if (bytesRecorded < 2) return 0f;

            long sum = 0;
            int samples = bytesRecorded / 2;
            for (int i = 0; i < samples; i++)
            {
                short sample = (short)(buffer[i * 2] | (buffer[i * 2 + 1] << 8));
                sum += Math.Abs(sample);
            }
            float avg = (float)sum / samples;
            return Math.Min(avg / 32768f * 2f, 1f);
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder(name.Length);
            foreach (var c in name)
                sb.Append(invalid.Contains(c) ? '_' : c);
            return sb.ToString();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Cleanup();
        }
    }
}