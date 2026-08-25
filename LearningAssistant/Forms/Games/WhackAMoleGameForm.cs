using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Forms.Games
{
    /// <summary>
    /// 打地鼠游戏窗体：限时快节奏，随机冒出目标，点中正确配对得分。
    /// 继承 <see cref="WebView2GameFormBase"/>，复用 WebView2 初始化、数据注入与成绩回写。
    /// </summary>
    public partial class WhackAMoleGameForm : WebView2GameFormBase
    {
        private readonly WordMatchGameService _gameService;

        protected override string FormTitle => "🔨 打地鼠";

        protected override string HtmlFileRelativePath => Path.Combine("Resources", "WhackAMoleGame", "index.html");

        public WhackAMoleGameForm(
            WordMatchGameService gameService,
            IContentLoaderService contentLoaderService,
            IUserSessionService userSessionService,
            IThemeService themeService,
            IUserSettingsService settingsService,
            ILogger<WhackAMoleGameForm> logger)
            : base(contentLoaderService, userSessionService, themeService, settingsService, logger)
        {
            _gameService = gameService;
        }

        /// <summary>从词库构建游戏数据（干扰项多，取更多词条备用，随机为主）；词库不足时提示并返回 null。</summary>
        protected override object? BuildData(LearningContext context, string themeName)
        {
            var items = _gameService.BuildItems(context, maxCount: Math.Max(12, MaxCountForGrid()), selection: WordSelection.Random,
                excludeIds: SkipKnown ? ExcludeAnsweredCorrectIds() : null);
            if (items.Count < 3)
            {
                MessageBox.Show("当前词库可用单词不足，请先在「内容编辑」中添加单词。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }
            return items;
        }

        /// <summary>统计当前词库仍可学习的条目总数（供前端"总剩余"展示）。</summary>
        protected override int CountRemainingTotal(LearningContext context) =>
            _gameService.CountRemaining(context, SkipKnown ? ExcludeAnsweredCorrectIds() : null);

        /// <summary>解析前端上报的命中结果并回写学习状态与错题本。</summary>
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
            _logger.LogInformation("打地鼠收到游戏结束消息，共 {Count} 个结果", results.Count);
        }
    }
}