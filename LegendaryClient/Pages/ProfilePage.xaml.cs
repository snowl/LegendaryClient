using LegendaryClient.Logic;
using LegendaryClient.Logic.Riot;
using LegendaryClient.Logic.Riot.Leagues;
using LegendaryClient.Logic.Riot.Platform;
using LegendaryClient.Logic.SQLite;
using LegendaryClient.Pages.Profile;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        RecentGames Games;
        SummonerLeaguesDTO Leagues;
        List<AggregatedChampion> ChampionStats;
        Type CurrentPage; //Stop changing to same page

        public ProfilePage()
        {
            InitializeComponent();

            FavouriteChampionImage.Opacity = 0;

            SearchForPlayer();
        }

        public void SwitchProfilePage<T>(params object[] Arguments)
        {
            if (CurrentPage == typeof(T))
                return;

            Page instance = (Page)Activator.CreateInstance(typeof(T), Arguments);
            CurrentPage = typeof(T);

            var fadeOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.25));
            fadeOutAnimation.Completed += (x, y) =>
            {
                ProfileControl.Content = instance.Content;
                var fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.25));
                ProfileControl.BeginAnimation(ContentControl.OpacityProperty, fadeInAnimation);
            };
            ProfileControl.BeginAnimation(ContentControl.OpacityProperty, fadeOutAnimation);
        }

        public async void SearchForPlayer(string SummonerName = "")
        {
            PublicSummoner Summoner = await RiotCalls.GetSummonerByName(String.IsNullOrWhiteSpace(SummonerName) ? Client.LoginPacket.AllSummonerData.Summoner.Name : SummonerName);

            Games = await RiotCalls.GetRecentGames(Summoner.AcctId);
            Games.GameStatistics.Sort((s1, s2) => s2.CreateDate.CompareTo(s1.CreateDate));

            AggregatedStats champStats = null;
            if (Summoner.SummonerLevel == 30)
                champStats = await RiotCalls.GetAggregatedStats(Summoner.AcctId, "CLASSIC", Client.LoginPacket.ClientSystemStates.currentSeason.ToString());

            ChampionStats = new List<AggregatedChampion>();
            if (champStats != null)
            {
                foreach (AggregatedStat stat in champStats.LifetimeStatistics)
                {
                    AggregatedChampion Champion = null;
                    Champion = ChampionStats.Find(x => x.ChampionId == stat.ChampionId);
                    if (Champion == null)
                    {
                        Champion = new AggregatedChampion();
                        Champion.ChampionId = stat.ChampionId;
                        ChampionStats.Add(Champion);
                    }

                    var type = typeof(AggregatedChampion);
                    string fieldName = Client.TitleCaseString(stat.StatType.Replace('_', ' ')).Replace(" ", "");
                    var f = type.GetField(fieldName);
                    f.SetValue(Champion, stat.Value);
                }

                ChampionStats.Sort((x, y) => y.TotalSessionsPlayed.CompareTo(x.TotalSessionsPlayed));
            }

            PlayerNameLabel.Content = Summoner.Name;

            int ProfileIconID = Summoner.ProfileIconId;
            var uriSource = Path.Combine(Client.ExecutingDirectory, "Assets", "profileicon", ProfileIconID + ".png");
            ProfileImage.Source = Client.GetImage(uriSource);

            string[] NoveltyName = Summoner.Name.Split(' ');
            //match novelty names
            if (NoveltyName.Length > 0
                && (Summoner.Name.ToLower().StartsWith("best") || Summoner.Name.ToLower().StartsWith("only")) &&
                champions.GetChampion(NoveltyName[1]) != null)
            {
                SetChampionImage(champions.GetChampion(NoveltyName[1]).id);
            }
            else if (ChampionStats.Count == 0)
            {
                if (Games.GameStatistics.Count > 0)
                    SetChampionImage((int)Games.GameStatistics[0].ChampionId);
                else //Hasn't played a game yet? np
                    SetChampionImage(17); //Set as random champion (chose by fair dice roll. guaranteed to be random)
            }
            else
            {
                SetChampionImage((int)ChampionStats[1].ChampionId);
            }
            //Switch to overview... make sure it switches if it's currently on overview by giving a fake type
            CurrentPage = typeof(string);
            SwitchProfilePage<Overview>(ChampionStats, Games);

            //Load everything after overview is loaded
            if (Summoner.SummonerLevel == 30)
                Leagues = await RiotCalls.GetAllLeaguesForPlayer(Summoner.SummonerId);

            LcdsResponseString TotalKudos = await RiotCalls.CallKudos("{\"commandName\":\"TOTALS\",\"summonerId\": " + Summoner.SummonerId + "}");

            TotalKudos.Value = TotalKudos.Value.Replace("{\"totals\":[0,", "").Replace("]}", "");
            string[] Kudos = TotalKudos.Value.Split(',');
            FriendlyLabel.Content = Kudos[0];
            HelpfulLabel.Content = Kudos[1];
            TeamworkLabel.Content = Kudos[2];
            HonorableLabel.Content = Kudos[3];
        }

        public void SetChampionImage(int ChampionId)
        {
            Rectangle cropRect = new Rectangle(new Point(0, 80), new System.Drawing.Size(1215, 150));
            Bitmap src = System.Drawing.Image.FromFile(Path.Combine(Client.ExecutingDirectory, "Assets", "champions", champions.GetChampion(ChampionId).splashPath)) as Bitmap;
            Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(src, new Rectangle(0, 0, target.Width, target.Height), cropRect, GraphicsUnit.Pixel);
            }

            FavouriteChampionImage.Source = Client.ToWpfBitmap(target);

            var fadeLabelInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.25));
            FavouriteChampionImage.BeginAnimation(System.Windows.Controls.Image.OpacityProperty, fadeLabelInAnimation);
        }

        private void PlayerSearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;

            SearchImage_MouseDown(null, null);
            SearchForPlayer(PlayerSearchBox.WaterTextbox.Text);
            PlayerSearchBox.WaterTextbox.Text = "";
            e.Handled = true;
        }

        #region Animations
        private void SearchImage_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var fadeInAnimation = new DoubleAnimation(0.7, TimeSpan.FromSeconds(0.5));
            SearchImage.BeginAnimation(System.Windows.Controls.Image.OpacityProperty, fadeInAnimation);
        }

        private void SearchImage_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var fadeOutAnimation = new DoubleAnimation(0.3, TimeSpan.FromSeconds(0.5));
            SearchImage.BeginAnimation(System.Windows.Controls.Image.OpacityProperty, fadeOutAnimation);
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

        #endregion

        private void OverviewButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SwitchProfilePage<Overview>(ChampionStats, Games);
        }

        private void ChampionsButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SwitchProfilePage<Champions>();
        }

    }

    public class AggregatedChampion
    {
        public double ChampionId = 0;
        public double TotalSessionsPlayed = 0;
        public double TotalSessionsLost = 0;
        public double TotalSessionsWon = 0;
        public double TotalChampionKills = 0;
        public double TotalDamageDealt = 0;
        public double TotalDamageTaken = 0;
        public double MostChampionKillsPerSession = 0;
        public double TotalMinionKills = 0;
        public double TotalDoubleKills = 0;
        public double TotalTripleKills = 0;
        public double TotalQuadraKills = 0;
        public double TotalPentaKills = 0;
        public double TotalDeathsPerSession = 0;
        public double TotalGoldEarned = 0;
        public double MostSpellsCast = 0;
        public double TotalTurretsKilled = 0;
        public double TotalMagicDamageDealt = 0;
        public double TotalPhysicalDamageDealt = 0;
        public double TotalAssists = 0;
        public double TotalTimeSpentDead = 0;
        public double TotalFirstBlood = 0;
        public double TotalUnrealKills = 0;
        public double MaxNumDeaths = 0;
        public double MaxChampionsKilled = 0;
        public double BotGamesPlayed = 0;
        public double RankedSoloGamesPlayed = 0;
        public double TotalHeal = 0;
        public double MaxTimeSpentLiving = 0;
        public double MaxLargestCriticalStrike = 0;
        public double TotalNeutralMinionsKilled = 0;
        public double TotalLeaves = 0;
        public double RankedPremadeGamesPlayed = 0;
        public double MaxTimePlayed = 0;
        public double KillingSpree = 0;
        public double MaxLargestKillingSpree = 0;
        public double NormalGamesPlayed = 0;
    }

    public class MatchStats
    {
        public double Lose = 0;
        public double Win = 0;
        public double NumDeaths = 0;
        public double ChampionsKilled = 0;
        public double Assists = 0;
        public double MinionsKilled = 0;
        public double Item0 = 0;
        public double Item1 = 0;
        public double Item2 = 0;
        public double Item3 = 0;
        public double Item4 = 0;
        public double Item5 = 0;
        public double Item6 = 0;
        public double VisionWardsBoughtInGame = 0;
        public double SightWardsBoughtInGame = 0;
        public double TotalTimeCrowdControlDealt = 0;
        public double TotalDamageDealt = 0;
        public double TotalDamageTaken = 0;
        public double WardKilled = 0;
        public double BarracksKilled = 0;
        public double Level = 0;
        public double TotalDamageDealtToChampions = 0;
        public double TurretsKilled = 0;
        public double GoldEarned = 0;
        public double PhysicalDamageDealtToChampions = 0;
        public double WardPlaced = 0;
        public double NeutralMinionsKilled = 0;
        public double MagicDamageDealtPlayer = 0;
        public double PhysicalDamageTaken = 0;
        public double PhysicalDamageDealtPlayer = 0;
        public double LargestMultiKill = 0;
        public double TrueDamageDealtPlayer = 0;
        public double TotalTimeSpentDead = 0;
        public double MagicDamageTaken = 0;
        public double LargestKillingSpree = 0;
        public double TrueDamageTaken = 0;
        public double MagicDamageDealtToChampions = 0;
        public double LargestCriticalStrike = 0;
        public double TrueDamageDealtToChampions = 0;
        public double TotalHeal = 0;
        public double NeutralMinionsKilledYourJungle = 0;
        public double NeutralMinionsKilledEnemyJungle = 0;
        public double CombatPlayerScore = 0;
        public double NodeNeutralize = 0;
        public double TotalPlayerScore = 0;
        public double ObjectivePlayerScore = 0;
        public double NodeCapture = 0;
        public double TotalScoreRank = 0;
        public double VictoryPointTotal = 0;
        public double TeamObjective = 0;
        public double NodeNeutralizeAssist = 0;
        public double NodeCaptureAssist = 0;
        public int AmountTogether = 0;
        public PlayerGameStats Game = null;
    }
}