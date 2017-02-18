using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Reddit_Wallpaper_Changer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        ///

        static Mutex mutex = new Mutex(false, "RedditWallpaperChanger_byUgleh");



        [STAThread]
        static void Main()
        {
        if (!mutex.WaitOne(TimeSpan.FromSeconds(2), false))
        {
            DialogResult dialogResult = MessageBox.Show("Run another instance of RWC?", "Reddit Wallpaper Changer", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new RWC());
            }
            else if (dialogResult == DialogResult.No)
            {
                //do something else
            }
            return;
        }

        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RWC());
        }
        finally { mutex.ReleaseMutex(); } // I find this more explicit
        }
    }
}
