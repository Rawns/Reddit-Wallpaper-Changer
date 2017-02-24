using System;
using System.Drawing;
using System.Windows.Forms;

namespace Reddit_Wallpaper_Changer
{
    public partial class PopupInfo : Form
    {
        public PopupInfo()
        {
            InitializeComponent();
        }

        Timer t1 = new Timer();
        Timer timer = new Timer();

        //======================================================================
        // Load form by triggering a Fade In
        //======================================================================
        private void PopupInfo_Load(object sender, EventArgs e)
        {
            this.BringToFront();
            this.lblTitle.Text = Properties.Settings.Default.currentWallpaperName;
            this.lnkWallpaper.Text = Properties.Settings.Default.currentWallpaperUrl;
            Bitmap img = new Bitmap(Properties.Settings.Default.currentWallpaperFile);
            this.imgWallpaper.BackgroundImage = img;
            this.imgWallpaper.BackgroundImageLayout = ImageLayout.Zoom;

            Opacity = 0;     
            t1.Interval = 5;  //we'll increase the opacity every 10ms
            t1.Tick += new EventHandler(fadeIn);  //this calls the function that changes opacity 
            t1.Start();
            

            timer.Interval = 5000;
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }

        //======================================================================
        // Fade In
        //======================================================================
        void fadeIn(object sender, EventArgs e)
        {
            if (Opacity >= 1)
                t1.Stop();   //this stops the timer if the form is completely displayed
            else
                Opacity += 0.05;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            this.Close();
        }

        //======================================================================
        // Fade out on Close
        //======================================================================
        private void main_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;   

            timer.Stop();
            timer.Dispose();

            t1.Tick += new EventHandler(fadeOut);  
            t1.Start();

            if (Opacity == 0)  
                e.Cancel = false;   

        }

        //======================================================================
        // Fade Out
        //======================================================================
        void fadeOut(object sender, EventArgs e)
        {
            if (Opacity <= 0) 
            {
                t1.Stop();
                t1.Dispose();
                Close();   
            }
            else
                Opacity -= 0.05;
        }

    }
}
