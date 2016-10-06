using System;
using System.IO;
using System.Xml;

namespace Reddit_Wallpaper_Changer
{
    class Xml
    {
        //======================================================================
        // Create XML files to store favourite and blacklisted wallpapers
        //======================================================================
        public static void createXML()
        {
            string fave = AppDomain.CurrentDomain.BaseDirectory + "Favourites.xml";
            string black = AppDomain.CurrentDomain.BaseDirectory + "Blacklist.xml";

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            if (!File.Exists(fave))
            {
                XmlWriter writer = XmlWriter.Create(fave, settings);
                writer.WriteStartDocument();
                writer.WriteComment("This file stores a list of any wallpapers you flag as a favourite.");
                writer.WriteStartElement("Favourites");
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();
                writer.Close();
                Logging.LogMessageToFile("New Favourites.xml file created successfully.");
            }

            if (!File.Exists(black))
            {
                XmlWriter writer = XmlWriter.Create(black, settings);
                writer.WriteStartDocument();
                writer.WriteComment("This file stores a list of any wallpapers you blacklist.");
                writer.WriteStartElement("Blacklisted");
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();
                writer.Close();
                Logging.LogMessageToFile("New Blacklisted.xml file created successfully.");
            }

        }

        //======================================================================
        // Delete the dummy wallpaper example if it exists
        //======================================================================
        public static void deleteDummy()
        {
            
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Favourites.xml"))
            {
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + "Favourites.xml");
            }

            string blacklisturl = "http://example.url/blacklisted_wallpaper.jpg";
            string blpath = AppDomain.CurrentDomain.BaseDirectory + "Blacklist.xml";

            try
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(blpath);
                foreach (XmlNode node in xml.SelectNodes("Blacklisted/Wallpaper"))
                {
                    if (node.SelectSingleNode("URL").InnerText == blacklisturl)
                    {
                        node.ParentNode.RemoveChild(node);
                    }
                }

                xml.Save(blpath);
            }
            catch
            {

            }
        }
    }
}
