using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.Practice;
using LearningAssistant.Platform.Media;
using LearningAssistant.Services.Media;
using LearningAssistant.Services.Practice;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Forms.Learning
{
    /// <summary>
    /// 单词练习窗口（P2）：提供跟读（TTS 示范 + 录音 + 参考分）与听写（听音写词）。
    /// 结果统一写入练习记录；听写未通过时由服务侧发布 ItemWrongEvent 进入错题本。
    /// </summary>
    public sealed class WordPracticeForm : Form, IThemeable
    {
        private readonly LearningItem _item;
        private readonly IWordPracticeService _practice;
        private readonly ILogger<WordPracticeForm>? _logger;
        private readonly IThemeService? _themeService;
        private readonly ISubtitleParser? _subtitleParser;

        private readonly Panel _headerPanel = new();
        private readonly Label _labelWord = new();
        private readonly Label _labelMeta = new();
        private readonly Button _buttonSpeak = new();
        private readonly TabControl _tabs = new();

        private readonly TabPage _tabShadow = new();
        private readonly Label _labelShadowHint = new();
        private readonly Button _buttonShadowPlay = new();
        private readonly Button _buttonShadowStart = new();
        private readonly Button _buttonShadowStop = new();
        private readonly Button _buttonShadowReplay = new();
        private readonly Label _labelShadowStatus = new();
        private readonly Label _labelShadowResult = new();
        private readonly System.Windows.Forms.Timer _recordingTimer = new() { Interval = 200 };

        private readonly TabPage _tabDictation = new();
        private readonly Label _labelDictationHint = new();
        private readonly Button _buttonDictationPlay = new();
        private readonly TextBox _textBoxDictationInput = new();
        private readonly Button _buttonDictationCheck = new();
        private readonly Label _labelDictationResult = new();
        private readonly Label _labelDictationDetail = new();

        private NAudioMediaPlayer? _replayPlayer;
        private string? _lastRecordingPath;
        private DateTime _recordingStartedAt;
        private bool _isRecording;
        private bool _isBusy;
        private bool _isClosed;

        public WordPracticeForm(
            LearningItem item,
            IWordPracticeService practice,
            ILogger<WordPracticeForm>? logger = null,
            IThemeService? themeService = null)
        {
            _item = item ?? throw new ArgumentNullException(nameof(item));
            _practice = practice ?? throw new ArgumentNullException(nameof(practice));
            _logger = logger;
            _themeService = themeService;
            _subtitleParser = Program.GetService<ISubtitleParser>();

            InitializeComponent();
            _themeService?.RegisterThemeable(this);
            BuildHeaderText();
            UpdateShadowControls();
        }

        private void InitializeComponent()
        {
            Text = "单词练习";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(720, 520);
            MinimumSize = new Size(640, 460);
            Font = new Font("微软雅黑", 9.5F);
            BackColor = Color.FromArgb(248, 250, 252);
            KeyPreview = true;

            BuildHeader();
            BuildTabs();
            BuildShadowTab();
            BuildDictationTab();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(16, 12, 16, 12)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.Controls.Add(_headerPanel, 0, 0);
            root.Controls.Add(_tabs, 0, 1);
            Controls.Add(root);

            _recordingTimer.Tick += RecordingTimer_Tick;
            FormClosed += WordPracticeForm_FormClosed;
        }

        private void BuildHeader()
        {
            _headerPanel.Dock = DockStyle.Fill;
            _headerPanel.BackColor = Color.White;

            _labelWord.AutoSize = false;
            _labelWord.Dock = DockStyle.Top;
            _labelWord.Height = 40;
            _labelWord.Font = new Font("微软雅黑", 18F, FontStyle.Bold);
            _labelWord.ForeColor = Color.FromArgb(15, 23, 42);
            _labelWord.TextAlign = ContentAlignment.MiddleLeft;

            _labelMeta.AutoSize = false;
            _labelMeta.Dock = DockStyle.Top;
            _labelMeta.Height = 24;
            _labelMeta.Font = new Font("微软雅黑", 9F);
            _labelMeta.ForeColor = Color.FromArgb(100, 116, 139);
            _labelMeta.TextAlign = ContentAlignment.MiddleLeft;

            _buttonSpeak.Text = "🔊 播放读音";
            _buttonSpeak.Size = new Size(110, 34);
            _buttonSpeak.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _buttonSpeak.Location = new Point(560, 14);
            _buttonSpeak.BackColor = Color.FromArgb(139, 92, 246);
            _buttonSpeak.ForeColor = Color.White;
            _buttonSpeak.FlatStyle = FlatStyle.Flat;
            _buttonSpeak.FlatAppearance.BorderSize = 0;
            _buttonSpeak.Font = new Font("微软雅黑", 9.5F, FontStyle.Bold);
            _buttonSpeak.Cursor = Cursors.Hand;
            _buttonSpeak.Click += ButtonSpeak_Click;

            _headerPanel.Controls.Add(_buttonSpeak);
            _headerPanel.Controls.Add(_labelMeta);
            _headerPanel.Controls.Add(_labelWord);
        }

        private void BuildTabs()
        {
            _tabs.Dock = DockStyle.Fill;
            _tabs.Font = new Font("微软雅黑", 10F, FontStyle.Bold);

            _tabShadow.Text = "🎤 跟读";
            _tabDictation.Text = "⌨️ 听写";
            _tabs.TabPages.Add(_tabShadow);
            _tabs.TabPages.Add(_tabDictation);
        }

        private void BuildShadowTab()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(12, 14, 12, 12)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _labelShadowHint.Text = "① 播放示范 → ② 开始跟读 → ③ 停止并评分（跟读只记练习，不进错题本）";
            _labelShadowHint.Dock = DockStyle.Fill;
            _labelShadowHint.ForeColor = Color.FromArgb(100, 116, 139);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight
            };

            ConfigureShadowButton(_buttonShadowPlay, "🔊 播放示范", Color.FromArgb(59, 130, 246));
            ConfigureShadowButton(_buttonShadowStart, "🎙 开始跟读", Color.FromArgb(34, 197, 94));
            ConfigureShadowButton(_buttonShadowStop, "⏹ 停止并评分", Color.FromArgb(239, 68, 68));
            ConfigureShadowButton(_buttonShadowReplay, "▶ 回放录音", Color.FromArgb(100, 116, 139));

            _buttonShadowPlay.Click += ButtonShadowPlay_Click;
            _buttonShadowStart.Click += ButtonShadowStart_Click;
            _buttonShadowStop.Click += ButtonShadowStop_Click;
            _buttonShadowReplay.Click += ButtonShadowReplay_Click;

            buttons.Controls.Add(_buttonShadowPlay);
            buttons.Controls.Add(_buttonShadowStart);
            buttons.Controls.Add(_buttonShadowStop);
            buttons.Controls.Add(_buttonShadowReplay);

            _labelShadowStatus.Dock = DockStyle.Fill;
            _labelShadowStatus.TextAlign = ContentAlignment.MiddleLeft;
            _labelShadowStatus.ForeColor = Color.FromArgb(51, 65, 85);
            _labelShadowStatus.Text = "准备就绪";

            _labelShadowResult.Dock = DockStyle.Fill;
            _labelShadowResult.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            _labelShadowResult.ForeColor = Color.FromArgb(139, 92, 246);
            _labelShadowResult.TextAlign = ContentAlignment.TopLeft;

            layout.Controls.Add(_labelShadowHint, 0, 0);
            layout.Controls.Add(buttons, 0, 1);
            layout.Controls.Add(_labelShadowStatus, 0, 2);
            layout.Controls.Add(_labelShadowResult, 0, 3);

            _tabShadow.Controls.Add(layout);
        }

        private void BuildDictationTab()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(12, 14, 12, 12)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _labelDictationHint.Text = "① 播放读音 → ② 输入听到的内容 → ③ 提交判定（错误自动进入错题本）";
            _labelDictationHint.Dock = DockStyle.Fill;
            _labelDictationHint.ForeColor = Color.FromArgb(100, 116, 139);

            var playPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight
            };
            ConfigureShadowButton(_buttonDictationPlay, "🔊 播放读音", Color.FromArgb(59, 130, 246));
            _buttonDictationPlay.Click += ButtonDictationPlay_Click;
            playPanel.Controls.Add(_buttonDictationPlay);

            _textBoxDictationInput.Dock = DockStyle.Fill;
            _textBoxDictationInput.Font = new Font("微软雅黑", 12F);
            _textBoxDictationInput.BorderStyle = BorderStyle.FixedSingle;
            _textBoxDictationInput.PlaceholderText = "请输入听到的单词";

            var checkPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight
            };
            ConfigureShadowButton(_buttonDictationCheck, "✅ 提交判定", Color.FromArgb(139, 92, 246));
            _buttonDictationCheck.Click += ButtonDictationCheck_Click;
            checkPanel.Controls.Add(_buttonDictationCheck);

            _labelDictationResult.Dock = DockStyle.Fill;
            _labelDictationResult.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            _labelDictationResult.ForeColor = Color.FromArgb(15, 23, 42);
            _labelDictationResult.TextAlign = ContentAlignment.TopLeft;

            _labelDictationDetail.Dock = DockStyle.Fill;
            _labelDictationDetail.ForeColor = Color.FromArgb(100, 116, 139);
            _labelDictationDetail.TextAlign = ContentAlignment.TopLeft;

            layout.Controls.Add(_labelDictationHint, 0, 0);
            layout.Controls.Add(playPanel, 0, 1);
            layout.Controls.Add(_textBoxDictationInput, 0, 2);
            layout.Controls.Add(checkPanel, 0, 3);
            layout.Controls.Add(_labelDictationResult, 0, 4);
            layout.Controls.Add(_labelDictationDetail, 0, 5);

            _tabDictation.Controls.Add(layout);
        }

        private static void ConfigureShadowButton(Button button, string text, Color backColor)
        {
            button.Text = text;
            button.AutoSize = false;
            button.Size = new Size(132, 40);
            button.Margin = new Padding(0, 0, 10, 0);
            button.BackColor = backColor;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("微软雅黑", 9.5F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

        private void BuildHeaderText()
        {
            _labelWord.Text = _item.GetMainContent();
            var pronunciation = _item.GetPronunciation();
            if (!string.IsNullOrWhiteSpace(pronunciation))
                _labelWord.Text += $"  /{pronunciation}/";

            var metaParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(_item.KnowledgePointPath))
                metaParts.Add(_item.KnowledgePointPath);
            metaParts.Add(_item.SubCategory.ToString());
            metaParts.Add(_item.AgeRange.ToDisplayString());
            metaParts.Add($"重要度：{_item.Importance.ToDisplayString()}");
            if (!string.IsNullOrWhiteSpace(_item.GetDisplayText())
                && !string.Equals(_item.GetDisplayText(), _item.GetMainContent(), StringComparison.Ordinal))
            {
                metaParts.Add(_item.GetDisplayText());
            }

            _labelMeta.Text = string.Join(" · ", metaParts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        private void UpdateShadowControls()
        {
            var canRecord = !_isRecording && !_isBusy;
            _buttonShadowStart.Enabled = canRecord;
            _buttonShadowStop.Enabled = _isRecording && !_isBusy;
            _buttonShadowPlay.Enabled = !_isBusy;
            _buttonShadowReplay.Enabled = !_isRecording && !_isBusy
                && !string.IsNullOrWhiteSpace(_lastRecordingPath)
                && File.Exists(_lastRecordingPath);
            _buttonDictationPlay.Enabled = !_isBusy;
            _buttonDictationCheck.Enabled = !_isBusy;
        }

        private async void ButtonSpeak_Click(object? sender, EventArgs e)
        {
            await PlayModelAsync();
        }

        private async void ButtonShadowPlay_Click(object? sender, EventArgs e)
        {
            await PlayModelAsync();
        }

        private async void ButtonDictationPlay_Click(object? sender, EventArgs e)
        {
            await PlayModelAsync();
        }

        private async Task PlayModelAsync()
        {
            if (_isBusy) return;
            _isBusy = true;
            UpdateShadowControls();
            try
            {
                await _practice.PlayModelAsync(_item);
                _labelShadowStatus.Text = "已播放示范读音";
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "播放单词示范失败");
                _labelShadowStatus.Text = $"播放失败：{ex.Message}";
            }
            finally
            {
                _isBusy = false;
                UpdateShadowControls();
            }
        }

        private void ButtonShadowStart_Click(object? sender, EventArgs e)
        {
            if (_isRecording || _isBusy) return;

            try
            {
                if (!_practice.StartShadowing(_item))
                {
                    _labelShadowStatus.Text = "录音启动失败，请检查麦克风或稍后再试";
                    return;
                }

                _isRecording = true;
                _recordingStartedAt = DateTime.Now;
                _labelShadowStatus.Text = "录音中… 0.0s";
                _labelShadowResult.Text = string.Empty;
                _recordingTimer.Start();
                UpdateShadowControls();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "启动单词跟读失败");
                _isRecording = false;
                _labelShadowStatus.Text = $"录音启动失败：{ex.Message}";
                UpdateShadowControls();
            }
        }

        private async void ButtonShadowStop_Click(object? sender, EventArgs e)
        {
            if (!_isRecording || _isBusy) return;

            _isBusy = true;
            _recordingTimer.Stop();
            UpdateShadowControls();

            try
            {
                var record = await _practice.StopShadowingAsync(_item);
                _isRecording = false;

                if (record == null)
                {
                    _labelShadowStatus.Text = "未能获取本次录音，请重试";
                    return;
                }

                _lastRecordingPath = record.RecordingPath;

                if (_practice.IsPronunciationScoringAvailable)
                {
                    _labelShadowResult.Text = $"参考分 {Math.Round(record.Score)} / 100";
                }
                else
                {
                    _labelShadowResult.Text = "已完成跟读记录";
                    _labelShadowStatus.Text = _practice.PronunciationUnavailableReason
                        ?? "未安装英语识别器，不计发音准确度";
                }

                if (_practice.IsPronunciationScoringAvailable)
                {
                    _labelShadowStatus.Text = $"录音 {Math.Max(0, record.DurationMs) / 1000.0:0.0}s，已写入练习记录";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "结束单词跟读失败");
                _isRecording = false;
                _labelShadowStatus.Text = $"评分失败：{ex.Message}";
            }
            finally
            {
                _isBusy = false;
                UpdateShadowControls();
            }
        }

        private async void ButtonShadowReplay_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_lastRecordingPath) || !File.Exists(_lastRecordingPath))
                return;

            try
            {
                if (_replayPlayer == null)
                {
                    if (_subtitleParser == null)
                    {
                        _labelShadowStatus.Text = "回放组件不可用";
                        return;
                    }

                    _replayPlayer = new NAudioMediaPlayer(_subtitleParser);
                }

                _replayPlayer.Load(_lastRecordingPath);
                await _replayPlayer.PlayAsync();
                _labelShadowStatus.Text = "正在回放录音";
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "回放单词录音失败");
                _labelShadowStatus.Text = $"回放失败：{ex.Message}";
            }
        }

        private async void ButtonDictationCheck_Click(object? sender, EventArgs e)
        {
            if (_isBusy) return;

            _isBusy = true;
            UpdateShadowControls();
            try
            {
                var check = _practice.CheckDictation(_item, _textBoxDictationInput.Text);
                var record = await _practice.RecordDictationAsync(_item, _textBoxDictationInput.Text, check);

                if (check.IsCorrect)
                {
                    _labelDictationResult.ForeColor = Color.FromArgb(34, 197, 94);
                    _labelDictationResult.Text = $"✅ 正确（匹配度 {check.Score} / 100）";
                    _labelDictationDetail.Text = $"用时 {record.DurationMs / 1000.0:0.0}s，已写入练习记录";
                }
                else
                {
                    _labelDictationResult.ForeColor = Color.FromArgb(239, 68, 68);
                    _labelDictationResult.Text = $"❌ 拼写不符（匹配度 {check.Score} / 100）";
                    _labelDictationDetail.Text = $"正确写法：{check.Expected}\n已自动加入错题本，可在复习中巩固。";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "听写判定失败");
                _labelDictationResult.ForeColor = Color.FromArgb(239, 68, 68);
                _labelDictationResult.Text = $"判定失败：{ex.Message}";
            }
            finally
            {
                _isBusy = false;
                UpdateShadowControls();
            }
        }

        private void RecordingTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isRecording) return;
            var elapsed = (DateTime.Now - _recordingStartedAt).TotalSeconds;
            _labelShadowStatus.Text = $"录音中… {elapsed:0.0}s";
            if (elapsed >= 30)
            {
                // 单词跟读不需要长录音，超时自动结束避免麦克风一直被占用。
                _recordingTimer.Stop();
                ButtonShadowStop_Click(this, EventArgs.Empty);
            }
        }

        private void WordPracticeForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            _isClosed = true;
            _recordingTimer.Stop();
            _recordingTimer.Dispose();

            if (_isRecording)
            {
                // 关闭时不阻塞 UI 线程，避免与录音完成回调在同一线程上互相等待。
                _ = StopRecordingOnCloseAsync();
            }

            try
            {
                _replayPlayer?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "释放回放组件失败");
            }
            finally
            {
                _replayPlayer = null;
            }
        }

        private async Task StopRecordingOnCloseAsync()
        {
            try
            {
                await _practice.StopShadowingAsync(_item);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "关闭窗口时结束录音失败");
            }
        }

        public void ApplyTheme(ThemeColors colors)
        {
            if (_isClosed) return;

            BackColor = colors.Background;
            _headerPanel.BackColor = colors.Surface;
            _tabs.BackColor = colors.Background;
            _tabShadow.BackColor = colors.Background;
            _tabDictation.BackColor = colors.Background;

            _labelWord.ForeColor = colors.TextPrimary;
            _labelMeta.ForeColor = colors.TextSecondary;
            _labelShadowHint.ForeColor = colors.TextSecondary;
            _labelShadowStatus.ForeColor = colors.TextSecondary;
            _labelShadowResult.ForeColor = colors.Primary;
            _labelDictationHint.ForeColor = colors.TextSecondary;
            _labelDictationDetail.ForeColor = colors.TextSecondary;

            _textBoxDictationInput.BackColor = colors.Surface;
            _textBoxDictationInput.ForeColor = colors.TextPrimary;

            var shadowResultColor = _labelDictationResult.ForeColor;
            if (shadowResultColor == Color.FromArgb(34, 197, 94)) _labelDictationResult.ForeColor = colors.Success;
            else if (shadowResultColor == Color.FromArgb(239, 68, 68)) _labelDictationResult.ForeColor = colors.Error;
            else _labelDictationResult.ForeColor = colors.TextPrimary;
        }
    }
}
