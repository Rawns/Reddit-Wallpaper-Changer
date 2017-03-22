using System;
using System.IO;
using System.Linq;
using System.Net;

namespace Reddit_Wallpaper_Changer
{
    class SaveWallpaper
    {

        //======================================================================
        // Auto save wallpaper
        //======================================================================
        public bool autoSaveWallpaper()
        {
            return true;
        }

        //======================================================================
        // Save current wallpaper
        //======================================================================
        public bool saveCurrentWallpaper(string fileName)
        {
            try
            {
                bool changed = false;
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    if (fileName.Contains(c))
                        changed = true;
                    fileName = fileName.Replace(c.ToString(), "_");
                }

                if (changed)
                    Logging.LogMessageToFile("Removed illegal characters from post title: " + fileName);

                if (!File.Exists(Properties.Settings.Default.defaultSaveLocation + @"\" + fileName))
                {
                    System.IO.File.Copy(Properties.Settings.Default.currentWallpaperFile, Properties.Settings.Default.defaultSaveLocation + @"\" + fileName);
                    return true;
                }
                else
                {
                    Logging.LogMessageToFile("Not auto saving " + fileName + " because it already exists.");
                    return true;
                }
            }
            catch (Exception Ex)
            {
                Logging.LogMessageToFile("Error Saving Wallpaper: " + Ex.Message);
                return false;
            }
        }

        //======================================================================
        // Override auto save faves if auto save all is enabled
        //======================================================================
        public bool saveSelectedWallpaper(string url, string threadid, string title)
        {
            try
            {
                string ext = Path.GetExtension(url);
                String fileName = title;


                bool changed = false;
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    if (fileName.Contains(c))
                        changed = true;
                    fileName = fileName.Replace(c.ToString(), "_");
                }

                if (changed)
                    Logging.LogMessageToFile("Removed illegal characters from post title: " + fileName + ext);

                if (!File.Exists(Properties.Settings.Default.defaultSaveLocation + @"\" + fileName + ext))
                {
                    using (WebClient webClient = Proxy.setProxy())
                    {
                        webClient.DownloadFile(url, Properties.Settings.Default.defaultSaveLocation + @"\" + fileName + ext);
                    }
                    Logging.LogMessageToFile("Saved " + fileName + ext + " to " + Properties.Settings.Default.defaultSaveLocation);
                    return true;
                }
                else
                {
                    Logging.LogMessageToFile("Not auto saving " + fileName + ext + " because it already exists.");
                    return true;
                }
            }
            catch (Exception Ex)
            {
                Logging.LogMessageToFile("Error Saving Wallpaper: " + Ex.Message);
            }

            return true;
        }
    }
}
