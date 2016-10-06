using System;
using System.IO;

namespace Reddit_Wallpaper_Changer
{
    class Logging
    {
        //======================================================================
        // Write to the logfile
        //======================================================================
        public static void LogMessageToFile(string msg)
        {
            if (Properties.Settings.Default.logging == true)
            {
                StreamWriter sw = null;
                string hostName = System.Environment.MachineName;
                string logfiledir = AppDomain.CurrentDomain.BaseDirectory + @"\Log";
                System.IO.Directory.CreateDirectory(logfiledir);

                if (File.Exists(logfiledir + @"\RWC.log"))
                {
                    long length = new System.IO.FileInfo(logfiledir + @"\RWC.log").Length;
                    long max = 1048576;

                    if (length >= max)
                    {
                        try
                        {
                            File.Delete(logfiledir + @"\RWC.log");
                        }
                        catch
                        {
                        }
                    }
                }

                try
                {
                    sw = new StreamWriter(logfiledir + @"\RWC.log", true);
                    sw.WriteLine(DateTime.Now.ToString() + " - " + hostName + ": " + msg);
                    sw.Flush();
                    sw.Close();
                }
                catch
                {
                }
            }
        }

    }
}
