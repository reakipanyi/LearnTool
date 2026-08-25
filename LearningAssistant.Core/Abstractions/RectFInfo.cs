namespace LearningAssistant.Abstractions
{
    public readonly record struct RectFInfo(float X, float Y, float Width, float Height)
    {
        public static RectFInfo Empty => new(0, 0, 0, 0);
    }
}
