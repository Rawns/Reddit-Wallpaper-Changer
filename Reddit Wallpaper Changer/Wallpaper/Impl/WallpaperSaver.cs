using Reddit_Wallpaper_Changer.Log;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace Reddit_Wallpaper_Changer.Wallpaper.Impl
{
    public class WallpaperSaver : IWallpaperSaver
    {

        //======================================================================
        // Save current wallpaper
        //======================================================================
        public bool SaveWallpaper(string fileName)
        {
            try
            {
                string validatedFileName = ValidateFileName(fileName);
                if (!File.Exists(Properties.Settings.Default.defaultSaveLocation + @"\" + validatedFileName))
                {
                    File.Copy(Properties.Settings.Default.currentWallpaperFile, Properties.Settings.Default.defaultSaveLocation + @"\" + validatedFileName);
                    return true;
                }
                else
                {
                    Logging.LogMessageToFile("Not auto saving " + fileName + " because it already exists.", 1);
                    return false;
                }
            }
            catch (Exception Ex)
            {
                Logging.LogMessageToFile("Error Saving Wallpaper: " + Ex.Message, 2);
                return false;
            }
        }

        public bool IsAlreadySavedWallpaper(string fileName)
        {
            string validatedFileName = ValidateFileName(fileName);
            return File.Exists(Properties.Settings.Default.defaultSaveLocation + @"\" + validatedFileName);
        }

        //======================================================================
        // Override auto save faves if auto save all is enabled
        //======================================================================
        public bool SaveSelectedWallpaper(string url, string threadid, string title)
        {
            try
            {
                string ext = Path.GetExtension(url);
                string fileName = title;

                bool changed = false;
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    if (fileName.Contains(c))
                        changed = true;
                    fileName = fileName.Replace(c.ToString(), "_");
                }

                if (changed)
                    Logging.LogMessageToFile("Removed illegal characters from post title: " + fileName + ext, 0);

                if (!File.Exists(Properties.Settings.Default.defaultSaveLocation + @"\" + fileName + ext))
                {
                    using (WebClient webClient = Proxy.setProxy())
                    {
                        webClient.DownloadFile(url, Properties.Settings.Default.defaultSaveLocation + @"\" + fileName + ext);
                    }
                    Logging.LogMessageToFile("Saved " + fileName + ext + " to " + Properties.Settings.Default.defaultSaveLocation, 0);
                    return true;
                }
                else
                {
                    Logging.LogMessageToFile("Not auto saving " + fileName + ext + " because it already exists.", 1);
                    return true;
                }
            }
            catch (Exception Ex)
            {
                Logging.LogMessageToFile("Error Saving Wallpaper: " + Ex.Message, 2);
            }

            return true;
        }

        private static string ValidateFileName(string fileName)
        {
            bool changed = false;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (fileName.Contains(c))
                    changed = true;
                fileName = fileName.Replace(c.ToString(), "_");
            }

            if (changed)
                Logging.LogMessageToFile("Removed illegal characters from post title: " + fileName, 0);

            return fileName;
        }
    }
}
