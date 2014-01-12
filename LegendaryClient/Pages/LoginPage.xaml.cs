using jabber.client;
using jabber.connection;
using LegendaryClient.Elements;
using LegendaryClient.Logic;
using LegendaryClient.Logic.JSON;
using LegendaryClient.Logic.Region;
using LegendaryClient.Logic.Riot;
using LegendaryClient.Logic.Riot.Platform;
using LegendaryClient.Logic.SQLite;
using LegendaryClient.Properties;
using Microsoft.Win32;
using RtmpSharp.IO;
using RtmpSharp.Net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LegendaryClient.Pages
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        private string PlayerRegion = "";
        private string InstallLocation = "";
        private FadeLabel LastRegionLabel;
        private BackgroundWorker worker = new BackgroundWorker();

        public LoginPage()
        {
            InitializeComponent();

            PlayerRegion = Settings.Default.Region;

            #region Render Regions
            List<BaseRegion> Regions = Client.GetInstances<BaseRegion>();
            Regions.Sort((x, y) => y.RegionName.CompareTo(x.RegionName));

            //Move the next region Margin pixels (is calculated each time a label is rendered)
            double Margin = 5;
            foreach (BaseRegion region in Regions)
            {
                FadeLabel RegionLabel = new FadeLabel(89, 168);
                RegionLabel.Content = region.RegionName;
                RegionLabel.HorizontalAlignment = HorizontalAlignment.Right;
                RegionLabel.VerticalAlignment = VerticalAlignment.Bottom;
                RegionLabel.Margin = new Thickness(0, 0, Margin, 5);
                Size FontSize = Client.MeasureTextSize(region.RegionName, RegionLabel.FontFamily, RegionLabel.FontStyle, RegionLabel.FontWeight, RegionLabel.FontStretch, RegionLabel.FontSize);
                Margin += FontSize.Width + 5;
                //If the region is the initial region
                if (PlayerRegion == region.RegionName)
                {
                    LastRegionLabel = RegionLabel;
                    LastRegionLabel.KeepColor = true;
                    var converter = new BrushConverter();
                    RegionLabel.Foreground = (Brush)converter.ConvertFromString("#FF595959");
                }
                RegionLabel.MouseDown += RegionLabel_MouseDown;
                LoginInfoGrid.Children.Add(RegionLabel);
            }
            #endregion Render Regions

            worker.WorkerSupportsCancellation = true;
            worker.DoWork += DoLogin;
            worker.RunWorkerCompleted += OnLoginComplete;

            if (Settings.Default.Password != null)
            {
                byte[] encrypted = (byte[])Settings.Default.Password;
                byte[] unencrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                UsernameTextBox.Text = Settings.Default.Username;
                PasswordBox.Password = Encoding.Unicode.GetString(unencrypted);
                LoginInfoGrid.Opacity = 0;
                LoginInfoGrid.Visibility = Visibility.Visible;
                LoginHandler(null, null);
            }

            LoginGrid.Opacity = 0;
            var fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.5));
            LoginGrid.BeginAnimation(Grid.OpacityProperty, fadeInAnimation);
        }

        private void DoLogin(object sender, DoWorkEventArgs e)
        {
            Client.Context = new ClientDataContext();
            Client.RunOnUIThread(new Action(() => StatusLabel.Content = "Logging in..."));
            BaseRegion SelectedRegion = BaseRegion.GetRegion(PlayerRegion);
            Client.Region = SelectedRegion;

            //Get current league version
            RiotPatcher patcher = new RiotPatcher(SelectedRegion, InstallLocation);
            patcher.GetDragon();

            #region Connect to PvP.Net
            var context = RiotCalls.RegisterObjects();
            Client.RtmpConnection = new RtmpClient(new Uri("rtmps://" + SelectedRegion.Server + ":2099"), context, ObjectEncoding.Amf3);
            Client.RtmpConnection.MessageReceived += Client.OnMessageReceived;
            Client.RtmpConnection.ConnectAsync().Wait();

            Client.RunOnUIThread(new Action(() => StatusLabel.Content = "Getting Authorization Key..."));

            AuthenticationCredentials newCredentials = new AuthenticationCredentials();
            Client.RunOnUIThread(new Action(() =>
            {
                newCredentials.Username = UsernameTextBox.Text;
                newCredentials.Password = PasswordBox.Password;
            }));

            newCredentials.ClientVersion = patcher.DDragonVersion;
            newCredentials.IpAddress = RiotCalls.GetIpAddress();
            newCredentials.Locale = SelectedRegion.Locale;
            newCredentials.Domain = "lolclient.lol.riotgames.com";
            try
            {
                newCredentials.AuthToken = RiotCalls.GetAuthKey(newCredentials.Username, newCredentials.Password, SelectedRegion.LoginQueue);
            }
            catch (Exception f)
            {
                Client.RunOnUIThread(new Action(() =>
                {
                    var fadeOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.5));
                    fadeOutAnimation.Completed += (x, y) =>
                    {
                        var fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.5));
                        LoginInfoGrid.BeginAnimation(Grid.OpacityProperty, fadeInAnimation);
                        LoginInfoGrid.Visibility = Visibility.Visible;
                        LoggingInGrid.Visibility = Visibility.Hidden;
                    };
                    LoggingInGrid.BeginAnimation(Grid.OpacityProperty, fadeOutAnimation);

                    if (f.Message.Contains("The remote name could not be resolved"))
                        HintLabel.Content = "Please make sure you are connected the internet!";
                    else if (f.Message.Contains("(403) Forbidden"))
                        HintLabel.Content = "Your username or password is incorrect!";
                    else
                        HintLabel.Content = f.Message;
                }));

                Client.RtmpConnection.Close();
                worker.CancelAsync();
                return;
            }

            Settings.Default.Username = newCredentials.Username;
            byte[] plainTextBytes = Encoding.Unicode.GetBytes(newCredentials.Password);
            byte[] encrypted = ProtectedData.Protect(plainTextBytes, null, DataProtectionScope.CurrentUser);
            Settings.Default.Password = encrypted;
            Settings.Default.Save();

            Client.RunOnUIThread(new Action(() => StatusLabel.Content = "Connecting to " + PlayerRegion + "..."));

            Client.RunOnUIThread(new Action(async () =>
            {
                Session login = await RiotCalls.Login(newCredentials);
                Client.PlayerSession = login;
            }));

            while (Client.PlayerSession == null) { }

            Client.RtmpConnection.SubscribeAsync("my-rtmps", "messagingDestination", "bc", "bc-" + Client.PlayerSession.AccountSummary.AccountId.ToString());
            Client.RtmpConnection.SubscribeAsync("my-rtmps", "messagingDestination", "gn-" + Client.PlayerSession.AccountSummary.AccountId.ToString(), "gn-" + Client.PlayerSession.AccountSummary.AccountId.ToString());
            Client.RtmpConnection.SubscribeAsync("my-rtmps", "messagingDestination", "cn-" + Client.PlayerSession.AccountSummary.AccountId.ToString(), "cn-" + Client.PlayerSession.AccountSummary.AccountId.ToString());

            Client.RunOnUIThread(new Action(() => StatusLabel.Content = "Retrieving Data..."));

            Client.RunOnUIThread(new Action(async () =>
            {
                bool LoggedIn = await Client.RtmpConnection.LoginAsync(UsernameTextBox.Text.ToLower(), Client.PlayerSession.Token);
                Client.LoginPacket = await RiotCalls.GetLoginDataPacketForUser();
            }));

            while (Client.LoginPacket == null) { }
            #endregion Connect to PvP.Net

            #region Patch
            Client.RunOnUIThread(new Action(() => StatusLabel.Content = "Checking version..."));
            Client.RunOnUIThread(new Action(() =>
            {
                StatusLabel.Content = "Patching...";
                var fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(1));
                PatchingGrid.BeginAnimation(Grid.OpacityProperty, fadeInAnimation);
            }));

            Client.RunOnUIThread(new Action(() =>
            {
                PatchProgressBar.Value += 2;
                PercentLabel.Content = PatchProgressBar.Value + "%";
            }));
            #endregion

            Client.RunOnUIThread(new Action(async () =>
            {
                StatusLabel.Content = "Loading data...";

                Client.SQLiteDatabase = new SQLite.SQLiteConnection("gameStats_en_US.sqlite");
                Client.Champions = (from s in Client.SQLiteDatabase.Table<champions>()
                                    orderby s.name
                                    select s).ToList();

                foreach (champions c in Client.Champions)
                {
                    string Source = Path.Combine(Client.ExecutingDirectory, "Assets", "champions", c.iconPath);
                    c.icon = Client.GetImage(Source);
                    Champions.InsertExtraChampData(c);
                }

                Client.ChampionSkins = (from s in Client.SQLiteDatabase.Table<championSkins>()
                                        orderby s.name
                                        select s).ToList();
                Client.Items = Items.PopulateItems();
                Client.Masteries = Masteries.PopulateMasteries();
                Client.Runes = Runes.PopulateRunes();

                Client.PlayerChampions = await RiotCalls.GetAvailableChampions();
                Client.GameConfigs = Client.LoginPacket.GameTypeConfigs;
            }));

            #region Chat
            Client.Players = new List<ChatPlayerItem>();
            Client.ChatClient = new JabberClient();
            Client.RunOnUIThread(new Action(() =>
            {
                Client.ChatClient.User = UsernameTextBox.Text;
                Client.ChatClient.Password = "AIR_" + PasswordBox.Password;
            }));

            while (String.IsNullOrEmpty(Client.ChatClient.User) || String.IsNullOrEmpty(Client.ChatClient.Password)) { }

            Client.ChatClient.AutoReconnect = 30;
            Client.ChatClient.KeepAlive = 10;
            Client.ChatClient.NetworkHost = Dns.GetHostAddresses("chat." + Client.Region.ChatName + ".lol.riotgames.com")[0].ToString();
            Client.ChatClient.Port = 5223;
            Client.ChatClient.Server = "pvp.net";
            Client.ChatClient.SSL = true;
            Client.ChatClient.OnInvalidCertificate += Client.ChatClient_OnInvalidCertificate;
            //Client.ChatClient.OnMessage += Client.ChatClient_OnMessage;
            Client.ChatClient.Connect();

            Client.RostManager = new RosterManager();
            Client.RostManager.Stream = Client.ChatClient;
            Client.RostManager.AutoSubscribe = true;
            Client.RostManager.AutoAllow = AutoSubscriptionHanding.AllowAll;
            Client.RostManager.OnRosterItem += Client.RostManager_OnRosterItem;
            Client.RostManager.OnRosterEnd += Client.RostManager_OnRosterEnd;

            Client.PresManager = new PresenceManager();
            Client.PresManager.Stream = Client.ChatClient;
            Client.PresManager.OnPrimarySessionChange += Client.PresManager_OnPrimarySessionChange;

            Client.ConfManager = new ConferenceManager();
            Client.ConfManager.Stream = Client.ChatClient;
            #endregion

            Client.RunOnUIThread(new Action(() =>
            {
                StatusLabel.Content = "Connected";
                var fadeInAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(1));
                PatchingGrid.BeginAnimation(Grid.OpacityProperty, fadeInAnimation);
            }));

            Thread.Sleep(500); //Pretend to do work
        }

        private void OnLoginComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            if (Client.LoginPacket != null)
            {
                var fadeOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.5));
                fadeOutAnimation.Completed += (x, y) =>
                {
                    Client.MainHolder.Content = new MainPage().Content;
                    var fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.5));
                    Client.MainHolder.BeginAnimation(ContentControl.OpacityProperty, fadeInAnimation);
                };
                Client.MainHolder.BeginAnimation(ContentControl.OpacityProperty, fadeOutAnimation);
            }
        }

        private void LoginHandler(object sender, RoutedEventArgs e)
        {
            if (!worker.IsBusy)
            {
                var fadeOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.5));
                fadeOutAnimation.Completed += (x, y) =>
                    {
                        var fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.5));
                        LoggingInGrid.BeginAnimation(Grid.OpacityProperty, fadeInAnimation);
                        LoggingInGrid.Visibility = Visibility.Visible;
                        LoginInfoGrid.Visibility = Visibility.Hidden;
                        worker.RunWorkerAsync();
                    };
                LoginInfoGrid.BeginAnimation(Grid.OpacityProperty, fadeOutAnimation);
            }
        }

        #region Fade labels
        private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (UsernameTextBox.Text.Length > 0)
            {
                var fadeLabelOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.1));
                UsernameLabel.BeginAnimation(Label.OpacityProperty, fadeLabelOutAnimation);
            }
            else
            {
                var fadeLabelInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.1));
                UsernameLabel.BeginAnimation(Label.OpacityProperty, fadeLabelInAnimation);
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Password.Length > 0)
            {
                var fadeLabelOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.1));
                PasswordLabel.BeginAnimation(Label.OpacityProperty, fadeLabelOutAnimation);
            }
            else
            {
                var fadeLabelInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.1));
                PasswordLabel.BeginAnimation(Label.OpacityProperty, fadeLabelInAnimation);
            }

            var fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.5));
            HintLabel.BeginAnimation(Label.OpacityProperty, fadeInAnimation);
        }
        #endregion Fade labels

        private void RegionLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FadeLabel RegionLabel = (FadeLabel)sender;
            if (PlayerRegion == (string)RegionLabel.Content)
                return;

            if (LastRegionLabel != null)
            {
                LastRegionLabel.FadeOut();
                LastRegionLabel.KeepColor = false;
            }

            LastRegionLabel = RegionLabel;
            RegionLabel.KeepColor = true;
            PlayerRegion = (string)RegionLabel.Content;
            Settings.Default.Region = PlayerRegion;
            Settings.Default.Save();
        }

        #region Settings
        private void FindLeagueButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "LoL Launcher|lol.launcher.exe";
            if (Directory.Exists(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), "Riot Games", "League of Legends")))
                openDialog.InitialDirectory = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), "Riot Games", "League of Legends");
            if (openDialog.ShowDialog().Value)
            {
                LocationLabel.Content = openDialog.FileName.Replace("lol.launcher.exe", "");
                InstallLocation = openDialog.FileName.Replace("lol.launcher.exe", "");
            }
        }

        private void SettingsButton_MouseEnter(object sender, MouseEventArgs e)
        {
            var fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.5));
            SettingsButton.BeginAnimation(Image.OpacityProperty, fadeInAnimation);
        }

        private void SettingsButton_MouseLeave(object sender, MouseEventArgs e)
        {
            var fadeOutAnimation = new DoubleAnimation(0.3, TimeSpan.FromSeconds(0.5));
            SettingsButton.BeginAnimation(Image.OpacityProperty, fadeOutAnimation);
        }

        private void SettingsButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (SettingsGrid.Visibility == System.Windows.Visibility.Hidden)
            {
                var fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.5));
                SettingsGrid.BeginAnimation(Grid.OpacityProperty, fadeInAnimation);
                SettingsGrid.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                var fadeOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.5));
                fadeOutAnimation.Completed += (x, y) => SettingsGrid.Visibility = System.Windows.Visibility.Hidden;
                SettingsGrid.BeginAnimation(Grid.OpacityProperty, fadeOutAnimation);
            }
        }
        #endregion Settings
    }
}