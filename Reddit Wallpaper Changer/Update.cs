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
        private Form1 form1;
        public Update(string latestVersion, Form1 form1)
        {
            InitializeComponent();
            this.latestVersion = latestVersion;
            textBox1.Text = latestVersion.Replace("\n", System.Environment.NewLine);
            this.form1 = form1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Hide();
            button2.Hide();
            progressBar.Visible = true;

            WebClient webClient = new WebClient();
            webClient.DownloadProgressChanged += (s, a) =>
            {
                progressBar.Value = a.ProgressPercentage;
            };
            webClient.DownloadFileCompleted += (s, a) =>
            {
                progressBar.Visible = false;
                label2.Visible = true;
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
                catch
                {
                }


            };
            System.IO.File.Move(System.Reflection.Assembly.GetExecutingAssembly().Location, System.Reflection.Assembly.GetExecutingAssembly().Location + ".old");
            webClient.DownloadFileAsync(new Uri("https://github.com/Ugleh/redditwallpaperchanger/raw/release/Reddit%20Wallpaper%20Changer.exe"),
                @"" + System.Reflection.Assembly.GetExecutingAssembly().Location);

        }

        private void Update_Load(object sender, EventArgs e)
        {
            this.BringToFront();
            this.TopMost = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
            form1.changeWallpaperTimerEnabled();
        }

        private void Update_FormClosing(object sender, FormClosingEventArgs e)
        {
            form1.changeWallpaperTimerEnabled();
        }
    }
}
