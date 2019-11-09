using Reddit_Wallpaper_Changer.Model;
using System;
using System.Threading.Tasks;

namespace Reddit_Wallpaper_Changer.Wallpaper
{
    public interface IWallpaperSetter
    {
        Task SetWallpaperAsync(string wallpaperFilePath, IProgress<string> progress);
    }
}
