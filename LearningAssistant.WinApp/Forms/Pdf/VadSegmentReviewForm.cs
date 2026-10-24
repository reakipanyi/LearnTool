using System.Globalization;
using LearningAssistant.Abstractions.Media;
using LearningAssistant.Common.UI;
using LearningAssistant.Models.Shadowing;
using LearningAssistant.Platform.Media;
using LearningAssistant.Services.Media;
using LearningAssistant.Services.Shadowing;

namespace LearningAssistant.Forms.Pdf;

/// <summary>
/// 自动断句建议预览：分析当前曲目后列出建议切片，可勾选/改起止/试听，
/// 确认后切片并写入本页 segments.json。已存在的手动切片不受影响。
/// </summary>
public sealed class VadSegmentReviewForm : Form
{
    private const string UseColumn = "Use";
    private const string IndexColumn = "Index";
    private const string StartColumn = "Start";
    private const string EndColumn = "End";
    private const string DurationColumn = "Duration";

    private readonly string _pdfPath;
    private readonly int _pageIndex;
    private readonly string _trackPath;
    private readonly double _captureSpeed;
    private readonly ShadowingSegmentStore _store = new();
    private readonly AudioSliceService _slicer = new();
    private readonly VadSegmentService _vad = new();
    private readonly DataGridView _grid = new();
    private readonly Label _labelTitle = new();
    private readonly Label _labelInfo = new();
    private readonly Label _labelStatus = new();
    private readonly ProgressBar _progress = new();
    private readonly Button _btnPreview = new();
    private readonly Button _btnSave = new();
    private readonly Button _btnCancel = new();
    private readonly System.Windows.Forms.Timer _previewTimer = new() { Interval = 120 };
    private CancellationTokenSource? _cts;
    private NAudioMediaPlayer? _previewPlayer;
    private int _previewEndMs;
    private bool _previewing;

    /// <summary>保存成功后生成的切片数。</summary>
    public int SavedSegmentCount { get; private set; }

    public VadSegmentReviewForm(string pdfPath, int pageIndex, string trackPath, double captureSpeed, bool isDark = false)
    {
        _pdfPath = pdfPath;
        _pageIndex = pageIndex;
        _trackPath = trackPath;
        _captureSpeed = captureSpeed;

        var palette = new PdfSidebarPalette(isDark);
        Text = "自动断句 · 建议切片";
        Size = new Size(780, 560);
        MinimumSize = new Size(640, 420);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = palette.Background;
        Font = new Font("Microsoft YaHei UI", 9F);
        BuildLayout(palette);

        Shown += async (_, _) => await AnalyzeAsync();
        FormClosed += (_, _) => Cleanup();
        _previewTimer.Tick += OnPreviewTimerTick;
    }

