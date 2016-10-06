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
using System.Xml.Linq;

namespace Reddit_Wallpaper_Changer
{
    public partial class RWC : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SystemParametersInfo(UInt32 uiAction, UInt32
        uiParam, String pvParam, UInt32 fWinIni);
        private static UInt32 SPI_SETDESKWALLPAPER = 20;
        private static UInt32 SPIF_UPDATEINIFILE = 0x0001;
        private static UInt32 SPIF_SENDWININICHANGE = 0x0002;

        public static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };
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

        public RWC()
        {
            InitializeComponent();
            SystemEvents.PowerModeChanged += OnPowerChange;
        }

        //======================================================================
        // Wallpaper layout styles
        //======================================================================
        public enum Style : int
        {
            Tiled,
            Centered,
            Stretched
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
            Logging.LogMessageToFile("RWC is starting.");
            r = new Random();
            taskIcon.Visible = true;
            setupSavedWallpaperLocation();
            setupLogging();
            setupProxySettings();
            setupButtons();
            setupPanels();
            Logging.LogMessageToFile("RWC Interface Loaded.");
            setupOthers();
            setupForm();
            deleteOldVersion();
            Xml.createXML();
            populateBlacklistHistory();
            updateStatus("RWC Setup Initated.");
            checkInternetTimer.Enabled = true;
        }

        //======================================================================
        // Set folder path for saving wallpapers
        //======================================================================
        private void setupSavedWallpaperLocation()
        {
            if (Properties.Settings.Default.defaultSaveLocation == "")
            {
                System.IO.Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "Saved Wallpapers");
                Properties.Settings.Default.defaultSaveLocation = AppDomain.CurrentDomain.BaseDirectory + "Saved Wallpapers";
                Properties.Settings.Default.Save();
            }

            txtSavePath.Text = Properties.Settings.Default.defaultSaveLocation;
            Logging.LogMessageToFile("Save location for wallpapers set to " + Properties.Settings.Default.defaultSaveLocation);
        }

        //======================================================================
        // Setup the logging check box
        //======================================================================
        private void setupLogging()
        {
            chkLogging.Checked = Properties.Settings.Default.logging;
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
        // Delete old executalbe after an update
        //======================================================================
        private void deleteOldVersion()
        {
            try
            {
                File.Delete(System.Reflection.Assembly.GetExecutingAssembly().Location + ".old");
            }
            catch
            {

            }
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
                Logging.LogMessageToFile("Proxy Enabled: " + Properties.Settings.Default.proxyAddress);

                if (Properties.Settings.Default.proxyAuth == true)
                {
                    chkAuth.Enabled = true;
                    chkAuth.Checked = true;
                    txtUser.Enabled = true;
                    txtUser.Text = Properties.Settings.Default.proxyUser;
                    txtPass.Enabled = true;
                    txtPass.Text = Properties.Settings.Default.proxyPass;
                    Logging.LogMessageToFile("Proxy Username: " + Properties.Settings.Default.proxyUser);
                    Logging.LogMessageToFile("Proxy Password: **********");
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

            //Set the Wallpaper Type to Random
            wallpaperGrabType.SelectedIndex = Properties.Settings.Default.wallpaperGrabType;
            Logging.LogMessageToFile("Wallpaper Grab Type: " + Properties.Settings.Default.wallpaperGrabType);
            //Setting the Subreddit Textbox to the default Settings
            subredditTextBox.Text = Properties.Settings.Default.subredditsUsed;
            Logging.LogMessageToFile("Selected Subreddits: " + Properties.Settings.Default.subredditsUsed);
            //Set the Search Query text
            searchQuery.Text = Properties.Settings.Default.searchQuery;
            Logging.LogMessageToFile("Search Query: " + Properties.Settings.Default.searchQuery);
            //Set the Time Value
            changeTimeValue.Value = Properties.Settings.Default.changeTimeValue;
            //Set the Time Type
            changeTimeType.SelectedIndex = Properties.Settings.Default.changeTimeType;
            //Set the current version for update check and label set.
            currentVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
            versionLabel.Text = currentVersion;

            //Set the Tray checkbox up
            startInTrayCheckBox.Checked = Properties.Settings.Default.startInTray;
            Logging.LogMessageToFile("Start In Tray: " + Properties.Settings.Default.startInTray);
            autoStartCheckBox.Checked = Properties.Settings.Default.autoStart;
            Logging.LogMessageToFile("Auto Start: " + Properties.Settings.Default.autoStart);
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
                    taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                    taskIcon.BalloonTipTitle = "Reddit Wallpaper Changer";
                    taskIcon.BalloonTipText = "RWC is up to date! :)";
                    taskIcon.ShowBalloonTip(700);
                }
            }
            catch
            {
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
        }

        //======================================================================
        // Save button code
        //======================================================================
        private void saveData()
        {
            bool updateTimerBool = false;
            if (Properties.Settings.Default.autoStart != autoStartCheckBox.Checked)
            {
                startup(autoStartCheckBox.Checked);
            }

            Properties.Settings.Default.startInTray = startInTrayCheckBox.Checked;
            Properties.Settings.Default.autoStart = autoStartCheckBox.Checked;
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
            Properties.Settings.Default.logging = chkAuth.Checked;
            Properties.Settings.Default.Save();
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
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerSupportsCancellation = true;
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(
        delegate(object o, DoWorkEventArgs args)
        {
            if (noResultCount >= 20)
            {
                noResultCount = 0;
                MessageBox.Show("No Results After 20 Retries. Disabling RWC. Try a different query?", "Reddit Wallpaper Changer: Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                updateStatus("RWC Disabled.");
                changeWallpaperTimer.Enabled = false;
                return;
            }
            updateStatus("Finding New Wallpaper");
            Random random = new Random();
            string[] randomT = { "&t=day", "&t=year", "&t=all", "&t=month", "&t=week" };
            string[] randomSort = { "&sort=relevance", "&sort=hot", "&sort=top", "&sort=comments" };
            string query = HttpUtility.UrlEncode(Properties.Settings.Default.searchQuery) + "+self%3Ano+((url%3A.png+OR+url%3A.jpg+OR+url%3A.jpeg)+OR+(url%3Aimgur.png+OR+url%3Aimgur.jpg+OR+url%3Aimgur.jpeg)+OR+(url%3Adeviantart))";
            String formURL = "http://www.reddit.com/r/";
            String subreddits = Properties.Settings.Default.subredditsUsed.Replace(" ", "").Replace("www.reddit.com/", "").Replace("reddit.com/", "").Replace("http://", "").Replace("/r/", "");
            if (subreddits.Equals(""))
            {
                formURL += "all";

            }
            else
            {
                if (subreddits.Contains("/m/"))
                {
                    formURL = "http://www.reddit.com/" + subreddits.Replace("http://", "").Replace("https://", "").Replace("user/", "u/");
                }
                else
                {
                    formURL += subreddits;
                }

            }
            int wallpaperGrabType = Properties.Settings.Default.wallpaperGrabType;
            switch (wallpaperGrabType)
            {
                case 0:
                    formURL += "/search.json?q=" + query + randomSort[random.Next(0, 4)] + randomT[random.Next(0, 5)] + "&restrict_sr=on";
                    break;
                case 1:
                    formURL += "/search.json?q=" + query + "&sort=new&restrict_sr=on";
                    break;
                case 2:
                    formURL += "/search.json?q=" + query + "&sort=hot&restrict_sr=on&t=day";

                    break;
                case 3:
                    formURL += "/search.json?q=" + query + "&sort=top&restrict_sr=on&t=hour";
                    break;
                case 4:
                    formURL += "/search.json?q=" + query + "&sort=top&restrict_sr=on&t=day";
                    break;
                case 5:
                    formURL += "/search.json?q=" + query + "&sort=top&restrict_sr=on&t=week";
                    break;
                case 6:
                    formURL += "/search.json?q=" + query + "&sort=top&restrict_sr=on&t=month";
                    break;
                case 7:
                    formURL += "/search.json?q=" + query + "&sort=top&restrict_sr=on&t=year";
                    break;
                case 8:
                    formURL += "/search.json?q=" + query + "&sort=top&restrict_sr=on&t=all";
                    break;
                case 9:
                    formURL += "/random.json?p=" + (System.Guid.NewGuid().ToString());
                    break;
            }

            String jsonData = "";
            bool failedDownload = false;
            // Console.WriteLine(formURL);
            using (WebClient client = new WebClient())
            {
                client.Proxy = null;

                // Use a proxy if specified
                if (Properties.Settings.Default.useProxy == true)
                {
                    WebProxy proxy = new WebProxy(Properties.Settings.Default.proxyAddress);

                    if (Properties.Settings.Default.proxyAuth == true)
                    {
                        proxy.Credentials = new NetworkCredential(Properties.Settings.Default.proxyUser, Properties.Settings.Default.proxyPass);
                        proxy.UseDefaultCredentials = false;
                        proxy.BypassProxyOnLocal = false;
                    }

                    client.Proxy = proxy;
                }
               
                try
                {
                    jsonData = client.DownloadString(formURL);

                }
                catch (System.Net.WebException Ex)
                {
                    if (Ex.Message == "The remote server returned an error: (503) Server Unavailable.")
                    {
                        updateStatus("Reddit Server Unavailable, try again later.");
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
                                Console.WriteLine("redditResult Count = 0, Changing Wallpaper.");
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

                                setWallpaper(token["data"]["url"].ToString(), token["data"]["title"].ToString(), token["data"]["id"].ToString());
                            }
                            else
                            {
                                token = redditResult.ElementAt(random.Next(0, redditResult.Count() - 1));
                                currentThread = "http://reddit.com" + token["data"]["permalink"].ToString();
                                setWallpaper(token["data"]["url"].ToString(), token["data"]["title"].ToString(), token["data"]["id"].ToString());

                            }
                        }

                    }
                    catch (System.InvalidOperationException)
                    {
                        updateStatus("Your query is bringing up no results.");
                        failedDownload = true;
                        breakBetweenChange.Enabled = true;
                    }


                }
                else
                {
                    breakBetweenChange.Enabled = true;
                }
            }
            catch (JsonReaderException)
            {
                breakBetweenChange.Enabled = true;

            }

        });

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
            XDocument xml = XDocument.Load("Blacklist.xml");
            var list = xml.Descendants("URL").Select(x => x.Value).ToList();

            if (list.Contains(url))
            {
                updateStatus("Wallpaper is blacklisted.");
                changeWallpaperTimer.Enabled = false;
                changeWallpaper();
                return;
            }

            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler(
        delegate(object o, DoWorkEventArgs args)
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
                Properties.Settings.Default.url = url;
                Properties.Settings.Default.threadTitle = title;
                Properties.Settings.Default.currentWallpaperName = title + extention;
                Properties.Settings.Default.threadID = threadID;
                Properties.Settings.Default.Save();

                if (ImageExtensions.Contains(extention.ToUpper()))
                {
                    if (System.IO.File.Exists(wallpaperFile))
                    {
                        try
                        {
                            System.IO.File.Delete(wallpaperFile);
                        }
                        catch (System.IO.IOException)
                        {

                        }
                    }
                    try
                    {
                        WebClient webClient = Proxy.setProxy();
                        webClient.DownloadFile(uri.AbsoluteUri, @wallpaperFile);
                        SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, @wallpaperFile, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
                        historyRepeated.Add(threadID);
                        noResultCount = 0;
                        BeginInvoke((MethodInvoker)delegate
                        {
                            updateStatus("Wallpaper Changed");
                        });
                    }
                    catch (System.Net.WebException)
                    {

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
                catch (ArgumentException)
                {
                    dataGridNumber += 1;
                    SetGrid(null, title, dataGridNumber, threadID, url);
                    historyDataGrid.Rows[0].Visible = false;
                    breakBetweenChange.Enabled = true;
                }

                wc.Dispose();
            }
        });
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
            if (startInTrayCheckBox.Checked)
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
                toolTip1.Active = false;
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

            }
            else
            {
                statusMenuItem1.ForeColor = Color.Red;
                statusMenuItem1.Text = "Not Running";

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
                currentWallpaper.Save(Properties.Settings.Default.defaultSaveLocation + @"\" + Properties.Settings.Default.currentWallpaperName);
                taskIcon.BalloonTipIcon = ToolTipIcon.Info;
                taskIcon.BalloonTipTitle = "Wallpaper Saved!";
                taskIcon.BalloonTipText = "Wallpaper saved to " + Properties.Settings.Default.defaultSaveLocation + @"\" + Properties.Settings.Default.currentWallpaperName;
                taskIcon.ShowBalloonTip(750); 
            }
            catch (Exception)
            {
                taskIcon.BalloonTipIcon = ToolTipIcon.Error;
                taskIcon.BalloonTipTitle = "Error Saving!";
                taskIcon.BalloonTipText = "Unable to save the wallpaper locally. :(";
                taskIcon.ShowBalloonTip(750);
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
            }
            else
            {
                updateStatus("Network Unavaliable. Rechecking.");
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
                this.monitorLayoutPanel.ColumnStyles.Clear();
                this.monitorLayoutPanel.ColumnCount = screens;            
                this.monitorLayoutPanel.AutoSize = true;

                int z = 0;
                foreach (var screen in Screen.AllScreens.OrderBy(i => i.Bounds.X))
                {
                    var percent = 100f / screens;
                    this.monitorLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, percent));
                    
                    Button monitor = new Button
                    {
                        Name = "Monitor" + z,
                        Size = new Size(95, 75),
                        BackgroundImageLayout = ImageLayout.Stretch,                                                  
                        BackgroundImage = Properties.Resources.display_enabled,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font("Segoe UI", 10, FontStyle.Bold),
                        ForeColor = Color.White,
                        BackColor = Color.Transparent,
                        Text = screen.Bounds.Width + "x" + screen.Bounds.Height,
                        Anchor = System.Windows.Forms.AnchorStyles.None
                    };
                    

                    this.monitorLayoutPanel.Controls.Add(monitor, z, 0);
                    z++;
                    monitor.MouseClick += new MouseEventHandler(monitor_Click);
                }
            }
        }

        //======================================================================
        // Change monitor colour based on click
        //======================================================================
        private void monitor_Click(object sender, MouseEventArgs e)
        {
            if (((Button)sender).BackgroundImage == Properties.Resources.display_disabled)
            {
                ((Button)sender).BackgroundImage = Properties.Resources.display_enabled;
            }
            else
            {
                ((Button)sender).BackgroundImage = Properties.Resources.display_disabled;
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


        //======================================================================
        // Add current wallpaper to favourites
        //======================================================================
        public void Favourite()
        {
            XDocument doc = XDocument.Load(AppDomain.CurrentDomain.BaseDirectory + "Favourites.xml");
            XElement favourite = doc.Element("Favourites");
            favourite.Add(new XElement("Wallpaper",
                new XElement("URL", "http://some.wallpaper/link.jpeg"),
                new XElement("Title", "Another Wallpaper!"),
                new XElement("ThreadID", "Thread ID here")));
            doc.Save("Favourites.xml");

            faveWallpaperMenuItem.Checked = true;

            taskIcon.BalloonTipIcon = ToolTipIcon.Info;
            taskIcon.BalloonTipTitle = "Favourite Wallpaper!";
            taskIcon.BalloonTipText = "The current Wallpaper has been added to your favourites successfully!";
            taskIcon.ShowBalloonTip(750);
        }

        //======================================================================
        //Add current wallpaper to blacklist
        //======================================================================
        public void Blacklist()
        {
            XDocument doc = XDocument.Load(AppDomain.CurrentDomain.BaseDirectory + "Blacklist.xml");
            XElement blacklist = doc.Element("Blacklisted");
            blacklist.Add(new XElement("Wallpaper",
                new XElement("URL", Properties.Settings.Default.url),
                new XElement("Title", Properties.Settings.Default.threadTitle),
                new XElement("ThreadID", Properties.Settings.Default.threadID)));
            doc.Save("Blacklist.xml");

            taskIcon.BalloonTipIcon = ToolTipIcon.Info;
            taskIcon.BalloonTipTitle = "Wallpaper Blacklisted!";
            taskIcon.BalloonTipText = "The current Wallpaper has been blacklisted! Finding a new wallpaper...";
            taskIcon.ShowBalloonTip(750);

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
            XDocument doc = XDocument.Load(AppDomain.CurrentDomain.BaseDirectory + "Blacklist.xml");
            XElement blacklist = doc.Element("Blacklisted");
            blacklist.Add(new XElement("Wallpaper",
                new XElement("URL", url),
                new XElement("Title", title),
                new XElement("ThreadID", threadid)));
            doc.Save(AppDomain.CurrentDomain.BaseDirectory + "Blacklist.xml");

            taskIcon.BalloonTipIcon = ToolTipIcon.Info;
            taskIcon.BalloonTipTitle = "Wallpaper Blacklisted!";
            taskIcon.BalloonTipText = "The historical Wallpaper has been blacklisted!";
            taskIcon.ShowBalloonTip(750);

            populateBlacklistHistory();
        }

        //======================================================================
        // Click on favourite menu
        //======================================================================
        private void faveWallpaperMenuItem_Click(object sender, EventArgs e)
        {
            Favourite();
        }

        //======================================================================
        // Click on blacklist menu
        //======================================================================
        private void blockWallpaperMenuItem_Click(object sender, EventArgs e)
        {
            Blacklist();
        }

        //======================================================================
        // Set wallpaper from selected histore entry
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
            Logging.LogMessageToFile("Refreshing Blacklisted wallpapers.");
            blacklistDataGrid.Rows.Clear();
            BackgroundWorker blUpdate= new BackgroundWorker();
            blUpdate.WorkerSupportsCancellation = true;
            blUpdate.WorkerReportsProgress = true;
            blUpdate.DoWork += new DoWorkEventHandler(
        delegate (object o, DoWorkEventArgs args)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(AppDomain.CurrentDomain.BaseDirectory + "Blacklist.xml");
                XmlNodeList list = doc.SelectNodes("Blacklisted/Wallpaper");

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
                    catch
                    {
                    }
                }

                if(blacklistProgress.Value == 100)
                {
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
            string path = AppDomain.CurrentDomain.BaseDirectory + "Blacklist.xml";
            string url = (blacklistDataGrid.Rows[currentMouseOverRow].Cells[4].Value.ToString());
            // string title = (blacklistDataGrid.Rows[currentMouseOverRow].Cells[1].Value.ToString());
            // string threadid = (blacklistDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString());

            try
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(path);
                foreach (XmlNode node in xml.SelectNodes("Blacklisted/Wallpaper"))
                {
                    if (node.SelectSingleNode("URL").InnerText == url)
                    {
                        node.ParentNode.RemoveChild(node);
                    }
                }

                xml.Save(path);
                populateBlacklistHistory();
            }
            catch
            {

            }   
        }
    }
}
