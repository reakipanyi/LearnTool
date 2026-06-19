using System.ComponentModel;

namespace LearningAssistant.Views
{
    /// <summary>
    /// 学习列表视图 - 左侧学习项列表
    /// </summary>
    public class LearningListView : UserControl
    {
        #region Controls

        private Panel _panelList = null!;
        private Label _labelListTitle = null!;
        private Label _labelListStatus = null!;
        private ListBox _listBoxItems = null!;

        #endregion

        #region Public Properties

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Panel PanelList => _panelList;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ListBox ListBoxItems => _listBoxItems;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelListTitle => _labelListTitle;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelListStatus => _labelListStatus;

        #endregion

        #region Events

        /// <summary>
        /// 列表选中项变更事件
        /// </summary>
        public event EventHandler? SelectedIndexChanged;

        #endregion

        #region Initialization

        public LearningListView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _panelList = new Panel();
            _labelListStatus = new Label();
            _labelListTitle = new Label();
            _listBoxItems = new ListBox();
            _panelList.SuspendLayout();
            SuspendLayout();
            // 
            // _panelList
            // 
            _panelList.BackColor = Color.FromArgb(248, 248, 252);
            _panelList.BorderStyle = BorderStyle.FixedSingle;
            _panelList.Controls.Add(_labelListStatus);
            _panelList.Controls.Add(_labelListTitle);
            _panelList.Controls.Add(_listBoxItems);
            _panelList.Dock = DockStyle.Fill;
            _panelList.Location = new Point(0, 0);
            _panelList.Name = "_panelList";
            _panelList.Size = new Size(260, 981);
            _panelList.TabIndex = 0;
            // 
            // _labelListStatus
            // 
            _labelListStatus.BackColor = Color.FromArgb(240, 240, 245);
            _labelListStatus.Dock = DockStyle.Bottom;
            _labelListStatus.Font = new Font("微软雅黑", 9F);
            _labelListStatus.ForeColor = Color.FromArgb(80, 100, 120);
            _labelListStatus.Location = new Point(0, 934);
            _labelListStatus.Name = "_labelListStatus";
            _labelListStatus.Size = new Size(258, 45);
            _labelListStatus.TabIndex = 2;
            _labelListStatus.Text = "共 0 项";
            _labelListStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _labelListTitle
            // 
            _labelListTitle.BackColor = Color.FromArgb(66, 133, 244);
            _labelListTitle.Dock = DockStyle.Top;
            _labelListTitle.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _labelListTitle.ForeColor = Color.White;
            _labelListTitle.Location = new Point(0, 0);
            _labelListTitle.Name = "_labelListTitle";
            _labelListTitle.Size = new Size(258, 40);
            _labelListTitle.TabIndex = 0;
            _labelListTitle.Text = "📚 学习列表";
            _labelListTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _listBoxItems
            // 
            _listBoxItems.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            _listBoxItems.DrawMode = DrawMode.OwnerDrawFixed;
            _listBoxItems.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _listBoxItems.FormattingEnabled = true;
            _listBoxItems.ItemHeight = 36;
            _listBoxItems.Location = new Point(0, 40);
            _listBoxItems.Name = "_listBoxItems";
            _listBoxItems.Size = new Size(259, 904);
            _listBoxItems.TabIndex = 1;
            _listBoxItems.SelectedIndexChanged += ListBoxItems_SelectedIndexChanged;
            // 
            // LearningListView
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(_panelList);
            Name = "LearningListView";
            Size = new Size(260, 981);
            _panelList.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void ListBoxItems_SelectedIndexChanged(object? sender, EventArgs e) => SelectedIndexChanged?.Invoke(sender, e);

        #endregion

        #region Public Methods

        /// <summary>
        /// 设置列表项
        /// </summary>
        public void SetItems(List<string> items)
        {
            _listBoxItems.Items.Clear();
            foreach (var item in items)
            {
                _listBoxItems.Items.Add(item);
            }
        }

        /// <summary>
        /// 设置选中索引
        /// </summary>
        public void SetSelectedIndex(int index)
        {
            if (index >= 0 && index < _listBoxItems.Items.Count)
            {
                _listBoxItems.SelectedIndex = index;
            }
        }

        /// <summary>
        /// 设置状态文本
        /// </summary>
        public void SetStatusText(string text)
        {
            _labelListStatus.Text = text;
        }

        #endregion
    }
}