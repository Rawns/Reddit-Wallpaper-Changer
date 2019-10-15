namespace Reddit_Wallpaper_Changer.Model
{
    public class ChangeWallpaperResult
    {
        public bool Success { get; }
        public string Title { get; }
        public string ThreadID { get; }

        public static ChangeWallpaperResult Failed()
        {
            return new ChangeWallpaperResult(false, null, null);
        }

        public ChangeWallpaperResult(bool success, string title, string threadID)
        {
            Success = success;
            Title = title;
            ThreadID = threadID;
        }
    }
}