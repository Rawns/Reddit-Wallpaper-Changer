namespace Reddit_Wallpaper_Changer.Model
{
    public class ImageInfo
    {
        public string Url { get; }
        public string Title { get; }
        public string ThreadId { get; }
        public string ThreadLink { get; }

        public ImageInfo(string url, string title, string threadId, string threadLink)
        {
            Url = url;
            Title = title;
            ThreadId = threadId;
            ThreadLink = threadLink;
        }
    }
}
