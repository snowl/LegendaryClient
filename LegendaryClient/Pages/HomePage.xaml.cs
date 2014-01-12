using LegendaryClient.Elements;
using LegendaryClient.Logic;
using LegendaryClient.Logic.Region;
using LegendaryClient.Logic.SQLite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace LegendaryClient.Pages
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        internal ArrayList gameList;
        internal ArrayList newsList;

        public HomePage()
        {
            InitializeComponent();

            BaseRegion region = BaseRegion.GetRegion(Client.LoginPacket.CompetitiveRegion);
            ChangeSpectatorRegion(region);
            GetNews(region);
        }

        private void ChangeSpectatorRegion(BaseRegion region)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                string spectatorJSON = "";
                using (WebClient client = new WebClient())
                {
                    spectatorJSON = client.DownloadString(region.SpectatorLink + "featured");
                }
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                Dictionary<string, object> deserializedJSON = serializer.Deserialize<Dictionary<string, object>>(spectatorJSON);
                gameList = deserializedJSON["gameList"] as ArrayList;
            };

            worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            {
                ParseSpectatorGames();
            };

            worker.RunWorkerAsync();
        }

        private void ParseSpectatorGames()
        {
            if (gameList == null)
                return;
            if (gameList.Count <= 0)
                return;

            FeaturedGamesStackPanel.Children.Clear();
            foreach (var objectGame in gameList)
            {
                Dictionary<string, object> SpectatorGame = objectGame as Dictionary<string, object>;
                FeaturedGameControl FeaturedGame = new FeaturedGameControl();
                FeaturedGame.Margin = new Thickness(5, 5, 5, 5);
                FeaturedGame.HorizontalAlignment = HorizontalAlignment.Left;

                foreach (KeyValuePair<string, object> pair in SpectatorGame)
                {
                    if (pair.Key == "participants")
                    {
                        ArrayList players = pair.Value as ArrayList;
                        foreach (var objectPlayer in players)
                        {
                            Dictionary<string, object> playerInfo = objectPlayer as Dictionary<string, object>;
                            int teamId = 100;
                            int championId = 0;
                            int spell1Id = 0;
                            int spell2Id = 0;
                            string PlayerName = "";
                            foreach (KeyValuePair<string, object> playerPair in playerInfo)
                            {
                                if (playerPair.Key == "teamId")
                                {
                                    teamId = (int)playerPair.Value;
                                }
                                if (playerPair.Key == "championId")
                                {
                                    championId = (int)playerPair.Value;
                                }
                                if (playerPair.Key == "summonerName")
                                {
                                    PlayerName = playerPair.Value as string;
                                }
                                if (playerPair.Key == "spell1Id")
                                {
                                    spell1Id = (int)playerPair.Value;
                                }
                                if (playerPair.Key == "spell2Id")
                                {
                                    spell2Id = (int)playerPair.Value;
                                }
                            }

                            Image image = new Image();
                            image.Source = champions.GetChampion(championId).icon;
                            image.Width = 36;
                            image.Height = 36;

                            if (teamId == 100)
                            {
                                FeaturedGame.TeamOneListView.Items.Add(image);
                            }
                            else
                            {
                                FeaturedGame.TeamTwoListView.Items.Add(image);
                            }
                        }
                    }
                    /*if (pair.Key == "bannedChampions")
                    {
                        ArrayList keyArray = pair.Value as ArrayList;
                        if (keyArray.Count > 0)
                        {
                            BlueBansLabel.Visibility = Visibility.Visible;
                            PurpleBansLabel.Visibility = Visibility.Visible;
                        }
                        foreach (Dictionary<string, object> keyArrayP in keyArray)
                        {
                            int cid = 0;
                            int teamId = 100;
                            foreach (KeyValuePair<string, object> keyArrayPair in keyArrayP)
                            {
                                if (keyArrayPair.Key == "championId")
                                {
                                    cid = (int)keyArrayPair.Value;
                                }
                                if (keyArrayPair.Key == "teamId")
                                {
                                    teamId = (int)keyArrayPair.Value;
                                }
                            }
                            ListViewItem item = new ListViewItem();
                            Image champImage = new Image();
                            champImage.Height = 58;
                            champImage.Width = 58;
                            champImage.Source = champions.GetChampion(cid).icon;
                            item.Content = champImage;
                            if (teamId == 100)
                            {
                                BlueBanListView.Items.Add(item);
                            }
                            else
                            {
                                PurpleBanListView.Items.Add(item);
                            }
                        }
                    }*/
                }
                FeaturedGamesStackPanel.Children.Add(FeaturedGame);
            }
        }

        private void GetNews(BaseRegion region)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                string newsJSON = "";
                using (WebClient client = new WebClient())
                {
                    newsJSON = client.DownloadString(region.NewsAddress);
                }
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                Dictionary<string, object> deserializedJSON = serializer.Deserialize<Dictionary<string, object>>(newsJSON);
                newsList = deserializedJSON["news"] as ArrayList;
                ArrayList promoList = deserializedJSON["promos"] as ArrayList;
                foreach (Dictionary<string, object> objectPromo in promoList)
                {
                    newsList.Add(objectPromo);
                }
            };

            worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            {
                ParseNews();
            };

            worker.RunWorkerAsync();
        }

        private void ParseNews()
        {
            if (newsList == null)
                return;
            if (newsList.Count <= 0)
                return;

            NewsStackPanel.Children.Clear();
            foreach (Dictionary<string, object> pair in newsList)
            {
                NewsItem item = new NewsItem();
                item.Margin = new System.Windows.Thickness(5, 5, 20, 5);
                foreach (KeyValuePair<string, object> kvPair in pair)
                {
                    if (kvPair.Key == "title")
                        item.NewsTitle.Content = kvPair.Value;
                    if (kvPair.Key == "description" || kvPair.Key == "promoText")
                        item.DescriptionLabel.Text = (string)kvPair.Value;
                    if (kvPair.Key == "linkUrl")
                        item.Tag = (string)kvPair.Value;
                    if (kvPair.Key == "thumbUrl")
                    {
                        BitmapImage promoImage = new BitmapImage();
                        promoImage.BeginInit(); //Download image
                        promoImage.UriSource = new Uri((string)kvPair.Value, UriKind.RelativeOrAbsolute);
                        promoImage.CacheOption = BitmapCacheOption.OnLoad;
                        promoImage.EndInit();
                        item.PromoImage.Source = promoImage;
                    }
                }
                item.MouseDown += item_MouseDown;
                NewsStackPanel.Children.Add(item);
            }
        }

        void item_MouseDown(object sender, MouseButtonEventArgs e)
        {
            NewsItem item = (NewsItem)sender;
            System.Diagnostics.Process.Start((string)item.Tag); //Launch the news article in browser
        }
    }
}