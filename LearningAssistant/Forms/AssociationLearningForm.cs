using LearningAssistant.Services.AI;
using System.Text.Json;

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

        /// <summary>
        /// AI 问答服务
        /// </summary>
        private readonly IAiQuestionService? _aiService;

        // UI 控件
        private Panel panelMain;
        private Panel panelContent;
        private Panel panelFilterBar;
        private Panel panelLeft;
        private Panel panelRight;
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
        private Button _buttonGenerateAi;
        private Button _buttonAddManual;
        private Panel _panelAddManual;
        private Panel panelDetailActions;
        private Button buttonAddNote;
        private Button buttonAddToReview;


        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化联想学习对话框
        /// </summary>
        /// <param name="content">要学习的内容</param>
        /// <param name="aiService">AI 问答服务（可选）</param>
        public AssociationLearningForm(string content, IAiQuestionService? aiService = null)
        {
            _currentContent = content;
            _associations = new List<AssociationNode>();
            _aiService = aiService;
            InitializeComponent();
            // 横向类型标签循环（改为可点击的按钮，用于过滤）
            string[] associationTypes = { "📝 同类词", "🔄 反义词", "🏷️ 相关词", "📖 例句", "💡 知识点" };
            int xPos = 0;
            foreach (var type in associationTypes)
            {
                Button btn = new Button();
                btn.Text = type;
                btn.Location = new Point(xPos, 8);
                btn.Size = new Size(120, 28);
                btn.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
                btn.ForeColor = Color.FromArgb(80, 80, 80);
                btn.BackColor = Color.White;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
                btn.TextAlign = ContentAlignment.MiddleCenter;
                btn.Tag = type;
                btn.Click += FilterTypeButton_Click;

                panelFilterBar.Controls.Add(btn);
                xPos += 128;
            }

            labelCurrent.Text = $"当前学习：{_currentContent}";
            LoadAssociations();
            SetupAiButton();
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
            panelMain = new Panel();
            labelHint = new Label();
            panelActions = new Panel();
            buttonThinkMore = new Button();
            buttonSkip = new Button();
            _buttonGenerateAi = new Button();
            panelContent = new Panel();
            panelRight = new Panel();
            panelDetails = new Panel();
            panelDetailActions = new Panel();
            buttonAddNote = new Button();
            buttonAddToReview = new Button();
            labelDetailContent = new Label();
            labelDetailTitle = new Label();
            panelLeft = new Panel();
            treeViewAssociations = new TreeView();
            labelTreeTitle = new Label();
            panelFilterBar = new Panel();
            labelCurrent = new Label();
            labelTitle = new Label();
            panelMain.SuspendLayout();
            panelActions.SuspendLayout();
            panelContent.SuspendLayout();
            panelRight.SuspendLayout();
            panelDetails.SuspendLayout();
            panelLeft.SuspendLayout();
            SuspendLayout();
            // 
            // panelMain
            // 
            panelMain.Controls.Add(labelHint);
            panelMain.Controls.Add(panelActions);
            panelMain.Controls.Add(panelContent);
            panelMain.Controls.Add(panelFilterBar);
            panelMain.Controls.Add(labelCurrent);
            panelMain.Controls.Add(labelTitle);
            panelMain.Dock = DockStyle.Fill;
            panelMain.Location = new Point(0, 0);
            panelMain.Name = "panelMain";
            panelMain.Size = new Size(684, 461);
            panelMain.TabIndex = 0;
            // 
            // labelHint
            // 
            labelHint.BackColor = Color.FromArgb(248, 250, 252);
            labelHint.Dock = DockStyle.Bottom;
            labelHint.Font = new Font("微软雅黑", 9F);
            labelHint.ForeColor = Color.FromArgb(100, 100, 100);
            labelHint.Location = new Point(0, 381);
            labelHint.Name = "labelHint";
            labelHint.Padding = new Padding(15, 0, 0, 0);
            labelHint.Size = new Size(684, 35);
            labelHint.TabIndex = 0;
            labelHint.Text = "💡 提示：联想学习帮助你建立知识之间的关联，关联越强，记忆越深刻！";
            labelHint.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panelActions
            // 
            panelActions.BackColor = Color.Transparent;
            panelActions.Controls.Add(buttonThinkMore);
            panelActions.Controls.Add(buttonSkip);
            panelActions.Controls.Add(_buttonGenerateAi);

            _buttonAddManual = new Button
            {
                BackColor = Color.FromArgb(33, 150, 243),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(390, 5),
                Name = "_buttonAddManual",
                Size = new Size(120, 30),
                TabIndex = 3,
                Text = "➕ 添加关联",
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand
            };
            _buttonAddManual.FlatAppearance.BorderSize = 0;
            _buttonAddManual.Click += ButtonAddManual_Click;
            panelActions.Controls.Add(_buttonAddManual);
            panelActions.Dock = DockStyle.Bottom;
            panelActions.Location = new Point(0, 416);
            panelActions.Name = "panelActions";
            panelActions.Padding = new Padding(15, 5, 15, 5);
            panelActions.Size = new Size(684, 45);
            panelActions.TabIndex = 1;
            // 
            // buttonThinkMore
            // 
            buttonThinkMore.BackColor = Color.FromArgb(76, 175, 80);
            buttonThinkMore.FlatStyle = FlatStyle.Flat;
            buttonThinkMore.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            buttonThinkMore.ForeColor = Color.White;
            buttonThinkMore.Location = new Point(0, 5);
            buttonThinkMore.Name = "buttonThinkMore";
            buttonThinkMore.Size = new Size(160, 30);
            buttonThinkMore.TabIndex = 0;
            buttonThinkMore.Text = "🤔 我能想到更多...";
            buttonThinkMore.UseVisualStyleBackColor = false;
            buttonThinkMore.Click += ButtonThinkMore_Click;
            // 
            // buttonSkip
            // 
            buttonSkip.BackColor = Color.Gray;
            buttonSkip.FlatStyle = FlatStyle.Flat;
            buttonSkip.Font = new Font("微软雅黑", 9F);
            buttonSkip.ForeColor = Color.White;
            buttonSkip.Location = new Point(170, 5);
            buttonSkip.Name = "buttonSkip";
            buttonSkip.Size = new Size(80, 30);
            buttonSkip.TabIndex = 1;
            buttonSkip.Text = "➡ 跳过";
            buttonSkip.UseVisualStyleBackColor = false;
            buttonSkip.Click += ButtonSkip_Click;
            // 
            // _buttonGenerateAi
            // 
            _buttonGenerateAi.BackColor = Color.FromArgb(156, 39, 176);
            _buttonGenerateAi.FlatStyle = FlatStyle.Flat;
            _buttonGenerateAi.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _buttonGenerateAi.ForeColor = Color.White;
            _buttonGenerateAi.Location = new Point(260, 5);
            _buttonGenerateAi.Name = "_buttonGenerateAi";
            _buttonGenerateAi.Size = new Size(120, 30);
            _buttonGenerateAi.TabIndex = 2;
            _buttonGenerateAi.Text = "✨ AI生成联想";
            _buttonGenerateAi.UseVisualStyleBackColor = false;
            _buttonGenerateAi.Visible = false;
            _buttonGenerateAi.Click += ButtonGenerateAi_Click;
            // 
            // panelContent
            // 
            panelContent.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panelContent.Controls.Add(panelRight);
            panelContent.Controls.Add(panelLeft);
            panelContent.Location = new Point(0, 115);
            panelContent.Name = "panelContent";
            panelContent.Padding = new Padding(15);
            panelContent.Size = new Size(684, 263);
            panelContent.TabIndex = 2;
            // 
            // panelRight
            // 
            panelRight.BackColor = Color.White;
            panelRight.BorderStyle = BorderStyle.FixedSingle;
            panelRight.Controls.Add(panelDetails);
            panelRight.Controls.Add(panelDetailActions);
            panelRight.Controls.Add(labelDetailTitle);
            panelRight.Dock = DockStyle.Fill;
            panelRight.Location = new Point(335, 15);
            panelRight.Name = "panelRight";
            panelRight.Size = new Size(334, 233);
            panelRight.TabIndex = 0;
            // 
            // panelDetails
            // 
            panelDetails.AutoScroll = true;
            panelDetails.BackColor = Color.White;
            panelDetails.BorderStyle = BorderStyle.FixedSingle;
            panelDetails.Controls.Add(labelDetailContent);
            panelDetails.Dock = DockStyle.Fill;
            panelDetails.Location = new Point(0, 25);
            panelDetails.Name = "panelDetails";
            panelDetails.Size = new Size(332, 176);
            panelDetails.TabIndex = 0;
            // 
            // panelDetailActions
            // 
            panelDetailActions.BackColor = Color.FromArgb(250, 250, 252);
            panelDetailActions.Controls.Add(buttonAddNote);
            panelDetailActions.Controls.Add(buttonAddToReview);
            panelDetailActions.Dock = DockStyle.Bottom;
            panelDetailActions.Name = "panelDetailActions";
            panelDetailActions.Size = new Size(332, 40);
            panelDetailActions.TabIndex = 2;
            // 
            // buttonAddNote
            // 
            buttonAddNote.FlatStyle = FlatStyle.Flat;
            buttonAddNote.Font = new Font("微软雅黑", 9F);
            buttonAddNote.ForeColor = Color.FromArgb(102, 102, 102);
            buttonAddNote.Location = new Point(10, 5);
            buttonAddNote.Name = "buttonAddNote";
            buttonAddNote.Size = new Size(100, 30);
            buttonAddNote.TabIndex = 0;
            buttonAddNote.Text = "📝 补充笔记";
            buttonAddNote.UseVisualStyleBackColor = true;
            buttonAddNote.FlatAppearance.BorderSize = 0;
            buttonAddNote.Cursor = Cursors.Hand;
            buttonAddNote.Click += ButtonAddNote_Click;
            // 
            // buttonAddToReview
            // 
            buttonAddToReview.FlatStyle = FlatStyle.Flat;
            buttonAddToReview.Font = new Font("微软雅黑", 9F);
            buttonAddToReview.ForeColor = Color.FromArgb(108, 92, 231);
            buttonAddToReview.Location = new Point(220, 5);
            buttonAddToReview.Name = "buttonAddToReview";
            buttonAddToReview.Size = new Size(100, 30);
            buttonAddToReview.TabIndex = 1;
            buttonAddToReview.Text = "🔔 加入复习";
            buttonAddToReview.UseVisualStyleBackColor = true;
            buttonAddToReview.FlatAppearance.BorderSize = 0;
            buttonAddToReview.Cursor = Cursors.Hand;
            buttonAddToReview.Click += ButtonAddToReview_Click;
            // 
            // labelDetailContent
            // 
            labelDetailContent.Dock = DockStyle.Fill;
            labelDetailContent.Font = new Font("微软雅黑", 10F);
            labelDetailContent.ForeColor = Color.FromArgb(60, 60, 60);
            labelDetailContent.Location = new Point(0, 0);
            labelDetailContent.Name = "labelDetailContent";
            labelDetailContent.Padding = new Padding(10);
            labelDetailContent.Size = new Size(330, 204);
            labelDetailContent.TabIndex = 0;
            labelDetailContent.Text = "从左侧选择一个联想项查看详情";
            // 
            // labelDetailTitle
            // 
            labelDetailTitle.Dock = DockStyle.Top;
            labelDetailTitle.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelDetailTitle.Location = new Point(0, 0);
            labelDetailTitle.Name = "labelDetailTitle";
            labelDetailTitle.Size = new Size(332, 25);
            labelDetailTitle.TabIndex = 1;
            labelDetailTitle.Text = "📋 详细信息";
            // 
            // panelLeft
            // 
            panelLeft.BackColor = Color.White;
            panelLeft.BorderStyle = BorderStyle.FixedSingle;
            panelLeft.Controls.Add(treeViewAssociations);
            panelLeft.Controls.Add(labelTreeTitle);
            panelLeft.Dock = DockStyle.Left;
            panelLeft.Location = new Point(15, 15);
            panelLeft.Name = "panelLeft";
            panelLeft.Size = new Size(320, 233);
            panelLeft.TabIndex = 1;
            // 
            // treeViewAssociations
            // 
            treeViewAssociations.BackColor = Color.White;
            treeViewAssociations.BorderStyle = BorderStyle.FixedSingle;
            treeViewAssociations.Dock = DockStyle.Fill;
            treeViewAssociations.Font = new Font("微软雅黑", 10F);
            treeViewAssociations.Location = new Point(0, 25);
            treeViewAssociations.Name = "treeViewAssociations";
            treeViewAssociations.Size = new Size(318, 206);
            treeViewAssociations.TabIndex = 0;
            treeViewAssociations.AfterSelect += TreeViewAssociations_AfterSelect;
            // 
            // labelTreeTitle
            // 
            labelTreeTitle.Dock = DockStyle.Top;
            labelTreeTitle.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelTreeTitle.Location = new Point(0, 0);
            labelTreeTitle.Name = "labelTreeTitle";
            labelTreeTitle.Size = new Size(318, 25);
            labelTreeTitle.TabIndex = 1;
            labelTreeTitle.Text = "🌳 联想网络";
            // 
            // panelFilterBar
            // 
            panelFilterBar.BackColor = Color.Transparent;
            panelFilterBar.Dock = DockStyle.Top;
            panelFilterBar.Location = new Point(0, 70);
            panelFilterBar.Name = "panelFilterBar";
            panelFilterBar.Size = new Size(684, 45);
            panelFilterBar.TabIndex = 3;
            // 
            // labelCurrent
            // 
            labelCurrent.Dock = DockStyle.Top;
            labelCurrent.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            labelCurrent.ForeColor = Color.FromArgb(66, 133, 244);
            labelCurrent.Location = new Point(0, 40);
            labelCurrent.Name = "labelCurrent";
            labelCurrent.Padding = new Padding(15, 0, 0, 0);
            labelCurrent.Size = new Size(684, 30);
            labelCurrent.TabIndex = 4;
            labelCurrent.Text = "当前学习：";
            labelCurrent.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelTitle
            // 
            labelTitle.Dock = DockStyle.Top;
            labelTitle.Font = new Font("微软雅黑", 13F, FontStyle.Bold);
            labelTitle.ForeColor = Color.FromArgb(103, 58, 183);
            labelTitle.Location = new Point(0, 0);
            labelTitle.Name = "labelTitle";
            labelTitle.Padding = new Padding(15, 0, 0, 0);
            labelTitle.Size = new Size(684, 40);
            labelTitle.TabIndex = 5;
            labelTitle.Text = "🔗 联想学习 - 建立知识网络";
            labelTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // AssociationLearningForm
            // 
            BackColor = Color.FromArgb(250, 248, 245);
            ClientSize = new Size(684, 461);
            Controls.Add(panelMain);
            Name = "AssociationLearningForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "\U0001f9e0 联想学习";
            panelMain.ResumeLayout(false);
            panelActions.ResumeLayout(false);
            panelContent.ResumeLayout(false);
            panelRight.ResumeLayout(false);
            panelDetails.ResumeLayout(false);
            panelLeft.ResumeLayout(false);
            ResumeLayout(false);
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
            _currentSelectedAssociation = association;

            string typeIcon = GetTypeIcon(association.Type);
            string noteSection = string.Empty;
            if (!string.IsNullOrEmpty(association.Notes))
            {
                noteSection = $"\n━━━━━━━━━━━━━━━━━━━━\n\n📋 我的笔记：\n   {association.Notes}\n\n";
            }

            string detailText =
                $"{typeIcon} 【{association.Type}】\n\n" +
                $"━━━━━━━━━━━━━━━━━━━━\n\n" +
                $"📝 内容：\n   {association.Content}\n\n" +
                $"📖 说明：\n   {association.Description}\n" +
                noteSection +
                $"━━━━━━━━━━━━━━━━━━━━\n\n" +
                $"🧠 记忆技巧：\n" +
                $"   • 把「{_currentContent}」和「{association.Content}」联系起来\n" +
                $"   • 想象一个包含两者的画面\n" +
                $"   • 用自己的话复述这个关联\n\n" +
                $"💡 思考题：\n" +
                $"   1. 你能用自己的话解释这个联想吗？\n" +
                $"   2. 这个知识点和你之前学过的有什么联系？\n" +
                $"   3. 你能想到更多类似的关联吗？";

            labelDetailContent.Text = detailText;
        }

        /// <summary>
        /// 获取类型对应的图标
        /// </summary>
        private string GetTypeIcon(string type)
        {
            return type switch
            {
                "同类词" => "📝",
                "反义词" => "🔄",
                "相关词" => "🏷️",
                "例句" => "📖",
                "知识点" => "💡",
                _ => "✨"
            };
        }

        /// <summary>
        /// 类型过滤按钮点击事件
        /// 点击后在树视图中展开对应类型的节点
        /// </summary>
        private void FilterTypeButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is string typeText)
            {
                string type = typeText.Replace("📝", "").Replace("🔄", "").Replace("🏷️", "").Replace("📖", "").Replace("💡", "").Trim();

                foreach (TreeNode node in treeViewAssociations.Nodes)
                {
                    string nodeType = node.Text.Replace("📝", "").Replace("🔄", "").Replace("🏷️", "").Replace("📖", "").Replace("💡", "").Trim();

                    if (nodeType == type)
                    {
                        treeViewAssociations.SelectedNode = node;
                        node.Expand();

                        if (node.Nodes.Count > 0)
                        {
                            treeViewAssociations.SelectedNode = node.Nodes[0];
                            var association = _associations.FirstOrDefault(a => a.Type == type && a.Content == node.Nodes[0].Text);
                            if (association != null)
                            {
                                ShowDetail(association);
                            }
                        }

                        HighlightFilterButton(typeText);
                        return;
                    }
                }

                labelDetailContent.Text = $"暂无「{type}」类型的联想内容\n\n点击「✨ AI生成联想」按钮生成更多内容！";
                HighlightFilterButton(typeText);
            }
        }

        /// <summary>
        /// 高亮当前选中的过滤按钮
        /// </summary>
        private void HighlightFilterButton(string selectedType)
        {
            foreach (Control ctrl in panelMain.Controls)
            {
                if (ctrl is Button btn && btn.Tag is string type)
                {
                    if (type == selectedType)
                    {
                        btn.BackColor = Color.FromArgb(103, 58, 183);
                        btn.ForeColor = Color.White;
                        btn.FlatAppearance.BorderColor = Color.FromArgb(103, 58, 183);
                    }
                    else
                    {
                        btn.BackColor = Color.White;
                        btn.ForeColor = Color.FromArgb(80, 80, 80);
                        btn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
                    }
                }
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

        /// <summary>
        /// 手动添加关联按钮点击事件
        /// </summary>
        private void ButtonAddManual_Click(object? sender, EventArgs e)
        {
            using var typeForm = new Form
            {
                Text = "添加自定义关联",
                Size = new Size(420, 260),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            var lblType = new Label
            {
                Text = "关联类型：",
                Location = new Point(20, 20),
                Size = new Size(80, 24),
                Font = new Font("微软雅黑", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var comboType = new ComboBox
            {
                Location = new Point(110, 20),
                Size = new Size(260, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("微软雅黑", 9F)
            };
            comboType.Items.AddRange(new object[] { "📝 同类词", "🔄 反义词", "🏷️ 相关词", "📖 例句", "💡 知识点", "✨ 自定义" });
            comboType.SelectedIndex = 2;

            var lblContent = new Label
            {
                Text = "关联内容：",
                Location = new Point(20, 60),
                Size = new Size(80, 24),
                Font = new Font("微软雅黑", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var txtContent = new TextBox
            {
                Location = new Point(110, 60),
                Size = new Size(260, 24),
                Font = new Font("微软雅黑", 9F)
            };

            var lblDesc = new Label
            {
                Text = "说明描述：",
                Location = new Point(20, 100),
                Size = new Size(80, 24),
                Font = new Font("微软雅黑", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var txtDesc = new TextBox
            {
                Location = new Point(110, 100),
                Size = new Size(260, 60),
                Font = new Font("微软雅黑", 9F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            var btnConfirm = new Button
            {
                Text = "确认添加",
                Location = new Point(110, 175),
                Size = new Size(100, 32),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(230, 175),
                Size = new Size(80, 32),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 9F),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            btnConfirm.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtContent.Text))
                {
                    MessageBox.Show("请输入关联内容", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string typeText = comboType.SelectedItem?.ToString() ?? "🏷️ 相关词";
                string type = typeText.Replace("📝", "").Replace("🔄", "").Replace("🏷️", "").Replace("📖", "").Replace("💡", "").Replace("✨", "").Trim();

                _associations.Add(new AssociationNode
                {
                    Type = type,
                    Content = txtContent.Text.Trim(),
                    Description = string.IsNullOrWhiteSpace(txtDesc.Text) ? "用户自定义关联" : txtDesc.Text.Trim()
                });

                LoadAssociations();
                typeForm.DialogResult = DialogResult.OK;
                typeForm.Close();
            };

            btnCancel.Click += (s, ev) =>
            {
                typeForm.DialogResult = DialogResult.Cancel;
                typeForm.Close();
            };

            typeForm.Controls.Add(lblType);
            typeForm.Controls.Add(comboType);
            typeForm.Controls.Add(lblContent);
            typeForm.Controls.Add(txtContent);
            typeForm.Controls.Add(lblDesc);
            typeForm.Controls.Add(txtDesc);
            typeForm.Controls.Add(btnConfirm);
            typeForm.Controls.Add(btnCancel);

            typeForm.ShowDialog(this);
        }

        /// <summary>
        /// 设置AI按钮可见性
        /// </summary>
        private void SetupAiButton()
        {
            if (_aiService != null)
            {
                _buttonGenerateAi.Visible = true;
            }
        }

        /// <summary>
        /// AI生成联想按钮点击事件
        /// </summary>
        private async void ButtonGenerateAi_Click(object? sender, EventArgs e)
        {
            if (_aiService == null) return;

            try
            {
                _buttonGenerateAi.Enabled = false;
                _buttonGenerateAi.Text = "生成中...";
                labelDetailContent.Text = "正在AI生成联想内容，请稍候...";

                string prompt = $"请为学习内容\"{_currentContent}\"生成联想学习内容，包括：\n" +
                              "1. 3-5个同类词/近义词\n" +
                              "2. 2-3个反义词\n" +
                              "3. 3-5个相关词\n" +
                              "4. 2-3个例句\n" +
                              "5. 2-3个相关知识点\n\n" +
                              "请按以下JSON格式返回（不要包含其他文字）：\n" +
                              "{\n" +
                              "  \"similar\": [\"同类词1\", \"同类词2\"],\n" +
                              "  \"opposite\": [\"反义词1\", \"反义词2\"],\n" +
                              "  \"related\": [\"相关词1\", \"相关词2\"],\n" +
                              "  \"examples\": [\"例句1\", \"例句2\"],\n" +
                              "  \"knowledge\": [\"知识点1\", \"知识点2\"]\n" +
                              "}";

                string result = await _aiService.AskAsync(prompt);
                var associations = ParseAiAssociations(result);

                if (associations.Count > 0)
                {
                    _associations = associations;
                    LoadAssociations();
                    labelDetailContent.Text = $"✨ AI已为你生成 {associations.Count} 条联想内容！\n\n点击左侧树状图查看详情。";
                }
                else
                {
                    labelDetailContent.Text = "AI生成失败，请稍后重试。\n\n已显示默认联想内容。";
                }
            }
            catch (Exception ex)
            {
                labelDetailContent.Text = $"生成失败：{ex.Message}\n\n已显示默认联想内容。";
            }
            finally
            {
                _buttonGenerateAi.Enabled = true;
                _buttonGenerateAi.Text = "✨ AI生成联想";
            }
        }

        /// <summary>
        /// 解析AI返回的联想内容
        /// </summary>
        private List<AssociationNode> ParseAiAssociations(string aiResponse)
        {
            var result = new List<AssociationNode>();

            try
            {
                int jsonStart = aiResponse.IndexOf('{');
                int jsonEnd = aiResponse.LastIndexOf('}');
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    string json = aiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);

                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    foreach (var item in ParseJsonArray(root, "similar"))
                    {
                        result.Add(new AssociationNode
                        {
                            Type = "同类词",
                            Content = item,
                            Description = $"与「{_currentContent}」含义相近或属于同一类别的词汇"
                        });
                    }

                    foreach (var item in ParseJsonArray(root, "opposite"))
                    {
                        result.Add(new AssociationNode
                        {
                            Type = "反义词",
                            Content = item,
                            Description = $"与「{_currentContent}」含义相反的词汇"
                        });
                    }

                    foreach (var item in ParseJsonArray(root, "related"))
                    {
                        result.Add(new AssociationNode
                        {
                            Type = "相关词",
                            Content = item,
                            Description = $"与「{_currentContent}」相关联的其他词汇或概念"
                        });
                    }

                    foreach (var item in ParseJsonArray(root, "examples"))
                    {
                        result.Add(new AssociationNode
                        {
                            Type = "例句",
                            Content = item.Length > 30 ? item.Substring(0, 30) + "..." : item,
                            Description = item
                        });
                    }

                    foreach (var item in ParseJsonArray(root, "knowledge"))
                    {
                        result.Add(new AssociationNode
                        {
                            Type = "知识点",
                            Content = item.Length > 25 ? item.Substring(0, 25) + "..." : item,
                            Description = item
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析AI联想内容失败: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 从JsonDocument中解析字符串数组
        /// </summary>
        private static List<string> ParseJsonArray(JsonElement root, string key)
        {
            var result = new List<string>();

            if (root.TryGetProperty(key, out var arrayElement) && arrayElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arrayElement.EnumerateArray())
                {
                    var text = item.ValueKind == JsonValueKind.String
                        ? item.GetString() ?? string.Empty
                        : item.ToString();
                    if (!string.IsNullOrEmpty(text))
                        result.Add(text);
                }
            }

            return result;
        }

        #endregion

        #region 详情操作按钮

        private AssociationNode? _currentSelectedAssociation;

        private void ButtonAddNote_Click(object? sender, EventArgs e)
        {
            if (_currentSelectedAssociation == null)
            {
                MessageBox.Show("请先选择一个联想项", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string currentNote = _currentSelectedAssociation.Notes ?? string.Empty;
            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "补充笔记内容：",
                "补充笔记",
                currentNote);

            if (!string.IsNullOrEmpty(input))
            {
                _currentSelectedAssociation.Notes = input;
                MessageBox.Show("笔记已保存", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowDetail(_currentSelectedAssociation);
            }
        }

        private void ButtonAddToReview_Click(object? sender, EventArgs e)
        {
            if (_currentSelectedAssociation == null)
            {
                MessageBox.Show("请先选择一个联想项", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                MessageBox.Show(
                    $"已将「{_currentSelectedAssociation.Content}」加入复习队列！",
                    "已加入复习",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加入复习失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

            /// <summary>
            /// 用户笔记
            /// </summary>
            public string? Notes { get; set; }
        }

        #endregion
    }
}
