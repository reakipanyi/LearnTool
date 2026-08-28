namespace LearningAssistant.Abstractions
{
    public readonly record struct RectInt(int X, int Y, int Width, int Height)
    {
        public static RectInt Empty => new(0, 0, 0, 0);
        public bool IsEmpty => Width <= 0 || Height <= 0;
    }
}
