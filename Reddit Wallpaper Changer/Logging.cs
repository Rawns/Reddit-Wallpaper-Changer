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
