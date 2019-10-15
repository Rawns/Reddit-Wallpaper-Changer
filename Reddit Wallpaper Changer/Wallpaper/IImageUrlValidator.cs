using System.Threading.Tasks;

namespace Reddit_Wallpaper_Changer.Wallpaper
{
    public interface IImageUrlValidator
    {
        Task<bool> CheckImageUrl(string url);
    }
}
