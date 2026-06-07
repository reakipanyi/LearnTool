using LearningAssistant.Services.Cloud;
using System.Diagnostics;

namespace LearningAssistant.Forms
{
    public partial class BaiduNetdiskAuthForm : Form
    {
        private readonly ICloudStorageService _cloudService;
        private readonly Action<bool> _onAuthCompleted;

        public BaiduNetdiskAuthForm(ICloudStorageService cloudService, Action<bool> onAuthCompleted)
        {
            InitializeComponent();
            _cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
            _onAuthCompleted = onAuthCompleted ?? throw new ArgumentNullException(nameof(onAuthCompleted));
        }

        private void BaiduNetdiskAuthForm_Load(object sender, EventArgs e)
        {
            try
            {
                var authUrl = _cloudService.GetAuthorizationUrlAsync().Result;
                labelAuthUrl.Text = authUrl;
                
                if (MessageBox.Show($"请访问以下URL完成百度网盘授权，或点击确定在浏览器中打开。\n\n{authUrl}", "百度网盘授权", 
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
                {
                    Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取授权URL失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private async void ButtonSubmit_Click(object sender, EventArgs e)
        {
            var authCode = textBoxAuthCode.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(authCode))
            {
                MessageBox.Show("请输入授权码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            buttonSubmit.Enabled = false;
            labelStatus.Text = "正在授权...";
            
            try
            {
                var success = await _cloudService.AuthenticateAsync(authCode);
                
                if (success)
                {
                    MessageBox.Show("授权成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _onAuthCompleted(true);
                    Close();
                }
                else
                {
                    MessageBox.Show("授权失败，请检查授权码是否正确", "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    labelStatus.Text = "授权失败，请重试";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"授权过程出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                labelStatus.Text = $"错误: {ex.Message}";
            }
            finally
            {
                buttonSubmit.Enabled = true;
            }
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            _onAuthCompleted(false);
            Close();
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            
            labelTitle = new Label();
            labelAuthUrlLabel = new Label();
            labelAuthUrl = new Label();
            labelCodeLabel = new Label();
            textBoxAuthCode = new TextBox();
            buttonSubmit = new Button();
            buttonCancel = new Button();
            labelStatus = new Label();

            SuspendLayout();

            labelTitle.Font = new Font("Microsoft YaHei", 14F, FontStyle.Bold);
            labelTitle.ForeColor = Color.FromArgb(33, 33, 33);
            labelTitle.Location = new Point(120, 20);
            labelTitle.Size = new Size(200, 30);
            labelTitle.Text = "☁️ 百度网盘授权";
            labelTitle.TextAlign = ContentAlignment.MiddleCenter;

            labelAuthUrlLabel.Font = new Font("Microsoft YaHei", 10F);
            labelAuthUrlLabel.ForeColor = Color.FromArgb(66, 66, 66);
            labelAuthUrlLabel.Location = new Point(20, 60);
            labelAuthUrlLabel.Size = new Size(100, 20);
            labelAuthUrlLabel.Text = "授权URL:";

            labelAuthUrl.Font = new Font("Microsoft YaHei", 9F);
            labelAuthUrl.ForeColor = Color.Blue;
            labelAuthUrl.Location = new Point(20, 85);
            labelAuthUrl.Size = new Size(420, 60);
            labelAuthUrl.Text = "";
            labelAuthUrl.AutoSize = false;
            labelAuthUrl.BorderStyle = BorderStyle.FixedSingle;
            labelAuthUrl.Padding = new Padding(5);
            labelAuthUrl.BackColor = Color.White;

            labelCodeLabel.Font = new Font("Microsoft YaHei", 10F);
            labelCodeLabel.ForeColor = Color.FromArgb(66, 66, 66);
            labelCodeLabel.Location = new Point(20, 160);
            labelCodeLabel.Size = new Size(100, 20);
            labelCodeLabel.Text = "授权码:";

            textBoxAuthCode.Font = new Font("Microsoft YaHei", 10F);
            textBoxAuthCode.Location = new Point(20, 185);
            textBoxAuthCode.Size = new Size(420, 25);
            textBoxAuthCode.PlaceholderText = "请输入授权后获取的授权码";

            buttonSubmit.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
            buttonSubmit.Location = new Point(150, 230);
            buttonSubmit.Size = new Size(120, 35);
            buttonSubmit.Text = "确认授权";
            buttonSubmit.BackColor = Color.FromArgb(33, 150, 243);
            buttonSubmit.ForeColor = Color.White;
            buttonSubmit.FlatStyle = FlatStyle.Flat;
            buttonSubmit.Click += ButtonSubmit_Click;

            buttonCancel.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
            buttonCancel.Location = new Point(280, 230);
            buttonCancel.Size = new Size(120, 35);
            buttonCancel.Text = "取消";
            buttonCancel.BackColor = Color.FromArgb(158, 158, 158);
            buttonCancel.ForeColor = Color.White;
            buttonCancel.FlatStyle = FlatStyle.Flat;
            buttonCancel.Click += ButtonCancel_Click;

            labelStatus.Font = new Font("Microsoft YaHei", 9F);
            labelStatus.ForeColor = Color.Red;
            labelStatus.Location = new Point(20, 275);
            labelStatus.Size = new Size(420, 20);
            labelStatus.Text = "";

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(460, 310);
            Controls.Add(labelTitle);
            Controls.Add(labelAuthUrlLabel);
            Controls.Add(labelAuthUrl);
            Controls.Add(labelCodeLabel);
            Controls.Add(textBoxAuthCode);
            Controls.Add(buttonSubmit);
            Controls.Add(buttonCancel);
            Controls.Add(labelStatus);
            Text = "百度网盘授权";
            Load += BaiduNetdiskAuthForm_Load;
            ResumeLayout(false);
        }

        private System.ComponentModel.IContainer components = null;
        private Label labelTitle;
        private Label labelAuthUrlLabel;
        private Label labelAuthUrl;
        private Label labelCodeLabel;
        private TextBox textBoxAuthCode;
        private Button buttonSubmit;
        private Button buttonCancel;
        private Label labelStatus;
    }
}