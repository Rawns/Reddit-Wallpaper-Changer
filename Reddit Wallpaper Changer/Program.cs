using Reddit_Wallpaper_Changer.Settings;
using Reddit_Wallpaper_Changer.Wallpaper;
using Reddit_Wallpaper_Changer.Wallpaper.Impl;
using SimpleInjector;
using System;
using System.Threading;
using System.Windows.Forms;

namespace Reddit_Wallpaper_Changer
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        ///

        private static Mutex mutex = new Mutex(false, "RedditWallpaperChanger_byUgleh");
        private static Container container;

        private static void RegisterContainer()
        {
            // 1. Create a new Simple Injector container
            container = new Container();

            // 2. Configure the container (register)
            container.Register<ISearchSettings, SettingsProvider>(Lifestyle.Singleton);
            container.Register<ILoadSettings, SettingsProvider>(Lifestyle.Singleton);

            container.Register<RWC>(Lifestyle.Singleton);
            container.Register<IWallpaperChanger, WallpaperChanger>(Lifestyle.Singleton);
            container.Register<Database>(Lifestyle.Singleton);
            container.Register<IWallpaperSaver, WallpaperSaver>(Lifestyle.Singleton);
            container.Register<IThumbnailCacheBuilder, ThumbnailCacheBuilder>(Lifestyle.Singleton);
            container.Register<ISearchUrlProvider, SearchUrlProvider>(Lifestyle.Singleton);
            container.Register<IImageInfoProvider, ImageInfoProvider>(Lifestyle.Singleton);
            container.Register<IImageLoader, ImageLoader>(Lifestyle.Singleton);
            container.Register<IWallpaperSetter, WallpaperSetter>(Lifestyle.Singleton);
            container.Register<IImageUrlValidator, ImageUrlValidator>(Lifestyle.Singleton);
            container.Register<ICurrentWallpaperHolder, CurrentWallpaperHolder>(Lifestyle.Singleton);

            // 3. Verify your configuration
            container.Verify();
        }

        [STAThread]
        private static void Main()
        {
            if (!mutex.WaitOne(TimeSpan.FromSeconds(2), false))
            {
                DialogResult dialogResult = MessageBox.Show("Run another instance of RWC?", "Reddit Wallpaper Changer", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    RegisterContainer();
                    Application.Run(container.GetInstance<RWC>());
                }
                else if (dialogResult == DialogResult.No)
                {
                    //do something else
                }
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                RegisterContainer();
                Application.Run(container.GetInstance<RWC>());
            }
            finally
            {
                // I find this more explicit
                mutex.ReleaseMutex();
            }
        }
    }
}