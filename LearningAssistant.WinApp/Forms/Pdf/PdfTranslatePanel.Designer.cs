using LearningAssistant.Common.UI;
using LearningAssistant.Forms.UserControls.Common;

namespace LearningAssistant.Forms.Pdf;

partial class PdfTranslatePanel
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Component Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        root = new TableLayoutPanel();
        header = new SidebarHeaderBar();
        body = new TableLayoutPanel();
        originalCard = new SidebarCard();
        originalBox = new SyncedTextBox();
        translationCard = new SidebarCard();
        translationBox = new SyncedTextBox();
        actions = new FlowLayoutPanel();
        ocrButton = new Button();
        shadowingButton = new Button();
        addToLearningButton = new Button();
        options = new FlowLayoutPanel();
        autoTranslateCheck = new CheckBox();
        autoSpeakCheck = new CheckBox();
        aiPolishCheck = new CheckBox();
        speedSelector = new SpeedSelectorControl();
        extras = new FlowLayoutPanel();
        targetLanguageLabel = new Label();
        targetLanguageCombo = new ComboBox();
        glossaryButton = new Button();
        reTranslateButton = new Button();
        speedLabel = new Label();
        _tip = new ToolTip(components);
        root.SuspendLayout();
        body.SuspendLayout();
        actions.SuspendLayout();
        options.SuspendLayout();
        extras.SuspendLayout();
        SuspendLayout();
        // 
        // root
        // 
        root.BackColor = Color.FromArgb(247, 248, 251);
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(body, 0, 1);
        root.Controls.Add(actions, 0, 2);
        root.Controls.Add(extras, 0, 3);
        root.Controls.Add(options, 0, 4);
        root.Dock = DockStyle.Fill;
        root.Location = new Point(0, 0);
        root.Margin = new Padding(0);
        root.Name = "root";
        root.Padding = new Padding(10, 2, 10, 8);
        root.RowCount = 5;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        root.Size = new Size(543, 526);
        root.TabIndex = 0;
        // 
        // header
        // 
        header.ActionText = "翻译";
        header.BackColor = Color.FromArgb(247, 248, 251);
        header.Dock = DockStyle.Fill;
        header.Icon = "🌐";
        header.Location = new Point(6, 2);
        header.Margin = new Padding(0);
        header.Name = "header";
        header.Padding = new Padding(2, 0, 2, 0);
        header.Size = new Size(531, 48);
        header.TabIndex = 0;
        header.TitleText = "翻译";
        // 
        // body
        // 
        body.BackColor = Color.FromArgb(247, 248, 251);
        body.ColumnCount = 2;
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        body.Controls.Add(originalCard, 0, 0);
        body.Controls.Add(translationCard, 1, 0);
        body.Dock = DockStyle.Fill;
        body.Location = new Point(10, 52);
        body.Margin = new Padding(0, 2, 0, 2);
        body.Name = "body";
        body.RowCount = 1;
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        body.Size = new Size(523, 396);
        body.TabIndex = 1;
        // 
        // originalCard
        // 
        originalCard.ActionText = "🔊 播放原文";
        originalCard.BackColor = Color.FromArgb(247, 248, 251);
        originalCard.Dock = DockStyle.Fill;
        originalCard.Location = new Point(0, 0);
        originalCard.Margin = new Padding(0, 0, 3, 0);
        originalCard.Name = "originalCard";
        originalCard.Padding = new Padding(2, 1, 2, 1);
        originalCard.Size = new Size(259, 396);
        originalCard.TabIndex = 0;
        originalCard.Title = "原文";
        // 
        // originalBox
        // 
        originalBox.BackColor = Color.White;
        originalBox.BorderStyle = BorderStyle.None;
        originalBox.Dock = DockStyle.Fill;
        originalBox.Font = new Font("Microsoft YaHei UI", 9.5F);
        originalBox.ForeColor = Color.FromArgb(15, 23, 42);
        originalBox.Location = new Point(10, 30);
        originalBox.Multiline = true;
        originalBox.Name = "originalBox";
        originalBox.ScrollBars = ScrollBars.Vertical;
        originalBox.Size = new Size(239, 356);
        originalBox.TabIndex = 0;
        // 
        // translationCard
        // 
        translationCard.ActionText = "🔊 播放译文";
        translationCard.BackColor = Color.FromArgb(247, 248, 251);
        translationCard.Dock = DockStyle.Fill;
        translationCard.Location = new Point(262, 0);
        translationCard.Margin = new Padding(3, 0, 0, 0);
        translationCard.Name = "translationCard";
        translationCard.Padding = new Padding(2, 1, 2, 1);
        translationCard.Size = new Size(261, 396);
        translationCard.TabIndex = 1;
        translationCard.Title = "译文";
        // 
        // translationBox
        // 
        translationBox.BackColor = Color.White;
        translationBox.BorderStyle = BorderStyle.None;
        translationBox.Dock = DockStyle.Fill;
        translationBox.Font = new Font("Microsoft YaHei UI", 9.5F);
        translationBox.ForeColor = Color.FromArgb(15, 23, 42);
        translationBox.Location = new Point(10, 30);
        translationBox.Multiline = true;
        translationBox.Name = "translationBox";
        translationBox.ScrollBars = ScrollBars.Vertical;
        translationBox.Size = new Size(241, 356);
        translationBox.TabIndex = 0;
        // 
        // actions
        // 
        actions.BackColor = Color.FromArgb(247, 248, 251);
        actions.Controls.Add(ocrButton);
        actions.Controls.Add(shadowingButton);
        actions.Controls.Add(addToLearningButton);
        actions.Dock = DockStyle.Fill;
        actions.Location = new Point(10, 452);
        actions.Margin = new Padding(0, 4, 0, 0);
        actions.Name = "actions";
        actions.Padding = new Padding(0, 5, 0, 0);
        actions.Size = new Size(523, 34);
        actions.TabIndex = 2;
        actions.WrapContents = false;
        // 
        // ocrButton
        // 
        ocrButton.BackColor = Color.White;
        ocrButton.Cursor = Cursors.Hand;
        ocrButton.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        ocrButton.FlatAppearance.BorderSize = 1;
        ocrButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        ocrButton.FlatStyle = FlatStyle.Flat;
        ocrButton.Font = new Font("Microsoft YaHei UI", 9F);
        ocrButton.ForeColor = Color.FromArgb(15, 23, 42);
        ocrButton.Margin = new Padding(0, 0, 8, 0);
        ocrButton.Name = "ocrButton";
        ocrButton.Size = new Size(96, 28);
        ocrButton.TabIndex = 0;
        ocrButton.Tag = "secondary";
        ocrButton.Text = "🔍 整页识别";
        ocrButton.UseVisualStyleBackColor = false;
        // 
        // shadowingButton
        // 
        shadowingButton.BackColor = Color.White;
        shadowingButton.Cursor = Cursors.Hand;
        shadowingButton.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        shadowingButton.FlatAppearance.BorderSize = 1;
        shadowingButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        shadowingButton.FlatStyle = FlatStyle.Flat;
        shadowingButton.Font = new Font("Microsoft YaHei UI", 9F);
        shadowingButton.ForeColor = Color.FromArgb(15, 23, 42);
        shadowingButton.Margin = new Padding(0, 0, 8, 0);
        shadowingButton.Name = "shadowingButton";
        shadowingButton.Size = new Size(104, 28);
        shadowingButton.TabIndex = 1;
        shadowingButton.Tag = "secondary";
        shadowingButton.Text = "✂ 句子跟读";
        shadowingButton.UseVisualStyleBackColor = false;
        // 
        // addToLearningButton
        // 
        addToLearningButton.BackColor = Color.White;
        addToLearningButton.Cursor = Cursors.Hand;
        addToLearningButton.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        addToLearningButton.FlatAppearance.BorderSize = 1;
        addToLearningButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        addToLearningButton.FlatStyle = FlatStyle.Flat;
        addToLearningButton.Font = new Font("Microsoft YaHei UI", 9F);
        addToLearningButton.ForeColor = Color.FromArgb(15, 23, 42);
        addToLearningButton.Margin = new Padding(0);
        addToLearningButton.Name = "addToLearningButton";
        addToLearningButton.Size = new Size(108, 28);
        addToLearningButton.TabIndex = 2;
        addToLearningButton.Tag = "secondary";
        addToLearningButton.Text = "➕ 加入学习";
        addToLearningButton.UseVisualStyleBackColor = false;
        // 
        // options
        // 
        options.BackColor = Color.FromArgb(247, 248, 251);
        options.Controls.Add(autoTranslateCheck);
        options.Controls.Add(autoSpeakCheck);
        options.Controls.Add(aiPolishCheck);
        options.Dock = DockStyle.Fill;
        options.Location = new Point(10, 524);
        options.Margin = new Padding(0, 2, 0, 0);
        options.Name = "options";
        options.Padding = new Padding(0, 4, 0, 0);
        options.Size = new Size(523, 28);
        options.TabIndex = 4;
        options.WrapContents = false;
        // 
        // autoTranslateCheck
        // 
        autoTranslateCheck.AutoSize = true;
        autoTranslateCheck.BackColor = Color.Transparent;
        autoTranslateCheck.FlatStyle = FlatStyle.System;
        autoTranslateCheck.ForeColor = Color.FromArgb(15, 23, 42);
        autoTranslateCheck.Margin = new Padding(2, 4, 20, 0);
        autoTranslateCheck.Name = "autoTranslateCheck";
        autoTranslateCheck.Size = new Size(79, 21);
        autoTranslateCheck.TabIndex = 0;
        autoTranslateCheck.Text = "自动翻译";
        // 
        // autoSpeakCheck
        // 
        autoSpeakCheck.AutoSize = true;
        autoSpeakCheck.BackColor = Color.Transparent;
        autoSpeakCheck.FlatStyle = FlatStyle.System;
        autoSpeakCheck.ForeColor = Color.FromArgb(15, 23, 42);
        autoSpeakCheck.Margin = new Padding(0, 4, 20, 0);
        autoSpeakCheck.Name = "autoSpeakCheck";
        autoSpeakCheck.Size = new Size(79, 21);
        autoSpeakCheck.TabIndex = 1;
        autoSpeakCheck.Text = "自动朗读";
        // 
        // aiPolishCheck
        // 
        aiPolishCheck.AutoSize = true;
        aiPolishCheck.BackColor = Color.Transparent;
        aiPolishCheck.FlatStyle = FlatStyle.System;
        aiPolishCheck.ForeColor = Color.FromArgb(15, 23, 42);
        aiPolishCheck.Margin = new Padding(0, 4, 0, 0);
        aiPolishCheck.Name = "aiPolishCheck";
        aiPolishCheck.Size = new Size(70, 21);
        aiPolishCheck.TabIndex = 2;
        aiPolishCheck.Text = "AI 润色";
        // 
        // extras
        // 
        extras.BackColor = Color.FromArgb(247, 248, 251);
        extras.Controls.Add(targetLanguageLabel);
        extras.Controls.Add(targetLanguageCombo);
        extras.Controls.Add(glossaryButton);
        extras.Controls.Add(reTranslateButton);
        extras.Controls.Add(speedLabel);
        extras.Controls.Add(speedSelector);
        extras.Dock = DockStyle.Fill;
        extras.Location = new Point(10, 494);
        extras.Margin = new Padding(0, 2, 0, 0);
        extras.Name = "extras";
        extras.Padding = new Padding(0, 4, 0, 0);
        extras.Size = new Size(523, 28);
        extras.TabIndex = 3;
        extras.WrapContents = false;
        // 
        // targetLanguageLabel
        // 
        targetLanguageLabel.AutoSize = true;
        targetLanguageLabel.ForeColor = Color.FromArgb(50, 70, 90);
        targetLanguageLabel.Location = new Point(2, 8);
        targetLanguageLabel.Margin = new Padding(0, 8, 6, 0);
        targetLanguageLabel.Name = "targetLanguageLabel";
        targetLanguageLabel.Size = new Size(32, 17);
        targetLanguageLabel.TabIndex = 0;
        targetLanguageLabel.Tag = "muted";
        targetLanguageLabel.Text = "译至";
        // 
        // targetLanguageCombo
        // 
        targetLanguageCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        targetLanguageCombo.FlatStyle = FlatStyle.Flat;
        targetLanguageCombo.Font = new Font("Microsoft YaHei UI", 9F);
        targetLanguageCombo.ForeColor = Color.FromArgb(15, 23, 42);
        targetLanguageCombo.FormattingEnabled = true;
        targetLanguageCombo.Location = new Point(40, 3);
        targetLanguageCombo.Margin = new Padding(0, 5, 10, 0);
        targetLanguageCombo.Name = "targetLanguageCombo";
        targetLanguageCombo.Size = new Size(108, 25);
        targetLanguageCombo.TabIndex = 1;
        // 
        // glossaryButton
        // 
        glossaryButton.BackColor = Color.White;
        glossaryButton.Cursor = Cursors.Hand;
        glossaryButton.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        glossaryButton.FlatAppearance.BorderSize = 1;
        glossaryButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        glossaryButton.FlatStyle = FlatStyle.Flat;
        glossaryButton.Font = new Font("Microsoft YaHei UI", 9F);
        glossaryButton.ForeColor = Color.FromArgb(15, 23, 42);
        glossaryButton.Margin = new Padding(0, 4, 6, 0);
        glossaryButton.Name = "glossaryButton";
        glossaryButton.Size = new Size(84, 28);
        glossaryButton.TabIndex = 2;
        glossaryButton.Tag = "secondary";
        glossaryButton.Text = "📖 术语表";
        glossaryButton.UseVisualStyleBackColor = false;
        // 
        // reTranslateButton
        // 
        reTranslateButton.BackColor = Color.White;
        reTranslateButton.Cursor = Cursors.Hand;
        reTranslateButton.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        reTranslateButton.FlatAppearance.BorderSize = 1;
        reTranslateButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        reTranslateButton.FlatStyle = FlatStyle.Flat;
        reTranslateButton.Font = new Font("Microsoft YaHei UI", 9F);
        reTranslateButton.ForeColor = Color.FromArgb(15, 23, 42);
        reTranslateButton.Margin = new Padding(0, 4, 16, 0);
        reTranslateButton.Name = "reTranslateButton";
        reTranslateButton.Size = new Size(96, 28);
        reTranslateButton.TabIndex = 3;
        reTranslateButton.Tag = "secondary";
        reTranslateButton.Text = "🔄 重新翻译";
        reTranslateButton.UseVisualStyleBackColor = false;
        // 
        // speedLabel
        // 
        speedLabel.AutoSize = true;
        speedLabel.ForeColor = Color.FromArgb(50, 70, 90);
        speedLabel.Location = new Point(418, 8);
        speedLabel.Margin = new Padding(0, 8, 2, 0);
        speedLabel.Name = "speedLabel";
        speedLabel.Size = new Size(32, 17);
        speedLabel.TabIndex = 4;
        speedLabel.Tag = "muted";
        speedLabel.Text = "语速";
        // 
        // speedSelector
        // 
        speedSelector.BackColor = Color.Transparent;
        speedSelector.Location = new Point(428, 4);
        speedSelector.Margin = new Padding(0, 4, 0, 0);
        speedSelector.Name = "speedSelector";
        speedSelector.Size = new Size(130, 28);
        speedSelector.TabIndex = 5;
        // 
        // PdfTranslatePanel
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(247, 248, 251);
        Controls.Add(root);
        Name = "PdfTranslatePanel";
        Size = new Size(555, 508);
        root.ResumeLayout(false);
        body.ResumeLayout(false);
        actions.ResumeLayout(false);
        options.ResumeLayout(false);
        options.PerformLayout();
        extras.ResumeLayout(false);
        extras.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel root;
    private SidebarHeaderBar header;
    private TableLayoutPanel body;
    private SidebarCard originalCard;
    private SyncedTextBox originalBox;
    private SidebarCard translationCard;
    private SyncedTextBox translationBox;
    private FlowLayoutPanel actions;
    private Button ocrButton;
    private Button shadowingButton;
    private Button addToLearningButton;
    private FlowLayoutPanel options;
    private CheckBox autoTranslateCheck;
    private CheckBox autoSpeakCheck;
    private CheckBox aiPolishCheck;
    private SpeedSelectorControl speedSelector;
    private FlowLayoutPanel extras;
    private Label targetLanguageLabel;
    private ComboBox targetLanguageCombo;
    private Button glossaryButton;
    private Button reTranslateButton;
    private Label speedLabel;
    private ToolTip _tip;
}
