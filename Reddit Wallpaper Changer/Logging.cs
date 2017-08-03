using System;
using System.IO;

namespace Reddit_Wallpaper_Changer
{
    class Logging
    {
        //======================================================================
        // Write to the logfile
        //======================================================================
        public static void LogMessageToFile(string msg, int code)
        {
            StreamWriter sw = null;
            string level = "";
            if (code == 0) { level = "INFORMATION:"; }
            if (code == 1) { level = "WARNING:"; }
            if (code == 2) { level = "ERROR:"; }
            if (code == 3) { level = "DICKBUTT:"; }

            string hostName = System.Environment.MachineName;
            string logfiledir = Properties.Settings.Default.AppDataPath + @"\Logs";
            System.IO.Directory.CreateDirectory(logfiledir);

            //======================================================================
            // Legacy: Remove old logs
            //======================================================================    
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Log\RWC.log"))
            {
                System.IO.Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "Log", true);
            }
                        

            if (File.Exists(logfiledir + @"\RWC.log"))
            {
                long length = new System.IO.FileInfo(logfiledir + @"\RWC.log").Length;
                long max = 512000;

                if (length >= max)
                {
                    try
                    {
                        if (File.Exists(logfiledir + @"\RWC1.log"))
                        {
                            if (File.Exists(logfiledir + @"\RWC2.log"))
                            {
                                if (File.Exists(logfiledir + @"\RWC3.log"))
                                {
                                    File.Delete(logfiledir + @"\RWC3.log");
                                    System.IO.File.Move(logfiledir + @"\RWC2.log", logfiledir + @"\RWC3.log");
                                    System.IO.File.Move(logfiledir + @"\RWC1.log", logfiledir + @"\RWC2.log");
                                    System.IO.File.Move(logfiledir + @"\RWC.log", logfiledir + @"\RWC1.log");
                                }
                                else
                                {
                                    System.IO.File.Move(logfiledir + @"\RWC2.log", logfiledir + @"\RWC3.log");
                                    System.IO.File.Move(logfiledir + @"\RWC1.log", logfiledir + @"\RWC2.log");
                                    System.IO.File.Move(logfiledir + @"\RWC.log", logfiledir + @"\RWC1.log");
                                }

                            }
                            else
                            {
                                System.IO.File.Move(logfiledir + @"\RWC1.log", logfiledir + @"\RWC2.log");
                                System.IO.File.Move(logfiledir + @"\RWC.log", logfiledir + @"\RWC1.log");
                            }
                        }
                        else
                        {
                            System.IO.File.Move(logfiledir + @"\RWC.log", logfiledir + @"\RWC1.log");
                        }
                    }
                    catch
                    {
                    }
                }
            }

            try
            {
                sw = new StreamWriter(logfiledir + @"\RWC.log", true);
                sw.WriteLine(DateTime.Now.ToString() + " - " + hostName + " - " + level + " " + msg);
                sw.Flush();
                sw.Close();
            }
            catch
            {
            }

        }

    }
}
