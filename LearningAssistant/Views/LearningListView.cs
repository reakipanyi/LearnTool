using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LearningAssistant.Views
{
    /// <summary>
    /// 学习列表视图 - 左侧学习项列表
    /// </summary>
    public class LearningListView : UserControl
    {
        #region Controls

        private Panel _panelList = null!;
        private Label _labelListStatus = null!;
        private Label _labelListTitle = null!;
        private ListBox _listBoxItems = null!;

        #endregion

        #region Public Controls

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Panel PanelList => _panelList;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ListBox ListBoxItems => _listBoxItems;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelListStatus => _labelListStatus;

        #endregion

        #region Events

        /// <summary>列表选中项变更事件</summary>
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
            _panelList.Size = new Size(260, 838);
            _panelList.TabIndex = 18;

            // 
            // _labelListStatus
            // 
            _labelListStatus.BackColor = Color.FromArgb(240, 240, 245);
            _labelListStatus.Dock = DockStyle.Bottom;
            _labelListStatus.Font = new Font("微软雅黑", 9F);
            _labelListStatus.ForeColor = Color.FromArgb(80, 100, 120);
            _labelListStatus.Location = new Point(0, 796);
            _labelListStatus.Name = "_labelListStatus";
            _labelListStatus.Size = new Size(258, 40);
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
            _labelListTitle.Size = new Size(258, 35);
            _labelListTitle.TabIndex = 0;
            _labelListTitle.Text = "📚 学习列表";
            _labelListTitle.TextAlign = ContentAlignment.MiddleCenter;

            // 
            // _listBoxItems
            // 
            _listBoxItems.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            _listBoxItems.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _listBoxItems.FormattingEnabled = true;
            _listBoxItems.ItemHeight = 22;
            _listBoxItems.Location = new Point(0, 35);
            _listBoxItems.Name = "_listBoxItems";
            _listBoxItems.Size = new Size(259, 764);
            _listBoxItems.TabIndex = 1;
            _listBoxItems.SelectedIndexChanged += (s, e) => SelectedIndexChanged?.Invoke(this, e);

            Controls.Add(_panelList);

            ResumeLayout(false);
        }

        #endregion

        #region Public API

        /// <summary>应用主题色</summary>
        public void ApplyTheme(Color headerColor, Color itemBackColor, Color itemForeColor)
        {
            _panelList.BackColor = itemBackColor;
            _labelListTitle.BackColor = headerColor;
            _listBoxItems.BackColor = itemBackColor;
            _listBoxItems.ForeColor = itemForeColor;
        }

        /// <summary>设置列表标题</summary>
        public void SetTitle(string title) => _labelListTitle.Text = title;

        /// <summary>设置列表项</summary>
        public void SetItems(List<string> items, int selectedIndex = -1)
        {
            _listBoxItems.Items.Clear();
            _listBoxItems.Items.AddRange(items.ToArray());
            if (selectedIndex >= 0 && selectedIndex < _listBoxItems.Items.Count)
                _listBoxItems.SelectedIndex = selectedIndex;
        }

        /// <summary>设置选中项</summary>
        public void SetSelectedIndex(int index)
        {
            if (index >= 0 && index < _listBoxItems.Items.Count)
            {
                _listBoxItems.SelectedIndexChanged -= OnSelectedIndexChanged;
                _listBoxItems.SelectedIndex = index;
                _listBoxItems.SelectedIndexChanged += OnSelectedIndexChanged;
            }
        }

        private void OnSelectedIndexChanged(object? sender, EventArgs e)
        {
            SelectedIndexChanged?.Invoke(this, e);
        }

        /// <summary>获取当前选中索引</summary>
        public int SelectedIndex => _listBoxItems.SelectedIndex;

        /// <summary>设置状态文本（如"共 N 项"）</summary>
        public void SetStatusText(string text) => _labelListStatus.Text = text;

        #endregion
    }
}
