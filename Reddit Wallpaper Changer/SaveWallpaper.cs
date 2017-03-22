using System;
using System.IO;
using System.Linq;


namespace Reddit_Wallpaper_Changer
{
    class SaveWallpaper
    {
        //======================================================================
        // Save current wallpaper if flagged as favourite
        //======================================================================
        private bool autoSaveCurrentFave()
        {
            return true;
        }

        //======================================================================
        // Save historical wallpaper if flagged as favourite
        //======================================================================
        private bool autoSaveHistoricalFave()
        {
            return true;
        }

        //======================================================================
        // Override auto save faves if auto save all is enabled
        //======================================================================
        private bool saveFavourite(string url)
        {
            try
            {
                String fileName = Properties.Settings.Default.currentWallpaperName;

                // Remove illegal characters from the post title
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
                    if (Properties.Settings.Default.disableNotifications == false)
                    {
                        //taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                        //taskIcon.BalloonTipTitle = "Wallpaper Saved!";
                        //taskIcon.BalloonTipText = "Wallpaper saved to " + Properties.Settings.Default.defaultSaveLocation + @"\" + fileName;
                        //taskIcon.ShowBalloonTip(750);
                    }
                    // updateStatus("Wallpaper saved!");
                    Logging.LogMessageToFile("Saved " + fileName + " to " + Properties.Settings.Default.defaultSaveLocation);
                    return true;
                }
                else
                {
                    if (Properties.Settings.Default.disableNotifications == false)
                    {
                        //taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                        //taskIcon.BalloonTipTitle = "Already Saved!";
                        //taskIcon.BalloonTipText = "No need to save this wallpaper as it already exists in your wallpapers folder! :)";
                        //taskIcon.ShowBalloonTip(750);
                    }
                    //updateStatus("Wallpaper already saved!");
                    Logging.LogMessageToFile("Not auto saving " + fileName + " because it already exists.");
                    return true;
                }
            }
            catch (Exception Ex)
            {
                if (Properties.Settings.Default.disableNotifications == false)
                {
                    //taskIcon.BalloonTipIcon = ToolTipIcon.Error;
                    //taskIcon.BalloonTipTitle = "Error Saving!";
                    //taskIcon.BalloonTipText = "Unable to save the wallpaper locally. :(";
                    //taskIcon.ShowBalloonTip(750);
                }
                // updateStatus("Error saving wallpaper!");
                Logging.LogMessageToFile("Error Saving Wallpaper: " + Ex.Message);
            }

            return true;
        }
    }
}
