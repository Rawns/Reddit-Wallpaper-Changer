using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Xml;
using System.Net;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Reddit_Wallpaper_Changer
{
    class Database
    {
        SQLiteConnection m_dbConnection;
        string dbPath = Properties.Settings.Default.AppDataPath + @"\Reddit-Wallpaper-Changer.sqlite";

        public string imgstring { get; set; }
        public string titlestring { get; set; }
        public string threadidstring { get; set; }
        public string urlstring { get; set; }
        public string datestring { get; set; }


        //======================================================================
        // Create the SQLite Blacklist database
        //======================================================================
        public void connectToDatabase()
        {
            if (!File.Exists(dbPath))
            {
                try
                {
                    SQLiteConnection.CreateFile(dbPath);
                    Logging.LogMessageToFile("Database 'Reddit-Wallpaper-Changer.sqlite' created successfully: " + dbPath);

                    m_dbConnection = new SQLiteConnection("Data Source=" + dbPath + ";Version=3;");
                    m_dbConnection.Open();
                    Logging.LogMessageToFile("Successfully connected to database 'Reddit-Wallpaper-Changer.sqlite'");

                    string sql = "CREATE TABLE blacklist (thumbnail STRING, title STRING, threadid STRING, url STRING, date STRING)";
                    SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();
                    Logging.LogMessageToFile("Table 'Blacklist' successfully created in 'Reddit-Wallpaper-Changer.sqlite'");

                    sql = "CREATE TABLE favourites (thumbnail STRING, title STRING, threadid STRING, url STRING, date STRING)";
                    command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();
                    Logging.LogMessageToFile("Table 'Favourites' successfully created in 'Reddit-Wallpaper-Changer.sqlite'");

                    sql = "CREATE TABLE history (thumbnail STRING, title STRING, threadid STRING, url STRING, date STRING)";
                    command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();
                    Logging.LogMessageToFile("Table 'History' successfully created in 'Reddit-Wallpaper-Changer.sqlite'");

                }
                catch(Exception ex)
                {
                    Logging.LogMessageToFile("Unexpected error creating database: " + ex.Message);
                }
            }
            else
            {
                try
                {
                    m_dbConnection = new SQLiteConnection("Data Source=" + dbPath + ";Version=3;");
                    m_dbConnection.Open();
                    Logging.LogMessageToFile("Successfully connected to database 'Reddit-Wallpaper-Changer.sqlite'");
                }
                catch(Exception ex)
                {
                    Logging.LogMessageToFile("Unexpected error connecting to database: " + ex.Message);
                }
            }
        }

        //======================================================================
        // One off task to migrate old Blacklist.xml file into Reddit-Wallpaper-Changer.sqlite
        //======================================================================
        public async void migrateOldBlacklist()
        {
            if (File.Exists(Properties.Settings.Default.AppDataPath + @"\Blacklist.xml"))
            {
                Logging.LogMessageToFile("Migrating Blacklist.xml to 'Reddit-Wallpaper-Changer.sqlite'. This is a one off task...");
                XmlDocument doc = new XmlDocument();
                XmlNodeList list;

                doc.Load(Properties.Settings.Default.AppDataPath + @"\Blacklist.xml");
                list = doc.SelectNodes("Blacklisted/Wallpaper");

                WebClient wc = Proxy.setProxy();
                foreach (XmlNode xn in list)
                {
                    try
                    {
                        string URL = xn["URL"].InnerText;
                        string Title = xn["Title"].InnerText;
                        string ThreadID = xn["ThreadID"].InnerText;
                        Logging.LogMessageToFile("Migrating: " + Title + ", " + ThreadID + ", " + URL);

                        Uri uri = new Uri(URL);

                        byte[] bytes = wc.DownloadData(URL);
                        using (MemoryStream ms = new MemoryStream(bytes))
                        {
                            var bmp = new Bitmap(ms);
                            Bitmap newImage = new Bitmap(150, 70);

                            Graphics gr = Graphics.FromImage(newImage);
                            gr.SmoothingMode = SmoothingMode.HighQuality;
                            gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            gr.DrawImage(bmp, new Rectangle(0, 0, 150, 70));
                            using (MemoryStream newms = new MemoryStream())
                            {
                                newImage.Save(newms, System.Drawing.Imaging.ImageFormat.Jpeg);

                                byte[] imageArray = newms.ToArray();
                                string base64ImageRepresentation = Convert.ToBase64String(imageArray);
                                string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.FFF");

                                string sql = "INSERT INTO blacklist (thumbnail, title, threadid, url, date) values ('" + base64ImageRepresentation + "', '" + Title + "', '" + ThreadID + "', '" + URL + "', '" + dateTime + "')";
                                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                                command.ExecuteNonQuery();
                            }

                            Logging.LogMessageToFile("Successfully migrated: " + Title + ", " + ThreadID + ", " + URL);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.LogMessageToFile("Unexpected error migrating: " + ex.Message);
                    }
                }

                Logging.LogMessageToFile("Migration to database completed successfully!");
                File.Delete(Properties.Settings.Default.AppDataPath + @"\Blacklist.xml");
                Logging.LogMessageToFile("Blacklist.xml deleted.");
            }
        }

        //======================================================================
        // Add wallpaper to blacklisted
        //======================================================================
        public async void blacklistWallpaper(string url, string title, string threadid)
        {
            try
            {
                string thumbnail = getThumbnail(url);
                string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                using (SQLiteCommand command = new SQLiteCommand("INSERT INTO blacklisted (thumbnail, title, threadid, url, date) values (@thumbnail, @title, @threadid, @url, @dateTime)", m_dbConnection))
                {
                    command.Parameters.AddWithValue("thumbnail", thumbnail);
                    command.Parameters.AddWithValue("title", title);
                    command.Parameters.AddWithValue("threadid", threadid);
                    command.Parameters.AddWithValue("url", url);
                    command.Parameters.AddWithValue("dateTime", dateTime);
                    command.ExecuteNonQuery();
                }
                Logging.LogMessageToFile("Wallpaper blacklisted! Title: " + title + ", Thread ID: " + threadid + ", URL: " + url);
            }
            catch(Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error blacklisting wallpaper: " + ex.Message);
            }
        }

        //======================================================================
        // Add wallpaper to favourites
        //======================================================================
        public async void faveWallpaper(string url, string title, string threadid)
        {
            try
            {
                string thumbnail = getThumbnail(url);
                string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                using (SQLiteCommand command = new SQLiteCommand("INSERT INTO favourites (thumbnail, title, threadid, url, date) values (@thumbnail, @title, @threadid, @url, @dateTime)", m_dbConnection))
                {
                    command.Parameters.AddWithValue("thumbnail", thumbnail);
                    command.Parameters.AddWithValue("title", title);
                    command.Parameters.AddWithValue("threadid", threadid);
                    command.Parameters.AddWithValue("url", url);
                    command.Parameters.AddWithValue("dateTime", dateTime);
                    command.ExecuteNonQuery();
                }
                Logging.LogMessageToFile("Wallpaper added to favourites! Title: " + title + ", Thread ID: " + threadid + ", URL: " + url);
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error favouriting wallpaper: " + ex.Message);
            }
        }

        //======================================================================
        // Add wallpaper to history
        //======================================================================
        public bool historyWallpaper(string url, string title, string threadid)
        {
            try
            {
                string thumbnail = getThumbnail(url);
                string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                using (SQLiteCommand command = new SQLiteCommand("INSERT INTO history (thumbnail, title, threadid, url, date) values (@thumbnail, @title, @threadid, @url, @dateTime)", m_dbConnection))
                { 
                    command.Parameters.AddWithValue("thumbnail", thumbnail);
                    command.Parameters.AddWithValue("title", title);
                    command.Parameters.AddWithValue("threadid", threadid);
                    command.Parameters.AddWithValue("url", url);
                    command.Parameters.AddWithValue("dateTime", dateTime);
                    command.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error adding wallpaper to history: " + ex.Message);
                return false;
            }
        }

        //======================================================================
        // Remove wallpaper from blacklist
        //======================================================================
        public void removeFromBlacklist(string url)
        {
            try
            {
                string sql = "DELETE FROM blacklist WHERE url = \"" + url + "\"";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();
                Logging.LogMessageToFile("Wallpaper removed from blacklist! URL: " + url);
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error removing wallpaper from Blacklist: " + ex.Message);
            }
        }

        //======================================================================
        // Remove wallpaper from favourites
        //======================================================================
        public void removeFromFavourites(string url)
        {
            try
            {
                string sql = "DELETE FROM favourites WHERE url = \"" + url + "\"";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();
                Logging.LogMessageToFile("Wallpaper removed from favourites! URL: " + url);
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error removing wallpaper from favourites: " + ex.Message);
            }
        }

        //======================================================================
        // Retrieve history from the database
        //======================================================================
        public List<Database> getFromHistory()
        {
            try
            {
                List<Database> items = new List<Database>();
                string sql = "SELECT * FROM history ORDER BY datetime(date) DESC";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new Database();
                        item.imgstring = (string)reader["thumbnail"];
                        item.titlestring = (string)reader["title"];
                        item.threadidstring = (string)reader["threadid"];
                        item.urlstring = (string)reader["url"];
                        item.datestring = (string)reader["date"];

                        items.Add(item);
                    }
                }

                return items;
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error retrieving History from database: " + ex.Message);
                return null;
            }
        }

        //======================================================================
        // Retrieve blacklist from the database
        //======================================================================
        public List<Database> getFromBlacklist()
        {
            try
            {
                List<Database> items = new List<Database>();
                string sql = "SELECT * FROM blacklist ORDER BY datetime(date) DESC";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new Database();
                        item.imgstring = (string)reader["thumbnail"];
                        item.titlestring = (string)reader["title"];
                        item.threadidstring = (string)reader["threadid"];
                        item.urlstring = (string)reader["url"];
                        item.datestring = (string)reader["date"];

                        items.Add(item);
                    }
                }

                return items;
            }
            catch(Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error retrieving Blacklist from database: " + ex.Message);
                return null;
            }
        }

        //======================================================================
        // Retrieve favourites from the database
        //======================================================================
        public List<Database> getFromFavourites()
        {
            try
            {
                List<Database> items = new List<Database>();
                string sql = "SELECT * FROM favourites ORDER BY datetime(date) DESC";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new Database();
                        item.imgstring = (string)reader["thumbnail"];
                        item.titlestring = (string)reader["title"];
                        item.threadidstring = (string)reader["threadid"];
                        item.urlstring = (string)reader["url"];
                        item.datestring = (string)reader["date"];

                        items.Add(item);
                    }
                }

                return items;
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error retrieving favourites from database: " + ex.Message);
                return null;
            }
        }

        //======================================================================
        // Check for blacklisted wallpaper
        //======================================================================
        public bool checkForEntry(string url)
        {
            try
            {
                string tmp = "";
                string sql = "SELECT url FROM blacklist WHERE url = \"" + url + "\"";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    tmp = reader["url"].ToString();
                }
                if (tmp == "")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch(Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error checking for Blacklist entry: " + ex.Message);
                return false;
            }
        }

        //======================================================================
        // generate a thumbnail of the wallpaper
        //======================================================================
        public string getThumbnail(string URL)
        {
            try
            {
                Uri uri = new Uri(URL);
                using (WebClient wc = Proxy.setProxy())
                {
                    byte[] bytes = wc.DownloadData(URL);
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        var bmp = new Bitmap(ms);
                        Bitmap newImage = new Bitmap(150, 70);

                        Graphics gr = Graphics.FromImage(newImage);
                        gr.SmoothingMode = SmoothingMode.HighQuality;
                        gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        gr.DrawImage(bmp, new Rectangle(0, 0, 150, 70));
                        using (MemoryStream newms = new MemoryStream())
                        {
                            newImage.Save(newms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        }

                        byte[] imageArray = ms.ToArray();
                        string base64ImageRepresentation = Convert.ToBase64String(imageArray);

                        return base64ImageRepresentation;
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error generating wallpaper thumbnail: " + ex.Message);
                return "";
            }
        }

        //======================================================================
        // Close database conenction
        //======================================================================
        public void disconnectFromDatabase()
        {
            m_dbConnection.Close();
        }

    }
}
