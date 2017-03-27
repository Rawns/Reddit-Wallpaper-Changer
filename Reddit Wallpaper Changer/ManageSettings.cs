using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;

namespace Reddit_Wallpaper_Changer
{
    class ManageSettings
    {

        //======================================================================
        // Export user settings
        //======================================================================
        public static void Export()
        {
            try
            {
                SaveFileDialog saveFile = new SaveFileDialog();
                saveFile.Filter = "XML File (*.xml)|*.xml";
                saveFile.FileName = "RWC Settings.xml";

                string version = Assembly.GetEntryAssembly().GetName().Version.ToString();

                DialogResult result = saveFile.ShowDialog();
                if (result == DialogResult.OK)
                {
                    string savedFile = saveFile.FileName;

                    using (FileStream fileStream = new FileStream(savedFile, FileMode.Create))
                    using (StreamWriter sw = new StreamWriter(fileStream))
                    using (XmlTextWriter writer = new XmlTextWriter(sw))
                    {
                        writer.Formatting = Formatting.Indented;
                        writer.Indentation = 4;

                        writer.WriteStartDocument();
                        writer.WriteStartElement("RWC-Settings");
                        writer.WriteElementString("Version", version);
                        writer.WriteElementString("GrabType", Properties.Settings.Default.wallpaperGrabType.ToString());
                        writer.WriteElementString("Subreddits", Properties.Settings.Default.subredditsUsed);
                        writer.WriteElementString("SearchQuery", Properties.Settings.Default.searchQuery);
                        writer.WriteElementString("ChangeTimerValue", Properties.Settings.Default.changeTimeValue.ToString());
                        writer.WriteElementString("ChangeTimerType", Properties.Settings.Default.changeTimeType.ToString());
                        writer.WriteElementString("StartInTray", Properties.Settings.Default.startInTray.ToString());
                        writer.WriteElementString("AutoStart", Properties.Settings.Default.autoStart.ToString());
                        writer.WriteElementString("UseProxy", Properties.Settings.Default.useProxy.ToString());
                        writer.WriteElementString("ProxyServer", Properties.Settings.Default.proxyAddress);
                        writer.WriteElementString("ProxyAuthentication", Properties.Settings.Default.proxyAuth.ToString());
                        writer.WriteElementString("DefaultSaveLocation", Properties.Settings.Default.defaultSaveLocation);
                        writer.WriteElementString("AutoSave", Properties.Settings.Default.autoSave.ToString());
                        writer.WriteElementString("AutoSaveFaves", Properties.Settings.Default.autoSaveFaves.ToString());
                        writer.WriteElementString("WallpaperFade", Properties.Settings.Default.wallpaperFade.ToString());
                        writer.WriteElementString("DisableNotifications", Properties.Settings.Default.disableNotifications.ToString());
                        writer.WriteElementString("SuppressDuplicates", Properties.Settings.Default.suppressDuplicates.ToString());
                        writer.WriteElementString("ValidateWallpaperSize", Properties.Settings.Default.sizeValidation.ToString());
                        writer.WriteElementString("WallpaperInfoPopup", Properties.Settings.Default.wallpaperInfoPopup.ToString());
                        writer.WriteElementString("AutoUpdateCheck", Properties.Settings.Default.autoUpdateCheck.ToString());
                        writer.WriteElementString("WallpaperFit", Properties.Settings.Default.wallpaperStyle);
                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                    }
                    Logging.LogMessageToFile("Settings have been successfully exported to: " + savedFile, 0);
                    MessageBox.Show("Your settings have been exported successfully!", "Settings Exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {

                }

            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error exporting settings: " + ex.Message, 2);
                MessageBox.Show("Unexpected error exporting settings to XML: " + ex.Message, "Error Exporting!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //======================================================================
        // Import user settings
        //======================================================================
        public static void Import()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "XML File (*.xml)|*.xml";
            openFile.FileName = "RWC Settings.xml";

            DialogResult result = openFile.ShowDialog();
            if (result == DialogResult.OK)
            {
                string selectedFile = openFile.FileName;
                string version = Assembly.GetEntryAssembly().GetName().Version.ToString();

                XmlDocument doc = new XmlDocument();
                doc.Load(selectedFile);
                XmlNodeList xnList = doc.SelectNodes("/RWC-Settings");
                foreach (XmlNode xn in xnList)
                {
                    try
                    {
                        string xmlVersion = xn["Version"].InnerText;

                        // Version check? Can't decide if needed or not...
                        if (xmlVersion != version)
                        {
                        //    MessageBox.Show("Unable to import settings as they have been exported from a previous version.\r\n\r\n" +
                        //        "XML Version: " + xmlVersion + "\r\n" +
                        //        "RWC Version: " + version + "\r\n\r\n" +
                        //        "Please configure Reddit Wallpaper Changer manually, or re-export the settings form an application running on version " + version + ".", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        //    return;
                        }

                        Properties.Settings.Default.wallpaperGrabType = Int32.Parse(xn["GrabType"].InnerText);
                        Properties.Settings.Default.subredditsUsed = xn["Subreddits"].InnerText;
                        Properties.Settings.Default.searchQuery = xn["SearchQuery"].InnerText;
                        Properties.Settings.Default.changeTimeValue = Int32.Parse(xn["ChangeTimerValue"].InnerText);
                        Properties.Settings.Default.changeTimeType = Int32.Parse(xn["ChangeTimerType"].InnerText);
                        Properties.Settings.Default.startInTray = Boolean.Parse(xn["StartInTray"].InnerText);
                        Properties.Settings.Default.autoStart = Boolean.Parse(xn["AutoStart"].InnerText);
                        Properties.Settings.Default.useProxy = Boolean.Parse(xn["UseProxy"].InnerText);
                        Properties.Settings.Default.proxyAddress = xn["ProxyServer"].InnerText;
                        Properties.Settings.Default.proxyAuth = Boolean.Parse(xn["ProxyAuthentication"].InnerText);
                        Properties.Settings.Default.defaultSaveLocation = xn["DefaultSaveLocation"].InnerText;
                        Properties.Settings.Default.autoSave = Boolean.Parse(xn["AutoSave"].InnerText);
                        Properties.Settings.Default.autoSaveFaves = Boolean.Parse(xn["AutoSaveFaves"].InnerText);
                        Properties.Settings.Default.wallpaperFade = Boolean.Parse(xn["WallpaperFade"].InnerText);
                        Properties.Settings.Default.disableNotifications = Boolean.Parse(xn["DisableNotifications"].InnerText);
                        Properties.Settings.Default.suppressDuplicates = Boolean.Parse(xn["SuppressDuplicates"].InnerText);
                        Properties.Settings.Default.sizeValidation = Boolean.Parse(xn["ValidateWallpaperSize"].InnerText);
                        Properties.Settings.Default.wallpaperInfoPopup = Boolean.Parse(xn["WallpaperInfoPopup"].InnerText);
                        Properties.Settings.Default.autoUpdateCheck = Boolean.Parse(xn["AutoUpdateCheck"].InnerText);
                        Properties.Settings.Default.wallpaperStyle = xn["WallpaperFit"].InnerText;
                        Properties.Settings.Default.Save();

                        Logging.LogMessageToFile("Settings have been successfully imported. Restarting RWC.", 0);
                        MessageBox.Show("Settings file imported successfully.\r\n\r\n" +
                            "Note: If a proxy is specified that requires authentication, you must manually enter the credentials.\r\n\r\n" +
                            "RWC will now restart so the new settings take effect.", "Settings Imported", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
                        System.Environment.Exit(0);
                    }
                    catch (Exception ex)
                    {
                        Logging.LogMessageToFile("Unexpected error importing settings file: " + ex.Message, 2);
                        MessageBox.Show("Unexpected error importing settings from XML: " + ex.Message, "Error Importing!", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    }
                }
            }
        }
    }
}
