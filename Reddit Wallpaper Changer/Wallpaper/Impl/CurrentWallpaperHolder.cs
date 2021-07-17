using Reddit_Wallpaper_Changer.Model;
using System.IO;

namespace Reddit_Wallpaper_Changer.Wallpaper.Impl
{
    public class CurrentWallpaperHolder : ICurrentWallpaperHolder
    {
        private ImageInfo CurrentImageInfo { get; set; }

        public ImageInfo GetCurrentWallpaper()
        {
            //return CurrentImageInfo;
            CurrentImageInfo = new ImageInfo(Properties.Settings.Default.url, Properties.Settings.Default.threadTitle, Properties.Settings.Default.threadID, Properties.Settings.Default.url);
            return CurrentImageInfo;
        }

        public void SetCurrentWallpaper(ImageInfo imageInfo, string wallpaperFilePath)
        {
            CurrentImageInfo = imageInfo;

            string extention = Path.GetExtension(wallpaperFilePath);
            Properties.Settings.Default.currentWallpaperFile = wallpaperFilePath;
            Properties.Settings.Default.url = imageInfo.Url;
            Properties.Settings.Default.threadTitle = imageInfo.Title;
            Properties.Settings.Default.currentWallpaperUrl = imageInfo.Url;
            Properties.Settings.Default.currentWallpaperName = imageInfo.Title + extention;
            Properties.Settings.Default.threadID = imageInfo.ThreadId;
            Properties.Settings.Default.Save();
        }
    }
}
