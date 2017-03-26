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
            this.imgWallpaper = new System.Windows.Forms.PictureBox();
            this.lnkWallpaper = new System.Windows.Forms.LinkLabel();
            this.txtWallpaperTitle = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.imgWallpaper)).BeginInit();
            this.SuspendLayout();
            // 
            // imgWallpaper
            // 
            this.imgWallpaper.BackgroundImage = global::Reddit_Wallpaper_Changer.Properties.Resources.display_enabled;
            this.imgWallpaper.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.imgWallpaper.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.imgWallpaper.Location = new System.Drawing.Point(183, 12);
            this.imgWallpaper.Name = "imgWallpaper";
            this.imgWallpaper.Size = new System.Drawing.Size(105, 66);
            this.imgWallpaper.TabIndex = 3;
            this.imgWallpaper.TabStop = false;
            // 
            // lnkWallpaper
            // 
            this.lnkWallpaper.AutoSize = true;
            this.lnkWallpaper.Location = new System.Drawing.Point(3, 74);
            this.lnkWallpaper.Name = "lnkWallpaper";
            this.lnkWallpaper.Size = new System.Drawing.Size(125, 13);
            this.lnkWallpaper.TabIndex = 4;
            this.lnkWallpaper.TabStop = true;
            this.lnkWallpaper.Text = "Taken from /r/Wallpaper";
            // 
            // txtWallpaperTitle
            // 
            this.txtWallpaperTitle.BackColor = System.Drawing.SystemColors.InactiveCaption;
            this.txtWallpaperTitle.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtWallpaperTitle.Enabled = false;
            this.txtWallpaperTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtWallpaperTitle.Location = new System.Drawing.Point(7, 6);
            this.txtWallpaperTitle.Multiline = true;
            this.txtWallpaperTitle.Name = "txtWallpaperTitle";
            this.txtWallpaperTitle.Size = new System.Drawing.Size(170, 65);
            this.txtWallpaperTitle.TabIndex = 5;
            this.txtWallpaperTitle.Text = "Wallpaper Title Here";
            this.txtWallpaperTitle.TextChanged += new System.EventHandler(this.txtWallpaperTitle_TextChanged);
            // 
            // PopupInfo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.InactiveCaption;
            this.ClientSize = new System.Drawing.Size(300, 90);
            this.Controls.Add(this.txtWallpaperTitle);
            this.Controls.Add(this.lnkWallpaper);
            this.Controls.Add(this.imgWallpaper);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "PopupInfo";
            this.Opacity = 0D;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Load += new System.EventHandler(this.PopupInfo_Load);
            ((System.ComponentModel.ISupportInitialize)(this.imgWallpaper)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.PictureBox imgWallpaper;
        private System.Windows.Forms.LinkLabel lnkWallpaper;
        private System.Windows.Forms.TextBox txtWallpaperTitle;
    }
}