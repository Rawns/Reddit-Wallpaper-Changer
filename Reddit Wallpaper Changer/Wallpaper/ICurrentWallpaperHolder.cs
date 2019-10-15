using Reddit_Wallpaper_Changer.Model;

namespace Reddit_Wallpaper_Changer.Wallpaper
{
    public interface ICurrentWallpaperHolder
    {
        ImageInfo GetCurrentWallpaper();

        void SetCurrentWallpaper(ImageInfo imageInfo, string wallpaperFilePath);
    }
}
