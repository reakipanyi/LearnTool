using LearningAssistant.Common.UI;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls.Gamification
{
    /// <summary>
    /// 目标日历视图 - 展示历史目标完成情况
    /// </summary>
    public class GoalCalendarView : UserControl
    {
        private ILearningGoalService? _goalService;
        private string? _currentUserId;
        private DateTime _currentMonth = DateTime.Today;
        private const int CellSize = 38;
        private const int HeaderHeight = 30;
        private const int WeekdayHeight = 24;

        private static readonly string[] WeekdayNames = { "日", "一", "二", "三", "四", "五", "六" };

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ILearningGoalService? GoalService
        {
            get => _goalService;
            set
            {
                _goalService = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? CurrentUserId
        {
            get => _currentUserId;
            set
            {
                _currentUserId = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime CurrentMonth
        {
            get => _currentMonth;
            set
            {
                _currentMonth = value;
                Invalidate();
            }
        }

        public GoalCalendarView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint, true);

            BackColor = Color.White;
            Size = new Size(280, 280);
        }

        public void PreviousMonth()
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            Invalidate();
        }

        public void NextMonth()
        {
            _currentMonth = _currentMonth.AddMonths(1);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            var rect = ClientRectangle;

            PaintHeader(g, rect);
            PaintWeekdays(g, rect);
            PaintCalendarCells(g, rect);
        }

        private void PaintHeader(Graphics g, Rectangle rect)
        {
            var headerRect = new Rectangle(rect.X, rect.Y, rect.Width, HeaderHeight);

            var monthText = $"{_currentMonth:yyyy年 M月}";
            using var titleFont = new Font("微软雅黑", 11F, FontStyle.Bold);
            var titleSize = g.MeasureString(monthText, titleFont);

            g.DrawString(monthText, titleFont,
                Brushes.Black,
                (rect.Width - titleSize.Width) / 2,
                (HeaderHeight - titleSize.Height) / 2);

            using var arrowFont = new Font("微软雅黑", 10F, FontStyle.Bold);
            g.DrawString("◀", arrowFont, Brushes.Gray, 10, (HeaderHeight - arrowFont.Height) / 2);
            g.DrawString("▶", arrowFont, Brushes.Gray, rect.Width - 25, (HeaderHeight - arrowFont.Height) / 2);
        }

        private void PaintWeekdays(Graphics g, Rectangle rect)
        {
            var weekdayY = HeaderHeight + 5;
            using var weekdayFont = new Font("微软雅黑", 9F, FontStyle.Bold);

            for (int i = 0; i < 7; i++)
            {
                var x = rect.X + 5 + i * (CellSize + 4);
                var dayRect = new RectangleF(x, weekdayY, CellSize, WeekdayHeight);

                var color = (i == 0 || i == 6)
                    ? Color.FromArgb(239, 83, 80)
                    : Color.FromArgb(100, 100, 100);

                TextRenderer.DrawText(g, WeekdayNames[i], weekdayFont,
                    Rectangle.Round(dayRect),
                    color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void PaintCalendarCells(Graphics g, Rectangle rect)
        {
            var records = GetDailyRecords();
            var recordDict = records.ToDictionary(r => r.Date.Date, r => r);

            var firstDay = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            var daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
            var startWeekday = (int)firstDay.DayOfWeek;
            var startY = HeaderHeight + WeekdayHeight + 10;

            using var dayFont = new Font("微软雅黑", 9F);
            using var todayFont = new Font("微软雅黑", 9F, FontStyle.Bold);

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(_currentMonth.Year, _currentMonth.Month, day);
                var weekIndex = (startWeekday + day - 1) / 7;
                var dayOfWeek = (int)date.DayOfWeek;

                var x = rect.X + 5 + dayOfWeek * (CellSize + 4);
                var y = startY + weekIndex * (CellSize + 4);

                var cellRect = new Rectangle(x, y, CellSize, CellSize);
                var isToday = date.Date == DateTime.Today;
                var isCurrentMonth = date.Month == _currentMonth.Month;

                var hasRecord = recordDict.TryGetValue(date.Date, out var record);
                var isCompleted = hasRecord && record!.AllCompleted;
                var completedCount = hasRecord ? record!.CompletedCount : 0;
                var totalCount = hasRecord ? record!.TotalCount : 0;

                if (isCompleted && hasRecord && totalCount > 0)
                {
                    using var fillPath = GdiHelper.CreateRoundedRectPath(cellRect, 6);
                    using var fillBrush = new LinearGradientBrush(
                        cellRect,
                        Color.FromArgb(76, 175, 80),
                        Color.FromArgb(56, 142, 60),
                        LinearGradientMode.ForwardDiagonal);
                    g.FillPath(fillBrush, fillPath);
                }
                else if (hasRecord && completedCount > 0 && totalCount > 0)
                {
                    using var fillPath = GdiHelper.CreateRoundedRectPath(cellRect, 6);
                    using var fillBrush = new SolidBrush(Color.FromArgb(255, 243, 224));
                    g.FillPath(fillBrush, fillPath);
                }
                else if (!isCurrentMonth)
                {
                    using var fillPath = GdiHelper.CreateRoundedRectPath(cellRect, 6);
                    using var fillBrush = new SolidBrush(Color.FromArgb(245, 245, 245));
                    g.FillPath(fillBrush, fillPath);
                }

                var textColor = isCompleted
                    ? Color.White
                    : isToday
                        ? Color.FromArgb(255, 152, 0)
                        : !isCurrentMonth
                            ? Color.FromArgb(180, 180, 180)
                            : Color.FromArgb(60, 60, 60);

                var font = isToday ? todayFont : dayFont;

                TextRenderer.DrawText(g, day.ToString(), font,
                    cellRect,
                    textColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                if (isToday)
                {
                    using var borderPen = new Pen(Color.FromArgb(255, 152, 0), 2);
                    using var borderPath = GdiHelper.CreateRoundedRectPath(
                        new Rectangle(cellRect.X + 1, cellRect.Y + 1, cellRect.Width - 2, cellRect.Height - 2), 5);
                    g.DrawPath(borderPen, borderPath);
                }
            }
        }

        private List<DailyGoalRecord> GetDailyRecords()
        {
            if (_goalService == null || string.IsNullOrEmpty(_currentUserId))
                return new List<DailyGoalRecord>();

            try
            {
                return _goalService.GetDailyRecords(_currentUserId, 90);
            }
            catch
            {
                return new List<DailyGoalRecord>();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (e.Y < HeaderHeight)
            {
                if (e.X < 30)
                {
                    PreviousMonth();
                }
                else if (e.X > Width - 30)
                {
                    NextMonth();
                }
            }
        }
    }
}
