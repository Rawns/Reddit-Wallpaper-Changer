using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;

namespace Reddit_Wallpaper_Changer
{
    class Settings
    {
        //TODO Add buttons onto the UI for importing/exporting...

        //======================================================================
        // Export user settings
        //======================================================================
        public void Export()
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
                        writer.WriteElementString("Subreddits", Properties.Settings.Default.subredditsUsed);
                        writer.WriteElementString("Auto-Start", Properties.Settings.Default.autoStart.ToString());
                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                    }
                    Logging.LogMessageToFile("Settings have been successfully exported to: " + );
                    MessageBox.Show("Settings file exported successfully!", "Settings Exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error exporting settings: " + ex.Message, "Error Exporting!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //======================================================================
        // Import user settings
        //======================================================================
        public void Import()
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

                        //if (xmlVersion != version)
                        //{
                        //    MessageBox.Show("Unable to import settings file as it was exported from a different version of FMS Select.\r\n\r\n" +
                        //        "File Version: " + xmlVersion + "\r\n" +
                        //        "FMS Select Version: " + version + "\r\n\r\n" +
                        //        "Please configure FMS Select manually, or re-export the settings form an application running on version " + version + ".", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        //    return;
                        //}

                        // chkSch3.Checked = Boolean.Parse(xn["School3Enabled"].InnerText);
                        // txtFed.Text = xn["Federation"].InnerText;


                        MessageBox.Show("Settings file imported successfully. Check that the details are correct and then click 'Save'.", "Settings Imported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Unexpected error importing settings: " + ex.Message, "Error Importing!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }
            }


        }

    }
    }
}
