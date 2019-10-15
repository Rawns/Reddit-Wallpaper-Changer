namespace Reddit_Wallpaper_Changer.Wallpaper
{
    public interface IWallpaperSaver
    {
        bool SaveWallpaper(string fileName);

        bool IsAlreadySavedWallpaper(string fileName);

        bool SaveSelectedWallpaper(string url, string threadid, string title);
    }
}
