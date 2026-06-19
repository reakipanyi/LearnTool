namespace LearningAssistant.Models.UI
{
    /// <summary>
    /// 彩纸粒子形状
    /// </summary>
    public enum ParticleShape
    {
        Rectangle,
        Circle,
        Triangle,
        Star
    }

    /// <summary>
    /// 彩纸粒子（用于庆祝动画）
    /// </summary>
    public class ConfettiParticle
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Size { get; set; }
        public Color Color { get; set; }
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }
        public float Rotation { get; set; }
        public float RotationSpeed { get; set; }
        public ParticleShape Shape { get; set; }
        public float Opacity { get; set; } = 1.0f;
        public float FadeSpeed { get; set; }
        public double WobbleOffset { get; set; }
        public float WobbleSpeed { get; set; }

        public void Update(float gravity, float wind)
        {
            X += VelocityX + wind;
            Y += VelocityY;
            VelocityY += gravity;
            Rotation += RotationSpeed;
            Opacity = Math.Max(0, Opacity - 0.005f);
        }
    }
}