    private void BuildLayout(PdfSidebarPalette palette)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = palette.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = palette.Background };
        _labelTitle.Text = $"第 {_pageIndex + 1} 页 · 自动断句建议";
        _labelTitle.AutoSize = true;
        _labelTitle.Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold);
        _labelTitle.ForeColor = palette.Text;
        _labelTitle.Location = new Point(0, 0);
        _labelInfo.Text = Path.GetFileName(_trackPath);
        _labelInfo.AutoSize = true;
        _labelInfo.ForeColor = palette.Muted;
        _labelInfo.Location = new Point(2, 30);
        header.Controls.Add(_labelTitle);
        header.Controls.Add(_labelInfo);

        _grid.Dock = DockStyle.Fill;
        _grid.BackgroundColor = palette.Surface;
        _grid.BorderStyle = BorderStyle.None;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        _grid.EnableHeadersVisualStyles = false;
        _grid.GridColor = palette.Border;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = palette.SurfaceAlt;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = palette.Text;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _grid.DefaultCellStyle.BackColor = palette.Surface;
        _grid.DefaultCellStyle.ForeColor = palette.Text;
        _grid.DefaultCellStyle.SelectionBackColor = palette.BrandSoft;
        _grid.DefaultCellStyle.SelectionForeColor = palette.Text;
        _grid.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F);

        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = UseColumn,
            HeaderText = "采用",
            Width = 52,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = IndexColumn,
            HeaderText = "#",
            Width = 44,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = StartColumn,
            HeaderText = "开始 (秒)",
            Width = 100,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = EndColumn,
            HeaderText = "结束 (秒)",
            Width = 100,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = DurationColumn,
            HeaderText = "时长 (秒)",
            Width = 90,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Note",
            HeaderText = "说明",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.CellValueChanged += OnCellValueChanged;

        var progressPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = palette.Background,
            Margin = new Padding(0, 4, 0, 0)
        };
        progressPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        progressPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
        _labelStatus.Text = "准备分析…";
        _labelStatus.AutoSize = true;
        _labelStatus.ForeColor = palette.Muted;
        _labelStatus.Dock = DockStyle.Fill;
        _progress.Dock = DockStyle.Fill;
        _progress.Style = ProgressBarStyle.Continuous;
        _progress.Maximum = 1000;
        progressPanel.Controls.Add(_labelStatus, 0, 0);
        progressPanel.Controls.Add(_progress, 0, 1);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = palette.Background,
            Padding = new Padding(0, 6, 0, 0)
        };
        StyleButton(_btnCancel, "取消", palette);
        StyleButton(_btnSave, "保存并切片", palette, primary: true);
        StyleButton(_btnPreview, "试听选中", palette);
        _btnSave.Enabled = false;
        _btnPreview.Enabled = false;
        _btnCancel.Click += (_, _) => Close();
        _btnPreview.Click += (_, _) => TogglePreview();
        _btnSave.Click += async (_, _) => await SaveAsync();
        footer.Controls.Add(_btnCancel);
        footer.Controls.Add(_btnSave);
        footer.Controls.Add(_btnPreview);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(_grid, 0, 1);
        root.Controls.Add(progressPanel, 0, 2);
        root.Controls.Add(footer, 0, 3);
        Controls.Add(root);
    }

    private static void StyleButton(Button button, string text, PdfSidebarPalette palette, bool primary = false)
    {
        button.Text = text;
        button.AutoSize = true;
        button.Height = 30;
        button.Padding = new Padding(12, 0, 12, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = palette.Border;
        button.BackColor = primary ? PdfSidebarTheme.Brand : palette.Surface;
        button.ForeColor = primary ? Color.White : palette.Text;
        button.Cursor = Cursors.Hand;
        button.Margin = new Padding(6, 0, 0, 0);
    }

    private async Task AnalyzeAsync()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        _btnSave.Enabled = false;
        _btnPreview.Enabled = false;
        _progress.Value = 0;
        _labelStatus.Text = "正在分析音频…";

        var progress = new Progress<double>(p =>
        {
            if (IsDisposed) return;
            _progress.Value = (int)Math.Clamp(p * 1000, 0, 1000);
            _labelStatus.Text = $"正在分析音频… {p * 100:0}%";
        });

        try
        {
            var segments = await _vad.AnalyzeAsync(_trackPath, progress, _cts.Token);
            if (IsDisposed) return;
            FillRows(segments);
            _progress.Value = 1000;
            _labelStatus.Text = segments.Count > 0
                ? $"共 {segments.Count} 段建议 · 可取消勾选跳过，或直接修改起止秒数"
                : "没有检测到语音片段：可能整段静音或噪声过大";
            _btnSave.Enabled = segments.Count > 0;
            _btnPreview.Enabled = segments.Count > 0;
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed) _labelStatus.Text = "已取消分析";
        }
        catch (Exception ex)
        {
            if (!IsDisposed) _labelStatus.Text = $"分析失败：{ex.Message}";
        }
    }

    private void FillRows(IReadOnlyList<VadSegment> segments)
    {
        _grid.Rows.Clear();
        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            _grid.Rows.Add(
                true,
                i + 1,
                FormatSeconds(segment.StartMs),
                FormatSeconds(segment.EndMs),
                FormatSeconds(segment.DurationMs),
                "VAD 建议");
        }
    }

    private void OnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (e.ColumnIndex != _grid.Columns[StartColumn].Index && e.ColumnIndex != _grid.Columns[EndColumn].Index)
            return;

        var row = _grid.Rows[e.RowIndex];
        if (!TryParseSeconds(row.Cells[StartColumn].Value, out int start)
            || !TryParseSeconds(row.Cells[EndColumn].Value, out int end)
            || end <= start)
        {
            row.Cells[DurationColumn].Value = "—";
            return;
        }
        row.Cells[DurationColumn].Value = FormatSeconds(end - start);
    }

    private void TogglePreview()
    {
        if (_previewing)
        {
            StopPreview();
            return;
        }

        if (_grid.CurrentRow == null)
            return;
        var row = _grid.CurrentRow;
        if (!TryParseSeconds(row.Cells[StartColumn].Value, out int start)
            || !TryParseSeconds(row.Cells[EndColumn].Value, out int end)
            || end <= start)
        {
            _labelStatus.Text = "当前行起止时间无效，无法试听";
            return;
        }

        _previewPlayer ??= CreatePreviewPlayer();
        if (_previewPlayer == null)
        {
            _labelStatus.Text = "音频播放组件不可用，无法试听";
            return;
        }

        _previewPlayer.Seek(TimeSpan.FromMilliseconds(start));
        _previewEndMs = end;
        _previewPlayer.PlayAsync();
        _previewing = true;
        _btnPreview.Text = "停止试听";
        _previewTimer.Start();
    }

    private NAudioMediaPlayer? CreatePreviewPlayer()
    {
        var parser = Program.ServiceProvider?.GetService(typeof(ISubtitleParser)) as ISubtitleParser;
        if (parser == null) return null;

        var player = new NAudioMediaPlayer(parser);
        player.Load(_trackPath);
        return player;
    }

    private void OnPreviewTimerTick(object? sender, EventArgs e)
    {
        if (!_previewing) return;
        if (_previewPlayer == null || _previewPlayer.Position.TotalMilliseconds >= _previewEndMs)
            StopPreview();
    }

    private void StopPreview()
    {
        _previewTimer.Stop();
        if (_previewPlayer != null && _previewPlayer.State == MediaPlaybackState.Playing)
            _previewPlayer.Pause();
        _previewing = false;
        _btnPreview.Text = "试听选中";
    }

    private async Task SaveAsync()
    {
        var picks = new List<(int StartMs, int EndMs)>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            if (row.Cells[UseColumn].Value is not bool use || !use) continue;

            if (!TryParseSeconds(row.Cells[StartColumn].Value, out int start)
                || !TryParseSeconds(row.Cells[EndColumn].Value, out int end)
                || end <= start)
            {
                _labelStatus.Text = $"第 {row.Index + 1} 行起止时间无效，请修正后再保存";
                return;
            }
            picks.Add((start, end));
        }

        if (picks.Count == 0)
        {
            _labelStatus.Text = "没有勾选任何片段";
            return;
        }

        _btnSave.Enabled = false;
        _btnPreview.Enabled = false;
        _labelStatus.Text = "正在生成切片…";
        _progress.Value = 0;

        var ids = picks.Select(_ => ShadowingSegmentStore.CreateSegmentId(_pageIndex)).ToList();
        var requests = picks
            .Select((pick, i) => new AudioSliceRequest(
                ids[i],
                _trackPath,
                pick.StartMs,
                pick.EndMs,
                _store.ResolvePath(_store.GetSliceRelativePath(ids[i]))))
            .ToList();

        var progress = new Progress<(int Done, int Total)>(p =>
        {
            if (IsDisposed) return;
            _progress.Value = p.Total > 0 ? (int)(p.Done * 1000.0 / p.Total) : 0;
            _labelStatus.Text = $"正在生成切片… {p.Done}/{p.Total}";
        });

        try
        {
            var results = await _slicer.SliceAllAsync(requests, progress, CancellationToken.None);
            if (IsDisposed) return;
            var byId = results.ToDictionary(r => r.SegmentId, StringComparer.Ordinal);
            int durationMs = AudioSliceService.ReadDurationMs(_trackPath);

            var manifest = new ShadowingSegmentManifest
            {
                PageIndex = _pageIndex,
                Source = new ShadowingSegmentSource
                {
                    TrackPath = _trackPath,
                    Fingerprint = ShadowingSegmentStore.BuildFingerprint(_trackPath, durationMs),
                    DurationMs = durationMs
                },
                Session = new ShadowingSegmentSession
                {
                    Id = ShadowingSegmentStore.CreateSessionId(DateTime.Now),
                    CaptureSpeed = _captureSpeed,
                    LockedPage = false,
                    UpdatedAt = DateTime.Now
                }
            };

            for (int i = 0; i < picks.Count; i++)
            {
                var result = byId.GetValueOrDefault(ids[i]);
                if (result == null || !result.Success) continue;

                manifest.Segments.Add(new ShadowingSegment
                {
                    Id = ids[i],
                    StartMs = picks[i].StartMs,
                    EndMs = picks[i].EndMs,
                    SlicePath = _store.GetSliceRelativePath(ids[i])
                });
            }

            if (manifest.Segments.Count == 0)
            {
                _labelStatus.Text = "切片全部失败，未保存";
                _btnSave.Enabled = true;
                return;
            }

            await _store.SaveSegmentsAsync(_pdfPath, manifest, CancellationToken.None);
            if (IsDisposed) return;
            SavedSegmentCount = manifest.Segments.Count;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            if (IsDisposed) return;
            _labelStatus.Text = $"保存失败：{ex.Message}";
            _btnSave.Enabled = true;
            _btnPreview.Enabled = true;
        }
    }

    private void Cleanup()
    {
        _previewTimer.Stop();
        _previewTimer.Dispose();
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _previewPlayer?.Dispose();
        _previewPlayer = null;
    }

    private static string FormatSeconds(int milliseconds)
        => (milliseconds / 1000.0).ToString("0.000", CultureInfo.InvariantCulture);

    private static bool TryParseSeconds(object? value, out int milliseconds)
    {
        milliseconds = 0;
        var text = value?.ToString();
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)
            || double.IsNaN(seconds) || seconds < 0) return false;

        milliseconds = (int)Math.Round(seconds * 1000);
        return true;
    }
}
