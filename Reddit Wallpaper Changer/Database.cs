using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;

namespace Reddit_Wallpaper_Changer
{
    class Database
    {
        SQLiteConnection m_dbConnection;
        string dbPath = Properties.Settings.Default.AppDataPath + @"\Blacklist.sqlite";

        public string imgstring;
        public string titlestring;
        public string threadidstring;
        public string urlstring;

        //======================================================================
        // Create the SQLite Blacklist database
        //======================================================================
        public void createDatabase()
        {
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }
        }

        //======================================================================
        // Open connection to the Blacklist database
        //======================================================================
        public void connnectToDatabase()
        {
            m_dbConnection = new SQLiteConnection("Data Source=" + dbPath + ";Version=3;");
            m_dbConnection.Open();
        }

        //======================================================================
        // Pupulate the blacklisted history panel
        //======================================================================
        public void createTable()
        {
            string sql = "CREATE TABLE blacklist (thumbnail STRING, title STRING, threadid STRING, url STRING)";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }

        //======================================================================
        // One off task to migrate old Blacklist.xml file into Blacklist.sqlite
        //======================================================================
        public void migrateOldBlacklist()
        {
            
        }

        //======================================================================
        // Add wallpaper details into the blacklisted database
        //======================================================================
        public void insertIntoTable(string thumbnail, string title, string threadid, string url)
        {
            string sql = "INSERT INTO blacklist (thumbnail, title, threadid, url) values ('" + thumbnail + "', '" + title + "', '" + threadid + "', '" + url + "')";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }

        //======================================================================
        // Retrieve blacklist data from the database
        //======================================================================
        public string getFromTable()
        {
            string sql = "SELECT * FROM blacklist";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                imgstring = reader["thumbnail"].ToString();
                titlestring = reader["title"].ToString();
                threadidstring = reader["threadid"].ToString();
                urlstring = reader["url"].ToString();               
            }

            return imgstring;
        }

        //======================================================================
        // Remove wallpaper form the blacklist
        //======================================================================
        public void deleteFromTable()
        {

        }

        //======================================================================
        // Check for blacklisted wallpaper
        //======================================================================
        public bool checkForEntry(string url)
        {
            string tmp = "";
            string sql = "SELECT url FROM blacklist WHERE url = '" + url + "'";
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
    }
}
