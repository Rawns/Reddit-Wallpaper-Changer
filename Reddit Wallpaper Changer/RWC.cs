using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using System.Net;
using System.Web;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections;
using Microsoft.Win32;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Globalization;


// RWC
namespace Reddit_Wallpaper_Changer
{
    public partial class RWC : Form
    {
        #region Windows API
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr result);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, IntPtr ZeroOnly);
        #endregion

        public static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPEG", ".BMP", ".GIF", ".PNG" };
        bool realClose = false;
        Color selectedBackColor = Color.FromArgb(214, 234, 244);
        Color selectedBorderColor = Color.FromArgb(130, 195, 228);
        Button selectedButton;
        Panel selectedPanel;
        String currentVersion;                    
        Bitmap currentWallpaper;
        String currentThread;
        ArrayList monitorRec = new ArrayList();
        Random r;
        int currentMouseOverRow;
        public String searchQueryValue;
        Boolean enabledOnSleep;
        ArrayList historyRepeated = new ArrayList();
        int noResultCount = 0;
        BackgroundWorker bw = new BackgroundWorker();
        Database database = new Database();
        SaveWallpaper savewallpaper = new SaveWallpaper();
        List<string> historyList = new List<string>();
        bool dissmissedOnce = false;

        public RWC()
        {
            InitializeComponent();

            // Copy user settings from previous application version if necessary (part of the upgrade proxess)
            if (Properties.Settings.Default.updateSettings)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.updateSettings = false;
                Properties.Settings.Default.Save();
                database.addVersion();
            }

            Logging.LogMessageToFile("===================================================================================================================", 0);
            Random random = new Random();
            int db = random.Next(0, 1000);
            if (db == 500) { SuperSecret.DickButt(); }
            Logging.LogMessageToFile("Reddit Wallpaper Changer Version " + Assembly.GetEntryAssembly().GetName().Version.ToString(), 0);
            Logging.LogMessageToFile("RWC is starting.", 0);
            Logging.LogMessageToFile("RWC Interface Loaded.", 0);

            SystemEvents.PowerModeChanged += OnPowerChange;

            #region ToolTips

            ToolTip tt = new ToolTip();
            tt.AutoPopDelay = 7500;
            tt.InitialDelay = 1000;
            tt.ReshowDelay = 500;
            tt.ShowAlways = true;
            tt.ToolTipTitle = "RWC";
            tt.ToolTipIcon = ToolTipIcon.Info;            

            tt.SetToolTip(this.chkAutoStart, "Run Reddit Wallpaper Changer when your computer starts.");
            tt.SetToolTip(this.chkStartInTray, "Start Reddit Wallpaper Changer minimised.");
            tt.SetToolTip(this.chkProxy, "Configure a proxy server for Reddit Wallpaper Changer to use.");
            tt.SetToolTip(this.chkAuth, "Enable if your proxy server requires authentication.");
            tt.SetToolTip(this.btnBrowse, "Sellect the downlaod destination for saved wallpapers.");
            tt.SetToolTip(this.btnSave, "Saves your settings.");
            tt.SetToolTip(this.btnWizard, "Open the Search wizard.");
            tt.SetToolTip(this.wallpaperGrabType, "Choose how you want to find a wallpaper.");
            tt.SetToolTip(this.changeTimeValue, "Choose how oftern to change your wallpaper.");
            tt.SetToolTip(this.subredditTextBox, "Enter the subs to scrape for wallpaper (eg, wallpaper, earthporn etc).\r\nMultiple subs can be provided and separated with a +.");
            tt.SetToolTip(this.chkAutoSave, "Enable this to automatically save all wallpapers to the below directory.");
            tt.SetToolTip(this.chkFade, "Enable this for a faded wallpaper transition using Active Desktop.\r\nDisable this option if you experience any issues when the wallpaper changes.");
            tt.SetToolTip(this.chkNotifications, "Disables all RWC System Tray/Notification Centre notifications.");
            tt.SetToolTip(this.chkFitWallpaper, "Enable this option to ensure that wallpapers matching your resolution are applied.\r\n\r\n" +
                "NOTE: If you have multiple screens, it will validate wallpaper sizes against the ENTIRE desktop area and not just your primary display (eg, 3840x1080 for two 1980x1080 displays).\r\n" +
                "Best suited to single monitors, or duel monitors with matching resolutions. If you experience a lack of wallpapers, try disabeling this option.");
            tt.SetToolTip(this.chkSuppressDuplicates, "Disable this option if you don't mind the occasional repeating wallpaper in the same session.");
            tt.SetToolTip(this.chkWallpaperInfoPopup, "Displays a mini wallpaper info popup at the bottom right of your primary display for 5 seconds.\r\n" +
                "Note: The 'Disable Notifications' option suppresses this popup.");
            tt.SetToolTip(this.chkAutoSaveFaves, "Enable this option to automatically save Favourite wallpapers to the below directory.");
            tt.SetToolTip(this.btnClearHistory, "This will erase ALL historical information from the History panel.");
            tt.SetToolTip(this.btnClearFavourites, "This will erase ALL wallpaper information from your Favourites.");
            tt.SetToolTip(this.btnClearBlacklisted, "This will erase ALL wallpaper information from your Blacklist.");
            tt.SetToolTip(this.btnBackup, "Backup Reddit Wallpaper Changer's database.");
            tt.SetToolTip(this.btnRestore, "Restore a previous backup.");
            tt.SetToolTip(this.btnRebuildThumbnails, "This will wipe the current thumbnail cache and recreate it.");
            tt.SetToolTip(this.chkUpdates, "Enable or disable automatic update checks.\r\nA manual check for updates can be done in the 'About' panel.");

            // Monitors
            tt.SetToolTip(this.btnWallpaperHelp, "Show info on the different wallpaper styles.");

            // About
            tt.SetToolTip(this.btnSubreddit, "Having issues? You can get support by posting on the Reddit Wallpaper Changer Subreddit.");
            tt.SetToolTip(this.btnBug, "Spotted a bug? Open a ticket on GitHub by clicking here!");
            tt.SetToolTip(this.btnDonate, "Reddit Wallpaper Changer is maintained by one guy in his own time!\r\nIf you'd like to say 'thanks' by getting him a beer, click here! :)");
            tt.SetToolTip(this.btnUpdate, "Click here to manually check for updates.");
            tt.SetToolTip(this.btnLog, "Click here to open the RWC log file in your default text editor.");
            tt.SetToolTip(this.btnImport, "Import custom settings from an XML file.");
            tt.SetToolTip(this.btnExport, "Export your current settings into an XML file.");
            tt.SetToolTip(this.btnUpload, "Having issues? Click here to automatically upload your log file to Pastebin!");
            #endregion
        }

        public int screens { get; set; }

        //======================================================================
        // Wallpaper layout styles
        //======================================================================
        public enum Style
        {
            Fill,
            Fit,
            Span,
            Stretch,
            Tile,
            Center
        }

        //======================================================================
        // Code for if the computer sleeps or wakes up
        //======================================================================
        void OnPowerChange(Object sender, PowerModeChangedEventArgs e)
        {
            Logging.LogMessageToFile("Device suspended. Going to sleep.", 0);
            if (e.Mode == PowerModes.Suspend)
            {
                if (wallpaperChangeTimer.Enabled == true)
                {
                    enabledOnSleep = true;
                    wallpaperChangeTimer.Enabled = false;
                }
            }

            else if (e.Mode == PowerModes.Resume)
            {
                Logging.LogMessageToFile("Device resumed. Back in action!", 0);
                if (enabledOnSleep)
                {
                    wallpaperChangeTimer.Enabled = true;
                }
            }
        }

        //======================================================================
        // Form load code
        //======================================================================
        private void RWC_Load(object sender, EventArgs e)
        {
            this.FormClosing += new FormClosingEventHandler(RWC_FormClosing);
            this.Size = new Size(466, 531);
            updateStatus("RWC Setup Initating.");
            r = new Random();
            taskIcon.Visible = true;
            if (Properties.Settings.Default.rebuildThumbCache == true) { removeThumbnailCache(); }
            setupSavedWallpaperLocation();
            setupAppDataLocation();
            setupThumbnailCache();
            buildThumbnailCache();
            setupProxySettings();
            setupButtons();
            setupPanels();          
            setupOthers();
            setupForm();
            logSettings();
            database.connectToDatabase();
            if (Properties.Settings.Default.dbMigrated == false) { database.migrateOldBlacklist(); }
            populateHistory();
            populateFavourites();
            populateBlacklist();
            updateStatus("RWC Setup Initated.");
            checkInternetTimer.Enabled = true;
        }

        //======================================================================
        // Set up a folder to place Logs, Blacklists, Favorites etc. in
        //======================================================================
        private void setupAppDataLocation()
        {
            string appDataFolderPath;
            if (Properties.Settings.Default.AppDataPath.Any())
                appDataFolderPath = Properties.Settings.Default.AppDataPath;
            else
                appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Reddit Wallpaper Changer";

            Directory.CreateDirectory(appDataFolderPath);
            Properties.Settings.Default.AppDataPath = appDataFolderPath;
            Properties.Settings.Default.Save();  
        }

        //======================================================================
        // Set up a thumbnail cache
        //======================================================================
        private void setupThumbnailCache()
        {
            string thumbnailCachePath;
            if (Properties.Settings.Default.thumbnailCache.Any())
                thumbnailCachePath = Properties.Settings.Default.thumbnailCache;
            else
                thumbnailCachePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Reddit Wallpaper Changer\ThumbnailCache";

            Directory.CreateDirectory(thumbnailCachePath);
            Properties.Settings.Default.thumbnailCache = thumbnailCachePath;
            Properties.Settings.Default.Save();
        }


        //======================================================================
        // Log startup info
        //======================================================================
        private void logSettings()
        {
            int screens = Screen.AllScreens.Count();

            Logging.LogMessageToFile("Auto Start: " + Properties.Settings.Default.autoStart, 0);
            Logging.LogMessageToFile("Start In Tray: " + Properties.Settings.Default.startInTray, 0);
            Logging.LogMessageToFile("Proxy Enabled: " + Properties.Settings.Default.useProxy, 0);
            if (Properties.Settings.Default.useProxy == true)
            {
                Logging.LogMessageToFile("Proxy Address:" + Properties.Settings.Default.proxyAddress, 0);
                Logging.LogMessageToFile("Proxy Authentication: " + Properties.Settings.Default.proxyAuth, 0);
            }
            Logging.LogMessageToFile("AppData Directory: " + Properties.Settings.Default.AppDataPath, 0);
            Logging.LogMessageToFile("Thumbnail Cache: " + Properties.Settings.Default.thumbnailCache, 0);
            Logging.LogMessageToFile("Automatically check for updates: " + Properties.Settings.Default.autoUpdateCheck, 0);
            Logging.LogMessageToFile("Save location for wallpapers: " + Properties.Settings.Default.defaultSaveLocation, 0);
            Logging.LogMessageToFile("Auto Save Favourite Wallpapers: " + Properties.Settings.Default.autoSaveFaves, 0); 
            Logging.LogMessageToFile("Auto Save All Wallpapers: " + Properties.Settings.Default.autoSave, 0);
            Logging.LogMessageToFile("Wallpaper Grab Type: " + Properties.Settings.Default.wallpaperGrabType, 0);
            Logging.LogMessageToFile("Selected Subreddits: " + Properties.Settings.Default.subredditsUsed, 0);
            Logging.LogMessageToFile("Wallpaper Fade Effect: " + Properties.Settings.Default.wallpaperFade, 0);
            Logging.LogMessageToFile("Search Query: " + Properties.Settings.Default.searchQuery, 0);
            Logging.LogMessageToFile("Change wallpaper every " + Properties.Settings.Default.changeTimeValue + " " + changeTimeType.Text, 0);
            Logging.LogMessageToFile("Number of detected displays: " + screens, 0);
            Logging.LogMessageToFile("Wallpaper Position: " + Properties.Settings.Default.wallpaperStyle, 0);
            Logging.LogMessageToFile("Validate wallpaper size: " + Properties.Settings.Default.fitWallpaper, 0);
            Logging.LogMessageToFile("Wallpaper Info Popup: " + Properties.Settings.Default.wallpaperInfoPopup, 0);
        }

        //======================================================================
        // Set folder path for saving wallpapers
        //======================================================================
        private void setupSavedWallpaperLocation()
        {
            if (Properties.Settings.Default.defaultSaveLocation == "")
            {
                String savedWallpaperPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + @"\Saved Wallpapers";
                System.IO.Directory.CreateDirectory(savedWallpaperPath);
                Properties.Settings.Default.defaultSaveLocation = savedWallpaperPath;
                Properties.Settings.Default.Save();
            }

            txtSavePath.Text = Properties.Settings.Default.defaultSaveLocation;
            chkAutoSave.Checked = Properties.Settings.Default.autoSave;            
        }

        //======================================================================
        // Windows Menus
        //======================================================================
        private void createWindowsMenu()
        {
            RegistryKey key;
            key = Registry.ClassesRoot.CreateSubKey("Folder\\shell\\Change Wallpaper", RegistryKeyPermissionCheck.ReadWriteSubTree);
            key = Registry.ClassesRoot.CreateSubKey("Folder\\shell\\Change Wallpaper\\command", RegistryKeyPermissionCheck.ReadWriteSubTree);
            key.SetValue("", Application.ExecutablePath);
        }

        //======================================================================
        // Populate the search query text box
        //======================================================================
        public void changeSearchQuery(string text)
        {
            searchQuery.Text = text;
        }

        //======================================================================
        // Set proxy settings if configured
        //======================================================================
        private void setupProxySettings()
        {
            if (Properties.Settings.Default.useProxy == true)
            {
                this.chkProxy.Checked = true;
                this.txtProxyServer.Enabled = true;
                this.txtProxyServer.Text = Properties.Settings.Default.proxyAddress;

                if (Properties.Settings.Default.proxyAuth == true)
                {
                    this.chkAuth.Enabled = true;
                    this.chkAuth.Checked = true;
                    this.txtUser.Enabled = true;
                    this.txtUser.Text = Properties.Settings.Default.proxyUser;
                    this.txtPass.Enabled = true;
                    txtPass.Text = Properties.Settings.Default.proxyPass;
                }
            }
        }

        //======================================================================
        // Setup the form
        //======================================================================
        private void setupForm()
        {
            //Change Label if it is a Multi Reddit.
            if (subredditTextBox.Text.Contains("/m/"))
            {
                label5.Text = "MultiReddit";
                label5.ForeColor = Color.Red;
            }
            else
            {
                label5.Text = "Subreddit(s):";
                label5.ForeColor = Color.Black;
            }
        }

        //======================================================================
        // Set up other aspects of the application
        //======================================================================
        private void setupOthers()
        
        {
            if (Properties.Settings.Default.UpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
            }

            wallpaperGrabType.SelectedIndex = Properties.Settings.Default.wallpaperGrabType;
            subredditTextBox.Text = Properties.Settings.Default.subredditsUsed;
            searchQuery.Text = Properties.Settings.Default.searchQuery;
            changeTimeValue.Value = Properties.Settings.Default.changeTimeValue;
            changeTimeType.SelectedIndex = Properties.Settings.Default.changeTimeType;
            chkStartInTray.Checked = Properties.Settings.Default.startInTray;
            chkAutoStart.Checked = Properties.Settings.Default.autoStart;
            chkFade.Checked = Properties.Settings.Default.wallpaperFade;
            chkNotifications.Checked = Properties.Settings.Default.disableNotifications;
            chkFitWallpaper.Checked = Properties.Settings.Default.fitWallpaper;
            chkSuppressDuplicates.Checked = Properties.Settings.Default.suppressDuplicates;
            chkWallpaperInfoPopup.Checked = Properties.Settings.Default.wallpaperInfoPopup;
            chkUpdates.Checked = Properties.Settings.Default.autoUpdateCheck;
            chkAutoSaveFaves.Checked = Properties.Settings.Default.autoSaveFaves;
            currentVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
            lblVersion.Text = "Current Version: " + currentVersion;
        }

        //======================================================================
        // Setup the five panels
        //======================================================================
        private void setupPanels()
        {
            int w = 450;
            int h = 405;
            aboutPanel.Size = new Size(w, h);
            configurePanel.Size = new Size(w, h);
            monitorPanel.Size = new Size(w, h);
            historyPanel.Size = new Size(w, h);
            blacklistPanel.Size = new Size(w, h);
            favouritesPanel.Size = new Size(w, h);

            int x = 0;
            int y = 65;    
            aboutPanel.Location = new Point(x, y);
            configurePanel.Location = new Point(x, y);
            monitorPanel.Location = new Point(x, y);
            historyPanel.Location = new Point(x, y);
            blacklistPanel.Location = new Point(x, y);
            favouritesPanel.Location = new Point(x, y);
        }

        //======================================================================
        // Setup the main buttons
        //======================================================================
        private void setupButtons()
        {
            aboutButton.BackColor = Color.White;
            aboutButton.FlatAppearance.BorderColor = Color.White;
            aboutButton.FlatAppearance.MouseDownBackColor = Color.White;
            aboutButton.FlatAppearance.MouseOverBackColor = Color.White;

            historyButton.BackColor = Color.White;
            historyButton.FlatAppearance.BorderColor = Color.White;
            historyButton.FlatAppearance.MouseDownBackColor = Color.White;
            historyButton.FlatAppearance.MouseOverBackColor = Color.White;

            monitorButton.BackColor = Color.White;
            monitorButton.FlatAppearance.BorderColor = Color.White;
            monitorButton.FlatAppearance.MouseDownBackColor = Color.White;
            monitorButton.FlatAppearance.MouseOverBackColor = Color.White;

            blacklistButton.BackColor = Color.White;
            blacklistButton.FlatAppearance.BorderColor = Color.White;
            blacklistButton.FlatAppearance.MouseDownBackColor = Color.White;
            blacklistButton.FlatAppearance.MouseOverBackColor = Color.White;

            favouritesButton.BackColor = Color.White;
            favouritesButton.FlatAppearance.BorderColor = Color.White;
            favouritesButton.FlatAppearance.MouseDownBackColor = Color.White;
            favouritesButton.FlatAppearance.MouseOverBackColor = Color.White;

            selectedPanel = configurePanel;
            selectedButton = configureButton;
        }

        //======================================================================
        // Parse subreddits string into array of current usable subreddits
        // Expects a URL-safe list of subreddit names accompanied by an
        //  optional 24-hour time range in the format [H:mm-H:mm]
        // Skips malformed entries
        //======================================================================
        private string[] parseSubredditsList(string subs)
        {
            var subsList = new List<string>();

            subs = subs.Trim('+');
            var splitList = subs.Split('+').ToList();

            Regex nameTimeRegex = new Regex(@"^(?<name>[^[\]+]+)(?:\[(?<t1>[0-9]{1,2}:[0-9]{2})-(?<t2>[0-9]{1,2}:[0-9]{2})+\])?");
            foreach (var entry in splitList)
            {
                var r = nameTimeRegex.Match(entry);
                if (!r.Groups["name"].Success)
                    continue;

                if (!r.Groups["t1"].Success)
                {
                    subsList.Add(r.Groups["name"].Value);
                    continue;
                }

                var t1 = r.Groups["t1"].Value;
                var t2 = r.Groups["t2"].Value;

                DateTime dt1;
                DateTime dt2;

                try
                {
                    dt1 = System.DateTime.ParseExact(t1, "H:mm", CultureInfo.InvariantCulture);
                    dt2 = System.DateTime.ParseExact(t2, "H:mm", CultureInfo.InvariantCulture);
                }
                catch (FormatException e)
                {
                    Logging.LogMessageToFile("Malformed timecode in entry: \"" + entry + "\". Skipping.", 1);
                    continue;
                }

                if (dt2 < dt1)
                    dt2 += new TimeSpan(1, 0, 0, 0);

                var now = System.DateTime.Now;
                if (now >= dt1 && now <= dt2)
                {
                    subsList.Add(r.Groups["name"].Value);
                }
            }

            return subsList.ToArray();
        }

        //======================================================================
        // Config button clicked
        //======================================================================
        private void configureButton_Click(object sender, EventArgs e)
        {
            if (selectedPanel != configurePanel)
            {
                selectedPanel.Visible = false;
                configurePanel.Visible = true;
                cleanButton(selectedButton);
                selectButton(configureButton);
                selectedButton = configureButton;
                selectedPanel = configurePanel;
            }
        }

        //======================================================================
        // Open the About panel
        //======================================================================
        private void aboutButton_Click(object sender, EventArgs e)
        {
            if (selectedPanel != aboutPanel)
            {
                selectedPanel.Visible = false;
                aboutPanel.Visible = true;
                cleanButton(selectedButton);
                selectButton(aboutButton);
                selectedButton = aboutButton;
                selectedPanel = aboutPanel;
            }
        }

        //======================================================================
        // History button click
        //======================================================================
        private void historyButton_Click(object sender, EventArgs e)
        {
            if (selectedPanel != historyPanel)
            {
                selectedPanel.Visible = false;
                historyPanel.Visible = true;
                cleanButton(selectedButton);
                selectButton(historyButton);
                selectedButton = historyButton;
                selectedPanel = historyPanel;
            }
        }

        //======================================================================
        // Open the Blacklisted panel
        //======================================================================
        private void blacklistButton_Click(object sender, EventArgs e)
        {
            if (selectedPanel != blacklistPanel)
            {
                selectedPanel.Visible = false;
                blacklistPanel.Visible = true;
                cleanButton(selectedButton);
                selectButton(blacklistButton);
                selectedButton = blacklistButton;
                selectedPanel = blacklistPanel;
            }
        }


        //======================================================================
        // Open the Favourites panel
        //======================================================================
        private void favouritesButton_Click(object sender, EventArgs e)
        {
            if (selectedPanel != favouritesPanel)
            {
                selectedPanel.Visible = false;
                favouritesPanel.Visible = true;
                cleanButton(selectedButton);
                selectButton(favouritesButton);
                selectedButton = favouritesButton;
                selectedPanel = favouritesPanel;
            }
        }

        //======================================================================
        // Monitor button click
        //======================================================================
        private void monitorButton_Click(object sender, EventArgs e)
        {
            if (selectedPanel != monitorPanel)
            {
                selectedPanel.Visible = false;
                monitorPanel.Visible = true;
                cleanButton(selectedButton);
                selectButton(monitorButton);
                selectedButton = monitorButton;
                selectedPanel = monitorPanel;
                monitorPanel_Paint();
            }
        }

        //======================================================================
        // Set sellected button formatting
        //======================================================================
        private void selectButton(Button btn)
        {
            btn.BackColor = selectedBackColor;
            btn.FlatAppearance.BorderColor = selectedBorderColor;
            btn.FlatAppearance.MouseDownBackColor = selectedBackColor;
            btn.FlatAppearance.MouseOverBackColor = selectedBackColor;
        }

        //======================================================================
        // Set unsellected button formatting
        //======================================================================
        private void cleanButton(Button btn)
        {
            btn.BackColor = Color.White;
            btn.FlatAppearance.BorderColor = Color.White;
            btn.FlatAppearance.MouseDownBackColor = Color.White;
            btn.FlatAppearance.MouseOverBackColor = Color.White;
        }

        //======================================================================
        // Go to Ugleh's Reddit page
        //======================================================================
        private void redditLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.reddit.com/user/Ugleh/");
        }

        //======================================================================
        // Check for updates to the software
        //======================================================================
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            Logging.LogMessageToFile("Manual check for updates initiated.", 0);

            btnUpdate.Enabled = false;
            btnUpdate.Text = "Checking....";
            currentVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();

            WebClient wc = Proxy.setProxy();
            try
            {
                String latestVersion = wc.DownloadString("https://raw.githubusercontent.com/Rawns/Reddit-Wallpaper-Changer/master/version");

                if (!latestVersion.ToString().Contains(currentVersion.Trim().ToString()))
                {
                    Logging.LogMessageToFile("Current Version: " + currentVersion + ". " + "Latest version: " + latestVersion, 0);
                    DialogResult choice = MessageBox.Show("You are running version " + currentVersion + ".\r\n\r\n" + "Download version " + latestVersion.Split(new[] { '\r', '\n' }).FirstOrDefault() + " now?", "Update Avaiable!", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (choice == DialogResult.Yes)
                    {
                        Form Update = new Update(latestVersion, this);
                        Update.Show();
                    }
                    else if (choice == DialogResult.No)
                    {
                        btnUpdate.Enabled = true;
                        btnUpdate.Text = "Check for Updates";
                        return;
                    }
                }
                else
                {
                    Logging.LogMessageToFile("Reddit Wallpaper Changer is up to date (" + currentVersion + ")", 0);
                    taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                    taskIcon.BalloonTipTitle = "Reddit Wallpaper Changer";
                    taskIcon.BalloonTipText = "RWC is up to date! :)";
                    taskIcon.ShowBalloonTip(700);
                }
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Error checking for updates: " + ex.Message, 2);
                taskIcon.BalloonTipIcon = ToolTipIcon.Error;
                taskIcon.BalloonTipTitle = "Reddit Wallpaper Changer";
                taskIcon.BalloonTipText = "Error checking for updates! :(";
                taskIcon.ShowBalloonTip(700);
            }

            wc.Dispose();
            btnUpdate.Text = "Check For Updates";
            btnUpdate.Enabled = true;
        }


        //======================================================================
        // Open the RWC Subreddit
        //======================================================================
        private void btnSubreddit_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.reddit.com/r/rwallpaperchanger/");
        }

        //======================================================================
        // Save all settings
        //======================================================================
        private void saveButton_Click(object sender, EventArgs e)
        {
            Logging.LogMessageToFile("Settings successfully saved.", 0);
            Logging.LogMessageToFile("New settings...", 0);
            saveData();
            // changeWallpaperTimer.Enabled = true;
            updateStatus("Settings Saved!");
            
        }

        //======================================================================
        // Save button code
        //======================================================================
        private void saveData()
        {
            if (changeTimeType.Text == "Days" && changeTimeValue.Value >= 8)
            {
                MessageBox.Show("Sorry, but upper limit for wallpaper changes is 7 Days!", "Too many days!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool updateTimerBool = false;
            if (Properties.Settings.Default.autoStart != chkAutoStart.Checked)
            {
                startup(chkAutoStart.Checked);
            }

            Properties.Settings.Default.startInTray = chkStartInTray.Checked;
            Properties.Settings.Default.autoStart = chkAutoStart.Checked;
            Properties.Settings.Default.wallpaperGrabType = wallpaperGrabType.SelectedIndex;
            Properties.Settings.Default.subredditsUsed = subredditTextBox.Text;
            Properties.Settings.Default.searchQuery = searchQuery.Text;
            if ((Properties.Settings.Default.changeTimeValue != (int)changeTimeValue.Value) || (Properties.Settings.Default.changeTimeType != changeTimeType.SelectedIndex))
                updateTimerBool = true;
            Properties.Settings.Default.changeTimeValue = (int)changeTimeValue.Value;
            Properties.Settings.Default.changeTimeType = changeTimeType.SelectedIndex;
            Properties.Settings.Default.useProxy = chkProxy.Checked;
            Properties.Settings.Default.proxyAddress = txtProxyServer.Text;
            Properties.Settings.Default.proxyAuth = chkAuth.Checked;
            Properties.Settings.Default.proxyUser = txtUser.Text;
            Properties.Settings.Default.proxyPass = txtPass.Text;
            Properties.Settings.Default.defaultSaveLocation = txtSavePath.Text;
            Properties.Settings.Default.autoSave = chkAutoSave.Checked;
            Properties.Settings.Default.disableNotifications = chkNotifications.Checked;
            Properties.Settings.Default.fitWallpaper = chkFitWallpaper.Checked;
            Properties.Settings.Default.suppressDuplicates = chkSuppressDuplicates.Checked;
            Properties.Settings.Default.wallpaperInfoPopup = chkWallpaperInfoPopup.Checked;
            Properties.Settings.Default.wallpaperFade = chkFade.Checked;
            Properties.Settings.Default.autoUpdateCheck = chkUpdates.Checked;
            Properties.Settings.Default.autoSaveFaves = chkAutoSaveFaves.Checked;
            Properties.Settings.Default.Save();
            logSettings();
            if (updateTimerBool)
                updateTimer();
            setupProxySettings();
        }

        //======================================================================
        // Update timer 
        //======================================================================
        private void updateTimer()
        {
            wallpaperChangeTimer.Enabled = false;

            if (Properties.Settings.Default.changeTimeType == 0) //Minutes
            {
                wallpaperChangeTimer.Interval = (int)TimeSpan.FromMinutes(Properties.Settings.Default.changeTimeValue).TotalMilliseconds;
            }
            else if (Properties.Settings.Default.changeTimeType == 1) //Hours
            {
                wallpaperChangeTimer.Interval = (int)TimeSpan.FromHours(Properties.Settings.Default.changeTimeValue).TotalMilliseconds;
            }
            else
            {
                wallpaperChangeTimer.Interval = (int)TimeSpan.FromDays(Properties.Settings.Default.changeTimeValue).TotalMilliseconds;
            }
                       
            wallpaperChangeTimer.Enabled = true;
        }

        //======================================================================
        // Start the timer for regular wallpaper changing
        ////======================================================================
        private void wallpaperChangeTimer_Tick(object sender, EventArgs e)
        {
            changeWallpaperTimer.Enabled = true;
        }

        //======================================================================
        // Search for a wallpaper
        //======================================================================
        #region Change Wallpaper
        private void changeWallpaper()
        {
            Logging.LogMessageToFile("Changing wallpaper.", 0);
            var bw = new BackgroundWorker();

            bw.DoWork += delegate
            {
                Logging.LogMessageToFile("The background worker started successfully and is looking for a wallpaper.", 0);
                if (noResultCount >= 50)
                {
                    noResultCount = 0;

                    if (Properties.Settings.Default.disableNotifications == false)
                    {
                         taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                         taskIcon.BalloonTipTitle = "Reddit Wallpaper Changer";
                         taskIcon.BalloonTipText = "No results after 50 attempts. Disabling Reddit Wallpaper Changer.";
                         taskIcon.ShowBalloonTip(700);
                    }

                    Logging.LogMessageToFile("No results after 50 attempts. Disabeling Reddit Wallpaper Changer.", 1);
                    updateStatus("RWC Disabled.");
                    changeWallpaperTimer.Enabled = false;
                    return;
                }
                updateStatus("Finding New Wallpaper");
                Random random = new Random();
                string[] randomT = { "&t=day", "&t=year", "&t=all", "&t=month", "&t=week" };
                string[] randomSort = { "&sort=relevance", "&sort=hot", "&sort=top", "&sort=comments", "&sort=new" };
                string query = HttpUtility.UrlEncode(Properties.Settings.Default.searchQuery) + "+self%3Ano+((url%3A.png+OR+url%3A.jpg+OR+url%3A.jpeg)+OR+(url%3Aimgur.png+OR+url%3Aimgur.jpg+OR+url%3Aimgur.jpeg)+OR+(url%3Adeviantart))";
                String formURL = "http://www.reddit.com/r/";
                String subreddits = Properties.Settings.Default.subredditsUsed.Replace(" ", "").Replace("www.reddit.com/", "").Replace("reddit.com/", "").Replace("http://", "").Replace("/r/", "");

                var rand = new Random();
                string[] subs = parseSubredditsList(subreddits);
                if (subs.Length == 0)
                {
                    updateStatus("No subreddits available for wallpaper change.");
                    Logging.LogMessageToFile("No subs to pull wallpapers from. Aborting.", 0);
                    return;
                }

                string sub = subs[rand.Next(0, subs.Length)];
                updateStatus("Searching /r/" + sub + " for a wallpaper...");
                Logging.LogMessageToFile("Selected sub to search: " + sub, 0);

                if (sub.Equals(""))
                {
                    formURL += "all";
                }
                else
                {
                    if (sub.Contains("/m/"))
                    {
                        formURL = "http://www.reddit.com/" + subreddits.Replace("http://", "").Replace("https://", "").Replace("user/", "u/");
                    }
                    else
                    {
                        formURL += sub;
                    }
                }
                int wallpaperGrabType = Properties.Settings.Default.wallpaperGrabType;
                switch (wallpaperGrabType)
                {
                    case 0:
                        // Random
                        formURL += "/search.json?q=" + query + randomSort[random.Next(0, 4)] + randomT[random.Next(0, 5)];
                        Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                        break;  
                    case 1:
                        // Newest 
                        formURL += "/search.json?q=" + query + "&sort=new";
                        Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                        break;
                    case 2:
                        // Hot Today
                        formURL += "/search.json?q=" + query + "&sort=hot&t=day";
                        Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                        break;
                    case 3:
                        // Top Last Hour
                        formURL += "/search.json?q=" + query + "&sort=top&t=hour";
                        Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                        break;
                    case 4:
                         // Top Today
                        formURL += "/search.json?q=" + query + "&sort=top&t=day";
                        Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                        break;
                    case 5:
                        // Top Week
                        formURL += "/search.json?q=" + query + "&sort=top&t=week";
                        Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                        break;
                    case 6:
                        // Top Month
                        formURL += "/search.json?q=" + query + "&sort=top&t=month";
                        Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                        break;
                    case 7:
                         // Top Year
                        formURL += "/search.json?q=" + query + "&sort=top&t=year";
                        Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                        break;
                    case 8:
                         // Top All Time
                         formURL += "/search.json?q=" + query + "&sort=top&t=all";
                         Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                         break;
                     case 9:
                         // Truly Random
                         formURL += "/random.json?p=" + (System.Guid.NewGuid().ToString());
                         Logging.LogMessageToFile("Full URL Search String: " + formURL, 0);
                         break;
                }

                String jsonData = "";
                bool failedDownload = false;
                using (WebClient wc = Proxy.setProxy())
                {
                    try
                    {
                        Logging.LogMessageToFile("Searching Reddit for a wallpaper.", 0);
                        jsonData = wc.DownloadString(formURL);
                    }
                    catch (System.Net.WebException ex)
                    {                      
                        updateStatus(ex.Message);
                        Logging.LogMessageToFile("Reddit server error: " + ex.Message, 2);
                        failedDownload = true;
                        restartTimer(breakBetweenChange);
                        return;
                    }
                    catch (Exception ex)
                    {
                        updateStatus("Error downloading search results.");
                        Logging.LogMessageToFile("Error downloading search results: " + ex.Message, 2);
                        failedDownload = true;
                        restartTimer(breakBetweenChange);
                        return;
                    }
                }
                try
                {
                    if (jsonData.Length == 0)
                    {
                        updateStatus("Subreddit Probably Doesn't Exist");
                        Logging.LogMessageToFile("Subreddit probably does not exist.", 1);
                        ++noResultCount;
                        failedDownload = true;
                        restartTimer(breakBetweenChange);
                        return;
                    }
                    JToken redditResult;
                    if (wallpaperGrabType == 9)
                    {
                        redditResult = JToken.Parse(jsonData);
                        redditResult = (JToken.Parse(redditResult.First.ToString())["data"]["children"]);
                    }
                    else
                    {
                        redditResult = JToken.Parse(jsonData)["data"]["children"];
                    }
                    if ((!failedDownload) || (!(redditResult.ToString().Length < 3)))
                    {
                        JToken token = null;
                        try
                        {

                            //IEnumerable<JToken> redditResultReversed = redditResult.Reverse();
                            //foreach (JToken toke in redditResultReversed)
                            //{
                            //    // if (!historyRepeated.Contains(toke["data"]["id"].ToString()))
                            //    // {
                            //    token = toke;
                            //    // }asd
                            //}

                            bool needsChange = false;
                            if (token == null)
                            {
                                if (redditResult.Count() == 0)
                                {
                                    ++noResultCount;
                                    updateStatus("No results found, searching again.");
                                    Logging.LogMessageToFile("No search results, trying to change wallpaper again.", 0);
                                    needsChange = true;
                                    changeWallpaper();
                                }
                                else
                                {
                                    // historyRepeated.Clear();
                                    int randIndex = r.Next(0, redditResult.Count() - 1);
                                    token = redditResult.ElementAt(randIndex);
                                }
                            }
                            if (!needsChange)
                            {
                                if (wallpaperGrabType != 0)
                                {
                                    currentThread = "http://reddit.com" + token["data"]["permalink"].ToString();
                                    Logging.LogMessageToFile("Found a wallpaper! Title: " + token["data"]["title"].ToString() + ", URL: " + token["data"]["url"].ToString() + ", ThreadID: " + token["data"]["id"].ToString(), 0);

                                    // check URL 
                                    if (Validation.checkImg(token["data"]["url"].ToString()))
                                    {
                                        if (Validation.checkImgur(token["data"]["url"].ToString()))
                                        {
                                            setWallpaper(token["data"]["url"].ToString(), token["data"]["title"].ToString(), token["data"]["id"].ToString());
                                        }
                                        else
                                        {
                                            updateStatus("Wallpaper has been removed from Imgur.");
                                            Logging.LogMessageToFile("The selected wallpaper was deleted from Imgur, searching again.", 1);
                                            ++noResultCount;
                                            restartTimer(breakBetweenChange);
                                            changeWallpaper();
                                        }
                                    }
                                    else
                                    {
                                        updateStatus("The selected URL is not for an image.");
                                        Logging.LogMessageToFile("Not a direct wallpaper URL, searching again.", 1);
                                        ++noResultCount;
                                        restartTimer(breakBetweenChange);
                                        return;
                                    }
                                }
                                else
                                {
                                    token = redditResult.ElementAt(random.Next(0, redditResult.Count() - 1));
                                    currentThread = "http://reddit.com" + token["data"]["permalink"].ToString();
                                    Logging.LogMessageToFile("Found a wallpaper! Title: " + token["data"]["title"].ToString() + ", URL: " + token["data"]["url"].ToString() + ", ThreadID: " + token["data"]["id"].ToString(), 0);

                                    // check URL 
                                    if (Validation.checkImg(token["data"]["url"].ToString()))
                                    {
                                        if (Validation.checkImgur(token["data"]["url"].ToString()))
                                        {
                                            setWallpaper(token["data"]["url"].ToString(), token["data"]["title"].ToString(), token["data"]["id"].ToString());
                                        }
                                        else
                                        {
                                            updateStatus("Wallpaper has been removed from Imgur.");
                                            Logging.LogMessageToFile("The selected wallpaper was deleted from Imgur, searching again.", 1);
                                            ++noResultCount;
                                            restartTimer(breakBetweenChange);
                                            changeWallpaper();
                                        }
                                    }
                                    else
                                    {
                                        updateStatus("The selected URL is not for an image.");
                                        Logging.LogMessageToFile("Not a direct wallpaper URL, searching again.", 1);
                                        ++noResultCount;
                                        restartTimer(breakBetweenChange);
                                        return;
                                    }
                                } 
                            }
                        }
                        catch (System.InvalidOperationException)
                        {
                            updateStatus("Your search query is bringing up no results.");
                            Logging.LogMessageToFile("No results from the search query.", 1);
                            failedDownload = true;
                            restartTimer(breakBetweenChange);
                        }
                    }
                    else
                    {
                        restartTimer(breakBetweenChange);
                    }
                }
                catch (JsonReaderException ex)
                {
                    updateStatus("Unexpected error: " + ex.Message);
                    Logging.LogMessageToFile("Unexpected error: " + ex.Message, 2);
                    restartTimer(breakBetweenChange);
                }
            };

            bw.RunWorkerAsync();
        }
        delegate void SetTextCallback(string text);
#endregion

        //======================================================================
        // Update status
        //======================================================================
        private void updateStatus(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.statuslabel.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(updateStatus);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.statuslabel1.Text = text;
            }
        }

        //======================================================================
        // Restart Timer from BackgroundWorker
        //======================================================================
        private void restartTimer(System.Windows.Forms.Timer timer)
        {
            this.Invoke((MethodInvoker)(() => { timer.Enabled = true; }));
        }

        //======================================================================
        // Set the wallpaper
        //======================================================================
        #region setWallpaper
        private void setWallpaper(string url, string title, string threadID)
        {
            Logging.LogMessageToFile("Setting wallpaper.", 0);

            if (database.checkForEntry(url))
            {
                updateStatus("Wallpaper is blacklisted.");
                Logging.LogMessageToFile("The selected wallpaper has been blacklisted, searching again.", 1);
                ++noResultCount;
                changeWallpaperTimer.Enabled = false;
                changeWallpaper();
                return;
            }

            if (Properties.Settings.Default.manualOverride == false)
            {
                if (Properties.Settings.Default.suppressDuplicates == true)
                {
                    if (historyList.Contains(threadID))
                    {
                        updateStatus("Wallpaper already used this session.");
                        Logging.LogMessageToFile("The selected wallpaper has already been used this session, searching again.", 1);
                        ++noResultCount;
                        changeWallpaperTimer.Enabled = false;
                        changeWallpaper();
                        return;
                    }
                }
            }

            Properties.Settings.Default.manualOverride = false;
            Properties.Settings.Default.Save();

            var bw = new BackgroundWorker();
            bw.DoWork += delegate
            {
                Uri uri2 = new Uri(url);
                string extention2 = System.IO.Path.GetExtension(uri2.LocalPath);

                // historyMenuStrip.Hide();
                BeginInvoke((MethodInvoker)delegate
                {
                    updateStatus("Setting Wallpaper");

                });
                string url2 = url.ToLower();
                if (url.Equals(null) || url.Length.Equals(0))
                {
                    restartTimer(changeWallpaperTimer);
                }
                else
                {
                    if (url2.Contains("imgur.com/a/"))
                    {
                        string jsonresult;
                        string imgurid = url.Replace("https://", "").Replace("http://", "").Replace("imgur.com/a/", "").Replace("//", "").Replace("/", "");
                        var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://api.imgur.com/3/album/" + imgurid);
                        httpWebRequest.ContentType = "application/json";
                        httpWebRequest.Accept = "*/*";
                        httpWebRequest.Method = "GET";
                        httpWebRequest.Headers.Add("Authorization", "Client-ID 355f2ab533c2ac7");

                        var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            jsonresult = streamReader.ReadToEnd();

                        }
                        JToken imgurResult = JToken.Parse(jsonresult)["data"]["images"];
                        int i = imgurResult.Count();
                        int selc = 0;
                        if (i - 1 != 0)
                        {
                            selc = r.Next(0, i - 1);

                        }
                        JToken img = imgurResult.ElementAt(selc);
                        url = img["link"].ToString();
                    }
                    else if (!ImageExtensions.Contains(extention2.ToUpper()) && (url2.Contains("deviantart")))
                    {
                        string jsonresult;
                        var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://backend.deviantart.com/oembed?url=" + url);
                        httpWebRequest.ContentType = "application/json";
                        httpWebRequest.Accept = "*/*";
                        httpWebRequest.Method = "GET";

                        var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            jsonresult = streamReader.ReadToEnd();

                        }
                        JToken imgResult = JToken.Parse(jsonresult);
                        url = imgResult["url"].ToString();

                    }
                    else if (!ImageExtensions.Contains(extention2.ToUpper()) && (url2.Contains("imgur.com")))
                    {
                        string jsonresult;
                        string imgurid = url.Replace("https://", "").Replace("http://", "").Replace("imgur.com/", "").Replace("//", "").Replace("/", "");
                        var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://api.imgur.com/3/image/" + imgurid);
                        httpWebRequest.ContentType = "application/json";
                        httpWebRequest.Accept = "*/*";
                        httpWebRequest.Method = "GET";
                        httpWebRequest.Headers.Add("Authorization", "Client-ID 355f2ab533c2ac7");

                        var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            jsonresult = streamReader.ReadToEnd();

                        }
                        JToken imgResult = JToken.Parse(jsonresult);
                        url = imgResult["data"]["link"].ToString();
                    }
                    Uri uri = new Uri(url);
                    string extention = System.IO.Path.GetExtension(uri.LocalPath);
                    string filename = threadID + extention;
                    string wallpaperFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), filename);
                    Properties.Settings.Default.currentWallpaperFile = wallpaperFile;
                    Properties.Settings.Default.url = url;
                    Properties.Settings.Default.threadTitle = title;
                    Properties.Settings.Default.currentWallpaperUrl = url;
                    Properties.Settings.Default.currentWallpaperName = title + extention;
                    Properties.Settings.Default.threadID = threadID;
                    Properties.Settings.Default.Save();

                    Logging.LogMessageToFile("URL: " + url, 0);
                    Logging.LogMessageToFile("Title: " + title, 0);
                    Logging.LogMessageToFile("Thread ID: " + threadID, 0);

                    if (ImageExtensions.Contains(extention.ToUpper()))
                    {
                        if (System.IO.File.Exists(wallpaperFile))
                        {
                            try
                            {
                                System.IO.File.Delete(wallpaperFile);
                            }
                            catch (System.IO.IOException Ex)
                            {
                                Logging.LogMessageToFile("Unexpected error deleting old wallpaper: " + Ex.Message, 1);

                            }
                        }
                        try
                        {
                            using (WebClient wc = Proxy.setProxy())
                            {

                                wc.DownloadFile(uri.AbsoluteUri, @wallpaperFile);

                                if (Properties.Settings.Default.fitWallpaper == true)
                                {
                                    string screenWidth = SystemInformation.VirtualScreen.Width.ToString();
                                    string screenHeight = SystemInformation.VirtualScreen.Height.ToString();

                                    var img = Bitmap.FromFile(wallpaperFile);
                                    string wallpaperWidth = img.Width.ToString();
                                    string wallpaperHeight = img.Height.ToString();

                                    if (!screenWidth.Equals(wallpaperWidth) || !screenHeight.Equals(wallpaperHeight))
                                    {
                                        Logging.LogMessageToFile("Wallpaper size mismatch. Screen: " + screenWidth + "x" + screenHeight + ", Wallpaper: " + wallpaperWidth + "x" + wallpaperHeight, 1);
                                        updateStatus("Wallpaper resolution mismatch.");
                                        ++noResultCount;
                                        restartTimer(breakBetweenChange);
                                        changeWallpaper();
                                        return;
                                    }

                                }

                                if (Properties.Settings.Default.wallpaperFade == true)
                                {
                                    Logging.LogMessageToFile("Applying wallpaper using Active Desktop.", 0);
                                    ActiveDesktop();

                                    ThreadStart threadStarter = () =>
                                    {
                                        Reddit_Wallpaper_Changer.ActiveDesktop.IActiveDesktop _activeDesktop = Reddit_Wallpaper_Changer.ActiveDesktop.ActiveDesktopWrapper.GetActiveDesktop();
                                        _activeDesktop.SetWallpaper(wallpaperFile, 0);
                                        _activeDesktop.ApplyChanges(Reddit_Wallpaper_Changer.ActiveDesktop.AD_Apply.ALL | Reddit_Wallpaper_Changer.ActiveDesktop.AD_Apply.FORCE);

                                        Marshal.ReleaseComObject(_activeDesktop);
                                    };
                                    Thread thread = new Thread(threadStarter);
                                    thread.SetApartmentState(ApartmentState.STA);
                                    thread.Start();
                                    thread.Join(2000);
                                }
                                else
                                {
                                    Logging.LogMessageToFile("Applying wallpaper using standard process.", 0);
                                    Reddit_Wallpaper_Changer.ActiveDesktop.SystemParametersInfo(Reddit_Wallpaper_Changer.ActiveDesktop.SPI_SETDESKWALLPAPER, 0, @wallpaperFile, Reddit_Wallpaper_Changer.ActiveDesktop.SPIF_UPDATEINIFILE | Reddit_Wallpaper_Changer.ActiveDesktop.SPIF_SENDWININICHANGE);

                                }
                            }

                            noResultCount = 0;
                            BeginInvoke((MethodInvoker)delegate
                            {
                                updateStatus("Wallpaper Changed!");
                            });

                            Logging.LogMessageToFile("Wallpaper changed!", 0);
                            historyList.Add(threadID);

                            database.historyWallpaper(url, title, threadID);
                            this.Invoke((MethodInvoker)(() => { buildThumbnailCache(); }));
                            this.Invoke((MethodInvoker)(() => { var done = populateHistory(); }));


                            if (Properties.Settings.Default.disableNotifications == false && Properties.Settings.Default.wallpaperInfoPopup == true)
                            {
                                int formx = 300;
                                int formy = 90;

                                int screenx = Screen.PrimaryScreen.Bounds.Width;
                                int screeny = Screen.PrimaryScreen.Bounds.Height;

                                int popupx = screenx - formx - 50;
                                int popupy = screeny - formy - 50;

                                BeginInvoke((MethodInvoker)delegate
                                {
                                    PopupInfo popup = new PopupInfo(threadID, title);
                                    popup.Location = new Point(popupx, popupy);
                                    popup.Show();
                                });
                            }

                            if (Properties.Settings.Default.autoSave == true)
                            {
                                savewallpaper.saveCurrentWallpaper(Properties.Settings.Default.currentWallpaperName);
                            }
                        }
                        catch (System.Net.WebException Ex)
                        {
                            Logging.LogMessageToFile("Unexpected Error: " + Ex.Message, 2);

                        }
                    }
                    else
                    {
                        Logging.LogMessageToFile("Wallpaper URL failed validation: " + extention.ToUpper(), 1);
                        restartTimer(changeWallpaperTimer);
                    }

                }

                using (WebClient wc = Proxy.setProxy())
                {
                    byte[] bytes = wc.DownloadData(url);

                    if (bytes.Count().Equals(0))
                    {
                        restartTimer(changeWallpaperTimer);
                    }
                    else
                    {
                        try
                        {
                            if (currentWallpaper != null)
                            {
                                currentWallpaper.Dispose();    
                            }

                            // database.historyWallpaper(url, title, threadID);
                            // buildThumbnailCache();
                            // populateHistory();

                            // this.Invoke((MethodInvoker)(() => { database.historyWallpaper(url, title, threadID); }));
                            // this.Invoke((MethodInvoker)(() => { buildThumbnailCache(); }));
                            // this.Invoke((MethodInvoker)(() => { var done = populateHistory(); }));

                            // this.Invoke((MethodInvoker)(() => { var done = populateHistory(); }));

                        }
                        catch (ArgumentException Ex)
                        {
                            Logging.LogMessageToFile("Unexpected Error: " + Ex.Message, 2);

                            // database.historyWallpaper(url, title, threadID);
                            // buildThumbnailCache();
                            // populateHistory();

                            //this.Invoke((MethodInvoker)(() => { database.historyWallpaper(url, title, threadID); }));
                            //this.Invoke((MethodInvoker)(() => { buildThumbnailCache(); }));
                            //this.Invoke((MethodInvoker)(() => { populateHistory(); }));
                            // this.Invoke((MethodInvoker)(() => { var done = populateHistory(); }));

                            restartTimer(breakBetweenChange);
                        }
                    }
                }
            };

            bw.RunWorkerAsync();
        }

        #endregion

        delegate void SetGridCallback();


        //======================================================================
        // Set grid for History menu
        //======================================================================
        private void SetGrid()
        {
            if (this.historyDataGrid.InvokeRequired)
            {
                SetGridCallback d = new SetGridCallback(SetGrid);
                this.Invoke(d, new object[] { });
            }
            else
            {
                populateHistory();
            }
        }

        //======================================================================
        // Form load screen
        //======================================================================
        private void RWC_Shown(object sender, EventArgs e)
        {
            if (chkStartInTray.Checked)
            {
                fakeClose(false);
            }
        }

        //======================================================================
        // Send to system tray
        //======================================================================
        private void fakeClose(bool p)
        {
            this.Visible = false;
            if(p)
            {
                if (Properties.Settings.Default.disableNotifications == false && !dissmissedOnce)
                {
                    taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                    taskIcon.BalloonTipTitle = "Reddit Wallpaper Changer";
                    taskIcon.BalloonTipText = "Down here if you need me!";
                    taskIcon.ShowBalloonTip(700);
                }
            }

            dissmissedOnce = true;
        }

        //======================================================================
        // Configure run on startup 
        //======================================================================
        private void startup(bool add)
        {
            try {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (add)
                {
                    //Surround path with " " to make sure that there are no problems
                    //if path contains spaces.
                    key.SetValue("RWC", "\"" + Application.ExecutablePath + "\"");
                }
                else
                    key.DeleteValue("RWC");

                key.Close();
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Error setting RWC to load on startup: " + ex.Message, 2);
            }
        }

        //======================================================================
        // Closing the form
        //======================================================================
        private void RWC_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!realClose)
            {
                e.Cancel = true;
                fakeClose(true);
            }
            else
            {

            }
        }

        //======================================================================
        // Restore from system tray
        //======================================================================
        private void taskIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Visible = true;
        }

        //======================================================================
        // Settings selected from the menu
        //======================================================================
        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Visible = true;
        }

        //======================================================================
        // Exit selected form the menu
        //======================================================================
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            wallpaperCleanup();
            Logging.LogMessageToFile("Exiting Reddit Wallpaper Changer.", 0);
            realClose = true;
            wallpaperChangeTimer.Enabled = false;
            changeWallpaperTimer.Enabled = false;
            Logging.LogMessageToFile("Reddit Wallpaper Changer is shutting down.", 0);
            database.disconnectFromDatabase();
            Application.Exit();
        }

        //======================================================================
        // Running selected from the menu
        //======================================================================
        private void statusMenuItem1_Click(object sender, EventArgs e)
        {
            statusMenuItem1.Checked = !statusMenuItem1.Checked;
            wallpaperChangeTimer.Enabled = statusMenuItem1.Checked;

            if (statusMenuItem1.Checked)
            {
                statusMenuItem1.ForeColor = Color.ForestGreen;
                statusMenuItem1.Text = "Running";
                Logging.LogMessageToFile("Running.", 0);
            }
            else
            {
                statusMenuItem1.ForeColor = Color.Red;
                statusMenuItem1.Text = "Not Running";
                Logging.LogMessageToFile("Not Running.", 0);
            }
        }

        private void currentThreadMenuItem1_Click(object sender, EventArgs e)
        {
            if (currentThread != null)
            {
                System.Diagnostics.Process.Start(currentThread);
            }
        }

        //======================================================================
        // Change wallpaper selected from the menu
        //======================================================================
        private void changeWallpaperMenuItem_Click(object sender, EventArgs e)
        {
            wallpaperChangeTimer.Enabled = false;
            wallpaperChangeTimer.Enabled = true;
            changeWallpaperTimer.Enabled = true;
        }

        //======================================================================
        // Startup time for update check
        //======================================================================
        private void startupTimer_Tick(object sender, EventArgs e)
        {
            startupTimer.Enabled = false;
            using (WebClient wc = Proxy.setProxy())
            {

                try
                {
                    if (Properties.Settings.Default.autoUpdateCheck == true)
                    {
                        String latestVersion = wc.DownloadString("https://raw.githubusercontent.com/Rawns/Reddit-Wallpaper-Changer/master/version");
                        if (!latestVersion.Contains(currentVersion.Trim().ToString()))
                        {
                            Form Update = new Update(latestVersion, this);
                            Update.Show();
                        }
                        else
                        {
                            changeWallpaperTimer.Enabled = true;
                        }
                    }
                    else
                    {
                        changeWallpaperTimer.Enabled = true;
                    }
                }
                catch (Exception ex)
                {
                    if (Properties.Settings.Default.disableNotifications == false)
                    {
                        taskIcon.BalloonTipIcon = ToolTipIcon.Error;
                        taskIcon.BalloonTipTitle = "Reddit Wallpaper Changer!";
                        taskIcon.BalloonTipText = "Error checking for updates.";
                        taskIcon.ShowBalloonTip(750);
                    }
                    Logging.LogMessageToFile("Error checking for updates: " + ex.Message, 0);
                }
            }
        }


        //======================================================================
        // Enable change wallpaper timer
        //======================================================================
        public void changeWallpaperTimerEnabled()
        {
            changeWallpaperTimer.Enabled = true;
        }

        //======================================================================
        // Trigger wallpaper change
        //======================================================================
        private void changeWallpaperTimer_Tick(object sender, EventArgs e)
        {

            changeWallpaperTimer.Enabled = false;
            changeWallpaper();
        }

        //======================================================================
        // Open thread from blacklist selection click
        //======================================================================
        private void blacklistDataGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            int rowIndex = e.RowIndex;
            try
            {

                System.Diagnostics.Process.Start("http://reddit.com/" + blacklistDataGrid.Rows[e.RowIndex].Cells[2].Value.ToString());
            }
            catch
            {

            }
        }

        //======================================================================
        // Open thread from history selection click
        //======================================================================
        private void historyDataGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            int rowIndex = e.RowIndex;
            try
            {
                System.Diagnostics.Process.Start("http://reddit.com/" + historyDataGrid.Rows[e.RowIndex].Cells[2].Value.ToString());
            }
            catch
            {

            }
        }

        //======================================================================
        // Open thread from favourites selection click
        //======================================================================
        private void favouritesDataGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            int rowIndex = e.RowIndex;
            try
            {
                System.Diagnostics.Process.Start("http://reddit.com/" + favouritesDataGrid.Rows[e.RowIndex].Cells[2].Value.ToString());
            }
            catch
            {

            }
        }

        //======================================================================
        // Save current wallpaper locally
        //======================================================================
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            String fileName = Properties.Settings.Default.currentWallpaperName;
            if (savewallpaper.saveCurrentWallpaper(fileName))
            {
                if (Properties.Settings.Default.disableNotifications == false)
                {
                    taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                    taskIcon.BalloonTipTitle = "Wallpaper Saved!";
                    taskIcon.BalloonTipText = "Wallpaper saved to " + Properties.Settings.Default.defaultSaveLocation;
                    taskIcon.ShowBalloonTip(750);
                }

                updateStatus("Wallpaper saved!");
            }
            else
            {
                if (Properties.Settings.Default.disableNotifications == false)
                {
                    taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                    taskIcon.BalloonTipTitle = "Already Saved!";
                    taskIcon.BalloonTipText = "No need to save this wallpaper as it already exists in your wallpapers folder! :)";
                    taskIcon.ShowBalloonTip(750);
                }
                updateStatus("Wallpaper already saved!");
            }
        }

        //======================================================================
        // Save the current wallpaper
        //======================================================================
        private void saveWallpaper_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                string fileName = saveWallpaper.FileName;
                currentWallpaper.Save(fileName);
            }
            catch(Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error: " + ex.Message, 2);
            }
        }

        //======================================================================
        // Test internet connection
        //======================================================================
        private void checkInternetTimer_Tick(object sender, EventArgs e)
        {
            noticeLabel.Text = "Checking Internet Connection...";
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                checkInternetTimer.Enabled = false;
                updateTimer();
                startupTimer.Enabled = true;
                Logging.LogMessageToFile("Internet is working.", 0);
            }
            else
            {
                updateStatus("Network Unavaliable. Rechecking.");
                Logging.LogMessageToFile("Network Unavaliable. Rechecking.", 1);
            }
        }

        //======================================================================
        // Change subreddit text box
        //======================================================================
        private void subredditTextBox_TextChanged(object sender, EventArgs e)
        {
            if (subredditTextBox.Text.Contains("/m/"))
            {
                label5.Text = "MultiReddit";
                label5.ForeColor = Color.Red;
            }
            else
            {
                label5.Text = "Subreddit(s):";
                label5.ForeColor = Color.Black;
            }
        }

        private void searchQuery_TextChanged(object sender, EventArgs e)
        {
            searchQueryValue = searchQuery.Text;
        }

        //======================================================================
        // Show the search wizard form
        //======================================================================
        private void searchWizardButton_Click(object sender, EventArgs e)
        {
            Form searchWizard = new SearchWizard(this);
            searchWizard.Show();
        }

        //======================================================================
        // One second break after setting a wallpaper. Once passed, this method is trigered
        //======================================================================
        private void breakBetweenChange_Tick(object sender, EventArgs e)
        {
            breakBetweenChange.Enabled = false;
            changeWallpaperTimer.Enabled = true;
        }

        //======================================================================
        // Add a button for each attached monitor 
        //======================================================================
        public void monitorPanel_Paint()
        {
            // Create list of controls and then remove them all
            List<Control> controlsToRemove = new List<Control>();
            foreach (Control item in monitorLayoutPanel.Controls.OfType<PictureBox>())
            {
                controlsToRemove.Add(item);
            }
            
            foreach (Control item in monitorLayoutPanel.Controls.OfType<Label>())
            {
                controlsToRemove.Add(item);
            }

            foreach (Control item in controlsToRemove)
            {
                monitorLayoutPanel.Controls.Remove(item);
                item.Dispose();
            }

            // Get number of attached monitors
            int screens = Screen.AllScreens.Count();

            // Auto add a table to nest the monitor images and labels
            this.monitorLayoutPanel.Refresh();
            this.monitorLayoutPanel.ColumnStyles.Clear();
            this.monitorLayoutPanel.RowStyles.Clear();
            this.monitorLayoutPanel.ColumnCount = screens;
            this.monitorLayoutPanel.RowCount = 2;
            this.monitorLayoutPanel.AutoSize = true;

            int z = 0;
            foreach (var screen in Screen.AllScreens.OrderBy(i => i.Bounds.X))
            {                  

                var percent = 100f / screens;
                this.monitorLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, percent));

                var monitorImg = Properties.Resources.display_enabled;
                int x = 100;
                int y = 75;

                if (screens >= 4)
                {
                    monitorImg = Properties.Resources.display_enabled_small;
                    x = 64;
                    y = 64;
                }

                PictureBox monitor = new PictureBox
                {
                    Name = "MonitorPic" + z,
                    Size = new Size(x, y),
                    BackgroundImageLayout = ImageLayout.Stretch,
                    BackgroundImage = monitorImg,
                    Anchor = System.Windows.Forms.AnchorStyles.None,                    
                };

                Label resolution = new Label
                {
                    Name = "MonitorLabel" + z,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.Black,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Text = "DISPLAY " + z + "\r\n" + screen.Bounds.Width + "x" + screen.Bounds.Height,
                    Anchor = System.Windows.Forms.AnchorStyles.None,
                };

                this.monitorLayoutPanel.Controls.Add(monitor, z, 0);
                this.monitorLayoutPanel.Controls.Add(resolution, z, 1);

                z++;    
            }

            comboType.Text = Properties.Settings.Default.wallpaperStyle;
            SetExample();
        }


        //======================================================================
        // Set the example wallpaper image
        //======================================================================
        public void SetExample()
        {
            if (comboType.Text == "Fill")
            {
                picStyles.Image = Properties.Resources.fill;
            }
            else if (comboType.Text == "Fit")
            {
                picStyles.Image = Properties.Resources.fit;
            }
            else if (comboType.Text == "Span")
            {
                picStyles.Image = Properties.Resources.span;
            }
            else if (comboType.Text == "Stretch")
            {
                picStyles.Image = Properties.Resources.stretch;
            }
            else if (comboType.Text == "Tile")
            {
                picStyles.Image = Properties.Resources.tile;
            }
            else if (comboType.Text == "Center")
            {
                picStyles.Image = Properties.Resources.centre;
            }
        }

        //======================================================================
        // History grid mouse click
        //======================================================================
        private void historyDataGrid_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                currentMouseOverRow = historyDataGrid.HitTest(e.X, e.Y).RowIndex;
                if (currentMouseOverRow >= 0)
                {
                    historyMenuStrip.Show(historyDataGrid, new Point(e.X, e.Y));
                }
                else
                {
                    contextMenuStrip.Show(historyDataGrid, new Point(e.X, e.Y));
                }
            }
        }

        //======================================================================
        // Blacklist grid mouse click
        //======================================================================
        private void blacklistDataGrid_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                currentMouseOverRow = blacklistDataGrid.HitTest(e.X, e.Y).RowIndex;
                if (currentMouseOverRow >= 0)
                {
                    blacklistMenuStrip.Show(blacklistDataGrid, new Point(e.X, e.Y));
                }
                else
                {
                    contextMenuStrip.Show(blacklistDataGrid, new Point(e.X, e.Y));
                }
            }
        }

        //======================================================================
        // History grid mouse click
        //======================================================================
        private void favouritesDataGrid_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                currentMouseOverRow = historyDataGrid.HitTest(e.X, e.Y).RowIndex;
                if (currentMouseOverRow >= 0)
                {
                    favouritesMenuStrip.Show(historyDataGrid, new Point(e.X, e.Y));
                }
                else
                {
                    contextMenuStrip.Show(historyDataGrid, new Point(e.X, e.Y));
                }
            }
        }

        //======================================================================
        // Truly random searching
        //======================================================================
        private void wallpaperGrabType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (wallpaperGrabType.Text.Equals("Truly Random"))
            {
                this.label2.Visible = false;
                this.searchQuery.Visible = false;
                this.label9.Visible = true;
            }
            else
            {
                if (!label2.Visible)
                {
                    this.label2.Visible = true;
                    this.searchQuery.Visible = true;
                    this.label9.Visible = false;
                }
            }
        }

        //======================================================================
        // Code for enabeling/disabeling proxy credentials
        //======================================================================
        private void chkAuth_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAuth.Checked == true)
            {
                this.txtUser.Enabled = true;
                this.txtUser.Text = Properties.Settings.Default.proxyUser;
                this.txtPass.Enabled = true;
                this.txtPass.Text = Properties.Settings.Default.proxyPass;
            }
            else
            {
                this.txtUser.Enabled = false;
                this.txtUser.Text = "";
                this.txtPass.Enabled = false;
                this.txtPass.Text = "";
            }
        }

        //======================================================================
        // Code for enabeling/disabeling proxy
        //======================================================================
        private void chkProxy_CheckedChanged(object sender, EventArgs e)
        {
            if (this.chkProxy.Checked == true)
            {
                this.txtProxyServer.Enabled = true;
                this.txtProxyServer.Text = Properties.Settings.Default.proxyAddress;
                this.chkAuth.Enabled = true;
            }
            else
            {
                this.txtProxyServer.Enabled = false;
                this.txtProxyServer.Text = "";
                this.chkAuth.Enabled = false;
                this.chkAuth.Checked = false;
                this.txtUser.Enabled = false;
                this.txtUser.Text = "";
                this.txtPass.Enabled = false;
                this.txtPass.Text = "";

            }
        }

        //======================================================================
        // Open Rawns profile page on Reddit
        //======================================================================
        private void rawnsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.reddit.com/user/Rawns/");
        }

        //======================================================================
        // Set default location for manually saved wallpapers
        //======================================================================
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            folderBrowser.Description = "Select a location to save wallpapers:";
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                txtSavePath.Text = folderBrowser.SelectedPath;
            }
        }

        //======================================================================
        // Add wallpaper to Favourites
        //======================================================================
        public async void addToFavourites(string url, string title, string threadid)
        {
            database.faveWallpaper(url, title, threadid);

            if (Properties.Settings.Default.disableNotifications == false)
            {
                taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                taskIcon.BalloonTipTitle = "Wallpaper Favourited!";
                taskIcon.BalloonTipText = "The Wallpaper has been added to your favourites!";
                taskIcon.ShowBalloonTip(750);
            }

            populateFavourites();

            if (Properties.Settings.Default.autoSaveFaves == true)
            {
                if (savewallpaper.saveSelectedWallpaper(url, threadid, title))
                {
                    if (Properties.Settings.Default.disableNotifications == false)
                    {
                        taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                        taskIcon.BalloonTipTitle = "Wallpaper Saved!";
                        taskIcon.BalloonTipText = "Wallpaper saved to " + Properties.Settings.Default.defaultSaveLocation;
                        taskIcon.ShowBalloonTip(750);
                    }

                    updateStatus("Wallpaper saved!");

                }
                else
                {
                    if (Properties.Settings.Default.disableNotifications == false)
                    {
                        taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                        taskIcon.BalloonTipTitle = "Already Saved!";
                        taskIcon.BalloonTipText = "No need to save this wallpaper as it already exists in your wallpapers folder! :)";
                        taskIcon.ShowBalloonTip(750);
                    }
                    updateStatus("Wallpaper already saved!");
                }
            }
        }

        //======================================================================
        // Click on favourite menu
        //======================================================================
        private async void faveWallpaperMenuItem_Click(object sender, EventArgs e)
        {
            string url = Properties.Settings.Default.url;
            string title = Properties.Settings.Default.threadTitle;
            string threadid = Properties.Settings.Default.threadID;

            database.faveWallpaper(url, title, threadid);

            if (Properties.Settings.Default.disableNotifications == false)
            {
                taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                taskIcon.BalloonTipTitle = "Wallpaper Favourited!";
                taskIcon.BalloonTipText = "Wallpaper added to favourites!";
                taskIcon.ShowBalloonTip(750);
            }

            populateFavourites();

            if (Properties.Settings.Default.autoSaveFaves == true)
            {
                if (savewallpaper.saveSelectedWallpaper(url, threadid, title))
                {
                    if (Properties.Settings.Default.disableNotifications == false)
                    {
                        taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                        taskIcon.BalloonTipTitle = "Wallpaper Saved!";
                        taskIcon.BalloonTipText = "Wallpaper saved to " + Properties.Settings.Default.defaultSaveLocation;
                        taskIcon.ShowBalloonTip(750);
                    }

                    updateStatus("Wallpaper saved!");

                }
                else
                {
                    if (Properties.Settings.Default.disableNotifications == false)
                    {
                        taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                        taskIcon.BalloonTipTitle = "Already Saved!";
                        taskIcon.BalloonTipText = "No need to save this wallpaper as it already exists in your wallpapers folder! :)";
                        taskIcon.ShowBalloonTip(750);
                    }
                    updateStatus("Wallpaper already saved!");
                }
            }
        }

        //======================================================================
        // Blacklist the current wallpaper
        //======================================================================
        public async void blacklistWallpaperMenuItem_Click(object sender, EventArgs e)
        {
            string url = Properties.Settings.Default.url;
            string title = Properties.Settings.Default.threadTitle;
            string threadid = Properties.Settings.Default.threadID;

            var blacklist = await database.blacklistWallpaper(url, title, threadid);
            
            if (Properties.Settings.Default.disableNotifications == false)
            {
                if (blacklist == true)
                {
                    taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                    taskIcon.BalloonTipTitle = "Wallpaper Blacklisted!";
                    taskIcon.BalloonTipText = "The wallpaper has been blacklisted! Finding a new wallpaper...";
                    taskIcon.ShowBalloonTip(750);
                }
                else
                {
                    taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                    taskIcon.BalloonTipTitle = "Error Blacklisting!";
                    taskIcon.BalloonTipText = "There was an error blacklisting the wallpaper!";
                    taskIcon.ShowBalloonTip(750);
                }
            }

            wallpaperChangeTimer.Enabled = false;
            wallpaperChangeTimer.Enabled = true;
            changeWallpaperTimer.Enabled = true;

            populateBlacklist();
        }

        //======================================================================
        // Blacklist wallpaper from History panel
        //======================================================================
        private async void blacklistWallpapertoolStripMenuItem_Click(object sender, EventArgs e)
        {
            string title = (historyDataGrid.Rows[currentMouseOverRow].Cells[1].Value.ToString());
            string threadid = (historyDataGrid.Rows[currentMouseOverRow].Cells[2].Value.ToString());
            string url = (historyDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString());

            var blacklist = await database.blacklistWallpaper(url, title, threadid);

            if (Properties.Settings.Default.disableNotifications == false)
            {
                if (blacklist == true)
                { 
                    taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                    taskIcon.BalloonTipTitle = "Wallpaper Blacklisted!";
                    taskIcon.BalloonTipText = "The wallpaper has been blacklisted!";
                    taskIcon.ShowBalloonTip(750);
                }
                else
                {
                    taskIcon.BalloonTipIcon = ToolTipIcon.Error;
                    taskIcon.BalloonTipTitle = "Error!";
                    taskIcon.BalloonTipText = "There was an error adding the wallpaper to your blacklist!";
                    taskIcon.ShowBalloonTip(750);
                }
            }

            if (url == Properties.Settings.Default.currentWallpaperUrl)
            {
                wallpaperChangeTimer.Enabled = false;
                wallpaperChangeTimer.Enabled = true;
                changeWallpaperTimer.Enabled = true;
            }

            populateBlacklist();
        }

        //======================================================================
        // Favourite wallpaper from the History Panel
        //======================================================================
        private async void favouriteThisWallpaperToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string title = (historyDataGrid.Rows[currentMouseOverRow].Cells[1].Value.ToString());
            string threadid = (historyDataGrid.Rows[currentMouseOverRow].Cells[2].Value.ToString());
            string url = (historyDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString());

            var favourite = await database.faveWallpaper(url, title, threadid);

            if (Properties.Settings.Default.disableNotifications == false)
            {
                if (favourite == true)
                {
                    taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                    taskIcon.BalloonTipTitle = "Wallpaper Favourited!";
                    taskIcon.BalloonTipText = "Wallpaper added to favourites!";
                    taskIcon.ShowBalloonTip(750);
                }
                else
                {
                    taskIcon.BalloonTipIcon = ToolTipIcon.Error;
                    taskIcon.BalloonTipTitle = "Error!";
                    taskIcon.BalloonTipText = "There was an error adding the Wallpaper to your favourites!";
                    taskIcon.ShowBalloonTip(750);
                }
            }

            populateFavourites();

            if (Properties.Settings.Default.autoSaveFaves == true)
            {
                if (savewallpaper.saveSelectedWallpaper(url, threadid, title))
                {
                    if (Properties.Settings.Default.disableNotifications == false)
                    {
                        taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                        taskIcon.BalloonTipTitle = "Wallpaper Saved!";
                        taskIcon.BalloonTipText = "Wallpaper saved to " + Properties.Settings.Default.defaultSaveLocation;
                        taskIcon.ShowBalloonTip(750);
                    }

                    updateStatus("Wallpaper saved!");

                }
                else
                {
                    if (Properties.Settings.Default.disableNotifications == false)
                    {
                        taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                        taskIcon.BalloonTipTitle = "Already Saved!";
                        taskIcon.BalloonTipText = "No need to save this wallpaper as it already exists in your wallpapers folder! :)";
                        taskIcon.ShowBalloonTip(750);
                    }
                    updateStatus("Wallpaper already saved!");
                }
            }
        }

        //======================================================================
        // Set wallpaper from selected history entry
        //======================================================================
        private void useThisWallpapertoolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logging.LogMessageToFile("Setting a historical wallpaper (bypassing 'use once' check).", 0);
            Properties.Settings.Default.manualOverride = true;
            Properties.Settings.Default.Save();

            string title = (historyDataGrid.Rows[currentMouseOverRow].Cells[1].Value.ToString());
            string threadid = (historyDataGrid.Rows[currentMouseOverRow].Cells[2].Value.ToString());
            string url = (historyDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString());
            setWallpaper(url, title, threadid);
        }



        //======================================================================
        // Set wallpaper from selected favourites entry
        //======================================================================
        private void useFaveMenu_Click(object sender, EventArgs e)
        {
            Logging.LogMessageToFile("Setting a favourite wallpaper (bypassing 'use once' check).", 0);
            Properties.Settings.Default.manualOverride = true;
            Properties.Settings.Default.Save();

            string title = (favouritesDataGrid.Rows[currentMouseOverRow].Cells[1].Value.ToString());
            string threadid = (favouritesDataGrid.Rows[currentMouseOverRow].Cells[2].Value.ToString());
            string url = (favouritesDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString());
            setWallpaper(url, title, threadid);

        }

        //======================================================================
        // Populate the History panel
        //======================================================================
        private async Task<bool> populateHistory()
        {
            // var built = buildThumbnailCache();

            historyDataGrid.Rows.Clear();
            Logging.LogMessageToFile("Refreshing History panel.", 0);
            try
            {
                foreach (var item in database.getFromHistory())
                {
                    var index = historyDataGrid.Rows.Add();
                    var row = historyDataGrid.Rows[index];

                    if (File.Exists(Properties.Settings.Default.thumbnailCache + @"\" + item.threadidstring + ".jpg"))
                    {
                        Image image = Bitmap.FromFile(Properties.Settings.Default.thumbnailCache + @"\" + item.threadidstring + ".jpg");
                        row.SetValues(image, item.titlestring, item.threadidstring, item.urlstring, item.datestring);
                    }
                    else
                    {
                        Image image = Reddit_Wallpaper_Changer.Properties.Resources.null_thumb;
                        row.SetValues(image, item.titlestring, item.threadidstring, item.urlstring, item.datestring);
                    }
                }
                Logging.LogMessageToFile("History panel reloaded.", 0);
                return true;
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Error populating history panel: " + ex.Message, 1);
                return false;
            }
        }

        //======================================================================
        // Populate the blacklisted history panel
        //======================================================================
        private async void populateBlacklist()
        {
            // var built = buildThumbnailCache();

            Logging.LogMessageToFile("Refreshing blacklisted panel.", 0);
            blacklistDataGrid.Rows.Clear();

            try
            {
                foreach (var item in database.getFromBlacklist())
                {
                    var index = blacklistDataGrid.Rows.Add();
                    var row = blacklistDataGrid.Rows[index];

                    Image image = Bitmap.FromFile(Properties.Settings.Default.thumbnailCache + @"\" + item.threadidstring + ".jpg");
                    row.SetValues(image, item.titlestring, item.threadidstring, item.urlstring, item.datestring);
                }

                Logging.LogMessageToFile("Blacklisted wallpapers loaded.", 0);

            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Error populating blacklist panel: " + ex.Message, 1);
            }
        }

        //======================================================================
        // Populate the Favourites panel
        //======================================================================
        private async void populateFavourites()
        {
            // var built = buildThumbnailCache();

            Logging.LogMessageToFile("Refreshing Favourites panel.", 0);
            favouritesDataGrid.Rows.Clear();

            try
            {
                foreach (var item in database.getFromFavourites())
                {
                    var index = favouritesDataGrid.Rows.Add();
                    var row = favouritesDataGrid.Rows[index];

                    Image image = Bitmap.FromFile(Properties.Settings.Default.thumbnailCache + @"\" + item.threadidstring + ".jpg");
                    row.SetValues(image, item.titlestring, item.threadidstring, item.urlstring, item.datestring);

                }

                Logging.LogMessageToFile("Favourite wallpapers loaded.", 0);
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Error populating favourites panel: " + ex.Message, 1);
            }
        }

        //======================================================================
        // Remove a previously blacklisted wallpaper
        //======================================================================
        private void unblacklistWallpaper_Click(object sender, EventArgs e)
        {
            try
            {
                String url = (blacklistDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString());
                String date = (blacklistDataGrid.Rows[currentMouseOverRow].Cells[4].Value.ToString());
                database.removeFromBlacklist(url, date);
                populateBlacklist();
            }
            catch(Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error removing wallpaper from blacklist: " + ex.Message, 1);
            }
        }


        //======================================================================
        // Remove a previously favourited wallpaper
        //======================================================================
        private void removeFaveMenu_Click(object sender, EventArgs e)
        {
            try
            {
                String url = (favouritesDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString());
                String date = (favouritesDataGrid.Rows[currentMouseOverRow].Cells[4].Value.ToString());
                database.removeFromFavourites(url, date);
                populateFavourites();
            }
            catch(Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error removing wallpaper from favourites: " + ex.Message, 1);
            }
        }


        //======================================================================
        // Remove wallpaper from history
        //======================================================================
        private void removeThisWallpaperFromHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                String url = (historyDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString());
                String date = (historyDataGrid.Rows[currentMouseOverRow].Cells[4].Value.ToString());
                database.removeFromHistory(url, date);
                populateHistory();
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error removing wallpaper from history: " + ex.Message, 1);
            }

        }


        //======================================================================
        // Open the bug form on GitHub
        //======================================================================
        private void btnBug_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Rawns/Reddit-Wallpaper-Changer/issues/new");
        }

        //======================================================================
        // Open the log form
        //======================================================================
        private void btnLog_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(Properties.Settings.Default.AppDataPath + @"\Logs\RWC.log");
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unexpected error opening Log file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logging.LogMessageToFile("Unexpected error opening Log file: " + ex.Message, 1);
            }
        }

        //======================================================================
        // Donation button
        //======================================================================
        private void btnDonate_Click(object sender, EventArgs e)
        {    
            System.Diagnostics.Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=S9YSLJS5DXDT8");
        }

        //======================================================================
        // Enable Active Desktop for wallpaper fade effect
        //======================================================================
        public static void ActiveDesktop()
        {
            IntPtr result = IntPtr.Zero;
            SendMessageTimeout(FindWindow("Progman", IntPtr.Zero), 0x52c, IntPtr.Zero, IntPtr.Zero, 0, 500, out result);

        }

        //======================================================================
        // Import settings
        //======================================================================
        private void btnImport_Click(object sender, EventArgs e)
        {
            ManageSettings.Import();
        }

        //======================================================================
        // Export settings
        //======================================================================
        private void btnExport_Click(object sender, EventArgs e)
        {
            ManageSettings.Export();
        }

        //======================================================================
        // Save Walpaper Layout Type
        //======================================================================
        private void btnMonitorSave_Click(object sender, EventArgs e)
        {

            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);

            if (comboType.Text == "Fill")
            {
                Properties.Settings.Default.wallpaperStyle = "Fill";
                Properties.Settings.Default.Save();
                key.SetValue(@"WallpaperStyle", 10.ToString());
                key.SetValue(@"TileWallpaper", 0.ToString());
                monitorPanel_Paint();
            }
            else if (comboType.Text == "Fit")
            {
                Properties.Settings.Default.wallpaperStyle = "Fit";
                Properties.Settings.Default.Save();
                key.SetValue(@"WallpaperStyle", 6.ToString());
                key.SetValue(@"TileWallpaper", 0.ToString());
                monitorPanel_Paint();
            }
            else if (comboType.Text == "Span")
            {
                Properties.Settings.Default.wallpaperStyle = "Span";
                Properties.Settings.Default.Save();
                key.SetValue(@"WallpaperStyle", 22.ToString());
                key.SetValue(@"TileWallpaper", 0.ToString());
                monitorPanel_Paint();
            }
            else if (comboType.Text == "Stretch")
            {
                Properties.Settings.Default.wallpaperStyle = "Stretch";
                Properties.Settings.Default.Save();
                key.SetValue(@"WallpaperStyle", 2.ToString());
                key.SetValue(@"TileWallpaper", 0.ToString());
                monitorPanel_Paint();
            }
            else if (comboType.Text == "Tile")
            {
                Properties.Settings.Default.wallpaperStyle = "Tile";
                Properties.Settings.Default.Save();
                key.SetValue(@"WallpaperStyle", 0.ToString());
                key.SetValue(@"TileWallpaper", 1.ToString());
                monitorPanel_Paint();
            }
            else if (comboType.Text == "Center")
            {
                Properties.Settings.Default.wallpaperStyle = "Center";
                Properties.Settings.Default.Save();
                key.SetValue(@"WallpaperStyle", 0.ToString());
                key.SetValue(@"TileWallpaper", 0.ToString());
                monitorPanel_Paint();
            }

            MessageBox.Show("Wallpaper style successfully changed to: " + comboType.Text, "Saved!", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        //======================================================================
        // Populate info box on chosen style
        //======================================================================
        private void comboType_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetExample();
        }

        //======================================================================
        // Open the wallpaper style info window
        //======================================================================
        private void btnWallpaperHelp_Click(object sender, EventArgs e)
        {
            Form WallpaperTypes = new WallpaperTypes();
            WallpaperTypes.Show();
        }

        //======================================================================
        // Upload log file to Pastebin
        //======================================================================
        private async void btnUpload_Click(object sender, EventArgs e)
        {
            this.btnUpload.Enabled = false;
            this.btnUpload.Text = "Uploading...";
            
            var uploaded = await Task.Run(Pastebin.UploadLog);

            this.btnUpload.Text = "Upload Log";
            this.btnUpload.Enabled = true;

            if (uploaded == true)
            {
                Clipboard.SetText(Properties.Settings.Default.logUrl);
                MessageBox.Show("Your logfile has been uploaded to Pastebin successfully.\r\n" +
                    "The URL to the Paste has been copied to your clipboard.", "Upload successful!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("The upload of your logfile to Pastebin failed. Check the log for details, or upload your log manually.", "Upload failed!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        //======================================================================
        // Check for more than 7 days
        //======================================================================
        private void changeTimeType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (changeTimeType.Text == "Days" && changeTimeValue.Value >= 8)
            {
                MessageBox.Show("Sorry, but upper limit for wallpaper changes is 7 Days!", "Too many days!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                changeTimeValue.Value = 7;
            }
        }

        //======================================================================
        // Override auto save faves if auto save all is enabled
        //======================================================================
        private void chkAutoSave_CheckedChanged(object sender, EventArgs e)
        {
            if (this.chkAutoSave.Checked == true)
            {
                chkAutoSaveFaves.Enabled = false;
                chkAutoSaveFaves.Checked = false;
                Properties.Settings.Default.autoSaveFaves = false;
                Properties.Settings.Default.Save();
            }
            else
            {
                chkAutoSaveFaves.Enabled = true;
            }
        }

        //======================================================================
        // Manually save selected favourite wallpaper
        //======================================================================
        private void saveThisWallpaperToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string title = (favouritesDataGrid.Rows[currentMouseOverRow].Cells[1].Value.ToString());
            string threadid = (favouritesDataGrid.Rows[currentMouseOverRow].Cells[2].Value.ToString());
            string url = (favouritesDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString());

            if (savewallpaper.saveSelectedWallpaper(url, threadid, title))
            {
                if (Properties.Settings.Default.disableNotifications == false)
                {
                    taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                    taskIcon.BalloonTipTitle = "Wallpaper Saved!";
                    taskIcon.BalloonTipText = "Wallpaper saved to " + Properties.Settings.Default.defaultSaveLocation;
                    taskIcon.ShowBalloonTip(750);
                }

                updateStatus("Wallpaper saved!");

            }
            else
            {
                if (Properties.Settings.Default.disableNotifications == false)
                {
                    taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                    taskIcon.BalloonTipTitle = "Already Saved!";
                    taskIcon.BalloonTipText = "No need to save this wallpaper as it already exists in your wallpapers folder! :)";
                    taskIcon.ShowBalloonTip(750);
                }
                updateStatus("Wallpaper already saved!");
            }
        }

        //======================================================================
        // Manually save selected historical wallpaper
        //======================================================================
        private void saveThisWallpaperToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            string title = (historyDataGrid.Rows[currentMouseOverRow].Cells[1].Value.ToString());
            string threadid = (historyDataGrid.Rows[currentMouseOverRow].Cells[2].Value.ToString());
            string url = (historyDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString());

            if (savewallpaper.saveSelectedWallpaper(url, threadid, title))
            {
                if (Properties.Settings.Default.disableNotifications == false)
                {
                    taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                    taskIcon.BalloonTipTitle = "Wallpaper Saved!";
                    taskIcon.BalloonTipText = "Wallpaper saved to " + Properties.Settings.Default.defaultSaveLocation;
                    taskIcon.ShowBalloonTip(750);
                }

                updateStatus("Wallpaper saved!");

            }
            else
            {
                if (Properties.Settings.Default.disableNotifications == false)
                {
                    taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                    taskIcon.BalloonTipTitle = "Already Saved!";
                    taskIcon.BalloonTipText = "No need to save this wallpaper as it already exists in your wallpapers folder! :)";
                    taskIcon.ShowBalloonTip(750);
                }
                updateStatus("Wallpaper already saved!");
            }
        }

        //======================================================================
        // Delete all wallpaper history from the database
        //======================================================================
        private void btnClearHistory_Click(object sender, EventArgs e)
        {
            DialogResult choice = MessageBox.Show("Are you sure you want to delete ALL wallpaper history?\r\n" + 
                "It's recommended that you take a backup first!\r\n\r\nTHIS ACTION CANNOT BE UNDONE!", "Clear History?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (choice == DialogResult.Yes)
            {
                if (database.wipeTable("history"))
                {
                    populateHistory();
                    MessageBox.Show("All historical wallpaper data has been deleted!", "History Deleted!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else if (choice == DialogResult.No)
            {

            }
        }

        //======================================================================
        // Delete all favourite wallpapers from the database 
        //======================================================================
        private void btnClearFavourites_Click(object sender, EventArgs e)
        {
            DialogResult choice = MessageBox.Show("Are you sure you want to remove all favourite wallpapers?\r\n" +
                "It's recommended that you take a backup first!\r\n\r\nTHIS ACTION CANNOT BE UNDONE!", "Clear Favourites?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (choice == DialogResult.Yes)
            {
                if (database.wipeTable("favourites"))
                {
                    populateFavourites();
                    MessageBox.Show("All wallpapers have been deleted from your favourites!", "Favourites Deleted!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else if (choice == DialogResult.No)
            {

            }
        }

        //======================================================================
        // Delete all blacklisted wallpapers from the database 
        //======================================================================
        private void btnClearBlacklisted_Click(object sender, EventArgs e)
        {
            DialogResult choice = MessageBox.Show("Are you sure you want to remove all blacklisted wallpapers?\r\n" +
                "It's recommended that you take a backup first!\r\n\r\nTHIS ACTION CANNOT BE UNDONE!", "Clear Blacklist?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (choice == DialogResult.Yes)
            {
                if (database.wipeTable("blacklist"))
                {
                    populateBlacklist();
                    MessageBox.Show("All wallpaper have been deleted from your blacklist!", "Blacklist Deleted!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

            }
            else if (choice == DialogResult.No)
            {

            }
        }

        //======================================================================
        // Backup SQLite database
        //======================================================================
        private void btnBackup_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            folderBrowser.Description = "Select a location to backup the database:";
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                Logging.LogMessageToFile("Database backup process started.", 0);
                if (database.backupDatabase(folderBrowser.SelectedPath))
                {
                    MessageBox.Show("Your database has been successfully backed up.", "Backup Successful!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Logging.LogMessageToFile("The backup process has completed successfully.", 0);
                }
                else
                {
                    MessageBox.Show("There was an error backing up your database.", "Backup Failed!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Logging.LogMessageToFile("The backup process has failed.", 2);
                }
            }
        }

        //======================================================================
        // Restore SQLite backup
        //======================================================================
        private void btnRestore_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileBorwser = new OpenFileDialog();
            fileBorwser.Filter = "SQLite Database (*.sqlite)|*.sqlite";
            fileBorwser.Multiselect = false;
            if (fileBorwser.ShowDialog() == DialogResult.OK)
            {
                Logging.LogMessageToFile("Database restore process has been started.", 0);
                if (database.restoreDatabase(fileBorwser.FileName))
                {
                    populateHistory();
                    populateFavourites();
                    populateBlacklist();
                    MessageBox.Show("Your database has been successfully restored.", "Restore Successful!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Logging.LogMessageToFile("The restore process has completed successfully.", 0);
                }
                else
                {
                    MessageBox.Show("There was an error restoring your database.", "Restore Failed!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Logging.LogMessageToFile("The restore process has failed.", 2);
                }
            }
        }

        //======================================================================
        // Generate thumbnails for History
        //======================================================================
        public void buildThumbnailCache()
        {
            try
            {
                Logging.LogMessageToFile("Updating wallpaper thumbnail cache.", 0);
                foreach (var item in database.getFromHistory())
                {
                    byte[] bytes = Convert.FromBase64String(item.imgstring);

                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        if (!File.Exists(Properties.Settings.Default.thumbnailCache + @"\" + item.threadidstring + ".jpg"))
                        {
                            using (Image image = Bitmap.FromStream(ms))
                            {
                                image.Save(Properties.Settings.Default.thumbnailCache + @"\" + item.threadidstring + ".jpg");
                            }
                        }
                    }
                }

                foreach (var item in database.getFromFavourites())
                {
                    byte[] bytes = Convert.FromBase64String(item.imgstring);

                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        if (!File.Exists(Properties.Settings.Default.thumbnailCache + @"\" + item.threadidstring + ".jpg"))
                        {
                            using (Image image = Image.FromStream(ms))
                            {
                                image.Save(Properties.Settings.Default.thumbnailCache + @"\" + item.threadidstring + ".jpg");
                            }
                        }
                    }
                }

                foreach (var item in database.getFromBlacklist())
                {
                    byte[] bytes = Convert.FromBase64String(item.imgstring);

                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        if (!File.Exists(Properties.Settings.Default.thumbnailCache + @"\" + item.threadidstring + ".jpg"))
                        {
                            using (Image image = Image.FromStream(ms))
                            {
                                image.Save(Properties.Settings.Default.thumbnailCache + @"\" + item.threadidstring + ".jpg");
                            }
                        }
                    }
                }
                Logging.LogMessageToFile("Wallpaper thumbnail cache updated.", 0);
            }
            catch(Exception ex)
            {
                Logging.LogMessageToFile("Error updating Wallpaper thumbnail cache: " + ex.Message, 1);
            }

        }

        //======================================================================
        // Rebuild cache
        //======================================================================
        private void btnRebuildThumbnails_Click(object sender, EventArgs e)
        {
            DialogResult choice = MessageBox.Show("This will remove all wallpaper thumbnails and recreate them\r\n" +
                "when Reddit Wallpaper Changer next starts. Continue?", "Rebuild Thumbnails?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (choice == DialogResult.Yes)
            {
                try
                {
                    MessageBox.Show("The wallpaper thumbnail cache will be recreated when\r\n" +
                        "Reddit Wallpaper Changer next opens.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Logging.LogMessageToFile("User has chosen to to clear the thumbnail cache.", 0);
                    Properties.Settings.Default.rebuildThumbCache = true;
                    Properties.Settings.Default.Save();

                }
                catch(Exception ex)
                {
                    Logging.LogMessageToFile("Error: " + ex.Message, 1);
                }

            }
            else if (choice == DialogResult.No)
            {

            }
        }

        //======================================================================
        // Remove all thumbnails
        //======================================================================
        public void removeThumbnailCache()
        {
            try
            {
                Logging.LogMessageToFile("Removing thumbnail cache", 0);
                var dir = new DirectoryInfo(Properties.Settings.Default.thumbnailCache);
                foreach (var file in dir.EnumerateFiles("*.jpg"))
                {
                    file.Delete();
                }
                Properties.Settings.Default.rebuildThumbCache = false;
                Properties.Settings.Default.Save();

                buildThumbnailCache();

                Logging.LogMessageToFile("Thumbnail cache erased.", 0);

            }
            catch(Exception ex)
            {
                Logging.LogMessageToFile("Error rebuilding thumbnail cache: " + ex.Message, 1);
            }
        }

        //======================================================================
        // Delete all downloaded wallpapers on close 
        //======================================================================
        public void wallpaperCleanup()
        {
            try
            {
                foreach (var item in database.deleteOnExit())
                {
                    var dir = new DirectoryInfo(System.IO.Path.GetTempPath());

                    foreach (var file in dir.EnumerateFiles(item.threadidstring + ".*"))
                    {
                        file.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Error deleting wallpaper: " + ex.Message, 1);
            }
        }

    }
}


