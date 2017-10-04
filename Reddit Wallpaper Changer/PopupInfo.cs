using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Reddit_Wallpaper_Changer
{
    public partial class PopupInfo : Form
    {
        public string title { get; set; }
        public string threadid { get; set; }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        private const int SW_SHOWNOACTIVATE = 4;
        private const int HWND_TOPMOST = -1;
        private const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        static extern bool SetWindowPos(
             int hWnd,             // Window handle
             int hWndInsertAfter,  // Placement-order handle
             int X,                // Horizontal position
             int Y,                // Vertical position
             int cx,               // Width
             int cy,               // Height
             uint uFlags);         // Window positioning flags

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        static void ShowInactiveTopmost(Form frm)
        {
            ShowWindow(frm.Handle, SW_SHOWNOACTIVATE);
            SetWindowPos(frm.Handle.ToInt32(), HWND_TOPMOST,
            frm.Left, frm.Top, frm.Width, frm.Height,
            SWP_NOACTIVATE);
        }


        public PopupInfo(string threadid, string title)
        {
            InitializeComponent();
            this.threadid = threadid;
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
            this.txtWallpaperTitle.Text = title;
            this.lnkWallpaper.Text = "http://www.reddit.com/" + threadid;

            Bitmap img = new Bitmap(Properties.Settings.Default.currentWallpaperFile);            
            this.imgWallpaper.BackgroundImage = img;
            this.imgWallpaper.BackgroundImageLayout = ImageLayout.Stretch;
            
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

        private void txtWallpaperTitle_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
