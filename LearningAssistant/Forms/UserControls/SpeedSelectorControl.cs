using LearningAssistant.Models.Config;
using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls
{
    public class SpeedSelectorControl : UserControl
    {
        private TtsConfig? _ttsConfig;
        private readonly Label _labelIcon;
        private readonly Button _buttonMinus;
        private readonly Label _labelSpeed;
        private readonly Button _buttonPlus;

        public event EventHandler? SpeedChanged;

        public SpeedSelectorControl()
        {
            _labelIcon = new Label();
            _buttonMinus = new Button();
            _labelSpeed = new Label();
            _buttonPlus = new Button();

            InitializeComponent();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TtsConfig? TtsConfig
        {
            get => _ttsConfig;
            set
            {
                _ttsConfig = value;
                UpdateLabelText();
            }
        }

        private void InitializeComponent()
        {
            this.Size = new Size(130, 28);
            this.BackColor = Color.Transparent;

            _labelIcon.Font = new Font("Segoe UI Symbol", 12F);
            _labelIcon.ForeColor = Color.FromArgb(70, 90, 110);
            _labelIcon.Location = new Point(0, 0);
            _labelIcon.Size = new Size(24, 28);
            _labelIcon.TextAlign = ContentAlignment.MiddleCenter;
            _labelIcon.Text = "⏱";

            _buttonMinus.FlatStyle = FlatStyle.Flat;
            _buttonMinus.FlatAppearance.BorderSize = 0;
            _buttonMinus.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 230, 230);
            _buttonMinus.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 200, 200);
            _buttonMinus.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _buttonMinus.ForeColor = Color.FromArgb(70, 90, 110);
            _buttonMinus.Location = new Point(28, 0);
            _buttonMinus.Size = new Size(26, 28);
            _buttonMinus.Text = "-";
            _buttonMinus.UseVisualStyleBackColor = true;
            _buttonMinus.Cursor = Cursors.Hand;
            _buttonMinus.Click += ButtonMinus_Click;

            _labelSpeed.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _labelSpeed.ForeColor = Color.FromArgb(50, 70, 90);
            _labelSpeed.Location = new Point(58, 0);
            _labelSpeed.Size = new Size(40, 28);
            _labelSpeed.TextAlign = ContentAlignment.MiddleCenter;
            UpdateLabelText();

            _buttonPlus.FlatStyle = FlatStyle.Flat;
            _buttonPlus.FlatAppearance.BorderSize = 0;
            _buttonPlus.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 230, 230);
            _buttonPlus.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 200, 200);
            _buttonPlus.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _buttonPlus.ForeColor = Color.FromArgb(70, 90, 110);
            _buttonPlus.Location = new Point(102, 0);
            _buttonPlus.Size = new Size(26, 28);
            _buttonPlus.Text = "+";
            _buttonPlus.UseVisualStyleBackColor = true;
            _buttonPlus.Cursor = Cursors.Hand;
            _buttonPlus.Click += ButtonPlus_Click;

            this.Controls.Add(_labelIcon);
            this.Controls.Add(_buttonMinus);
            this.Controls.Add(_labelSpeed);
            this.Controls.Add(_buttonPlus);
        }

        private void ButtonMinus_Click(object? sender, EventArgs e)
        {
            if (_ttsConfig == null) return;
            float currentSpeed = _ttsConfig.Speed;
            float newSpeed = Math.Max(0.5f, currentSpeed - 0.1f);
            _ttsConfig.Speed = (float)Math.Round(newSpeed * 10) / 10;
            UpdateLabelText();
            SpeedChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonPlus_Click(object? sender, EventArgs e)
        {
            if (_ttsConfig == null) return;
            float currentSpeed = _ttsConfig.Speed;
            float newSpeed = Math.Min(2.0f, currentSpeed + 0.1f);
            _ttsConfig.Speed = (float)Math.Round(newSpeed * 10) / 10;
            UpdateLabelText();
            SpeedChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateLabelText()
        {
            if (_ttsConfig == null)
            {
                _labelSpeed.Text = "100%";
            }
            else
            {
                _labelSpeed.Text = $"{(int)(_ttsConfig.Speed * 100)}%";
            }
        }

        public void RefreshDisplay()
        {
            UpdateLabelText();
        }
    }
}