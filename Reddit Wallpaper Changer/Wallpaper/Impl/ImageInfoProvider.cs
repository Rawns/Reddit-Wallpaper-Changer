using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reddit_Wallpaper_Changer.Log;
using Reddit_Wallpaper_Changer.Model;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Reddit_Wallpaper_Changer.Wallpaper.Impl
{
    public class ImageInfoProvider : IImageInfoProvider
    {
        private IImageUrlValidator ImageUrlValidator { get; }
        public ImageInfoProvider(IImageUrlValidator imageUrlValidator)
        {
            ImageUrlValidator = imageUrlValidator;
        }

        public async Task<ImageInfo> GetImageInfoAsync(string searchUrl, int wallpaperGrabType, IProgress<string> progress)
        {
            var jsonData = await DownLoadString(searchUrl, progress);
            if (jsonData.Length == 0)
            {
                progress.Report("Subreddit Probably Doesn't Exist");
                Logging.LogMessageToFile("Subreddit probably does not exist.", 1);
                return null;
            }

            try
            {
                JToken redditResult = ParseJson(wallpaperGrabType, jsonData);
                if (!redditResult.HasValues)
                {
                    progress.Report("No results found, searching again.");
                    Logging.LogMessageToFile("No search results, trying to change wallpaper again.", 0);
                    return null;
                }

                JToken token = redditResult.Last;
                if (wallpaperGrabType != 0)
                {
                    Logging.LogMessageToFile("Found a wallpaper! Title: " + token["data"]["title"].ToString() + ", URL: " + token["data"]["url"].ToString() + ", ThreadID: " + token["data"]["id"].ToString(), 0);

                    if (await ImageUrlValidator.CheckImageUrl(token["data"]["url"].ToString()))
                    {
                        return new ImageInfo(token["data"]["url"].ToString(),
                            token["data"]["title"].ToString(),
                            token["data"]["id"].ToString(),
                            "http://reddit.com" + token["data"]["permalink"].ToString()
                        );
                    }
                    else
                    {
                        progress.Report("The selected URL is not for an image or image has been removed");
                        Logging.LogMessageToFile("Not a direct wallpaper URL, searching again.", 1);
                        return null;
                    }
                }
                else
                {
                    var random = new Random();
                    token = redditResult.ElementAt(random.Next(0, redditResult.Count() - 1));
                    Logging.LogMessageToFile("Found a wallpaper! Title: " + token["data"]["title"].ToString() + ", URL: " + token["data"]["url"].ToString() + ", ThreadID: " + token["data"]["id"].ToString(), 0);

                    // check URL
                    if (await ImageUrlValidator.CheckImageUrl(token["data"]["url"].ToString()))
                    {
                        return new ImageInfo(token["data"]["url"].ToString(),
                            token["data"]["title"].ToString(),
                            token["data"]["id"].ToString(),
                            "http://reddit.com" + token["data"]["permalink"].ToString()
                        );
                    }
                    else
                    {
                        progress.Report("The selected URL is not for an image or image has been removed");
                        Logging.LogMessageToFile("Not a direct wallpaper URL, searching again.", 1);
                        return null;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                progress.Report("Your search query is bringing up no results.");
                Logging.LogMessageToFile("No results from the search query.", 1);
            }
            catch (JsonReaderException ex)
            {
                progress.Report("Unexpected error: " + ex.Message);
                Logging.LogMessageToFile("Unexpected error: " + ex.Message, 2);
            }

            return null;
        }

        private async Task<string> DownLoadString(string formURL, IProgress<string> progress)
        {
            using (var wc = Proxy.setProxy())
            {
                try
                {
                    Logging.LogMessageToFile("Searching Reddit for a wallpaper.", 0);
                    return await wc.DownloadStringTaskAsync(formURL);
                }
                catch (WebException ex)
                {
                    progress.Report(ex.Message);
                    Logging.LogMessageToFile("Reddit server error: " + ex.Message, 2);
                }
                catch (Exception ex)
                {
                    progress.Report("Error downloading search results.");
                    Logging.LogMessageToFile("Error downloading search results: " + ex.Message, 2);
                }
            }

            return string.Empty;
        }

        private static JToken ParseJson(int wallpaperGrabType, string jsonData)
        {
            if (wallpaperGrabType == 9)
            {
                return JToken.Parse(jsonData).First["data"]["children"];
            }
            else
            {
                return JToken.Parse(jsonData)["data"]["children"];
            }
        }
    }
}