using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using System.Text;
using System.Threading.Tasks;

namespace Reddit_Wallpaper_Changer
{
    class Pastebin
    {
        public static void UploadLog()
        {
            System.Collections.Specialized.NameValueCollection Data = new System.Collections.Specialized.NameValueCollection();
            Data["api_paste_name"] = "RWC_Log_" + DateTime.Now.ToString() + ".log";
            Data["api_paste_expire_Date"] = "N";
            Data["api_paste_code"] = File.ReadAllText(Properties.Settings.Default.AppDataPath + @"\Logs\RWC.log");
            Data["api_dev_key"] = "017c00e3a11ee8c70499c1f4b6b933f0";
            Data["api_option"] = "paste";

            WebClient wb = new WebClient();
            byte[] bytes = wb.UploadValues("http://pastebin.com/api/api_post.php", Data);

            string response;
            using (MemoryStream ms = new MemoryStream(bytes))
            using (StreamReader reader = new StreamReader(ms))
                response = reader.ReadToEnd();

            if (response.StartsWith("Bad API request"))
            {
                MessageBox.Show("Failed to upload log to Pastebin: " + response, "Failed to upload!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                Clipboard.SetText(response);
                Logging.LogMessageToFile("Logfile uploaded to Pastebin: " + response);
                MessageBox.Show("Your logfile has been uploaded to pastebin and and the URL copied to your clipboard!", "Uploaded!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }
    }
}
