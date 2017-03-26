using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Reddit_Wallpaper_Changer
{

    class Pastebin
    {
        public static async Task<bool> UploadLog()
        {
            Properties.Settings.Default.logUrl = "";
            Properties.Settings.Default.Save();

            System.Collections.Specialized.NameValueCollection Data = new System.Collections.Specialized.NameValueCollection();
            Data["api_paste_name"] = "RWC_Log_" + DateTime.Now.ToString() + ".log";
            Data["api_paste_expire_Date"] = "N";
            Data["api_paste_code"] = File.ReadAllText(Properties.Settings.Default.AppDataPath + @"\Logs\RWC.log");
            Data["api_dev_key"] = "017c00e3a11ee8c70499c1f4b6b933f0";
            Data["api_option"] = "paste";

            using (WebClient wc = Proxy.setProxy())
            {
                try
                {
                    byte[] bytes = wc.UploadValues("http://pastebin.com/api/api_post.php", Data);
                    string response;
                    using (MemoryStream ms = new MemoryStream(bytes))
                    using (StreamReader reader = new StreamReader(ms))
                        response = reader.ReadToEnd();

                    if (response.StartsWith("Bad API request"))
                    {
                        Logging.LogMessageToFile("Failed to upload log to Pastebin: " + response, 0);
                        return false;

                    }
                    else
                    {
                        Logging.LogMessageToFile("Logfile successfully uploaded to Pastebin: " + response, 0);
                        Properties.Settings.Default.logUrl = response;
                        Properties.Settings.Default.Save();
                        return true;

                    }
                }
                catch (Exception ex)
                {
                    Logging.LogMessageToFile("Error uploading logfile to Pastebin: " + ex.Message, 2);
                    return true;

                }
            }
        }
    }
}
