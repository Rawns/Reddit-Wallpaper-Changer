using Reddit_Wallpaper_Changer.Log;
using Reddit_Wallpaper_Changer.Wallpaper;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Reddit_Wallpaper_Changer.Wallpaper.Impl
{
    /// <summary>
    /// Validation class for all validation processes within RWC
    /// </summary>
    class ImageUrlValidator : IImageUrlValidator
    {
        //======================================================================
        // Check that the selected wallpaper URL is for an image
        //======================================================================
        public async Task<bool> CheckImageUrl(string url)
        {
            if (url.Contains("deviantart"))
            {
                return true;
            }

            if (url.Contains("imgur"))
            {
                return await CheckImgur(url);
            }

            return await CheckNotDevianArt(url);
        }

        private async Task<bool> CheckNotDevianArt(string url)
        {
            try
            {
                Logging.LogMessageToFile("Checking to ensure the chosen wallpaper URL is for an image.", 0);
                HttpWebRequest imageCheck = (HttpWebRequest)WebRequest.Create(url);
                imageCheck.Timeout = 5000;
                imageCheck.Method = "HEAD";
                imageCheck.AllowAutoRedirect = false;
                var imageResponse = await imageCheck.GetResponseAsync();

                // If anything other than OK, assume that image has been deleted
                if (!imageResponse.ContentType.StartsWith("image/"))
                {
                    imageCheck.Abort();
                    return false;
                }
                else
                {
                    imageCheck.Abort();
                    Logging.LogMessageToFile("The chosen URL is for an image.", 0);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile(ex.ToString(), 1);
                return false;
            }
        }

        //======================================================================
        // If the URL is an Imgur link, check the Wallpaper is still available
        //======================================================================
        private async Task<bool> CheckImgur(string url)
        {
            try
            {
                Logging.LogMessageToFile("Checking to ensure the chosen wallpaper is still available on Imgur.", 0);
                // A request for a deleted image on Imgur will return status code 302 & redirect to http://i.imgur.com/removed.png returning status code 200
                HttpWebRequest imgurRequest = (HttpWebRequest)WebRequest.Create(url);
                imgurRequest.Timeout = 5000;
                imgurRequest.Method = "HEAD";
                imgurRequest.AllowAutoRedirect = false;
                var imgurResponse = (await imgurRequest.GetResponseAsync()) as HttpWebResponse;

                if (imgurResponse.StatusCode.ToString() != "OK")
                {
                    imgurRequest.Abort();
                    return false;
                }
                else
                {
                    imgurRequest.Abort();
                    Logging.LogMessageToFile("The chosen wallpaper is still available on Imgur.", 0);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
