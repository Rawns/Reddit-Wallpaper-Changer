using Reddit_Wallpaper_Changer.Model;
using System;
using System.Threading.Tasks;

namespace Reddit_Wallpaper_Changer.Wallpaper
{
    public interface IImageLoader
    {
        Task<string> TryLoadImageAsync(ImageInfo imageInfo, IProgress<string> progress);
    }
}
