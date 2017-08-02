using System;
using System.Net;

namespace Reddit_Wallpaper_Changer
{
    /// <summary>
    /// Class for checking and setting a proxy server
    /// </summary>
    class Proxy
    {
        //======================================================================
        // Set up proxy along with any credntials required for auth
        //======================================================================
        public static WebClient setProxy()
        {
            using (WebClient wc = new WebClient())
            {
                wc.Proxy = null;

                if (Properties.Settings.Default.useProxy == true)
                {
                    try
                    {
                        WebProxy proxy = new WebProxy(Properties.Settings.Default.proxyAddress);

                        if (Properties.Settings.Default.proxyAuth == true)
                        {
                            proxy.Credentials = new NetworkCredential(Properties.Settings.Default.proxyUser, Properties.Settings.Default.proxyPass);
                            proxy.UseDefaultCredentials = false;
                            proxy.BypassProxyOnLocal = false;
                        }

                        wc.Proxy = proxy;
                    }
                    catch (Exception ex)
                    {
                        Logging.LogMessageToFile("Unexpeced error setting proxy: " + ex.Message, 2);
                    }
                }
                
                return wc;
            }
        }
    }
}
