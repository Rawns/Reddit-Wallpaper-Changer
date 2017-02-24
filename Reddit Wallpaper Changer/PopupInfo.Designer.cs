namespace Reddit_Wallpaper_Changer
{
    partial class PopupInfo
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.imgWallpaper = new System.Windows.Forms.PictureBox();
            this.lnkWallpaper = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)(this.imgWallpaper)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(16, 23);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(78, 13);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Wallpaper Title";
            // 
            // imgWallpaper
            // 
            this.imgWallpaper.BackgroundImage = global::Reddit_Wallpaper_Changer.Properties.Resources.display_enabled;
            this.imgWallpaper.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.imgWallpaper.Location = new System.Drawing.Point(188, 13);
            this.imgWallpaper.Name = "imgWallpaper";
            this.imgWallpaper.Size = new System.Drawing.Size(100, 65);
            this.imgWallpaper.TabIndex = 3;
            this.imgWallpaper.TabStop = false;
            // 
            // lnkWallpaper
            // 
            this.lnkWallpaper.AutoSize = true;
            this.lnkWallpaper.Location = new System.Drawing.Point(16, 50);
            this.lnkWallpaper.Name = "lnkWallpaper";
            this.lnkWallpaper.Size = new System.Drawing.Size(125, 13);
            this.lnkWallpaper.TabIndex = 4;
            this.lnkWallpaper.TabStop = true;
            this.lnkWallpaper.Text = "Taken from /r/Wallpaper";
            // 
            // PopupInfo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 90);
            this.Controls.Add(this.lnkWallpaper);
            this.Controls.Add(this.imgWallpaper);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "PopupInfo";
            this.Opacity = 0.5D;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.main_FormClosing);
            this.Load += new System.EventHandler(this.PopupInfo_Load);
            ((System.ComponentModel.ISupportInitialize)(this.imgWallpaper)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.PictureBox imgWallpaper;
        private System.Windows.Forms.LinkLabel lnkWallpaper;
    }
}