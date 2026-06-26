using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls
{
    /// <summary>
    /// 空状态类型预设
    /// 每个空状态都回答三个问题：是什么、为什么空、怎么做
    /// </summary>
    public enum EmptyStateType
    {
        /// <summary>
        /// 自定义
        /// </summary>
        Custom,

        /// <summary>
        /// 无数据（通用）
        /// </summary>
        NoData,

        /// <summary>
        /// 搜索无结果
        /// </summary>
        NoSearchResult,

        /// <summary>
        /// 无网络
        /// </summary>
        NoNetwork,

        /// <summary>
        /// 无收藏
        /// </summary>
        NoFavorites,

        /// <summary>
        /// 无学习内容
        /// </summary>
        NoLearningContent,

        /// <summary>
        /// 无历史记录
        /// </summary>
        NoHistory,

        /// <summary>
        /// 无通知
        /// </summary>
        NoNotifications,

        /// <summary>
        /// 加载失败
        /// </summary>
        LoadError,

        /// <summary>
        /// 空文件夹
        /// </summary>
        EmptyFolder,

        /// <summary>
        /// 无成就
        /// </summary>
        NoAchievements,

        /// <summary>
        /// 无笔记
        /// </summary>
        NoNotes,

        /// <summary>
        /// 无错题
        /// </summary>
        NoWrongAnswers,

        /// <summary>
        /// 无挑战
        /// </summary>
        NoChallenges,

        /// <summary>
        /// 无学习记录
        /// </summary>
        NoLearningRecords,

        /// <summary>
        /// 空收藏夹
        /// </summary>
        EmptyFavoritesFolder,

        /// <summary>
        /// 权限不足
        /// </summary>
        NoPermission,

        /// <summary>
        /// 复习已完成（今日复习已清空）- UI/UX优化规范
        /// </summary>
        ReviewCompleted,

        /// <summary>
        /// 无待复习内容 - UI/UX优化规范
        /// </summary>
        NoReviewDue
    }

    public class EmptyStateView : UserControl
    {
        private Label _labelIcon = null!;
        private Label _labelTitle = null!;
        private Label _labelDescription = null!;
        private Button _buttonAction = null!;
        private PictureBox? _pictureIcon;

        private string _icon = "📭";
        private string _title = "暂无数据";
        private string _description = "";
        private string _actionText = "";
        private int _iconSize = 64;
        private EmptyStateType _stateType = EmptyStateType.Custom;
        private Image? _customImage;
        private bool _useImageIcon;

        [Category("Appearance")]
        [DefaultValue("📭")]
        public string Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                if (_labelIcon != null)
                    _labelIcon.Text = value;
            }
        }

        [Category("Appearance")]
        [DefaultValue("暂无数据")]
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                if (_labelTitle != null)
                    _labelTitle.Text = value;
            }
        }

        [Category("Appearance")]
        [DefaultValue("")]
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                if (_labelDescription != null)
                {
                    _labelDescription.Text = value;
                    _labelDescription.Visible = !string.IsNullOrEmpty(value);
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue("")]
        public string ActionText
        {
            get => _actionText;
            set
            {
                _actionText = value;
                if (_buttonAction != null)
                {
                    _buttonAction.Text = value;
                    _buttonAction.Visible = !string.IsNullOrEmpty(value);
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(64)]
        public int IconSize
        {
            get => _iconSize;
            set
            {
                _iconSize = value;
                if (_labelIcon != null)
                {
                    _labelIcon.Font = new Font("Segoe UI Emoji", _iconSize / 2.5f);
                    _labelIcon.Height = _iconSize + 10;
                }
                if (_pictureIcon != null)
                {
                    _pictureIcon.Size = new Size(_iconSize, _iconSize);
                    _pictureIcon.Height = _iconSize + 10;
                }
                LayoutControls();
            }
        }

        [Category("Appearance")]
        [DefaultValue(EmptyStateType.Custom)]
        public EmptyStateType StateType
        {
            get => _stateType;
            set
            {
                _stateType = value;
                ApplyPreset();
            }
        }

        [Category("Appearance")]
        [DefaultValue(null)]
        public Image? CustomImage
        {
            get => _customImage;
            set
            {
                _customImage = value;
                if (_pictureIcon != null)
                {
                    _pictureIcon.Image = value;
                    _useImageIcon = value != null;
                    UpdateIconDisplay();
                }
            }
        }

        [Category("Action")]
        public event EventHandler? ActionClicked;

        public EmptyStateView()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.ResizeRedraw,
                true);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _labelIcon = new Label();
            _labelTitle = new Label();
            _labelDescription = new Label();
            _buttonAction = new Button();
            _pictureIcon = new PictureBox();

            SuspendLayout();

            _pictureIcon.SizeMode = PictureBoxSizeMode.Zoom;
            _pictureIcon.BackColor = Color.Transparent;
            _pictureIcon.Size = new Size(_iconSize, _iconSize + 10);
            _pictureIcon.Visible = false;

            _labelIcon.Dock = DockStyle.Top;
            _labelIcon.Font = new Font("Segoe UI Emoji", 26F);
            _labelIcon.TextAlign = ContentAlignment.MiddleCenter;
            _labelIcon.Text = _icon;
            _labelIcon.Height = 70;
            _labelIcon.BackColor = Color.Transparent;

            _labelTitle.Dock = DockStyle.Top;
            _labelTitle.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            _labelTitle.ForeColor = Color.FromArgb(102, 102, 102);
            _labelTitle.TextAlign = ContentAlignment.MiddleCenter;
            _labelTitle.Text = _title;
            _labelTitle.Height = 28;
            _labelTitle.BackColor = Color.Transparent;

            _labelDescription.Dock = DockStyle.Top;
            _labelDescription.Font = new Font("微软雅黑", 9F);
            _labelDescription.ForeColor = Color.FromArgb(153, 153, 153);
            _labelDescription.TextAlign = ContentAlignment.MiddleCenter;
            _labelDescription.Text = _description;
            _labelDescription.Height = 22;
            _labelDescription.Visible = false;
            _labelDescription.BackColor = Color.Transparent;

            _buttonAction.Text = _actionText;
            _buttonAction.Visible = false;
            _buttonAction.FlatStyle = FlatStyle.Flat;
            _buttonAction.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _buttonAction.ForeColor = Color.White;
            _buttonAction.BackColor = Color.FromArgb(33, 150, 243);
            _buttonAction.Cursor = Cursors.Hand;
            _buttonAction.Size = new Size(120, 32);
            _buttonAction.FlatAppearance.BorderSize = 0;
            _buttonAction.Click += ButtonAction_Click;

            Controls.Add(_buttonAction);
            Controls.Add(_labelDescription);
            Controls.Add(_labelTitle);
            Controls.Add(_pictureIcon);
            Controls.Add(_labelIcon);

            BackColor = Color.Transparent;
            Size = new Size(300, 180);
            DoubleBuffered = true;

            ResumeLayout(false);
        }

        private void ButtonAction_Click(object? sender, EventArgs e)
        {
            ActionClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ApplyPreset()
        {
            switch (_stateType)
            {
                case EmptyStateType.NoData:
                    _icon = "📭";
                    _title = "暂无数据";
                    _description = "还没有任何数据，添加一些内容开始吧";
                    _actionText = "";
                    break;

                case EmptyStateType.NoSearchResult:
                    _icon = "🔍";
                    _title = "没有找到相关内容";
                    _description = "试试其他关键词，或检查拼写是否正确";
                    _actionText = "清除筛选";
                    break;

                case EmptyStateType.NoNetwork:
                    _icon = "📡";
                    _title = "网络连接失败";
                    _description = "请检查网络连接后重试";
                    _actionText = "重试";
                    break;

                case EmptyStateType.NoFavorites:
                    _icon = "⭐";
                    _title = "还没有收藏";
                    _description = "遇到重要的知识点，点击收藏按钮保存下来吧";
                    _actionText = "去学习";
                    break;

                case EmptyStateType.NoLearningContent:
                    _icon = "📚";
                    _title = "还没有学习内容";
                    _description = "导入或创建学习内容，开始你的学习之旅";
                    _actionText = "添加内容";
                    break;

                case EmptyStateType.NoHistory:
                    _icon = "📜";
                    _title = "暂无历史记录";
                    _description = "浏览记录会显示在这里，方便你快速回顾";
                    _actionText = "";
                    break;

                case EmptyStateType.NoNotifications:
                    _icon = "🔔";
                    _title = "暂无通知";
                    _description = "有新消息时会在这里第一时间提醒你";
                    _actionText = "";
                    break;

                case EmptyStateType.LoadError:
                    _icon = "⚠️";
                    _title = "加载失败";
                    _description = "数据加载失败，请检查网络后重试";
                    _actionText = "重新加载";
                    break;

                case EmptyStateType.EmptyFolder:
                    _icon = "📁";
                    _title = "文件夹为空";
                    _description = "这个文件夹里还没有内容，添加一些试试吧";
                    _actionText = "";
                    break;

                case EmptyStateType.NoAchievements:
                    _icon = "🌱";
                    _title = "成就等待解锁";
                    _description = "坚持学习，解锁更多成就徽章，见证你的成长";
                    _actionText = "查看成就列表";
                    break;

                case EmptyStateType.NoNotes:
                    _icon = "📝";
                    _title = "暂无笔记";
                    _description = "在学习过程中，可以随时记录你的想法和心得";
                    _actionText = "开始学习";
                    break;

                case EmptyStateType.NoWrongAnswers:
                    _icon = "✅";
                    _title = "还没有错题";
                    _description = "太棒了！你还没有答错的题目，继续保持";
                    _actionText = "随便练练";
                    break;

                case EmptyStateType.NoChallenges:
                    _icon = "🎯";
                    _title = "今日挑战还未开始";
                    _description = "完成每日挑战可以获得额外的 XP 和分数奖励";
                    _actionText = "开始挑战";
                    break;

                case EmptyStateType.NoLearningRecords:
                    _icon = "📊";
                    _title = "还没有学习记录";
                    _description = "开始你的第一次学习吧，数据会在这里展示";
                    _actionText = "开始学习";
                    break;

                case EmptyStateType.EmptyFavoritesFolder:
                    _icon = "🗂️";
                    _title = "文件夹为空";
                    _description = "把相关的收藏移动到这里，让分类更清晰";
                    _actionText = "移动收藏";
                    break;

                case EmptyStateType.NoPermission:
                    _icon = "🔒";
                    _title = "权限不足";
                    _description = "你没有权限访问此内容，请联系管理员获取权限";
                    _actionText = "";
                    break;

                case EmptyStateType.ReviewCompleted:
                    _icon = "🎉";
                    _title = "今日复习已清空！";
                    _description = "太棒了！你已完成所有到期的复习项。学习新内容将自动填充明天的复习队列。";
                    _actionText = "立即学习新内容 →";
                    break;

                case EmptyStateType.NoReviewDue:
                    _icon = "📚";
                    _title = "暂无待复习内容";
                    _description = "开始学习新内容，系统会自动安排复习时间。";
                    _actionText = "去学习 →";
                    break;

                case EmptyStateType.Custom:
                default:
                    break;
            }

            if (_labelIcon != null) _labelIcon.Text = _icon;
            if (_labelTitle != null) _labelTitle.Text = _title;
            if (_labelDescription != null)
            {
                _labelDescription.Text = _description;
                _labelDescription.Visible = !string.IsNullOrEmpty(_description);
            }
            if (_buttonAction != null)
            {
                _buttonAction.Text = _actionText;
                _buttonAction.Visible = !string.IsNullOrEmpty(_actionText);
            }

            _useImageIcon = false;
            UpdateIconDisplay();
            LayoutControls();
        }

        private void UpdateIconDisplay()
        {
            if (_labelIcon == null || _pictureIcon == null) return;

            _labelIcon.Visible = !_useImageIcon;
            _pictureIcon.Visible = _useImageIcon;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutControls();
        }

        private void LayoutControls()
        {
            if (_labelIcon == null || _labelTitle == null || _labelDescription == null || _buttonAction == null)
                return;

            int iconHeight = _useImageIcon && _pictureIcon != null
                ? _pictureIcon.Height
                : _labelIcon.Height;

            int totalHeight = iconHeight + _labelTitle.Height;

            if (_labelDescription.Visible)
                totalHeight += _labelDescription.Height;

            if (_buttonAction.Visible)
                totalHeight += _buttonAction.Height + 15;

            int startY = (Height - totalHeight) / 2;

            if (_useImageIcon && _pictureIcon != null)
            {
                _pictureIcon.SetBounds((Width - _iconSize) / 2, startY, _iconSize, _iconSize);
            }
            else
            {
                _labelIcon.SetBounds(0, startY, Width, _labelIcon.Height);
            }

            int titleY = startY + iconHeight;
            _labelTitle.SetBounds(0, titleY, Width, _labelTitle.Height);

            if (_labelDescription.Visible)
            {
                _labelDescription.SetBounds(0, _labelTitle.Bottom, Width, _labelDescription.Height);
            }

            if (_buttonAction.Visible)
            {
                int buttonY = _labelDescription.Visible
                    ? _labelDescription.Bottom + 15
                    : _labelTitle.Bottom + 15;
                _buttonAction.Location = new Point((Width - _buttonAction.Width) / 2, buttonY);
            }
        }

        public void SetState(EmptyStateType type, string? customTitle = null, string? customDescription = null, string? actionText = null)
        {
            _stateType = type;
            ApplyPreset();

            if (!string.IsNullOrEmpty(customTitle))
                Title = customTitle;

            if (!string.IsNullOrEmpty(customDescription))
                Description = customDescription;

            if (actionText != null)
                ActionText = actionText;
        }

        public void ShowError(string message, string actionText = "重试", EventHandler? action = null)
        {
            SetState(EmptyStateType.LoadError, customDescription: message, actionText: actionText);

            if (action != null)
            {
                ActionClicked -= action;
                ActionClicked += action;
            }
        }

        public void ShowNoData(string description = "", string actionText = "")
        {
            SetState(EmptyStateType.NoData, customDescription: description, actionText: actionText);
        }

        public void ShowNoSearchResult(string keyword = "")
        {
            string desc = string.IsNullOrEmpty(keyword)
                ? "试试其他关键词，或检查拼写是否正确"
                : $"未找到与 \"{keyword}\" 相关的结果";
            SetState(EmptyStateType.NoSearchResult, customDescription: desc);
        }

        public void ShowNoFavorites(EventHandler? action = null)
        {
            SetState(EmptyStateType.NoFavorites);
            if (action != null)
            {
                ActionClicked -= action;
                ActionClicked += action;
            }
        }

        public void ShowNoAchievements(EventHandler? action = null)
        {
            SetState(EmptyStateType.NoAchievements);
            if (action != null)
            {
                ActionClicked -= action;
                ActionClicked += action;
            }
        }

        public void ShowNoNotes(EventHandler? action = null)
        {
            SetState(EmptyStateType.NoNotes);
            if (action != null)
            {
                ActionClicked -= action;
                ActionClicked += action;
            }
        }

        public void ShowNoWrongAnswers(EventHandler? action = null)
        {
            SetState(EmptyStateType.NoWrongAnswers);
            if (action != null)
            {
                ActionClicked -= action;
                ActionClicked += action;
            }
        }

        public void ShowNoChallenges(EventHandler? action = null)
        {
            SetState(EmptyStateType.NoChallenges);
            if (action != null)
            {
                ActionClicked -= action;
                ActionClicked += action;
            }
        }

        public void ShowNoLearningRecords(EventHandler? action = null)
        {
            SetState(EmptyStateType.NoLearningRecords);
            if (action != null)
            {
                ActionClicked -= action;
                ActionClicked += action;
            }
        }

        public void ShowEmptyFolder(string? customDescription = null)
        {
            SetState(EmptyStateType.EmptyFolder, customDescription: customDescription);
        }

        public void ShowNoPermission()
        {
            SetState(EmptyStateType.NoPermission);
        }

        /// <summary>
        /// 显示复习已完成空状态（UI/UX优化规范）
        /// </summary>
        public void ShowReviewCompleted(EventHandler? action = null)
        {
            SetState(EmptyStateType.ReviewCompleted);
            if (action != null)
            {
                ActionClicked -= action;
                ActionClicked += action;
            }
        }

        /// <summary>
        /// 显示暂无待复习空状态（UI/UX优化规范）
        /// </summary>
        public void ShowNoReviewDue(EventHandler? action = null)
        {
            SetState(EmptyStateType.NoReviewDue);
            if (action != null)
            {
                ActionClicked -= action;
                ActionClicked += action;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
        }
    }
}
