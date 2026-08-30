using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Forms.Games
{
    public partial class SpellingGameForm : WebView2GameFormBase
    {
        private readonly WordMatchGameService _gameService;

        private readonly Label _labelTotal = new();
        private readonly Label _labelCorrect = new();
        private readonly Label _labelWrong = new();
        private readonly Label _labelRemaining = new();

        private int _currentGameTotalItems;

        protected override string FormTitle => "✍️ 单词拼写";

        protected override string HtmlFileRelativePath => Path.Combine("Resources", "SpellingGame", "index.html");

        protected override bool ShowRowColConfig => false;

        public SpellingGameForm(
            WordMatchGameService gameService,
            IContentLoaderService contentLoaderService,
            IUserSessionService userSessionService,
            IThemeService themeService,
            IUserSettingsService settingsService,
            ILogger<SpellingGameForm> logger)
            : base(contentLoaderService, userSessionService, themeService, settingsService, logger)
        {
            _gameService = gameService;
        }

        protected override void OnTopPanelBuilt(Panel topPanel)
        {
            void SetupLabel(Label lbl, string text, int x, Color color)
            {
                lbl.Text = text;
                lbl.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
                lbl.AutoSize = false;
                lbl.Size = new Size(100, 34);
                lbl.TextAlign = ContentAlignment.MiddleLeft;
                lbl.Location = new Point(x, 8);
                lbl.ForeColor = color;
            }

            SetupLabel(_labelTotal, "总共: 0", 8, Color.FromArgb(59, 130, 246));
            SetupLabel(_labelCorrect, "正确: 0", 118, Color.FromArgb(34, 197, 94));
            SetupLabel(_labelWrong, "错误: 0", 228, Color.FromArgb(239, 68, 68));
            SetupLabel(_labelRemaining, "剩余: 0", 338, Color.FromArgb(234, 179, 8));

            topPanel.Controls.Add(_labelTotal);
            topPanel.Controls.Add(_labelCorrect);
            topPanel.Controls.Add(_labelWrong);
            topPanel.Controls.Add(_labelRemaining);

            _currentGameTotalItems = 0;
        }

        protected override object? BuildData(LearningContext context, string themeName)
        {
            var items = _gameService.BuildItems(context, maxCount: MaxCountForGrid(), selection: WordSelection.WrongFirst,
                excludeIds: SkipKnown ? ExcludeAnsweredCorrectIds() : null);
            if (items.Count == 0)
            {
                MessageBox.Show("当前词库没有可用的单词，请先在「内容编辑」中添加内容。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }
            _currentGameTotalItems = items.Count;
            return items;
        }

        protected override void StartGame()
        {
            _currentGameTotalItems = 0;
            base.StartGame();

            _labelTotal.Text = $"总共: {_currentGameTotalItems}";
            _labelCorrect.Text = "正确: 0";
            _labelWrong.Text = "错误: 0";
            _labelRemaining.Text = $"剩余: {_currentGameTotalItems}";
        }

        protected override int CountRemainingTotal(LearningContext context) =>
            _gameService.CountRemaining(context, SkipKnown ? ExcludeAnsweredCorrectIds() : null);

        protected override void OnGameEnd(JsonElement gameRoot, LearningContext context)
        {
            if (!gameRoot.TryGetProperty("results", out var resultsProp)) return;

            var results = resultsProp.EnumerateArray()
                .Select(r => new WordMatchResult
                {
                    Id = r.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                    Correct = r.TryGetProperty("correct", out var c) && c.GetBoolean()
                })
                .ToList();

            _gameService.ApplyResults(CurrentUserId, context, results);
            _logger.LogInformation("单词拼写收到游戏结束消息，共 {Count} 个结果", results.Count);

            int right = gameRoot.TryGetProperty("right", out var r) ? r.GetInt32() : 0;
            int errors = gameRoot.TryGetProperty("errors", out var e) ? e.GetInt32() : 0;
            int remaining = _currentGameTotalItems - right - errors;

            _labelCorrect.Text = $"正确: {right}";
            _labelWrong.Text = $"错误: {errors}";
            _labelRemaining.Text = $"剩余: {Math.Max(0, remaining)}";
        }
    }
}