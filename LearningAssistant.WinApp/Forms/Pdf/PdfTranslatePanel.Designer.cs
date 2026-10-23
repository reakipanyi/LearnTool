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
        root.Controls.Add(options, 0, 3);
        root.Controls.Add(extras, 0, 4);
        root.Dock = DockStyle.Fill;
        root.Location = new Point(0, 0);
        root.Margin = new Padding(0);
        root.Name = "root";
        root.Padding = new Padding(6, 2, 6, 6);
        root.RowCount = 5;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        root.Size = new Size(543, 504);
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
        body.Location = new Point(6, 52);
        body.Margin = new Padding(0, 2, 0, 2);
        body.Name = "body";
        body.RowCount = 1;
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        body.Size = new Size(531, 374);
        body.TabIndex = 1;
        // 
        // originalCard
        // 
        originalCard.ActionText = "🔊 读原文";
        originalCard.BackColor = Color.FromArgb(247, 248, 251);
        originalCard.Dock = DockStyle.Fill;
        originalCard.Location = new Point(0, 0);
        originalCard.Margin = new Padding(0, 0, 2, 0);
        originalCard.Name = "originalCard";
        originalCard.Padding = new Padding(2, 1, 2, 1);
        originalCard.Size = new Size(263, 374);
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
        originalBox.Size = new Size(243, 334);
        originalBox.TabIndex = 0;
        // 
        // translationCard
        // 
        translationCard.ActionText = "🔊 读译文";
        translationCard.BackColor = Color.FromArgb(247, 248, 251);
        translationCard.Dock = DockStyle.Fill;
        translationCard.Location = new Point(265, 0);
        translationCard.Margin = new Padding(2, 0, 0, 0);
        translationCard.Name = "translationCard";
        translationCard.Padding = new Padding(2, 1, 2, 1);
        translationCard.Size = new Size(266, 374);
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
        translationBox.Size = new Size(246, 334);
        translationBox.TabIndex = 0;
        // 
        // actions
        // 
        actions.BackColor = Color.FromArgb(247, 248, 251);
        actions.Controls.Add(ocrButton);
        actions.Controls.Add(shadowingButton);
        actions.Controls.Add(addToLearningButton);
        actions.Dock = DockStyle.Fill;
        actions.Location = new Point(6, 430);
        actions.Margin = new Padding(0, 2, 0, 0);
        actions.Name = "actions";
        actions.Padding = new Padding(0, 5, 0, 0);
        actions.Size = new Size(531, 38);
        actions.TabIndex = 2;
        actions.WrapContents = false;
        // 
        // ocrButton
        // 
        ocrButton.BackColor = Color.White;
        ocrButton.Cursor = Cursors.Hand;
        ocrButton.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        ocrButton.FlatAppearance.BorderSize = 1;
        ocrButton.FlatStyle = FlatStyle.Flat;
        ocrButton.Font = new Font("Microsoft YaHei UI", 9F);
        ocrButton.ForeColor = Color.FromArgb(15, 23, 42);
        ocrButton.Margin = new Padding(0, 0, 6, 0);
        ocrButton.Name = "ocrButton";
        ocrButton.Size = new Size(92, 28);
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
        shadowingButton.FlatStyle = FlatStyle.Flat;
        shadowingButton.Font = new Font("Microsoft YaHei UI", 9F);
        shadowingButton.ForeColor = Color.FromArgb(15, 23, 42);
        shadowingButton.Margin = new Padding(0, 0, 6, 0);
        shadowingButton.Name = "shadowingButton";
        shadowingButton.Size = new Size(96, 28);
        shadowingButton.TabIndex = 0;
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
        addToLearningButton.FlatStyle = FlatStyle.Flat;
        addToLearningButton.Font = new Font("Microsoft YaHei UI", 9F);
        addToLearningButton.ForeColor = Color.FromArgb(15, 23, 42);
        addToLearningButton.Name = "addToLearningButton";
        addToLearningButton.Size = new Size(100, 28);
        addToLearningButton.TabIndex = 1;
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
        options.Controls.Add(speedSelector);
        options.Dock = DockStyle.Fill;
        options.Location = new Point(6, 468);
        options.Margin = new Padding(0);
        options.Name = "options";
        options.Size = new Size(531, 30);
        options.TabIndex = 3;
        options.WrapContents = false;
        // 
        // autoTranslateCheck
        // 
        autoTranslateCheck.AutoSize = true;
        autoTranslateCheck.BackColor = Color.Transparent;
        autoTranslateCheck.FlatStyle = FlatStyle.System;
        autoTranslateCheck.ForeColor = Color.FromArgb(15, 23, 42);
        autoTranslateCheck.Margin = new Padding(2, 4, 6, 0);
        autoTranslateCheck.Name = "autoTranslateCheck";
        autoTranslateCheck.Size = new Size(115, 21);
        autoTranslateCheck.TabIndex = 0;
        autoTranslateCheck.Text = "🌐 识别后翻译";
        // 
        // autoSpeakCheck
        // 
        autoSpeakCheck.AutoSize = true;
        autoSpeakCheck.BackColor = Color.Transparent;
        autoSpeakCheck.FlatStyle = FlatStyle.System;
        autoSpeakCheck.ForeColor = Color.FromArgb(15, 23, 42);
        autoSpeakCheck.Margin = new Padding(2, 4, 6, 0);
        autoSpeakCheck.Name = "autoSpeakCheck";
        autoSpeakCheck.Size = new Size(115, 21);
        autoSpeakCheck.TabIndex = 1;
        autoSpeakCheck.Text = "🔊 识别后朗读";
        // 
        // aiPolishCheck
        // 
        aiPolishCheck.AutoSize = true;
        aiPolishCheck.BackColor = Color.Transparent;
        aiPolishCheck.FlatStyle = FlatStyle.System;
        aiPolishCheck.ForeColor = Color.FromArgb(15, 23, 42);
        aiPolishCheck.Margin = new Padding(2, 4, 2, 0);
        aiPolishCheck.Name = "aiPolishCheck";
        aiPolishCheck.Size = new Size(79, 21);
        aiPolishCheck.TabIndex = 2;
        aiPolishCheck.Text = "✨ 润色";
        // 
        // speedSelector
        // 
        speedSelector.BackColor = Color.Transparent;
        speedSelector.Location = new Point(319, 1);
        speedSelector.Margin = new Padding(2, 0, 0, 0);
        speedSelector.Name = "speedSelector";
        speedSelector.Size = new Size(130, 28);
        speedSelector.TabIndex = 3;
        // 
        // extras
        // 
        extras.BackColor = Color.FromArgb(247, 248, 251);
        extras.Controls.Add(targetLanguageLabel);
        extras.Controls.Add(targetLanguageCombo);
        extras.Controls.Add(glossaryButton);
        extras.Dock = DockStyle.Fill;
        extras.Location = new Point(6, 470);
        extras.Margin = new Padding(0, 2, 0, 0);
        extras.Name = "extras";
        extras.Padding = new Padding(0, 2, 0, 0);
        extras.Size = new Size(531, 30);
        extras.TabIndex = 4;
        extras.WrapContents = false;
        // 
        // targetLanguageLabel
        // 
        targetLanguageLabel.AutoSize = true;
        targetLanguageLabel.ForeColor = Color.FromArgb(50, 70, 90);
        targetLanguageLabel.Location = new Point(2, 6);
        targetLanguageLabel.Margin = new Padding(2, 5, 4, 0);
        targetLanguageLabel.Name = "targetLanguageLabel";
        targetLanguageLabel.Size = new Size(32, 17);
        targetLanguageLabel.TabIndex = 0;
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
        targetLanguageCombo.Margin = new Padding(2, 2, 0, 0);
        targetLanguageCombo.Name = "targetLanguageCombo";
        targetLanguageCombo.Size = new Size(112, 25);
        targetLanguageCombo.TabIndex = 1;
        // 
        // glossaryButton
        // 
        glossaryButton.BackColor = Color.White;
        glossaryButton.Cursor = Cursors.Hand;
        glossaryButton.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        glossaryButton.FlatAppearance.BorderSize = 1;
        glossaryButton.FlatStyle = FlatStyle.Flat;
        glossaryButton.Font = new Font("Microsoft YaHei UI", 9F);
        glossaryButton.ForeColor = Color.FromArgb(15, 23, 42);
        glossaryButton.Margin = new Padding(6, 3, 0, 0);
        glossaryButton.Name = "glossaryButton";
        glossaryButton.Size = new Size(88, 26);
        glossaryButton.TabIndex = 2;
        glossaryButton.Tag = "secondary";
        glossaryButton.Text = "📖 术语表";
        glossaryButton.UseVisualStyleBackColor = false;
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
    private ToolTip _tip;
}
