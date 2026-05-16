
namespace UnifiedLearningAssistant.Views.UI
{
    public class ConfettiControl : Control
    {
        private readonly List&lt;ConfettiParticle&gt; _particles = new List&lt;ConfettiParticle&gt;();
        private readonly Timer _animationTimer = new Timer();
        private readonly Random _random = new Random();
        private bool _isAnimating;

        public ConfettiControl()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            
            _animationTimer.Interval = 16;
            _animationTimer.Tick += OnAnimationTick;
        }

        public void StartCelebration()
        {
            if (_isAnimating) return;

            _isAnimating = true;
            _particles.Clear();

            var colors = new[]
            {
                Color.FromArgb(255, 59, 48),
                Color.FromArgb(255, 149, 0),
                Color.FromArgb(255, 204, 0),
                Color.FromArgb(52, 199, 89),
                Color.FromArgb(0, 122, 255),
                Color.FromArgb(88, 86, 214),
                Color.FromArgb(175, 82, 222)
            };

            for (int i = 0; i &lt; 100; i++)
            {
                _particles.Add(new ConfettiParticle
                {
                    X = _random.Next(Width),
                    Y = -_random.Next(200),
                    Size = _random.Next(8, 20),
                    Color = colors[_random.Next(colors.Length)],
                    VelocityX = (float)(_random.NextDouble() * 4 - 2),
                    VelocityY = (float)(_random.NextDouble() * 3 + 2),
                    Rotation = _random.Next(360),
                    RotationSpeed = (float)(_random.NextDouble() * 10 - 5),
                    Shape = _random.Next(2) == 0 ? ParticleShape.Rectangle : ParticleShape.Circle
                });
            }

            _animationTimer.Start();
        }

        public void StopCelebration()
        {
            _isAnimating = false;
            _animationTimer.Stop();
            _particles.Clear();
            Invalidate();
        }

        private void OnAnimationTick(object? sender, EventArgs e)
        {
            if (!_isAnimating) return;

            bool hasActiveParticles = false;

            foreach (var particle in _particles)
            {
                particle.X += particle.VelocityX;
                particle.Y += particle.VelocityY;
                particle.VelocityY += 0.1f;
                particle.Rotation += particle.RotationSpeed;

                if (particle.Y &lt; Height + 50)
                {
                    hasActiveParticles = true;
                }
            }

            if (!hasActiveParticles)
            {
                StopCelebration();
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            foreach (var particle in _particles)
            {
                if (particle.Y &gt; Height + 50) continue;

                using var brush = new SolidBrush(particle.Color);
                
                g.TranslateTransform(particle.X, particle.Y);
                g.RotateTransform(particle.Rotation);

                if (particle.Shape == ParticleShape.Rectangle)
                {
                    g.FillRectangle(brush, -particle.Size / 2, -particle.Size / 2, particle.Size, particle.Size * 0.6f);
                }
                else
                {
                    g.FillEllipse(brush, -particle.Size / 2, -particle.Size / 2, particle.Size, particle.Size);
                }

                g.ResetTransform();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        private class ConfettiParticle
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
        }

        private enum ParticleShape
        {
            Rectangle,
            Circle
        }
    }
}

