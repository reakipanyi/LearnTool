using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Forms.Games
{
    /// <summary>
    /// 斯特鲁普（Stroop）游戏窗体：抑制自动读字冲动，训练认知抑制能力。
    /// 三种模式：颜色-词语 Stroop、图形 Stroop、数字 Stroop。
    /// 不依赖词库，纯前端生成题目数据。
    /// </summary>
    public class StroopGameForm : WebView2GameFormBase
    {
        private readonly ComboBox _comboMode = new();
        private readonly ComboBox _comboDifficulty = new();
        private readonly Dictionary<string, (int totalQuestions, int challengeTimeSec)> _difficultyMap = new()
        {
            ["简单"] = (10, 0),
            ["普通"] = (15, 0),
            ["困难"] = (20, 0)
        };
        private string _currentMode = "colorWord";
        private string _currentDifficulty = "普通";

        protected override string FormTitle => "🧠 斯特鲁普";

        protected override string HtmlFileRelativePath => Path.Combine("Resources", "StroopGame", "index.html");

        public StroopGameForm(
            IContentLoaderService contentLoaderService,
            IUserSessionService userSessionService,
            IThemeService themeService,
            IUserSettingsService settingsService,
            ILogger<StroopGameForm> logger)
            : base(contentLoaderService, userSessionService, themeService, settingsService, logger)
        {
        }

        /// <summary>重写基类 UI 构建：将科目/子类别/行列控件替换为模式与难度下拉。</summary>
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

            // 游戏标题
            var lblTitle = new Label
            {
                Text = "🧠 斯特鲁普",
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(140, 36),
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
            _comboMode.Width = 130;
            _comboMode.Items.AddRange(new object[] { "颜色-词语", "图形", "数字" });
            _comboMode.SelectedIndex = 0;
            _comboMode.SelectedIndexChanged += (s, e) =>
            {
                _currentMode = _comboMode.SelectedItem?.ToString() switch
                {
                    "颜色-词语" => "colorWord",
                    "图形" => "shape",
                    "数字" => "number",
                    _ => "colorWord"
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
                if (_comboDifficulty.SelectedItem is string diff)
                {
                    _currentDifficulty = diff;
                }
            };

            // 开始游戏按钮
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

            // 布局
            lblTitle.Location = new Point(8, 8);
            lblMode.Location = new Point(155, 8);
            _comboMode.Location = new Point(205, 8);
            lblDiff.Location = new Point(345, 8);
            _comboDifficulty.Location = new Point(395, 8);
            btnStart.Location = new Point(505, 8);

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(lblMode);
            topPanel.Controls.Add(_comboMode);
            topPanel.Controls.Add(lblDiff);
            topPanel.Controls.Add(_comboDifficulty);
            topPanel.Controls.Add(btnStart);
        }

        /// <summary>返回游戏配置数据，不依赖词库。</summary>
        protected override object? BuildData(LearningContext context, string themeName)
        {
            var cfg = _difficultyMap.TryGetValue(_currentDifficulty, out var c) ? c : (totalQuestions: 15, challengeTimeSec: 0);
            return new
            {
                mode = _currentMode,
                difficulty = _currentDifficulty,
                totalQuestions = cfg.totalQuestions,
                challengeTimeSec = cfg.challengeTimeSec,
                theme = themeName
            };
        }

        /// <summary>统计剩余条目（斯特鲁普无词库，返回 0）。</summary>
        protected override int CountRemainingTotal(LearningContext context) => 0;

        /// <summary>处理前端上报的游戏结束消息。</summary>
        protected override void OnGameEnd(JsonElement gameRoot, LearningContext context)
        {
            var subMode = gameRoot.TryGetProperty("subMode", out var modeProp) ? modeProp.GetString() : "";
            var difficulty = gameRoot.TryGetProperty("difficulty", out var diffProp) ? diffProp.GetString() : "";
            var timeMs = gameRoot.TryGetProperty("timeMs", out var tProp) ? tProp.GetInt64() : 0;
            var score = gameRoot.TryGetProperty("score", out var sProp) ? sProp.GetInt32() : 0;
            var correct = gameRoot.TryGetProperty("correct", out var cProp) ? cProp.GetInt32() : 0;
            var errors = gameRoot.TryGetProperty("errors", out var eProp) ? eProp.GetInt32() : 0;
            var total = gameRoot.TryGetProperty("total", out var totalProp) ? totalProp.GetInt32() : 0;

            _logger.LogInformation(
                "斯特鲁普游戏结束: mode={Mode}, difficulty={Difficulty}, timeMs={TimeMs}, " +
                "score={Score}, correct={Correct}, errors={Errors}, total={Total}",
                subMode, difficulty, timeMs, score, correct, errors, total);
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