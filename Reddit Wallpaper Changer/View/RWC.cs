using Microsoft.Win32;
using Reddit_Wallpaper_Changer.Log;
using Reddit_Wallpaper_Changer.Model;
using Reddit_Wallpaper_Changer.Settings;
using Reddit_Wallpaper_Changer.Wallpaper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

// RWC
namespace Reddit_Wallpaper_Changer
{
    public partial class RWC : Form
    {
        private bool realClose = false;
        private Color selectedBackColor = Color.FromArgb(214, 234, 244);
        private Color selectedBorderColor = Color.FromArgb(130, 195, 228);
        private Button selectedButton;
        private Panel selectedPanel;
        private string currentVersion;
        private int currentMouseOverRow;
        public string searchQueryValue;
        private bool enabledOnSleep;

        private Database DB { get; }
        private IWallpaperSaver WallpaperSaver { get; }
        private IWallpaperChanger WallpaperChanger { get; }
        private IThumbnailCacheBuilder ThumbnailCacheBuilder { get; }
        private ICurrentWallpaperHolder CurrentWallpaperHolder { get; }
        private CancellationTokenSource TokenSource { get; }

        public RWC(IWallpaperChanger wallpaperChanger,
            Database database,
            IWallpaperSaver saveWallpaper,
            ICurrentWallpaperHolder currentWallpaperHolder,
            IThumbnailCacheBuilder thumbnailCacheBuilder)
        {
            InitializeComponent();

            DB = database;
            WallpaperChanger = wallpaperChanger;
            WallpaperSaver = saveWallpaper;
            CurrentWallpaperHolder = currentWallpaperHolder;
            ThumbnailCacheBuilder = thumbnailCacheBuilder;
            TokenSource = new CancellationTokenSource();

            // Copy user settings from previous application version if necessary (part of the upgrade process)
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

            tt.SetToolTip(chkAutoStart, "Run Reddit Wallpaper Changer when your computer starts.");
            tt.SetToolTip(chkStartInTray, "Start Reddit Wallpaper Changer minimized.");
            tt.SetToolTip(chkProxy, "Configure a proxy server for Reddit Wallpaper Changer to use.");
            tt.SetToolTip(chkAuth, "Enable if your proxy server requires authentication.");
            tt.SetToolTip(btnBrowse, "Select the download destination for saved wallpapers.");
            tt.SetToolTip(btnSave, "Saves your settings.");
            tt.SetToolTip(btnWizard, "Open the Search wizard.");
            tt.SetToolTip(wallpaperGrabType, "Choose how you want to find a wallpaper.");
            tt.SetToolTip(changeTimeValue, "Choose how often to change your wallpaper.");
            tt.SetToolTip(subredditTextBox, "Enter the subs to scrape for wallpaper (eg, wallpaper, earthporn etc).\r\nMultiple subs can be provided and separated with a +.");
            tt.SetToolTip(chkAutoSave, "Enable this to automatically save all wallpapers to the below directory.");
            tt.SetToolTip(chkFade, "Enable this for a faded wallpaper transition using Active Desktop.\r\nDisable this option if you experience any issues when the wallpaper changes.");
            tt.SetToolTip(chkNotifications, "Disables all RWC System Tray/Notification Centre notifications.");
            tt.SetToolTip(chkFitWallpaper, "Enable this option to ensure that wallpapers matching your resolution are applied.\r\n\r\n" +
                "NOTE: If you have multiple screens, it will validate wallpaper sizes against the ENTIRE desktop area and not just your primary display (eg, 3840x1080 for two 1980x1080 displays).\r\n" +
                "Best suited to single monitors, or duel monitors with matching resolutions. If you experience a lack of wallpapers, try disabeling this option.");
            tt.SetToolTip(chkSuppressDuplicates, "Disable this option if you don't mind the occasional repeating wallpaper in the same session.");
            tt.SetToolTip(chkWallpaperInfoPopup, "Displays a mini wallpaper info popup at the bottom right of your primary display for 5 seconds.\r\n" +
                "Note: The 'Disable Notifications' option suppresses this popup.");
            tt.SetToolTip(chkAutoSaveFaves, "Enable this option to automatically save Favourite wallpapers to the below directory.");
            tt.SetToolTip(btnClearHistory, "This will erase ALL historical information from the History panel.");
            tt.SetToolTip(btnClearFavourites, "This will erase ALL wallpaper information from your Favourites.");
            tt.SetToolTip(btnClearBlacklisted, "This will erase ALL wallpaper information from your Blacklist.");
            tt.SetToolTip(btnBackup, "Backup Reddit Wallpaper Changer's database.");
            tt.SetToolTip(btnRestore, "Restore a previous backup.");
            tt.SetToolTip(btnRebuildThumbnails, "This will wipe the current thumbnail cache and recreate it.");
            tt.SetToolTip(chkUpdates, "Enable or disable automatic update checks.\r\nA manual check for updates can be done in the 'About' panel.");

            // Monitors
            tt.SetToolTip(btnWallpaperHelp, "Show info on the different wallpaper styles.");

            // About
            tt.SetToolTip(btnSubreddit, "Having issues? You can get support by posting on the Reddit Wallpaper Changer Subreddit.");
            tt.SetToolTip(btnBug, "Spotted a bug? Open a ticket on GitHub by clicking here!");
            tt.SetToolTip(btnDonate, "Reddit Wallpaper Changer is maintained by one guy in his own time!\r\nIf you'd like to say 'thanks' by getting him a beer, click here! :)");
            tt.SetToolTip(btnUpdate, "Click here to manually check for updates.");
            tt.SetToolTip(btnLog, "Click here to open the RWC log file in your default text editor.");
            tt.SetToolTip(btnImport, "Import custom settings from an XML file.");
            tt.SetToolTip(btnExport, "Export your current settings into an XML file.");
            tt.SetToolTip(btnUpload, "Having issues? Click here to automatically upload your log file to Pastebin!");

            #endregion ToolTips
        }

