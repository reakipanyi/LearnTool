using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace LearningAssistant.Forms.Pdf;

/// <summary>
/// PDF 内嵌 AI 侧栏（P0-6）：输入框提问 + 作用域切换 + 流式输出 + 停止/复制/重新生成。
/// 只负责交互与状态机，取词、上下文裁剪与 AI 调用全部交给 <see cref="IPdfAiService"/>。
/// </summary>
public sealed class PdfAiSidebarPanel : UserControl
{
    /// <summary>答案中的页码锚点，例如 [[p12]]</summary>
    private static readonly Regex PageAnchorRegex = new(@"\[\[p(\d+)\]\]", RegexOptions.Compiled);

    private static readonly Color Brand = Color.FromArgb(109, 40, 217);
    private static readonly Color BrandSoft = Color.FromArgb(243, 240, 255);
    private static readonly Color Border = Color.FromArgb(196, 181, 253);
    private static readonly Color TextColor = Color.FromArgb(40, 40, 40);
    private static readonly Color Muted = Color.FromArgb(120, 120, 120);

    private readonly TableLayoutPanel _root = new();
    private readonly Label _titleLabel = new();
    private readonly Button _closeButton = new();
    private readonly ComboBox _scopeBox = new();
    private readonly Panel _rangeRow = new();
    private readonly NumericUpDown _rangeStart = new();
    private readonly NumericUpDown _rangeEnd = new();
    private readonly RichTextBox _output = new();
    private readonly FlowLayoutPanel _pageChips = new();
    private readonly Label _metaLabel = new();
    private readonly Label _statusLabel = new();
    private readonly Button _stopButton = new();
    private readonly Button _copyButton = new();
    private readonly Button _regenerateButton = new();
    private readonly Button _saveButton = new();
    private readonly TextBox _input = new();
    private readonly Button _sendButton = new();

    private CancellationTokenSource? _requestCts;
    private bool _isBusy;
    private string _lastQuestion = string.Empty;
    private string _lastAnswer = string.Empty;

    public PdfAiSidebarPanel()
    {
        BackColor = Color.White;
        BuildLayout();
        WireEvents();
        SetBusy(false);
    }

    /// <summary>PDF AI 编排服务；为空时提示先完成 AI 配置。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IPdfAiService? PdfAiService { get; set; }

    /// <summary>当前文档路径，返回空表示没有打开文档。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<string?>? PdfPathProvider { get; set; }

    /// <summary>当前页索引（从 0 开始）。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<int>? CurrentPageProvider { get; set; }

    /// <summary>文档总页数（从 1 开始计数前的总数）。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<int>? PageCountProvider { get; set; }

    /// <summary>画布上选中的文本；无选中时返回空。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<string?>? SelectedTextProvider { get; set; }

    /// <summary>全文作用域的隐私确认；返回 false 时取消本次请求。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<string, bool>? ConfirmWholeDocument { get; set; }

    /// <summary>点击页码 chip 后跳转（页码从 1 开始）。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<int>? NavigateToPage { get; set; }

    /// <summary>把当前答案保存为标注。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<string>? SaveAnswerAsAnnotation { get; set; }

    /// <summary>请求关闭侧栏。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action? CloseRequested { get; set; }

    /// <summary>切换文档时清空会话，历史不跨文档混用。</summary>
    public void ResetForDocument()
    {
        CancelCurrentRequest();
        _lastQuestion = string.Empty;
        _lastAnswer = string.Empty;
        _output.Clear();
        _pageChips.Controls.Clear();
        _metaLabel.Text = string.Empty;
        SetStatus("就绪");
    }

    /// <summary>把焦点放到输入框，并带上外部预设的选中文本。</summary>
    public void FocusInput(string? presetQuestion = null)
    {
        if (!string.IsNullOrWhiteSpace(presetQuestion))
            _input.Text = presetQuestion;

        _input.Focus();
        _input.SelectionStart = _input.TextLength;
    }

