using System;
using System.Windows.Forms;
using System.Net;
using System.IO;
using Reddit_Wallpaper_Changer.Log;

namespace Reddit_Wallpaper_Changer
{
    public partial class Update : Form
    {
        private const string latestInstallerUrl = "https://github.com/Rawns/Reddit-Wallpaper-Changer/releases/download/release/Reddit.Wallpaper.Changer.msi";
        private string latestVersion;
        public Update(string latestVersion)
        {
            InitializeComponent();
            this.latestVersion = latestVersion;
            textBox1.Text = latestVersion.Replace("\n", Environment.NewLine);
        }

        //======================================================================
        // Code to run on form load
        //======================================================================
        private void Update_Load(object sender, EventArgs e)
        {
            BringToFront();
            TopMost = true;
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
                using (WebClient wc = Proxy.setProxy())
                {
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
                            Environment.Exit(0);

                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error Updating: " + ex.Message, "RWC", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Logging.LogMessageToFile("Error Updating: " + ex.Message, 2);
                        }
                    };

                    // Download the latest MSI installer to the users Temp folder
                    wc.DownloadFileAsync(new Uri(latestInstallerUrl) , temp + "Reddit.Wallpaper.Changer.msi");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Updating: " + ex.Message, "RWC", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logging.LogMessageToFile("Error Updating: " + ex.Message, 2);
            }
        }
    }
}
