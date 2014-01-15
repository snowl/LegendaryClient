using LegendaryClient.Elements;
using LegendaryClient.Logic;
using LegendaryClient.Logic.Maps;
using LegendaryClient.Logic.Riot;
using LegendaryClient.Logic.Riot.Platform;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LegendaryClient.Pages
{
    /// <summary>
    /// Interaction logic for PlayPage.xaml
    /// </summary>
    public partial class PlayPage : Page
    {
        GameQueueConfig[] OpenQueues = new GameQueueConfig[0];
        GameQueueConfig SelectedConfig;

        public PlayPage()
        {
            InitializeComponent();

            PopulateQueues();
        }

        public async void PopulateQueues()
        {
            List<BaseMap> Maps = Client.GetInstances<BaseMap>();
            foreach (BaseMap map in Maps)
            {
                Label MapLabel = new Label();
                MapLabel.Content = map.DisplayName;
                MapLabel.Tag = map.GetType().FullName.Replace("LegendaryClient.Logic.Maps.Map", "");
                MapLabel.FontWeight = FontWeights.Bold;
                MapLabel.Margin = new Thickness(5, 5, 0, 0);
                var converter = new BrushConverter();
                MapLabel.Foreground = (Brush)converter.ConvertFromString("#FFE4E4E4");
                QueuePanel.Children.Add(MapLabel);
            }

            OpenQueues = await RiotCalls.GetAvailableQueues();

            foreach (GameQueueConfig config in OpenQueues)
            {
                List<Label> InsertAfterLabels = new List<Label>();

                foreach (int MapId in config.SupportedMapIds)
                {
                    int i = 0;
                    foreach (Control labelControl in QueuePanel.Children)
                    {
                        if ((string)labelControl.Tag == MapId.ToString())
                        {
                            FadeLabel QueueLabel = new FadeLabel();
                            QueueLabel.Margin = new Thickness(20, 0, 0, 0);
                            QueueLabel.Content = Client.InternalQueueToPretty(config.CacheName);
                            QueueLabel.Tag = config.Id + "|" + MapId;
                            QueueLabel.MouseDown += SelectQueue;
                            QueuePanel.Children.Insert(i + 1, QueueLabel);
                            break;
                        }
                        i = i + 1;
                    }
                }
            }
        }

        internal async void SelectQueue(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            FadeLabel SelectedQueueLabel = (FadeLabel)sender;
            string[] QueueInfo = ((string)SelectedQueueLabel.Tag).Split('|');
            int QueueId = Convert.ToInt32(QueueInfo[0]);
            int MapId = Convert.ToInt32(QueueInfo[1]);

            SelectedConfig = Array.Find(OpenQueues, x => x.Id == QueueId);
            BaseMap map = BaseMap.GetMap(MapId);

            QueueLabel.Content = Client.InternalQueueToPretty(SelectedConfig.CacheName);
            MapLabel.Content = map.DisplayName;

            QueueInfo qinfo = await RiotCalls.GetQueueInformation(SelectedConfig.Id);
            if (!Client.InternalQueueToPretty(SelectedConfig.CacheName).StartsWith("ranked team"))
                AmountInQueueLabel.Content = string.Format("people in queue {0}", qinfo.QueueLength);
            else
                AmountInQueueLabel.Content = string.Format("teams in queue {0}", qinfo.QueueLength);
            TimeSpan time = TimeSpan.FromMilliseconds(qinfo.WaitTime);
            WaitTimeLabel.Content = string.Format("avg time {0:D2}:{1:D2}", time.Minutes, time.Seconds);

            var fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.25));
            QueueGrid.BeginAnimation(Grid.OpacityProperty, fadeInAnimation);
            QueueGrid.Visibility = Visibility.Visible;
        }

        private async void InviteButton_Click(object sender, RoutedEventArgs e)
        {
            if (InvitePlayerTextBox.Text.Length != 0)
            {
                var fadeLabelOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.1));
                NoSummonerLabel.BeginAnimation(Label.OpacityProperty, fadeLabelOutAnimation);

                PublicSummoner summoner = await RiotCalls.GetSummonerByName(InvitePlayerTextBox.Text);
                if (summoner == null)
                {
                    var fadeLabelInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.1));
                    NoSummonerLabel.BeginAnimation(Label.OpacityProperty, fadeLabelInAnimation);
                    return;
                }

                InvitePlayerItem newPlayer = new InvitePlayerItem();
                newPlayer.PlayerNameLabel.Content = summoner.Name;
                InvitePlayerPanel.Children.Add(newPlayer);

                InvitePlayerTextBox.Text = "";
            }
        }

        private void InvitePlayerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (InvitePlayerTextBox.Text.Length > 0)
            {
                var fadeLabelOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.1));
                HintLabel.BeginAnimation(Label.OpacityProperty, fadeLabelOutAnimation);
                InviteButton.Visibility = Visibility.Visible;
                var fadeButtonInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.1));
                InviteButton.BeginAnimation(Button.OpacityProperty, fadeButtonInAnimation);
            }
            else
            {
                var fadeLabelInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.1));
                HintLabel.BeginAnimation(Label.OpacityProperty, fadeLabelInAnimation);
                var fadeButtonOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.1));
                fadeButtonOutAnimation.Completed += (x, y) => InviteButton.Visibility = Visibility.Hidden;
                InviteButton.BeginAnimation(Button.OpacityProperty, fadeButtonOutAnimation);
            }
        }

        private void MatchButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var fadeOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.25));
            fadeOutAnimation.Completed += (x, y) =>
            {
                CustomGrid.Visibility = Visibility.Hidden;
            };
            CustomGrid.BeginAnimation(Grid.OpacityProperty, fadeOutAnimation);
            if (SelectedConfig != null)
            {
                var fadeButtonInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.25));
                QueueGrid.BeginAnimation(Grid.OpacityProperty, fadeButtonInAnimation);
                QueueGrid.Visibility = Visibility.Visible;
            }
            QueuePanelGrid.Visibility = Visibility.Visible;
            CustomPanelGrid.Visibility = Visibility.Hidden;
        }

        private async void CustomButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var fadeOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.25));
            fadeOutAnimation.Completed += (x, y) =>
            {
                QueueGrid.Visibility = Visibility.Hidden;
            };
            QueueGrid.BeginAnimation(Grid.OpacityProperty, fadeOutAnimation);
            var fadeButtonInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.25));
            CustomGrid.BeginAnimation(Grid.OpacityProperty, fadeButtonInAnimation);
            CustomGrid.Visibility = Visibility.Visible;
            CustomPanelGrid.Visibility = Visibility.Visible;
            QueuePanelGrid.Visibility = Visibility.Hidden;

            CustomGameListView.Items.Clear();
            //allItems.Clear();
            PracticeGameSearchResult[] Games = await RiotCalls.ListAllPracticeGames();
            foreach (PracticeGameSearchResult game in Games)
            {
                GameItem item = new GameItem
                {
                    GameName = game.Name,
                    GameOwner = game.Owner.SummonerName,
                    Map = BaseMap.GetMap(game.GameMapId).DisplayName,
                    Private = game.PrivateGame.ToString().Replace("True", "Y").Replace("False", ""),
                    Slots = (game.Team1Count + game.Team2Count) + "/" + game.MaxNumPlayers,
                    Id = game.Id
                };
                CustomGameListView.Items.Add(item);
                //allItems.Add(item);
            }
        }

        private void CustomGameListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CustomGameListView.SelectedIndex != -1)
            {
                PasswordTextBox.Text = "";
                if (CustomGameListView.SelectedItem == null)
                    return;
                GameItem gitem = (GameItem)CustomGameListView.SelectedItem;

                if (gitem.Private == "Y")
                {
                    var fadeButtonInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.25));
                    PasswordGrid.BeginAnimation(Grid.OpacityProperty, fadeButtonInAnimation);
                    PasswordGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    var fadeButtonInAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.25));
                    fadeButtonInAnimation.Completed += (x, y) => PasswordGrid.Visibility = Visibility.Hidden;
                    PasswordGrid.BeginAnimation(Grid.OpacityProperty, fadeButtonInAnimation);
                }
            }
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PasswordTextBox.Text.Length > 0)
            {
                var fadeLabelOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.1));
                PasswordHintLabel.BeginAnimation(Label.OpacityProperty, fadeLabelOutAnimation);
            }
            else
            {
                var fadeLabelInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.1));
                PasswordHintLabel.BeginAnimation(Label.OpacityProperty, fadeLabelInAnimation);
            }
        }
    }

    public class GameItem
    {
        public string GameName { get; set; }
        public string GameOwner { get; set; }
        public string Slots { get; set; }
        public int Spectators { get; set; }
        public double Id { get; set; }
        public string Map { get; set; }
        public string Private { get; set; }
        public string Type { get; set; }
    }
}