using Reddit_Wallpaper_Changer.Model;
using System;
using System.Threading.Tasks;

namespace Reddit_Wallpaper_Changer.Wallpaper
{
    public interface IImageInfoProvider
    {
        Task<ImageInfo> GetImageInfoAsync(string searchUrl, int wallpaperGrabType, IProgress<string> progress);
    }
}
