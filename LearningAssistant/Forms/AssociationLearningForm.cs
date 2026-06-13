namespace LearningAssistant.Forms
{
    /// <summary>
    /// 联想学习对话框
    /// 
    /// 核心功能：帮助用户建立知识关联网络，通过展示同类词、反义词、相关词、例句和知识点，
    /// 帮助用户理解知识点之间的关联，增强记忆效果。
    /// 
    /// 设计原理：
    /// 1. 知识网络：将孤立的知识点连接成网络，形成系统性理解
    /// 2. 多维度联想：从多个角度展示知识关联（同义、反义、相关、用法、概念）
    /// 3. 主动思考引导：通过思考题引导用户深入思考
    /// 4. 可视化呈现：使用树状结构展示关联关系
    /// 
    /// 使用场景：
    /// - 学习汉字、单词时建立联想记忆
    /// - 理解概念之间的关系
    /// - 构建个人知识图谱
    /// </summary>
    public partial class AssociationLearningForm : Form
    {
        #region 字段定义

        /// <summary>
        /// 当前学习内容
        /// </summary>
        private string _currentContent;

        /// <summary>
        /// 联想节点列表
        /// </summary>
        private List<AssociationNode> _associations;

        // UI 控件
        private Panel panelMain;
        private TreeView treeViewAssociations;
        private Panel panelDetails;
        private Label labelHint;
        private Label labelCurrent;
        private Label labelDetailTitle;
        private Label labelTitle;
        private Label labelTreeTitle;
        private Label labelDetailContent;
        private Panel panelActions;
        private Button buttonThinkMore;
        private Button buttonSkip;


        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化联想学习对话框
        /// </summary>
        /// <param name="content">要学习的内容</param>
        public AssociationLearningForm(string content)
        {
            _currentContent = content;
            _associations = new List<AssociationNode>();
            InitializeComponent();
            // 横向类型标签循环（无需提前声明，动态创建）
            string[] associationTypes = { "📝 同类词", "🔄 反义词", "🏷️ 相关词", "📖 例句", "💡 知识点" };
            int xPos = 15;
            foreach (var type in associationTypes)
            {
                Label label = new Label();
                label.Text = type;
                label.Location = new Point(xPos, 85);
                label.Size = new Size(120, 25);
                label.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
                label.ForeColor = Color.FromArgb(80, 80, 80);
                label.BorderStyle = BorderStyle.FixedSingle;
                label.TextAlign = ContentAlignment.MiddleCenter;

                panelMain.Controls.Add(label);
                xPos += 130;
            }

            labelCurrent.Text = $"当前学习：{_currentContent}";
            LoadAssociations();
        }

        #endregion

        #region UI 初始化

        /// <summary>
        /// 初始化窗体组件
        /// 
        /// 创建完整的联想学习界面，包括：
        /// - 标题区域
        /// - 当前内容显示
        /// - 联想类型标签
        /// - 联想网络树视图
        /// - 详情面板
        /// - 操作按钮
        /// </summary>
        private void InitializeComponent()
        {
            // 窗体基础属性
            this.SuspendLayout();

            this.Text = "🧠 联想学习";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(250, 248, 245);

            #region 实例化所有控件 new()
            panelMain = new Panel();
            labelHint = new Label();
            labelCurrent = new Label();
            treeViewAssociations = new TreeView();
            panelDetails = new Panel();
            labelDetailTitle = new Label();
            labelTitle = new Label();
            labelTreeTitle = new Label();
            labelDetailContent = new Label();
            panelActions = new Panel();
            buttonThinkMore = new Button();
            buttonSkip = new Button();
            #endregion

            #region panelMain 整体配置
            panelMain.Dock = DockStyle.Fill;
            #endregion

            #region 标题 labelTitle
            labelTitle.Text = "🔗 联想学习 - 建立知识网络";
            labelTitle.Font = new Font("微软雅黑", 13F, FontStyle.Bold);
            labelTitle.ForeColor = Color.FromArgb(103, 58, 183);
            labelTitle.Dock = DockStyle.Top;
            labelTitle.Height = 40;
            labelTitle.TextAlign = ContentAlignment.MiddleLeft;
            labelTitle.Padding = new Padding(15, 0, 0, 0);
            #endregion

            #region 底部提示 labelHint
            labelHint.Text = "💡 提示：联想学习帮助你建立知识之间的关联，关联越强，记忆越深刻！";
            labelHint.Dock = DockStyle.Bottom;
            labelHint.Height = 35;
            labelHint.Font = new Font("微软雅黑", 9F);
            labelHint.ForeColor = Color.FromArgb(100, 100, 100);
            labelHint.TextAlign = ContentAlignment.MiddleLeft;
            labelHint.Padding = new Padding(15, 0, 0, 0);
            labelHint.BackColor = Color.FromArgb(248, 250, 252);
            #endregion


            #region 树视图标题 labelTreeTitle
            labelTreeTitle.Text = "🌳 联想网络";
            labelTreeTitle.Location = new Point(15, 115);
            labelTreeTitle.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelTreeTitle.AutoSize = true;
            #endregion

            #region TreeView
            treeViewAssociations.Location = new Point(15, 140);
            treeViewAssociations.Size = new Size(300, 280);
            treeViewAssociations.Font = new Font("微软雅黑", 10F);
            treeViewAssociations.BackColor = Color.White;
            treeViewAssociations.BorderStyle = BorderStyle.FixedSingle;
            treeViewAssociations.AfterSelect += TreeViewAssociations_AfterSelect;
            #endregion

            #region 详情标题 labelDetailTitle
            labelDetailTitle.Text = "📋 详细信息";
            labelDetailTitle.Location = new Point(330, 115);
            labelDetailTitle.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelDetailTitle.AutoSize = true;
            #endregion

            #region 详情面板 panelDetails
            panelDetails.Location = new Point(330, 140);
            panelDetails.Size = new Size(340, 280);
            panelDetails.BackColor = Color.White;
            panelDetails.BorderStyle = BorderStyle.FixedSingle;
            panelDetails.AutoScroll = true;
            #endregion

            #region 详情内部标签 labelDetailContent
            labelDetailContent.Name = "detailContent";
            labelDetailContent.Dock = DockStyle.Fill;
            labelDetailContent.Font = new Font("微软雅黑", 10F);
            labelDetailContent.ForeColor = Color.FromArgb(60, 60, 60);
            labelDetailContent.Padding = new Padding(10);
            labelDetailContent.Text = "从左侧选择一个联想项查看详情";
            #endregion

            #region 按钮容器 panelActions
            panelActions.Location = new Point(330, 430);
            panelActions.Size = new Size(340, 35);
            panelActions.BackColor = Color.Transparent;
            #endregion

            #region 思考更多按钮
            buttonThinkMore.Text = "🤔 我能想到更多...";
            buttonThinkMore.Location = new Point(0, 0);
            buttonThinkMore.Size = new Size(160, 30);
            buttonThinkMore.BackColor = Color.FromArgb(76, 175, 80);
            buttonThinkMore.ForeColor = Color.White;
            buttonThinkMore.FlatStyle = FlatStyle.Flat;
            buttonThinkMore.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            buttonThinkMore.Click += ButtonThinkMore_Click;
            #endregion

            #region 跳过按钮
            buttonSkip.Text = "➡ 跳过";
            buttonSkip.Location = new Point(175, 0);
            buttonSkip.Size = new Size(80, 30);
            buttonSkip.BackColor = Color.Gray;
            buttonSkip.ForeColor = Color.White;
            buttonSkip.FlatStyle = FlatStyle.Flat;
            buttonSkip.Font = new Font("微软雅黑", 9F);
            buttonSkip.Click += ButtonSkip_Click;
            #endregion

            #region 当前内容 labelCurrent
            labelCurrent.Text = "当前学习：";
            labelCurrent.Location = new Point(15, 50);
            labelCurrent.Size = new Size(650, 30);
            labelCurrent.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            labelCurrent.ForeColor = Color.FromArgb(66, 133, 244);
            #endregion
            #region 逐级控件嵌套 Add 加入容器（标准顺序）
            // 详情面板内部
            panelDetails.Controls.Add(labelDetailContent);

            // 按钮面板内部
            panelActions.Controls.Add(buttonThinkMore);
            panelActions.Controls.Add(buttonSkip);

            // 主面板批量加入子控件
            panelMain.Controls.Add(labelHint);
            panelMain.Controls.Add(panelActions);
            panelMain.Controls.Add(panelDetails);
            panelMain.Controls.Add(labelDetailTitle);
            panelMain.Controls.Add(treeViewAssociations);
            panelMain.Controls.Add(labelTreeTitle);
            panelMain.Controls.Add(labelCurrent);
            panelMain.Controls.Add(labelTitle);

            // 窗体挂载主面板
            this.Controls.Add(panelMain);
            #endregion

            this.ResumeLayout(false);
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载联想数据
        /// 
        /// 根据当前学习内容生成联想，并填充到树视图中。
        /// </summary>
        private void LoadAssociations()
        {
            // 生成联想内容
            _associations = GenerateAssociations(_currentContent);

            // 清空树视图
            treeViewAssociations.Nodes.Clear();

            // 同类词节点
            TreeNode similarNode = new TreeNode("📝 同类词");
            var similarItems = _associations.Where(a => a.Type == "同类词").ToList();
            foreach (var item in similarItems)
            {
                similarNode.Nodes.Add(new TreeNode(item.Content));
            }
            if (similarItems.Count > 0)
                treeViewAssociations.Nodes.Add(similarNode);

            // 反义词节点
            TreeNode oppositeNode = new TreeNode("🔄 反义词");
            var oppositeItems = _associations.Where(a => a.Type == "反义词").ToList();
            foreach (var item in oppositeItems)
            {
                oppositeNode.Nodes.Add(new TreeNode(item.Content));
            }
            if (oppositeItems.Count > 0)
                treeViewAssociations.Nodes.Add(oppositeNode);

            // 相关词节点
            TreeNode relatedNode = new TreeNode("🏷️ 相关词");
            var relatedItems = _associations.Where(a => a.Type == "相关词").ToList();
            foreach (var item in relatedItems)
            {
                relatedNode.Nodes.Add(new TreeNode(item.Content));
            }
            if (relatedItems.Count > 0)
                treeViewAssociations.Nodes.Add(relatedNode);

            // 例句节点
            TreeNode exampleNode = new TreeNode("📖 例句");
            var exampleItems = _associations.Where(a => a.Type == "例句").ToList();
            foreach (var item in exampleItems)
            {
                TreeNode exampleTreeNode = new TreeNode(item.Content);
                exampleNode.Nodes.Add(exampleTreeNode);
            }
            if (exampleItems.Count > 0)
                treeViewAssociations.Nodes.Add(exampleNode);

            // 知识点节点
            TreeNode knowledgeNode = new TreeNode("💡 知识点");
            var knowledgeItems = _associations.Where(a => a.Type == "知识点").ToList();
            foreach (var item in knowledgeItems)
            {
                TreeNode knowledgeTreeNode = new TreeNode(item.Content);
                knowledgeNode.Nodes.Add(knowledgeTreeNode);
            }
            if (knowledgeItems.Count > 0)
                treeViewAssociations.Nodes.Add(knowledgeNode);

            // 展开所有节点
            treeViewAssociations.ExpandAll();
        }

        /// <summary>
        /// 生成联想内容
        /// 
        /// 根据内容类型生成不同的联想：
        /// - 汉字：形近字、音近字、词语搭配、笔画顺序、结构
        /// - 英文单词：近义词、反义词、例句
        /// 
        /// 实际应用中，应该从数据库或知识图谱中获取真实的联想数据。
        /// </summary>
        /// <param name="content">学习内容</param>
        /// <returns>联想节点列表</returns>
        private List<AssociationNode> GenerateAssociations(string content)
        {
            var associations = new List<AssociationNode>();

            // 示例：为单个汉字生成联想
            if (content.Length == 1 && IsChinese(content[0]))
            {
                // 同类词（形近字、音近字）
                associations.Add(new AssociationNode
                {
                    Type = "同类词",
                    Content = "相关汉字示例",
                    Description = "与当前汉字形状或发音相似的其他汉字"
                });

                // 相关词
                associations.Add(new AssociationNode
                {
                    Type = "相关词",
                    Content = "词语搭配",
                    Description = "可以与当前汉字搭配使用的其他词语"
                });

                // 知识点
                associations.Add(new AssociationNode
                {
                    Type = "知识点",
                    Content = "这个字的笔画顺序",
                    Description = "书写时的正确笔顺：横、竖、撇、捺..."
                });

                associations.Add(new AssociationNode
                {
                    Type = "知识点",
                    Content = "这个字的结构",
                    Description = "属于左右结构/上下结构/包围结构等"
                });
            }
            // 英文单词联想
            else if (IsEnglish(content))
            {
                // 同类词（近义词）
                associations.Add(new AssociationNode
                {
                    Type = "同类词",
                    Content = "近义词",
                    Description = "含义相近的其他英文单词"
                });

                // 反义词
                associations.Add(new AssociationNode
                {
                    Type = "反义词",
                    Content = "反义词",
                    Description = "含义相反的英文单词"
                });

                // 例句
                associations.Add(new AssociationNode
                {
                    Type = "例句",
                    Content = "例句1",
                    Description = "在真实语境中的使用示例"
                });
            }

            return associations;
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 树视图选择事件处理
        /// 
        /// 用户选择联想项后，在详情面板中显示该联想的详细信息。
        /// </summary>
        /// <param name="sender">触发事件的树视图</param>
        /// <param name="e">树视图事件参数</param>
        private void TreeViewAssociations_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // 确保选择的是子节点（非根节点）
            if (e.Node != null && e.Node.Parent != null)
            {
                string type = e.Node.Parent.Text;
                string content = e.Node.Text;

                // 查找对应的联想节点
                var association = _associations.FirstOrDefault(a =>
                    a.Type == type.Replace("📝", "").Replace("🔄", "").Replace("🏷️", "").Replace("📖", "").Replace("💡", "").Trim() &&
                    a.Content == content);

                if (association != null)
                {
                    ShowDetail(association);
                }
            }
        }

        /// <summary>
        /// 显示联想详情
        /// 
        /// 在详情面板中显示联想的详细信息，包括类型、内容、说明和思考题。
        /// </summary>
        /// <param name="association">联想节点</param>
        private void ShowDetail(AssociationNode association)
        {
            var detailLabel = panelDetails.Controls["detailContent"] as Label;
            if (detailLabel != null)
            {
                string detailText = $"类型：{association.Type}\n\n" +
                                  $"内容：{association.Content}\n\n" +
                                  $"说明：{association.Description}\n\n" +
                                  "━━━━━━━━━━━━━━━━━━━━\n" +
                                  "💡 思考题：\n" +
                                  "你能用自己的话解释这个联想吗？\n" +
                                  "这个知识点和你之前学过的有什么联系？";

                detailLabel.Text = detailText;
            }
        }

        /// <summary>
        /// "我能想到更多..."按钮点击事件处理
        /// 
        /// 显示深入思考的引导问题，鼓励用户主动思考。
        /// </summary>
        /// <param name="sender">触发事件的按钮</param>
        /// <param name="e">事件参数</param>
        private void ButtonThinkMore_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "🤔 继续思考...\n\n" +
                "1. 这个内容让你想起了什么？\n" +
                "2. 它和生活中的什么事情相关？\n" +
                "3. 有没有什么有趣的记忆方法？\n\n" +
                "💡 主动思考比被动接受记得更牢！",
                "深入思考",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        private void ButtonSkip_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查字符是否为中文
        /// </summary>
        /// <param name="c">字符</param>
        /// <returns>是否为中文字符</returns>
        private bool IsChinese(char c)
        {
            return c >= 0x4e00 && c <= 0x9fa5;
        }

        /// <summary>
        /// 检查文本是否为英文
        /// </summary>
        /// <param name="text">文本</param>
        /// <returns>是否为纯英文</returns>
        private bool IsEnglish(string text)
        {
            return text.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));
        }

        #endregion

        #region 内部类

        /// <summary>
        /// 联想节点
        /// 
        /// 表示一个知识关联项，包含类型、内容和说明。
        /// </summary>
        private class AssociationNode
        {
            /// <summary>
            /// 联想类型（同类词、反义词、相关词、例句、知识点）
            /// </summary>
            public string Type { get; set; }

            /// <summary>
            /// 联想内容
            /// </summary>
            public string Content { get; set; }

            /// <summary>
            /// 联想说明
            /// </summary>
            public string Description { get; set; }
        }

        #endregion
    }
}
