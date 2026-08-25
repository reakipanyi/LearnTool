using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls.Gamification
{
    public class LevelBadge : UserControl
    {
        private int _level = 1;
        private int _currentXP = 0;
        private int _xpToNextLevel = 100;
        private string _levelTitle = "初学者";
        private Color _progressColor = Color.FromArgb(255, 152, 0);
        private Color _trackColor = Color.FromArgb(240, 240, 240);
        private Color _textColor = Color.FromArgb(51, 51, 51);

        private readonly System.Windows.Forms.Timer _animationTimer = new();
        private float _displayProgress = 0f;
        private float _targetProgress = 0f;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Level
        {
            get => _level;
            set
            {
                _level = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CurrentXP
        {
            get => _currentXP;
            set
            {
                _currentXP = value;
                UpdateProgress();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int XPToNextLevel
        {
            get => _xpToNextLevel;
            set
            {
                _xpToNextLevel = Math.Max(1, value);
                UpdateProgress();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string LevelTitle
        {
            get => _levelTitle;
            set
            {
                _levelTitle = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ProgressColor
        {
            get => _progressColor;
            set
            {
                _progressColor = value;
                Invalidate();
            }
        }

        public event EventHandler? LevelUp;

        public LevelBadge()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Size = new Size(100, 120);

            _animationTimer.Interval = 30;
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            float diff = _targetProgress - _displayProgress;
            if (Math.Abs(diff) < 0.01f)
            {
                _displayProgress = _targetProgress;
                _animationTimer.Stop();
            }
            else
            {
                _displayProgress += diff * 0.15f;
            }
            Invalidate();
        }

        private void UpdateProgress()
        {
            _targetProgress = (float)_currentXP / _xpToNextLevel;
            _targetProgress = Math.Clamp(_targetProgress, 0f, 1f);
            _animationTimer.Start();
        }

        public void SetXP(int currentXP, int xpToNextLevel)
        {
            _currentXP = currentXP;
            _xpToNextLevel = Math.Max(1, xpToNextLevel);
            UpdateProgress();
        }

        public void TriggerLevelUp(int newLevel, string newTitle)
        {
            _level = newLevel;
            _levelTitle = newTitle;
            LevelUp?.Invoke(this, EventArgs.Empty);

            int oldXP = _xpToNextLevel;
            _currentXP = 0;
            _xpToNextLevel = CalculateXPForLevel(newLevel);
            _targetProgress = 0f;
            _displayProgress = 1f;
            _animationTimer.Start();

            Invalidate();
        }

        private int CalculateXPForLevel(int level)
        {
            return 100 + (level - 1) * 50;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int centerX = Width / 2;
            int circleDiameter = Math.Min(Width, Height - 25);
            int circleRadius = circleDiameter / 2;
            int circleY = 10;

            float progress = _displayProgress;

            using (Pen trackPen = new(_trackColor, 6))
            {
                g.DrawEllipse(trackPen, centerX - circleRadius + 3, circleY + 3, circleDiameter - 6, circleDiameter - 6);
            }

            using (Pen progressPen = new(_progressColor, 6))
            {
                progressPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                progressPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                float startAngle = -90f;
                float sweepAngle = 360f * progress;

                g.DrawArc(progressPen,
                    centerX - circleRadius + 3, circleY + 3,
                    circleDiameter - 6, circleDiameter - 6,
                    startAngle, sweepAngle);
            }

            string levelText = $"Lv.{_level}";
            using Font levelFont = new("微软雅黑", 14F, FontStyle.Bold);
            SizeF levelSize = g.MeasureString(levelText, levelFont);
            using Brush textBrush = new SolidBrush(_textColor);
            g.DrawString(levelText, levelFont, textBrush,
                centerX - levelSize.Width / 2,
                circleY + circleRadius - levelSize.Height / 2 - 5);

            string xpText = $"{_currentXP}/{_xpToNextLevel}";
            using Font xpFont = new("微软雅黑", 7.5F);
            SizeF xpSize = g.MeasureString(xpText, xpFont);
            using Brush xpBrush = new SolidBrush(Color.FromArgb(102, 102, 102));
            g.DrawString(xpText, xpFont, xpBrush,
                centerX - xpSize.Width / 2,
                circleY + circleRadius + 3);

            using Font titleFont = new("微软雅黑", 9F, FontStyle.Bold);
            SizeF titleSize = g.MeasureString(_levelTitle, titleFont);
            using Brush titleBrush = new SolidBrush(_progressColor);
            g.DrawString(_levelTitle, titleFont, titleBrush,
                centerX - titleSize.Width / 2,
                circleY + circleDiameter + 5);
        }
    }
}
