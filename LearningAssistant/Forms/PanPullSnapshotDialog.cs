using System.ComponentModel;

namespace LearningAssistant.Forms;

/// <summary>
/// P1 独立入口：拉取快照参数对话框（路径 + 深度 + 文件上限 + 快速模式勾选）。
/// 纯代码创建（不设计器），保持轻量。
/// </summary>
public class PanPullSnapshotDialog : Form
{
    private readonly TextBox _txtPath;
    private readonly NumericUpDown _numDepth;
    private readonly NumericUpDown _numMaxFiles;
    private readonly CheckBox _chkSkipSize;
    private readonly Button _btnOk;
    private readonly Button _btnCancel;

    public string DirectoryPath => _txtPath.Text?.Trim() ?? "";
    public int MaxDepth => (int)_numDepth.Value;
    public int MaxFileCount => (int)_numMaxFiles.Value;
    public bool SkipSizeComputing => _chkSkipSize.Checked;

    public PanPullSnapshotDialog(string? defaultPath = null)
    {
        Font = new Font("Microsoft YaHei UI", 9F);
        Text = "📥 拉取网盘目录快照";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(520, 250);

        var lbl = new Label
        {
            Text = "请输入要整理的网盘根目录完整路径（如 /我的资源/高中数学 或 /）：",
            AutoSize = true,
            Location = new Point(14, 16),
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
        };

        _txtPath = new TextBox
        {
            Location = new Point(14, 40),
            Width = 480,
            Text = defaultPath ?? "/"
        };

        var lblDepth = new Label { Text = "递归深度", AutoSize = true, Location = new Point(14, 78) };
        _numDepth = new NumericUpDown
        {
            Location = new Point(100, 76), Width = 80,
            Minimum = 1, Maximum = 10, Value = 2, Increment = 1
        };
        var tipDepth = new Label
        {
            Text = "(1=仅当前目录，0=全部；推荐 2~4)",
            AutoSize = true,
            ForeColor = Color.Gray,
            Location = new Point(190, 80)
        };

        var lblMax = new Label { Text = "文件上限", AutoSize = true, Location = new Point(14, 110) };
        _numMaxFiles = new NumericUpDown
        {
            Location = new Point(100, 108), Width = 100,
            Minimum = 100, Maximum = 100000, Value = 3000, Increment = 500
        };
        var tipMax = new Label
        {
            Text = "(达到上限后停止，防止大目录超时)",
            AutoSize = true,
            ForeColor = Color.Gray,
            Location = new Point(210, 112)
        };

        _chkSkipSize = new CheckBox
        {
            Text = "⚡ 快速模式：跳过文件大小/重复检测（大目录显著加速）",
            AutoSize = true,
            Location = new Point(14, 142),
            Checked = true   // 用户明确说「不太关注文件大小」→ 默认勾选
        };
        _chkSkipSize.CheckedChanged += (_, _) =>
        {
            _chkSkipSize.Text = _chkSkipSize.Checked
                ? "⚡ 快速模式：跳过文件大小/重复检测（大目录显著加速，✅ 已启用）"
                : "📊 完整模式：包含大小统计+重复检测（耗时更久）";
        };

        _btnOk = new Button
        {
            Text = "开始拉取", Size = new Size(120, 32),
            Location = new Point(240, 180), DialogResult = DialogResult.OK,
            BackColor = Color.FromArgb(66, 133, 244), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
        };
        _btnCancel = new Button
        {
            Text = "取消", Size = new Size(90, 32),
            Location = new Point(370, 180), DialogResult = DialogResult.Cancel
        };
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        _btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtPath.Text))
            {
                MessageBox.Show("请输入目录路径。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.None;
            }
        };

        Controls.Add(lbl);
        Controls.Add(_txtPath);
        Controls.Add(lblDepth);
        Controls.Add(_numDepth);
        Controls.Add(tipDepth);
        Controls.Add(lblMax);
        Controls.Add(_numMaxFiles);
        Controls.Add(tipMax);
        Controls.Add(_chkSkipSize);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);
    }

    // 设计器专用：隐藏 AutoGenerateFieldAttribute 相关
    [EditorBrowsable(EditorBrowsableState.Never)]
    private void InitializeComponent() { }
}
