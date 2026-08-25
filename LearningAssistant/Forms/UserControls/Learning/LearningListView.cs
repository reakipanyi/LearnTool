using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls.Learning
{
    /// <summary>
    /// 学习列表视图 - 左侧学习项列表
    /// </summary>
    public class LearningListView : UserControl, IThemeable
    {
        #region Controls

        private Panel _panelList = null!;
        private Label _labelListTitle = null!;
        private Label _labelListStatus = null!;
        private ListBox _listBoxItems = null!;
        private Panel _panelSearch = null!;
        private TextBox _textBoxSearch = null!;
        private Button _buttonFilterFavorites = null!;
        private Label _labelSearchIcon = null!;
        private ToolTip _toolTip = null!;

        #endregion

        #region Fields

        private List<string> _allItems = new();
        private string _searchKeyword = string.Empty;
        private bool _showFavoritesOnly = false;
        private HashSet<string> _favoriteItems = new();
        private bool _isUpdatingItems = false;
        private ThemeColors _themeColors = ThemeService.GetColors(ThemeMode.Light);

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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TextBox TextBoxSearch => _textBoxSearch;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndexInAllItems
        {
            get
            {
                if (_listBoxItems.SelectedItem == null) return -1;
                string selectedText = _listBoxItems.SelectedItem.ToString() ?? string.Empty;
                return _allItems.IndexOf(selectedText);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// 列表选中项变更事件
        /// </summary>
        public event EventHandler? SelectedIndexChanged;

        /// <summary>
        /// 搜索文本变更事件
        /// </summary>
        public event EventHandler? SearchTextChanged;

        #endregion

        #region Initialization

        public LearningListView()
        {
            InitializeComponent();
            InitPlaceholder();
            InitToolTips();
        }

        private void InitPlaceholder()
        {
            _textBoxSearch.Text = "搜索...";
            _textBoxSearch.ForeColor = Color.Gray;
        }

        private void InitToolTips()
        {
            _toolTip.SetToolTip(_buttonFilterFavorites, "只显示收藏");
        }

        private void InitializeComponent()
        {
            _panelList = new Panel();
            _labelListStatus = new Label();
            _listBoxItems = new ListBox();
            _panelSearch = new Panel();
            _textBoxSearch = new TextBox();
            _buttonFilterFavorites = new Button();
            _labelSearchIcon = new Label();
            _toolTip = new ToolTip();
            _labelListTitle = new Label();
            _panelList.SuspendLayout();
            _panelSearch.SuspendLayout();
            SuspendLayout();
            // 
            // _panelList
            // 
            _panelList.BackColor = Color.FromArgb(248, 248, 252);
            _panelList.BorderStyle = BorderStyle.FixedSingle;
            _panelList.Controls.Add(_labelListStatus);
            _panelList.Controls.Add(_listBoxItems);
            _panelList.Controls.Add(_panelSearch);
            _panelList.Controls.Add(_labelListTitle);
            _panelList.Dock = DockStyle.Fill;
            _panelList.Location = new Point(0, 0);
            _panelList.Name = "_panelList";
            _panelList.Size = new Size(260, 981);
            _panelList.TabIndex = 0;
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
            // _panelSearch
            // 
            _panelSearch.BackColor = Color.FromArgb(240, 240, 245);
            _panelSearch.Dock = DockStyle.Top;
            _panelSearch.Location = new Point(0, 40);
            _panelSearch.Name = "_panelSearch";
            _panelSearch.Padding = new Padding(8, 6, 8, 6);
            _panelSearch.Size = new Size(258, 36);
            _panelSearch.TabIndex = 3;
            // 
            // _textBoxSearch
            // 
            _textBoxSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _textBoxSearch.BorderStyle = BorderStyle.FixedSingle;
            _textBoxSearch.Font = new Font("微软雅黑", 9F);
            _textBoxSearch.Location = new Point(26, 6);
            _textBoxSearch.Name = "_textBoxSearch";
            _textBoxSearch.Size = new Size(186, 23);
            _textBoxSearch.TabIndex = 0;
            _textBoxSearch.TextChanged += TextBoxSearch_TextChanged;
            _textBoxSearch.Enter += TextBoxSearch_Enter;
            _textBoxSearch.Leave += TextBoxSearch_Leave;
            // 
            // _labelSearchIcon
            // 
            _labelSearchIcon.Dock = DockStyle.Left;
            _labelSearchIcon.Font = new Font("Segoe UI Emoji", 10F);
            _labelSearchIcon.Location = new Point(8, 6);
            _labelSearchIcon.Name = "_labelSearchIcon";
            _labelSearchIcon.Size = new Size(20, 24);
            _labelSearchIcon.TabIndex = 1;
            _labelSearchIcon.Text = "🔍";
            _labelSearchIcon.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _buttonFilterFavorites
            // 
            _buttonFilterFavorites.Dock = DockStyle.Right;
            _buttonFilterFavorites.FlatStyle = FlatStyle.Flat;
            _buttonFilterFavorites.Font = new Font("Segoe UI Emoji", 10F);
            _buttonFilterFavorites.Location = new Point(214, 6);
            _buttonFilterFavorites.Name = "_buttonFilterFavorites";
            _buttonFilterFavorites.Size = new Size(36, 24);
            _buttonFilterFavorites.TabIndex = 2;
            _buttonFilterFavorites.Text = "⭐";
            _buttonFilterFavorites.UseVisualStyleBackColor = true;
            _buttonFilterFavorites.FlatAppearance.BorderSize = 0;
            _buttonFilterFavorites.Click += ButtonFilterFavorites_Click;
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
            // _listBoxItems
            // 
            _listBoxItems.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            _listBoxItems.DrawMode = DrawMode.OwnerDrawFixed;
            _listBoxItems.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _listBoxItems.FormattingEnabled = true;
            _listBoxItems.ItemHeight = 36;
            _listBoxItems.Location = new Point(0, 76);
            _listBoxItems.Name = "_listBoxItems";
            _listBoxItems.Size = new Size(259, 868);
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
            _panelSearch.Controls.Add(_textBoxSearch);
            _panelSearch.Controls.Add(_labelSearchIcon);
            _panelSearch.Controls.Add(_buttonFilterFavorites);
            _panelList.ResumeLayout(false);
            _panelSearch.ResumeLayout(false);
            _panelSearch.PerformLayout();
            ResumeLayout(false);
        }

        private void ListBoxItems_SelectedIndexChanged(object? sender, EventArgs e) => SelectedIndexChanged?.Invoke(sender, e);

        private void TextBoxSearch_Enter(object? sender, EventArgs e)
        {
            if (_textBoxSearch.Text == "搜索...")
            {
                _textBoxSearch.Text = "";
                _textBoxSearch.ForeColor = _themeColors.TextPrimary;
            }
        }

        private void TextBoxSearch_Leave(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_textBoxSearch.Text))
            {
                _textBoxSearch.Text = "搜索...";
                _textBoxSearch.ForeColor = Color.Gray;
            }
        }

        private void TextBoxSearch_TextChanged(object? sender, EventArgs e)
        {
            if (_textBoxSearch.Text == "搜索...")
            {
                _searchKeyword = string.Empty;
            }
            else
            {
                _searchKeyword = _textBoxSearch.Text.Trim();
            }
            UpdateFilteredItems();
            SearchTextChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonFilterFavorites_Click(object? sender, EventArgs e)
        {
            _showFavoritesOnly = !_showFavoritesOnly;
            _buttonFilterFavorites.BackColor = _showFavoritesOnly 
                ? Color.FromArgb(255, 215, 0) 
                : Color.Transparent;
            UpdateFilteredItems();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 应用主题颜色（夜间模式下左侧列表背景与文字随主题切换）
        /// </summary>
        public void ApplyTheme(ThemeColors colors)
        {
            _themeColors = colors;
            _panelList.BackColor = colors.Surface;
            _listBoxItems.BackColor = colors.Surface;
            _listBoxItems.ForeColor = colors.TextPrimary;
            _panelSearch.BackColor = colors.ThemeMode == ThemeMode.Dark
                ? Color.FromArgb(42, 42, 42)
                : Color.FromArgb(240, 240, 245);
            _textBoxSearch.BackColor = colors.SurfaceElevated;
            _textBoxSearch.ForeColor = colors.TextPrimary;
            _labelSearchIcon.ForeColor = colors.TextSecondary;
            _labelListStatus.BackColor = colors.ThemeMode == ThemeMode.Dark
                ? Color.FromArgb(42, 42, 42)
                : Color.FromArgb(240, 240, 245);
            _labelListStatus.ForeColor = colors.TextSecondary;
            if (_textBoxSearch.Text == "搜索...")
            {
                _textBoxSearch.ForeColor = Color.Gray;
            }
            _listBoxItems.Invalidate();
        }

        /// <summary>
        /// 设置列表项
        /// </summary>
        public void SetItems(List<string> items)
        {
            _allItems = new List<string>(items);
            UpdateFilteredItems();
        }

        /// <summary>
        /// 设置收藏项列表
        /// </summary>
        public void SetFavoriteItems(HashSet<string> favorites)
        {
            _favoriteItems = new HashSet<string>(favorites);
            UpdateFilteredItems();
        }

        /// <summary>
        /// 设置选中索引（基于筛选后的列表）
        /// </summary>
        public void SetSelectedIndex(int index)
        {
            if (index >= 0 && index < _listBoxItems.Items.Count)
            {
                _listBoxItems.SelectedIndex = index;
            }
        }

        /// <summary>
        /// 设置选中索引（基于完整列表）
        /// </summary>
        public void SetSelectedIndexFromFullList(int fullIndex)
        {
            if (fullIndex >= 0 && fullIndex < _allItems.Count)
            {
                string itemText = _allItems[fullIndex];
                int filteredIndex = _listBoxItems.Items.IndexOf(itemText);
                if (filteredIndex >= 0)
                {
                    _listBoxItems.SelectedIndex = filteredIndex;
                    _listBoxItems.TopIndex = Math.Max(0, filteredIndex - 5);
                }
            }
        }

        /// <summary>
        /// 设置状态文本
        /// </summary>
        public void SetStatusText(string text)
        {
            _labelListStatus.Text = text;
        }

        /// <summary>
        /// 按关键字筛选列表项
        /// </summary>
        public void FilterItems(string keyword)
        {
            _searchKeyword = keyword?.Trim() ?? string.Empty;
            UpdateFilteredItems();
        }

        /// <summary>
        /// 检查指定项是否为收藏项
        /// </summary>
        public bool IsFavoriteItem(string itemText)
        {
            return _favoriteItems.Contains(itemText);
        }

        #endregion

        #region Private Methods

        private void UpdateFilteredItems()
        {
            if (_isUpdatingItems) return;
            _isUpdatingItems = true;

            try
            {
                var filtered = _allItems.AsEnumerable();

                if (!string.IsNullOrEmpty(_searchKeyword))
                {
                    filtered = filtered.Where(item => 
                        item.IndexOf(_searchKeyword, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                if (_showFavoritesOnly)
                {
                    filtered = filtered.Where(item => 
                        _favoriteItems.Contains(item) || 
                        _favoriteItems.Any(fav => fav.EndsWith($"]{item}")));
                }

                var selectedItem = _listBoxItems.SelectedItem?.ToString();
                
                _listBoxItems.Items.Clear();
                foreach (var item in filtered)
                {
                    _listBoxItems.Items.Add(item);
                }

                if (selectedItem != null && _listBoxItems.Items.Contains(selectedItem))
                {
                    _listBoxItems.SelectedItem = selectedItem;
                }
                else if (_listBoxItems.Items.Count > 0)
                {
                    _listBoxItems.SelectedIndex = 0;
                }
            }
            finally
            {
                _isUpdatingItems = false;
            }

            int totalCount = _allItems.Count;
            int filteredCount = _listBoxItems.Items.Count;
            int favoriteCount = _favoriteItems.Count;
            
            string status = $"共 {filteredCount} 项";
            if (filteredCount != totalCount)
            {
                status += $" (全部 {totalCount})";
            }
            if (_showFavoritesOnly)
            {
                status = $"⭐ 收藏 {filteredCount} 项";
            }
            SetStatusText(status);
        }

        #endregion
    }
}