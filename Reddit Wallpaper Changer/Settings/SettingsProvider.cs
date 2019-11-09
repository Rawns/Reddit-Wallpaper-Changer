namespace Reddit_Wallpaper_Changer.Settings
{
    public class SettingsProvider : ISearchSettings, ILoadSettings
    {
        public string GetSearchQuerry()
        {
            return Properties.Settings.Default.searchQuery;
        }

        public string GetSubReddits()
        {
            return Properties.Settings.Default.subredditsUsed;
        }

        public bool GetFitWallpaper()
        {
            return Properties.Settings.Default.fitWallpaper;
        }

        public string GetImgurDevKey()
        {
            return "Client-ID 355f2ab533c2ac7";
        }
    }
}