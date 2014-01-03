using jabber.client;
using jabber.connection;
using LegendaryClient.Logic;
using LegendaryClient.Logic.JSON;
using LegendaryClient.Logic.Region;
using LegendaryClient.Logic.Riot;
using LegendaryClient.Logic.Riot.Platform;
using LegendaryClient.Logic.SQLite;
using LegendaryClient.Logic.SWF;
using LegendaryClient.Logic.SWF.SWFTypes;
using RtmpSharp.IO;
using RtmpSharp.Messaging;
using RtmpSharp.Net;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace LegendaryClient.Windows
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();

            //Get client data after patcher completed
            Client.SQLiteDatabase = new SQLite.SQLiteConnection("gameStats_en_US.sqlite");
            Client.Champions = (from s in Client.SQLiteDatabase.Table<champions>()
                                orderby s.name
                                select s).ToList();

            if (Properties.Settings.Default.FavouriteChamps == null)
            {
                Properties.Settings.Default.FavouriteChamps = new Int32[0];
            }

            foreach (champions c in Client.Champions)
            {
                string Source = Path.Combine(Client.ExecutingDirectory, "Assets", "champions", c.iconPath);
                c.icon = Client.GetImage(Source);
                Champions.InsertExtraChampData(c);
                if (Properties.Settings.Default.FavouriteChamps.Contains(c.id))
                {
                    c.IsFavourite = true;
                }
            }

            Client.ChampionSkins = (from s in Client.SQLiteDatabase.Table<championSkins>()
                                    orderby s.name
                                    select s).ToList();
            Client.SearchTags = (from s in Client.SQLiteDatabase.Table<championSearchTags>()
                                 orderby s.id
                                 select s).ToList();
            Client.Keybinds = (from s in Client.SQLiteDatabase.Table<keybindingEvents>()
                               orderby s.id
                               select s).ToList();
            Client.Items = Items.PopulateItems();
            Client.Masteries = Masteries.PopulateMasteries();
            Client.Runes = Runes.PopulateRunes();

            //Retrieve latest client version
            SWFReader reader = new SWFReader("ClientLibCommon.dat");
            foreach (Tag tag in reader.Tags)
            {
                if (tag is DoABC)
                {
                    DoABC abcTag = (DoABC)tag;
                    if (abcTag.Name.Contains("riotgames/platform/gameclient/application/Version"))
                    {
                        var str = System.Text.Encoding.Default.GetString(abcTag.ABCData);
                        //Ugly hack ahead - turn back now! (http://pastebin.com/yz1X4HBg)
                        string[] firstSplit = str.Split((char)6);
                        string[] secondSplit = firstSplit[0].Split((char)19);
                        Client.Version = secondSplit[1];
                    }
                }
            }

            if (!String.IsNullOrWhiteSpace(Properties.Settings.Default.SavedUsername))
            {
                RememberUsernameCheckbox.IsChecked = true;
                LoginUsernameBox.Text = Properties.Settings.Default.SavedUsername;
            }
            if (!String.IsNullOrWhiteSpace(Properties.Settings.Default.SavedPassword))
            {
                RememberPasswordCheckbox.IsChecked = true;
                LoginPasswordBox.Password = Properties.Settings.Default.SavedPassword;
            }
            if (!String.IsNullOrWhiteSpace(Properties.Settings.Default.Region))
            {
                RegionComboBox.SelectedValue = Properties.Settings.Default.Region;
            }
            string uriSource = Path.Combine(Client.ExecutingDirectory, "Assets", "champions", champions.GetChampion(Client.LatestChamp).splashPath);
            LoginImage.Source = Client.GetImage(uriSource);
            if (!String.IsNullOrWhiteSpace(Properties.Settings.Default.SavedPassword) &&
                !String.IsNullOrWhiteSpace(Properties.Settings.Default.Region) &&
                Properties.Settings.Default.AutoLogin)
            {
                AutoLoginCheckBox.IsChecked = true;
                LoginButton_Click(null, null);
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs args)
        {
            if (RememberPasswordCheckbox.IsChecked == true)
                Properties.Settings.Default.SavedPassword = LoginPasswordBox.Password;
            else
                Properties.Settings.Default.SavedPassword = "";

            if (RememberUsernameCheckbox.IsChecked == true)
                Properties.Settings.Default.SavedUsername = LoginUsernameBox.Text;
            else
                Properties.Settings.Default.SavedUsername = "";

            Properties.Settings.Default.AutoLogin = (bool)AutoLoginCheckBox.IsChecked;
            Properties.Settings.Default.Region = (string)RegionComboBox.SelectedValue;
            Properties.Settings.Default.Save();

            HideGrid.Visibility = Visibility.Hidden;
            ErrorTextBox.Visibility = Visibility.Hidden;
            LoggingInLabel.Visibility = Visibility.Visible;
            LoggingInProgressRing.Visibility = Visibility.Visible;
            BaseRegion SelectedRegion = BaseRegion.GetRegion((string)RegionComboBox.SelectedValue);
            Client.Region = SelectedRegion;

            var context = RiotCalls.RegisterObjects();
            Client.RtmpConnection = new RtmpClient(new Uri("rtmps://" + SelectedRegion.Server + ":2099"), context, ObjectEncoding.Amf3);
            Client.RtmpConnection.MessageReceived += Client.OnMessageReceived;
            Client.RtmpConnection.CallbackException += Client.CallbackException;
            RiotCalls.OnInvocationError += Client.CallbackException;
            await Client.RtmpConnection.ConnectAsync();

            AuthenticationCredentials newCredentials = new AuthenticationCredentials();
            newCredentials.Username = LoginUsernameBox.Text;
            newCredentials.Password = LoginPasswordBox.Password;
            newCredentials.ClientVersion = Client.Version;
            newCredentials.IpAddress = RiotCalls.GetIpAddress();
            newCredentials.Locale = SelectedRegion.Locale;
            newCredentials.Domain = "lolclient.lol.riotgames.com";
            try
            {
                newCredentials.AuthToken = RiotCalls.GetAuthKey(LoginUsernameBox.Text, LoginPasswordBox.Password, SelectedRegion.LoginQueue);
            }
            catch (Exception e)
            {
                HideGrid.Visibility = Visibility.Visible;
                ErrorTextBox.Visibility = Visibility.Visible;
                LoggingInProgressRing.Visibility = Visibility.Hidden;
                LoggingInLabel.Visibility = Visibility.Hidden;
                if (e.Message.Contains("The remote name could not be resolved"))
                    ErrorTextBox.Text = "Please make sure you are connected the internet!";
                else if (e.Message.Contains("(403) Forbidden"))
                    ErrorTextBox.Text = "Your username or password is incorrect!";
                else
                    ErrorTextBox.Text = "Unable to get Auth Key";

                ErrorTextBox.Text += string.Format("{0}{1}{0}{2}", Environment.NewLine, e.Message, e.StackTrace);
                return;
            }

            Session login = await RiotCalls.Login(newCredentials);
            Client.PlayerSession = login;
            await Client.RtmpConnection.SubscribeAsync("my-rtmps", "messagingDestination", "bc", "bc-" + login.AccountSummary.AccountId.ToString());
            await Client.RtmpConnection.SubscribeAsync("my-rtmps", "messagingDestination", "gn-" + login.AccountSummary.AccountId.ToString(), "gn-" + login.AccountSummary.AccountId.ToString());
            await Client.RtmpConnection.SubscribeAsync("my-rtmps", "messagingDestination", "cn-" + login.AccountSummary.AccountId.ToString(), "cn-" + login.AccountSummary.AccountId.ToString());
            bool LoggedIn = await Client.RtmpConnection.LoginAsync(LoginUsernameBox.Text.ToLower(), login.Token);
            LoginDataPacket packet = await RiotCalls.GetLoginDataPacketForUser();
            string State = await RiotCalls.GetAccountState();

            if (State != "ENABLED")
            {
                HideGrid.Visibility = Visibility.Visible;
                ErrorTextBox.Visibility = Visibility.Visible;
                LoggingInProgressRing.Visibility = Visibility.Hidden;
                LoggingInLabel.Visibility = Visibility.Hidden;
                ErrorTextBox.Text = "Your account state was invalid: " + State;
                return;
            }

            GotLoginPacket(packet);
        }

        void client_CallbackException(object sender, Exception e)
        {
            throw e;
        }

        private async void GotLoginPacket(LoginDataPacket packet)
        {
            Client.LoginPacket = packet;
            Client.PlayerChampions = await RiotCalls.GetAvailableChampions();
            //Client.PVPNet.OnError -= PVPNet_OnError;
            Client.GameConfigs = packet.GameTypeConfigs;
            Client.IsLoggedIn = true;

            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                Client.StatusContainer.Visibility = System.Windows.Visibility.Visible;
                Client.Container.Margin = new Thickness(0, 0, 0, 40);

                //Setup chat
                Client.ChatClient.AutoReconnect = 30;
                Client.ChatClient.KeepAlive = 10;
                Client.ChatClient.NetworkHost = Dns.GetHostAddresses("chat." + Client.Region.ChatName + ".lol.riotgames.com")[0].ToString();
                Client.ChatClient.Port = 5223;
                Client.ChatClient.Server = "pvp.net";
                Client.ChatClient.SSL = true;
                Client.ChatClient.User = LoginUsernameBox.Text;
                Client.ChatClient.Password = "AIR_" + LoginPasswordBox.Password;
                Client.ChatClient.OnInvalidCertificate += Client.ChatClient_OnInvalidCertificate;
                Client.ChatClient.OnMessage += Client.ChatClient_OnMessage;
                Client.ChatClient.Connect();

                Client.RostManager = new RosterManager();
                Client.RostManager.Stream = Client.ChatClient;
                Client.RostManager.AutoSubscribe = true;
                Client.RostManager.AutoAllow = jabber.client.AutoSubscriptionHanding.AllowAll;
                Client.RostManager.OnRosterItem += Client.RostManager_OnRosterItem;
                Client.RostManager.OnRosterEnd += new bedrock.ObjectHandler(Client.ChatClientConnect);

                Client.PresManager = new PresenceManager();
                Client.PresManager.Stream = Client.ChatClient;
                Client.PresManager.OnPrimarySessionChange += Client.PresManager_OnPrimarySessionChange;

                Client.ConfManager = new ConferenceManager();
                Client.ConfManager.Stream = Client.ChatClient;

                Client.SwitchPage(new MainPage());
            }));
        }
    }
}