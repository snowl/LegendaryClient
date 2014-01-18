using LegendaryClient.Elements;
using LegendaryClient.Logic;
using LegendaryClient.Logic.PlayerSpell;
using LegendaryClient.Logic.Riot;
using LegendaryClient.Logic.Riot.Platform;
using LegendaryClient.Logic.SQLite;
using LegendaryClient.Pages.Profile;
using RtmpSharp.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace LegendaryClient.Pages
{
    /// <summary>
    /// Interaction logic for ChampSelectPage.xaml
    /// </summary>
    public partial class ChampSelectPage : Page
    {
        private bool _BanningPhase;
        private bool BanningPhase
        {
            get { return _BanningPhase; }
            set
            {
                if (_BanningPhase != value)
                {
                    RenderChamps(value);
                    _BanningPhase = value;
                }
            }
        }

        private int _LastPickTurn;
        private int LastPickTurn
        {
            get { return _LastPickTurn; }
            set
            {
                if (_LastPickTurn != value)
                {
                    CountdownCounter = GameConfigType.MainPickTimerDuration - 1;
                    _LastPickTurn = value;
                }
            }
        }

        private Champions ChampionPage;
        private GameTypeConfigDTO GameConfigType;
        private GameDTO LatestDTO;
        private List<ChampionDTO> ChampList;
        private List<ChampionBanInfoDTO> ChampionsForBan;
        private PotentialTradersDTO CanTradeWith;
        private DispatcherTimer CountdownTimer;
        private int CountdownCounter;

        public ChampSelectPage(params object[] Arguments)
        {
            InitializeComponent();
            StartChampSelect(Arguments);
        }

        private async void StartChampSelect(params object[] Arguments)
        {
            GameDTO SeedDTO = (GameDTO)Arguments[0];

            ChampionPage = new Champions();
            ChampionPage.OnClickChampion += ChampionPage_OnClickChampion;

            ChampList = new List<ChampionDTO>(Client.PlayerChampions);

            Client.FocusClient();

            GameConfigType = Client.LoginPacket.GameTypeConfigs.Find(x => x.Id == SeedDTO.GameTypeConfigId);
            CountdownCounter = GameConfigType.MainPickTimerDuration - 5; //Seems to be a 5 second inconsistancy with riot and what they actually provide.

            CountdownTimer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                CountdownLabel.Content = CountdownCounter.ToString(); if (CountdownCounter != 0) CountdownCounter -= 1;
            }, Application.Current.Dispatcher);
            CountdownTimer.Start();

            await RiotCalls.SetClientReceivedGameMessage(SeedDTO.Id, "CHAMP_SELECT_CLIENT");

            LatestDTO = await RiotCalls.GetLatestGameTimerState(SeedDTO.Id, SeedDTO.GameState, SeedDTO.PickTurn);

            ChampionBanInfoDTO[] ChampsForBan = await RiotCalls.GetChampionsForBan();
            ChampionsForBan = new List<ChampionBanInfoDTO>(ChampsForBan);
            ChampionsForBan.Sort((x, y) => champions.GetChampion(x.ChampionId).displayName.CompareTo(champions.GetChampion(y.ChampionId).displayName));

            ChampionControl.Content = ChampionPage.Content;

            ChampSelect_OnMessageReceived(this, LatestDTO);
            Client.RtmpConnection.MessageReceived += ChampSelect_OnMessageReceived;

            BottomGrid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            ChampionControl.Height = Client.Win.Height - BottomGrid.DesiredSize.Height - 118;
        }

        private void ChampSelect_OnMessageReceived(object sender, object message)
        {
            if (message.GetType() == typeof(GameDTO) || ((MessageReceivedEventArgs)message).Body.GetType() == typeof(GameDTO))
            {
                GameDTO ChampDTO = null;

                if (message.GetType() == typeof(GameDTO))
                    ChampDTO = message as GameDTO;
                else
                    ChampDTO = ((MessageReceivedEventArgs)message).Body as GameDTO;

                LatestDTO = ChampDTO;

                Client.RunOnUIThread(new Action(() =>
                {
                    List<Participant> AllParticipants = new List<Participant>(ChampDTO.TeamOne.ToArray());
                    AllParticipants.AddRange(ChampDTO.TeamTwo);

                    foreach (Participant p in AllParticipants)
                    {
                        if (p is PlayerParticipant)
                        {
                            PlayerParticipant play = (PlayerParticipant)p;
                            if (play.PickTurn == ChampDTO.PickTurn)
                            {
                                if (play.SummonerId == Client.LoginPacket.AllSummonerData.Summoner.SumId)
                                {
                                    if (!BanningPhase)
                                        ChampionPage.ChampionHolderPanel.Visibility = Visibility.Visible;

                                    ChampionPage.ChampionPanel.Opacity = 1;
                                    ChampionPage.HolderPanel.IsHitTestVisible = true;
                                    break;
                                }
                            }
                        }
                        ChampionPage.ChampionHolderPanel.Visibility = Visibility.Collapsed;
                        ChampionPage.ChampionPanel.Opacity = 0.5;
                        ChampionPage.HolderPanel.IsHitTestVisible = false;
                    }


                    if (ChampDTO.GameState == "PRE_CHAMP_SELECT")
                    {
                        BanningPhase = true;
                        BanGrid.Visibility = Visibility.Visible;
                        CountdownCounter = GameConfigType.BanTimerDuration - 1; //Actual time (including the pause at end)
                        BlueBans.Children.Clear();
                        RedBans.Children.Clear();
                        foreach (var x in ChampDTO.BannedChampions)
                        {
                            SmallChampionItem champImage = new SmallChampionItem();
                            champImage.DataContext = champions.GetChampion(x.ChampionId);
                            if (x.TeamId == 100)
                                BlueBans.Children.Add(champImage);
                            else
                                RedBans.Children.Add(champImage);

                            foreach (ChampionItem y in ChampionPage.ChampionPanel.Children)
                            {
                                if (((champions)y.Tag).id == x.ChampionId)
                                {
                                    foreach (ChampionDTO PlayerChamps in ChampList.ToArray())
                                    {
                                        if (x.ChampionId == PlayerChamps.ChampionId)
                                        {
                                            ChampList.Remove(PlayerChamps);
                                            break;
                                        }
                                    }

                                    foreach (ChampionBanInfoDTO BanChamps in ChampionsForBan.ToArray())
                                    {
                                        if (x.ChampionId == BanChamps.ChampionId)
                                        {
                                            ChampionsForBan.Remove(BanChamps);
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        RenderChamps(true);
                    }
                    else if (ChampDTO.GameState == "CHAMP_SELECT")
                    {
                        //Picking has started. If pickturn has changed reset timer
                        LastPickTurn = ChampDTO.PickTurn;
                        BanningPhase = false;
                    }

                    #region Display players
                    BluePanel.Children.Clear();
                    RedPanel.Children.Clear();
                    int i = 0;
                    bool RedSide = false;

                    //Aram hack, view other players champions & names (thanks to Andrew)
                    List<PlayerChampionSelectionDTO> OtherPlayers = new List<PlayerChampionSelectionDTO>(ChampDTO.PlayerChampionSelections.ToArray());
                    bool AreWePurpleSide = false;

                    foreach (Participant participant in AllParticipants)
                    {
                        Participant tempParticipant = participant;
                        i++;
                        PlayerItem control = new PlayerItem();

                        if (tempParticipant is PlayerParticipant)
                        {
                            PlayerParticipant player = tempParticipant as PlayerParticipant;
                            control.PlayerNameLabel.Content = player.SummonerName;

                            foreach (PlayerChampionSelectionDTO selection in ChampDTO.PlayerChampionSelections)
                            {
                                if (selection.SummonerInternalName == player.SummonerInternalName)
                                {
                                    //Clear our teams champion selection for aram hack
                                    OtherPlayers.Remove(selection);
                                    control = RenderPlayer(selection, player);
                                }
                            }
                        }

                        //Display purple side if we have gone through our team
                        if (i > ChampDTO.TeamOne.Count)
                        {
                            i = 0;
                            RedSide = true;
                        }

                        if (!RedSide)
                        {
                            control.RedGrid.Visibility = Visibility.Collapsed;
                            BluePanel.Children.Add(control);
                        }
                        else
                        {
                            RedPanel.Children.Add(control);
                        }
                    }

                    #endregion Display players
                }));
            }
        }

        internal void RenderChamps(bool RenderBans)
        {
            ChampionPage.ChampionPanel.Children.Clear();
            if (!RenderBans)
            {
                foreach (ChampionDTO champ in ChampList)
                {
                    champions getChamp = champions.GetChampion(champ.ChampionId);
                    if (champ.Owned || champ.FreeToPlay)
                    {
                        ChampionItem championImage = new ChampionItem();
                        champions champion = champions.GetChampion(champ.ChampionId);
                        championImage.DataContext = champion;

                        championImage.Tag = champion;
                        championImage.MouseDown += ChampionPage.championImage_MouseDown;
                        ChampionPage.ChampionPanel.Children.Add(championImage);
                    }
                }
            }
            else
            {
                foreach (ChampionBanInfoDTO champ in ChampionsForBan)
                {
                    champions getChamp = champions.GetChampion(champ.ChampionId);
                    if (champ.EnemyOwned)
                    {
                        ChampionItem championImage = new ChampionItem();
                        champions champion = champions.GetChampion(champ.ChampionId);
                        championImage.DataContext = champion;

                        championImage.Tag = champion;
                        championImage.MouseDown += ChampionPage.championImage_MouseDown;
                        ChampionPage.ChampionPanel.Children.Add(championImage);
                    }
                }
            }

            ChampionPage.SearchTextBox_KeyUp(null, null);
        }

        internal PlayerItem RenderPlayer(PlayerChampionSelectionDTO selection, PlayerParticipant player)
        {
            PlayerItem control = new PlayerItem();
            //Render champion
            if (selection.ChampionId != 0)
            {
                control.ChampionImage.Source = champions.GetChampion(selection.ChampionId).icon;
            }
            //Render summoner spells
            if (selection.Spell1Id != 0)
            {
                string uriSource = Path.Combine(Client.ExecutingDirectory, "Assets", "spell", SummonerSpell.GetSpellImageName((int)selection.Spell1Id));
                control.SummonerSpell1Image.Source = Client.GetImage(uriSource);
                uriSource = Path.Combine(Client.ExecutingDirectory, "Assets", "spell", SummonerSpell.GetSpellImageName((int)selection.Spell2Id));
                control.SummonerSpell2Image.Source = Client.GetImage(uriSource);
            }
            //Has locked in
            if (player.PickMode == 2 || (player.PickTurn != LatestDTO.PickTurn && selection.ChampionId != 0) || LatestDTO.GameState == "POST_CHAMP_SELECT" || LatestDTO.GameState == "START_REQUESTED")
            {
                control.LockedInGrid.Visibility = Visibility.Hidden;
                ChampList.Remove(ChampList.Find(x => x.ChampionId == selection.ChampionId));
            }
            //Make obvious whos pick turn it is
            if (player.PickTurn != LatestDTO.PickTurn && (LatestDTO.GameState == "CHAMP_SELECT" || LatestDTO.GameState == "PRE_CHAMP_SELECT"))
            {
            }
            else
            {

            }
            //If trading with this player is possible
            if (CanTradeWith != null && (CanTradeWith.PotentialTraders.Contains(player.SummonerInternalName)))
            {
                control.TradeButton.Visibility = System.Windows.Visibility.Visible;
            }
            //If this player is duo/trio/quadra queued with players
            if (player.TeamParticipantId != null && (double)player.TeamParticipantId != 0)
            {
                //Byte hack to get individual hex colors
                byte[] values = BitConverter.GetBytes((double)player.TeamParticipantId);
                if (!BitConverter.IsLittleEndian) Array.Reverse(values);

                byte r = values[2];
                byte b = values[3];
                byte g = values[4];

                System.Drawing.Color myColor = System.Drawing.Color.FromArgb(r, b, g);

                var converter = new System.Windows.Media.BrushConverter();
                var brush = (Brush)converter.ConvertFromString("#" + myColor.Name);
                control.TeamRectangle.Fill = brush;
                control.TeamRectangle.Visibility = System.Windows.Visibility.Visible;
            }
            control.PlayerNameLabel.Content = player.SummonerName;
            return control;
        }

        internal async void ChampionPage_OnClickChampion(ChampionItem sender)
        {
            ChampionItem item = sender as ChampionItem;
            if (item != null)
            {
                champions Champ = (champions)sender.Tag;
                if (!BanningPhase)
                {
                    if (item.Tag != null)
                    {
                        await RiotCalls.SelectChampion(Champ.id);
                    }
                }
                else
                {
                    if (item.Tag != null)
                    {
                        await RiotCalls.BanChampion(Champ.id);
                    }
                }
            }
        }
    }
}
