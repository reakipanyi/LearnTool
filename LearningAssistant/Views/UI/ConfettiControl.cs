
namespace LearningAssistant.Views.UI
{
    public class ConfettiControl : Control
    {
        private readonly List<ConfettiParticle> _particles = new List<ConfettiParticle>();
        private readonly System.Windows.Forms.Timer _animationTimer = new System.Windows.Forms.Timer();
        private readonly Random _random = new Random();
        private bool _isAnimating;
        private bool _enableBurstEffect = true;

        private readonly Color[] _celebrationColors = new[]
        {
            Color.FromArgb(255, 59, 48),
            Color.FromArgb(255, 149, 0),
            Color.FromArgb(255, 204, 0),
            Color.FromArgb(52, 199, 89),
            Color.FromArgb(0, 122, 255),
            Color.FromArgb(88, 86, 214),
            Color.FromArgb(175, 82, 222),
            Color.FromArgb(255, 69, 58),
            Color.FromArgb(245, 90, 140),
            Color.FromArgb(100, 200, 220)
        };

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
            GenerateParticles(100);

            if (_enableBurstEffect)
            {
                GenerateBurstParticles(50);
            }

            _animationTimer.Start();
        }

        private void GenerateParticles(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _particles.Add(CreateParticle());
            }
        }

        private void GenerateBurstParticles(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var particle = CreateParticle();
                particle.X = _random.Next(Width);
                particle.Y = -_random.Next(100);
                particle.VelocityY = (float)(_random.NextDouble() * 4 + 3);
                particle.Size = _random.Next(6, 15);
                particle.RotationSpeed = (float)(_random.NextDouble() * 15 - 7.5);
                _particles.Add(particle);
            }
        }

        private ConfettiParticle CreateParticle()
        {
            var shapes = new[] { ParticleShape.Rectangle, ParticleShape.Circle, ParticleShape.Triangle, ParticleShape.Star };
            return new ConfettiParticle
            {
                X = _random.Next(Width),
                Y = -_random.Next(200),
                Size = _random.Next(8, 20),
                Color = _celebrationColors[_random.Next(_celebrationColors.Length)],
                VelocityX = (float)(_random.NextDouble() * 6 - 3),
                VelocityY = (float)(_random.NextDouble() * 3 + 2),
                Rotation = _random.Next(360),
                RotationSpeed = (float)(_random.NextDouble() * 12 - 6),
                Shape = shapes[_random.Next(shapes.Length)],
                Opacity = 1.0f,
                FadeSpeed = (float)(_random.NextDouble() * 0.01 + 0.005),
                WobbleOffset = (float)(_random.NextDouble() * Math.PI * 2),
                WobbleSpeed = (float)(_random.NextDouble() * 0.1 + 0.05)
            };
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
            float time = (float)(DateTime.Now.Ticks / 10000000.0);

            foreach (var particle in _particles)
            {
                particle.WobbleOffset += particle.WobbleSpeed;
                particle.X += particle.VelocityX + (float)Math.Sin(particle.WobbleOffset) * 0.5f;
                particle.Y += particle.VelocityY;
                particle.VelocityY += 0.08f;
                particle.Rotation += particle.RotationSpeed;

                if (particle.Y > Height * 0.7)
                {
                    particle.Opacity -= particle.FadeSpeed;
                }

                if (particle.Y < Height + 50 && particle.Opacity > 0)
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
                if (particle.Y > Height + 50 || particle.Opacity <= 0) continue;

                using var brush = new SolidBrush(Color.FromArgb((int)(particle.Opacity * 255), particle.Color));

                g.TranslateTransform(particle.X, particle.Y);
                g.RotateTransform(particle.Rotation);

                switch (particle.Shape)
                {
                    case ParticleShape.Rectangle:
                        g.FillRectangle(brush, -particle.Size / 2, -particle.Size / 2, particle.Size, particle.Size * 0.6f);
                        break;
                    case ParticleShape.Circle:
                        g.FillEllipse(brush, -particle.Size / 2, -particle.Size / 2, particle.Size, particle.Size);
                        break;
                    case ParticleShape.Triangle:
                        DrawTriangle(g, brush, particle.Size);
                        break;
                    case ParticleShape.Star:
                        DrawStar(g, brush, particle.Size);
                        break;
                }

                g.ResetTransform();
            }
        }

        private void DrawTriangle(Graphics g, Brush brush, float size)
        {
            var points = new PointF[]
            {
                new PointF(0, -size / 2),
                new PointF(-size / 2, size / 2),
                new PointF(size / 2, size / 2)
            };
            g.FillPolygon(brush, points);
        }

        private void DrawStar(Graphics g, Brush brush, float size)
        {
            var points = new PointF[10];
            for (int i = 0; i < 10; i++)
            {
                float radius = (i % 2 == 0) ? size / 2 : size / 4;
                float angle = (float)(i * Math.PI / 5 - Math.PI / 2);
                points[i] = new PointF(
                    (float)Math.Cos(angle) * radius,
                    (float)Math.Sin(angle) * radius
                );
            }
            g.FillPolygon(brush, points);
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
            public float Opacity { get; set; } = 1.0f;
            public float FadeSpeed { get; set; }
            public double WobbleOffset { get; set; }
            public float WobbleSpeed { get; set; }
        }

        private enum ParticleShape
        {
            Rectangle,
            Circle,
            Triangle,
            Star
        }
    }
}
