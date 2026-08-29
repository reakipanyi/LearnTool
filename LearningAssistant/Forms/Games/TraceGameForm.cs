using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Forms.Games
{
    /// <summary>
    /// 线条追踪：一堆互相缠绕的曲线，用眼睛追踪起点到终点的对应关系。
    /// 训练持续注意力和视觉分辨能力。
    /// 不依赖词库，纯前端 Canvas 渲染。
    /// </summary>
    public class TraceGameForm : WebView2GameFormBase
    {
        private readonly ComboBox _comboDifficulty = new();
        private readonly Dictionary<string, (int totalQuestions, int challengeTimeSec)> _difficultyMap = new()
        {
            ["简单"] = (6, 0),
            ["普通"] = (8, 0),
            ["困难"] = (10, 0)
        };
        private string _currentDifficulty = "普通";

        protected override string FormTitle => "🌀 线条追踪";

        protected override string HtmlFileRelativePath => Path.Combine("Resources", "TraceGame", "index.html");

        public TraceGameForm(
            IContentLoaderService contentLoaderService,
            IUserSessionService userSessionService,
            IThemeService themeService,
            IUserSettingsService settingsService,
            ILogger<TraceGameForm> logger)
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
                Text = "🌀 线条追踪",
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(160, 36),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(68, 102, 221)
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
            lblDiff.Location = new Point(175, 8);
            _comboDifficulty.Location = new Point(225, 8);
            btnStart.Location = new Point(335, 8);

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(lblDiff);
            topPanel.Controls.Add(_comboDifficulty);
            topPanel.Controls.Add(btnStart);
        }

        protected override object? BuildData(LearningContext context, string themeName)
        {
            var cfg = _difficultyMap.TryGetValue(_currentDifficulty, out var c) ? c : (totalQuestions: 8, challengeTimeSec: 0);
            return new
            {
                difficulty = _currentDifficulty,
                lineCount = cfg.totalQuestions,
                challengeTimeSec = cfg.challengeTimeSec,
                theme = themeName
            };
        }

        protected override int CountRemainingTotal(LearningContext context) => 0;

        protected override void OnGameEnd(JsonElement gameRoot, LearningContext context)
        {
            var difficulty = gameRoot.TryGetProperty("difficulty", out var diffProp) ? diffProp.GetString() : "";
            var timeMs = gameRoot.TryGetProperty("timeMs", out var tProp) ? tProp.GetInt64() : 0;
            var score = gameRoot.TryGetProperty("score", out var sProp) ? sProp.GetInt32() : 0;
            var correct = gameRoot.TryGetProperty("correct", out var cProp) ? cProp.GetInt32() : 0;
            var errors = gameRoot.TryGetProperty("errors", out var eProp) ? eProp.GetInt32() : 0;
            var total = gameRoot.TryGetProperty("total", out var totalProp) ? totalProp.GetInt32() : 0;

            _logger.LogInformation(
                "线条追踪游戏结束: difficulty={Difficulty}, timeMs={TimeMs}, " +
                "score={Score}, correct={Correct}, errors={Errors}, total={Total}",
                difficulty, timeMs, score, correct, errors, total);
        }

        public override void ApplyTheme(ThemeColors colors)
        {
            base.ApplyTheme(colors);
            _comboDifficulty.BackColor = colors.Surface;
            _comboDifficulty.ForeColor = colors.TextPrimary;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _comboDifficulty.Dispose();
            base.Dispose(disposing);
        }
    }
}