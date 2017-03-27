using System;
using System.Net;


namespace Reddit_Wallpaper_Changer
{
    /// <summary>
    /// Validation class for all validation processes within RWC
    /// </summary>
    class Validation
    {
        //======================================================================
        // Check that the selected wallpaper URL is for an image
        //======================================================================
        public static bool checkImg(string url)
        {
            try
            {
                if (!url.Contains("deviantart"))
                {
                    Logging.LogMessageToFile("Checking to ensure the chosen walllpaper URL is for an image.", 0);
                    HttpWebRequest imageCheck = (HttpWebRequest)WebRequest.Create(url);
                    imageCheck.Timeout = 5000;
                    imageCheck.Method = "HEAD";
                    imageCheck.AllowAutoRedirect = false;
                    var imageResponse = imageCheck.GetResponse();

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
                return true;
            }
            catch(Exception ex)
            {
                return true;
            }
        }

        //======================================================================
        // If the URL is an Imgur link, check the Wallpaper is still available
        //======================================================================
        public static bool checkImgur(string url)
        {
            try
            {
                if (url.Contains("imgur"))
                {
                    Logging.LogMessageToFile("Checking to ensure the chosen walllpaper is still available on Imgur.", 0);
                    // A request for a deleted image on Imgur will return status code 302 & redirect to http://i.imgur.com/removed.png returning status code 200
                    HttpWebRequest imgurRequest = (HttpWebRequest)WebRequest.Create(url);
                    imgurRequest.Timeout = 5000;
                    imgurRequest.Method = "HEAD";
                    imgurRequest.AllowAutoRedirect = false;
                    HttpWebResponse imgurResponse = imgurRequest.GetResponse() as HttpWebResponse;

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
                return true;
            }
            catch
            {
                return true;
            }
        }
    }
}
