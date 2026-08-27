using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Forms.Games
{
    /// <summary>
    /// 数独游戏窗体：经典 9×9 数独，支持三种难度、笔记模式、自动校验。
    /// 不依赖词库，纯前端生成题目。
    /// </summary>
    public class SudokuGameForm : WebView2GameFormBase
    {
        private readonly ComboBox _comboDifficulty = new();
        private readonly Dictionary<string, (int clues, int helpPenalty)> _difficultyMap = new()
        {
            ["简单"] = (40, 0),
            ["普通"] = (30, 100),
            ["困难"] = (24, 200)
        };
        private string _currentDifficulty = "普通";

        protected override string FormTitle => "🧩 数独";

        protected override string HtmlFileRelativePath => Path.Combine("Resources", "SudokuGame", "index.html");

        public SudokuGameForm(
            IContentLoaderService contentLoaderService,
            IUserSessionService userSessionService,
            IThemeService themeService,
            IUserSettingsService settingsService,
            ILogger<SudokuGameForm> logger)
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
                Text = "🧩 数独",
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(120, 36),
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
                if (_comboDifficulty.SelectedItem is string diff)
                    _currentDifficulty = diff;
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
            lblDiff.Location = new Point(140, 8);
            _comboDifficulty.Location = new Point(190, 8);
            btnStart.Location = new Point(300, 8);

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(lblDiff);
            topPanel.Controls.Add(_comboDifficulty);
            topPanel.Controls.Add(btnStart);
        }

        protected override object? BuildData(LearningContext context, string themeName)
        {
            var config = _difficultyMap.TryGetValue(_currentDifficulty, out var cfg) ? cfg : (clues: 30, helpPenalty: 100);
            return new
            {
                difficulty = _currentDifficulty,
                clueCount = config.clues,
                helpPenalty = config.helpPenalty,
                theme = themeName
            };
        }

        protected override int CountRemainingTotal(LearningContext context) => 0;

        protected override void OnGameEnd(JsonElement gameRoot, LearningContext context)
        {
            var difficulty = gameRoot.TryGetProperty("difficulty", out var diffProp) ? diffProp.GetString() : "";
            var timeMs = gameRoot.TryGetProperty("timeMs", out var tProp) ? tProp.GetInt64() : 0;
            var errors = gameRoot.TryGetProperty("errors", out var eProp) ? eProp.GetInt32() : 0;
            var score = gameRoot.TryGetProperty("score", out var sProp) ? sProp.GetInt32() : 0;
            var hintsUsed = gameRoot.TryGetProperty("hintsUsed", out var hProp) ? hProp.GetInt32() : 0;

            _logger.LogInformation(
                "数独游戏结束: difficulty={Difficulty}, timeMs={TimeMs}, errors={Errors}, score={Score}, hintsUsed={Hints}",
                difficulty, timeMs, errors, score, hintsUsed);
        }

        public override void ApplyTheme(ThemeColors colors)
        {
            base.ApplyTheme(colors);
            _comboDifficulty.BackColor = colors.Surface;
            _comboDifficulty.ForeColor = colors.TextPrimary;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _comboDifficulty.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}