        //======================================================================
        // Code for if the computer sleeps or wakes up
        //======================================================================
        private void OnPowerChange(object sender, PowerModeChangedEventArgs e)
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
            FormClosing += new FormClosingEventHandler(RWC_FormClosing);
            Size = new Size(466, 531);
            updateStatus("RWC Setup Initiating.");
            taskIcon.Visible = true;
            if (Properties.Settings.Default.rebuildThumbCache == true) { ThumbnailCacheBuilder.RemoveThumbnailCache(); }
            setupSavedWallpaperLocation();
            setupAppDataLocation();
            setupThumbnailCache();
            setupProxySettings();
            setupButtons();
            setupPanels();
            setupOthers();
            setupForm();
            logSettings();
            DB.connectToDatabase();
            if (Properties.Settings.Default.dbMigrated == false) { DB.migrateOldBlacklist(); }
            ThumbnailCacheBuilder.BuildThumbnailCache();
            populateHistory();
            populateFavourites();
            populateBlacklist();
            updateStatus("RWC Setup Initiated.");
            checkInternetTimer.Enabled = true;
        }

        //======================================================================
        // Set up a folder to place Logs, Blacklists, Favorites etc. in
        //======================================================================
        private void setupAppDataLocation()
        {
            if (Properties.Settings.Default.AppDataPath == "")
            {
                string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Reddit Wallpaper Changer";
                Directory.CreateDirectory(appDataFolderPath);
                Properties.Settings.Default.AppDataPath = appDataFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        //======================================================================
        // Set up a thumbnail cache
        //======================================================================
        private void setupThumbnailCache()
        {
            if (Properties.Settings.Default.thumbnailCache == "")
            {
                string thumbnailCache = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Reddit Wallpaper Changer\ThumbnailCache";
                Directory.CreateDirectory(thumbnailCache);
                Properties.Settings.Default.thumbnailCache = thumbnailCache;
                Properties.Settings.Default.Save();
            }
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
                string savedWallpaperPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + @"\Saved Wallpapers";
                Directory.CreateDirectory(savedWallpaperPath);
                Properties.Settings.Default.defaultSaveLocation = savedWallpaperPath;
                Properties.Settings.Default.Save();
            }

            txtSavePath.Text = Properties.Settings.Default.defaultSaveLocation;
            chkAutoSave.Checked = Properties.Settings.Default.autoSave;
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
                chkProxy.Checked = true;
                txtProxyServer.Enabled = true;
                txtProxyServer.Text = Properties.Settings.Default.proxyAddress;

                if (Properties.Settings.Default.proxyAuth == true)
                {
                    chkAuth.Enabled = true;
                    chkAuth.Checked = true;
                    txtUser.Enabled = true;
                    txtUser.Text = Properties.Settings.Default.proxyUser;
                    txtPass.Enabled = true;
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
        // Set selected button formatting
        //======================================================================
        private void selectButton(Button btn)
        {
            btn.BackColor = selectedBackColor;
            btn.FlatAppearance.BorderColor = selectedBorderColor;
            btn.FlatAppearance.MouseDownBackColor = selectedBackColor;
            btn.FlatAppearance.MouseOverBackColor = selectedBackColor;
        }

        //======================================================================
        // Set unselected button formatting
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

            try
            {
                string latestVersion = "";
                using (WebClient wc = Proxy.setProxy())
                {
                    latestVersion = wc.DownloadString("https://raw.githubusercontent.com/Rawns/Reddit-Wallpaper-Changer/master/version");
                }

                if (!latestVersion.ToString().Contains(currentVersion.Trim().ToString()))
                {
                    Logging.LogMessageToFile("Current Version: " + currentVersion + ". " + "Latest version: " + latestVersion, 0);
                    DialogResult choice = MessageBox.Show("You are running version " + currentVersion + ".\r\n\r\n" + "Download version " + latestVersion.Split(new[] { '\r', '\n' }).FirstOrDefault() + " now?",
                        "Update Available!",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    if (choice == DialogResult.Yes)
                    {
                        Form Update = new Update(latestVersion);
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
            wallpaperChangeTimer.Interval = GetInterval();
            wallpaperChangeTimer.Enabled = true;
        }

        private int GetInterval()
        {
            var shotTimeSpan = TimeSpan.FromSeconds(3);
            var nextUpdateTimePoint = Properties.Settings.Default.lastWallpaperUpdateTime;

            if (Properties.Settings.Default.changeTimeType == 0) //Minutes
            {
                nextUpdateTimePoint += TimeSpan.FromMinutes(Properties.Settings.Default.changeTimeValue);
            }
            else if (Properties.Settings.Default.changeTimeType == 1) //Hours
            {
                nextUpdateTimePoint += TimeSpan.FromHours(Properties.Settings.Default.changeTimeValue);
            }
            else //Days
            {
                nextUpdateTimePoint += TimeSpan.FromDays(Properties.Settings.Default.changeTimeValue);
            }

            if (nextUpdateTimePoint + shotTimeSpan < System.DateTime.Now)
            {
                Logging.LogMessageToFile("Next update in " + shotTimeSpan.Seconds + " seconds", 0);
                return (int)shotTimeSpan.TotalMilliseconds;
            }
            else
            {
                Logging.LogMessageToFile("Next update at " + nextUpdateTimePoint, 0);
                return (int)(nextUpdateTimePoint - System.DateTime.Now).TotalMilliseconds;
            }
        }

        private async Task ChangeWallpaper()
        {
            var result = await WallpaperChanger.ChangeWallpaperAsync(
                new Progress<string>(text => updateStatus(text)),
                TokenSource.Token
            );

            if (result.Success)
            {
                if (Properties.Settings.Default.disableNotifications == false && Properties.Settings.Default.wallpaperInfoPopup == true)
                {
                    int formx = 300;
                    int formy = 90;

                    int screenx = Screen.PrimaryScreen.Bounds.Width;
                    int screeny = Screen.PrimaryScreen.Bounds.Height;

                    int popupx = screenx - formx - 50;
                    int popupy = screeny - formy - 50;
                    PopupInfo popup = new PopupInfo(result.Title, result.ThreadID)
                    {
                        Location = new Point(popupx, popupy)
                    };
                    popup.Show();
                }
            }
            else
            {
                ShowNotification("Reddit Wallpaper Changer", "No results after 50 attempts. Disabling Reddit Wallpaper Changer.");
                Logging.LogMessageToFile("No results after 50 attempts. Disabling Reddit Wallpaper Changer.", 1);
                updateStatus("RWC Disabled.");
            }

            Properties.Settings.Default.lastWallpaperUpdateTime = System.DateTime.Now;
            Properties.Settings.Default.Save();

            updateTimer();
        }

        //======================================================================
        // Wallpaper changing
        ////======================================================================
        private async void wallpaperChangeTimer_Tick(object sender, EventArgs e)
        {
            wallpaperChangeTimer.Enabled = false;
            await ChangeWallpaper();
        }

        //======================================================================
        // Update status
        //======================================================================
        private void updateStatus(string text)
        {
            statuslabel1.Text = text;
        }

        //======================================================================
        // Set the wallpaper
        //======================================================================
        private delegate void SetGridCallback();

        //======================================================================
        // Set grid for History menu
        //======================================================================
        private void SetGrid()
        {
            if (historyDataGrid.InvokeRequired)
            {
                SetGridCallback d = new SetGridCallback(SetGrid);
                Invoke(d, new object[] { });
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
            Visible = false;
            if (p)
            {
                ShowNotification("Reddit Wallpaper Changer", "Down here if you need me!");
            }
        }

        //======================================================================
        // Configure run on startup
        //======================================================================
        private void startup(bool add)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (add)
                    {
                        //Surround path with " " to make sure that there are no problems
                        //if path contains spaces.
                        key.SetValue("RWC", "\"" + Application.ExecutablePath + "\"");
                    }
                    else
                        key.DeleteValue("RWC");
                }
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
        }

        //======================================================================
        // Restore from system tray
        //======================================================================
        private void taskIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Visible = true;
        }

        //======================================================================
        // Settings selected from the menu
        //======================================================================
        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Visible = true;
        }

        //======================================================================
        // Exit selected form the menu
        //======================================================================
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logging.LogMessageToFile("Exiting Reddit Wallpaper Changer.", 0);
            realClose = true;
            TokenSource.Cancel();
            wallpaperChangeTimer.Enabled = false;
            Logging.LogMessageToFile("Reddit Wallpaper Changer is shutting down.", 0);
            DB.disconnectFromDatabase();
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
            var imageInfo = CurrentWallpaperHolder.GetCurrentWallpaper();
            if (imageInfo != null)
            {
                System.Diagnostics.Process.Start(imageInfo.ThreadLink);
            }
        }

        //======================================================================
        // Change wallpaper selected from the menu
        //======================================================================
        private async void changeWallpaperMenuItem_Click(object sender, EventArgs e)
        {
            wallpaperChangeTimer.Enabled = false;
            await ChangeWallpaper();
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
                    if (Properties.Settings.Default.autoUpdateCheck == true && false)
                    {
                        string latestVersion = wc.DownloadString("https://raw.githubusercontent.com/Rawns/Reddit-Wallpaper-Changer/master/version");
                        if (!latestVersion.Contains(currentVersion.Trim().ToString()))
                        {
                            Form Update = new Update(latestVersion);
                            Update.Show();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowNotificationError("Reddit Wallpaper Changer!", "Error checking for updates.");
                    Logging.LogMessageToFile("Error checking for updates: " + ex.Message, 0);
                }
            }

            updateTimer();
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
            string fileName = Properties.Settings.Default.currentWallpaperName;
            if (WallpaperSaver.IsAlreadySavedWallpaper(fileName))
            {
                ShowNotification("Already Saved!", "No need to save this wallpaper as it already exists in your wallpapers folder! :)");
                updateStatus("Wallpaper already saved!");
                return;
            }

            if (WallpaperSaver.SaveWallpaper(fileName))
            {
                ShowNotification("Wallpaper Saved!", "Wallpaper saved to " + Properties.Settings.Default.defaultSaveLocation);
                updateStatus("Wallpaper saved!");
            }
            else
            {
                ShowNotificationError("Can't save Wallpaper!", "Can't save wallpaper to " + Properties.Settings.Default.defaultSaveLocation);
                updateStatus("Can't save wallpaper!");
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
                //currentWallpaper.Save(fileName);
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Unexpected error: " + ex.Message, 2);
            }
        }

        //======================================================================
        // Test internet connection
        //============================================================ver==========
        private void checkInternetTimer_Tick(object sender, EventArgs e)
        {
            noticeLabel.Text = "Checking Internet Connection...";
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                checkInternetTimer.Enabled = false;
                startupTimer.Enabled = true;
                Logging.LogMessageToFile("Internet is working.", 0);
            }
            else
            {
                updateStatus("Network Unavailable. Rechecking.");
                Logging.LogMessageToFile("Network Unavailable. Rechecking.", 1);
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
            monitorLayoutPanel.Refresh();
            monitorLayoutPanel.ColumnStyles.Clear();
            monitorLayoutPanel.RowStyles.Clear();
            monitorLayoutPanel.ColumnCount = screens;
            monitorLayoutPanel.RowCount = 2;
            monitorLayoutPanel.AutoSize = true;

            int z = 0;
            foreach (var screen in Screen.AllScreens.OrderBy(i => i.Bounds.X))
            {
                var percent = 100f / screens;
                monitorLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, percent));

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

                monitorLayoutPanel.Controls.Add(monitor, z, 0);
                monitorLayoutPanel.Controls.Add(resolution, z, 1);

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
                label2.Visible = false;
                searchQuery.Visible = false;
                label9.Visible = true;
            }
            else
            {
                if (!label2.Visible)
                {
                    label2.Visible = true;
                    searchQuery.Visible = true;
                    label9.Visible = false;
                }
            }
        }

