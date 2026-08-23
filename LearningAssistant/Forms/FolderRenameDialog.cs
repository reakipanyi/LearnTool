using LearningAssistant.Models.PanAnalysis;

namespace LearningAssistant.Forms;

/// <summary>
/// 文件夹重命名弹窗（P0.5）。
/// 功能：手动勾选追加文件大小后缀 + 5 种格式 + 小数位数 + 前缀/后缀 + 重新计算大小 + 实时预览。
/// ⭐ 用户决策默认值：AppendSizeSuffix=false（记忆上次）、SuffixFormat=BracketGB、DecimalPlaces=2、Position=Suffix
/// </summary>
public partial class FolderRenameDialog : Form
{
    #region === 字段 ===
    private readonly PanDirectorySnapshot? _snapshot;
    private readonly PanFileInfo _folderInfo;
    private FolderRenameOptions _options;
    private bool _suppressEvents;

    /// <summary>用户最终确认的重命名选项（点击确认后才有值）</summary>
    public FolderRenameOptions? Result { get; private set; }

    /// <summary>用户最终输入的新名称</summary>
    public string NewName { get; private set; } = "";

    // ⭐ 用户决策默认值
    private const bool DefaultAppendSize = false;
    private const FolderSizeSuffixFormat DefaultFormat = FolderSizeSuffixFormat.BracketGB;
    private const int DefaultDecimals = 2;
    private const SuffixPosition DefaultPosition = SuffixPosition.Suffix;
    #endregion

    #region === 构造函数 ===
    public FolderRenameDialog()
    {
        _folderInfo = new PanFileInfo { IsFolder = true, Name = "", Path = "/" };
        _options = new FolderRenameOptions
        {
            AppendSizeSuffix = DefaultAppendSize,
            SuffixFormat = DefaultFormat,
            DecimalPlaces = DefaultDecimals,
            Position = DefaultPosition
        };
        InitializeComponent();
        LoadOptionsToUI();
        UpdatePreview();
    }

    /// <summary>
    /// 运行时构造。
    /// </summary>
    /// <param name="snapshot">快照（用于计算文件夹大小，可为 null=无法估算）</param>
    /// <param name="folderInfo">要重命名的文件夹</param>
    /// <param name="lastOptions">上次用户选择的偏好（从 Settings 读取），null=用默认值</param>
    public FolderRenameDialog(PanDirectorySnapshot? snapshot, PanFileInfo folderInfo, FolderRenameOptions? lastOptions = null)
    {
        _snapshot = snapshot;
        _folderInfo = folderInfo ?? throw new ArgumentNullException(nameof(folderInfo));
        _options = lastOptions ?? new FolderRenameOptions
        {
            AppendSizeSuffix = DefaultAppendSize,
            SuffixFormat = DefaultFormat,
            DecimalPlaces = DefaultDecimals,
            Position = DefaultPosition
        };
        InitializeComponent();
        LoadOptionsToUI();
        ComputeFromSnapshot();
        UpdatePreview();
    }
    #endregion

    #region === UI 加载 ===
    private void LoadOptionsToUI()
    {
        _suppressEvents = true;
        try
        {
            txtNewName.Text = _folderInfo.Name;
            chkAppendSize.Checked = _options.AppendSizeSuffix;
            cboFormat.SelectedIndex = (int)_options.SuffixFormat;
            nudDecimals.Value = _options.DecimalPlaces;
            radSuffix.Checked = (_options.Position == SuffixPosition.Suffix);
            radPrefix.Checked = (_options.Position == SuffixPosition.Prefix);
            chkShowCount.Checked = _options.ShowCountInPreview;
        }
        finally { _suppressEvents = false; }
    }
    #endregion

    #region === Step2: 大小计算（快照聚合 + 格式化）===

    /// <summary>从快照聚合计算文件夹大小（0 API，毫秒级，快照截断时偏小）</summary>
    private void ComputeFromSnapshot()
    {
        if (_snapshot == null)
        {
            _options.ComputedSizeBytes = 0;
            _options.ComputedFileCount = 0;
            _options.ComputedSubFolderCount = 0;
            _options.IsSizeEstimated = true;
            return;
        }

        try
        {
            var folderPrefix = _folderInfo.Path.TrimEnd('/') + "/";
            long totalSize = 0;
            int fileCount = 0;
            int subFolderCount = 0;

            foreach (var file in _snapshot.Files)
            {
                if (file.Path.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    totalSize += file.SizeBytes;
                    fileCount++;
                }
            }

            foreach (var folder in _snapshot.Folders)
            {
                if (folder.Path.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    subFolderCount++;
                }
            }

            _options.ComputedSizeBytes = totalSize;
            _options.ComputedFileCount = fileCount;
            _options.ComputedSubFolderCount = subFolderCount;
            _options.IsSizeEstimated = !_snapshot.IsComplete; // 快照不完整时标记为估算
        }
        catch
        {
            _options.IsSizeEstimated = true;
        }
    }

    /// <summary>把字节数格式化为人类可读字符串。⭐ 用户决策：<1MB=KB / 1-1024MB=MB / ≥1GB=GB，数字与单位间有空格</summary>
    public static string FormatHumanReadable(long bytes, int decimalPlaces = 2)
    {
        if (bytes < 0) return "0 B";
        double len = bytes;
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        while (len >= 1024 && order < units.Length - 1)
        {
            order++;
            len /= 1024;
        }
        var fmt = "0." + new string('0', decimalPlaces);
        return $"{len.ToString(fmt)} {units[order]}";
    }

