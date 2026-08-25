namespace LearningAssistant.Abstractions
{
    public readonly record struct ColorInfo(int R, int G, int B, int A = 255)
    {
        public static ColorInfo FromArgb(int r, int g, int b) => new(r, g, b);
        public static ColorInfo FromArgb(int a, int r, int g, int b) => new(r, g, b, a);
    }
}
