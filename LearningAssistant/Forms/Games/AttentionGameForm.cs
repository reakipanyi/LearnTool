using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Forms.Games
{
    /// <summary>
    /// 分配注意力训练营：合并双任务反应、多重目标追踪、双任务计数三种模式。
    /// 训练同时处理多项任务的能力。
    /// 不依赖词库，纯前端生成题目数据。
    /// </summary>
    public class AttentionGameForm : WebView2GameFormBase
    {
        private readonly ComboBox _comboMode = new();
        private readonly ComboBox _comboDifficulty = new();
        private readonly Dictionary<string, (int totalQuestions, int challengeTimeSec)> _difficultyMap = new()
        {
            ["简单"] = (8, 0),
            ["普通"] = (12, 0),
            ["困难"] = (16, 0)
        };
        private string _currentMode = "dualtask";
        private string _currentDifficulty = "普通";

        protected override string FormTitle => "🎯 分配注意力训练营";

        protected override string HtmlFileRelativePath => Path.Combine("Resources", "AttentionGame", "index.html");

        public AttentionGameForm(
            IContentLoaderService contentLoaderService,
            IUserSessionService userSessionService,
            IThemeService themeService,
            IUserSettingsService settingsService,
            ILogger<AttentionGameForm> logger)
            : base(contentLoaderService, userSessionService, themeService, settingsService, logger)
        {
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RebuildTopBar();
        }

        private void RebuildTopBar()
        {
            if (Controls.Count < 2) return;
            var topPanel = Controls[1] as Panel;
            if (topPanel == null) return;

            topPanel.Controls.Clear();

            var lblTitle = new Label
            {
                Text = "🎯 分配注意力训练营",
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(200, 36),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(68, 102, 221)
            };

            // 模式选择
            var lblMode = new Label
            {
                Text = "模式：",
                Font = new Font("微软雅黑", 10F),
                AutoSize = false,
                Size = new Size(50, 36),
                TextAlign = ContentAlignment.MiddleRight
            };
            _comboMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboMode.Font = new Font("微软雅黑", 10F);
            _comboMode.Width = 150;
            _comboMode.Items.AddRange(new object[] { "双任务反应", "多重目标追踪", "双任务计数" });
            _comboMode.SelectedIndex = 0;
            _comboMode.SelectedIndexChanged += (s, e) =>
            {
                _currentMode = _comboMode.SelectedItem?.ToString() switch
                {
                    "双任务反应" => "dualtask",
                    "多重目标追踪" => "mot",
                    "双任务计数" => "dualcount",
                    _ => "dualtask"
                };
            };

            // 难度选择
            var lblDiff = new Label
            {
                Text = "难度：",
                Font = new Font("微软雅黑", 10F),
                AutoSize = false,
                Size = new Size(50, 36),
                TextAlign = ContentAlignment.MiddleRight
            };
            _comboDifficulty.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboDifficulty.Font = new Font("微软雅黑", 10F);
            _comboDifficulty.Width = 100;
            _comboDifficulty.Items.AddRange(new object[] { "简单", "普通", "困难" });
            _comboDifficulty.SelectedItem = _currentDifficulty;
            _comboDifficulty.SelectedIndexChanged += (s, e) =>
            {
                if (_comboDifficulty.SelectedItem is string diff) _currentDifficulty = diff;
            };

            var btnStart = new Button
            {
                Text = "🎮 开始游戏",
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(99, 102, 241),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width = 120,
                Height = 36
            };
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Click += (s, e) => StartGame();

            lblTitle.Location = new Point(8, 8);
            lblMode.Location = new Point(215, 8);
            _comboMode.Location = new Point(265, 8);
            lblDiff.Location = new Point(425, 8);
            _comboDifficulty.Location = new Point(475, 8);
            btnStart.Location = new Point(585, 8);

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(lblMode);
            topPanel.Controls.Add(_comboMode);
            topPanel.Controls.Add(lblDiff);
            topPanel.Controls.Add(_comboDifficulty);
            topPanel.Controls.Add(btnStart);
        }

        protected override object? BuildData(LearningContext context, string themeName)
        {
            var cfg = _difficultyMap.TryGetValue(_currentDifficulty, out var c) ? c : (totalQuestions: 12, challengeTimeSec: 0);
            return new
            {
                mode = _currentMode,
                difficulty = _currentDifficulty,
                totalQuestions = cfg.totalQuestions,
                challengeTimeSec = cfg.challengeTimeSec,
                theme = themeName
            };
        }

        protected override int CountRemainingTotal(LearningContext context) => 0;

        protected override void OnGameEnd(JsonElement gameRoot, LearningContext context)
        {
            var mode = gameRoot.TryGetProperty("mode", out var mProp) ? mProp.GetString() : "";
            var subMode = gameRoot.TryGetProperty("subMode", out var smProp) ? smProp.GetString() : "";
            var difficulty = gameRoot.TryGetProperty("difficulty", out var diffProp) ? diffProp.GetString() : "";
            var timeMs = gameRoot.TryGetProperty("timeMs", out var tProp) ? tProp.GetInt64() : 0;
            var score = gameRoot.TryGetProperty("score", out var sProp) ? sProp.GetInt32() : 0;
            var correct = gameRoot.TryGetProperty("correct", out var cProp) ? cProp.GetInt32() : 0;
            var errors = gameRoot.TryGetProperty("errors", out var eProp) ? eProp.GetInt32() : 0;
            var total = gameRoot.TryGetProperty("total", out var totalProp) ? totalProp.GetInt32() : 0;

            _logger.LogInformation(
                "分配注意力训练营游戏结束: mode={Mode}, subMode={SubMode}, difficulty={Difficulty}, " +
                "timeMs={TimeMs}, score={Score}, correct={Correct}, errors={Errors}, total={Total}",
                mode, subMode, difficulty, timeMs, score, correct, errors, total);
        }

        public override void ApplyTheme(ThemeColors colors)
        {
            base.ApplyTheme(colors);
            _comboMode.BackColor = colors.Surface;
            _comboMode.ForeColor = colors.TextPrimary;
            _comboDifficulty.BackColor = colors.Surface;
            _comboDifficulty.ForeColor = colors.TextPrimary;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _comboMode.Dispose();
                _comboDifficulty.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}