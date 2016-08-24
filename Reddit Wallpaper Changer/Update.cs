using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
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
        // Begin updating RWC
        //======================================================================
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            btnUpdate.Enabled = false;
            btnUpdate.BackgroundImage = Properties.Resources.update_disabled;
            progressBar.Visible = true;

            try
            {
                WebClient client = new WebClient();
                client.Proxy = null;

                // Use a proxy if specified
                if (Properties.Settings.Default.useProxy == true)
                {
                    WebProxy proxy = new WebProxy(Properties.Settings.Default.proxyAddress);

                    if (Properties.Settings.Default.proxyAuth == true)
                    {
                        proxy.Credentials = new NetworkCredential(Properties.Settings.Default.proxyUser, Properties.Settings.Default.proxyPass);
                        proxy.UseDefaultCredentials = false;
                        proxy.BypassProxyOnLocal = false;
                    }

                    client.Proxy = proxy;
                }

                
                client.DownloadProgressChanged += (s, a) =>
                {
                    progressBar.Value = a.ProgressPercentage;
                };
                client.DownloadFileCompleted += (s, a) =>
                {
                    progressBar.Visible = false;
                // any other code to process the file
                    try
                    {
                        //Update Settings
                        Properties.Settings.Default.UpgradeRequired = true;
                        Properties.Settings.Default.Save();

                        //run the program again and close this one
                        System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".old", ""));


                        //close this one
                        System.Environment.Exit(0);
                        //Application.Exit();

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error Updating: " + ex.Message, "RWC", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                System.IO.File.Move(System.Reflection.Assembly.GetExecutingAssembly().Location, System.Reflection.Assembly.GetExecutingAssembly().Location + ".old");
                client.DownloadFileAsync(new Uri("https://github.com/Rawns/RWC-Source/releases/download/release/Reddit.Wallpaper.Changer.exe"),
                    @"" + System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Updating: " + ex.Message, "RWC", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
        // Code to run on form close
        //======================================================================
        private void Update_FormClosing(object sender, FormClosingEventArgs e)
        {
            RWC.changeWallpaperTimerEnabled();
        }
    }
}
