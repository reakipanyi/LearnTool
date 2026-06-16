using System.Threading;
using System.Threading.Tasks;

namespace LearningAssistant.Services.Feedback
{
    public interface IEncouragementService
    {
        Task PlayRandomKnownFeedbackAsync(CancellationToken cancellationToken = default);

        Task PlayRandomUnknownFeedbackAsync(CancellationToken cancellationToken = default);
    }
}
