using Newtonsoft.Json.Linq;
using Reddit_Wallpaper_Changer.Log;
using Reddit_Wallpaper_Changer.Model;
using Reddit_Wallpaper_Changer.Settings;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Reddit_Wallpaper_Changer.Wallpaper.Impl
{
    public class ImageLoader : IImageLoader
    {
        public static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPEG", ".BMP", ".GIF", ".PNG" };
        private ILoadSettings Settings { get; }

        public ImageLoader(ILoadSettings settings)
        {
            Settings = settings;
        }

        public async Task<string> TryLoadImageAsync(ImageInfo imageInfo, IProgress<string> progress)
        {
            Logging.LogMessageToFile("Loading wallpaper.", 0);

            string url = await GetUrlAsync(imageInfo);
            Uri uri = new Uri(url);
            string extention = Path.GetExtension(uri.LocalPath);
            string filename = imageInfo.ThreadId + extention;
            string wallpaperFilePath = Path.Combine(Path.GetTempPath(), filename);

            Logging.LogMessageToFile("URL: " + url, 0);
            Logging.LogMessageToFile("Title: " + imageInfo.Title, 0);
            Logging.LogMessageToFile("Thread ID: " + imageInfo.ThreadId, 0);

            if (!ImageExtensions.Contains(extention.ToUpper()))
            {
                Logging.LogMessageToFile("Wallpaper URL failed validation: " + extention.ToUpper(), 1);
                return string.Empty;
            }

            if (File.Exists(wallpaperFilePath))
            {
                try
                {
                    File.Delete(wallpaperFilePath);
                }
                catch (IOException Ex)
                {
                    Logging.LogMessageToFile("Unexpected error deleting old wallpaper: " + Ex.Message, 1);
                }
            }

            try
            {
                using (WebClient wc = Proxy.setProxy())
                {
                    await wc.DownloadFileTaskAsync(uri.AbsoluteUri, @wallpaperFilePath);
                }

                if (Settings.GetFitWallpaper())
                {
                    string screenWidth = SystemInformation.VirtualScreen.Width.ToString();
                    string screenHeight = SystemInformation.VirtualScreen.Height.ToString();

                    var img = Image.FromFile(wallpaperFilePath);
                    string wallpaperWidth = img.Width.ToString();
                    string wallpaperHeight = img.Height.ToString();

                    if (!screenWidth.Equals(wallpaperWidth) || !screenHeight.Equals(wallpaperHeight))
                    {
                        Logging.LogMessageToFile("Wallpaper size mismatch. Screen: " + screenWidth + "x" + screenHeight + ", Wallpaper: " + wallpaperWidth + "x" + wallpaperHeight, 1);
                        progress.Report("Wallpaper resolution mismatch.");
                        return string.Empty;
                    }
                }

                return wallpaperFilePath;
            }
            catch (WebException Ex)
            {
                Logging.LogMessageToFile("Unexpected Error: " + Ex.Message, 2);
            }

            return string.Empty;
        }

        private async Task<string> GetUrlAsync(ImageInfo imageInfo)
        {
            Uri uri = new Uri(imageInfo.Url);
            string extention = Path.GetExtension(uri.LocalPath);
            string url = imageInfo.Url.ToLower();

            if (url.Contains("imgur.com/a/"))
            {
                string imgurid = url.Replace("https://", "").Replace("http://", "").Replace("imgur.com/a/", "").Replace("//", "").Replace("/", "");
                var httpWebRequest = CreateImgurRequest("https://api.imgur.com/3/album/" + imgurid);

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    string jsonresult = await streamReader.ReadToEndAsync();
                    JToken imgurResult = JToken.Parse(jsonresult)["data"]["images"];
                    int i = imgurResult.Count();
                    int selc = 0;
                    if (imgurResult.Count() == 1)
                    {
                        selc = new Random().Next(0, i - 1);
                    }
                    JToken img = imgurResult.ElementAt(selc);
                    url = img["link"].ToString();
                }
            }
            else if (!ImageExtensions.Contains(extention.ToUpper()) && (url.Contains("deviantart")))
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://backend.deviantart.com/oembed?url=" + url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Accept = "*/*";
                httpWebRequest.Method = "GET";

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    string jsonresult = await streamReader.ReadToEndAsync();
                    JToken imgResult = JToken.Parse(jsonresult);
                    url = imgResult["url"].ToString();
                }
            }
            else if (!ImageExtensions.Contains(extention.ToUpper()) && (url.Contains("imgur.com")))
            {
                string imgurid = url.Replace("https://", "").Replace("http://", "").Replace("imgur.com/", "").Replace("//", "").Replace("/", "");
                var httpWebRequest = CreateImgurRequest("https://api.imgur.com/3/image/" + imgurid);

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    string jsonresult = await streamReader.ReadToEndAsync();
                    JToken imgResult = JToken.Parse(jsonresult);
                    url = imgResult["data"]["link"].ToString();
                }
            }

            return url;
        }

        private HttpWebRequest CreateImgurRequest(string url)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Accept = "*/*";
            httpWebRequest.Method = "GET";
            httpWebRequest.Headers.Add("Authorization", Settings.GetImgurDevKey());

            return httpWebRequest;
        }
    }
}
