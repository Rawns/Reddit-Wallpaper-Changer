using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Net;
using System.Web;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Collections;
using Microsoft.Win32;
namespace Reddit_Wallpaper_Changer
{
    public partial class RWC : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SystemParametersInfo(UInt32 uiAction, UInt32
        uiParam, String pvParam, UInt32 fWinIni);
        private static UInt32 SPI_SETDESKWALLPAPER = 20;
        //private static UInt32 SPIF_SENDCHANGE = 0x02;
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
        //Dictionary<string, string> historyRepeated = new Dictionary<string, string>();

        public RWC()
        {
            InitializeComponent();
            SystemEvents.PowerModeChanged += OnPowerChange;
        }


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
            updateStatus("RWC Setup Initating.");
            r = new Random();
            taskIcon.Visible = true;
            setupSavedWallpaperLocation();
            setupProxy();
            setupButtons();
            setupPanels();
            setupOthers();
            setupForm();
            deleteOldVersion();
            //createWindowsMenu();
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
        }

        //======================================================================
        // ???
        //======================================================================
        private void deleteWindowsMenu()
        {
            //throw new NotImplementedException();
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
        // Delete old version after an update
        //======================================================================
        private void deleteOldVersion()
        {
            File.Delete(System.Reflection.Assembly.GetExecutingAssembly().Location + ".old");
        }

        public void changeSearchQuery(string text)
        {
            searchQuery.Text = text;
        }

        //======================================================================
        // Set proxy settings if configured
        //======================================================================
        private void setupProxy()
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
        // Set up other aspects
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
            //Setting the Subreddit Textbox to the default Settings
            subredditTextBox.Text = Properties.Settings.Default.subredditsUsed;
            //Set the Search Query text
            searchQuery.Text = Properties.Settings.Default.searchQuery;
            //Set the Time Value
            changeTimeValue.Value = Properties.Settings.Default.changeTimeValue;
            //Set the Time Type
            changeTimeType.SelectedIndex = Properties.Settings.Default.changeTimeType;
            //Set the current version for update check and label set.
            currentVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
            versionLabel.Text = currentVersion;

            //Set the Tray checkbox up
            startInTrayCheckBox.Checked = Properties.Settings.Default.startInTray;
            autoStartCheckBox.Checked = Properties.Settings.Default.autoStart;

        }

        //======================================================================
        // Setup the different panels
        //======================================================================
        private void setupPanels()
        {
            aboutPanel.Size = new Size(364, 405);
            configurePanel.Size = new Size(364, 405);
            monitorPanel.Size = new Size(364, 405);
        }

        //======================================================================
        // Setup the different buttons
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


            selectedPanel = configurePanel;
            selectedButton = configureButton;
        }

        public void DrawLShapeLine(System.Drawing.Graphics g, int intMarginLeft, int intMarginTop, int intWidth, int intHeight)
        {

            Pen myPen = new Pen(Color.DarkGray);
            myPen.Width = 1;
            // Create array of points that define lines to draw. 
            Point[] points = 
         { 
            new Point(intMarginLeft, intHeight + intMarginTop), 
            new Point(intMarginLeft + intWidth, intMarginTop + intHeight), 
         };

            g.DrawLines(myPen, points);
        }

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
                //selectPanel(selectedPanel);
            }

        }

        private void selectPanel(Panel selectedPanel)
        {
            selectedPanel.Location = new Point(0, 57);
            selectedPanel.Size = new Size(365, 331);
        }

        private void configurePanel_Paint(object sender, PaintEventArgs e)
        {
            DrawLShapeLine(e.Graphics, 0, 14, 370, -14);
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
        // Set sellected button formatting
        //======================================================================
        private void selectButton(Button button2)
        {
            button2.BackColor = selectedBackColor;
            button2.FlatAppearance.BorderColor = selectedBorderColor;
            button2.FlatAppearance.MouseDownBackColor = selectedBackColor;
            button2.FlatAppearance.MouseOverBackColor = selectedBackColor;
        }

        //======================================================================
        // Set unsellected button formatting
        //======================================================================
        private void cleanButton(Button button)
        {
            button.BackColor = Color.White;
            button.FlatAppearance.BorderColor = Color.White;
            button.FlatAppearance.MouseDownBackColor = Color.White;
            button.FlatAppearance.MouseOverBackColor = Color.White;
        }

        private void aboutPanel_Paint(object sender, PaintEventArgs e)
        {
            DrawLShapeLine(e.Graphics, 0, 14, 370, -14);

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

            using (WebClient client = new WebClient())
            { 
                client.Proxy = null;
                
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
                    String latestVersion = client.DownloadString("https://raw.githubusercontent.com/Rawns/RWC-Source/master/version");

                    if (!latestVersion.ToString().Contains(currentVersion.Trim().ToString()))
                    {
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
                        taskIcon.Icon = Properties.Resources.rwc;
                        taskIcon.BalloonTipTitle = "Reddit Wallpaper Changer";
                        taskIcon.BalloonTipText = "RWC is up to date! :)";
                        taskIcon.ShowBalloonTip(700);

                        // MessageBox.Show("RWC is up to date. :)", "Reddit Wallpaper Changer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch(Exception ex)
                {
                    taskIcon.BalloonTipIcon = ToolTipIcon.Error;
                    taskIcon.BalloonTipTitle = "Reddit Wallpaper Changer";
                    taskIcon.BalloonTipText = "Error checking for updates! :(";
                    taskIcon.ShowBalloonTip(700);

                    // MessageBox.Show("Error checking for updates: " + ex.Message + " :(", "Reddit Wallpaper Changer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                client.Dispose();
                btnUpdate.Text = "Check For Updates";
                btnUpdate.Enabled = true;
            }
        }


        //======================================================================
        // Open the Subreddit
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
            //this.Visible = false;
            //taskIcon.Visible = true;
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
            Properties.Settings.Default.Save();
            if (updateTimerBool)
                updateTimer();
            setupProxy();

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

        private void wallpaperChangeTimer_Tick(object sender, EventArgs e)
        {
            changeWallpaperTimer.Enabled = true;
        }

        //======================================================================
        // Change the wallpaper
        //======================================================================
        private void changeWallpaper()
        {
            BackgroundWorker bw = new BackgroundWorker();
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
                catch (System.Net.WebException e)
                {
                    if (e.Message == "The remote server returned an error: (503) Server Unavailable.")
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
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(
        delegate(object o, DoWorkEventArgs args)
        {
            Uri uri2 = new Uri(url);
            string extention2 = System.IO.Path.GetExtension(uri2.LocalPath);

            contextMenuStrip2.Hide();
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
                Properties.Settings.Default.currentWallpaperName = title + extention;
                Properties.Settings.Default.Save();

                //MessageBox.Show(url);
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
                        WebClient webClient = new WebClient();
                        webClient.Proxy = null;

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

                            webClient.Proxy = proxy;
                        }

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
            WebClient wc = new WebClient();
            wc.Proxy = null;

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

                wc.Proxy = proxy;
            }

            byte[] bytes = wc.DownloadData(url);
            //Console.WriteLine(bytes.Count().ToString());
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
            }
        });

            //    bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
            //delegate(object o, RunWorkerCompletedEventArgs args)
            //{
            //    MessageBox.Show("Finished");
            //});
            bw.RunWorkerAsync();
        }

        delegate void SetGridCallback(Bitmap img, string title, int dataGridNumber, string threadID, string url);

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

        private void Form1_Shown(object sender, EventArgs e)
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
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!realClose)
            {
                e.Cancel = true;
                fakeClose(true);
            }
            else
            {
                toolTip1.Active = false;
                deleteWindowsMenu();
            }
        }

        //======================================================================
        // Restore from system tray
        //======================================================================
        private void taskIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Visible = true;

        }

        private void taskIcon_BalloonTipClicked(object sender, EventArgs e)
        {

        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Visible = true;


        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            realClose = true;
            Application.Exit();
        }

        private void statusMenuItem1_Click(object sender, EventArgs e)
        {
            statusMenuItem1.Checked = !statusMenuItem1.Checked;
            wallpaperChangeTimer.Enabled = statusMenuItem1.Checked;

            if (statusMenuItem1.Checked)
            {
                statusMenuItem1.ForeColor = Color.YellowGreen;
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

        private void changeWallpaperMenuItem_Click(object sender, EventArgs e)
        {
            wallpaperChangeTimer.Enabled = false;
            wallpaperChangeTimer.Enabled = true;

            changeWallpaperTimer.Enabled = true;
        }

        private void startupTimer_Tick(object sender, EventArgs e)
        {
            startupTimer.Enabled = false;


            //Update Check
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
                    String latestVersion = client.DownloadString("https://raw.githubusercontent.com/Rawns/RWC-Source/master/version");
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

                }

                client.Dispose();
            }
        }

        public void changeWallpaperTimerEnabled()
        {
            changeWallpaperTimer.Enabled = true;

        }
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

        private void historyPanel_Paint(object sender, PaintEventArgs e)
        {
            DrawLShapeLine(e.Graphics, 0, 14, 370, -14);

        }

        private void changeWallpaperTimer_Tick(object sender, EventArgs e)
        {

            changeWallpaperTimer.Enabled = false;
            changeWallpaper();
        }

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

        private void saveWallpaper_FileOk(object sender, CancelEventArgs e)
        {
            string fileName = saveWallpaper.FileName;
            currentWallpaper.Save(fileName);
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

                //Internet is working.
            }
            else
            {
                updateStatus("Network Unavaliable. Rechecking.");
            }
        }

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
        // Draw monitor panel
        //======================================================================
        private void monitorPanel_Paint(object sender, PaintEventArgs e)
        {
            DrawLShapeLine(e.Graphics, 0, 14, 370, -14);
            if (!monitorsCreated)
            {
                monitorsCreated = true;
                int negativeSpaceX = 0;
                foreach (var screen in Screen.AllScreens)
                {
                    Label monLabel = new Label();
                    //MessageBox.Show(screen.Bounds.ToString());
                    if (screen.Bounds.X < 0)
                    {
                        negativeSpaceX = Math.Abs(screen.Bounds.X / 20);
                    }
                    monLabel.Location = new Point(negativeSpaceX + (screen.Bounds.X / 20), screen.Bounds.Y / 20);
                    monLabel.AutoSize = false;
                    monLabel.BorderStyle = BorderStyle.FixedSingle;
                    monLabel.BackColor = Color.Green;
                    monLabel.Size = new Size(screen.Bounds.Width / 20, screen.Bounds.Height / 20);
                    monLabel.Text = screen.DeviceName + " [" + screen.Bounds.Width + "x" + screen.Bounds.Height + "]";
                    monLabel.Cursor = Cursors.Hand;
                    monitorPanel.Controls.Add(monLabel);
                    monLabel.MouseClick += new MouseEventHandler(labelclick);

                }
            }
        }

        private void labelclick(object sender, MouseEventArgs e)
        {
            if (((Label)sender).BackColor == Color.Red)
            {
                ((Label)sender).BackColor = Color.Green;
            }
            else
            {
                ((Label)sender).BackColor = Color.Red;
            }
        }

        private void historyDataGrid_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                currentMouseOverRow = historyDataGrid.HitTest(e.X, e.Y).RowIndex;
                if (currentMouseOverRow >= 0)
                {
                    contextMenuStrip2.Show(historyDataGrid, new Point(e.X, e.Y));
                }
                else
                {
                    contextMenuStrip1.Show(historyDataGrid, new Point(e.X, e.Y));
                }

            }
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
        }

        private void contextMenuStrip2_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

            string url = (historyDataGrid.Rows[currentMouseOverRow].Cells[4].Value.ToString());
            string title = (historyDataGrid.Rows[currentMouseOverRow].Cells[1].Value.ToString());
            string threadid = (historyDataGrid.Rows[currentMouseOverRow].Cells[3].Value.ToString());
            setWallpaper(url, title, threadid);

        }

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

        private void faveWallpaperMenuItem_Click(object sender, EventArgs e)
        {

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
    }
}
