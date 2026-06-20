using LearningAssistant.Models;
using LearningAssistant.Models.UI;

namespace LearningAssistant.Managers
{
    public class ConfettiManager : IDisposable
    {
        private readonly List<ConfettiParticle> _particles = new();
        private readonly Color[] _colors = new[] {
            Color.FromArgb(255, 59, 48), Color.FromArgb(255, 149, 0), Color.FromArgb(255, 204, 0),
            Color.FromArgb(52, 199, 89), Color.FromArgb(0, 122, 255), Color.FromArgb(88, 86, 214),
            Color.FromArgb(175, 82, 222), Color.FromArgb(255, 69, 58), Color.FromArgb(245, 90, 140),
            Color.FromArgb(100, 200, 220)
        };
        private readonly Random _random = new Random();
        private readonly Dictionary<int, SolidBrush> _brushCache = new();
        private Control? _targetControl;
        private bool _disposed = false;

        /// <summary>
        /// 设置目标控件（用于绘制彩纸）
        /// </summary>
        public void SetTargetControl(Control control)
        {
            _targetControl = control;
        }

        /// <summary>
        /// 启动彩纸动画
        /// </summary>
        /// <param name="centerX">中心X坐标</param>
        /// <param name="centerY">中心Y坐标</param>
        /// <param name="count">粒子数量</param>
        public void Start(float centerX, float centerY, int count = 50)
        {
            _particles.Clear();

            for (int i = 0; i < count; i++)
            {
                float angle = (float)(_random.NextDouble() * Math.PI * 2);
                float speed = (float)(_random.NextDouble() * 8 + 4);

                _particles.Add(new ConfettiParticle
                {
                    X = centerX,
                    Y = centerY,
                    VelocityX = (float)Math.Cos(angle) * speed,
                    VelocityY = (float)Math.Sin(angle) * speed - 5,
                    Rotation = (float)(_random.NextDouble() * Math.PI * 2),
                    RotationSpeed = (float)((_random.NextDouble() - 0.5) * 0.3),
                    Color = _colors[_random.Next(_colors.Length)],
                    Shape = (ParticleShape)_random.Next(4),
                    Size = _random.Next(6, 12)
                });
            }
        }

        /// <summary>
        /// 更新粒子状态
        /// </summary>
        public void Update()
        {
            float gravity = 0.2f;
            float wind = (float)(Math.Sin(DateTime.Now.Ticks / 1000000.0) * 0.5);

            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var particle = _particles[i];
                particle.Update(gravity, wind);

                // 移除超出范围或透明度过低的粒子
                if (particle.Y > (_targetControl?.Height ?? 800) + 50 || particle.Opacity <= 0)
                {
                    _particles.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 绘制粒子
        /// </summary>
        public void Draw(Graphics g)
        {
            foreach (var particle in _particles)
            {
                var brush = GetBrush(particle.Color);
                var alpha = (int)(particle.Opacity * 255);
                brush.Color = Color.FromArgb(alpha, particle.Color);

                var state = g.Save();
                g.TranslateTransform(particle.X, particle.Y);
                g.RotateTransform(particle.Rotation * 180 / (float)Math.PI);

                switch (particle.Shape)
                {
                    case ParticleShape.Rectangle:
                        g.FillRectangle(brush, -particle.Size / 2, -particle.Size / 4, particle.Size, particle.Size / 2);
                        break;
                    case ParticleShape.Circle:
                        g.FillEllipse(brush, -particle.Size / 2, -particle.Size / 2, particle.Size, particle.Size);
                        break;
                    case ParticleShape.Triangle:
                        var points = new PointF[] {
                            new PointF(0, -particle.Size / 2),
                            new PointF(particle.Size / 2, particle.Size / 2),
                            new PointF(-particle.Size / 2, particle.Size / 2)
                        };
                        g.FillPolygon(brush, points);
                        break;
                    case ParticleShape.Star:
                        DrawStar(g, brush, particle.Size);
                        break;
                }

                g.Restore(state);
            }
        }

        /// <summary>
        /// 绘制五角星
        /// </summary>
        private void DrawStar(Graphics g, SolidBrush brush, float size)
        {
            var points = new PointF[10];
            for (int i = 0; i < 10; i++)
            {
                float radius = i % 2 == 0 ? size / 2 : size / 4;
                float angle = (float)(i * Math.PI / 5 - Math.PI / 2);
                points[i] = new PointF(
                    (float)Math.Cos(angle) * radius,
                    (float)Math.Sin(angle) * radius
                );
            }
            g.FillPolygon(brush, points);
        }

        /// <summary>
        /// 获取缓存的画刷
        /// </summary>
        private SolidBrush GetBrush(Color color)
        {
            int key = color.ToArgb();
            if (!_brushCache.TryGetValue(key, out var brush))
            {
                brush = new SolidBrush(color);
                _brushCache[key] = brush;
            }
            return brush;
        }

        /// <summary>
        /// 检查是否还有活动的粒子
        /// </summary>
        public bool HasActiveParticles => _particles.Count > 0;

        /// <summary>
        /// 获取粒子数量
        /// </summary>
        public int ParticleCount => _particles.Count;

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            foreach (var brush in _brushCache.Values)
            {
                brush.Dispose();
            }
            _brushCache.Clear();
            _particles.Clear();
            _targetControl = null;
            _disposed = true;
        }
    }
}
