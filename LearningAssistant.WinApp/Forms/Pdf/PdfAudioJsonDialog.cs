namespace LearningAssistant.Forms.Pdf;

/// <summary>
/// 匹配档案原文编辑器：直接改目录档案 JSON，保存后立即重新匹配。
/// 保存时不校验书名与内容指纹，方便手工调整书目、规则与逐页覆盖。
/// </summary>
public sealed class PdfAudioJsonDialog : Form
{
    private readonly TextBox _text = new();

    public PdfAudioJsonDialog(string json, string fileLabel)
    {
        Text = $"编辑匹配档案（JSON） - {fileLabel}";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(980, 700);
        MinimumSize = new Size(720, 520);
        ShowInTaskbar = false;
        MinimizeBox = false;

        BuildLayout();
        _text.Text = json;
    }

    /// <summary>用户确认后的档案原文。</summary>
    public string Json => _text.Text;

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14, 12, 14, 10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        var hint = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(940, 0),
            Text = "直接编辑目录档案原文（rules / books / overrides / rulePatch 等）。" +
                   "保存后立即按新内容重新匹配；书目可手动改名，不再按指纹校验。",
            Margin = new Padding(0, 0, 0, 8)
        };

        _text.Dock = DockStyle.Fill;
        _text.Multiline = true;
        _text.WordWrap = false;
        _text.ScrollBars = ScrollBars.Both;
        _text.AcceptsTab = true;
        _text.Font = new Font("Consolas", 10F);
        _text.Margin = new Padding(0);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0, 8, 0, 0)
        };

        var btnCancel = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            AutoSize = true,
            MinimumSize = new Size(88, 30),
            Margin = new Padding(8, 0, 0, 0)
        };
        var btnOk = new Button
        {
            Text = "保存并重新匹配",
            DialogResult = DialogResult.OK,
            AutoSize = true,
            MinimumSize = new Size(132, 30),
            Margin = new Padding(0)
        };

        buttons.Controls.Add(btnCancel);
        buttons.Controls.Add(btnOk);

        root.Controls.Add(hint, 0, 0);
        root.Controls.Add(_text, 0, 1);
        root.Controls.Add(buttons, 0, 2);
        Controls.Add(root);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }
}
