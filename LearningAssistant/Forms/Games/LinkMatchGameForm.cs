using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Forms.Games
{
    /// <summary>
    /// 连连看游戏窗体：卡片正面常显，配对需满足"两点之间可达路径 ≤2 次转弯"才能消除。
    /// 继承 <see cref="WebView2GameFormBase"/>，复用 WebView2 初始化、数据注入与成绩回写。
    /// </summary>
    public partial class LinkMatchGameForm : WebView2GameFormBase
    {
        private readonly WordMatchGameService _gameService;

        protected override string FormTitle => "🔗 连连看";

        protected override string HtmlFileRelativePath => Path.Combine("Resources", "LinkMatchGame", "index.html");

        public LinkMatchGameForm(
            WordMatchGameService gameService,
            IContentLoaderService contentLoaderService,
            IUserSessionService userSessionService,
            IThemeService themeService,
            IUserSettingsService settingsService,
            ILogger<LinkMatchGameForm> logger)
            : base(contentLoaderService, userSessionService, themeService, settingsService, logger)
        {
            _gameService = gameService;
        }

        /// <summary>从词库构建游戏数据（错题优先，每组按 行×列 配对）；词库不足时提示并返回 null。</summary>
        protected override object? BuildData(LearningContext context, string themeName)
        {
            var items = _gameService.BuildItems(context, maxCount: MaxCountForGrid(), selection: WordSelection.WrongFirst,
                excludeIds: SkipKnown ? ExcludeAnsweredCorrectIds() : null);
            if (items.Count < 2)
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

        /// <summary>解析前端上报的配对结果并回写学习状态与错题本。</summary>
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
            _logger.LogInformation("连连看收到游戏结束消息，共 {Count} 个结果", results.Count);
        }
    }
}