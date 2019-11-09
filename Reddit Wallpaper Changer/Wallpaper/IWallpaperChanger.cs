using Reddit_Wallpaper_Changer.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Reddit_Wallpaper_Changer.Wallpaper
{
    public interface IWallpaperChanger
    {
        Task<ChangeWallpaperResult> ChangeWallpaperAsync(IProgress<string> progress, CancellationToken token);

        Task<ChangeWallpaperResult> SetWallpaperAsync(ImageInfo imageInfo, IProgress<string> progress);
    }
}
