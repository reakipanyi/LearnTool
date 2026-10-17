namespace LearningAssistant.Services.Media;

public sealed class AbLoopState
{
    public TimeSpan? A { get; set; }
    public TimeSpan? B { get; set; }

    public bool ShouldLoopBack(TimeSpan position, out TimeSpan target)
    {
        target = A ?? TimeSpan.Zero;
        if (A is null || B is null) return false;
        if (B <= A) return false;
        return position >= B;
    }
}