    /// <summary>根据格式枚举生成大小后缀字符串（如 [3.25 GB]）</summary>
    private string BuildSizeSuffix()
    {
        if (!_options.AppendSizeSuffix) return "";
        var sizeStr = FormatHumanReadable(_options.ComputedSizeBytes, _options.DecimalPlaces);
        return _options.SuffixFormat switch
        {
            FolderSizeSuffixFormat.ParenthesisGB => $"_({sizeStr.Replace(" ", "")})",  // _(3.25GB) 紧凑无空格
            FolderSizeSuffixFormat.BracketGB     => $"_[{sizeStr}]",                   // _[3.25 GB] ⭐ 默认
            FolderSizeSuffixFormat.ChineseBracket => $"【{sizeStr.Replace(" ", "")}】", // 【3.25GB】
            FolderSizeSuffixFormat.HyphenGB       => $"-{sizeStr.Replace(" ", "")}",   // -3.25GB
            FolderSizeSuffixFormat.PrefixGB      => $"[{sizeStr}]",                   // 前缀 [3.25 GB]
            _ => ""
        };
    }
    #endregion

    #region === 实时预览 ===
    private void UpdatePreview()
    {
        if (_suppressEvents) return;
        try
        {
            var baseName = txtNewName.Text.Trim();
            if (string.IsNullOrEmpty(baseName)) baseName = _folderInfo.Name;

            var suffix = BuildSizeSuffix();
            string finalName;
            if (!_options.AppendSizeSuffix || string.IsNullOrEmpty(suffix))
            {
                finalName = baseName;
            }
            else if (_options.Position == SuffixPosition.Prefix || _options.SuffixFormat == FolderSizeSuffixFormat.PrefixGB)
            {
                // 前缀模式
                finalName = suffix + baseName;
            }
            else
            {
                // 后缀模式
                finalName = baseName + suffix;
            }
            lblPreview.Text = finalName;

            // 显示大小信息
            var sizeStr = FormatHumanReadable(_options.ComputedSizeBytes, _options.DecimalPlaces);
            if (_options.ShowCountInPreview)
            {
                lblSizeInfo.Text = $"计算结果：{sizeStr}（包含 {_options.ComputedFileCount} 个文件 + {_options.ComputedSubFolderCount} 个子目录）";
            }
            else
            {
                lblSizeInfo.Text = $"计算结果：{sizeStr}";
            }

            // 估算提示
            if (_options.IsSizeEstimated && _options.AppendSizeSuffix)
            {
                lblSizeInfo.Text += "  ⚠️ 基于部分快照估算，实际大小可能更大";
                lblSizeInfo.ForeColor = Color.FromArgb(230, 126, 34); // 橙色
            }
            else
            {
                lblSizeInfo.ForeColor = Color.FromArgb(100, 116, 139);
            }
        }
        catch { /* ignore */ }
    }
    #endregion

    #region === 事件处理 ===
    private void txtNewName_TextChanged(object sender, EventArgs e) => UpdatePreview();
    private void chkAppendSize_CheckedChanged(object sender, EventArgs e)
    {
        _options.AppendSizeSuffix = chkAppendSize.Checked;
        pnlSizeOptions.Enabled = chkAppendSize.Checked;
        UpdatePreview();
    }
    private void cboFormat_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cboFormat.SelectedIndex >= 0)
        {
            _options.SuffixFormat = (FolderSizeSuffixFormat)cboFormat.SelectedIndex;
            UpdatePreview();
        }
    }
    private void nudDecimals_ValueChanged(object sender, EventArgs e)
    {
        _options.DecimalPlaces = (int)nudDecimals.Value;
        UpdatePreview();
    }
    private void radSuffix_CheckedChanged(object sender, EventArgs e)
    {
        if (radSuffix.Checked) { _options.Position = SuffixPosition.Suffix; UpdatePreview(); }
    }
    private void radPrefix_CheckedChanged(object sender, EventArgs e)
    {
        if (radPrefix.Checked) { _options.Position = SuffixPosition.Prefix; UpdatePreview(); }
    }
    private void chkShowCount_CheckedChanged(object sender, EventArgs e)
    {
        _options.ShowCountInPreview = chkShowCount.Checked;
        UpdatePreview();
    }

    private void btnRecalculate_Click(object sender, EventArgs e)
    {
        // P0.5：仅重新从快照聚合（API 精确计算留后续阶段）
        ComputeFromSnapshot();
        UpdatePreview();
        MessageBox.Show($"已重新计算：{FormatHumanReadable(_options.ComputedSizeBytes, _options.DecimalPlaces)}\n" +
                        $"（{_options.ComputedFileCount} 个文件 + {_options.ComputedSubFolderCount} 个子目录）\n\n" +
                        "P0.5 仅支持快照聚合计算。实时拉 API 精确大小将在后续阶段实现。",
                        "重新计算完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void btnOK_Click(object sender, EventArgs e)
    {
        var name = txtNewName.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("请输入新名称。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        // 非法字符校验
        char[] illegal = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
        if (name.IndexOfAny(illegal) >= 0)
        {
            MessageBox.Show("名称不能包含非法字符：\\ / : * ? \" < > |", "重命名失败",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (name.Length > 255)
        {
            MessageBox.Show("名称过长（≤ 255 字符）", "重命名失败",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        NewName = name;
        _options.AppendSizeSuffix = chkAppendSize.Checked;
        _options.SuffixFormat = (FolderSizeSuffixFormat)cboFormat.SelectedIndex;
        _options.DecimalPlaces = (int)nudDecimals.Value;
        _options.Position = radSuffix.Checked ? SuffixPosition.Suffix : SuffixPosition.Prefix;
        _options.ShowCountInPreview = chkShowCount.Checked;
        Result = _options;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
    #endregion
}