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
using System.Xml;
using System.Runtime.InteropServices;
using System.Collections;
using Microsoft.Win32;
using System.Threading;

namespace Reddit_Wallpaper_Changer
{
    public partial class RWC : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr result);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, IntPtr ZeroOnly);
    
        public static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPEG", ".BMP", ".GIF", ".PNG" };
        bool realClose = false;
        Color selectedBackColor = Color.FromArgb(214, 234, 244);
        Color selectedBorderColor = Color.FromArgb(130, 195, 228);
        Button selectedButton;
        Panel selectedPanel;
        String currentVersion;
        int dataGridNumber;                                          
        Bitmap currentWallpaper;
        String currentThread;
        Boolean monitorsCreated = false;
        ArrayList monitorRec = new ArrayList();
        Image memoryStreamImage;
        Random r;
        int currentMouseOverRow;
        public String searchQueryValue;
        Boolean enabledOnSleep;
        ArrayList historyRepeated = new ArrayList();
        int noResultCount = 0;
        BackgroundWorker bw = new BackgroundWorker();
        Blacklist blacklist;

        public RWC()
        {
            InitializeComponent();
            SystemEvents.PowerModeChanged += OnPowerChange;

            ToolTip tt = new ToolTip();
            tt.AutoPopDelay = 5000;
            tt.InitialDelay = 1000;
            tt.ReshowDelay = 500;
            tt.ShowAlways = true;
            tt.ToolTipTitle = "RWC";
            tt.ToolTipIcon = ToolTipIcon.Info;            

            // Settings
            tt.SetToolTip(this.chkAutoStart, "Run Reddit Wallpaper Changer when your computer starts.");
            tt.SetToolTip(this.chkStartInTray, "Start Reddit Wallpaper Changer minimised.");
            tt.SetToolTip(this.chkProxy, "Configure a proxy server for Reddit Wallpaper Changer to use.");
            tt.SetToolTip(this.chkAuth, "Enable if your proxy server requires authentication.");
            tt.SetToolTip(this.btnBrowse, "Sellect the downlaod destination for saved wallpapers.");
            tt.SetToolTip(this.saveButton, "Saves your settings.");
            tt.SetToolTip(this.btnWizard, "Open the Search wizard.");
            tt.SetToolTip(this.wallpaperGrabType, "Choose how you want to find a wallpaper.");
            tt.SetToolTip(this.changeTimeValue, "Choose how oftern to change your wallpaper.");
            tt.SetToolTip(this.subredditTextBox, "Enter the subs to scrape for wallpaper (eg, wallpaper, earthporn etc).\r\nMultiple subs can be provided and separated with a +.");
            tt.SetToolTip(this.chkAutoSave, "Enable this to automatically save all wallpapers to the above directory.");
            tt.SetToolTip(this.chkFade, "Enable this for a faded wallpaper transition using Active Desktop.\r\nDisable this option if you experience any issues when the wallpaper changes.");

            // Monitors


            // About
            tt.SetToolTip(this.btnSubreddit, "Having issues? You can get support by posting on the Reddit Wallpaper Changer Subreddit.");
            tt.SetToolTip(this.btnBug, "Spotted a bug? Open a ticket on GitHub by clicking here!");
            tt.SetToolTip(this.btnDonate, "Reddit Wallpaper Changer is maintained by one guy in his own time! If you'd like to say 'thanks' and get him a beer, click here! :)");
            tt.SetToolTip(this.btnUpdate, "Click here to manually check for updates.");
            tt.SetToolTip(this.btnLog, "Click here to open the RWC log file in your default text editor.");

            Logging.LogMessageToFile("===================================================================================================================");
            Logging.LogMessageToFile("Reddit Wallpaper Changer Version " + Assembly.GetEntryAssembly().GetName().Version.ToString());
            Logging.LogMessageToFile("RWC is starting.");
            Logging.LogMessageToFile("RWC Interface Loaded.");

        }

        //======================================================================
        // Wallpaper layout styles
        //======================================================================
        public enum Style : int
        {
            Tiled,
            Centered,
            Stretched,
            Fill
        }

        //======================================================================
        // Code for if the computer sleeps or wakes up
        //======================================================================
        void OnPowerChange(Object sender, PowerModeChangedEventArgs e)
        {
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
            this.Size = new Size(391, 508);
            updateStatus("RWC Setup Initating.");
            r = new Random();
            taskIcon.Visible = true;
            setupSavedWallpaperLocation();
            setupAppDataLocation();
            setupProxySettings();
            setupButtons();
            setupPanels();          
            setupOthers();
            setupForm();
            logSettings();
            UpgradeCleanup.deleteOldVersion();
            blacklist = new Blacklist(Properties.Settings.Default.AppDataPath + @"\Blacklist.xml");           
            populateBlacklistHistory();
            updateStatus("RWC Setup Initated.");
            checkInternetTimer.Enabled = true;
        }

        //======================================================================
        // Set up a folder to place Logs, Blacklists, Favorites etc. in
        //======================================================================
        private void setupAppDataLocation()
        {
            if (Properties.Settings.Default.AppDataPath == "")
            {
                // If it has not been set before, or is not set by the user, create a new folder in %APPDATA%
                String appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Reddit Wallpaper Changer";
                System.IO.Directory.CreateDirectory(appDataFolderPath);
                Properties.Settings.Default.AppDataPath = appDataFolderPath;
                Properties.Settings.Default.Save();
            }     
        }

        //======================================================================
        // Log startup info
        //======================================================================
        private void logSettings()
        {
            int screens = Screen.AllScreens.Count();

            
            Logging.LogMessageToFile("Auto Start: " + Properties.Settings.Default.autoStart);
            Logging.LogMessageToFile("Start In Tray: " + Properties.Settings.Default.startInTray);
            Logging.LogMessageToFile("Proxy Enabled: " + Properties.Settings.Default.useProxy);
            if (Properties.Settings.Default.useProxy == true)
            {
                Logging.LogMessageToFile("Proxy Address:" + Properties.Settings.Default.proxyAddress);
                Logging.LogMessageToFile("Proxy Authentication: " + Properties.Settings.Default.proxyAuth);
            }
            Logging.LogMessageToFile("Save location for wallpapers set to " + Properties.Settings.Default.defaultSaveLocation);
            Logging.LogMessageToFile("Auto Save All Wallpapers: " + Properties.Settings.Default.autoSave);
            Logging.LogMessageToFile("Wallpaper Grab Type: " + Properties.Settings.Default.wallpaperGrabType);
            Logging.LogMessageToFile("Selected Subreddits: " + Properties.Settings.Default.subredditsUsed);
            Logging.LogMessageToFile("Wallpaper Fade Effect: " + Properties.Settings.Default.wallpaperFade);
            Logging.LogMessageToFile("Search Query: " + Properties.Settings.Default.searchQuery);
            Logging.LogMessageToFile("Change wallpaper every " + Properties.Settings.Default.changeTimeValue + " " + changeTimeType.Text);
            Logging.LogMessageToFile("Detected " + screens + " display(s).");

        }

        //======================================================================
        // Set folder path for saving wallpapers
        //======================================================================
        private void setupSavedWallpaperLocation()
        {
            if (Properties.Settings.Default.defaultSaveLocation == "")
            {
                // if the user hasn't set a path yet, create a new directory in My Pictures
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
            currentVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
            lblVersion.Text = "Current Version: " + currentVersion;
        }

        //======================================================================
        // Setup the four panels
        //======================================================================
        private void setupPanels()
        {
            int w = 375;
            int h = 405;
            aboutPanel.Size = new Size(w, h);
            configurePanel.Size = new Size(w, h);
            monitorPanel.Size = new Size(w, h);
            historyPanel.Size = new Size(w, h);
            blacklistPanel.Size = new Size(w, h);

            int x = 0;
            int y = 65;    
            aboutPanel.Location = new Point(x, y);
            configurePanel.Location = new Point(x, y);
            monitorPanel.Location = new Point(x, y);
            historyPanel.Location = new Point(x, y);
            blacklistPanel.Location = new Point(x, y);
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
        // Monitor button click
        //======================================================================
        private void monitorButton_Click_1(object sender, EventArgs e)
        {
            if (selectedPanel != monitorPanel)
            {
                selectedPanel.Visible = false;
                monitorPanel.Visible = true;
                cleanButton(selectedButton);
                selectButton(monitorButton);
                selectedButton = monitorButton;
                selectedPanel = monitorPanel;
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
            Logging.LogMessageToFile("Manual check for updates initiated.");

            btnUpdate.Enabled = false;
            btnUpdate.Text = "Checking....";
            currentVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();

            WebClient wc = Proxy.setProxy();
            try
            {
                String latestVersion = wc.DownloadString("https://raw.githubusercontent.com/Rawns/Reddit-Wallpaper-Changer/master/version");

                if (!latestVersion.ToString().Contains(currentVersion.Trim().ToString()))
                {
                    Logging.LogMessageToFile("Current Version: " + currentVersion + ". " + "Latest version: " + latestVersion);
                    DialogResult choice = MessageBox.Show("You are running version " + currentVersion + "." + Environment.NewLine + "Download version " + latestVersion + " now?", "Update Avaiable!", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

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
                    Logging.LogMessageToFile("Reddit Wallpaper Changer is up to date (" + currentVersion + ")");
                    taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                    taskIcon.BalloonTipTitle = "Reddit Wallpaper Changer";
                    taskIcon.BalloonTipText = "RWC is up to date! :)";
                    taskIcon.ShowBalloonTip(700);
                }
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Error checking for updates: " + ex.Message);
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
            saveData();
            changeWallpaperTimer.Enabled = true;
            updateStatus("Save Successful");
            Logging.LogMessageToFile("Settings saved.");
        }

        //======================================================================
        // Save button code
        //======================================================================
        private void saveData()
        {
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
        //======================================================================
        private void wallpaperChangeTimer_Tick(object sender, EventArgs e)
        {
            changeWallpaperTimer.Enabled = true;
        }

        //======================================================================
        // Search for a wallpaper
        //======================================================================
        private void changeWallpaper()
        {
            Logging.LogMessageToFile("Changing wallpaper.");
            var bw = new BackgroundWorker();

            // BackgroundWorker bw = new BackgroundWorker();
            // bw.WorkerSupportsCancellation = true;
            // bw.WorkerReportsProgress = true;
            // bw.DoWork += new DoWorkEventHandler(delegate(object o, DoWorkEventArgs args)
             bw.DoWork += delegate
             {
                Logging.LogMessageToFile("The background worker started successfully and is looking for a wallpaper.");
                if (noResultCount >= 50)
                {
                    noResultCount = 0;
                    MessageBox.Show("No Results After 50 Retries. Disabling Reddit Wallpaper Changer.", "Reddit Wallpaper Changer: Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Logging.LogMessageToFile("No results after 50 retries. Disabeling Reddit Wallpaper Changer.");
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
                string[] subs = subreddits.Split('+');
                string sub = subs[rand.Next(0, subs.Length)];
                updateStatus("Sub: " + sub);
                Logging.LogMessageToFile("Sellected sub to search: " + sub);

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
                        formURL += "/search.json?q=" + query + randomSort[random.Next(0, 4)] + randomT[random.Next(0, 5)] + "&restrict_sr=on";
                        Logging.LogMessageToFile("Full URL Search String: " + formURL);
                        break;
                    case 1:
                        formURL += "/search.json?q=" + query + "&sort=new&restrict_sr=on";
                        Logging.LogMessageToFile("Full URL Search String: " + formURL);
                        break;
                    case 2:
                        formURL += "/search.json?q=" + query + "&sort=hot&restrict_sr=on&t=day";
                        Logging.LogMessageToFile("Full URL Search String: " + formURL);
                        break;
                    case 3:
                        formURL += "/search.json?q=" + query + "&sort=top&restrict_sr=on&t=hour";
                        Logging.LogMessageToFile("Full URL Search String: " + formURL);
                        break;
                    case 4:
                        formURL += "/search.json?q=" + query + "&sort=top&restrict_sr=on&t=day";
                        Logging.LogMessageToFile("Full URL Search String: " + formURL);
                        break;
                    case 5:
                        formURL += "/search.json?q=" + query + "&sort=top&restrict_sr=on&t=week";
                        Logging.LogMessageToFile("Full URL Search String: " + formURL);
                        break;
                    case 6:
                        formURL += "/search.json?q=" + query + "&sort=top&restrict_sr=on&t=month";
                        Logging.LogMessageToFile("Full URL Search String: " + formURL);
                        break;
                    case 7:
                        formURL += "/search.json?q=" + query + "&sort=top&restrict_sr=on&t=year";
                        Logging.LogMessageToFile("Full URL Search String: " + formURL);
                        break;
                    case 8:
                        formURL += "/search.json?q=" + query + "&sort=top&restrict_sr=on&t=all";
                        Logging.LogMessageToFile("Full URL Search String: " + formURL);
                        break;
                    case 9:
                        formURL += "/random.json?p=" + (System.Guid.NewGuid().ToString());
                        Logging.LogMessageToFile("Full URL Search String: " + formURL);
                        break;
                }

                String jsonData = "";
                bool failedDownload = false;
                using (WebClient client = new WebClient())
                {
                     WebClient wc = Proxy.setProxy();

                     try
                    {
                        Logging.LogMessageToFile("Searching Reddit for a wallpaper.");
                        jsonData = client.DownloadString(formURL);

                    }
                    catch (System.Net.WebException Ex)
                    {
                        if (Ex.Message == "The remote server returned an error: (503) Server Unavailable.")
                        {
                            updateStatus("Reddit Server Unavailable, try again later.");
                            Logging.LogMessageToFile("Reddit Server Unavailable, try again later.");
                        }
                        failedDownload = true;
                    }
                    client.Dispose();
                }
                try
                {
                    if (jsonData.Length == 0)
                    {
                        updateStatus("Subreddit Probably Doesn't Exist");
                        Logging.LogMessageToFile("Subreddit probably does not exist.");
                        ++noResultCount;
                        failedDownload = true;
                        breakBetweenChange.Enabled = true;
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
                            IEnumerable<JToken> redditResultReversed = redditResult.Reverse();
                            foreach (JToken toke in redditResultReversed)
                            {
                                if (!historyRepeated.Contains(toke["data"]["id"].ToString()))
                                {
                                    token = toke;
                                }
                            }
                            bool needsChange = false;
                            if (token == null)
                            {
                                if (redditResult.Count() == 0)
                                {
                                    ++noResultCount;
                                    Logging.LogMessageToFile("No search results, trying to change wallpaper again.");
                                    needsChange = true;
                                    changeWallpaper();
                                }
                                else
                                {
                                    historyRepeated.Clear();
                                    int randIndex = r.Next(0, redditResult.Count() - 1);
                                    token = redditResult.ElementAt(randIndex);

                                }
                            }
                            if (!needsChange)
                            {
                                if (wallpaperGrabType != 0)
                                {

                                    currentThread = "http://reddit.com" + token["data"]["permalink"].ToString();
                                    Logging.LogMessageToFile("Found a wallpaper! Title: " + token["data"]["title"].ToString() + ", URL: " + token["data"]["url"].ToString() + ", ThreadID: " + token["data"]["id"].ToString());
                                    setWallpaper(token["data"]["url"].ToString(), token["data"]["title"].ToString(), token["data"]["id"].ToString());
                                }
                                else
                                {
                                    token = redditResult.ElementAt(random.Next(0, redditResult.Count() - 1));
                                    currentThread = "http://reddit.com" + token["data"]["permalink"].ToString();
                                    Logging.LogMessageToFile("Found a wallpaper! Title: " + token["data"]["title"].ToString() + ", URL: " + token["data"]["url"].ToString() + ", ThreadID: " + token["data"]["id"].ToString());
                                    setWallpaper(token["data"]["url"].ToString(), token["data"]["title"].ToString(), token["data"]["id"].ToString());

                                }
                            }

                        }
                        catch (System.InvalidOperationException)
                        {
                            updateStatus("Your query is bringing up no results.");
                            Logging.LogMessageToFile("No results from the search query.");
                            failedDownload = true;
                            breakBetweenChange.Enabled = true;
                        }


                    }
                    else
                    {
                        breakBetweenChange.Enabled = true;
                    }
                }
                catch (JsonReaderException ex)
                {
                    Logging.LogMessageToFile("Unexpected error: " + ex.Message);
                    breakBetweenChange.Enabled = true;

                }

            };

            bw.RunWorkerAsync();
        }
        delegate void SetTextCallback(string text);

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
                this.statuslabel.Text = text;
            }
        }

        //======================================================================
        // Set the wallpaper
        //======================================================================
        private void setWallpaper(string url, string title, string threadID)
        {
            HttpWebRequest imageCheck = (HttpWebRequest)WebRequest.Create(url);
            // imageCheck.Timeout = 5000;

            imageCheck.Method = "HEAD";
            imageCheck.AllowAutoRedirect = false;
            var imageResponse = imageCheck.GetResponse();

            // If anything other than OK, assume that image has been deleted
            if (!imageResponse.ContentType.StartsWith("image/"))
            {
                imageCheck.Abort();
                updateStatus("Non-image URL.");
                Logging.LogMessageToFile("Not a direct wallpaper URL, searching again.");
                noResultCount++;
                changeWallpaperTimer.Enabled = false;
                changeWallpaper();
                return;
            }
            else
            {
                imageCheck.Abort();
                Logging.LogMessageToFile("The chosen URL is for an image.");
            }
            

            // Check if the image that has been found has been deleted from imgur
            if (url.Contains("imgur"))
            {
                // A request for a deleted image on Imgur will return status code 302 & redirect to http://i.imgur.com/removed.png returning status code 200
                HttpWebRequest imgurRequest = (HttpWebRequest)WebRequest.Create(url);
                // imgurRequest.Timeout = 5000;
                imgurRequest.Method = "HEAD";
                imgurRequest.AllowAutoRedirect = false;
                HttpWebResponse imgurResponse = imgurRequest.GetResponse() as HttpWebResponse;

                // If anything other than OK, assume that image has been deleted
                if (imgurResponse.StatusCode.ToString() != "OK")
                {
                    imgurRequest.Abort();
                    updateStatus("Wallpaper was deleted from Imgur.");
                    Logging.LogMessageToFile("The selected wallpaper was deleted from Imgur, searching again.");
                    noResultCount++;
                    changeWallpaperTimer.Enabled = false;
                    changeWallpaper();
                    return;
                }
                else
                {
                    imgurRequest.Abort();
                    Logging.LogMessageToFile("The chosen wallpaper is still available on Imgur.");
                }
            }


            Logging.LogMessageToFile("Setting wallpaper.");          

            if (blacklist.containsURL(url))
            {
                updateStatus("Wallpaper is blacklisted.");
                Logging.LogMessageToFile("The selected wallpaper has been blacklisted, searching again.");
                changeWallpaperTimer.Enabled = false;
                changeWallpaper();
                return;
            }

            List<string> historyList = new List<string>();
            foreach (DataGridViewRow item in historyDataGrid.Rows)
            {
                if (item.Cells[4].Value != null)
                {
                    historyList.Add(item.Cells[4].Value.ToString());
                }
            }

            if (historyList.Contains(url))
            {
                updateStatus("Wallpaper already used this session.");
                Logging.LogMessageToFile("The selected wallpaper has already been used this session, searching again.");
                changeWallpaperTimer.Enabled = false;
                changeWallpaper();
                return;
            }

            var bw = new BackgroundWorker();
            bw.DoWork += delegate
            {
                Uri uri2 = new Uri(url);
                string extention2 = System.IO.Path.GetExtension(uri2.LocalPath);

                historyMenuStrip.Hide();
                BeginInvoke((MethodInvoker)delegate
                {
                    updateStatus("Setting Wallpaper");

                });
                string url2 = url.ToLower();
                if (url.Equals(null) || url.Length.Equals(0))
                {
                    changeWallpaperTimer.Enabled = true;
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
                    string filename = "currentWallpaper" + extention;
                    string wallpaperFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), filename);
                    Properties.Settings.Default.currentWallpaperFile = wallpaperFile;
                    Properties.Settings.Default.url = url;
                    Properties.Settings.Default.threadTitle = title;
                    Properties.Settings.Default.currentWallpaperUrl = url;
                    Properties.Settings.Default.currentWallpaperName = title + extention;
                    Properties.Settings.Default.threadID = threadID;
                    Properties.Settings.Default.Save();

                    Logging.LogMessageToFile("URL: " + url);
                    Logging.LogMessageToFile("Title: " + title);
                    Logging.LogMessageToFile("Thread ID: " + threadID);

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
                                Logging.LogMessageToFile("Unexpected error: " + Ex.Message);

                            }
                        }
                        try
                        {
                            WebClient webClient = Proxy.setProxy();
                            webClient.DownloadFile(uri.AbsoluteUri, @wallpaperFile);

                            if (Properties.Settings.Default.wallpaperFade == true)
                            {
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
                                Reddit_Wallpaper_Changer.ActiveDesktop.SystemParametersInfo(Reddit_Wallpaper_Changer.ActiveDesktop.SPI_SETDESKWALLPAPER, 0, @wallpaperFile, Reddit_Wallpaper_Changer.ActiveDesktop.SPIF_UPDATEINIFILE | Reddit_Wallpaper_Changer.ActiveDesktop.SPIF_SENDWININICHANGE);

                            }

                            historyRepeated.Add(threadID);
                            noResultCount = 0;
                            BeginInvoke((MethodInvoker)delegate
                            {
                                updateStatus("Wallpaper Changed!");
                            });
                            Logging.LogMessageToFile("Wallpaper changed!");
                            
                            if (Properties.Settings.Default.autoSave == true)
                            {
                                AutoSave();
                            }
                        }
                        catch (System.Net.WebException Ex)
                        {
                            Logging.LogMessageToFile("Unexpected Error: " + Ex.Message);

                        }
                    }
                    else
                    {
                        changeWallpaperTimer.Enabled = true;
                    }

                }

                WebClient wc = Proxy.setProxy();
                byte[] bytes = wc.DownloadData(url);

                if (bytes.Count().Equals(0))
                {
                    changeWallpaperTimer.Enabled = true;
                }
                else
                {
                    try
                    {

                        MemoryStream ms = new MemoryStream(bytes);
                        memoryStreamImage = System.Drawing.Image.FromStream(ms);
                        ms.Dispose();
                        ms.Close();

                        if (currentWallpaper != null)
                        {
                            currentWallpaper.Dispose();

                        }
                        currentWallpaper = new Bitmap(memoryStreamImage);
                        dataGridNumber += 1;

                        SetGrid(new Bitmap(memoryStreamImage, new Size(100, 100)), title, dataGridNumber, threadID, url);
                        memoryStreamImage.Dispose();

                    }
                    catch (ArgumentException Ex)
                    {
                        Logging.LogMessageToFile("Unexpected Error: " + Ex.Message);
                        dataGridNumber += 1;
                        SetGrid(null, title, dataGridNumber, threadID, url);
                        historyDataGrid.Rows[0].Visible = false;
                        breakBetweenChange.Enabled = true;
                    }

                    wc.Dispose();
                }
            };

            bw.RunWorkerAsync();
        }

        delegate void SetGridCallback(Bitmap img, string title, int dataGridNumber, string threadID, string url);

        //======================================================================
        // Set grid for History menu
        //======================================================================
        private void SetGrid(Bitmap img, string title, int dataGridNumber, string threadID, string url)
        {
            if (this.historyDataGrid.InvokeRequired)
            {
                SetGridCallback d = new SetGridCallback(SetGrid);
                this.Invoke(d, new object[] { img, title, dataGridNumber, threadID, url });
            }
            else
            {
                historyDataGrid.Rows.Insert(0, img, title, dataGridNumber, threadID, url);
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
                taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                taskIcon.BalloonTipTitle = "Reddit Wallpaper Changer";
                taskIcon.BalloonTipText = "Down here if you need me!";
                taskIcon.ShowBalloonTip(700);

            }
        }

        //======================================================================
        // Configure run on startup 
        //======================================================================
        private void startup(bool add)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(
                       @"Software\Microsoft\Windows\CurrentVersion\Run", true);
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
                // toolTip1.Active = false;
                // deleteWindowsMenu();
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
            Logging.LogMessageToFile("Exiting Reddit Wallpaper Changer.");
            realClose = true;
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
                Logging.LogMessageToFile("Running.");

            }
            else
            {
                statusMenuItem1.ForeColor = Color.Red;
                statusMenuItem1.Text = "Not Running";
                Logging.LogMessageToFile("Not Running.");

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
            WebClient wc = Proxy.setProxy();

            try
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
            catch
            {
                taskIcon.BalloonTipIcon = ToolTipIcon.Error;
                taskIcon.BalloonTipTitle = "Reddit Wallpaper Changer!";
                taskIcon.BalloonTipText = "Error checking for updates.";
                taskIcon.ShowBalloonTip(750);
            }

            wc.Dispose();
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
        // Open thread from history selection click
        //======================================================================
        private void historyDataGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            int rowIndex = e.RowIndex;
            System.Diagnostics.Process.Start("http://reddit.com/" + historyDataGrid.Rows[e.RowIndex].Cells[3].Value.ToString());
        }

        //======================================================================
        // Save wallpaper locally
        //======================================================================
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // saveWallpaper.ShowDialog();
            try
            {
                if (!File.Exists(Properties.Settings.Default.defaultSaveLocation + @"\" + Properties.Settings.Default.currentWallpaperName))
                {

                    System.IO.File.Copy(Properties.Settings.Default.currentWallpaperFile, Properties.Settings.Default.defaultSaveLocation + @"\" + Properties.Settings.Default.currentWallpaperName);
                    taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                    taskIcon.BalloonTipTitle = "Wallpaper Saved!";
                    taskIcon.BalloonTipText = "Wallpaper saved to " + Properties.Settings.Default.defaultSaveLocation + @"\" + Properties.Settings.Default.currentWallpaperName;
                    taskIcon.ShowBalloonTip(750);
                    Logging.LogMessageToFile("Saved " + Properties.Settings.Default.currentWallpaperName + " to " + Properties.Settings.Default.defaultSaveLocation);
                }
                else
                {
                    taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                    taskIcon.BalloonTipTitle = "Already Saved!";
                    taskIcon.BalloonTipText = "No need to save this wallpaper as it already exists in your wallpapers folder! :)";
                    taskIcon.ShowBalloonTip(750);
                }
 
            }
            catch (Exception Ex)
            {
                taskIcon.BalloonTipIcon = ToolTipIcon.Error;
                taskIcon.BalloonTipTitle = "Error Saving!";
                taskIcon.BalloonTipText = "Unable to save the wallpaper locally. :(";
                taskIcon.ShowBalloonTip(750);
                Logging.LogMessageToFile("Error Saving Wallpaper: " + Ex.Message);
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
            catch
            {

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
                noticeLabel.Text = "";
                checkInternetTimer.Enabled = false;
                updateTimer();
                startupTimer.Enabled = true;
                Logging.LogMessageToFile("Internet is working.");
            }
            else
            {
                updateStatus("Network Unavaliable. Rechecking.");
                Logging.LogMessageToFile("Network Unavaliable. Rechecking.");
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
        
        private void breakBetweenChange_Tick(object sender, EventArgs e)
        {
            breakBetweenChange.Enabled = false;
            changeWallpaperTimer.Enabled = true;
        }

        //======================================================================
        // Add a button for each attached monitor 
        //======================================================================
        private void monitorPanel_Paint(object sender, PaintEventArgs e)
        {
            if (!monitorsCreated)
            {
                monitorsCreated = true;

                int screens = Screen.AllScreens.Count();

                if (screens == 1)
                {
                    comboType.Enabled = false;
                    monitorSaveButton.Enabled = false;
                    comboType.Text = "-";
                }

                this.monitorLayoutPanel.ColumnStyles.Clear();
                this.monitorLayoutPanel.ColumnCount = screens;
                this.monitorLayoutPanel.RowCount = 2;           
                this.monitorLayoutPanel.AutoSize = true;

                int z = 0;
                foreach (var screen in Screen.AllScreens.OrderBy(i => i.Bounds.X))
                {
                    var percent = 100f / screens;
                    this.monitorLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, percent));

                    PictureBox monitor = new PictureBox
                    {
                        Name = "MonitorPic" + z,
                        Size = new Size(95, 75),
                        BackgroundImageLayout = ImageLayout.Stretch,
                        BackgroundImage = Properties.Resources.display_enabled,
                        Anchor = System.Windows.Forms.AnchorStyles.None
                    };

                    Label rez = new Label
                    {
                        Name = "MonitorLabel" + z,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font("Segoe UI", 10, FontStyle.Bold),
                        ForeColor = Color.Black,
                        BackColor = Color.Transparent,
                        Text = screen.Bounds.Width + "x" + screen.Bounds.Height,
                        Anchor = System.Windows.Forms.AnchorStyles.Bottom
                    };

                    this.monitorLayoutPanel.Controls.Add(monitor, z, 0);
                    this.monitorLayoutPanel.Controls.Add(rez, z, 1);
                    z++;
                }
            }
        }

        //TODO: Pick up multi monitor again, 
        //======================================================================
        // Change monitor colour based on click
        //======================================================================
        // private void monitor_Click(object sender, MouseEventArgs e)
        //{
        //    if (((Button)sender).BackgroundImage == Properties.Resources.display_disabled)
        //    {
        //        ((Button)sender).BackgroundImage = Properties.Resources.display_enabled;
        //    }
        //    else
        //    {
        //        ((Button)sender).BackgroundImage = Properties.Resources.display_disabled;
        //    }
        //}

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
                    contextMenuStrip1.Show(historyDataGrid, new Point(e.X, e.Y));
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
            if (chkProxy.Checked == true)
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
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                txtSavePath.Text = folderBrowser.SelectedPath;
            }
        }

        //TODO: Must do something with this sometime! 
        //======================================================================
        // Add current wallpaper to favourites
        //======================================================================
        //public void Favourite()
        //{
        //    XDocument doc = XDocument.Load(AppDomain.CurrentDomain.BaseDirectory + "Favourites.xml");
        //    XElement favourite = doc.Element("Favourites");
        //    favourite.Add(new XElement("Wallpaper",
        //        new XElement("URL", "http://some.wallpaper/link.jpeg"),
        //        new XElement("Title", "Another Wallpaper!"),
        //        new XElement("ThreadID", "Thread ID here")));
        //    doc.Save("Favourites.xml");

        //    faveWallpaperMenuItem.Checked = true;

        //    taskIcon.BalloonTipIcon = ToolTipIcon.Info;
        //    taskIcon.BalloonTipTitle = "Favourite Wallpaper!";
        //    taskIcon.BalloonTipText = "The current Wallpaper has been added to your favourites successfully!";
        //    taskIcon.ShowBalloonTip(750);
        //}

        //======================================================================
        //Add current wallpaper to blacklist
        //======================================================================
        public void Blacklist()
        {
            blacklist.addEntry(Properties.Settings.Default.url, Properties.Settings.Default.threadTitle, Properties.Settings.Default.threadID);          

            taskIcon.BalloonTipIcon = ToolTipIcon.Info;
            taskIcon.BalloonTipTitle = "Wallpaper Blacklisted!";
            taskIcon.BalloonTipText = "The current Wallpaper has been blacklisted! Finding a new wallpaper...";
            taskIcon.ShowBalloonTip(750);

            Logging.LogMessageToFile("Wallpaper Blacklisted! Wallpaper Title: " + Properties.Settings.Default.threadTitle + 
                ", URL: " + Properties.Settings.Default.url + 
                ", ThreadID: " + Properties.Settings.Default.threadID);

            wallpaperChangeTimer.Enabled = false;
            wallpaperChangeTimer.Enabled = true;
            changeWallpaperTimer.Enabled = true;

            populateBlacklistHistory();

        }

        //======================================================================
        // Add wallpaper from history view to blacklist
        //======================================================================
        public void MenuBlacklist(string url, string title, string threadid)
        {
            blacklist.addEntry(url, title, threadid);           

            taskIcon.BalloonTipIcon = ToolTipIcon.Info;
            taskIcon.BalloonTipTitle = "Wallpaper Blacklisted!";
            taskIcon.BalloonTipText = "The historical Wallpaper has been blacklisted!";
            taskIcon.ShowBalloonTip(750);

            Logging.LogMessageToFile("Wallpaper Blacklisted! Wallpaper Title: " + title + ", URL: " + url + ", ThreadID: " + threadid);

            if (url == Properties.Settings.Default.currentWallpaperUrl)
            {
                wallpaperChangeTimer.Enabled = false;
                wallpaperChangeTimer.Enabled = true;
                changeWallpaperTimer.Enabled = true;
            }
            populateBlacklistHistory();
        }

        //======================================================================
        // Click on favourite menu
        //======================================================================
        //private void faveWallpaperMenuItem_Click(object sender, EventArgs e)
        //{
        //    Favourite();
        //}

        //======================================================================
        // Click on blacklist menu
        //======================================================================
        private void blockWallpaperMenuItem_Click(object sender, EventArgs e)
        {
            Blacklist();
        }

        //======================================================================
        // Set wallpaper from selected history entry
        //======================================================================
        private void useThisWallpapertoolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = (historyDataGrid.Rows[currentMouseOverRow].Cells[4].Value.ToString());
            string title = (historyDataGrid.Rows[currentMouseOverRow].Cells[1].Value.ToString());
            string threadid = (historyDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString());
            setWallpaper(url, title, threadid);
        }

        //======================================================================
        // Historical Blacklist menu click
        //======================================================================
        private void blacklistWallpapertoolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = (historyDataGrid.Rows[currentMouseOverRow].Cells[4].Value.ToString());
            string title = (historyDataGrid.Rows[currentMouseOverRow].Cells[1].Value.ToString());
            string threadid = (historyDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString());
            MenuBlacklist(url, title, threadid);
        }

        //======================================================================
        // Pupulate the blacklisted history panel
        //======================================================================
        private void populateBlacklistHistory()
        {
            Logging.LogMessageToFile("Refreshing blacklisted wallpaper history.");
            blacklistDataGrid.Rows.Clear();
            BackgroundWorker blUpdate= new BackgroundWorker();
            blUpdate.WorkerSupportsCancellation = true;
            blUpdate.WorkerReportsProgress = true;
            blUpdate.DoWork += new DoWorkEventHandler(
        delegate (object o, DoWorkEventArgs args)
        {
            try
            {
                XmlNodeList list = blacklist.getXMLContent("Blacklist");                

                int count = list.Count;
                int i = 0;                
                foreach (XmlNode xn in list)
                {
                    try
                    {
                        string URL = xn["URL"].InnerText;
                        string Title = xn["Title"].InnerText;
                        string ThreadID = xn["ThreadID"].InnerText;

                        WebClient wc = Proxy.setProxy();
                        byte[] bytes = wc.DownloadData(URL);

                        MemoryStream img = new MemoryStream(bytes);
                        memoryStreamImage = System.Drawing.Image.FromStream(img);

                        this.Invoke((MethodInvoker)delegate
                        {
                            blacklistDataGrid.Rows.Add(new Bitmap(memoryStreamImage, new Size(100, 100)), Title, dataGridNumber, ThreadID, URL);
                        });

                        img.Dispose();
                        img.Close();

                        i++;

                        int percent = i * 100 / (count);

                        this.Invoke((MethodInvoker)delegate
                        {
                            blacklistProgress.Value = percent;
                        });
                        wc.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logging.LogMessageToFile("Unexpected Error: " + ex.Message);
                    }
                }

                if(blacklistProgress.Value == 100)
                {
                    // this.blacklistDataGrid.Sort(this.blacklistDataGrid.Columns[2], ListSortDirection.Descending);
                    this.Invoke((MethodInvoker)delegate
                    {
                        blacklistProgress.Value = 0;
                        Logging.LogMessageToFile("Blacklisted wallpapers loaded.");
                    });
                }
            }
            catch (Exception ex)
            {
                Logging.LogMessageToFile("Error refreshing blacklist: " + ex.Message);
            }

        });           
            blUpdate.RunWorkerAsync();
        }

        //======================================================================
        // Remove a previously blacklisted wallpaper
        //======================================================================
        private void unblacklistWallpaper_Click(object sender, EventArgs e)
        {
            String url = (blacklistDataGrid.Rows[currentMouseOverRow].Cells[4].Value.ToString());
            blacklist.removeEntry(url);
            populateBlacklistHistory();
        }

        //======================================================================
        // Select multi wallpaper type
        //======================================================================
        private void comboType_SelectedValueChanged(object sender, EventArgs e)
        {
            int screens = Screen.AllScreens.Count();

            if (comboType.Text == "Tiled")
            { 

            }
            else if(comboType.Text == "Centered")
            {

            }
            else if(comboType.Text == "Fill")
            {

            }
            else if(comboType.Text == "Stretched")
            { 

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
            catch { }
        }

        //======================================================================
        // Donation button
        //======================================================================
        private void btnDonate_Click(object sender, EventArgs e)
        {    
            System.Diagnostics.Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=S9YSLJS5DXDT8");
        }

        //======================================================================
        // Auto save all wallpapers
        //======================================================================
        private void AutoSave()
        {
            try
            {
                if (!File.Exists(Properties.Settings.Default.defaultSaveLocation + @"\" + Properties.Settings.Default.currentWallpaperName))
                {

                    System.IO.File.Copy(Properties.Settings.Default.currentWallpaperFile, Properties.Settings.Default.defaultSaveLocation + @"\" + Properties.Settings.Default.currentWallpaperName);
                    Logging.LogMessageToFile("Auto saved " + Properties.Settings.Default.currentWallpaperName + " to " + Properties.Settings.Default.defaultSaveLocation);
                }
                else
                {
                    Logging.LogMessageToFile("Not auto saving " + Properties.Settings.Default.currentWallpaperName + " because it already exists.");  
                }
            }
            catch (Exception Ex)
            {
                taskIcon.BalloonTipIcon = ToolTipIcon.Error;
                taskIcon.BalloonTipTitle = "Error Saving!";
                taskIcon.BalloonTipText = "Unable to automatically save the wallpaper. :(";
                taskIcon.ShowBalloonTip(750);
                Logging.LogMessageToFile("Error automatically saving wallpaper: " + Ex.Message);
            }

        }

        //======================================================================
        // Enable Active Desktop for wallpaper fade effect
        //======================================================================
        public static void ActiveDesktop()
        {
            IntPtr result = IntPtr.Zero;
            SendMessageTimeout(FindWindow("Progman", IntPtr.Zero), 0x52c, IntPtr.Zero, IntPtr.Zero, 0, 500, out result);

        }
    }
}
