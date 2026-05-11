using System.Drawing.Drawing2D;

namespace UnifiedLearningAssistant.Common
{
    public static class WinFormsExtensions
    {
        public static void SetRoundedCorner(this Control control, int radius = 10)
        {
            var path = new GraphicsPath();
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(control.Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(control.Width - radius, control.Height - radius, radius, radius, 0, 90);
            path.AddArc(0, control.Height - radius, radius, radius, 90, 90);
            path.CloseAllFigures();
            control.Region = new Region(path);
        }

        public static void SetDoubleBuffered(this Control control)
        {
            typeof(Control).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                control,
                new object[] { true });
        }

        public static void SetFlatStyle(this Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            button.FlatAppearance.MouseDownBackColor = Color.LightGray;
        }

        public static void SetGradientBackground(this Control control, Color startColor, Color endColor)
        {
            control.Paint += (sender, e) =>
            {
                var rect = new Rectangle(0, 0, control.Width, control.Height);
                using var brush = new LinearGradientBrush(rect, startColor, endColor, LinearGradientMode.Vertical);
                e.Graphics.FillRectangle(brush, rect);
            };
        }

        public static void ResizeFontToFit(this Label label, int minSize = 8, int maxSize = 72)
        {
            if (label.Text == null || label.Width == 0 || label.Height == 0)
                return;

            int size = maxSize;
            while (size >= minSize)
            {
                using var font = new Font(label.Font.FontFamily, size, label.Font.Style);
                var sizeF = label.CreateGraphics().MeasureString(label.Text, font);
                if (sizeF.Width <= label.Width && sizeF.Height <= label.Height)
                {
                    label.Font = font;
                    return;
                }
                size--;
            }
            label.Font = new Font(label.Font.FontFamily, minSize, label.Font.Style);
        }

        public static void ScaleFont(this Control control, float scaleFactor)
        {
            control.Font = new Font(control.Font.FontFamily, control.Font.Size * scaleFactor, control.Font.Style);

            foreach (Control child in control.Controls)
            {
                child.ScaleFont(scaleFactor);
            }
        }

        public static void CenterControl(this Control control, Control parent)
        {
            control.Location = new Point(
                (parent.Width - control.Width) / 2,
                (parent.Height - control.Height) / 2);
        }

        public static void EnableHighDpi(this Form form)
        {
            form.AutoScaleMode = AutoScaleMode.Dpi;
            //form.DoubleBuffered = true;
        }

        public static void SetChildFonts(this Control parent, Font font)
        {
            foreach (Control child in parent.Controls)
            {
                child.Font = font;
                child.SetChildFonts(font);
            }
        }

        public static void SmoothScroll(this Panel panel)
        {
            panel.AutoScroll = true;
            typeof(Panel).InvokeMember(
                "SetScrollState",
                System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                panel,
                new object[] { 0x0001, true });
        }
    }
}
