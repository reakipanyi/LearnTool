using System.Drawing;
using LearningAssistant.Abstractions;

namespace LearningAssistant.Common
{
    public static class ColorInfoExtensions
    {
        public static Color ToColor(this ColorInfo c) => Color.FromArgb(c.A, c.R, c.G, c.B);

        public static ColorInfo ToColorInfo(this Color c) => new(c.R, c.G, c.B, c.A);
    }
}
