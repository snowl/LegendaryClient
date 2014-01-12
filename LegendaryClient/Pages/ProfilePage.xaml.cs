using LegendaryClient.Logic;
using LegendaryClient.Logic.Riot;
using LegendaryClient.Logic.Riot.Leagues;
using LegendaryClient.Logic.Riot.Platform;
using LegendaryClient.Logic.SQLite;
using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LegendaryClient.Pages
{
    /// <summary>
    /// Interaction logic for ProfilePage.xaml
    /// </summary>
    public partial class ProfilePage : Page
    {
        public ProfilePage()
        {
            InitializeComponent();

            FavouriteChampionImage.Opacity = 0;

            SearchForPlayer();
        }

        public async void SearchForPlayer(string SummonerName = "")
        {
            PublicSummoner Summoner = await RiotCalls.GetSummonerByName(String.IsNullOrWhiteSpace(SummonerName) ? Client.LoginPacket.AllSummonerData.Summoner.Name : SummonerName);

            RecentGames games = await RiotCalls.GetRecentGames(Summoner.AcctId);
            games.GameStatistics.Sort((s1, s2) => s2.CreateDate.CompareTo(s1.CreateDate));

            ChampionStatInfo[] TopChampions = new ChampionStatInfo[0];
            if (Summoner.SummonerLevel == 30)
            {
                TopChampions = await RiotCalls.RetrieveTopPlayedChampions(Summoner.AcctId, "CLASSIC");
            }

            //Dont update till retrieved all data
            PlayerNameLabel.Content = Summoner.Name;

            int ProfileIconID = Summoner.ProfileIconId;
            var uriSource = Path.Combine(Client.ExecutingDirectory, "Assets", "profileicon", ProfileIconID + ".png");
            ProfileImage.Source = Client.GetImage(uriSource);

            if (TopChampions.Length < 2)
            {
                if (games.GameStatistics.Count > 0)
                    SetChampionImage((int)games.GameStatistics[0].ChampionId);
                else //Hasn't played a game yet? np
                    SetChampionImage(17); //Set as random champion (chose by fair dice roll. guaranteed to be random)
            }
            else
            {
                SetChampionImage((int)TopChampions[1].ChampionId);
            }
        }

        public void SetChampionImage(int ChampionId)
        {
            System.Drawing.Rectangle cropRect = new System.Drawing.Rectangle(new System.Drawing.Point(0, 80), new System.Drawing.Size(1215, 150));
            System.Drawing.Bitmap src = System.Drawing.Image.FromFile(Path.Combine(Client.ExecutingDirectory, "Assets", "champions", champions.GetChampion(ChampionId).splashPath)) as System.Drawing.Bitmap;
            System.Drawing.Bitmap target = new System.Drawing.Bitmap(cropRect.Width, cropRect.Height);

            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(target))
            {
                g.DrawImage(src, new System.Drawing.Rectangle(0, 0, target.Width, target.Height),
                                cropRect,
                                System.Drawing.GraphicsUnit.Pixel);
            }

            FavouriteChampionImage.Source = Client.ToWpfBitmap(target);

            var fadeLabelInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.25));
            FavouriteChampionImage.BeginAnimation(Image.OpacityProperty, fadeLabelInAnimation);
        }

        private void SearchImage_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var fadeInAnimation = new DoubleAnimation(0.7, TimeSpan.FromSeconds(0.5));
            SearchImage.BeginAnimation(Image.OpacityProperty, fadeInAnimation);
        }

        private void SearchImage_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var fadeOutAnimation = new DoubleAnimation(0.3, TimeSpan.FromSeconds(0.5));
            SearchImage.BeginAnimation(Image.OpacityProperty, fadeOutAnimation);
        }

        private void SearchImage_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SearchGrid.Visibility == System.Windows.Visibility.Hidden)
            {
                var fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.5));
                SearchGrid.BeginAnimation(Grid.OpacityProperty, fadeInAnimation);
                SearchGrid.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                var fadeOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.5));
                fadeOutAnimation.Completed += (x, y) => SearchGrid.Visibility = System.Windows.Visibility.Hidden;
                SearchGrid.BeginAnimation(Grid.OpacityProperty, fadeOutAnimation);
            }
        }

        private void PlayerSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PlayerSearchBox.Text.Length > 0)
            {
                var fadeLabelOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.1));
                HintLabel.BeginAnimation(Label.OpacityProperty, fadeLabelOutAnimation);
            }
            else
            {
                var fadeLabelInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.1));
                HintLabel.BeginAnimation(Label.OpacityProperty, fadeLabelInAnimation);
            }
        }

        private void PlayerSearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;

            SearchImage_MouseDown(null, null);
            SearchForPlayer(PlayerSearchBox.Text);
            PlayerSearchBox.Text = "";
            e.Handled = true;
        }
    }
}