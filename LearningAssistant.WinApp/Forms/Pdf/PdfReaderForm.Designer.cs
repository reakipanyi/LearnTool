using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Common.UI;
using LearningAssistant.Forms.UserControls.Common;
using LearningAssistant.Managers;
using LearningAssistant.Models.Config;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.Learning.Status;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Models.Practice;
using LearningAssistant.Presenters;
using LearningAssistant.Services;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Media;
using LearningAssistant.Services.Pdf;
using LearningAssistant.Services.Practice;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace LearningAssistant.Forms.Pdf
{
    partial class PdfReaderForm
    {
#nullable disable

        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new Container();
            _splitContainerMain = new SplitContainer();
            _panelPdf = new Panel();
            _panelNavigation = new Panel();
            _toolbarGroupTools = new Panel();
            _buttonSpotlightMode = new Button();
            _buttonTemporaryPenMode = new Button();
            _loadingIndicator = new LoadingIndicator();
            _buttonImageEmbedMode = new Button();
            panel2 = new Panel();
            _buttonPenTypePencil = new Button();
            _buttonPenTypeMarker = new Button();
            _buttonPenTypePen = new Button();
            _buttonChecklistMode = new Button();
            _buttonCollapseNav = new Button();
            _buttonExportAnnotationsAsImage = new Button();
            _toolbarGroupView = new Panel();
            _trackBarZoom = new TrackBar();
            _buttonDualPage = new Button();
            _buttonResetView = new Button();
            _buttonZoomOut = new Button();
            _buttonFullscreen = new Button();
            _buttonRotate = new Button();
            _labelZoom = new Label();
            _buttonZoomIn = new Button();
            _buttonLockView = new Button();
            _buttonStrikethroughMode = new Button();
            _buttonNightMode = new Button();
            _buttonToggleAnnotationLayer = new Button();
            _buttonAskAi = new Button();
            _buttonTranslationToggle = new Button();
            _buttonClearAllAnnotations = new Button();
            _buttonMagnifierMode = new Button();
            _buttonArrowMode = new Button();
            pnlPage = new Panel();
            _buttonOpenFolder = new Button();
            _buttonPrev = new Button();
            _buttonPrevSpread = new Button();
            _progressBarPage = new ProgressBar();
            _textBoxPage = new TextBox();
            _buttonNext = new Button();
            _buttonNextSpread = new Button();
            _labelPageCount = new Label();
            _panelAnnotationOptions = new Panel();
            _buttonDashStyle = new Button();
            _trackBarThickness = new TrackBar();
            _labelThicknessValue = new Label();
            _panelColor = new Panel();
            _buttonColorBlack = new Button();
            _buttonColorBlue = new Button();
            _buttonColorGreen = new Button();
            _buttonColorOrange = new Button();
            _buttonColorRed = new Button();
            _buttonColorPurple = new Button();
            _buttonColorCyan = new Button();
            _buttonColorTeal = new Button();
            _buttonColorPink = new Button();
            _buttonColorBrown = new Button();
            _buttonColorGray = new Button();
            _buttonColorYellow = new Button();
            _buttonColorLime = new Button();
            _buttonColorVioletRed = new Button();
            _buttonColorDodgerBlue = new Button();
            _buttonColorMore = new Button();
            _buttonHighlightColor0 = new Button();
            _buttonHighlightColor1 = new Button();
            _buttonHighlightColor2 = new Button();
            _buttonHighlightColor3 = new Button();
            _buttonHighlightColor4 = new Button();
            _buttonUndoAnnotation = new Button();
            _buttonHighlightMode = new Button();
            _buttonRectangleMode = new Button();
            _buttonEllipseMode = new Button();
            _buttonPenMode = new Button();
            _buttonMosaicMode = new Button();
            _buttonTextMode = new Button();
            _buttonEraserMode = new Button();
            _buttonMoreTools = new Button();
            _statusBar = new Panel();
            _statusLabelLeft = new Label();
            _statusLabelRight = new Label();
            _pictureBoxPdf = new BufferedPictureBox();
            _contextMenuPdf = new ContextMenuStrip(components);
            menuItemCopy = new ToolStripMenuItem();
            menuItemSearch = new ToolStripMenuItem();
            menuItemAnnotationSeparator = new ToolStripSeparator();
            menuItemHighlight = new ToolStripMenuItem();
            menuItemRectangle = new ToolStripMenuItem();
            menuItemText = new ToolStripMenuItem();
            menuItemZoomSeparator = new ToolStripSeparator();
            menuItemZoomIn = new ToolStripMenuItem();
            menuItemZoomOut = new ToolStripMenuItem();
            menuItemResetZoom = new ToolStripMenuItem();
            menuItemExport = new ToolStripMenuItem();
            menuItemRotationSeparator = new ToolStripSeparator();
            menuItemRotateLeft = new ToolStripMenuItem();
            menuItemRotateRight = new ToolStripMenuItem();
            menuItemResetRotation = new ToolStripMenuItem();
            _ocrPanel = new Panel();
            _ocrPictureBox = new PictureBox();
            _ocrCloseButton = new Button();
            _pageTransitionOverlay = new Panel();
            _transitionLabel = new Label();
            _panelLeftContainer = new Panel();
            _tabControlLeft = new BufferedTabControl();
            _tabPageFiles = new TabPage();
            _treeViewFiles = new TreeView();
            _textBoxFilter = new TextBox();
            _tabPageThumbnails = new TabPage();
            _panelThumbnails = new Panel();
            _flowLayoutPanelThumbnails = new FlowLayoutPanel();
            _tabPageTranslate = new TabPage();
            _tabPageStudy = new TabPage();
            _tabPageBookmarksAndHighlights = new TabPage();
            _tabPageAudio = new TabPage();
            _toolTip = new ToolTip(components);
            moreMenu = new ContextMenuStrip(components);
            _toastLabel = new Label();
            _pageTransitionTimer = new System.Windows.Forms.Timer(components);
            ((ISupportInitialize)_splitContainerMain).BeginInit();
            _splitContainerMain.Panel1.SuspendLayout();
            _splitContainerMain.Panel2.SuspendLayout();
            _splitContainerMain.SuspendLayout();
            _panelPdf.SuspendLayout();
            _panelNavigation.SuspendLayout();
            _toolbarGroupTools.SuspendLayout();
            panel2.SuspendLayout();
            _toolbarGroupView.SuspendLayout();
            ((ISupportInitialize)_trackBarZoom).BeginInit();
            pnlPage.SuspendLayout();
            _panelAnnotationOptions.SuspendLayout();
            ((ISupportInitialize)_trackBarThickness).BeginInit();
            _panelColor.SuspendLayout();
            _statusBar.SuspendLayout();
            ((ISupportInitialize)_pictureBoxPdf).BeginInit();
            _contextMenuPdf.SuspendLayout();
            _ocrPanel.SuspendLayout();
            ((ISupportInitialize)_ocrPictureBox).BeginInit();
            _pageTransitionOverlay.SuspendLayout();
            _panelLeftContainer.SuspendLayout();
            _tabControlLeft.SuspendLayout();
            _tabPageFiles.SuspendLayout();
            _tabPageThumbnails.SuspendLayout();
            _panelThumbnails.SuspendLayout();
            _tabPageTranslate.SuspendLayout();
            _tabPageBookmarksAndHighlights.SuspendLayout();
            SuspendLayout();
            // 
            // _splitContainerMain
            // 
            _splitContainerMain.Dock = DockStyle.Fill;
            _splitContainerMain.FixedPanel = FixedPanel.Panel2;
            _splitContainerMain.Location = new Point(0, 0);
            _splitContainerMain.Name = "_splitContainerMain";
            // 
            // _splitContainerMain.Panel1
            // 
            _splitContainerMain.Panel1.Controls.Add(_panelPdf);
            // 
            // _splitContainerMain.Panel2
            // 
            _splitContainerMain.Panel2.Controls.Add(_panelLeftContainer);
            _splitContainerMain.Size = new Size(1863, 1034);
            _splitContainerMain.SplitterDistance = 1296;
            _splitContainerMain.TabIndex = 0;
            // 
            // _panelPdf
            // 
            _panelPdf.BackColor = Color.FromArgb(240, 240, 240);
            _panelPdf.Controls.Add(_panelNavigation);
            _panelPdf.Controls.Add(_statusBar);
            _panelPdf.Controls.Add(_pictureBoxPdf);
            _panelPdf.Controls.Add(_ocrPanel);
            _panelPdf.Controls.Add(_pageTransitionOverlay);
            _panelPdf.Dock = DockStyle.Fill;
            _panelPdf.Location = new Point(0, 0);
            _panelPdf.Name = "_panelPdf";
            _panelPdf.Size = new Size(1296, 1034);
            _panelPdf.TabIndex = 1;
            // 
            // _panelNavigation
            // 
            _panelNavigation.AutoScroll = true;
            _panelNavigation.BackColor = Color.White;
            _panelNavigation.Controls.Add(_toolbarGroupTools);
            _panelNavigation.Dock = DockStyle.Top;
            _panelNavigation.Location = new Point(0, 0);
            _panelNavigation.Name = "_panelNavigation";
            _panelNavigation.Padding = new Padding(8);
            _panelNavigation.Size = new Size(1296, 87);
            _panelNavigation.TabIndex = 3;
            // 
            // _toolbarGroupTools
            // 
            _toolbarGroupTools.Controls.Add(_buttonSpotlightMode);
            _toolbarGroupTools.Controls.Add(_buttonTemporaryPenMode);
            _toolbarGroupTools.Controls.Add(_loadingIndicator);
            _toolbarGroupTools.Controls.Add(_buttonImageEmbedMode);
            _toolbarGroupTools.Controls.Add(panel2);
            _toolbarGroupTools.Controls.Add(_buttonChecklistMode);
            _toolbarGroupTools.Controls.Add(_buttonCollapseNav);
            _toolbarGroupTools.Controls.Add(_buttonExportAnnotationsAsImage);
            _toolbarGroupTools.Controls.Add(_toolbarGroupView);
            _toolbarGroupTools.Controls.Add(_buttonStrikethroughMode);
            _toolbarGroupTools.Controls.Add(_buttonNightMode);
            _toolbarGroupTools.Controls.Add(_buttonToggleAnnotationLayer);
            _toolbarGroupTools.Controls.Add(_buttonAskAi);
            _toolbarGroupTools.Controls.Add(_buttonTranslationToggle);
            _toolbarGroupTools.Controls.Add(_buttonClearAllAnnotations);
            _toolbarGroupTools.Controls.Add(_buttonMagnifierMode);
            _toolbarGroupTools.Controls.Add(_buttonArrowMode);
            _toolbarGroupTools.Controls.Add(pnlPage);
            _toolbarGroupTools.Controls.Add(_panelAnnotationOptions);
            _toolbarGroupTools.Controls.Add(_buttonUndoAnnotation);
            _toolbarGroupTools.Controls.Add(_buttonHighlightMode);
            _toolbarGroupTools.Controls.Add(_buttonRectangleMode);
            _toolbarGroupTools.Controls.Add(_buttonEllipseMode);
            _toolbarGroupTools.Controls.Add(_buttonPenMode);
            _toolbarGroupTools.Controls.Add(_buttonMosaicMode);
            _toolbarGroupTools.Controls.Add(_buttonTextMode);
            _toolbarGroupTools.Controls.Add(_buttonEraserMode);
            _toolbarGroupTools.Controls.Add(_buttonMoreTools);
            _toolbarGroupTools.Dock = DockStyle.Fill;
            _toolbarGroupTools.Location = new Point(8, 8);
            _toolbarGroupTools.Name = "_toolbarGroupTools";
            _toolbarGroupTools.Size = new Size(1280, 71);
            _toolbarGroupTools.TabIndex = 3;
            // 
            // _buttonSpotlightMode
            // 
            _buttonSpotlightMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonSpotlightMode.FlatStyle = FlatStyle.Flat;
            _buttonSpotlightMode.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonSpotlightMode.Location = new Point(1446, 6);
            _buttonSpotlightMode.Name = "_buttonSpotlightMode";
            _buttonSpotlightMode.Size = new Size(28, 31);
            _buttonSpotlightMode.TabIndex = 24;
            _buttonSpotlightMode.Text = "🔦";
            _toolTip.SetToolTip(_buttonSpotlightMode, "聚光灯 (K)");
            _buttonSpotlightMode.UseVisualStyleBackColor = false;
            _buttonSpotlightMode.Click += ButtonSpotlightMode_Click;
            // 
            // _buttonTemporaryPenMode
            // 
            _buttonTemporaryPenMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonTemporaryPenMode.FlatStyle = FlatStyle.Flat;
            _buttonTemporaryPenMode.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonTemporaryPenMode.Location = new Point(1252, 6);
            _buttonTemporaryPenMode.Name = "_buttonTemporaryPenMode";
            _buttonTemporaryPenMode.Size = new Size(28, 31);
            _buttonTemporaryPenMode.TabIndex = 26;
            _buttonTemporaryPenMode.Text = "🖊";
            _toolTip.SetToolTip(_buttonTemporaryPenMode, "临时笔 (一次性，笔迹2秒后自动消失)");
            _buttonTemporaryPenMode.UseVisualStyleBackColor = false;
            _buttonTemporaryPenMode.Click += ButtonTemporaryPenMode_Click;
            // 
            // _loadingIndicator
            // 
            _loadingIndicator.BackColor = Color.FromArgb(245, 245, 245);
            _loadingIndicator.ForeColor = Color.FromArgb(66, 133, 244);
            _loadingIndicator.Location = new Point(1195, 3);
            _loadingIndicator.Name = "_loadingIndicator";
            _loadingIndicator.Size = new Size(38, 36);
            _loadingIndicator.TabIndex = 5;
            _loadingIndicator.Visible = false;
            _loadingIndicator.Click += _loadingIndicator_Click;
            // 
            // _buttonImageEmbedMode
            // 
            _buttonImageEmbedMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonImageEmbedMode.FlatStyle = FlatStyle.Flat;
            _buttonImageEmbedMode.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonImageEmbedMode.Location = new Point(1414, 6);
            _buttonImageEmbedMode.Name = "_buttonImageEmbedMode";
            _buttonImageEmbedMode.Size = new Size(28, 31);
            _buttonImageEmbedMode.TabIndex = 23;
            _buttonImageEmbedMode.Text = "🖼";
            _toolTip.SetToolTip(_buttonImageEmbedMode, "嵌入图片 (I)");
            _buttonImageEmbedMode.UseVisualStyleBackColor = false;
            _buttonImageEmbedMode.Click += ButtonImageEmbedMode_Click;
            // 
            // panel2
            // 
            panel2.Controls.Add(_buttonPenTypePencil);
            panel2.Controls.Add(_buttonPenTypeMarker);
            panel2.Controls.Add(_buttonPenTypePen);
            panel2.Location = new Point(574, 37);
            panel2.Name = "panel2";
            panel2.Size = new Size(104, 32);
            panel2.TabIndex = 31;
            // 
            // _buttonPenTypePencil
            // 
            _buttonPenTypePencil.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonPenTypePencil.FlatStyle = FlatStyle.Flat;
            _buttonPenTypePencil.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonPenTypePencil.Location = new Point(3, 0);
            _buttonPenTypePencil.Name = "_buttonPenTypePencil";
            _buttonPenTypePencil.Size = new Size(28, 32);
            _buttonPenTypePencil.TabIndex = 15;
            _buttonPenTypePencil.Text = "✎";
            _toolTip.SetToolTip(_buttonPenTypePencil, "铅笔");
            _buttonPenTypePencil.UseVisualStyleBackColor = false;
            _buttonPenTypePencil.Click += ButtonPenTypePencil_Click;
            // 
            // _buttonPenTypeMarker
            // 
            _buttonPenTypeMarker.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonPenTypeMarker.FlatStyle = FlatStyle.Flat;
            _buttonPenTypeMarker.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonPenTypeMarker.Location = new Point(67, 0);
            _buttonPenTypeMarker.Name = "_buttonPenTypeMarker";
            _buttonPenTypeMarker.Size = new Size(28, 32);
            _buttonPenTypeMarker.TabIndex = 17;
            _buttonPenTypeMarker.Text = "▮";
            _toolTip.SetToolTip(_buttonPenTypeMarker, "马克笔");
            _buttonPenTypeMarker.UseVisualStyleBackColor = false;
            _buttonPenTypeMarker.Click += ButtonPenTypeMarker_Click;
            // 
            // _buttonPenTypePen
            // 
            _buttonPenTypePen.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonPenTypePen.FlatStyle = FlatStyle.Flat;
            _buttonPenTypePen.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            _buttonPenTypePen.Location = new Point(35, 0);
            _buttonPenTypePen.Name = "_buttonPenTypePen";
            _buttonPenTypePen.Size = new Size(28, 32);
            _buttonPenTypePen.TabIndex = 16;
            _buttonPenTypePen.Text = "✒";
            _toolTip.SetToolTip(_buttonPenTypePen, "水笔");
            _buttonPenTypePen.UseVisualStyleBackColor = false;
            _buttonPenTypePen.Click += ButtonPenTypePen_Click;
            // 
            // _buttonChecklistMode
            // 
            _buttonChecklistMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonChecklistMode.FlatStyle = FlatStyle.Flat;
            _buttonChecklistMode.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonChecklistMode.Location = new Point(1382, 6);
            _buttonChecklistMode.Name = "_buttonChecklistMode";
            _buttonChecklistMode.Size = new Size(28, 31);
            _buttonChecklistMode.TabIndex = 22;
            _buttonChecklistMode.Text = "☑";
            _toolTip.SetToolTip(_buttonChecklistMode, "核对清单 (C)");
            _buttonChecklistMode.UseVisualStyleBackColor = false;
            _buttonChecklistMode.Click += ButtonChecklistMode_Click;
            // 
            // _buttonCollapseNav
            // 
            _buttonCollapseNav.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonCollapseNav.FlatStyle = FlatStyle.Flat;
            _buttonCollapseNav.Font = new Font("Microsoft YaHei UI", 8F);
            _buttonCollapseNav.ForeColor = Color.FromArgb(80, 80, 80);
            _buttonCollapseNav.Location = new Point(1486, 44);
            _buttonCollapseNav.Name = "_buttonCollapseNav";
            _buttonCollapseNav.Size = new Size(24, 24);
            _buttonCollapseNav.TabIndex = 30;
            _buttonCollapseNav.Text = "▾";
            _toolTip.SetToolTip(_buttonCollapseNav, "折叠/展开工具栏");
            _buttonCollapseNav.UseVisualStyleBackColor = false;
            _buttonCollapseNav.Visible = false;
            // 
            // _buttonExportAnnotationsAsImage
            // 
            _buttonExportAnnotationsAsImage.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonExportAnnotationsAsImage.FlatStyle = FlatStyle.Flat;
            _buttonExportAnnotationsAsImage.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonExportAnnotationsAsImage.Location = new Point(1350, 6);
            _buttonExportAnnotationsAsImage.Name = "_buttonExportAnnotationsAsImage";
            _buttonExportAnnotationsAsImage.Size = new Size(28, 31);
            _buttonExportAnnotationsAsImage.TabIndex = 21;
            _buttonExportAnnotationsAsImage.Text = "📷";
            _toolTip.SetToolTip(_buttonExportAnnotationsAsImage, "导出标注为图片");
            _buttonExportAnnotationsAsImage.UseVisualStyleBackColor = false;
            _buttonExportAnnotationsAsImage.Click += ExportAnnotationsAsImage_Click;
            // 
            // _toolbarGroupView
            // 
            _toolbarGroupView.Controls.Add(_trackBarZoom);
            _toolbarGroupView.Controls.Add(_buttonDualPage);
            _toolbarGroupView.Controls.Add(_buttonResetView);
            _toolbarGroupView.Controls.Add(_buttonZoomOut);
            _toolbarGroupView.Controls.Add(_buttonFullscreen);
            _toolbarGroupView.Controls.Add(_buttonRotate);
            _toolbarGroupView.Controls.Add(_labelZoom);
            _toolbarGroupView.Controls.Add(_buttonZoomIn);
            _toolbarGroupView.Controls.Add(_buttonLockView);
            _toolbarGroupView.Location = new Point(465, -1);
            _toolbarGroupView.Name = "_toolbarGroupView";
            _toolbarGroupView.Size = new Size(376, 40);
            _toolbarGroupView.TabIndex = 1;
            // 
            // _trackBarZoom
            // 
            _trackBarZoom.AutoSize = false;
            _trackBarZoom.Location = new Point(37, 4);
            _trackBarZoom.Maximum = 400;
            _trackBarZoom.Minimum = 10;
            _trackBarZoom.Name = "_trackBarZoom";
            _trackBarZoom.Size = new Size(100, 32);
            _trackBarZoom.TabIndex = 1;
            _trackBarZoom.TickStyle = TickStyle.None;
            _trackBarZoom.Value = 100;
            _trackBarZoom.Scroll += TrackBarZoom_Scroll;
            // 
            // _buttonDualPage
            // 
            _buttonDualPage.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonDualPage.FlatStyle = FlatStyle.Flat;
            _buttonDualPage.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonDualPage.Location = new Point(281, 4);
            _buttonDualPage.Name = "_buttonDualPage";
            _buttonDualPage.Size = new Size(28, 32);
            _buttonDualPage.TabIndex = 1;
            _buttonDualPage.Text = "📖";
            _buttonDualPage.UseVisualStyleBackColor = false;
            _buttonDualPage.Click += ButtonDualPage_Click;
            // 
            // _buttonResetView
            // 
            _buttonResetView.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonResetView.FlatStyle = FlatStyle.Flat;
            _buttonResetView.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonResetView.Location = new Point(313, 4);
            _buttonResetView.Name = "_buttonResetView";
            _buttonResetView.Size = new Size(28, 32);
            _buttonResetView.TabIndex = 4;
            _buttonResetView.Text = "↺";
            _buttonResetView.UseVisualStyleBackColor = false;
            _buttonResetView.Click += ButtonResetView_Click;
            // 
            // _buttonZoomOut
            // 
            _buttonZoomOut.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonZoomOut.FlatStyle = FlatStyle.Flat;
            _buttonZoomOut.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            _buttonZoomOut.Location = new Point(5, 4);
            _buttonZoomOut.Name = "_buttonZoomOut";
            _buttonZoomOut.Size = new Size(28, 32);
            _buttonZoomOut.TabIndex = 0;
            _buttonZoomOut.Text = "−";
            _buttonZoomOut.UseVisualStyleBackColor = false;
            _buttonZoomOut.Click += ButtonZoomOut_Click;
            // 
            // _buttonFullscreen
            // 
            _buttonFullscreen.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonFullscreen.FlatStyle = FlatStyle.Flat;
            _buttonFullscreen.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonFullscreen.Location = new Point(249, 4);
            _buttonFullscreen.Name = "_buttonFullscreen";
            _buttonFullscreen.Size = new Size(28, 32);
            _buttonFullscreen.TabIndex = 2;
            _buttonFullscreen.Text = "⛶";
            _buttonFullscreen.UseVisualStyleBackColor = false;
            _buttonFullscreen.Click += ButtonFullscreen_Click;
            // 
            // _buttonRotate
            // 
            _buttonRotate.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonRotate.FlatStyle = FlatStyle.Flat;
            _buttonRotate.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonRotate.Location = new Point(345, 4);
            _buttonRotate.Name = "_buttonRotate";
            _buttonRotate.Size = new Size(28, 32);
            _buttonRotate.TabIndex = 5;
            _buttonRotate.Text = "↻";
            _toolTip.SetToolTip(_buttonRotate, "顺时针旋转90°（右键逆时针旋转）");
            _buttonRotate.UseVisualStyleBackColor = false;
            _buttonRotate.Click += ButtonRotate_Click;
            _buttonRotate.MouseDown += ButtonRotate_MouseDown;
            // 
            // _labelZoom
            // 
            _labelZoom.AutoSize = true;
            _labelZoom.Font = new Font("Microsoft YaHei UI", 9F);
            _labelZoom.ForeColor = Color.FromArgb(102, 102, 102);
            _labelZoom.Location = new Point(141, 12);
            _labelZoom.Name = "_labelZoom";
            _labelZoom.Size = new Size(40, 17);
            _labelZoom.TabIndex = 2;
            _labelZoom.Text = "100%";
            // 
            // _buttonZoomIn
            // 
            _buttonZoomIn.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonZoomIn.FlatStyle = FlatStyle.Flat;
            _buttonZoomIn.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            _buttonZoomIn.Location = new Point(185, 4);
            _buttonZoomIn.Name = "_buttonZoomIn";
            _buttonZoomIn.Size = new Size(28, 32);
            _buttonZoomIn.TabIndex = 3;
            _buttonZoomIn.Text = "+";
            _buttonZoomIn.UseVisualStyleBackColor = false;
            _buttonZoomIn.Click += ButtonZoomIn_Click;
            // 
            // _buttonLockView
            // 
            _buttonLockView.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonLockView.FlatStyle = FlatStyle.Flat;
            _buttonLockView.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonLockView.Location = new Point(217, 4);
            _buttonLockView.Name = "_buttonLockView";
            _buttonLockView.Size = new Size(28, 32);
            _buttonLockView.TabIndex = 6;
            _buttonLockView.Text = "🔓";
            _buttonLockView.UseVisualStyleBackColor = false;
            _buttonLockView.Click += ButtonLockView_Click;
            // 
            // _buttonStrikethroughMode
            // 
            _buttonStrikethroughMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonStrikethroughMode.FlatStyle = FlatStyle.Flat;
            _buttonStrikethroughMode.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Strikeout);
            _buttonStrikethroughMode.Location = new Point(1286, 6);
            _buttonStrikethroughMode.Name = "_buttonStrikethroughMode";
            _buttonStrikethroughMode.Size = new Size(28, 31);
            _buttonStrikethroughMode.TabIndex = 25;
            _buttonStrikethroughMode.Text = "S";
            _toolTip.SetToolTip(_buttonStrikethroughMode, "删除线 (W)");
            _buttonStrikethroughMode.UseVisualStyleBackColor = false;
            _buttonStrikethroughMode.Click += ButtonStrikethroughMode_Click;
            // 
            // _buttonNightMode
            // 
            _buttonNightMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonNightMode.FlatStyle = FlatStyle.Flat;
            _buttonNightMode.Font = new Font("Microsoft YaHei UI", 12F);
            _buttonNightMode.Location = new Point(333, 4);
            _buttonNightMode.Name = "_buttonNightMode";
            _buttonNightMode.Size = new Size(28, 31);
            _buttonNightMode.TabIndex = 0;
            _buttonNightMode.Text = "🌙";
            _toolTip.SetToolTip(_buttonNightMode, "夜间模式");
            _buttonNightMode.UseVisualStyleBackColor = false;
            _buttonNightMode.Click += ButtonNightMode_Click;
            // 
            // _buttonToggleAnnotationLayer
            // 
            _buttonToggleAnnotationLayer.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonToggleAnnotationLayer.FlatStyle = FlatStyle.Flat;
            _buttonToggleAnnotationLayer.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonToggleAnnotationLayer.Location = new Point(1318, 6);
            _buttonToggleAnnotationLayer.Name = "_buttonToggleAnnotationLayer";
            _buttonToggleAnnotationLayer.Size = new Size(28, 31);
            _buttonToggleAnnotationLayer.TabIndex = 20;
            _buttonToggleAnnotationLayer.Text = "👁";
            _toolTip.SetToolTip(_buttonToggleAnnotationLayer, "显示/隐藏标注层");
            _buttonToggleAnnotationLayer.UseVisualStyleBackColor = false;
            _buttonToggleAnnotationLayer.Click += ButtonToggleAnnotationLayer_Click;
            // 
            // _buttonAskAi
            // 
            _buttonAskAi.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonAskAi.FlatStyle = FlatStyle.Flat;
            _buttonAskAi.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonAskAi.Location = new Point(365, 4);
            _buttonAskAi.Name = "_buttonAskAi";
            _buttonAskAi.Size = new Size(28, 31);
            _buttonAskAi.TabIndex = 9;
            _buttonAskAi.Text = "🤖";
            _toolTip.SetToolTip(_buttonAskAi, "AI 问答");
            _buttonAskAi.UseVisualStyleBackColor = false;
            _buttonAskAi.Click += ButtonAskAi_Click;
            // 
            // _buttonTranslationToggle
            // 
            _buttonTranslationToggle.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonTranslationToggle.FlatStyle = FlatStyle.Flat;
            _buttonTranslationToggle.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            _buttonTranslationToggle.Location = new Point(397, 4);
            _buttonTranslationToggle.Name = "_buttonTranslationToggle";
            _buttonTranslationToggle.Size = new Size(28, 31);
            _buttonTranslationToggle.TabIndex = 26;
            _buttonTranslationToggle.Text = "译";
            _toolTip.SetToolTip(_buttonTranslationToggle, "翻译模式");
            _buttonTranslationToggle.UseVisualStyleBackColor = false;
            _buttonTranslationToggle.Click += ButtonTranslationToggle_Click;
            // 
            // _buttonClearAllAnnotations
            // 
            _buttonClearAllAnnotations.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonClearAllAnnotations.FlatStyle = FlatStyle.Flat;
            _buttonClearAllAnnotations.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonClearAllAnnotations.Location = new Point(431, 4);
            _buttonClearAllAnnotations.Name = "_buttonClearAllAnnotations";
            _buttonClearAllAnnotations.Size = new Size(28, 31);
            _buttonClearAllAnnotations.TabIndex = 8;
            _buttonClearAllAnnotations.Text = "🗑";
            _toolTip.SetToolTip(_buttonClearAllAnnotations, "清除所有标注");
            _buttonClearAllAnnotations.UseVisualStyleBackColor = false;
            _buttonClearAllAnnotations.Click += ButtonClearAllAnnotations_Click;
            // 
            // _buttonMagnifierMode
            // 
            _buttonMagnifierMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonMagnifierMode.FlatStyle = FlatStyle.Flat;
            _buttonMagnifierMode.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonMagnifierMode.Location = new Point(333, 4);
            _buttonMagnifierMode.Name = "_buttonMagnifierMode";
            _buttonMagnifierMode.Size = new Size(28, 31);
            _buttonMagnifierMode.TabIndex = 25;
            _buttonMagnifierMode.Text = "🔍";
            _toolTip.SetToolTip(_buttonMagnifierMode, "放大镜 (L)");
            _buttonMagnifierMode.UseVisualStyleBackColor = false;
            _buttonMagnifierMode.Click += ButtonMagnifierMode_Click;
            // 
            // _buttonArrowMode
            // 
            _buttonArrowMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonArrowMode.FlatStyle = FlatStyle.Flat;
            _buttonArrowMode.Font = new Font("Microsoft YaHei UI", 12F);
            _buttonArrowMode.Location = new Point(101, 4);
            _buttonArrowMode.Name = "_buttonArrowMode";
            _buttonArrowMode.Size = new Size(28, 31);
            _buttonArrowMode.TabIndex = 3;
            _buttonArrowMode.Text = "━";
            _toolTip.SetToolTip(_buttonArrowMode, "箭头 (A)");
            _buttonArrowMode.UseVisualStyleBackColor = false;
            _buttonArrowMode.Click += ButtonArrowMode_Click;
            // 
            // pnlPage
            // 
            pnlPage.Controls.Add(_buttonOpenFolder);
            pnlPage.Controls.Add(_buttonPrev);
            pnlPage.Controls.Add(_buttonPrevSpread);
            pnlPage.Controls.Add(_progressBarPage);
            pnlPage.Controls.Add(_textBoxPage);
            pnlPage.Controls.Add(_buttonNext);
            pnlPage.Controls.Add(_buttonNextSpread);
            pnlPage.Controls.Add(_labelPageCount);
            pnlPage.Location = new Point(846, -2);
            pnlPage.Name = "pnlPage";
            pnlPage.Size = new Size(343, 40);
            pnlPage.TabIndex = 1;
            // 
            // _buttonOpenFolder
            // 
            _buttonOpenFolder.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonOpenFolder.FlatStyle = FlatStyle.Flat;
            _buttonOpenFolder.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonOpenFolder.Location = new Point(6, 3);
            _buttonOpenFolder.Name = "_buttonOpenFolder";
            _buttonOpenFolder.Size = new Size(28, 32);
            _buttonOpenFolder.TabIndex = 9;
            _buttonOpenFolder.Text = "📂";
            _buttonOpenFolder.UseVisualStyleBackColor = false;
            _buttonOpenFolder.Click += ButtonOpenFolder_Click;
            // 
            // _buttonPrev
            // 
            _buttonPrev.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonPrev.FlatStyle = FlatStyle.Flat;
            _buttonPrev.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonPrev.Location = new Point(40, 3);
            _buttonPrev.Name = "_buttonPrev";
            _buttonPrev.Size = new Size(28, 32);
            _buttonPrev.TabIndex = 0;
            _buttonPrev.Text = "◀";
            _toolTip.SetToolTip(_buttonPrev, "上一页（单页步进）");
            _buttonPrev.UseVisualStyleBackColor = false;
            _buttonPrev.Click += ButtonPrev_Click;
            // 
            // _buttonPrevSpread
            // 
            _buttonPrevSpread.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonPrevSpread.FlatStyle = FlatStyle.Flat;
            _buttonPrevSpread.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonPrevSpread.Location = new Point(177, 3);
            _buttonPrevSpread.Name = "_buttonPrevSpread";
            _buttonPrevSpread.Size = new Size(28, 32);
            _buttonPrevSpread.TabIndex = 10;
            _buttonPrevSpread.Text = "«";
            _toolTip.SetToolTip(_buttonPrevSpread, "上一跨页（双页模式，一次翻两页）");
            _buttonPrevSpread.UseVisualStyleBackColor = false;
            _buttonPrevSpread.Visible = false;
            _buttonPrevSpread.Click += ButtonPrevSpread_Click;
            // 
            // _progressBarPage
            // 
            _progressBarPage.Location = new Point(235, 7);
            _progressBarPage.Maximum = 1;
            _progressBarPage.Minimum = 1;
            _progressBarPage.Name = "_progressBarPage";
            _progressBarPage.Size = new Size(106, 24);
            _progressBarPage.Style = ProgressBarStyle.Continuous;
            _progressBarPage.TabIndex = 4;
            _progressBarPage.Value = 1;
            // 
            // _textBoxPage
            // 
            _textBoxPage.BorderStyle = BorderStyle.FixedSingle;
            _textBoxPage.Font = new Font("Microsoft YaHei UI", 10F);
            _textBoxPage.Location = new Point(74, 7);
            _textBoxPage.Name = "_textBoxPage";
            _textBoxPage.Size = new Size(40, 24);
            _textBoxPage.TabIndex = 1;
            _textBoxPage.Text = "1";
            _textBoxPage.TextAlign = HorizontalAlignment.Center;
            _textBoxPage.KeyDown += TextBoxPage_KeyDown;
            // 
            // _buttonNext
            // 
            _buttonNext.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonNext.FlatStyle = FlatStyle.Flat;
            _buttonNext.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonNext.Location = new Point(145, 3);
            _buttonNext.Name = "_buttonNext";
            _buttonNext.Size = new Size(28, 32);
            _buttonNext.TabIndex = 3;
            _buttonNext.Text = "▶";
            _toolTip.SetToolTip(_buttonNext, "下一页（单页步进）");
            _buttonNext.UseVisualStyleBackColor = false;
            _buttonNext.Click += ButtonNext_Click;
            // 
            // _buttonNextSpread
            // 
            _buttonNextSpread.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonNextSpread.FlatStyle = FlatStyle.Flat;
            _buttonNextSpread.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonNextSpread.Location = new Point(208, 3);
            _buttonNextSpread.Name = "_buttonNextSpread";
            _buttonNextSpread.Size = new Size(28, 32);
            _buttonNextSpread.TabIndex = 11;
            _buttonNextSpread.Text = "»";
            _toolTip.SetToolTip(_buttonNextSpread, "下一跨页（双页模式，一次翻两页）");
            _buttonNextSpread.UseVisualStyleBackColor = false;
            _buttonNextSpread.Visible = false;
            _buttonNextSpread.Click += ButtonNextSpread_Click;
            // 
            // _labelPageCount
            // 
            _labelPageCount.AutoSize = true;
            _labelPageCount.Font = new Font("Microsoft YaHei UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 134);
            _labelPageCount.ForeColor = Color.FromArgb(102, 102, 102);
            _labelPageCount.Location = new Point(120, 11);
            _labelPageCount.Name = "_labelPageCount";
            _labelPageCount.Size = new Size(21, 16);
            _labelPageCount.TabIndex = 2;
            _labelPageCount.Text = "/ 1";
            // 
            // _panelAnnotationOptions
            // 
            _panelAnnotationOptions.Controls.Add(_buttonDashStyle);
            _panelAnnotationOptions.Controls.Add(_trackBarThickness);
            _panelAnnotationOptions.Controls.Add(_labelThicknessValue);
            _panelAnnotationOptions.Controls.Add(_panelColor);
            _panelAnnotationOptions.Location = new Point(5, 36);
            _panelAnnotationOptions.Name = "_panelAnnotationOptions";
            _panelAnnotationOptions.Size = new Size(563, 38);
            _panelAnnotationOptions.TabIndex = 9;
            _panelAnnotationOptions.Visible = false;
            // 
            // _buttonDashStyle
            // 
            _buttonDashStyle.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonDashStyle.FlatStyle = FlatStyle.Flat;
            _buttonDashStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            _buttonDashStyle.Location = new Point(3, 3);
            _buttonDashStyle.Name = "_buttonDashStyle";
            _buttonDashStyle.Size = new Size(28, 31);
            _buttonDashStyle.TabIndex = 0;
            _buttonDashStyle.Text = "—";
            _toolTip.SetToolTip(_buttonDashStyle, "切换实线/虚线");
            _buttonDashStyle.UseVisualStyleBackColor = false;
            _buttonDashStyle.Click += ButtonDashStyle_Click;
            // 
            // _trackBarThickness
            // 
            _trackBarThickness.AutoSize = false;
            _trackBarThickness.Location = new Point(40, 5);
            _trackBarThickness.Maximum = 20;
            _trackBarThickness.Minimum = 1;
            _trackBarThickness.Name = "_trackBarThickness";
            _trackBarThickness.Size = new Size(100, 28);
            _trackBarThickness.TabIndex = 4;
            _trackBarThickness.TickStyle = TickStyle.None;
            _trackBarThickness.Value = 3;
            _trackBarThickness.Scroll += TrackBarThickness_Scroll;
            // 
            // _labelThicknessValue
            // 
            _labelThicknessValue.AutoSize = true;
            _labelThicknessValue.Font = new Font("Microsoft YaHei UI", 9F);
            _labelThicknessValue.ForeColor = Color.FromArgb(102, 102, 102);
            _labelThicknessValue.Location = new Point(136, 9);
            _labelThicknessValue.Name = "_labelThicknessValue";
            _labelThicknessValue.Size = new Size(29, 17);
            _labelThicknessValue.TabIndex = 5;
            _labelThicknessValue.Text = "3px";
            // 
            // _panelColor
            // 
            _panelColor.Controls.Add(_buttonColorBlack);
            _panelColor.Controls.Add(_buttonColorBlue);
            _panelColor.Controls.Add(_buttonColorGreen);
            _panelColor.Controls.Add(_buttonColorOrange);
            _panelColor.Controls.Add(_buttonColorRed);
            _panelColor.Controls.Add(_buttonColorPurple);
            _panelColor.Controls.Add(_buttonColorCyan);
            _panelColor.Controls.Add(_buttonColorTeal);
            _panelColor.Controls.Add(_buttonColorPink);
            _panelColor.Controls.Add(_buttonColorBrown);
            _panelColor.Controls.Add(_buttonColorGray);
            _panelColor.Controls.Add(_buttonColorYellow);
            _panelColor.Controls.Add(_buttonColorLime);
            _panelColor.Controls.Add(_buttonColorVioletRed);
            _panelColor.Controls.Add(_buttonColorDodgerBlue);
            _panelColor.Controls.Add(_buttonColorMore);
            _panelColor.Controls.Add(_buttonHighlightColor0);
            _panelColor.Controls.Add(_buttonHighlightColor1);
            _panelColor.Controls.Add(_buttonHighlightColor2);
            _panelColor.Controls.Add(_buttonHighlightColor3);
            _panelColor.Controls.Add(_buttonHighlightColor4);
            _panelColor.Location = new Point(167, 6);
            _panelColor.Name = "_panelColor";
            _panelColor.Size = new Size(391, 27);
            _panelColor.TabIndex = 1;
            // 
            // _buttonColorBlack
            // 
            _buttonColorBlack.Location = new Point(0, 0);
            _buttonColorBlack.Name = "_buttonColorBlack";
            _buttonColorBlack.Size = new Size(25, 25);
            _buttonColorBlack.TabIndex = 0;
            // 
            // _buttonColorBlue
            // 
            _buttonColorBlue.Location = new Point(0, 0);
            _buttonColorBlue.Name = "_buttonColorBlue";
            _buttonColorBlue.Size = new Size(25, 25);
            _buttonColorBlue.TabIndex = 2;
            // 
            // _buttonColorGreen
            // 
            _buttonColorGreen.Location = new Point(0, 0);
            _buttonColorGreen.Name = "_buttonColorGreen";
            _buttonColorGreen.Size = new Size(25, 25);
            _buttonColorGreen.TabIndex = 3;
            // 
            // _buttonColorOrange
            // 
            _buttonColorOrange.Location = new Point(0, 0);
            _buttonColorOrange.Name = "_buttonColorOrange";
            _buttonColorOrange.Size = new Size(25, 25);
            _buttonColorOrange.TabIndex = 4;
            // 
            // _buttonColorRed
            // 
            _buttonColorRed.Location = new Point(0, 0);
            _buttonColorRed.Name = "_buttonColorRed";
            _buttonColorRed.Size = new Size(25, 25);
            _buttonColorRed.TabIndex = 5;
            // 
            // _buttonColorPurple
            // 
            _buttonColorPurple.Location = new Point(0, 0);
            _buttonColorPurple.Name = "_buttonColorPurple";
            _buttonColorPurple.Size = new Size(25, 25);
            _buttonColorPurple.TabIndex = 6;
            // 
            // _buttonColorCyan
            // 
            _buttonColorCyan.Location = new Point(0, 0);
            _buttonColorCyan.Name = "_buttonColorCyan";
            _buttonColorCyan.Size = new Size(25, 25);
            _buttonColorCyan.TabIndex = 7;
            // 
            // _buttonColorTeal
            // 
            _buttonColorTeal.Location = new Point(0, 0);
            _buttonColorTeal.Name = "_buttonColorTeal";
            _buttonColorTeal.Size = new Size(25, 25);
            _buttonColorTeal.TabIndex = 8;
            // 
            // _buttonColorPink
            // 
            _buttonColorPink.Location = new Point(0, 0);
            _buttonColorPink.Name = "_buttonColorPink";
            _buttonColorPink.Size = new Size(25, 25);
            _buttonColorPink.TabIndex = 9;
            // 
            // _buttonColorBrown
            // 
            _buttonColorBrown.Location = new Point(0, 0);
            _buttonColorBrown.Name = "_buttonColorBrown";
            _buttonColorBrown.Size = new Size(25, 25);
            _buttonColorBrown.TabIndex = 10;
            // 
            // _buttonColorGray
            // 
            _buttonColorGray.Location = new Point(0, 0);
            _buttonColorGray.Name = "_buttonColorGray";
            _buttonColorGray.Size = new Size(25, 25);
            _buttonColorGray.TabIndex = 11;
            // 
            // _buttonColorYellow
            // 
            _buttonColorYellow.Location = new Point(0, 0);
            _buttonColorYellow.Name = "_buttonColorYellow";
            _buttonColorYellow.Size = new Size(25, 25);
            _buttonColorYellow.TabIndex = 12;
            // 
            // _buttonColorLime
            // 
            _buttonColorLime.Location = new Point(0, 0);
            _buttonColorLime.Name = "_buttonColorLime";
            _buttonColorLime.Size = new Size(25, 25);
            _buttonColorLime.TabIndex = 13;
            // 
            // _buttonColorVioletRed
            // 
            _buttonColorVioletRed.Location = new Point(0, 0);
            _buttonColorVioletRed.Name = "_buttonColorVioletRed";
            _buttonColorVioletRed.Size = new Size(25, 25);
            _buttonColorVioletRed.TabIndex = 14;
            // 
            // _buttonColorDodgerBlue
            // 
            _buttonColorDodgerBlue.Location = new Point(0, 0);
            _buttonColorDodgerBlue.Name = "_buttonColorDodgerBlue";
            _buttonColorDodgerBlue.Size = new Size(25, 25);
            _buttonColorDodgerBlue.TabIndex = 15;
            // 
            // _buttonColorMore
            // 
            _buttonColorMore.BackColor = Color.FromArgb(240, 240, 240);
            _buttonColorMore.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            _buttonColorMore.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 220, 220);
            _buttonColorMore.FlatStyle = FlatStyle.Flat;
            _buttonColorMore.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            _buttonColorMore.Location = new Point(2, 4);
            _buttonColorMore.Name = "_buttonColorMore";
            _buttonColorMore.Size = new Size(25, 25);
            _buttonColorMore.TabIndex = 14;
            _buttonColorMore.Text = "+";
            _buttonColorMore.UseVisualStyleBackColor = false;
            _buttonColorMore.Click += ButtonColorMore_Click;
            // 
            // _buttonHighlightColor0
            // 
            _buttonHighlightColor0.BackColor = Color.FromArgb(255, 255, 185);
            _buttonHighlightColor0.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            _buttonHighlightColor0.FlatAppearance.BorderSize = 1;
            _buttonHighlightColor0.FlatStyle = FlatStyle.Flat;
            _buttonHighlightColor0.Location = new Point(2, 4);
            _buttonHighlightColor0.Name = "_buttonHighlightColor0";
            _buttonHighlightColor0.Size = new Size(22, 22);
            _buttonHighlightColor0.TabIndex = 200;
            _buttonHighlightColor0.UseVisualStyleBackColor = false;
            _buttonHighlightColor0.Visible = false;
            // 
            // _buttonHighlightColor1
            // 
            _buttonHighlightColor1.BackColor = Color.FromArgb(185, 255, 185);
            _buttonHighlightColor1.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            _buttonHighlightColor1.FlatAppearance.BorderSize = 1;
            _buttonHighlightColor1.FlatStyle = FlatStyle.Flat;
            _buttonHighlightColor1.Location = new Point(30, 4);
            _buttonHighlightColor1.Name = "_buttonHighlightColor1";
            _buttonHighlightColor1.Size = new Size(22, 22);
            _buttonHighlightColor1.TabIndex = 201;
            _buttonHighlightColor1.UseVisualStyleBackColor = false;
            _buttonHighlightColor1.Visible = false;
            // 
            // _buttonHighlightColor2
            // 
            _buttonHighlightColor2.BackColor = Color.FromArgb(185, 237, 255);
            _buttonHighlightColor2.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            _buttonHighlightColor2.FlatAppearance.BorderSize = 1;
            _buttonHighlightColor2.FlatStyle = FlatStyle.Flat;
            _buttonHighlightColor2.Location = new Point(58, 4);
            _buttonHighlightColor2.Name = "_buttonHighlightColor2";
            _buttonHighlightColor2.Size = new Size(22, 22);
            _buttonHighlightColor2.TabIndex = 202;
            _buttonHighlightColor2.UseVisualStyleBackColor = false;
            _buttonHighlightColor2.Visible = false;
            // 
            // _buttonHighlightColor3
            // 
            _buttonHighlightColor3.BackColor = Color.FromArgb(255, 237, 240);
            _buttonHighlightColor3.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            _buttonHighlightColor3.FlatAppearance.BorderSize = 1;
            _buttonHighlightColor3.FlatStyle = FlatStyle.Flat;
            _buttonHighlightColor3.Location = new Point(86, 4);
            _buttonHighlightColor3.Name = "_buttonHighlightColor3";
            _buttonHighlightColor3.Size = new Size(22, 22);
            _buttonHighlightColor3.TabIndex = 203;
            _buttonHighlightColor3.UseVisualStyleBackColor = false;
            _buttonHighlightColor3.Visible = false;
            // 
            // _buttonHighlightColor4
            // 
            _buttonHighlightColor4.BackColor = Color.FromArgb(255, 230, 185);
            _buttonHighlightColor4.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            _buttonHighlightColor4.FlatAppearance.BorderSize = 1;
            _buttonHighlightColor4.FlatStyle = FlatStyle.Flat;
            _buttonHighlightColor4.Location = new Point(114, 4);
            _buttonHighlightColor4.Name = "_buttonHighlightColor4";
            _buttonHighlightColor4.Size = new Size(22, 22);
            _buttonHighlightColor4.TabIndex = 204;
            _buttonHighlightColor4.UseVisualStyleBackColor = false;
            _buttonHighlightColor4.Visible = false;
            // 
            // _buttonUndoAnnotation
            // 
            _buttonUndoAnnotation.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonUndoAnnotation.FlatStyle = FlatStyle.Flat;
            _buttonUndoAnnotation.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonUndoAnnotation.Location = new Point(265, 4);
            _buttonUndoAnnotation.Name = "_buttonUndoAnnotation";
            _buttonUndoAnnotation.Size = new Size(28, 31);
            _buttonUndoAnnotation.TabIndex = 7;
            _buttonUndoAnnotation.Text = "↩";
            _toolTip.SetToolTip(_buttonUndoAnnotation, "撤销 (Ctrl+Z)");
            _buttonUndoAnnotation.UseVisualStyleBackColor = false;
            _buttonUndoAnnotation.Click += ButtonUndoAnnotation_Click;
            // 
            // _buttonHighlightMode
            // 
            _buttonHighlightMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonHighlightMode.FlatStyle = FlatStyle.Flat;
            _buttonHighlightMode.Font = new Font("Microsoft YaHei UI", 11F);
            _buttonHighlightMode.Location = new Point(5, 4);
            _buttonHighlightMode.Name = "_buttonHighlightMode";
            _buttonHighlightMode.Size = new Size(28, 31);
            _buttonHighlightMode.TabIndex = 1;
            _buttonHighlightMode.Text = "⭐";
            _toolTip.SetToolTip(_buttonHighlightMode, "高亮 (H)");
            _buttonHighlightMode.UseVisualStyleBackColor = false;
            _buttonHighlightMode.Click += ButtonHighlightMode_Click;
            // 
            // _buttonRectangleMode
            // 
            _buttonRectangleMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonRectangleMode.FlatStyle = FlatStyle.Flat;
            _buttonRectangleMode.Font = new Font("Microsoft YaHei UI", 12F);
            _buttonRectangleMode.Location = new Point(37, 4);
            _buttonRectangleMode.Name = "_buttonRectangleMode";
            _buttonRectangleMode.Size = new Size(28, 31);
            _buttonRectangleMode.TabIndex = 1;
            _buttonRectangleMode.Text = "▢";
            _toolTip.SetToolTip(_buttonRectangleMode, "矩形 (R)");
            _buttonRectangleMode.UseVisualStyleBackColor = false;
            _buttonRectangleMode.Click += ButtonRectangleMode_Click;
            // 
            // _buttonEllipseMode
            // 
            _buttonEllipseMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonEllipseMode.FlatStyle = FlatStyle.Flat;
            _buttonEllipseMode.Font = new Font("Microsoft YaHei UI", 12F);
            _buttonEllipseMode.Location = new Point(69, 4);
            _buttonEllipseMode.Name = "_buttonEllipseMode";
            _buttonEllipseMode.Size = new Size(28, 31);
            _buttonEllipseMode.TabIndex = 2;
            _buttonEllipseMode.Text = "◯";
            _toolTip.SetToolTip(_buttonEllipseMode, "椭圆 (E)");
            _buttonEllipseMode.UseVisualStyleBackColor = false;
            _buttonEllipseMode.Click += ButtonEllipseMode_Click;
            // 
            // _buttonPenMode
            // 
            _buttonPenMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonPenMode.FlatStyle = FlatStyle.Flat;
            _buttonPenMode.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonPenMode.Location = new Point(133, 4);
            _buttonPenMode.Name = "_buttonPenMode";
            _buttonPenMode.Size = new Size(28, 31);
            _buttonPenMode.TabIndex = 14;
            _buttonPenMode.Text = "✒";
            _toolTip.SetToolTip(_buttonPenMode, "画笔 (P)");
            _buttonPenMode.UseVisualStyleBackColor = false;
            _buttonPenMode.Click += ButtonPenMode_Click;
            // 
            // _buttonMosaicMode
            // 
            _buttonMosaicMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonMosaicMode.FlatStyle = FlatStyle.Flat;
            _buttonMosaicMode.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonMosaicMode.Location = new Point(167, 4);
            _buttonMosaicMode.Name = "_buttonMosaicMode";
            _buttonMosaicMode.Size = new Size(28, 31);
            _buttonMosaicMode.TabIndex = 5;
            _buttonMosaicMode.Text = "▦";
            _buttonMosaicMode.UseVisualStyleBackColor = false;
            _buttonMosaicMode.Click += ButtonMosaicMode_Click;
            // 
            // _buttonTextMode
            // 
            _buttonTextMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonTextMode.FlatStyle = FlatStyle.Flat;
            _buttonTextMode.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
            _buttonTextMode.Location = new Point(199, 4);
            _buttonTextMode.Name = "_buttonTextMode";
            _buttonTextMode.Size = new Size(28, 31);
            _buttonTextMode.TabIndex = 6;
            _buttonTextMode.Text = "T";
            _toolTip.SetToolTip(_buttonTextMode, "文字 (T)");
            _buttonTextMode.UseVisualStyleBackColor = false;
            _buttonTextMode.Click += ButtonTextMode_Click;
            // 
            // _buttonEraserMode
            // 
            _buttonEraserMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonEraserMode.FlatStyle = FlatStyle.Flat;
            _buttonEraserMode.Font = new Font("Microsoft YaHei UI", 11F);
            _buttonEraserMode.Location = new Point(231, 4);
            _buttonEraserMode.Name = "_buttonEraserMode";
            _buttonEraserMode.Size = new Size(28, 31);
            _buttonEraserMode.TabIndex = 19;
            _buttonEraserMode.Text = "\U0001f9fd";
            _toolTip.SetToolTip(_buttonEraserMode, "橡皮擦");
            _buttonEraserMode.UseVisualStyleBackColor = false;
            _buttonEraserMode.Click += ButtonEraserMode_Click;
            // 
            // _buttonMoreTools
            // 
            _buttonMoreTools.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonMoreTools.FlatStyle = FlatStyle.Flat;
            _buttonMoreTools.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonMoreTools.Location = new Point(299, 4);
            _buttonMoreTools.Name = "_buttonMoreTools";
            _buttonMoreTools.Size = new Size(28, 31);
            _buttonMoreTools.TabIndex = 8;
            _buttonMoreTools.Text = "⋯";
            _toolTip.SetToolTip(_buttonMoreTools, "更多工具");
            _buttonMoreTools.UseVisualStyleBackColor = false;
            _buttonMoreTools.Click += ButtonMoreTools_Click;
            // 
            // _statusBar
            // 
            _statusBar.BackColor = Color.White;
            _statusBar.Controls.Add(_statusLabelLeft);
            _statusBar.Controls.Add(_statusLabelRight);
            _statusBar.Dock = DockStyle.Bottom;
            _statusBar.Location = new Point(0, 1010);
            _statusBar.Name = "_statusBar";
            _statusBar.Size = new Size(1296, 24);
            _statusBar.TabIndex = 4;
            // 
            // _statusLabelLeft
            // 
            _statusLabelLeft.AutoSize = true;
            _statusLabelLeft.Font = new Font("Microsoft YaHei UI", 9F);
            _statusLabelLeft.ForeColor = Color.FromArgb(71, 85, 105);
            _statusLabelLeft.Location = new Point(12, 4);
            _statusLabelLeft.Name = "_statusLabelLeft";
            _statusLabelLeft.Size = new Size(142, 17);
            _statusLabelLeft.TabIndex = 0;
            _statusLabelLeft.Text = "第 1 页 / 共 1 页 · 100%";
            // 
            // _statusLabelRight
            // 
            _statusLabelRight.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _statusLabelRight.AutoSize = true;
            _statusLabelRight.Font = new Font("Microsoft YaHei UI", 9F);
            _statusLabelRight.ForeColor = Color.FromArgb(71, 85, 105);
            _statusLabelRight.Location = new Point(1146, 4);
            _statusLabelRight.Name = "_statusLabelRight";
            _statusLabelRight.Size = new Size(130, 17);
            _statusLabelRight.TabIndex = 1;
            _statusLabelRight.Text = "缩放: 100% · 高亮模式";
            _statusLabelRight.TextAlign = ContentAlignment.MiddleRight;
            // 
            // _pictureBoxPdf
            // 
            _pictureBoxPdf.ContextMenuStrip = _contextMenuPdf;
            _pictureBoxPdf.Dock = DockStyle.Fill;
            _pictureBoxPdf.Location = new Point(0, 0);
            _pictureBoxPdf.Name = "_pictureBoxPdf";
            _pictureBoxPdf.Size = new Size(1296, 1034);
            _pictureBoxPdf.TabIndex = 1;
            _pictureBoxPdf.TabStop = false;
            _pictureBoxPdf.Paint += PictureBoxPdf_Paint;
            _pictureBoxPdf.MouseWheel += PictureBoxPdf_MouseWheel;
            // 
            // _contextMenuPdf
            // 
            _contextMenuPdf.Items.AddRange(new ToolStripItem[] { menuItemCopy, menuItemSearch, menuItemAnnotationSeparator, menuItemHighlight, menuItemRectangle, menuItemText, menuItemZoomSeparator, menuItemZoomIn, menuItemZoomOut, menuItemResetZoom, menuItemExport, menuItemRotationSeparator, menuItemRotateLeft, menuItemRotateRight, menuItemResetRotation });
            _contextMenuPdf.Name = "_contextMenuPdf";
            _contextMenuPdf.Size = new Size(183, 286);
            // 
            // menuItemCopy
            // 
            menuItemCopy.Name = "menuItemCopy";
            menuItemCopy.Size = new Size(182, 22);
            menuItemCopy.Text = "📋 复制";
            menuItemCopy.Click += MenuItemCopy_Click;
            // 
            // menuItemSearch
            // 
            menuItemSearch.Name = "menuItemSearch";
            menuItemSearch.ShortcutKeyDisplayString = "Ctrl+F";
            menuItemSearch.Size = new Size(182, 22);
            menuItemSearch.Text = "🔍 搜索";
            menuItemSearch.Click += MenuItemSearch_Click;
            // 
            // menuItemAnnotationSeparator
            // 
            menuItemAnnotationSeparator.Name = "menuItemAnnotationSeparator";
            menuItemAnnotationSeparator.Size = new Size(179, 6);
            // 
            // menuItemHighlight
            // 
            menuItemHighlight.Name = "menuItemHighlight";
            menuItemHighlight.Size = new Size(182, 22);
            menuItemHighlight.Text = "🖍 高亮标注";
            menuItemHighlight.Click += MenuItemHighlight_Click;
            // 
            // menuItemRectangle
            // 
            menuItemRectangle.Name = "menuItemRectangle";
            menuItemRectangle.Size = new Size(182, 22);
            menuItemRectangle.Text = "▭ 矩形标注";
            menuItemRectangle.Click += MenuItemRectangle_Click;
            // 
            // menuItemText
            // 
            menuItemText.Name = "menuItemText";
            menuItemText.Size = new Size(182, 22);
            menuItemText.Text = "T 文字注解";
            menuItemText.Click += MenuItemText_Click;
            // 
            // menuItemZoomSeparator
            // 
            menuItemZoomSeparator.Name = "menuItemZoomSeparator";
            menuItemZoomSeparator.Size = new Size(179, 6);
            // 
            // menuItemZoomIn
            // 
            menuItemZoomIn.Name = "menuItemZoomIn";
            menuItemZoomIn.ShortcutKeyDisplayString = "Ctrl+=";
            menuItemZoomIn.Size = new Size(182, 22);
            menuItemZoomIn.Text = "＋ 放大";
            menuItemZoomIn.Click += MenuItemZoomIn_Click;
            // 
            // menuItemZoomOut
            // 
            menuItemZoomOut.Name = "menuItemZoomOut";
            menuItemZoomOut.ShortcutKeyDisplayString = "Ctrl+-";
            menuItemZoomOut.Size = new Size(182, 22);
            menuItemZoomOut.Text = "－ 缩小";
            menuItemZoomOut.Click += MenuItemZoomOut_Click;
            // 
            // menuItemResetZoom
            // 
            menuItemResetZoom.Name = "menuItemResetZoom";
            menuItemResetZoom.ShortcutKeyDisplayString = "Ctrl+0";
            menuItemResetZoom.Size = new Size(182, 22);
            menuItemResetZoom.Text = "⟲ 重置缩放";
            menuItemResetZoom.Click += MenuItemResetZoom_Click;
            // 
            // menuItemExport
            // 
            menuItemExport.Name = "menuItemExport";
            menuItemExport.Size = new Size(182, 22);
            menuItemExport.Text = "⤓ 导出当前页";
            menuItemExport.Click += MenuItemExport_Click;
            // 
            // menuItemRotationSeparator
            // 
            menuItemRotationSeparator.Name = "menuItemRotationSeparator";
            menuItemRotationSeparator.Size = new Size(179, 6);
            // 
            // menuItemRotateLeft
            // 
            menuItemRotateLeft.Name = "menuItemRotateLeft";
            menuItemRotateLeft.Size = new Size(182, 22);
            menuItemRotateLeft.Text = "↺ 向左旋转 90°";
            menuItemRotateLeft.Click += MenuItemRotateLeft_Click;
            // 
            // menuItemRotateRight
            // 
            menuItemRotateRight.Name = "menuItemRotateRight";
            menuItemRotateRight.Size = new Size(182, 22);
            menuItemRotateRight.Text = "↻ 向右旋转 90°";
            menuItemRotateRight.Click += MenuItemRotateRight_Click;
            // 
            // menuItemResetRotation
            // 
            menuItemResetRotation.Name = "menuItemResetRotation";
            menuItemResetRotation.Size = new Size(182, 22);
            menuItemResetRotation.Text = "⟳ 重置旋转";
            menuItemResetRotation.Click += MenuItemResetRotation_Click;
            // 
            // _ocrPanel
            // 
            _ocrPanel.BackColor = Color.LightGray;
            _ocrPanel.BorderStyle = BorderStyle.FixedSingle;
            _ocrPanel.Controls.Add(_ocrPictureBox);
            _ocrPanel.Controls.Add(_ocrCloseButton);
            _ocrPanel.Location = new Point(100, 150);
            _ocrPanel.Name = "_ocrPanel";
            _ocrPanel.Size = new Size(200, 150);
            _ocrPanel.TabIndex = 2;
            _ocrPanel.Visible = false;
            // 
            // _ocrPictureBox
            // 
            _ocrPictureBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _ocrPictureBox.Location = new Point(2, 26);
            _ocrPictureBox.Name = "_ocrPictureBox";
            _ocrPictureBox.Size = new Size(192, 120);
            _ocrPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            _ocrPictureBox.TabIndex = 0;
            _ocrPictureBox.TabStop = false;
            // 
            // _ocrCloseButton
            // 
            _ocrCloseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _ocrCloseButton.Font = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold);
            _ocrCloseButton.Location = new Point(168, 2);
            _ocrCloseButton.Name = "_ocrCloseButton";
            _ocrCloseButton.Size = new Size(24, 20);
            _ocrCloseButton.TabIndex = 1;
            _ocrCloseButton.Text = "×";
            _ocrCloseButton.UseVisualStyleBackColor = true;
            _ocrCloseButton.Click += OcrCloseButton_Click;
            // 
            // _pageTransitionOverlay
            // 
            _pageTransitionOverlay.BackColor = Color.White;
            _pageTransitionOverlay.Controls.Add(_transitionLabel);
            _pageTransitionOverlay.Dock = DockStyle.Fill;
            _pageTransitionOverlay.Location = new Point(0, 0);
            _pageTransitionOverlay.Name = "_pageTransitionOverlay";
            _pageTransitionOverlay.Size = new Size(1296, 1034);
            _pageTransitionOverlay.TabIndex = 0;
            _pageTransitionOverlay.Visible = false;
            // 
            // _transitionLabel
            // 
            _transitionLabel.Dock = DockStyle.Fill;
            _transitionLabel.Font = new Font("Microsoft YaHei UI", 24F, FontStyle.Bold);
            _transitionLabel.ForeColor = Color.FromArgb(200, 100, 100, 100);
            _transitionLabel.Location = new Point(0, 0);
            _transitionLabel.Name = "_transitionLabel";
            _transitionLabel.Size = new Size(1296, 1034);
            _transitionLabel.TabIndex = 0;
            _transitionLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _panelLeftContainer
            // 
            _panelLeftContainer.BackColor = Color.White;
            _panelLeftContainer.Controls.Add(_tabControlLeft);
            _panelLeftContainer.Dock = DockStyle.Fill;
            _panelLeftContainer.Location = new Point(0, 0);
            _panelLeftContainer.Name = "_panelLeftContainer";
            _panelLeftContainer.Size = new Size(563, 1034);
            _panelLeftContainer.TabIndex = 0;
            // 
            // _tabControlLeft
            // 
            _tabControlLeft.Controls.Add(_tabPageFiles);
            _tabControlLeft.Controls.Add(_tabPageThumbnails);
            _tabControlLeft.Controls.Add(_tabPageTranslate);
            _tabControlLeft.Controls.Add(_tabPageStudy);
            _tabControlLeft.Controls.Add(_tabPageBookmarksAndHighlights);
            _tabControlLeft.Controls.Add(_tabPageAudio);
            _tabControlLeft.Dock = DockStyle.Fill;
            _tabControlLeft.DrawMode = TabDrawMode.OwnerDrawFixed;
            _tabControlLeft.Font = new Font("Microsoft YaHei UI", 9F);
            _tabControlLeft.ItemSize = new Size(62, 30);
            _tabControlLeft.Location = new Point(0, 0);
            _tabControlLeft.Name = "_tabControlLeft";
            _tabControlLeft.SelectedIndex = 0;
            _tabControlLeft.Size = new Size(563, 1034);
            _tabControlLeft.SizeMode = TabSizeMode.Fixed;
            _tabControlLeft.TabIndex = 0;
            // 
            // _tabPageFiles
            // 
            _tabPageFiles.Controls.Add(_treeViewFiles);
            _tabPageFiles.Controls.Add(_textBoxFilter);
            _tabPageFiles.Location = new Point(4, 34);
            _tabPageFiles.Name = "_tabPageFiles";
            _tabPageFiles.Padding = new Padding(3);
            _tabPageFiles.Size = new Size(322, 996);
            _tabPageFiles.TabIndex = 2;
            _tabPageFiles.Text = "📁";
            _tabPageFiles.UseVisualStyleBackColor = true;
            // 
            // _treeViewFiles
            // 
            _treeViewFiles.BorderStyle = BorderStyle.None;
            _treeViewFiles.Dock = DockStyle.Fill;
            _treeViewFiles.Location = new Point(3, 27);
            _treeViewFiles.Name = "_treeViewFiles";
            _treeViewFiles.Size = new Size(316, 966);
            _treeViewFiles.TabIndex = 0;
            _treeViewFiles.AfterSelect += TreeViewFiles_AfterSelect;
            // 
            // _textBoxFilter
            // 
            _textBoxFilter.BorderStyle = BorderStyle.FixedSingle;
            _textBoxFilter.Dock = DockStyle.Top;
            _textBoxFilter.Font = new Font("Microsoft YaHei UI", 10F);
            _textBoxFilter.Location = new Point(3, 3);
            _textBoxFilter.Name = "_textBoxFilter";
            _textBoxFilter.PlaceholderText = "🔍 搜索文件...";
            _textBoxFilter.Size = new Size(316, 24);
            _textBoxFilter.TabIndex = 1;
            _textBoxFilter.TextChanged += TextBoxFilter_TextChanged;
            // 
            // _tabPageThumbnails
            // 
            _tabPageThumbnails.Controls.Add(_panelThumbnails);
            _tabPageThumbnails.Location = new Point(4, 34);
            _tabPageThumbnails.Name = "_tabPageThumbnails";
            _tabPageThumbnails.Padding = new Padding(3);
            _tabPageThumbnails.Size = new Size(555, 996);
            _tabPageThumbnails.TabIndex = 1;
            _tabPageThumbnails.Text = "🖼";
            _tabPageThumbnails.UseVisualStyleBackColor = true;
            // 
            // _panelThumbnails
            // 
            _panelThumbnails.BackColor = Color.FromArgb(245, 245, 245);
            _panelThumbnails.Controls.Add(_flowLayoutPanelThumbnails);
            _panelThumbnails.Dock = DockStyle.Fill;
            _panelThumbnails.Location = new Point(3, 3);
            _panelThumbnails.Name = "_panelThumbnails";
            _panelThumbnails.Size = new Size(549, 990);
            _panelThumbnails.TabIndex = 0;
            // 
            // _flowLayoutPanelThumbnails
            // 
            _flowLayoutPanelThumbnails.AutoScroll = true;
            _flowLayoutPanelThumbnails.BackColor = Color.FromArgb(245, 245, 245);
            _flowLayoutPanelThumbnails.Dock = DockStyle.Fill;
            _flowLayoutPanelThumbnails.Location = new Point(0, 0);
            _flowLayoutPanelThumbnails.Name = "_flowLayoutPanelThumbnails";
            _flowLayoutPanelThumbnails.Padding = new Padding(6, 6, 0, 6);
            _flowLayoutPanelThumbnails.Size = new Size(549, 990);
            _flowLayoutPanelThumbnails.TabIndex = 0;
            // 
            // _tabPageTranslate
            // 
            _tabPageTranslate.Location = new Point(4, 34);
            _tabPageTranslate.Name = "_tabPageTranslate";
            _tabPageTranslate.Padding = new Padding(3);
            _tabPageTranslate.Size = new Size(555, 996);
            _tabPageTranslate.TabIndex = 0;
            _tabPageTranslate.Text = "🌐";
            _tabPageTranslate.UseVisualStyleBackColor = true;
            // 
            // _tabPageStudy
            // 
            _tabPageStudy.Location = new Point(4, 34);
            _tabPageStudy.Name = "_tabPageStudy";
            _tabPageStudy.Padding = new Padding(3);
            _tabPageStudy.Size = new Size(555, 996);
            _tabPageStudy.TabIndex = 5;
            _tabPageStudy.Text = "🎯";
            _tabPageStudy.UseVisualStyleBackColor = true;
            // 
            // _tabPageBookmarksAndHighlights
            // 
            _tabPageBookmarksAndHighlights.Location = new Point(4, 34);
            _tabPageBookmarksAndHighlights.Name = "_tabPageBookmarksAndHighlights";
            _tabPageBookmarksAndHighlights.Padding = new Padding(3);
            _tabPageBookmarksAndHighlights.Size = new Size(555, 996);
            _tabPageBookmarksAndHighlights.TabIndex = 3;
            _tabPageBookmarksAndHighlights.Text = "📑";
            _tabPageBookmarksAndHighlights.UseVisualStyleBackColor = true;
            // 
            // _tabPageAudio
            // 
            _tabPageAudio.Location = new Point(4, 34);
            _tabPageAudio.Name = "_tabPageAudio";
            _tabPageAudio.Padding = new Padding(3);
            _tabPageAudio.Size = new Size(555, 996);
            _tabPageAudio.TabIndex = 5;
            _tabPageAudio.Text = "🎤";
            _tabPageAudio.UseVisualStyleBackColor = true;
            // 
            // moreMenu
            // 
            moreMenu.Name = "moreMenu";
            moreMenu.Size = new Size(61, 4);
            // 
            // _toastLabel
            // 
            _toastLabel.AutoSize = true;
            _toastLabel.BackColor = Color.FromArgb(60, 60, 60);
            _toastLabel.Font = new Font("Microsoft YaHei UI", 10F);
            _toastLabel.ForeColor = Color.White;
            _toastLabel.Location = new Point(284, 261);
            _toastLabel.Name = "_toastLabel";
            _toastLabel.Padding = new Padding(15, 8, 15, 8);
            _toastLabel.Size = new Size(30, 36);
            _toastLabel.TabIndex = 2;
            _toastLabel.TextAlign = ContentAlignment.MiddleCenter;
            _toastLabel.Visible = false;
            // 
            // PdfReaderForm
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1863, 1034);
            Controls.Add(_splitContainerMain);
            Controls.Add(_toastLabel);
            Name = "PdfReaderForm";
            StartPosition = FormStartPosition.CenterScreen;
            DragDrop += PdfReaderFormV2_DragDrop;
            DragEnter += PdfReaderFormV2_DragEnter;
            _splitContainerMain.Panel1.ResumeLayout(false);
            _splitContainerMain.Panel2.ResumeLayout(false);
            ((ISupportInitialize)_splitContainerMain).EndInit();
            _splitContainerMain.ResumeLayout(false);
            _panelPdf.ResumeLayout(false);
            _panelNavigation.ResumeLayout(false);
            _toolbarGroupTools.ResumeLayout(false);
            panel2.ResumeLayout(false);
            _toolbarGroupView.ResumeLayout(false);
            _toolbarGroupView.PerformLayout();
            ((ISupportInitialize)_trackBarZoom).EndInit();
            pnlPage.ResumeLayout(false);
            pnlPage.PerformLayout();
            _panelAnnotationOptions.ResumeLayout(false);
            _panelAnnotationOptions.PerformLayout();
            ((ISupportInitialize)_trackBarThickness).EndInit();
            _panelColor.ResumeLayout(false);
            _statusBar.ResumeLayout(false);
            _statusBar.PerformLayout();
            ((ISupportInitialize)_pictureBoxPdf).EndInit();
            _contextMenuPdf.ResumeLayout(false);
            _ocrPanel.ResumeLayout(false);
            ((ISupportInitialize)_ocrPictureBox).EndInit();
            _pageTransitionOverlay.ResumeLayout(false);
            _panelLeftContainer.ResumeLayout(false);
            _tabControlLeft.ResumeLayout(false);
            _tabPageFiles.ResumeLayout(false);
            _tabPageFiles.PerformLayout();
            _tabPageThumbnails.ResumeLayout(false);
            _panelThumbnails.ResumeLayout(false);
            _tabPageTranslate.ResumeLayout(false);
            _tabPageBookmarksAndHighlights.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