    /// <summary>外部入口（总结本页/解释选中）直接以预设问题发起一次问答；可同时指定作用域。</summary>
    public void AskWithPreset(string question, PdfAiScope? scope = null)
    {
        if (scope.HasValue)
            _scopeBox.SelectedIndex = scope.Value switch
            {
                PdfAiScope.Selection => 0,
                PdfAiScope.CurrentPage => 1,
                PdfAiScope.SpecifiedPages => 2,
                _ => 3
            };

        _input.Text = question;
        _ = SendAsync();
    }

    private void BuildLayout()
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(8, 8, 8, 8);

        _root.Dock = DockStyle.Fill;
        _root.ColumnCount = 1;
        _root.RowCount = 8;
        _root.BackColor = Color.White;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F)); // 标题 + 作用域
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F)); // 指定页范围（按需显示）
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // 输出
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F)); // 页码 chip
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F)); // 元信息
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F)); // 状态 + 停止
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F)); // 复制 / 重新生成 / 保存
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F)); // 输入 + 发送

        _titleLabel.Text = "AI 阅读助手";
        _titleLabel.AutoSize = false;
        _titleLabel.Dock = DockStyle.Fill;
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        _titleLabel.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        _titleLabel.ForeColor = TextColor;

        _scopeBox.Dock = DockStyle.Right;
        _scopeBox.Width = 128;
        _scopeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _scopeBox.FlatStyle = FlatStyle.Flat;
        _scopeBox.Items.AddRange(new object[] { "选中内容", "本页及相邻页", "指定页", "整份文档" });

        _closeButton.Text = "✕";
        _closeButton.Dock = DockStyle.Right;
        _closeButton.Width = 28;
        StyleSecondaryButton(_closeButton);

        var header = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
        header.Controls.Add(_titleLabel);
        header.Controls.Add(_closeButton);
        header.Controls.Add(_scopeBox);
        _root.Controls.Add(header, 0, 0);

        // 指定页范围行：仅在“指定页”作用域下可见
        _rangeRow.Dock = DockStyle.Fill;
        _rangeRow.Visible = false;
        _rangeRow.Margin = new Padding(0, 2, 0, 2);
        var rangeStartLabel = new Label { Text = "从第", AutoSize = true, Location = new Point(0, 7), ForeColor = Muted };
        _rangeStart.Location = new Point(44, 4);
        _rangeStart.Width = 64;
        _rangeStart.Minimum = 1;
        _rangeStart.Maximum = 1;
        var rangeEndLabel = new Label { Text = "到第", AutoSize = true, Location = new Point(116, 7), ForeColor = Muted };
        _rangeEnd.Location = new Point(162, 4);
        _rangeEnd.Width = 64;
        _rangeEnd.Minimum = 1;
        _rangeEnd.Maximum = 1;
        var rangeUnitLabel = new Label { Text = "页", AutoSize = true, Location = new Point(230, 7), ForeColor = Muted };
        _rangeRow.Controls.AddRange(new Control[]
        {
            rangeStartLabel, _rangeStart, rangeEndLabel, _rangeEnd, rangeUnitLabel
        });
        _root.Controls.Add(_rangeRow, 0, 1);

        _output.Dock = DockStyle.Fill;
        _output.ReadOnly = true;
        _output.BorderStyle = BorderStyle.FixedSingle;
        _output.BackColor = Color.White;
        _output.ForeColor = TextColor;
        _output.Font = new Font("Microsoft YaHei UI", 9.5F);
        _output.DetectUrls = false;
        _output.Margin = new Padding(0, 4, 0, 4);
        _root.Controls.Add(_output, 0, 2);

        _pageChips.Dock = DockStyle.Fill;
        _pageChips.AutoScroll = true;
        _pageChips.WrapContents = false;
        _pageChips.FlowDirection = FlowDirection.LeftToRight;
        _pageChips.Margin = new Padding(0);
        _root.Controls.Add(_pageChips, 0, 3);

        _metaLabel.Dock = DockStyle.Fill;
        _metaLabel.AutoSize = false;
        _metaLabel.TextAlign = ContentAlignment.MiddleLeft;
        _metaLabel.ForeColor = Muted;
        _metaLabel.Font = new Font("Microsoft YaHei UI", 8F);
        _metaLabel.Margin = new Padding(0);
        _root.Controls.Add(_metaLabel, 0, 4);

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.AutoSize = false;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.ForeColor = Muted;
        _statusLabel.Font = new Font("Microsoft YaHei UI", 8.5F);
        _statusLabel.Margin = new Padding(0);

        _stopButton.Text = "停止";
        _stopButton.Size = new Size(64, 24);
        StyleSecondaryButton(_stopButton);
        var statusHost = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
        statusHost.Controls.Add(_statusLabel);
        statusHost.Controls.Add(_stopButton);
        _stopButton.Dock = DockStyle.Right;
        _root.Controls.Add(statusHost, 0, 5);
        _statusLabel.Dock = DockStyle.Fill;

        var actionRow = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
        _copyButton.Text = "复制";
        _regenerateButton.Text = "重新生成";
        _saveButton.Text = "保存为标注";
        int actionX = 0;
        foreach (var button in new[] { _copyButton, _regenerateButton, _saveButton })
        {
            button.Size = new Size(button == _copyButton ? 64 : 96, 30);
            button.Location = new Point(actionX, 3);
            button.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            StyleSecondaryButton(button);
            actionRow.Controls.Add(button);
            actionX += button.Width + 8;
        }
        _root.Controls.Add(actionRow, 0, 6);

        _input.Dock = DockStyle.Fill;
        _input.Multiline = true;
        _input.ScrollBars = ScrollBars.Vertical;
        _input.PlaceholderText = "向 AI 提问，Enter 发送，Shift+Enter 换行";
        _input.Font = new Font("Microsoft YaHei UI", 9.5F);
        _input.BorderStyle = BorderStyle.FixedSingle;
        _input.Margin = new Padding(0, 4, 0, 0);

        _sendButton.Text = "发送";
        _sendButton.Dock = DockStyle.Right;
        _sendButton.Width = 64;
        StylePrimaryButton(_sendButton);

        var inputHost = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
        inputHost.Controls.Add(_input);
        inputHost.Controls.Add(_sendButton);
        _root.Controls.Add(inputHost, 0, 7);

        Controls.Add(_root);
    }

    private void WireEvents()
    {
        _sendButton.Click += async (_, _) => await SendAsync();
        _stopButton.Click += (_, _) => CancelCurrentRequest();
        _copyButton.Click += (_, _) => CopyAnswer();
        _regenerateButton.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_lastQuestion)) return;
            _input.Text = _lastQuestion;
            await SendAsync();
        };
        _saveButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_lastAnswer)) return;
            SaveAnswerAsAnnotation?.Invoke(_lastAnswer);
        };
        _closeButton.Click += (_, _) => CloseRequested?.Invoke();
        _scopeBox.SelectedIndexChanged += (_, _) => UpdateRangeRowVisibility();
        _input.KeyDown += Input_KeyDown;
    }

    private void Input_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter || e.Shift) return;

        e.SuppressKeyPress = true;
        _ = SendAsync();
    }

    private void UpdateRangeRowVisibility()
    {
        bool specified = CurrentScope == PdfAiScope.SpecifiedPages;
        _rangeRow.Visible = specified;
        if (!specified) return;

        int pageCount = Math.Max(1, PageCountProvider?.Invoke() ?? 1);
        _rangeStart.Maximum = pageCount;
        _rangeEnd.Maximum = pageCount;
        int current = Math.Clamp((CurrentPageProvider?.Invoke() ?? 0) + 1, 1, pageCount);
        _rangeEnd.Value = current;
        if (_rangeStart.Value > _rangeEnd.Value)
            _rangeStart.Value = _rangeEnd.Value;
    }

    private PdfAiScope CurrentScope => _scopeBox.SelectedIndex switch
    {
        0 => PdfAiScope.Selection,
        1 => PdfAiScope.CurrentPage,
        2 => PdfAiScope.SpecifiedPages,
        _ => PdfAiScope.WholeDocument
    };

    /// <summary>发送一次问答：状态机 准备上下文 → 生成中 → 完成/失败/已停止</summary>
    private async Task SendAsync()
    {
        if (_isBusy) return;

        string question = _input.Text.Trim();
        if (question.Length == 0)
        {
            SetStatus("请输入问题");
            _input.Focus();
            return;
        }

        if (PdfAiService == null)
        {
            SetStatus("AI 服务不可用，请先完成 AI 配置");
            return;
        }

        string? pdfPath = PdfPathProvider?.Invoke();
        if (string.IsNullOrWhiteSpace(pdfPath))
        {
            SetStatus("当前没有打开的 PDF 文档");
            return;
        }

        var scope = CurrentScope;
        if (scope == PdfAiScope.Selection && string.IsNullOrWhiteSpace(SelectedTextProvider?.Invoke()))
        {
            SetStatus("请先在页面上选中文字，或把作用域切换为“本页/全文”");
            return;
        }

        if (scope == PdfAiScope.WholeDocument
            && ConfirmWholeDocument != null
            && !ConfirmWholeDocument("AI 问答"))
        {
            SetStatus("已取消发送整份文档");
            return;
        }

        int currentPage = Math.Max(0, CurrentPageProvider?.Invoke() ?? 0);
        int startIndex = scope == PdfAiScope.SpecifiedPages ? (int)_rangeStart.Value - 1 : currentPage;
        int endIndex = scope == PdfAiScope.SpecifiedPages ? (int)_rangeEnd.Value - 1 : currentPage;
        string? selectedText = SelectedTextProvider?.Invoke();

        _lastQuestion = question;
        _lastAnswer = string.Empty;
        _metaLabel.Text = string.Empty;
        _pageChips.Controls.Clear();
        AppendTurnHeader(question);

        _requestCts = new CancellationTokenSource();
        var token = _requestCts.Token;
        SetBusy(true);
        SetStatus("正在准备上下文（首次需建立文本索引）…");

        try
        {
            var buffer = new StringBuilder();
            bool firstChunk = true;

            await foreach (var chunk in PdfAiService.AskScopedStreamAsync(
                               pdfPath,
                               question,
                               scope,
                               currentPage,
                               startIndex,
                               endIndex,
                               selectedText,
                               context => RunOnUiThread(() => ApplyContext(context)),
                               token))
            {
                if (firstChunk)
                {
                    firstChunk = false;
                    SetStatus("生成中…");
                }

                buffer.Append(chunk);
                AppendAnswerText(chunk);
            }

            _lastAnswer = buffer.ToString();
            AppendAnswerText(Environment.NewLine + Environment.NewLine);
            SetStatus(firstChunk ? "未生成内容" : "完成");
            RenderPageChips(_lastAnswer);
        }
        catch (OperationCanceledException)
        {
            _lastAnswer = string.Empty;
            AppendAnswerText("（已停止）" + Environment.NewLine + Environment.NewLine);
            SetStatus("已停止");
        }
        catch (Exception ex)
        {
            AppendAnswerText($"请求失败：{ex.Message}" + Environment.NewLine + Environment.NewLine);
            SetStatus("失败：" + ex.Message);
        }
        finally
        {
            _requestCts?.Dispose();
            _requestCts = null;
            SetBusy(false);
        }
    }

    private void CancelCurrentRequest()
    {
        _requestCts?.Cancel();
    }

    private void CopyAnswer()
    {
        if (string.IsNullOrWhiteSpace(_lastAnswer))
        {
            SetStatus("暂无可复制的内容");
            return;
        }

        Clipboard.SetText(_lastAnswer);
        SetStatus("已复制");
    }

    /// <summary>把上下文元信息落到页脚：模型、覆盖页数与未覆盖提示</summary>
    private void ApplyContext(PdfAiStreamContext context)
    {
        string model = string.IsNullOrWhiteSpace(context.ModelId) ? "未知模型" : context.ModelId;
        var parts = new List<string> { model, $"覆盖 {context.IncludedPages.Count}/{context.TotalPages} 页" };
        if (context.CoverageWarning != null)
            parts.Add(context.CoverageWarning);

        _metaLabel.Text = string.Join(" · ", parts);
        _metaLabel.ForeColor = context.CoverageWarning != null ? Color.FromArgb(180, 95, 0) : Muted;
    }

    private void RenderPageChips(string answer)
    {
        _pageChips.Controls.Clear();
        if (string.IsNullOrEmpty(answer)) return;

        var pages = PageAnchorRegex.Matches(answer)
            .Select(match => int.TryParse(match.Groups[1].Value, out var page) ? page : -1)
            .Where(page => page > 0)
            .Distinct()
            .OrderBy(page => page)
            .ToList();
        if (pages.Count == 0) return;

        _pageChips.Controls.Add(new Label
        {
            Text = "引用页：",
            AutoSize = true,
            ForeColor = Muted,
            Font = new Font("Microsoft YaHei UI", 8F),
            Margin = new Padding(0, 8, 2, 0)
        });

        foreach (int page in pages)
        {
            var link = new LinkLabel
            {
                Text = $"第 {page} 页",
                AutoSize = true,
                LinkColor = Brand,
                ActiveLinkColor = Color.FromArgb(139, 92, 246),
                VisitedLinkColor = Brand,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 6, 8, 0)
            };
            int target = page;
            link.LinkClicked += (_, _) => NavigateToPage?.Invoke(target);
            _pageChips.Controls.Add(link);
        }
    }

    private void AppendTurnHeader(string question)
    {
        _output.SelectionStart = _output.TextLength;
        _output.SelectionLength = 0;
        _output.SelectionFont = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _output.SelectionColor = Brand;
        _output.AppendText($"问：{question}{Environment.NewLine}");
        _output.SelectionFont = new Font("Microsoft YaHei UI", 9.5F);
        _output.SelectionColor = TextColor;
    }

    private void AppendAnswerText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        // 流式追加只在末尾写入，避免每次全量重排；已停止/失败的分隔也走这里
        _output.SelectionStart = _output.TextLength;
        _output.SelectionLength = 0;
        _output.SelectionFont = new Font("Microsoft YaHei UI", 9.5F);
        _output.SelectionColor = TextColor;
        _output.AppendText(text);
        _output.SelectionStart = _output.TextLength;
        _output.ScrollToCaret();
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        _sendButton.Enabled = !busy;
        _regenerateButton.Enabled = !busy && !string.IsNullOrWhiteSpace(_lastQuestion);
        _stopButton.Enabled = busy;
        _scopeBox.Enabled = !busy;
    }

    private void SetStatus(string text)
    {
        _statusLabel.Text = text;
        _statusLabel.ForeColor = text.StartsWith("失败", StringComparison.Ordinal)
            ? Color.FromArgb(200, 60, 60)
            : Muted;
    }

    /// <summary>把后台线程的上下文回调切回 UI 线程</summary>
    private void RunOnUiThread(Action action)
    {
        if (IsDisposed) return;

        if (InvokeRequired)
        {
            if (IsHandleCreated) BeginInvoke(action);
        }
        else
        {
            action();
        }
    }

    private static void StylePrimaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = Brand;
        button.ForeColor = Color.White;
        button.Cursor = Cursors.Hand;
        button.FlatAppearance.BorderSize = 0;
    }

    private static void StyleSecondaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = BrandSoft;
        button.ForeColor = Brand;
        button.Cursor = Cursors.Hand;
        button.FlatAppearance.BorderColor = Border;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _requestCts?.Cancel();
            _requestCts?.Dispose();
            _requestCts = null;
        }

        base.Dispose(disposing);
    }
}
