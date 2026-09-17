using LearningAssistant.Models.PanAnalysis;

namespace LearningAssistant.Forms.UserControls.Pan;

/// <summary>
/// P1-3: 拖拽 AI 推荐路径浮窗。
/// 拖拽文件时跟随光标显示，列出基于文件特征的推荐目标路径。
/// 用户点击推荐项 → 立即移动到该路径；按 Esc 或释放鼠标在空白处 → 取消。
/// </summary>
public class PanDropHintWindow : Form
{
    private readonly ListView _lstHints;
    private readonly Label _lblTitle;
    private List<PanDropHint> _hints = new();

    /// <summary>用户选中某条推荐路径时触发（参数：推荐项）</summary>
    public event EventHandler<PanDropHint>? HintSelected;

    /// <summary>浮窗被用户关闭（Esc 或失焦）</summary>
    public event EventHandler? HintCancelled;

    public PanDropHintWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        Size = new Size(280, 180);
        KeyPreview = true;
        BackColor = Color.White;
        Padding = new Padding(0);

        // 圆角阴影：通过 Region 实现（简化为直角，避免复杂 GDI+）
        var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(1) };
        pnl.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(66, 133, 244), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
        };

        _lblTitle = new Label
        {
            Text = "🤖 AI 推荐目标路径（点击直接移动）",
            Dock = DockStyle.Top,
            Height = 26,
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(66, 133, 244),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 0, 0)
        };

        _lstHints = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            HideSelection = false,
            Font = new Font("Microsoft YaHei UI", 9F),
            BackColor = Color.White
        };
        _lstHints.Columns.Add("推荐路径", 240);
        _lstHints.Columns.Add("依据", 36);
        _lstHints.MouseClick += LstHints_MouseClick;
        _lstHints.KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Cancel(); };

        pnl.Controls.Add(_lstHints);
        pnl.Controls.Add(_lblTitle);
        Controls.Add(pnl);

        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Cancel(); };
        Deactivate += (_, _) => Cancel();
    }

    /// <summary>显示推荐列表并定位到光标附近</summary>
    public void ShowHints(List<PanDropHint> hints, Point cursorScreenPos)
    {
        _hints = hints ?? new();
        _lstHints.BeginUpdate();
        _lstHints.Items.Clear();
        foreach (var h in _hints)
        {
            var item = new ListViewItem(h.DisplayPath) { Tag = h };
            item.SubItems.Add(h.SourceLabel);
            // 颜色：AI 建议 = 蓝色（优先），启发式 = 深灰
            item.ForeColor = h.IsFromAI ? Color.FromArgb(66, 133, 244) : Color.FromArgb(80, 80, 80);
            _lstHints.Items.Add(item);
        }
        _lstHints.EndUpdate();

        // 自适应高度
        var height = 26 + Math.Min(4, _hints.Count) * 22 + 8;
        Height = Math.Max(60, height);

        // 定位：光标右下方，避免遮挡
        var x = cursorScreenPos.X + 16;
        var y = cursorScreenPos.Y + 16;
        var screen = Screen.FromPoint(cursorScreenPos).WorkingArea;
        if (x + Width > screen.Right) x = cursorScreenPos.X - Width - 8;
        if (y + Height > screen.Bottom) y = cursorScreenPos.Y - Height - 8;
        Location = new Point(x, y);

        if (_hints.Count == 0)
        {
            _lblTitle.Text = "ℹ️ 暂无推荐路径（手动拖拽到目标即可）";
            Height = 60;
        }
        else
        {
            _lblTitle.Text = $"🤖 AI 推荐 {_hints.Count} 个目标路径";
        }

        Show();
    }

    private void LstHints_MouseClick(object? sender, MouseEventArgs e)
    {
        try
        {
            var item = _lstHints.GetItemAt(e.X, e.Y);
            if (item?.Tag is PanDropHint hint)
            {
                HintSelected?.Invoke(this, hint);
                Close();
            }
        }
        catch { /* ignore */ }
    }

    private void Cancel()
    {
        try
        {
            HintCancelled?.Invoke(this, EventArgs.Empty);
            Close();
        }
        catch { /* ignore */ }
    }
}

/// <summary>单个 AI 推荐路径</summary>
public class PanDropHint
{
    /// <summary>目标完整路径（以 / 结尾）</summary>
    public string TargetPath { get; set; } = "/";

    /// <summary>显示用路径（缩短版，避免过长）</summary>
    public string DisplayPath { get; set; } = "/";

    /// <summary>推荐来源标签：AI / 启发式 / 历史</summary>
    public string SourceLabel { get; set; } = "启发式";

    /// <summary>是否来自 AI 建议（true=蓝色高亮，false=灰色）</summary>
    public bool IsFromAI { get; set; }

    /// <summary>推荐理由（鼠标悬停可显示）</summary>
    public string Reason { get; set; } = "";
}
