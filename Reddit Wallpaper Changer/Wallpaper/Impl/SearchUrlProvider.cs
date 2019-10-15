using Reddit_Wallpaper_Changer.Log;
using Reddit_Wallpaper_Changer.Settings;
using System;
using System.Web;

namespace Reddit_Wallpaper_Changer.Wallpaper.Impl
{
    internal class SearchUrlProvider : ISearchUrlProvider
    {
        private const string redditURL = "http://www.reddit.com/r/";
        private static readonly string[] randomT = { "&t=day", "&t=year", "&t=all", "&t=month", "&t=week" };
        private static readonly string[] randomSort = { "&sort=relevance", "&sort=hot", "&sort=top", "&sort=comments", "&sort=new" };

        private ISearchSettings SearchSettings { get; }

        public SearchUrlProvider(ISearchSettings searchSettings)
        {
            SearchSettings = searchSettings;
        }

        public string GetSearchUrl(int wallpaperGrabType, IProgress<string> progress)
        {
            string query = HttpUtility.UrlEncode(SearchSettings.GetSearchQuerry()) + "+self%3Ano+((url%3A.png+OR+url%3A.jpg+OR+url%3A.jpeg)+OR+(url%3Aimgur.png+OR+url%3Aimgur.jpg+OR+url%3Aimgur.jpeg)+OR+(url%3Adeviantart))";

            var sub = GetSubReddit();
            progress.Report("Searching /r/" + sub + " for a wallpaper...");
            Logging.LogMessageToFile("Selected sub to search: " + sub, 0);

            string formURL = redditURL;
            if (sub.Equals(""))
            {
                formURL += "all";
            }
            else
            {
                if (sub.Contains("/m/"))
                {
                    formURL = "http://www.reddit.com/" + SearchSettings.GetSubReddits().Replace("http://", "").Replace("https://", "").Replace("user/", "u/");
                }
                else
                {
                    formURL += sub;
                }
            }

            switch (wallpaperGrabType)
            {
                case 0:
                    // Random
                    var random = new Random();
                    formURL += "/search.json?q=" + query + randomSort[random.Next(0, 4)] + randomT[random.Next(0, 5)] + "&restrict_sr=on";
                    Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                    break;

                case 1:
                    // Newest
                    formURL += "/search.json?q=" + query + "&sort=new&restrict_sr=on";
                    Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                    break;

                case 2:
                    // Hot Today
                    formURL += "/search.json?q=" + query + "&sort=hot&restrict_sr=on&t=day";
                    Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                    break;

                case 3:
                    // Top Last Hour
                    formURL += "/search.json?q=" + query + "&sort=top&restrict_sr=on&t=hour";
                    Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                    break;

                case 4:
                    // Top Today
                    formURL += "/search.json?q=" + query + "&sort=top&restrict_sr=on&t=day";
                    Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                    break;

                case 5:
                    // Top Week
                    formURL += "/search.json?q=" + query + "&sort=top&restrict_sr=on&t=week";
                    Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                    break;

                case 6:
                    // Top Month
                    formURL += "/search.json?q=" + query + "&sort=top&restrict_sr=on&t=month";
                    Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                    break;

                case 7:
                    // Top Year
                    formURL += "/search.json?q=" + query + "&sort=top&restrict_sr=on&t=year";
                    Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                    break;

                case 8:
                    // Top All Time
                    formURL += "/search.json?q=" + query + "&sort=top&restrict_sr=on&t=all";
                    Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                    break;

                case 9:
                    // Truly Random
                    formURL += "/random.json?p=" + (Guid.NewGuid().ToString());
                    Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                    break;
            }

            return formURL;
        }

        private string GetSubReddit()
        {
            var subreddits = SearchSettings.GetSubReddits().Replace(" ", "").Replace("www.reddit.com/", "").Replace("reddit.com/", "").Replace("http://", "").Replace("/r/", "");
            string[] subs = subreddits.Split('+');
            if (subs.Length == 0)
            {
                return string.Empty;
            }
            if (subs.Length == 1)
            {
                return subs[0];
            }
            var random = new Random();
            return subs[random.Next(0, subs.Length)];
        }
    }
}