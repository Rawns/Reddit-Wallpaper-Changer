using System;

namespace Reddit_Wallpaper_Changer.Wallpaper
{
    public interface ISearchUrlProvider
    {
        string GetSearchUrl(int wallpaperGrabType, IProgress<string> progress);
    }
}
