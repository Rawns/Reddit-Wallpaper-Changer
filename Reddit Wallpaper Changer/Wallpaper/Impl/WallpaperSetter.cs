using Reddit_Wallpaper_Changer.Log;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Reddit_Wallpaper_Changer.Wallpaper.Impl
{
    public class WallpaperSetter : IWallpaperSetter
    {
        private Database Database { get; }

        public WallpaperSetter(Database database)
        {
            Database = database;
        }

        public async Task SetWallpaperAsync(string wallpaperFilePath, IProgress<string> progress)
        {
            Logging.LogMessageToFile("Setting wallpaper.", 0);

            if (Properties.Settings.Default.wallpaperFade == true)
            {
                Logging.LogMessageToFile("Applying wallpaper using Active Desktop.", 0);
                ActiveDesktop();
                await Task.Delay(1000);
                ActiveDesktop.IActiveDesktop _activeDesktop = Reddit_Wallpaper_Changer.ActiveDesktop.ActiveDesktopWrapper.GetActiveDesktop();
                _activeDesktop.SetWallpaper(wallpaperFilePath, 0);
                _activeDesktop.ApplyChanges(Reddit_Wallpaper_Changer.ActiveDesktop.AD_Apply.ALL | Reddit_Wallpaper_Changer.ActiveDesktop.AD_Apply.FORCE);
                Marshal.ReleaseComObject(_activeDesktop);
            }
            else
            {
                Logging.LogMessageToFile("Applying wallpaper using standard process.", 0);
                Reddit_Wallpaper_Changer.ActiveDesktop.SystemParametersInfo(Reddit_Wallpaper_Changer.ActiveDesktop.SPI_SETDESKWALLPAPER, 0, wallpaperFilePath, Reddit_Wallpaper_Changer.ActiveDesktop.SPIF_UPDATEINIFILE | Reddit_Wallpaper_Changer.ActiveDesktop.SPIF_SENDWININICHANGE);
            }

            progress.Report("Wallpaper Changed!");
            Logging.LogMessageToFile("Wallpaper changed!", 0);
        }

        private void ActiveDesktop()
        {
            IntPtr result = IntPtr.Zero;
            WinApiProvider.SendMessageTimeout(WinApiProvider.FindWindow("Progman", IntPtr.Zero), 0x52c, IntPtr.Zero, IntPtr.Zero, 0, 500, out result);
        }
    }
}
