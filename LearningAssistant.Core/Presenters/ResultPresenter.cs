using Microsoft.Extensions.Logging;
using LearningAssistant.Services.Learning;
using LearningAssistant.Views;

namespace LearningAssistant.Presenters
{
    public class ResultPresenter : IDisposable
    {
        private readonly ILogger<ResultPresenter> _logger;
        private readonly IResultView _view;
        private readonly IStudyEngine _studyEngine;

        public ResultPresenter(ILogger<ResultPresenter> logger, IResultView view, IStudyEngine studyEngine)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _studyEngine = studyEngine ?? throw new ArgumentNullException(nameof(studyEngine));

            _view.ReviewUnknownClicked += View_ReviewUnknownClicked;
            _view.CloseClicked += View_CloseClicked;
            _logger.LogInformation("ResultPresenter initialized");
        }

        public void Initialize()
        {
            _logger.LogInformation("Initializing ResultPresenter");
            UpdateView();
        }

        private void UpdateView()
        {
            var stats = _studyEngine.GetStatistics();
            _view.AccuracyRate = $"正确率: {stats.AccuracyRate:F1}%";
            _view.KnownItems = $"已掌握: {_studyEngine.KnownItems.Count}";
            _view.UnknownItems = $"未掌握: {_studyEngine.UnknownItems.Count}";
            _view.Statistics = $"总题数: {stats.TotalTestCount} | 正确: {stats.CorrectCount}";
        }

        private void View_ReviewUnknownClicked(object? sender, EventArgs e)
        {
            _logger.LogInformation("Review unknown items clicked");
            OnReviewUnknown?.Invoke(this, EventArgs.Empty);
        }

        private void View_CloseClicked(object? sender, EventArgs e)
        {
            _view.CloseView();
        }

        public void Dispose()
        {
            _view.ReviewUnknownClicked -= View_ReviewUnknownClicked;
            _view.CloseClicked -= View_CloseClicked;
            _logger.LogInformation("ResultPresenter disposed");
        }

        public event EventHandler? OnReviewUnknown;
    }
}