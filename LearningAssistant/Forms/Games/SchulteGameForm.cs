using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Forms.Games
{
    /// <summary>
    /// 舒尔特方格游戏窗体：按顺序点击随机打乱的数字方格，锻炼专注力与视觉广度。
    /// 不依赖词库，纯前端生成盘面数据。
    /// </summary>
    public class SchulteGameForm : WebView2GameFormBase
    {
        private readonly ComboBox _comboDifficulty = new();
        private readonly Dictionary<string, (int gridSize, int challengeTime)> _difficultyMap = new()
        {
            ["简单"] = (3, 30),
            ["普通"] = (5, 90),
            ["困难"] = (6, 120),
            ["专家"] = (7, 150)
        };
        private string _currentDifficulty = "普通";

        protected override string FormTitle => "🔢 舒尔特方格";

        protected override string HtmlFileRelativePath => Path.Combine("Resources", "SchulteGame", "index.html");

        public SchulteGameForm(
            IContentLoaderService contentLoaderService,
            IUserSessionService userSessionService,
            IThemeService themeService,
            IUserSettingsService settingsService,
            ILogger<SchulteGameForm> logger)
            : base(contentLoaderService, userSessionService, themeService, settingsService, logger)
        {
        }

        /// <summary>重写基类 UI 构建：将科目/子类别/行列控件替换为难度下拉。</summary>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RebuildTopBar();
        }

        private void RebuildTopBar()
        {
            // 找到顶部栏的第一个 panel（基类 BuildUi 创建的 topPanel）
            if (Controls.Count < 2) return;
            var topPanel = Controls[1] as Panel;
            if (topPanel == null) return;

            // 清空原有控件
            topPanel.Controls.Clear();

            // 游戏标题
            var lblTitle = new Label
            {
                Text = "🔢 舒尔特方格",
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(160, 36),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(68, 102, 221)
            };

            // 难度下拉
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
            _comboDifficulty.Items.AddRange(new object[] { "简单", "普通", "困难", "专家" });
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
            lblDiff.Location = new Point(180, 8);
            _comboDifficulty.Location = new Point(230, 8);
            btnStart.Location = new Point(340, 8);

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(lblDiff);
            topPanel.Controls.Add(_comboDifficulty);
            topPanel.Controls.Add(btnStart);
        }

        /// <summary>返回游戏配置数据，不依赖词库。</summary>
        protected override object? BuildData(LearningContext context, string themeName)
        {
            var config = _difficultyMap.TryGetValue(_currentDifficulty, out var cfg) ? cfg : (gridSize: 5, challengeTime: 90);
            return new
            {
                difficulty = _currentDifficulty,
                gridSize = cfg.gridSize,
                challengeTimeSec = cfg.challengeTime,
                theme = themeName
            };
        }

        /// <summary>统计剩余条目（舒尔特无词库，返回 0）。</summary>
        protected override int CountRemainingTotal(LearningContext context) => 0;

        /// <summary>处理前端上报的游戏结束消息（首版仅记录日志）。</summary>
        protected override void OnGameEnd(JsonElement gameRoot, LearningContext context)
        {
            var mode = gameRoot.TryGetProperty("mode", out var modeProp) ? modeProp.GetString() : "";
            var difficulty = gameRoot.TryGetProperty("difficulty", out var diffProp) ? diffProp.GetString() : "";
            var timeMs = gameRoot.TryGetProperty("timeMs", out var tProp) ? tProp.GetInt64() : 0;
            var errors = gameRoot.TryGetProperty("errors", out var eProp) ? eProp.GetInt32() : 0;
            var score = gameRoot.TryGetProperty("score", out var sProp) ? sProp.GetInt32() : 0;

            _logger.LogInformation(
                "舒尔特方格游戏结束: mode={Mode}, difficulty={Difficulty}, timeMs={TimeMs}, errors={Errors}, score={Score}",
                mode, difficulty, timeMs, errors, score);
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