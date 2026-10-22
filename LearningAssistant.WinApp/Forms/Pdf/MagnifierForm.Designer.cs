namespace LearningAssistant.Forms.Pdf;

partial class MagnifierForm
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        pictureBox = new PictureBox();
        ((System.ComponentModel.ISupportInitialize)pictureBox).BeginInit();
        SuspendLayout();
        // 
        // pictureBox
        // 
        pictureBox.BorderStyle = BorderStyle.FixedSingle;
        pictureBox.Cursor = Cursors.SizeAll;
        pictureBox.Dock = DockStyle.Fill;
        pictureBox.Location = new Point(0, 0);
        pictureBox.Name = "pictureBox";
        pictureBox.Size = new Size(336, 360);
        pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
        pictureBox.TabIndex = 0;
        pictureBox.TabStop = false;
        pictureBox.MouseDown += PictureBox_MouseDown;
        pictureBox.MouseMove += PictureBox_MouseMove;
        pictureBox.MouseUp += PictureBox_MouseUp;
        // 
        // MagnifierForm
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        ClientSize = new Size(336, 360);
        Controls.Add(pictureBox);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        Name = "MagnifierForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Text = "放大镜";
        TopMost = true;
        ((System.ComponentModel.ISupportInitialize)pictureBox).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private PictureBox pictureBox;
}
