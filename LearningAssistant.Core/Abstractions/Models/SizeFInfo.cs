namespace LearningAssistant.Abstractions
{
    public readonly record struct SizeFInfo(float Width, float Height)
    {
        public static SizeFInfo Empty => new(0, 0);
    }
}