        //======================================================================
        // Code for enabling/disabling proxy credentials
        //======================================================================
        private void chkAuth_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAuth.Checked == true)
            {
                txtUser.Enabled = true;
                txtUser.Text = Properties.Settings.Default.proxyUser;
                txtPass.Enabled = true;
                txtPass.Text = Properties.Settings.Default.proxyPass;
            }
            else
            {
                txtUser.Enabled = false;
                txtUser.Text = "";
                txtPass.Enabled = false;
                txtPass.Text = "";
            }
        }

        //======================================================================
        // Code for enabeling/disabeling proxy
        //======================================================================
        private void chkProxy_CheckedChanged(object sender, EventArgs e)
        {
            if (chkProxy.Checked == true)
            {
                txtProxyServer.Enabled = true;
                txtProxyServer.Text = Properties.Settings.Default.proxyAddress;
                chkAuth.Enabled = true;
            }
            else
            {
                txtProxyServer.Enabled = false;
                txtProxyServer.Text = "";
                chkAuth.Enabled = false;
                chkAuth.Checked = false;
                txtUser.Enabled = false;
                txtUser.Text = "";
                txtPass.Enabled = false;
                txtPass.Text = "";
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
        public void addToFavourites(string url, string title, string threadid)
        {
            DB.faveWallpaper(url, title, threadid);

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
                if (WallpaperSaver.SaveSelectedWallpaper(url, threadid, title))
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

        private void ShowNotification(string title, string text)
        {
            if (Properties.Settings.Default.disableNotifications == false)
            {
                taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                taskIcon.BalloonTipTitle = title;
                taskIcon.BalloonTipText = text;
                taskIcon.ShowBalloonTip(750);
            }
        }

        private void ShowNotificationError(string title, string text)
        {
            if (Properties.Settings.Default.disableNotifications == false)
            {
                taskIcon.BalloonTipIcon = ToolTipIcon.Error;
                taskIcon.BalloonTipTitle = title;
                taskIcon.BalloonTipText = text;
                taskIcon.ShowBalloonTip(750);
            }
        }

        //======================================================================
        // Click on favourite menu
        //======================================================================
        private void faveWallpaperMenuItem_Click(object sender, EventArgs e)
        {
            var imageInfo = CurrentWallpaperHolder.GetCurrentWallpaper();

            DB.faveWallpaper(imageInfo.Url, imageInfo.Title, imageInfo.ThreadId);

            ShowNotification("Wallpaper Favourited!", "Wallpaper added to favourites!");

            populateFavourites();

            if (Properties.Settings.Default.autoSaveFaves == true)
            {
                if (WallpaperSaver.SaveSelectedWallpaper(imageInfo.Url, imageInfo.ThreadId, imageInfo.Title))
                {
                    ShowNotification("Wallpaper Saved!", "Wallpaper saved to " + Properties.Settings.Default.defaultSaveLocation);
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
        public void blacklistWallpaperMenuItem_Click(object sender, EventArgs e)
        {
            var imageInfo = CurrentWallpaperHolder.GetCurrentWallpaper();

            if (DB.blacklistWallpaper(imageInfo.Url, imageInfo.Title, imageInfo.ThreadId))
            {
                ShowNotification("Wallpaper Blacklisted!", "The wallpaper has been blacklisted! Finding a new wallpaper...");
            }
            else
            {
                ShowNotification("Error Blacklisting!", "There was an error blacklisting the wallpaper!");
            }

            wallpaperChangeTimer.Enabled = false;
            wallpaperChangeTimer.Enabled = true;
            populateBlacklist();
        }

        //======================================================================
        // Blacklist wallpaper from History panel
        //======================================================================
        private void blacklistWallpapertoolStripMenuItem_Click(object sender, EventArgs e)
        {
            string title = (historyDataGrid.Rows[currentMouseOverRow].Cells[1].Value.ToString());
            string threadid = (historyDataGrid.Rows[currentMouseOverRow].Cells[2].Value.ToString());
            string url = (historyDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString());

            if (DB.blacklistWallpaper(url, title, threadid))
            {
                ShowNotification("Wallpaper Blacklisted!", "The wallpaper has been blacklisted! Finding a new wallpaper...");
            }
            else
            {
                ShowNotification("Error Blacklisting!", "There was an error blacklisting the wallpaper!");
            }

            if (url == Properties.Settings.Default.currentWallpaperUrl)
            {
                wallpaperChangeTimer.Enabled = false;
                wallpaperChangeTimer.Enabled = true;
            }

            populateBlacklist();
        }

        //======================================================================
        // Favourite wallpaper from the History Panel
        //======================================================================
        private void favouriteThisWallpaperToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string title = (historyDataGrid.Rows[currentMouseOverRow].Cells[1].Value.ToString());
            string threadid = (historyDataGrid.Rows[currentMouseOverRow].Cells[2].Value.ToString());
            string url = (historyDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString());

            var favourite = DB.faveWallpaper(url, title, threadid);

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
                if (WallpaperSaver.SaveSelectedWallpaper(url, threadid, title))
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
        private async void useThisWallpapertoolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logging.LogMessageToFile("Setting a historical wallpaper (bypassing 'use once' check).", 0);

            string title = historyDataGrid.Rows[currentMouseOverRow].Cells[1].Value.ToString();
            string threadId = historyDataGrid.Rows[currentMouseOverRow].Cells[2].Value.ToString();
            string url = historyDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString();
            string threadLink = historyDataGrid.Rows[currentMouseOverRow].Cells[4].Value.ToString();
            await WallpaperChanger.SetWallpaperAsync(new ImageInfo(url, title, threadId, threadLink)
                , new Progress<string>(text => updateStatus(text)));
        }

        //======================================================================
        // Set wallpaper from selected favourites entry
        //======================================================================
        private async void useFaveMenu_Click(object sender, EventArgs e)
        {
            Logging.LogMessageToFile("Setting a favourite wallpaper (bypassing 'use once' check).", 0);

            string title = favouritesDataGrid.Rows[currentMouseOverRow].Cells[1].Value.ToString();
            string threadId = favouritesDataGrid.Rows[currentMouseOverRow].Cells[2].Value.ToString();
            string url = favouritesDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString();
            string threadLink = favouritesDataGrid.Rows[currentMouseOverRow].Cells[4].Value.ToString();
            await WallpaperChanger.SetWallpaperAsync(new ImageInfo(url, title, threadId, threadLink)
                , new Progress<string>(text => updateStatus(text)));
        }

        //======================================================================
        // Populate the History panel
        //======================================================================
        private bool populateHistory()
        {
            historyDataGrid.Rows.Clear();
            Logging.LogMessageToFile("Refreshing History panel.", 0);
            try
            {
                foreach (var item in DB.getFromHistory())
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
                        Image image = Properties.Resources.null_thumb;
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
        private void populateBlacklist()
        {
            Logging.LogMessageToFile("Refreshing blacklisted panel.", 0);
            blacklistDataGrid.Rows.Clear();

            try
            {
                foreach (var item in DB.getFromBlacklist())
                {
                    var index = blacklistDataGrid.Rows.Add();
                    var row = blacklistDataGrid.Rows[index];

                    Image image = Image.FromFile(Properties.Settings.Default.thumbnailCache + @"\" + item.threadidstring + ".jpg");
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
        private void populateFavourites()
        {
            // var built = buildThumbnailCache();

            Logging.LogMessageToFile("Refreshing Favourites panel.", 0);
            favouritesDataGrid.Rows.Clear();

            try
            {
                foreach (var item in DB.getFromFavourites())
                {
                    var index = favouritesDataGrid.Rows.Add();
                    var row = favouritesDataGrid.Rows[index];

                    Image image = Image.FromFile(Properties.Settings.Default.thumbnailCache + @"\" + item.threadidstring + ".jpg");
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
                string url = (blacklistDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString());
                string date = (blacklistDataGrid.Rows[currentMouseOverRow].Cells[4].Value.ToString());
                DB.removeFromBlacklist(url, date);
                populateBlacklist();
            }
            catch (Exception ex)
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
                string url = (favouritesDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString());
                string date = (favouritesDataGrid.Rows[currentMouseOverRow].Cells[4].Value.ToString());
                DB.removeFromFavourites(url, date);
                populateFavourites();
            }
            catch (Exception ex)
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
                DB.removeFromHistory(url, date);
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
            catch (Exception ex)
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
        // Save Wallpaper Layout Type
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
            btnUpload.Enabled = false;
            btnUpload.Text = "Uploading...";

            var uploaded = await Pastebin.UploadLog();

            btnUpload.Text = "Upload Log";
            btnUpload.Enabled = true;

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
            if (chkAutoSave.Checked == true)
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

            if (WallpaperSaver.SaveSelectedWallpaper(url, threadid, title))
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

            if (WallpaperSaver.SaveSelectedWallpaper(url, threadid, title))
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
                if (DB.wipeTable("history"))
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
                if (DB.wipeTable("favourites"))
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
                if (DB.wipeTable("blacklist"))
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
                if (DB.backupDatabase(folderBrowser.SelectedPath))
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
                if (DB.restoreDatabase(fileBorwser.FileName))
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

                    System.Diagnostics.Process.Start(System.Reflection.Assembly.GetEntryAssembly().Location);
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Logging.LogMessageToFile("Error: " + ex.Message, 1);
                }
            }
        }
    }
}
