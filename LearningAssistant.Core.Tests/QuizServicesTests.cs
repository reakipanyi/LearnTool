using LearningAssistant.Models.Quiz;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Quiz;
using Xunit;

namespace LearningAssistant.Core.Tests;

public class QuizServicesTests
{
    [Fact]
    public void GetResult_ComputesCorrectCountAndWrongQuestions()
    {
        var engine = new QuizEngineService(new StubAiQuestionService());
        var session = new QuizSession { UserId = "user-1", Title = "测试" };
        session.Questions.Add(new QuizQuestion
        {
            Type = QuestionType.SingleChoice,
            CorrectOptionIndices = new List<int> { 1 }
        });
        session.Questions.Add(new QuizQuestion
        {
            Type = QuestionType.SingleChoice,
            CorrectOptionIndices = new List<int> { 0 }
        });

        engine.SubmitAnswer(session, 0, new List<int> { 1 });
        engine.SubmitAnswer(session, 1, new List<int> { 2 });

        var result = engine.GetResult(session);

        Assert.Equal(2, result.TotalQuestions);
        Assert.Equal(1, result.CorrectAnswers);
        Assert.Equal(50, result.AccuracyRate);
        Assert.Single(result.WrongQuestions);
    }

    [Fact]
    public async Task StartRecallAsync_WithoutSpeechSupport_ReturnsUnavailableFeedback()
    {
        var service = new VoiceRecallService(null, new StubAiQuestionService());

        var result = await service.StartRecallAsync(new ReviewItem { Content = "问题", Answer = "答案" });

        Assert.False(service.IsVoiceSupported);
        Assert.False(result.IsCorrect);
        Assert.Equal("答案", result.CorrectAnswer);
        Assert.Contains("语音识别", result.Feedback);
    }

    private sealed class StubAiQuestionService : IAiQuestionService
    {
        public Task<string> AskAsync(string text, string context = "", CancellationToken cancellationToken = default)
            => Task.FromResult(string.Empty);

        public Task<string> GenerateExerciseAsync(string text, string language, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Empty);

        public Task<string> SummarizeTextAsync(string text, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Empty);
    }
}
