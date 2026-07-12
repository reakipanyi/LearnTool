using LearningAssistant.Services.Learning;
using LearningAssistant.Services.TTS;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls
{
    public class ContentFieldRow : Panel
    {
        private readonly Label _labelLabel;
        private readonly Label _valueLabel;
        private readonly Button _speakButton;
        private readonly Button _copyButton;
        private readonly TableLayoutPanel _layout;

        private Font? _fontLabel;
        private Font? _fontValue;
        private Font? _fontButton;

        private bool _isSpeaking;
        private ContentField? _field;
        private ISpeechCoordinator? _speechCoordinator;

        public ContentFieldRow()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint, true);

            _labelLabel = new Label();
            _valueLabel = new Label();
            _speakButton = new Button();
            _copyButton = new Button();
            _layout = new TableLayoutPanel();

            InitControls();

            this.BackColor = Color.Transparent;
            this.Padding = new Padding(0);
            this.Dock = DockStyle.Top;

            _speakButton.Click += SpeakButton_Click;
            _copyButton.Click += CopyButton_Click;
        }

        private void InitControls()
        {
            _layout.ColumnCount = 3;
            _layout.RowCount = 1;
            _layout.Dock = DockStyle.Fill;
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55F));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55F));

            _labelLabel.Font = new Font("微软雅黑", 11F, FontStyle.Bold, GraphicsUnit.Point, 134);
            _labelLabel.ForeColor = Color.FromArgb(108, 117, 125);
            _labelLabel.TextAlign = ContentAlignment.MiddleLeft;
            _labelLabel.UseMnemonic = false;
            _labelLabel.AutoSize = false;
            _labelLabel.Dock = DockStyle.Fill;
            _labelLabel.Padding = new Padding(0, 0, 4, 0);

            _valueLabel.Font = new Font("微软雅黑", 11F, FontStyle.Regular, GraphicsUnit.Point, 134);
            _valueLabel.ForeColor = Color.FromArgb(33, 33, 33);
            _valueLabel.TextAlign = ContentAlignment.MiddleLeft;
            _valueLabel.UseMnemonic = false;
            _valueLabel.AutoSize = false;
            _valueLabel.Dock = DockStyle.Fill;
            _valueLabel.Padding = new Padding(0);

            _speakButton.Text = "🔊";
            _speakButton.Font = new Font("Arial", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            _speakButton.Width = 24;
            _speakButton.Height = 24;
            _speakButton.FlatStyle = FlatStyle.Flat;
            _speakButton.FlatAppearance.BorderSize = 0;
            _speakButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
            _speakButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 220, 220);
            _speakButton.BackColor = Color.Transparent;
            _speakButton.Visible = false;
            _speakButton.Cursor = Cursors.Hand;

            _copyButton.Text = "📋";
            _copyButton.Font = new Font("Arial", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            _copyButton.Width = 24;
            _copyButton.Height = 24;
            _copyButton.FlatStyle = FlatStyle.Flat;
            _copyButton.FlatAppearance.BorderSize = 0;
            _copyButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
            _copyButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 220, 220);
            _copyButton.BackColor = Color.Transparent;
            _copyButton.Cursor = Cursors.Hand;

            var buttonsPanel = new Panel();
            buttonsPanel.Dock = DockStyle.Fill;
            buttonsPanel.Controls.Add(_speakButton);
            buttonsPanel.Controls.Add(_copyButton);
            _speakButton.Location = new Point(0, 0);
            _copyButton.Location = new Point(26, 0);

            _layout.Controls.Add(_labelLabel, 0, 0);
            _layout.Controls.Add(_valueLabel, 1, 0);
            _layout.Controls.Add(buttonsPanel, 2, 0);

            this.Controls.Add(_layout);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ContentField? Field
        {
            get => _field;
            set
            {
                _field = value;
                UpdateDisplay();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ISpeechCoordinator? SpeechCoordinator
        {
            get => _speechCoordinator;
            set
            {
                if (_speechCoordinator != null)
                {
                    _speechCoordinator.SpeakStateChanged -= SpeechCoordinator_SpeakStateChanged;
                }
                _speechCoordinator = value;
                if (_speechCoordinator != null)
                {
                    _speechCoordinator.SpeakStateChanged += SpeechCoordinator_SpeakStateChanged;
                }
                UpdateSpeakButtonState();
            }
        }

        private void UpdateDisplay()
        {
            if (_field == null)
            {
                _labelLabel.Text = string.Empty;
                _valueLabel.Text = string.Empty;
                _speakButton.Visible = false;
                return;
            }

            _labelLabel.Text = _field.Label;
            _valueLabel.Text = _field.Value;
            _speakButton.Visible = _field.HasSpeakText;
            UpdateSpeakButtonState();
        }

        private void UpdateSpeakButtonState()
        {
            if (_speechCoordinator == null)
            {
                _speakButton.Enabled = false;
                return;
            }

            _speakButton.Enabled = _field?.HasSpeakText == true;
            UpdateSpeakingState();
        }

        private void UpdateSpeakingState()
        {
            string speakKey = GetSpeakKey();
            bool isCurrentSpeaking = _speechCoordinator?.IsSpeaking == true && _speechCoordinator.CurrentSpeakKey == speakKey;
            bool isOtherSpeaking = _speechCoordinator?.IsSpeaking == true && _speechCoordinator.CurrentSpeakKey != speakKey && _speechCoordinator.CurrentSpeakKey.StartsWith("field:");
            
            if (isCurrentSpeaking)
            {
                _isSpeaking = true;
                _speakButton.Text = "⏸";
                _speakButton.Enabled = true;
                _speakButton.ForeColor = Color.FromArgb(76, 175, 80);
                _valueLabel.ForeColor = Color.FromArgb(76, 175, 80);
                this.BackColor = Color.FromArgb(245, 255, 245);
            }
            else if (isOtherSpeaking)
            {
                _isSpeaking = false;
                _speakButton.Text = "🔊";
                _speakButton.Enabled = false;
                _speakButton.ForeColor = Color.FromArgb(180, 180, 180);
                _valueLabel.ForeColor = Color.FromArgb(108, 117, 125);
                this.BackColor = Color.Transparent;
            }
            else
            {
                _isSpeaking = false;
                _speakButton.Text = "🔊";
                _speakButton.Enabled = _field?.HasSpeakText == true;
                _speakButton.ForeColor = SystemColors.ControlText;
                _valueLabel.ForeColor = Color.FromArgb(33, 33, 33);
                this.BackColor = Color.Transparent;
            }
        }

        private string GetSpeakKey()
        {
            if (_field == null) return "__EMPTY__";
            return $"field:{_field.Label}:{_field.SpeakText}";
        }

        private void SpeechCoordinator_SpeakStateChanged(object? sender, SpeakStateChangedEventArgs e)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            this.BeginInvoke(() =>
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    UpdateSpeakingState();
                }
            });
        }

        public async Task TriggerSpeak()
        {
            if (_field?.HasSpeakText != true || _speechCoordinator == null || string.IsNullOrWhiteSpace(_field.SpeakText)) return;

            try
            {
                string speakKey = GetSpeakKey();
                
                if (_speechCoordinator.IsSpeaking && _speechCoordinator.CurrentSpeakKey == speakKey)
                {
                    await _speechCoordinator.StopAsync();
                }
                else
                {
                    string lang = _field.Language;
                    await _speechCoordinator.SpeakAsync(_field.SpeakText, lang, CancellationToken.None, speakKey);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Speak failed: {ex.Message}");
            }
        }

        private async void SpeakButton_Click(object? sender, EventArgs e)
        {
            await TriggerSpeak();
        }

        private void CopyButton_Click(object? sender, EventArgs e)
        {
            if (_field == null) return;

            try
            {
                Clipboard.SetText(_field.Value);
                string originalText = _copyButton.Text;
                _copyButton.Text = "✓";
                _copyButton.ForeColor = Color.FromArgb(76, 175, 80);
                
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    if (!_copyButton.IsDisposed && _copyButton.IsHandleCreated)
                    {
                        _copyButton.BeginInvoke(() =>
                        {
                            if (!_copyButton.IsDisposed && _copyButton.IsHandleCreated)
                            {
                                _copyButton.Text = originalText;
                                _copyButton.ForeColor = SystemColors.ControlText;
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Copy failed: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_speechCoordinator != null)
                {
                    _speechCoordinator.SpeakStateChanged -= SpeechCoordinator_SpeakStateChanged;
                }
                
                _fontLabel?.Dispose();
                _fontValue?.Dispose();
                _fontButton?.Dispose();
                
                _labelLabel?.Dispose();
                _valueLabel?.Dispose();
                _speakButton?.Dispose();
                _copyButton?.Dispose();
                _layout?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}