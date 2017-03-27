using System;
using System.Windows.Forms;
using System.Net;
using System.IO;

namespace Reddit_Wallpaper_Changer
{
    public partial class Update : Form
    {
        private string latestVersion;
        private RWC RWC;
        public Update(string latestVersion, RWC RWC)
        {
            InitializeComponent();
            this.latestVersion = latestVersion;
            textBox1.Text = latestVersion.Replace("\n", System.Environment.NewLine);
            this.RWC = RWC;
        }

        //======================================================================
        // Code to run on form load
        //======================================================================
        private void Update_Load(object sender, EventArgs e)
        {
            this.BringToFront();
            this.TopMost = true;
        }

        //======================================================================
        // Begin updating RWC
        //======================================================================
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            Logging.LogMessageToFile("Updating Reddit Wallpaper Changer.", 0);
            btnUpdate.Enabled = false;
            progressBar.Visible = true;
            string temp = Path.GetTempPath();

            try
            {
                WebClient wc = Proxy.setProxy();        
                wc.DownloadProgressChanged += (s, a) =>
                {
                    progressBar.Value = a.ProgressPercentage;
                };

                // Run this code once the download is completed
                wc.DownloadFileCompleted += (s, a) =>
                {
                    Logging.LogMessageToFile("Update successfully downloaded.", 0);
                    progressBar.Visible = false;
               
                    try
                    {
                        Logging.LogMessageToFile("Launching installer and exiting.", 0);
                        System.Diagnostics.Process.Start(temp + "Reddit.Wallpaper.Changer.msi");
                        System.Environment.Exit(0);

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error Updating: " + ex.Message, "RWC", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Logging.LogMessageToFile("Error Updating: " + ex.Message, 2);
                    }
                };

                // Download the latest MSI instaler to the users Temp folder
                wc.DownloadFileAsync(new Uri("https://github.com/Rawns/Reddit-Wallpaper-Changer/releases/download/release/Reddit.Wallpaper.Changer.msi"), temp + "Reddit.Wallpaper.Changer.msi");
                wc.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Updating: " + ex.Message, "RWC", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logging.LogMessageToFile("Error Updating: " + ex.Message, 2);
            }
        }

        //======================================================================
        // Code to run on form close
        //======================================================================
        private void Update_FormClosing(object sender, FormClosingEventArgs e)
        {
            RWC.changeWallpaperTimerEnabled();
        }
    }
}
