using Reddit_Wallpaper_Changer.Log;
using Reddit_Wallpaper_Changer.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Reddit_Wallpaper_Changer.Wallpaper.Impl
{
    public class WallpaperChanger : IWallpaperChanger
    {
        private ISet<string> HistoryList { get; }
        private Database Database { get; }
        private IWallpaperSaver Savewallpaper { get; }
        private IThumbnailCacheBuilder ThumbnailCacheBuilder { get; }
        private ISearchUrlProvider SearchUrlProvider { get; }
        private IImageInfoProvider ImageInfoProvider { get; }
        private IImageLoader ImageLoader { get; }
        private IWallpaperSetter WallpaperSetter { get; }
        private ICurrentWallpaperHolder CurrentWallpaperHolder { get; }

        private static int isRunning = 0;

        public WallpaperChanger(Database database,
            IWallpaperSaver savewallpaper,
            IThumbnailCacheBuilder thumbnailCacheBuilder,
            ISearchUrlProvider searchUrlProvider,
            IImageInfoProvider imageInfoProvider,
            IImageLoader imageLoader,
            IWallpaperSetter wallpaperSetter,
            ICurrentWallpaperHolder currentWallpaperHolder)
        {
            Database = database;
            Savewallpaper = savewallpaper;
            ThumbnailCacheBuilder = thumbnailCacheBuilder;
            SearchUrlProvider = searchUrlProvider;
            ImageInfoProvider = imageInfoProvider;
            ImageLoader = imageLoader;
            WallpaperSetter = wallpaperSetter;
            CurrentWallpaperHolder = currentWallpaperHolder;

            HistoryList = new HashSet<string>();
        }

        public async Task<ChangeWallpaperResult> ChangeWallpaperAsync(IProgress<string> progress, CancellationToken token)
        {
            if (Interlocked.Exchange(ref isRunning, 1) == 1)
            {
                Logging.LogMessageToFile("Wallpaper is already in the changing process", 0);
                return ChangeWallpaperResult.Failed();
            }

            Logging.LogMessageToFile("Changing wallpaper.", 0);
            progress.Report("Finding New Wallpaper");

            bool success = false;
            for (int tryNumber = 0; tryNumber < 50 && !success && !token.IsCancellationRequested; tryNumber++)
            {
                try
                {
                    var result = await ChangeWallpaperImpl(progress);
                    if (result.Success)
                    {
                        return result;
                    }
                }
                catch(Exception ex)
                {
                    Logging.LogMessageToFile("Unexpected exception: " + ex.Message, 2);
                }

                await Task.Delay(TimeSpan.FromSeconds(1.5), token);
            }

            isRunning = 0;
            return ChangeWallpaperResult.Failed();
        }

        private async Task<ChangeWallpaperResult> ChangeWallpaperImpl(IProgress<string> progress)
        {
            int wallpaperGrabType = Properties.Settings.Default.wallpaperGrabType;
            string searchUrl = SearchUrlProvider.GetSearchUrl(wallpaperGrabType, progress);

            var imageInfo = await ImageInfoProvider.GetImageInfoAsync(searchUrl, wallpaperGrabType, progress);
            if (imageInfo == null || string.IsNullOrEmpty(imageInfo.Url))
            {
                return ChangeWallpaperResult.Failed();
            }

            if (Database.IsBlackListed(imageInfo.Url))
            {
                progress.Report("Wallpaper is blacklisted.");
                Logging.LogMessageToFile("The selected wallpaper has been blacklisted, searching again.", 0);
                return ChangeWallpaperResult.Failed();
            }

            if (Properties.Settings.Default.suppressDuplicates == true
                && HistoryList.Contains(imageInfo.ThreadId))
            {
                progress.Report("Wallpaper already used in this session.");
                Logging.LogMessageToFile("The selected wallpaper has already been used in this session, searching again.", 0);
                return ChangeWallpaperResult.Failed();
            }

            return await SetWallpaperAsync(imageInfo, progress);
        }

        public async Task<ChangeWallpaperResult> SetWallpaperAsync(ImageInfo imageInfo, IProgress<string> progress)
        {
            var wallpaperFilePath = await ImageLoader.TryLoadImageAsync(imageInfo, progress);
            if (string.IsNullOrEmpty(wallpaperFilePath))
            {
                return ChangeWallpaperResult.Failed();
            }

            await WallpaperSetter.SetWallpaperAsync(wallpaperFilePath, progress);

            HistoryList.Add(imageInfo.ThreadId);
            Database.AddWallpaperToHistory(imageInfo.Url, imageInfo.Title, imageInfo.ThreadId);

            ThumbnailCacheBuilder.BuildThumbnailCache();

            if (Properties.Settings.Default.autoSave == true)
            {
                Savewallpaper.SaveWallpaper(Properties.Settings.Default.currentWallpaperName);
            }

            CurrentWallpaperHolder.SetCurrentWallpaper(imageInfo, wallpaperFilePath);

            return new ChangeWallpaperResult(true, imageInfo.Title, imageInfo.ThreadId);
        }
    }
}