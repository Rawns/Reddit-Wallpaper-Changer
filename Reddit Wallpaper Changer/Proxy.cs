using System.Net;

namespace Reddit_Wallpaper_Changer
{
    class Proxy
    {
        //======================================================================
        // Set up proxy along with any credntials required for auth
        //======================================================================
        public static WebClient setProxy()
        {
            WebClient wc = new WebClient();
            wc.Proxy = null;

            if (Properties.Settings.Default.useProxy == true)
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

            return wc;
        }
    }
}
