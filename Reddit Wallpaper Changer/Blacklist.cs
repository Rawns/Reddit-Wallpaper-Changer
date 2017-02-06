using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Reddit_Wallpaper_Changer
{
    class Blacklist
    {
        private String blacklistPath;

        public Blacklist(string filepath)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            if (!File.Exists(filepath))
            {
                XmlWriter writer = XmlWriter.Create(filepath, settings);
                writer.WriteStartDocument();
                writer.WriteComment("This file stores a list of any wallpapers you blacklist");
                writer.WriteStartElement("Blacklisted");
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();
                writer.Close();
                Logging.LogMessageToFile("New Blacklist.xml file created successfully.");
            }

            blacklistPath = filepath;
        }

        //======================================================================
        // Add an entry to the Blacklist
        //======================================================================
        public void addEntry(String url, String title, String threadID)
        {
            XDocument doc = XDocument.Load(blacklistPath);
            XElement blacklist = doc.Element("Blacklisted");
            blacklist.Add(new XElement("Wallpaper",
                new XElement("URL", url),
                new XElement("Title", title),
                new XElement("ThreadID", threadID)));
            doc.Save(blacklistPath);
        }

        //======================================================================
        // Remove an entry from the Blacklist
        //======================================================================
        public void removeEntry(String url)
        {
            try
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(blacklistPath);

                foreach (XmlNode node in xml.SelectNodes("Blacklisted/Wallpaper"))
                {
                    if (node.SelectSingleNode("URL").InnerText == url)
                    {
                        node.ParentNode.RemoveChild(node);
                    }

                    xml.Save(blacklistPath);
                    Logging.LogMessageToFile("Wallpaper removed from the blacklist. URL: " + url);
                }
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected Error: " + ex.Message);
            }
            
        }

        //======================================================================
        // Return content of the specified XML as a list
        //======================================================================
        public XmlNodeList getXMLContent(String XML)
        {
            XmlDocument doc = new XmlDocument();
            XmlNodeList list;

            doc.Load(blacklistPath);
            list = doc.SelectNodes("Blacklisted/Wallpaper");
            return list;
        }

        //======================================================================
        // Check if a URL is contained in the XML
        //======================================================================

        public bool containsURL(String url)
        {
            XDocument xml = XDocument.Load(blacklistPath);

            var list = xml.Descendants("URL").Select(x => x.Value).ToList();

            if (list.Contains(url))
            {
                return true;
            } 
            else
            {
                return false;
            }
            
        }

    }
}
