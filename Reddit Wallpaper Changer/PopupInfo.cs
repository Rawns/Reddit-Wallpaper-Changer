using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Reddit_Wallpaper_Changer
{
    public partial class PopupInfo : Form
    {

        public string url { get; set; }
        public string title { get; set; }

        public PopupInfo(string url, string title)
        {
            InitializeComponent();
            this.url = url;
            this.title = title;
                
        }
        
        Timer fade = new Timer();
        Timer timer = new Timer();

        //======================================================================
        // Load form by triggering a Fade In
        //======================================================================
        private void PopupInfo_Load(object sender, EventArgs e)
        {
            this.BringToFront();
            // this.BackColor = 
            this.txtWallpaperTitle.Text = title;
            this.lnkWallpaper.Text = url;
            Bitmap img = new Bitmap(Properties.Settings.Default.currentWallpaperFile);
            this.imgWallpaper.BackgroundImage = img;
            this.imgWallpaper.BackgroundImageLayout = ImageLayout.Zoom;
            // this.imgWallpaper.BorderStyle = BorderStyle.FixedSingle;

            Opacity = 0;     
            fade.Interval = 10;  
            fade.Tick += new EventHandler(fadeIn);
            fade.Start();
            
            timer.Interval = 3000;
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();            
        }

        //======================================================================
        // Fade In
        //======================================================================
        void fadeIn(object sender, EventArgs e)
        {
            if (Opacity >= 1)
            {
                fade.Stop();
                timer.Start();
            }
            else
                Opacity += 0.05;
        }


        //======================================================================
        // Close once tick has ran
        //======================================================================
        void timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            timer.Dispose();

            fade.Tick += new EventHandler(fadeOut);
            fade.Start();

            if (Opacity == 0)
                Close();
        }

        //======================================================================
        // Fade Out
        //======================================================================
        void fadeOut(object sender, EventArgs e)
        {
            if (Opacity <= 0) 
            {
                fade.Stop();
                fade.Dispose();
                Close();   
            }
            else
                Opacity -= 0.05;
        }
    }
}
