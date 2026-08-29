using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Forms.Games
{
    /// <summary>
    /// 工作记忆训练营：合并 N-Back、数字倒背、空间矩阵记忆、序列操作记忆四种模式。
    /// 训练前额叶核心工作记忆能力：短时存储 + 信息加工。
    /// 不依赖词库，纯前端生成题目数据。
    /// </summary>
    public class MemoryGameForm : WebView2GameFormBase
    {
        private readonly ComboBox _comboMode = new();
        private readonly ComboBox _comboDifficulty = new();
        private readonly Dictionary<string, (int totalQuestions, int challengeTimeSec)> _difficultyMap = new()
        {
            ["简单"] = (20, 0),
            ["普通"] = (25, 0),
            ["困难"] = (30, 0)
        };
        private string _currentMode = "nback";
        private string _currentDifficulty = "普通";

        protected override string FormTitle => "🧠 工作记忆训练营";

        protected override string HtmlFileRelativePath => Path.Combine("Resources", "MemoryGame", "index.html");

        public MemoryGameForm(
            IContentLoaderService contentLoaderService,
            IUserSessionService userSessionService,
            IThemeService themeService,
            IUserSettingsService settingsService,
            ILogger<MemoryGameForm> logger)
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
                Text = "🧠 工作记忆训练营",
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(180, 36),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(68, 102, 221)
            };

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
            _comboMode.Items.AddRange(new object[] { "N-Back", "数字倒背", "空间矩阵", "序列操作" });
            _comboMode.SelectedIndex = 0;
            _comboMode.SelectedIndexChanged += (s, e) =>
            {
                _currentMode = _comboMode.SelectedItem?.ToString() switch
                {
                    "N-Back" => "nback",
                    "数字倒背" => "digitSpan",
                    "空间矩阵" => "spatial",
                    "序列操作" => "sequence",
                    _ => "nback"
                };
            };

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
            lblMode.Location = new Point(195, 8);
            _comboMode.Location = new Point(245, 8);
            lblDiff.Location = new Point(385, 8);
            _comboDifficulty.Location = new Point(435, 8);
            btnStart.Location = new Point(545, 8);

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(lblMode);
            topPanel.Controls.Add(_comboMode);
            topPanel.Controls.Add(lblDiff);
            topPanel.Controls.Add(_comboDifficulty);
            topPanel.Controls.Add(btnStart);
        }

        protected override object? BuildData(LearningContext context, string themeName)
        {
            var cfg = _difficultyMap.TryGetValue(_currentDifficulty, out var c) ? c : (totalQuestions: 25, challengeTimeSec: 0);
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
            var subMode = gameRoot.TryGetProperty("subMode", out var modeProp) ? modeProp.GetString() : "";
            var difficulty = gameRoot.TryGetProperty("difficulty", out var diffProp) ? diffProp.GetString() : "";
            var timeMs = gameRoot.TryGetProperty("timeMs", out var tProp) ? tProp.GetInt64() : 0;
            var score = gameRoot.TryGetProperty("score", out var sProp) ? sProp.GetInt32() : 0;
            var correct = gameRoot.TryGetProperty("correct", out var cProp) ? cProp.GetInt32() : 0;
            var errors = gameRoot.TryGetProperty("errors", out var eProp) ? eProp.GetInt32() : 0;
            var total = gameRoot.TryGetProperty("total", out var totalProp) ? totalProp.GetInt32() : 0;

            _logger.LogInformation(
                "工作记忆训练营游戏结束: mode={Mode}, difficulty={Difficulty}, timeMs={TimeMs}, " +
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