using LearningAssistant.Common;
using LearningAssistant.Forms.UserControls;
using LearningAssistant.Forms.UserControls.Learning;
using LearningAssistant.Services.Learning;

namespace LearningAssistant.Forms.Learning
{
    public class ReviewForm : Form
    {
        private ReviewPanel _reviewPanel = null!;
        private readonly ISpacedRepetitionService _spacedRepetitionService;
        private readonly string _userId = Constants.DefaultUserId;

        public event EventHandler? StartReview;

        public ReviewForm(ISpacedRepetitionService spacedRepetitionService, string userId = Constants.DefaultUserId)
        {
            _spacedRepetitionService = spacedRepetitionService;
            _userId = userId;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _reviewPanel = new ReviewPanel(_spacedRepetitionService, _userId);

            SuspendLayout();

            _reviewPanel.Dock = DockStyle.Fill;
            _reviewPanel.StartReviewClicked += OnStartReviewClicked;

            Text = "🔔 间隔重复复习";
            Size = new Size(470, 670);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = new Font("微软雅黑", 9F);
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;

            Controls.Add(_reviewPanel);

            ResumeLayout(false);
        }

        private void OnStartReviewClicked(object? sender, EventArgs e)
        {
            StartReview?.Invoke(this, EventArgs.Empty);
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _reviewPanel.Dispose();
        }
    }
}
