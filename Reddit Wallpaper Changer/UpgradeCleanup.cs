using System;
using System.IO;


namespace Reddit_Wallpaper_Changer
{
    class UpgradeCleanup
    {
        //======================================================================
        // Delete old executalbe after an update
        //======================================================================
        public static void deleteOldVersion()
        {
            try
            {
                File.Delete(System.Reflection.Assembly.GetExecutingAssembly().Location + ".old");
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Error removing old version: " + ex.Message);
            }
        }
    }
}
