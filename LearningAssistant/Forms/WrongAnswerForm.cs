using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms
{
    public partial class WrongAnswerForm : Form, IThemeable
    {
        private readonly IWrongAnswerService _wrongAnswerService;
        private readonly ILogger<WrongAnswerForm>? _logger;
        private readonly IThemeService? _themeService;
        private readonly IUserSessionService? _userSessionService;
        private readonly string _userId;
        private List<WrongAnswerItem> _allItems = new();
        private List<WrongAnswerItem> _filteredItems = new();
        private WrongAnswerItem? _currentItem;
        private SubjectType? _currentSubject = null;
        private string _currentStatus = "全部";
        private bool _isBatchMode = false;
        private HashSet<string> _selectedIds = new();

        private Panel panelSidebar;
        private Label labelSearchTitle;
        private TextBox textBoxSearch;
        private Label labelCategoryTitle;
        private ListBox listBoxCategories;
        private Label labelStatusTitle;
        private RadioButton radioStatusAll;
        private RadioButton radioStatusReview;
        private RadioButton radioStatusMastered;
        private Panel panelBottomActions;
        private Button buttonBatchMode;
        private Button buttonStartReview;
        private Button buttonBatchMastered;
        private Button buttonBatchDelete;
        private Label labelSelectedCount;

        private Panel panelMain;
        private Panel panelStatsBar;
        private Label labelStatTotal;
        private Label labelStatReview;
        private Label labelStatMastered;
        private Label labelStatAccuracy;
        private Label labelStatToday;

        private Panel panelDetail;
        private Label labelQuestion;
        private TextBox textBoxQuestion;
        private Label labelCorrectAnswer;
        private TextBox textBoxCorrectAnswer;
        private Label labelUserAnswer;
        private TextBox textBoxUserAnswer;
        private Label labelExplanation;
        private TextBox textBoxExplanation;
        private Label labelDetailStats;
        private Panel panelDetailButtons;
        private Button buttonMarkMastered;
        private Button buttonDelete;
        private Button buttonExport;
        private Button buttonClose;

        private Form? _reviewForm;

        public WrongAnswerForm(
            IWrongAnswerService wrongAnswerService,
            ILogger<WrongAnswerForm>? logger = null,
            IThemeService? themeService = null,
            IUserSessionService? userSessionService = null,
            string? userId = null)
        {
            _wrongAnswerService = wrongAnswerService ?? throw new ArgumentNullException(nameof(wrongAnswerService));
            _logger = logger;
            _themeService = themeService;
            _userSessionService = userSessionService;
            _userId = userId ?? userSessionService?.CurrentUserId ?? Environment.UserName;

            InitializeComponent();
            _themeService?.RegisterThemeable(this);
            LoadWrongAnswers();
            LoadCategories();
        }


        #region 窗体控件字段（和设计器自动生成格式保持一致）
        private IContainer components = null;
        private SplitContainer splitContainerMain;
        private SplitContainer splitContainerDetail;
        private Panel panelList;
        private ListBox listBoxWrongAnswers;
        #endregion

        private void InitializeComponent()
        {
            splitContainerMain = new SplitContainer();
            panelSidebar = new Panel();
            labelSearchTitle = new Label();
            textBoxSearch = new TextBox();
            labelCategoryTitle = new Label();
            listBoxCategories = new ListBox();
            labelStatusTitle = new Label();
            radioStatusAll = new RadioButton();
            radioStatusReview = new RadioButton();
            radioStatusMastered = new RadioButton();
            panelBottomActions = new Panel();
            buttonBatchMode = new Button();
            buttonStartReview = new Button();
            labelSelectedCount = new Label();
            buttonBatchMastered = new Button();
            buttonBatchDelete = new Button();
            panelMain = new Panel();
            splitContainerDetail = new SplitContainer();
            panelList = new Panel();
            listBoxWrongAnswers = new ListBox();
            panelDetail = new Panel();
            labelQuestion = new Label();
            textBoxQuestion = new TextBox();
            labelCorrectAnswer = new Label();
            textBoxCorrectAnswer = new TextBox();
            labelUserAnswer = new Label();
            textBoxUserAnswer = new TextBox();
            labelExplanation = new Label();
            textBoxExplanation = new TextBox();
            labelDetailStats = new Label();
            panelDetailButtons = new Panel();
            buttonMarkMastered = new Button();
            buttonDelete = new Button();
            buttonExport = new Button();
            buttonClose = new Button();
            panelStatsBar = new Panel();
            labelStatTotal = new Label();
            labelStatReview = new Label();
            labelStatMastered = new Label();
            labelStatAccuracy = new Label();
            labelStatToday = new Label();
            ((ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            panelSidebar.SuspendLayout();
            panelBottomActions.SuspendLayout();
            panelMain.SuspendLayout();
            ((ISupportInitialize)splitContainerDetail).BeginInit();
            splitContainerDetail.Panel1.SuspendLayout();
            splitContainerDetail.Panel2.SuspendLayout();
            splitContainerDetail.SuspendLayout();
            panelList.SuspendLayout();
            panelDetail.SuspendLayout();
            panelDetailButtons.SuspendLayout();
            panelStatsBar.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainerMain
            // 
            splitContainerMain.BackColor = Color.FromArgb(230, 230, 240);
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.Location = new Point(0, 0);
            splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            splitContainerMain.Panel1.Controls.Add(panelSidebar);
            // 
            // splitContainerMain.Panel2
            // 
            splitContainerMain.Panel2.Controls.Add(panelMain);
            splitContainerMain.Size = new Size(1032, 561);
            splitContainerMain.SplitterDistance = 286;
            splitContainerMain.SplitterWidth = 1;
            splitContainerMain.TabIndex = 0;
            // 
            // panelSidebar
            // 
            panelSidebar.BackColor = Color.White;
            panelSidebar.Controls.Add(labelSearchTitle);
            panelSidebar.Controls.Add(textBoxSearch);
            panelSidebar.Controls.Add(labelCategoryTitle);
            panelSidebar.Controls.Add(listBoxCategories);
            panelSidebar.Controls.Add(labelStatusTitle);
            panelSidebar.Controls.Add(radioStatusAll);
            panelSidebar.Controls.Add(radioStatusReview);
            panelSidebar.Controls.Add(radioStatusMastered);
            panelSidebar.Controls.Add(panelBottomActions);
            panelSidebar.Dock = DockStyle.Fill;
            panelSidebar.Location = new Point(0, 0);
            panelSidebar.Name = "panelSidebar";
            panelSidebar.Size = new Size(286, 561);
            panelSidebar.TabIndex = 0;
            // 
            // labelSearchTitle
            // 
            labelSearchTitle.AutoSize = true;
            labelSearchTitle.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            labelSearchTitle.ForeColor = Color.FromArgb(33, 33, 33);
            labelSearchTitle.Location = new Point(12, 12);
            labelSearchTitle.Name = "labelSearchTitle";
            labelSearchTitle.Size = new Size(59, 19);
            labelSearchTitle.TabIndex = 0;
            labelSearchTitle.Text = "🔍 搜索";
            // 
            // textBoxSearch
            // 
            textBoxSearch.BorderStyle = BorderStyle.FixedSingle;
            textBoxSearch.Font = new Font("微软雅黑", 10F);
            textBoxSearch.Location = new Point(12, 42);
            textBoxSearch.Name = "textBoxSearch";
            textBoxSearch.PlaceholderText = "搜索题目...";
            textBoxSearch.Size = new Size(196, 25);
            textBoxSearch.TabIndex = 1;
            textBoxSearch.TextChanged += TextBoxSearch_TextChanged;
            // 
            // labelCategoryTitle
            // 
            labelCategoryTitle.AutoSize = true;
            labelCategoryTitle.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            labelCategoryTitle.ForeColor = Color.FromArgb(33, 33, 33);
            labelCategoryTitle.Location = new Point(12, 78);
            labelCategoryTitle.Name = "labelCategoryTitle";
            labelCategoryTitle.Size = new Size(89, 19);
            labelCategoryTitle.TabIndex = 2;
            labelCategoryTitle.Text = "📂 分类筛选";
            // 
            // listBoxCategories
            // 
            listBoxCategories.BackColor = Color.White;
            listBoxCategories.BorderStyle = BorderStyle.None;
            listBoxCategories.Cursor = Cursors.Hand;
            listBoxCategories.Font = new Font("微软雅黑", 10F);
            listBoxCategories.ForeColor = Color.FromArgb(50, 50, 50);
            listBoxCategories.Location = new Point(12, 108);
            listBoxCategories.Name = "listBoxCategories";
            listBoxCategories.Size = new Size(196, 133);
            listBoxCategories.TabIndex = 3;
            listBoxCategories.SelectedIndexChanged += ListBoxCategories_SelectedIndexChanged;
            // 
            // labelStatusTitle
            // 
            labelStatusTitle.AutoSize = true;
            labelStatusTitle.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            labelStatusTitle.ForeColor = Color.FromArgb(33, 33, 33);
            labelStatusTitle.Location = new Point(12, 268);
            labelStatusTitle.Name = "labelStatusTitle";
            labelStatusTitle.Size = new Size(89, 19);
            labelStatusTitle.TabIndex = 4;
            labelStatusTitle.Text = "🏷️ 状态筛选";
            // 
            // radioStatusAll
            // 
            radioStatusAll.AutoSize = true;
            radioStatusAll.Checked = true;
            radioStatusAll.Font = new Font("微软雅黑", 9F);
            radioStatusAll.ForeColor = Color.FromArgb(50, 50, 50);
            radioStatusAll.Location = new Point(16, 298);
            radioStatusAll.Name = "radioStatusAll";
            radioStatusAll.Size = new Size(62, 21);
            radioStatusAll.TabIndex = 5;
            radioStatusAll.TabStop = true;
            radioStatusAll.Text = "● 全部";
            radioStatusAll.CheckedChanged += RadioStatus_CheckedChanged;
            // 
            // radioStatusReview
            // 
            radioStatusReview.AutoSize = true;
            radioStatusReview.Font = new Font("微软雅黑", 9F);
            radioStatusReview.ForeColor = Color.FromArgb(50, 50, 50);
            radioStatusReview.Location = new Point(16, 324);
            radioStatusReview.Name = "radioStatusReview";
            radioStatusReview.Size = new Size(73, 21);
            radioStatusReview.TabIndex = 6;
            radioStatusReview.Text = "○ 待复习";
            radioStatusReview.CheckedChanged += RadioStatus_CheckedChanged;
            // 
            // radioStatusMastered
            // 
            radioStatusMastered.AutoSize = true;
            radioStatusMastered.Font = new Font("微软雅黑", 9F);
            radioStatusMastered.ForeColor = Color.FromArgb(50, 50, 50);
            radioStatusMastered.Location = new Point(16, 350);
            radioStatusMastered.Name = "radioStatusMastered";
            radioStatusMastered.Size = new Size(73, 21);
            radioStatusMastered.TabIndex = 7;
            radioStatusMastered.Text = "○ 已掌握";
            radioStatusMastered.CheckedChanged += RadioStatus_CheckedChanged;
            // 
            // panelBottomActions
            // 
            panelBottomActions.BackColor = Color.FromArgb(248, 248, 252);
            panelBottomActions.Controls.Add(buttonBatchMode);
            panelBottomActions.Controls.Add(buttonStartReview);
            panelBottomActions.Controls.Add(labelSelectedCount);
            panelBottomActions.Controls.Add(buttonBatchMastered);
            panelBottomActions.Controls.Add(buttonBatchDelete);
            panelBottomActions.Dock = DockStyle.Bottom;
            panelBottomActions.Location = new Point(0, 511);
            panelBottomActions.Name = "panelBottomActions";
            panelBottomActions.Size = new Size(286, 50);
            panelBottomActions.TabIndex = 8;
            // 
            // buttonBatchMode
            // 
            buttonBatchMode.BackColor = Color.FromArgb(240, 240, 245);
            buttonBatchMode.Cursor = Cursors.Hand;
            buttonBatchMode.FlatAppearance.BorderSize = 0;
            buttonBatchMode.FlatStyle = FlatStyle.Flat;
            buttonBatchMode.Font = new Font("微软雅黑", 9F);
            buttonBatchMode.ForeColor = Color.FromArgb(60, 60, 60);
            buttonBatchMode.Location = new Point(12, 10);
            buttonBatchMode.Name = "buttonBatchMode";
            buttonBatchMode.Size = new Size(95, 30);
            buttonBatchMode.TabIndex = 0;
            buttonBatchMode.Text = "☑ 批量操作";
            buttonBatchMode.UseVisualStyleBackColor = false;
            buttonBatchMode.Click += ButtonBatchMode_Click;
            // 
            // buttonStartReview
            // 
            buttonStartReview.BackColor = Color.FromArgb(63, 81, 181);
            buttonStartReview.Cursor = Cursors.Hand;
            buttonStartReview.FlatAppearance.BorderSize = 0;
            buttonStartReview.FlatStyle = FlatStyle.Flat;
            buttonStartReview.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            buttonStartReview.ForeColor = Color.White;
            buttonStartReview.Location = new Point(113, 10);
            buttonStartReview.Name = "buttonStartReview";
            buttonStartReview.Size = new Size(95, 30);
            buttonStartReview.TabIndex = 1;
            buttonStartReview.Text = "📖 开始复习";
            buttonStartReview.UseVisualStyleBackColor = false;
            buttonStartReview.Click += ButtonStartReview_Click;
            // 
            // labelSelectedCount
            // 
            labelSelectedCount.AutoSize = true;
            labelSelectedCount.Font = new Font("微软雅黑", 9F);
            labelSelectedCount.ForeColor = Color.FromArgb(60, 60, 60);
            labelSelectedCount.Location = new Point(12, 15);
            labelSelectedCount.Name = "labelSelectedCount";
            labelSelectedCount.Size = new Size(71, 17);
            labelSelectedCount.TabIndex = 2;
            labelSelectedCount.Text = "已选择 0 项";
            labelSelectedCount.Visible = false;
            // 
            // buttonBatchMastered
            // 
            buttonBatchMastered.BackColor = Color.FromArgb(76, 175, 80);
            buttonBatchMastered.Cursor = Cursors.Hand;
            buttonBatchMastered.Enabled = false;
            buttonBatchMastered.FlatAppearance.BorderSize = 0;
            buttonBatchMastered.FlatStyle = FlatStyle.Flat;
            buttonBatchMastered.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            buttonBatchMastered.ForeColor = Color.White;
            buttonBatchMastered.Location = new Point(100, 10);
            buttonBatchMastered.Name = "buttonBatchMastered";
            buttonBatchMastered.Size = new Size(100, 30);
            buttonBatchMastered.TabIndex = 3;
            buttonBatchMastered.Text = "✅ 标记已掌握";
            buttonBatchMastered.UseVisualStyleBackColor = false;
            buttonBatchMastered.Visible = false;
            buttonBatchMastered.Click += ButtonBatchMastered_Click;
            // 
            // buttonBatchDelete
            // 
            buttonBatchDelete.BackColor = Color.FromArgb(244, 67, 54);
            buttonBatchDelete.Cursor = Cursors.Hand;
            buttonBatchDelete.Enabled = false;
            buttonBatchDelete.FlatAppearance.BorderSize = 0;
            buttonBatchDelete.FlatStyle = FlatStyle.Flat;
            buttonBatchDelete.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            buttonBatchDelete.ForeColor = Color.White;
            buttonBatchDelete.Location = new Point(208, 10);
            buttonBatchDelete.Name = "buttonBatchDelete";
            buttonBatchDelete.Size = new Size(95, 30);
            buttonBatchDelete.TabIndex = 4;
            buttonBatchDelete.Text = "🗑️ 批量删除";
            buttonBatchDelete.UseVisualStyleBackColor = false;
            buttonBatchDelete.Visible = false;
            buttonBatchDelete.Click += ButtonBatchDelete_Click;
            // 
            // panelMain
            // 
            panelMain.BackColor = Color.FromArgb(248, 248, 252);
            panelMain.Controls.Add(splitContainerDetail);
            panelMain.Controls.Add(panelStatsBar);
            panelMain.Dock = DockStyle.Fill;
            panelMain.Location = new Point(0, 0);
            panelMain.Name = "panelMain";
            panelMain.Size = new Size(745, 561);
            panelMain.TabIndex = 0;
            // 
            // splitContainerDetail
            // 
            splitContainerDetail.BackColor = Color.FromArgb(230, 230, 240);
            splitContainerDetail.Dock = DockStyle.Fill;
            splitContainerDetail.Location = new Point(0, 60);
            splitContainerDetail.Name = "splitContainerDetail";
            splitContainerDetail.Orientation = Orientation.Horizontal;
            // 
            // splitContainerDetail.Panel1
            // 
            splitContainerDetail.Panel1.Controls.Add(panelList);
            // 
            // splitContainerDetail.Panel2
            // 
            splitContainerDetail.Panel2.Controls.Add(panelDetail);
            splitContainerDetail.Size = new Size(745, 501);
            splitContainerDetail.SplitterDistance = 25;
            splitContainerDetail.SplitterWidth = 1;
            splitContainerDetail.TabIndex = 0;
            // 
            // panelList
            // 
            panelList.BackColor = Color.FromArgb(248, 248, 252);
            panelList.Controls.Add(listBoxWrongAnswers);
            panelList.Dock = DockStyle.Fill;
            panelList.Location = new Point(0, 0);
            panelList.Name = "panelList";
            panelList.Size = new Size(745, 25);
            panelList.TabIndex = 0;
            // 
            // listBoxWrongAnswers
            // 
            listBoxWrongAnswers.BackColor = Color.FromArgb(248, 248, 252);
            listBoxWrongAnswers.BorderStyle = BorderStyle.None;
            listBoxWrongAnswers.Cursor = Cursors.Hand;
            listBoxWrongAnswers.Dock = DockStyle.Fill;
            listBoxWrongAnswers.DrawMode = DrawMode.OwnerDrawFixed;
            listBoxWrongAnswers.Font = new Font("微软雅黑", 10F);
            listBoxWrongAnswers.ForeColor = Color.FromArgb(50, 50, 50);
            listBoxWrongAnswers.ItemHeight = 48;
            listBoxWrongAnswers.Location = new Point(0, 0);
            listBoxWrongAnswers.Name = "listBoxWrongAnswers";
            listBoxWrongAnswers.Size = new Size(745, 25);
            listBoxWrongAnswers.TabIndex = 0;
            listBoxWrongAnswers.DrawItem += ListBoxWrongAnswers_DrawItem;
            listBoxWrongAnswers.SelectedIndexChanged += ListBoxWrongAnswers_SelectedIndexChanged;
            // 
            // panelDetail
            // 
            panelDetail.BackColor = Color.White;
            panelDetail.Controls.Add(labelQuestion);
            panelDetail.Controls.Add(textBoxQuestion);
            panelDetail.Controls.Add(labelCorrectAnswer);
            panelDetail.Controls.Add(textBoxCorrectAnswer);
            panelDetail.Controls.Add(labelUserAnswer);
            panelDetail.Controls.Add(textBoxUserAnswer);
            panelDetail.Controls.Add(labelExplanation);
            panelDetail.Controls.Add(textBoxExplanation);
            panelDetail.Controls.Add(labelDetailStats);
            panelDetail.Controls.Add(panelDetailButtons);
            panelDetail.Dock = DockStyle.Fill;
            panelDetail.Location = new Point(0, 0);
            panelDetail.Name = "panelDetail";
            panelDetail.Size = new Size(745, 475);
            panelDetail.TabIndex = 0;
            // 
            // labelQuestion
            // 
            labelQuestion.AutoSize = true;
            labelQuestion.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelQuestion.ForeColor = Color.FromArgb(33, 33, 33);
            labelQuestion.Location = new Point(20, 15);
            labelQuestion.Name = "labelQuestion";
            labelQuestion.Size = new Size(65, 19);
            labelQuestion.TabIndex = 0;
            labelQuestion.Text = "❌ 题目:";
            // 
            // textBoxQuestion
            // 
            textBoxQuestion.BackColor = Color.FromArgb(250, 250, 252);
            textBoxQuestion.BorderStyle = BorderStyle.FixedSingle;
            textBoxQuestion.Font = new Font("微软雅黑", 10F);
            textBoxQuestion.Location = new Point(20, 40);
            textBoxQuestion.Multiline = true;
            textBoxQuestion.Name = "textBoxQuestion";
            textBoxQuestion.ReadOnly = true;
            textBoxQuestion.ScrollBars = ScrollBars.Vertical;
            textBoxQuestion.Size = new Size(700, 50);
            textBoxQuestion.TabIndex = 1;
            // 
            // labelCorrectAnswer
            // 
            labelCorrectAnswer.AutoSize = true;
            labelCorrectAnswer.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelCorrectAnswer.ForeColor = Color.FromArgb(76, 175, 80);
            labelCorrectAnswer.Location = new Point(20, 100);
            labelCorrectAnswer.Name = "labelCorrectAnswer";
            labelCorrectAnswer.Size = new Size(93, 19);
            labelCorrectAnswer.TabIndex = 2;
            labelCorrectAnswer.Text = "✅ 正确答案:";
            // 
            // textBoxCorrectAnswer
            // 
            textBoxCorrectAnswer.BackColor = Color.FromArgb(250, 250, 252);
            textBoxCorrectAnswer.BorderStyle = BorderStyle.FixedSingle;
            textBoxCorrectAnswer.Font = new Font("微软雅黑", 10F);
            textBoxCorrectAnswer.ForeColor = Color.FromArgb(76, 175, 80);
            textBoxCorrectAnswer.Location = new Point(20, 125);
            textBoxCorrectAnswer.Multiline = true;
            textBoxCorrectAnswer.Name = "textBoxCorrectAnswer";
            textBoxCorrectAnswer.ReadOnly = true;
            textBoxCorrectAnswer.ScrollBars = ScrollBars.Vertical;
            textBoxCorrectAnswer.Size = new Size(700, 40);
            textBoxCorrectAnswer.TabIndex = 3;
            // 
            // labelUserAnswer
            // 
            labelUserAnswer.AutoSize = true;
            labelUserAnswer.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelUserAnswer.ForeColor = Color.FromArgb(244, 67, 54);
            labelUserAnswer.Location = new Point(20, 175);
            labelUserAnswer.Name = "labelUserAnswer";
            labelUserAnswer.Size = new Size(93, 19);
            labelUserAnswer.TabIndex = 4;
            labelUserAnswer.Text = "❌ 你的答案:";
            // 
            // textBoxUserAnswer
            // 
            textBoxUserAnswer.BackColor = Color.FromArgb(250, 250, 252);
            textBoxUserAnswer.BorderStyle = BorderStyle.FixedSingle;
            textBoxUserAnswer.Font = new Font("微软雅黑", 10F);
            textBoxUserAnswer.ForeColor = Color.FromArgb(244, 67, 54);
            textBoxUserAnswer.Location = new Point(20, 200);
            textBoxUserAnswer.Multiline = true;
            textBoxUserAnswer.Name = "textBoxUserAnswer";
            textBoxUserAnswer.ReadOnly = true;
            textBoxUserAnswer.ScrollBars = ScrollBars.Vertical;
            textBoxUserAnswer.Size = new Size(700, 40);
            textBoxUserAnswer.TabIndex = 5;
            // 
            // labelExplanation
            // 
            labelExplanation.AutoSize = true;
            labelExplanation.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelExplanation.ForeColor = Color.FromArgb(33, 33, 33);
            labelExplanation.Location = new Point(20, 250);
            labelExplanation.Name = "labelExplanation";
            labelExplanation.Size = new Size(65, 19);
            labelExplanation.TabIndex = 6;
            labelExplanation.Text = "📝 解析:";
            // 
            // textBoxExplanation
            // 
            textBoxExplanation.BackColor = Color.FromArgb(250, 250, 252);
            textBoxExplanation.BorderStyle = BorderStyle.FixedSingle;
            textBoxExplanation.Font = new Font("微软雅黑", 10F);
            textBoxExplanation.Location = new Point(20, 275);
            textBoxExplanation.Multiline = true;
            textBoxExplanation.Name = "textBoxExplanation";
            textBoxExplanation.ReadOnly = true;
            textBoxExplanation.ScrollBars = ScrollBars.Vertical;
            textBoxExplanation.Size = new Size(700, 80);
            textBoxExplanation.TabIndex = 7;
            // 
            // labelDetailStats
            // 
            labelDetailStats.AutoSize = true;
            labelDetailStats.Font = new Font("微软雅黑", 9F);
            labelDetailStats.ForeColor = Color.FromArgb(150, 150, 150);
            labelDetailStats.Location = new Point(20, 365);
            labelDetailStats.Name = "labelDetailStats";
            labelDetailStats.Size = new Size(0, 17);
            labelDetailStats.TabIndex = 8;
            // 
            // panelDetailButtons
            // 
            panelDetailButtons.BackColor = Color.Transparent;
            panelDetailButtons.Controls.Add(buttonMarkMastered);
            panelDetailButtons.Controls.Add(buttonDelete);
            panelDetailButtons.Controls.Add(buttonExport);
            panelDetailButtons.Controls.Add(buttonClose);
            panelDetailButtons.Location = new Point(20, 395);
            panelDetailButtons.Name = "panelDetailButtons";
            panelDetailButtons.Size = new Size(700, 40);
            panelDetailButtons.TabIndex = 9;
            // 
            // buttonMarkMastered
            // 
            buttonMarkMastered.BackColor = Color.FromArgb(76, 175, 80);
            buttonMarkMastered.Cursor = Cursors.Hand;
            buttonMarkMastered.Enabled = false;
            buttonMarkMastered.FlatAppearance.BorderSize = 0;
            buttonMarkMastered.FlatStyle = FlatStyle.Flat;
            buttonMarkMastered.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            buttonMarkMastered.ForeColor = Color.White;
            buttonMarkMastered.Location = new Point(0, 0);
            buttonMarkMastered.Name = "buttonMarkMastered";
            buttonMarkMastered.Size = new Size(100, 35);
            buttonMarkMastered.TabIndex = 0;
            buttonMarkMastered.Text = "✅ 已掌握";
            buttonMarkMastered.UseVisualStyleBackColor = false;
            buttonMarkMastered.Click += ButtonMarkMastered_Click;
            // 
            // buttonDelete
            // 
            buttonDelete.BackColor = Color.FromArgb(244, 67, 54);
            buttonDelete.Cursor = Cursors.Hand;
            buttonDelete.Enabled = false;
            buttonDelete.FlatAppearance.BorderSize = 0;
            buttonDelete.FlatStyle = FlatStyle.Flat;
            buttonDelete.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            buttonDelete.ForeColor = Color.White;
            buttonDelete.Location = new Point(108, 0);
            buttonDelete.Name = "buttonDelete";
            buttonDelete.Size = new Size(80, 35);
            buttonDelete.TabIndex = 1;
            buttonDelete.Text = "🗑️ 删除";
            buttonDelete.UseVisualStyleBackColor = false;
            buttonDelete.Click += ButtonDelete_Click;
            // 
            // buttonExport
            // 
            buttonExport.BackColor = Color.FromArgb(33, 150, 243);
            buttonExport.Cursor = Cursors.Hand;
            buttonExport.FlatAppearance.BorderSize = 0;
            buttonExport.FlatStyle = FlatStyle.Flat;
            buttonExport.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            buttonExport.ForeColor = Color.White;
            buttonExport.Location = new Point(540, 0);
            buttonExport.Name = "buttonExport";
            buttonExport.Size = new Size(70, 35);
            buttonExport.TabIndex = 2;
            buttonExport.Text = "📤 导出";
            buttonExport.UseVisualStyleBackColor = false;
            buttonExport.Click += ButtonExport_Click;
            // 
            // buttonClose
            // 
            buttonClose.BackColor = Color.Gray;
            buttonClose.Cursor = Cursors.Hand;
            buttonClose.FlatAppearance.BorderSize = 0;
            buttonClose.FlatStyle = FlatStyle.Flat;
            buttonClose.Font = new Font("微软雅黑", 9F);
            buttonClose.ForeColor = Color.White;
            buttonClose.Location = new Point(620, 0);
            buttonClose.Name = "buttonClose";
            buttonClose.Size = new Size(80, 35);
            buttonClose.TabIndex = 3;
            buttonClose.Text = "关闭";
            buttonClose.UseVisualStyleBackColor = false;
            buttonClose.Click += ButtonClose_Click;
            // 
            // panelStatsBar
            // 
            panelStatsBar.BackColor = Color.White;
            panelStatsBar.Controls.Add(labelStatTotal);
            panelStatsBar.Controls.Add(labelStatReview);
            panelStatsBar.Controls.Add(labelStatMastered);
            panelStatsBar.Controls.Add(labelStatAccuracy);
            panelStatsBar.Controls.Add(labelStatToday);
            panelStatsBar.Dock = DockStyle.Top;
            panelStatsBar.Location = new Point(0, 0);
            panelStatsBar.Name = "panelStatsBar";
            panelStatsBar.Size = new Size(745, 60);
            panelStatsBar.TabIndex = 1;
            // 
            // labelStatTotal
            // 
            labelStatTotal.AutoSize = true;
            labelStatTotal.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelStatTotal.ForeColor = Color.FromArgb(33, 33, 33);
            labelStatTotal.Location = new Point(20, 18);
            labelStatTotal.Name = "labelStatTotal";
            labelStatTotal.Size = new Size(91, 19);
            labelStatTotal.TabIndex = 0;
            labelStatTotal.Text = "📊 总错题: 0";
            // 
            // labelStatReview
            // 
            labelStatReview.AutoSize = true;
            labelStatReview.Font = new Font("微软雅黑", 10F);
            labelStatReview.ForeColor = Color.FromArgb(244, 67, 54);
            labelStatReview.Location = new Point(140, 18);
            labelStatReview.Name = "labelStatReview";
            labelStatReview.Size = new Size(84, 20);
            labelStatReview.TabIndex = 1;
            labelStatReview.Text = "⏳ 待复习: 0";
            // 
            // labelStatMastered
            // 
            labelStatMastered.AutoSize = true;
            labelStatMastered.Font = new Font("微软雅黑", 10F);
            labelStatMastered.ForeColor = Color.FromArgb(76, 175, 80);
            labelStatMastered.Location = new Point(260, 18);
            labelStatMastered.Name = "labelStatMastered";
            labelStatMastered.Size = new Size(89, 20);
            labelStatMastered.TabIndex = 2;
            labelStatMastered.Text = "✅ 已掌握: 0";
            // 
            // labelStatAccuracy
            // 
            labelStatAccuracy.AutoSize = true;
            labelStatAccuracy.Font = new Font("微软雅黑", 10F);
            labelStatAccuracy.ForeColor = Color.FromArgb(33, 150, 243);
            labelStatAccuracy.Location = new Point(380, 18);
            labelStatAccuracy.Name = "labelStatAccuracy";
            labelStatAccuracy.Size = new Size(101, 20);
            labelStatAccuracy.TabIndex = 3;
            labelStatAccuracy.Text = "🎯 正确率: 0%";
            // 
            // labelStatToday
            // 
            labelStatToday.AutoSize = true;
            labelStatToday.Font = new Font("微软雅黑", 10F);
            labelStatToday.ForeColor = Color.FromArgb(156, 39, 176);
            labelStatToday.Location = new Point(500, 18);
            labelStatToday.Name = "labelStatToday";
            labelStatToday.Size = new Size(102, 20);
            labelStatToday.TabIndex = 4;
            labelStatToday.Text = "📅 今日新增: 0";
            // 
            // WrongAnswerForm
            // 
            BackColor = Color.FromArgb(245, 245, 250);
            ClientSize = new Size(1032, 561);
            Controls.Add(splitContainerMain);
            Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            MinimumSize = new Size(800, 500);
            Name = "WrongAnswerForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "📕 错题本";
            Resize += WrongAnswerForm_Resize;
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            panelSidebar.ResumeLayout(false);
            panelSidebar.PerformLayout();
            panelBottomActions.ResumeLayout(false);
            panelBottomActions.PerformLayout();
            panelMain.ResumeLayout(false);
            splitContainerDetail.Panel1.ResumeLayout(false);
            splitContainerDetail.Panel2.ResumeLayout(false);
            ((ISupportInitialize)splitContainerDetail).EndInit();
            splitContainerDetail.ResumeLayout(false);
            panelList.ResumeLayout(false);
            panelDetail.ResumeLayout(false);
            panelDetail.PerformLayout();
            panelDetailButtons.ResumeLayout(false);
            panelStatsBar.ResumeLayout(false);
            panelStatsBar.PerformLayout();
            ResumeLayout(false);
        }


        private void WrongAnswerForm_Resize(object? sender, EventArgs e)
        {
            if (panelDetail == null) return;

            int width = panelDetail.Width - 40;
            if (width < 200) width = 200;

            if (textBoxQuestion != null) textBoxQuestion.Width = width;
            if (textBoxCorrectAnswer != null) textBoxCorrectAnswer.Width = width;
            if (textBoxUserAnswer != null) textBoxUserAnswer.Width = width;
            if (textBoxExplanation != null) textBoxExplanation.Width = width;

            if (panelDetailButtons != null)
            {
                panelDetailButtons.Width = width;
                if (buttonExport != null) buttonExport.Left = width - 158;
                if (buttonClose != null) buttonClose.Left = width - 80;
            }
        }

        private void ListBoxWrongAnswers_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _filteredItems.Count) return;

            var item = _filteredItems[e.Index];
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool isChecked = _selectedIds.Contains(item.Id);

            Color bgColor;
            if (isSelected)
                bgColor = Color.FromArgb(230, 230, 250);
            else
                bgColor = e.Index % 2 == 0 ? Color.White : Color.FromArgb(250, 250, 252);

            using var bgBrush = new SolidBrush(bgColor);
            g.FillRectangle(bgBrush, e.Bounds);

            int padding = 10;
            int x = padding;

            if (_isBatchMode)
            {
                int checkSize = 16;
                int checkY = e.Bounds.Y + (e.Bounds.Height - checkSize) / 2;
                using var checkPen = new Pen(Color.FromArgb(150, 150, 150), 1.5f);
                g.DrawRectangle(checkPen, x, checkY, checkSize, checkSize);

                if (isChecked)
                {
                    using var checkBrush = new SolidBrush(Color.FromArgb(63, 81, 181));
                    g.FillRectangle(checkBrush, x + 2, checkY + 2, checkSize - 4, checkSize - 4);

                    using var tickPen = new Pen(Color.White, 2f);
                    g.DrawLine(tickPen, x + 4, checkY + 9, x + 7, checkY + 12);
                    g.DrawLine(tickPen, x + 7, checkY + 12, x + 13, checkY + 5);
                }

                x += checkSize + 10;
            }

            string statusIcon = item.IsMastered ? "✅" : "❌";
            string statusColor = item.IsMastered ? "#4CAF50" : "#F44336";

            using var iconFont = new Font("Segoe UI Emoji", 10F);
            using var iconBrush = new SolidBrush(item.IsMastered ? Color.FromArgb(76, 175, 80) : Color.FromArgb(244, 67, 54));
            g.DrawString(statusIcon, iconFont, iconBrush, x, e.Bounds.Y + padding);

            int titleX = x + 25;
            string title = item.Question.Length > 35 ? item.Question.Substring(0, 35) + "..." : item.Question;

            using var titleFont = new Font("微软雅黑", 10F, FontStyle.Bold);
            using var titleBrush = new SolidBrush(Color.FromArgb(50, 50, 50));
            var titleRect = new RectangleF(titleX, e.Bounds.Y + padding + 2, e.Bounds.Width - titleX - padding, 20);
            using var titleFormat = new StringFormat
            {
                FormatFlags = StringFormatFlags.NoWrap,
                Trimming = StringTrimming.EllipsisCharacter
            };
            g.DrawString(title, titleFont, titleBrush, titleRect, titleFormat);

            string meta = $"【{item.Subject}】 错误{item.WrongCount}次 | {item.AddedAt:yyyy-MM-dd}";
            using var metaFont = new Font("微软雅黑", 8F);
            using var metaBrush = new SolidBrush(Color.FromArgb(150, 150, 150));
            var metaRect = new RectangleF(titleX, e.Bounds.Y + padding + 22, e.Bounds.Width - titleX - padding, 16);
            g.DrawString(meta, metaFont, metaBrush, metaRect, titleFormat);
        }

        private void LoadWrongAnswers()
        {
            try
            {
                _allItems = _wrongAnswerService.GetWrongAnswers(_userId);
                _filteredItems = new List<WrongAnswerItem>(_allItems);
                UpdateDisplay();
                UpdateStats();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载错题本失败");
                MessageBox.Show($"加载错题本失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadCategories()
        {
            try
            {
                var subjects = _allItems.Select(i => i.Subject).Distinct().OrderBy(s => s).ToList();
                listBoxCategories.Items.Clear();
                listBoxCategories.Items.Add("📋 全部");
                foreach (var sub in subjects)
                {
                    listBoxCategories.Items.Add($"📁 {sub}");
                }
                listBoxCategories.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载分类失败");
            }
        }

        private void ApplyFilters()
        {
            IEnumerable<WrongAnswerItem> query = _allItems;

            if (_currentSubject.HasValue)
            {
                query = query.Where(i => i.Subject == _currentSubject.Value);
            }

            if (_currentStatus == "待复习")
            {
                query = query.Where(i => !i.IsMastered);
            }
            else if (_currentStatus == "已掌握")
            {
                query = query.Where(i => i.IsMastered);
            }

            string searchText = textBoxSearch.Text.Trim();
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(i =>
                    i.Question.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    i.Subject.ToString().IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            _filteredItems = query.OrderByDescending(i => i.AddedAt).ToList();
            UpdateDisplay();
            UpdateStats();
        }

        private void UpdateDisplay()
        {
            listBoxWrongAnswers.Items.Clear();
            foreach (var item in _filteredItems)
            {
                listBoxWrongAnswers.Items.Add(item);
            }
        }

        private void UpdateStats()
        {
            int total = _allItems.Count;
            int mastered = _allItems.Count(i => i.IsMastered);
            int unmastered = total - mastered;
            double accuracy = total > 0 ? (double)mastered / total * 100 : 0;
            int todayAdded = _allItems.Count(i => i.AddedAt.Date == DateTime.Today);

            labelStatTotal.Text = $"📊 总错题: {total}";
            labelStatReview.Text = $"⏳ 待复习: {unmastered}";
            labelStatMastered.Text = $"✅ 已掌握: {mastered}";
            labelStatAccuracy.Text = $"🎯 正确率: {accuracy:F1}%";
            labelStatToday.Text = $"📅 今日新增: {todayAdded}";
        }

        private void ListBoxCategories_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (listBoxCategories.SelectedIndex < 0) return;

            string selected = listBoxCategories.SelectedItem?.ToString() ?? "";

            if (selected == "📋 全部")
            {
                _currentSubject = null;
            }
            else
            {
                var subjectStr = selected.Replace("📁 ", "").Trim();
                SubjectSubCategoryMapping.TryParseSubject(subjectStr, out var subject);
                _currentSubject = subject;
            }

            ApplyFilters();
        }

        private void RadioStatus_CheckedChanged(object? sender, EventArgs e)
        {
            if (radioStatusAll.Checked)
                _currentStatus = "全部";
            else if (radioStatusReview.Checked)
                _currentStatus = "待复习";
            else if (radioStatusMastered.Checked)
                _currentStatus = "已掌握";

            ApplyFilters();
        }

        private void TextBoxSearch_TextChanged(object? sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void ListBoxWrongAnswers_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (listBoxWrongAnswers.SelectedIndex < 0 || listBoxWrongAnswers.SelectedItem is not WrongAnswerItem item)
            {
                _currentItem = null;
                ClearDetail();
                return;
            }

            if (_isBatchMode)
            {
                if (_selectedIds.Contains(item.Id))
                    _selectedIds.Remove(item.Id);
                else
                    _selectedIds.Add(item.Id);

                listBoxWrongAnswers.Invalidate();
                UpdateSelectedCount();
                return;
            }

            _currentItem = item;
            LoadDetail(item);
            SetDetailButtonsEnabled(true);
        }

        private void LoadDetail(WrongAnswerItem item)
        {
            textBoxQuestion.Text = item.Question;
            textBoxCorrectAnswer.Text = item.CorrectAnswer;
            textBoxUserAnswer.Text = item.UserAnswer;
            textBoxExplanation.Text = item.Explanation;
            labelDetailStats.Text = $"错误次数: {item.WrongCount} | 复习次数: {item.ReviewCount} | 添加时间: {item.AddedAt:yyyy-MM-dd}";
        }

        private void ClearDetail()
        {
            textBoxQuestion.Text = "";
            textBoxCorrectAnswer.Text = "";
            textBoxUserAnswer.Text = "";
            textBoxExplanation.Text = "";
            labelDetailStats.Text = "";
            SetDetailButtonsEnabled(false);
        }

        private void SetDetailButtonsEnabled(bool enabled)
        {
            buttonMarkMastered.Enabled = enabled;
            buttonDelete.Enabled = enabled;
        }

        private void ButtonMarkMastered_Click(object? sender, EventArgs e)
        {
            if (_currentItem == null) return;

            try
            {
                _wrongAnswerService.MarkAsMastered(_userId, _currentItem.Id);
                LoadWrongAnswers();
                LoadCategories();
                MessageBox.Show("已标记为已掌握", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "标记已掌握失败");
                MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonDelete_Click(object? sender, EventArgs e)
        {
            if (_currentItem == null) return;

            var result = MessageBox.Show($"确定要删除这道错题吗？\n\n{_currentItem.Question}", "确认删除",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                _wrongAnswerService.RemoveWrongAnswer(_userId, _currentItem.Id);
                LoadWrongAnswers();
                LoadCategories();
                ClearDetail();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "删除错题失败");
                MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonExport_Click(object? sender, EventArgs e)
        {
            try
            {
                using var dialog = new SaveFileDialog();
                dialog.Filter = "文本文件|*.txt";
                dialog.FileName = $"错题本_{DateTime.Now:yyyyMMdd}.txt";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _wrongAnswerService.ExportWrongAnswers(_userId, dialog.FileName);
                    MessageBox.Show($"错题本已导出到:\n{dialog.FileName}", "导出成功",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出错题本失败");
                MessageBox.Show($"导出错题本失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonClose_Click(object? sender, EventArgs e)
        {
            Close();
        }

        private void ButtonBatchMode_Click(object? sender, EventArgs e)
        {
            _isBatchMode = !_isBatchMode;
            _selectedIds.Clear();
            buttonBatchMode.Text = _isBatchMode ? "☑ 退出批量" : "☑ 批量操作";
            buttonBatchMode.BackColor = _isBatchMode ? Color.FromArgb(63, 81, 181) : Color.FromArgb(240, 240, 245);
            buttonBatchMode.ForeColor = _isBatchMode ? Color.White : Color.FromArgb(60, 60, 60);

            buttonStartReview.Visible = !_isBatchMode;
            labelSelectedCount.Visible = _isBatchMode;
            buttonBatchMastered.Visible = _isBatchMode;
            buttonBatchDelete.Visible = _isBatchMode;

            UpdateSelectedCount();
            listBoxWrongAnswers.Invalidate();

            if (_isBatchMode)
            {
                MessageBox.Show("批量操作模式\n\n请点击列表项选择，然后使用底部按钮进行操作",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateSelectedCount()
        {
            labelSelectedCount.Text = $"已选择 {_selectedIds.Count} 项";
            bool hasSelection = _selectedIds.Count > 0;
            buttonBatchMastered.Enabled = hasSelection;
            buttonBatchDelete.Enabled = hasSelection;
        }

        private void ButtonBatchMastered_Click(object? sender, EventArgs e)
        {
            if (_selectedIds.Count == 0) return;

            var result = MessageBox.Show($"确定要将选中的 {_selectedIds.Count} 道错题标记为已掌握吗？",
                "确认批量操作", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                var itemIds = _selectedIds.ToList();
                _wrongAnswerService.BatchUpdateMastery(_userId, itemIds, MasteryLevel.Mastered);

                MessageBox.Show($"已成功标记 {itemIds.Count} 道错题为已掌握", "操作完成",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                _selectedIds.Clear();
                LoadWrongAnswers();
                LoadCategories();
                UpdateSelectedCount();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "批量标记已掌握失败");
                MessageBox.Show($"批量操作失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonBatchDelete_Click(object? sender, EventArgs e)
        {
            if (_selectedIds.Count == 0) return;

            var result = MessageBox.Show($"确定要删除选中的 {_selectedIds.Count} 道错题吗？\n此操作不可恢复！",
                "确认批量删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                var itemIds = _selectedIds.ToList();
                _wrongAnswerService.BatchRemove(_userId, itemIds);

                MessageBox.Show($"已成功删除 {itemIds.Count} 道错题", "操作完成",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                _selectedIds.Clear();
                LoadWrongAnswers();
                LoadCategories();
                UpdateSelectedCount();
                ClearDetail();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "批量删除失败");
                MessageBox.Show($"批量删除失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonStartReview_Click(object? sender, EventArgs e)
        {
            var itemsToReview = _filteredItems.Where(i => !i.IsMastered).ToList();
            if (itemsToReview.Count == 0)
            {
                MessageBox.Show("没有需要复习的错题", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            StartReviewMode(itemsToReview);
        }

        private void StartReviewMode(List<WrongAnswerItem> items)
        {
            if (items.Count == 0) return;

            int currentIndex = 0;
            bool showAnswer = false;

            Form reviewForm = new Form
            {
                Text = $"📕 错题复习  1/{items.Count}",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(245, 245, 250),
                Font = new Font("微软雅黑", 10F),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Panel panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            Label labelSubject = new Label
            {
                Text = $"【{items[0].Subject}】",
                Location = new Point(30, 25),
                AutoSize = true,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            };

            Label labelQuestion = new Label
            {
                Text = items[0].Question,
                Location = new Point(30, 55),
                Size = new Size(440, 100),
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33)
            };

            Label labelUserAnswer = new Label
            {
                Text = $"❌ 你的答案: {items[0].UserAnswer}",
                Location = new Point(30, 165),
                AutoSize = true,
                Font = new Font("微软雅黑", 10F),
                ForeColor = Color.FromArgb(244, 67, 54)
            };

            Button buttonShowAnswer = new Button
            {
                Text = "显示答案 ▼",
                Location = new Point(30, 210),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(63, 81, 181),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold)
            };
            buttonShowAnswer.FlatAppearance.BorderSize = 0;

            Panel panelAnswer = new Panel
            {
                Location = new Point(30, 255),
                Size = new Size(440, 40),
                Visible = false
            };

            Label labelCorrect = new Label
            {
                Text = $"✅ 正确答案: {items[0].CorrectAnswer}",
                AutoSize = true,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80),
                Location = new Point(0, 0)
            };

            Label labelExplanation = new Label
            {
                Text = $"📝 解析: {items[0].Explanation}",
                AutoSize = true,
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(0, 25)
            };

            panelAnswer.Controls.Add(labelCorrect);
            panelAnswer.Controls.Add(labelExplanation);

            Panel panelBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 248, 252)
            };

            Button buttonRemembered = new Button
            {
                Text = "记住了 ✅",
                Location = new Point(80, 12),
                Size = new Size(140, 36),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold)
            };
            buttonRemembered.FlatAppearance.BorderSize = 0;

            Button buttonNotYet = new Button
            {
                Text = "还不会 ❌",
                Location = new Point(270, 12),
                Size = new Size(140, 36),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold)
            };
            buttonNotYet.FlatAppearance.BorderSize = 0;

            panelBottom.Controls.Add(buttonRemembered);
            panelBottom.Controls.Add(buttonNotYet);

            panelContent.Controls.Add(labelSubject);
            panelContent.Controls.Add(labelQuestion);
            panelContent.Controls.Add(labelUserAnswer);
            panelContent.Controls.Add(buttonShowAnswer);
            panelContent.Controls.Add(panelAnswer);

            reviewForm.Controls.Add(panelContent);
            reviewForm.Controls.Add(panelBottom);

            buttonShowAnswer.Click += (s, e) =>
            {
                showAnswer = !showAnswer;
                panelAnswer.Visible = showAnswer;
                buttonShowAnswer.Text = showAnswer ? "收起答案 ▲" : "显示答案 ▼";
                if (showAnswer)
                {
                    panelAnswer.Height = 80;
                }
            };

            void UpdateCard(int idx)
            {
                if (idx < 0 || idx >= items.Count) return;
                reviewForm.Text = $"📕 错题复习  {idx + 1}/{items.Count}";
                labelSubject.Text = $"【{items[idx].Subject}】";
                labelQuestion.Text = items[idx].Question;
                labelUserAnswer.Text = $"❌ 你的答案: {items[idx].UserAnswer}";
                labelCorrect.Text = $"✅ 正确答案: {items[idx].CorrectAnswer}";
                labelExplanation.Text = $"📝 解析: {items[idx].Explanation}";
                showAnswer = false;
                panelAnswer.Visible = false;
                buttonShowAnswer.Text = "显示答案 ▼";
            }

            buttonRemembered.Click += (s, e) =>
            {
                try
                {
                    _wrongAnswerService.MarkAsMastered(_userId, items[currentIndex].Id);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "复习模式标记已掌握失败");
                }

                currentIndex++;
                if (currentIndex >= items.Count)
                {
                    MessageBox.Show("复习完成！太棒了！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    reviewForm.Close();
                    LoadWrongAnswers();
                }
                else
                {
                    UpdateCard(currentIndex);
                }
            };

            buttonNotYet.Click += (s, e) =>
            {
                currentIndex++;
                if (currentIndex >= items.Count)
                {
                    MessageBox.Show("复习完成！继续加油！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    reviewForm.Close();
                    LoadWrongAnswers();
                }
                else
                {
                    UpdateCard(currentIndex);
                }
            };

            _reviewForm = reviewForm;
            reviewForm.ShowDialog(this);
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;

            if (panelSidebar != null)
                panelSidebar.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.White;
            if (panelMain != null)
                panelMain.BackColor = colors.Background;
            if (panelStatsBar != null)
                panelStatsBar.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.White;
            if (panelDetail != null)
                panelDetail.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.White;
            if (panelBottomActions != null)
                panelBottomActions.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Background : Color.FromArgb(248, 248, 252);

            if (labelSearchTitle != null) labelSearchTitle.ForeColor = colors.TextPrimary;
            if (labelCategoryTitle != null) labelCategoryTitle.ForeColor = colors.TextPrimary;
            if (labelStatusTitle != null) labelStatusTitle.ForeColor = colors.TextPrimary;

            if (textBoxSearch != null)
            {
                textBoxSearch.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Background : Color.White;
                textBoxSearch.ForeColor = colors.TextPrimary;
            }

            if (listBoxCategories != null)
            {
                listBoxCategories.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.White;
                listBoxCategories.ForeColor = colors.TextPrimary;
            }

            if (radioStatusAll != null) radioStatusAll.ForeColor = colors.TextPrimary;
            if (radioStatusReview != null) radioStatusReview.ForeColor = colors.TextPrimary;
            if (radioStatusMastered != null) radioStatusMastered.ForeColor = colors.TextPrimary;

            if (listBoxWrongAnswers != null)
            {
                listBoxWrongAnswers.BackColor = colors.Background;
                listBoxWrongAnswers.ForeColor = colors.TextPrimary;
            }

            if (labelStatTotal != null) labelStatTotal.ForeColor = colors.TextPrimary;

            if (textBoxQuestion != null)
            {
                textBoxQuestion.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Background : Color.FromArgb(250, 250, 252);
                textBoxQuestion.ForeColor = colors.TextPrimary;
            }
            if (textBoxCorrectAnswer != null)
            {
                textBoxCorrectAnswer.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Background : Color.FromArgb(250, 250, 252);
            }
            if (textBoxUserAnswer != null)
            {
                textBoxUserAnswer.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Background : Color.FromArgb(250, 250, 252);
            }
            if (textBoxExplanation != null)
            {
                textBoxExplanation.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Background : Color.FromArgb(250, 250, 252);
                textBoxExplanation.ForeColor = colors.TextPrimary;
            }

            if (labelQuestion != null) labelQuestion.ForeColor = colors.TextPrimary;
            if (labelExplanation != null) labelExplanation.ForeColor = colors.TextPrimary;
            if (labelDetailStats != null) labelDetailStats.ForeColor = colors.TextSecondary;
        }

        #region IDisposable Support
        private bool _disposed = false;

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // 注销主题服务
                _themeService?.UnregisterThemeable(this);

                // 注销事件订阅
                if (textBoxSearch != null) textBoxSearch.TextChanged -= TextBoxSearch_TextChanged;
                if (listBoxCategories != null) listBoxCategories.SelectedIndexChanged -= ListBoxCategories_SelectedIndexChanged;
                if (radioStatusAll != null) radioStatusAll.CheckedChanged -= RadioStatus_CheckedChanged;
                if (radioStatusReview != null) radioStatusReview.CheckedChanged -= RadioStatus_CheckedChanged;
                if (radioStatusMastered != null) radioStatusMastered.CheckedChanged -= RadioStatus_CheckedChanged;
                if (buttonBatchMode != null) buttonBatchMode.Click -= ButtonBatchMode_Click;
                if (buttonStartReview != null) buttonStartReview.Click -= ButtonStartReview_Click;
                if (buttonBatchMastered != null) buttonBatchMastered.Click -= ButtonBatchMastered_Click;
                if (buttonBatchDelete != null) buttonBatchDelete.Click -= ButtonBatchDelete_Click;
                if (listBoxWrongAnswers != null)
                {
                    listBoxWrongAnswers.DrawItem -= ListBoxWrongAnswers_DrawItem;
                    listBoxWrongAnswers.SelectedIndexChanged -= ListBoxWrongAnswers_SelectedIndexChanged;
                }
                if (buttonMarkMastered != null) buttonMarkMastered.Click -= ButtonMarkMastered_Click;
                if (buttonDelete != null) buttonDelete.Click -= ButtonDelete_Click;
                if (buttonExport != null) buttonExport.Click -= ButtonExport_Click;
                if (buttonClose != null) buttonClose.Click -= ButtonClose_Click;
                Resize -= WrongAnswerForm_Resize;

                // 关闭复习窗体
                if (_reviewForm != null && !_reviewForm.IsDisposed)
                {
                    _reviewForm.Close();
                    _reviewForm.Dispose();
                    _reviewForm = null;
                }

                // 释放组件
                components?.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }
        #endregion

    }
}
