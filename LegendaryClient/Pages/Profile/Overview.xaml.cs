using LegendaryClient.Elements;
using LegendaryClient.Logic;
using LegendaryClient.Logic.Riot.Platform;
using LegendaryClient.Logic.SQLite;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace LegendaryClient.Pages.Profile
{
    /// <summary>
    /// Interaction logic for Overview.xaml
    /// </summary>
    public partial class Overview : System.Windows.Controls.Page
    {
        List<AggregatedChampion> ChampionStats;
        RecentGames Games;

        public Overview(params object[] Arguments)
        {
            InitializeComponent();

            ChampionStats = (List<AggregatedChampion>)Arguments[0];
            Games = (RecentGames)Arguments[1];

            ParseStats();
        }

        public void ParseStats()
        {
            foreach (AggregatedChampion Champ in ChampionStats.Take(8))
            {
                if (Champ.ChampionId != 0.0)
                {
                    TopChampionControl ChampControl = new TopChampionControl();
                    ChampControl.Height = 60;
                    ChampControl.Margin = new Thickness(0, 2.5, 0, 2.5);

                    champions Champion = champions.GetChampion((int)Champ.ChampionId);
                    ChampControl.ChampionImage.Source = Champion.icon;
                    Rectangle cropRect = new Rectangle(new System.Drawing.Point(10, 90), new System.Drawing.Size(300, 60));
                    Bitmap src = Image.FromFile(Path.Combine(Client.ExecutingDirectory, "Assets", "champions", Champion.portraitPath)) as Bitmap;
                    Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

                    using (Graphics g = Graphics.FromImage(target))
                    {
                        g.DrawImage(src, new Rectangle(0, 0, target.Width, target.Height), cropRect, GraphicsUnit.Pixel);
                    }

                    ChampControl.BackgroundImage.Source = Client.ToWpfBitmap(target);

                    ChampControl.ChampionNameLabel.Content = Champion.name;
                    int GamesPlayed = (int)Champ.TotalSessionsPlayed;
                    ChampControl.GamesPlayedLabel.Content = GamesPlayed;
                    ChampControl.CSLabel.Content = string.Format("{0:0.0}", Champ.TotalMinionKills / GamesPlayed);
                    if (Champ.TotalDeathsPerSession > 0)
                        ChampControl.KDALabel.Content = string.Format("{0:0.00} KDA", (Champ.TotalChampionKills + Champ.TotalAssists) / Champ.TotalDeathsPerSession);
                    else
                        ChampControl.KDALabel.Content = "Inf. KDA";

                    ChampControl.KillsLabel.Content = string.Format("{0:0.0}", Champ.TotalChampionKills / GamesPlayed);
                    ChampControl.DeathsLabel.Content = string.Format("{0:0.0}", Champ.TotalDeathsPerSession / GamesPlayed);
                    ChampControl.AssistsLabel.Content = string.Format("{0:0.0}", Champ.TotalAssists / GamesPlayed);

                    TopChampionsPanel.Children.Add(ChampControl);
                }
            }

            List<MatchStats> GameStats = new List<MatchStats>();

            foreach (PlayerGameStats Game in Games.GameStatistics)
            {
                MatchStats Match = GameStats.Find((x) => x.Game.ChampionId == Game.ChampionId);

                if (Match != null)
                {
                    foreach (RawStat Stat in Game.Statistics)
                    {
                        var type = typeof(MatchStats);
                        string fieldName = Client.TitleCaseString(Stat.StatType.Replace('_', ' ')).Replace(" ", "");
                        var f = type.GetField(fieldName);
                        f.SetValue(Match, (double)f.GetValue(Match) + Stat.Value);
                    }
                }
                else
                {
                    Match = new MatchStats();
                    foreach (RawStat Stat in Game.Statistics)
                    {
                        var type = typeof(MatchStats);
                        string fieldName = Client.TitleCaseString(Stat.StatType.Replace('_', ' ')).Replace(" ", "");
                        var f = type.GetField(fieldName);
                        f.SetValue(Match, Stat.Value);
                    }

                    Match.Game = Game;

                    GameStats.Add(Match);
                }
                Match.AmountTogether += 1;
            }

            foreach (MatchStats Match in GameStats.Take(8))
            {
                ChampionOverviewControl GameControl = new ChampionOverviewControl();
                GameControl.Margin = new Thickness(10, 5, 10, 5);

                var uriSource = Path.Combine(Client.ExecutingDirectory, "Assets", "champions", champions.GetChampion((int)Match.Game.ChampionId).iconPath);
                GameControl.ChampionImage.Source = Client.GetImage(uriSource);

                GameControl.KillsLabel.Content = string.Format("{0:0.0}", Match.ChampionsKilled / Match.AmountTogether);
                GameControl.DeathsLabel.Content = string.Format("{0:0.0}", Match.NumDeaths / Match.AmountTogether);
                GameControl.AssistsLabel.Content = string.Format("{0:0.0}", Match.Assists / Match.AmountTogether);

                GameControl.WinsLabel.Content = Match.Win;
                GameControl.LossesLabel.Content = Match.Lose;

                LatestGamesPanel.Children.Add(GameControl);
            }
        }
    }
}
