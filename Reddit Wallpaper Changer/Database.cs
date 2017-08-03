using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Xml;
using System.Net;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Reflection;

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
                    Logging.LogMessageToFile("Database 'Reddit-Wallpaper-Changer.sqlite' created successfully: " + dbPath, 0);

                    m_dbConnection = new SQLiteConnection("Data Source=" + dbPath + ";Version=3;");
                    m_dbConnection.Open();
                    Logging.LogMessageToFile("Successfully connected to database 'Reddit-Wallpaper-Changer.sqlite'", 0);

                    string sql = "CREATE TABLE version (version STRING, date STRING)";
                    SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();
                    Logging.LogMessageToFile("Table 'version' successfully created.", 0);

                    sql = "CREATE TABLE blacklist (thumbnail STRING, title STRING, threadid STRING, url STRING, date STRING)";
                    command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();
                    Logging.LogMessageToFile("Table 'blacklist' successfully created.", 0);

                    sql = "CREATE INDEX idx_blacklist ON blacklist (url)";
                    command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();
                    Logging.LogMessageToFile("Index 'idx_blacklist' successfully created.", 0);

                    sql = "CREATE TABLE favourites (thumbnail STRING, title STRING, threadid STRING, url STRING, date STRING)";
                    command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();
                    Logging.LogMessageToFile("Table 'favourites' successfully created.", 0);

                    sql = "CREATE INDEX idx_favourites ON favourites (url)";
                    command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();
                    Logging.LogMessageToFile("Index 'idx_favourites' successfully created.", 0);

                    sql = "CREATE TABLE history (thumbnail STRING, title STRING, threadid STRING, url STRING, date STRING)";
                    command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();
                    Logging.LogMessageToFile("Table 'history' successfully created.", 0);

                    sql = "CREATE INDEX idx_history ON history (url)";
                    command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();
                    Logging.LogMessageToFile("Index 'idx_history' successfully created.", 0);

                    addVersion();

                }
                catch(Exception ex)
                {
                    Logging.LogMessageToFile("Unexpected error creating database: " + ex.Message, 2);
                }
            }
            else
            {
                try
                {
                    m_dbConnection = new SQLiteConnection("Data Source=" + dbPath + ";Version=3;");
                    m_dbConnection.Open();
                    Logging.LogMessageToFile("Successfully connected to database 'Reddit-Wallpaper-Changer.sqlite'", 0);
                }
                catch(Exception ex)
                {
                    Logging.LogMessageToFile("Unexpected error connecting to database: " + ex.Message, 2);
                }
            }
        }

        //======================================================================
        // One off task to migrate old Blacklist.xml file into Reddit-Wallpaper-Changer.sqlite
        //======================================================================
        public void migrateOldBlacklist()
        {
            try
            {
                Logging.LogMessageToFile("Migrating Blacklist.xml to 'Reddit-Wallpaper-Changer.sqlite'. This is a one off task...", 0);

                if (File.Exists(Properties.Settings.Default.AppDataPath + @"\Blacklist.xml"))
                {
                    XmlDocument doc = new XmlDocument();
                    XmlNodeList list;

                    doc.Load(Properties.Settings.Default.AppDataPath + @"\Blacklist.xml");
                    list = doc.SelectNodes("Blacklisted/Wallpaper");

                    using (WebClient wc = Proxy.setProxy())
                    {
                        foreach (XmlNode xn in list)
                        {
                            try
                            {
                                string URL = xn["URL"].InnerText;
                                string Title = xn["Title"].InnerText;
                                Title = Title.Replace("'", "''");
                                string ThreadID = xn["ThreadID"].InnerText;
                                Logging.LogMessageToFile("Migrating: " + Title + ", " + ThreadID + ", " + URL, 0);

                                Uri uri = new Uri(URL);

                                byte[] bytes = wc.DownloadData(URL);
                                using (MemoryStream ms = new MemoryStream(bytes))
                                {
                                    using (var bmp = new Bitmap(ms))
                                    {
                                        using (Bitmap newImage = new Bitmap(150, 70))
                                        {
                                            using (Graphics gr = Graphics.FromImage(newImage))
                                            {
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
                                            }
                                        }
                                    }
                                }
                                Logging.LogMessageToFile("Successfully migrated: " + Title + ", " + ThreadID + ", " + URL, 0);
                            }
                            catch (WebException ex)
                            {
                                Logging.LogMessageToFile("Unexpected error migrating: " + ex.Message, 2);
                            }
                        }
                    }

                    Logging.LogMessageToFile("Migration to database completed successfully!", 0);
                    File.Delete(Properties.Settings.Default.AppDataPath + @"\Blacklist.xml");
                    Logging.LogMessageToFile("Blacklist.xml deleted.", 0);
                    Properties.Settings.Default.dbMigrated = true;
                    Properties.Settings.Default.Save();
                }
                else
                {
                    Logging.LogMessageToFile("No blacklist.xml file to migrate", 0);
                    Properties.Settings.Default.dbMigrated = true;
                    Properties.Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Error migrating: " + ex.Message, 2);
            }
        }

        //======================================================================
        // Add wallpaper to blacklisted
        //======================================================================
        public async Task<bool> blacklistWallpaper(string url, string title, string threadid)
        {
            try
            {
                string thumbnail = getThumbnail(url);
                string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                title = title.Replace("'", "''");
                using (SQLiteCommand command = new SQLiteCommand("INSERT INTO blacklist (thumbnail, title, threadid, url, date) values (@thumbnail, @title, @threadid, @url, @dateTime)", m_dbConnection))
                {
                    command.Parameters.AddWithValue("thumbnail", thumbnail);
                    command.Parameters.AddWithValue("title", title);
                    command.Parameters.AddWithValue("threadid", threadid);
                    command.Parameters.AddWithValue("url", url);
                    command.Parameters.AddWithValue("dateTime", dateTime);
                    command.ExecuteNonQuery();
                }
                Logging.LogMessageToFile("Wallpaper blacklisted! Title: " + title + ", Thread ID: " + threadid + ", URL: " + url, 0);
                return true;
            }
            catch(Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error blacklisting wallpaper: " + ex.Message, 1);
                return false;
            }
        }

        //======================================================================
        // Add wallpaper to favourites
        //======================================================================
        public async Task<bool> faveWallpaper(string url, string title, string threadid)
        {
            try
            {
                string thumbnail = getThumbnail(url);
                string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                title = title.Replace("'", "''");
                using (SQLiteCommand command = new SQLiteCommand("INSERT INTO favourites (thumbnail, title, threadid, url, date) values (@thumbnail, @title, @threadid, @url, @dateTime)", m_dbConnection))
                {
                    command.Parameters.AddWithValue("thumbnail", thumbnail);
                    command.Parameters.AddWithValue("title", title);
                    command.Parameters.AddWithValue("threadid", threadid);
                    command.Parameters.AddWithValue("url", url);
                    command.Parameters.AddWithValue("dateTime", dateTime);
                    command.ExecuteNonQuery();
                }
                Logging.LogMessageToFile("Wallpaper added to favourites! Title: " + title + ", Thread ID: " + threadid + ", URL: " + url, 0);
                return true;
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error favouriting wallpaper: " + ex.Message, 1);
                return false;
            }
        }

        //======================================================================
        // Add version
        //======================================================================
        public void addVersion()
        {
            try
            {
                string currentVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
                string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                using (SQLiteCommand command = new SQLiteCommand("INSERT INTO version (version, date) values (@version, @dateTime)", m_dbConnection))
                {
                    command.Parameters.AddWithValue("dateTime", dateTime);
                    command.Parameters.AddWithValue("version", currentVersion);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error adding version to database: " + ex.Message, 1);
            }
        }

        //======================================================================
        // Add wallpaper to history
        //======================================================================
        public void historyWallpaper(string url, string title, string threadid)
        {
            try
            {
                string thumbnail = getThumbnail(url);
                string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                title = title.Replace("'", "''");
                using (SQLiteCommand command = new SQLiteCommand("INSERT INTO history (thumbnail, title, threadid, url, date) values (@thumbnail, @title, @threadid, @url, @dateTime)", m_dbConnection))
                { 
                    command.Parameters.AddWithValue("thumbnail", thumbnail);
                    command.Parameters.AddWithValue("title", title);
                    command.Parameters.AddWithValue("threadid", threadid);
                    command.Parameters.AddWithValue("url", url);
                    command.Parameters.AddWithValue("dateTime", dateTime);
                    command.ExecuteNonQuery();
                }

                //return true;
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error adding wallpaper to history: " + ex.Message, 1);
                //return false;
            }
        }

        //======================================================================
        // Remove wallpaper from blacklist
        //======================================================================
        public void removeFromBlacklist(string url, string date)
        {
            try
            {
                using (SQLiteCommand command = new SQLiteCommand("DELETE FROM blacklist WHERE date = @dateTime", m_dbConnection))
                {
                    command.Parameters.AddWithValue("dateTime", date);
                    command.ExecuteNonQuery();
                }
                Logging.LogMessageToFile("Wallpaper removed from blacklist! URL: " + url, 0);
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error removing wallpaper from Blacklist: " + ex.Message, 1);
            }
        }

        //======================================================================
        // Remove wallpaper from favourites
        //======================================================================
        public void removeFromFavourites(string url, string date)
        {
            try
            {
                using (SQLiteCommand command = new SQLiteCommand("DELETE FROM favourites WHERE date = @dateTime", m_dbConnection))
                {
                    command.Parameters.AddWithValue("dateTime", date);
                    command.ExecuteNonQuery();
                }
                Logging.LogMessageToFile("Wallpaper removed from favourites! URL: " + url, 0);
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error removing wallpaper from favourites: " + ex.Message, 1);
            }
        }

        //======================================================================
        // Remove wallpaper from History
        //======================================================================
        public void removeFromHistory(string url, string date)
        {
            try
            {
                using (SQLiteCommand command = new SQLiteCommand("DELETE FROM history WHERE date = @dateTime", m_dbConnection))
                {
                    command.Parameters.AddWithValue("dateTime", date);
                    command.ExecuteNonQuery();
                }
                Logging.LogMessageToFile("Wallpaper removed from history! URL: " + url, 0);
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error removing wallpaper from history: " + ex.Message, 1);
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
                Logging.LogMessageToFile("Unexpected error retrieving History from database: " + ex.Message, 1);
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
                Logging.LogMessageToFile("Unexpected error retrieving Blacklist from database: " + ex.Message, 1);
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
                Logging.LogMessageToFile("Unexpected error retrieving favourites from database: " + ex.Message, 1);
                return null;
            }
        }

        //======================================================================
        // Get wallpaper history for deletion on exit
        //======================================================================
        public List<Database> deleteOnExit()
        {
            try
            {
                List<Database> items = new List<Database>();
                string sql = "SELECT threadid FROM history";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new Database();
                        item.threadidstring = (string)reader["threadid"];

                        items.Add(item);
                    }
                }

                return items;
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error retrieving threadid's from database: " + ex.Message, 1);
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
                Logging.LogMessageToFile("Unexpected error checking for Blacklist entry: " + ex.Message, 1);
                return false;
            }
        }

        //======================================================================
        // Generate thumbnail of the wallpaper and convert to base64 to store in db
        //======================================================================
        public string getThumbnail(string URL)
        {
            try
            {
                Uri uri = new Uri(URL);
                using (WebClient wc = Proxy.setProxy())
                {
                    try
                    {
                        byte[] bytes = wc.DownloadData(URL);
                        using (MemoryStream ms = new MemoryStream(bytes))
                        {
                            using (var bmp = new Bitmap(ms))
                            {
                                using (Bitmap newImage = new Bitmap(150, 70))
                                {
                                    using (Graphics gr = Graphics.FromImage(newImage))
                                    {
                                        gr.SmoothingMode = SmoothingMode.HighQuality;
                                        gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                        gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                        gr.DrawImage(bmp, new Rectangle(0, 0, 150, 70));
                                        using (MemoryStream newms = new MemoryStream())
                                        {
                                            newImage.Save(newms, System.Drawing.Imaging.ImageFormat.Jpeg);
                                            byte[] imageArray = newms.ToArray();
                                            string base64ImageRepresentation = Convert.ToBase64String(imageArray);
                                            return base64ImageRepresentation;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.LogMessageToFile("Unexpected error generating wallpaper thumbnail: " + ex.Message, 1);
                        return "";
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error generating wallpaper thumbnail: " + ex.Message, 1);
                return "";
            }
        }

        //======================================================================
        // Delete all values from table
        //======================================================================
        public bool wipeTable(string table)
        {
            try
            {
                if (table == "favourites")
                {
                    using (SQLiteCommand command = new SQLiteCommand("DELETE FROM favourites", m_dbConnection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                if (table == "history")
                {
                    using (SQLiteCommand command = new SQLiteCommand("DELETE FROM history", m_dbConnection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                if (table == "blacklist")
                {
                    using (SQLiteCommand command = new SQLiteCommand("DELETE FROM blacklist", m_dbConnection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                Logging.LogMessageToFile("All " + table + " contents have been successfully deleted!", 0);
                return true;
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error deleting data from " + table + ": " + ex.Message, 1);
                return false;
            }
        }

        //======================================================================
        // Backup database
        //======================================================================
        public bool backupDatabase(string backupPath)
        {
            try
            {
                string dbName = "Reddit-Wallpaper-Changer.sqlite";
                string backupSource = Properties.Settings.Default.AppDataPath;
                disconnectFromDatabase();
                Logging.LogMessageToFile("Backing up database to: " + Path.Combine(backupPath, dbName), 0);
                File.Copy(Path.Combine(backupSource, dbName), Path.Combine(backupPath, dbName), true);
                connectToDatabase();
                return true;
            }

            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error backing up database: " + ex.Message, 2);
                if (m_dbConnection != null && m_dbConnection.State == System.Data.ConnectionState.Open)
                {
                    connectToDatabase();
                }
                return false;
            }
        }

        //======================================================================
        // Restore sqlite database
        //======================================================================
        public bool restoreDatabase(string restoreSource)
        {
            try
            {
                string dbName = "Reddit-Wallpaper-Changer.sqlite";
                string restorePath = Properties.Settings.Default.AppDataPath;
                disconnectFromDatabase();
                Logging.LogMessageToFile("Restoring database to: " + Path.Combine(restorePath, dbName), 0);
                File.Copy(restoreSource, Path.Combine(restorePath, dbName), true);
                connectToDatabase();
                return true;
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error restoring database: " + ex.Message, 2);
                if (m_dbConnection != null && m_dbConnection.State == System.Data.ConnectionState.Open)
                {
                    connectToDatabase();
                }
                return false;
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


