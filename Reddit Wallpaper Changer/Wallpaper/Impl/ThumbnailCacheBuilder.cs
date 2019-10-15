using Reddit_Wallpaper_Changer.Log;
using System;
using System.Drawing;
using System.IO;

namespace Reddit_Wallpaper_Changer.Wallpaper.Impl
{
    public class ThumbnailCacheBuilder : IThumbnailCacheBuilder
    {
        private Database DB { get; }

        public ThumbnailCacheBuilder(Database database)
        {
            DB = database;
        }

        //======================================================================
        // Generate thumbnails for History
        //======================================================================
        public void BuildThumbnailCache()
        {
            Logging.LogMessageToFile("Updating wallpaper thumbnail cache.", 0);
            try
            {
                foreach (var item in DB.getFromHistory())
                {
                    SaveThumbnail(item);
                }

                foreach (var item in DB.getFromFavourites())
                {
                    SaveThumbnail(item);
                }

                foreach (var item in DB.getFromBlacklist())
                {
                    SaveThumbnail(item);
                }
                Logging.LogMessageToFile("Wallpaper thumbnail cache updated.", 0);
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Error updating Wallpaper thumbnail cache: " + ex.Message, 1);
            }
        }

        //======================================================================
        // Remove all thumbnails
        //======================================================================
        public void RemoveThumbnailCache()
        {
            try
            {
                Logging.LogMessageToFile("Removing thumbnail cache", 0);
                var dir = new DirectoryInfo(Properties.Settings.Default.thumbnailCache);
                foreach (var file in dir.EnumerateFiles("*.jpg"))
                {
                    file.Delete();
                }
                Logging.LogMessageToFile("Thumbnail cache erased.", 0);
                Properties.Settings.Default.rebuildThumbCache = false;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Error rebuilding thumbnail cache: " + ex.Message, 1);
            }
        }

        private void SaveThumbnail(Model.HistoryItem item)
        {
            byte[] bytes = Convert.FromBase64String(item.imgstring);

            using (MemoryStream ms = new MemoryStream(bytes))
            {
                if (!File.Exists(Properties.Settings.Default.thumbnailCache + @"\" + item.threadidstring + ".jpg"))
                {
                    using (Image image = Image.FromStream(ms))
                    {
                        image.Save(Properties.Settings.Default.thumbnailCache + @"\" + item.threadidstring + ".jpg");
                    }
                }
            }
        }
    }
}
