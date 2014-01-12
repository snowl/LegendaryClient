using jabber.protocol.client;
using jabber.protocol.iq;
using LegendaryClient.Elements;
using LegendaryClient.Logic;
using LegendaryClient.Logic.Riot;
using LegendaryClient.Logic.Riot.Leagues;
using LegendaryClient.Logic.Riot.Platform;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace LegendaryClient.Pages
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        internal ChatBoxControl ChatControl;

        public MainPage()
        {
            InitializeComponent();

            Client.MainContainer = ContentContainer;
            Client.SwitchPage<HomePage>();

            if (Properties.Settings.Default.Status != "")
            {
                StatusTextbox.Text = Properties.Settings.Default.Status;
                Client.CurrentStatus = Properties.Settings.Default.Status;
            }

            //Should these be data contexted? idk
            NameLabel.Content = Client.LoginPacket.AllSummonerData.Summoner.Name;
            LevelLabel.Content = Client.LoginPacket.AllSummonerData.SummonerLevel.Level;
            IPLabel.DataContext = Client.LoginPacket;
            RPLabel.DataContext = Client.LoginPacket;
            LPLabel.DataContext = Client.Context;
            LeagueLabel.DataContext = Client.Context;

            int ProfileIconID = Client.LoginPacket.AllSummonerData.Summoner.ProfileIconId;
            string uriSource = System.IO.Path.Combine(Client.ExecutingDirectory, "Assets", "profileicon", ProfileIconID + ".png");
            ProfileImage.Source = Client.GetImage(uriSource);

            if (Client.LoginPacket.AllSummonerData.SummonerLevel.Level == 30)
            {
                ExpProgressBar.Value = 100;
                ExpLabel.Visibility = Visibility.Hidden;
                PlayerStatSummary RankedCheck = Client.LoginPacket.PlayerStatSummaries.PlayerStatSummarySet.Find(x => x.PlayerStatSummaryTypeString == "RankedSolo5x5");
                if (RankedCheck != null)
                {
                    Client.PlayerIsRanked = true;
                    new Action(async () =>
                    {
                        SummonerLeaguesDTO MyLeagues = await RiotCalls.GetAllLeaguesForPlayer(Client.LoginPacket.AllSummonerData.Summoner.SumId);
                        GotLeaguesForPlayer(MyLeagues);
                    }).Invoke();
                }
            }
            else
            {
                double CurrentEXP = Client.LoginPacket.AllSummonerData.SummonerLevelAndPoints.ExpPoints;
                double RequiredExp = Client.LoginPacket.AllSummonerData.SummonerLevel.ExpToNextLevel;
                ExpProgressBar.Value = (CurrentEXP / RequiredExp) * 100;
                ExpLabel.Content = string.Format("{0}/{1}", CurrentEXP, RequiredExp);
            }
            Client.OnUpdatePlayer += OnUpdatePlayer;
        }

        private void GotLeaguesForPlayer(SummonerLeaguesDTO result)
        {
            Client.RunOnUIThread(new Action(() =>
            {
                foreach (LeagueListDTO leagues in result.SummonerLeagues)
                {
                    if (leagues.Queue == "RANKED_SOLO_5x5")
                    {
                        Client.Context.Tier = leagues.Tier + " " + leagues.RequestorsRank;
                        Client.Context.LeagueName = leagues.Name;
                        foreach (LeagueItemDTO player in leagues.Entries)
                        {
                            if (player.PlayerOrTeamName == Client.LoginPacket.AllSummonerData.Summoner.Name)
                            {
                                string Series = "";
                                if (player.MiniSeries != null)
                                    Series = player.MiniSeries.Progress.Replace('N', '-');

                                Client.Context.CurrentLP = (player.LeaguePoints == 100 ? Series : Convert.ToString(player.LeaguePoints) + " LP");
                            }
                        }
                    }
                }
                //TODO: Make these datacontext
                LPLabel.Content = Client.Context.CurrentLP;
                LeagueLabel.Content = Client.Context.Tier;
            }));
        }

        private void HeaderTriggerGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            ShowHeader();
        }

        private void HeaderGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (Client.CurrentPage == typeof(HomePage))
                return;

            HideHeader();
        }

        #region Animations
        private void ChatTriggerGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            ChatTriggerGrid.Visibility = Visibility.Hidden;

            var moveAnimation = new ThicknessAnimation(new Thickness(0, ChatGrid.Margin.Top, 0, 30), TimeSpan.FromSeconds(0.25));
            ChatGrid.BeginAnimation(Grid.MarginProperty, moveAnimation);
        }

        private void ChatGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            ChatTriggerGrid.Visibility = Visibility.Visible;

            var moveAnimation = new ThicknessAnimation(new Thickness(0, ChatGrid.Margin.Top, -190, 30), TimeSpan.FromSeconds(0.25));
            ChatGrid.BeginAnimation(Grid.MarginProperty, moveAnimation);
        }

        private void ShowHeader()
        {
            var moveAnimation = new ThicknessAnimation(new Thickness(0, 30, 0, 0), TimeSpan.FromSeconds(0.25));
            HeaderGrid.BeginAnimation(Grid.MarginProperty, moveAnimation);
            moveAnimation = new ThicknessAnimation(new Thickness(0, 130, ChatGrid.Margin.Right, 30), TimeSpan.FromSeconds(0.25));
            ChatGrid.BeginAnimation(Grid.MarginProperty, moveAnimation);

            var fadeOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.25));
            TrianglePoly.BeginAnimation(Polygon.OpacityProperty, fadeOutAnimation);
        }

        private void HideHeader()
        {
            var moveAnimation = new ThicknessAnimation(new Thickness(0, -60, 0, 0), TimeSpan.FromSeconds(0.25));
            HeaderGrid.BeginAnimation(Grid.MarginProperty, moveAnimation);
            moveAnimation = new ThicknessAnimation(new Thickness(0, 40, ChatGrid.Margin.Right, 30), TimeSpan.FromSeconds(0.25));
            ChatGrid.BeginAnimation(Grid.MarginProperty, moveAnimation);

            var fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.25));
            TrianglePoly.BeginAnimation(Polygon.OpacityProperty, fadeInAnimation);
        }
        #endregion Animations

        private void HomeButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Client.SwitchPage<HomePage>(true);
            ShowHeader();
        }

        private void ProfileButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Client.SwitchPage<ProfilePage>(true);
            HideHeader();
        }

        private void PlayButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Client.SwitchPage<PlayPage>(true);
            HideHeader();
        }

        #region Chat
        void OnUpdatePlayer(object sender, ChatPlayerItem e)
        {
            Client.RunOnUIThread(new Action(() =>
            {
                SmallChatItem item = null;
                foreach (SmallChatItem items in ChatStackPanel.Children)
                {
                    if ((string)items.PlayerNameLabel.Content == e.Username)
                        item = items;
                }

                if (item == null)
                {
                    item = new SmallChatItem();
                    item.Tag = e;
                    item.MouseDoubleClick += item_MouseDoubleClick;
                    ChatStackPanel.Children.Add(item);
                }

                item.PlayerNameLabel.Content = e.Username;
                item.StatusLabel.Content = e.Status;
                var converter = new BrushConverter();

                if (!e.IsOnline)
                    ChatStackPanel.Children.Remove(item);
                else if (e.GameStatus == "outOfGame" && !e.IsAway)
                    item.StatusEllipse.Fill = (Brush)converter.ConvertFromString("#2ecc71");
                else
                    item.StatusEllipse.Fill = (Brush)converter.ConvertFromString("#e74c3c");

                foreach (PlayerChatControl items in PlayerChatStackPanel.Children)
                {
                    if ((string)items.PlayerNameLabel.Content == e.Username)
                    {
                        items.StatusEllipse.Stroke = null;
                        if (!e.IsOnline)
                        {
                            items.StatusEllipse.Fill = (Brush)converter.ConvertFromString("#02000000");
                            items.StatusEllipse.Stroke = (Brush)converter.ConvertFromString("#FFA0A0A0");
                        }
                        else if (e.GameStatus == "outOfGame" && !e.IsAway)
                        {
                            items.StatusEllipse.Fill = (Brush)converter.ConvertFromString("#2ecc71");
                        }
                        else
                        {
                            items.StatusEllipse.Fill = (Brush)converter.ConvertFromString("#e74c3c");
                        }
                    }
                }
            }));
        }

        void item_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SmallChatItem item = (SmallChatItem)sender;
            ChatPlayerItem player = (ChatPlayerItem)item.Tag;

            foreach (PlayerChatControl items in PlayerChatStackPanel.Children)
            {
                if ((string)items.PlayerNameLabel.Content == player.Username && items.Visibility != Visibility.Collapsed)
                    return;
            }

            PlayerChatControl PlayerControl = new PlayerChatControl();
            PlayerControl.Tag = player;
            PlayerControl.PlayerNameLabel.Content = player.Username;
            PlayerControl.StatusEllipse.Fill = item.StatusEllipse.Fill;
            PlayerControl.Margin = new Thickness(5, 0, 5, 0);
            PlayerControl.MouseDown += PlayerControl_MouseDown;
            PlayerChatStackPanel.Children.Add(PlayerControl);
        }

        void PlayerControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PlayerChatControl PlayerControl = (PlayerChatControl)sender;
            ChatPlayerItem item = (ChatPlayerItem)PlayerControl.Tag;
            if (ChatControl == null)
            {
                ChatControl = new ChatBoxControl();
                HolderGrid.Children.Add(ChatControl);
            }
            else
            {
                string CurrentName = (string)ChatControl.PlayerNameLabel.Content;
                if (CurrentName == item.Username)
                {
                    HolderGrid.Children.Remove(ChatControl);
                    ChatControl = null;
                    //Client.ChatClient.OnMessage -= Client.ChatItem.ChatClient_OnMessage;
                    return;
                }
            }

            Panel.SetZIndex(ChatControl, 1);

            ChatControl.PlayerNameLabel.Content = item.Username;

            ChatControl.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            ChatControl.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            Point relativePoint = PlayerControl.TransformToAncestor(Client.Win).Transform(new Point(0, 0));
            ChatControl.Margin = new System.Windows.Thickness(relativePoint.X, 0, 0, 30);
        }

        private void ChangeStatus(object sender, MouseButtonEventArgs e)
        {
            Ellipse Status = (Ellipse)sender;

            Client.IsAway = false;
            var moveAnimation = new ThicknessAnimation(new Thickness(22, 0, 0, 2), TimeSpan.FromSeconds(0.25));
            if (Status.Name == "OnlineStatusEllipse") 
            {
                Client.CurrentPresence = PresenceType.available;
            }
            else if (Status.Name == "BusyStatusEllipse")
            {
                moveAnimation = new ThicknessAnimation(new Thickness(88, 0, 0, 2), TimeSpan.FromSeconds(0.25));
                Client.IsAway = true;
                Client.CurrentPresence = PresenceType.available;
            }
            else
            {
                moveAnimation = new ThicknessAnimation(new Thickness(152.5, 0, 0, 2), TimeSpan.FromSeconds(0.25));
                Client.CurrentPresence = PresenceType.invisible;
            }
            StatusRectangle.BeginAnimation(Rectangle.MarginProperty, moveAnimation);
            Client.SetPresence();
        }

        private void StatusTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (Client.CurrentStatus != StatusTextbox.Text && !string.IsNullOrWhiteSpace(StatusTextbox.Text))
            {
                Client.CurrentStatus = StatusTextbox.Text;
            }
            else if (string.IsNullOrWhiteSpace(StatusTextbox.Text))
            {
                Client.CurrentStatus = "Online";
            }

            Properties.Settings.Default.Status = StatusTextbox.Text;
            Properties.Settings.Default.Save();
            Client.SetPresence();
        }

        private void StatusTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (StatusTextbox.Text.Length > 0)
            {
                var fadeLabelOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.1));
                StatusLabel.BeginAnimation(Label.OpacityProperty, fadeLabelOutAnimation);
            }
            else
            {
                var fadeLabelInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.1));
                StatusLabel.BeginAnimation(Label.OpacityProperty, fadeLabelInAnimation);
            }
        }

        private void StatusTextbox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;

            StatusTextbox_LostFocus(null, null);
            e.Handled = true;
        }
        #endregion
    }
}