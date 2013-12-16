using LegendaryClient.Controls;
using LegendaryClient.Logic;
using LegendaryClient.Logic.SQLite;
using PVPNetConnect.RiotObjects.Platform.Summoner.Spellbook;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;

namespace LegendaryClient.Windows.Profile
{
    /// <summary>
    /// Interaction logic for Runes.xaml
    /// </summary>
    public partial class Runes : Page
    {
        private SpellBookPageDTO SelectedBook;
        private LargeChatPlayer PlayerItem;
        public List<PVPNetConnect.RiotObjects.Platform.Summoner.Runes.SummonerRune> runes = 
            new List<PVPNetConnect.RiotObjects.Platform.Summoner.Runes.SummonerRune>();
        public Runes()
        {
            InitializeComponent();
            for (int i = 1; i <= Client.LoginPacket.AllSummonerData.SpellBook.BookPages.Count; i++)
                RunePageListView.Items.Add(i);
            Client.LoginPacket.AllSummonerData.SpellBook.BookPages.Sort((x, y) => x.PageId.CompareTo(y.PageId));
            RunePageListView.SelectedIndex = Client.LoginPacket.AllSummonerData.SpellBook.BookPages.IndexOf(
                Client.LoginPacket.AllSummonerData.SpellBook.BookPages.Find(x => x.Current == true));
            GetAvailableRunes();
        }

        private async void GetAvailableRunes()
        {
            PVPNetConnect.RiotObjects.Platform.Summoner.Runes.SummonerRuneInventory runeInven = 
                await Client.PVPNet.GetSummonerRuneInventory(Client.LoginPacket.AllSummonerData.Summoner.SumId);
            runes = runeInven.SummonerRunes;
            //runes.Sort();
            AvailableRuneList.Items.Clear();
            foreach (PVPNetConnect.RiotObjects.Platform.Summoner.Runes.SummonerRune rune in runes)
            {
                foreach (runes Rune in Client.Runes)
                {
                    if (Rune.id == rune.RuneId)
                    {
                        /*if (AvailableRuneList.Items.(x => x.(Rune.name))
                        {
                            AvailableRuneList.Items[AvailableRuneList.Items.IndexOf(Rune.name)] = ;
                        }
                        else
                        {*/
                            AvailableRuneList.Items.Add(Rune.name);
                        //}
                    }
                }
            }
        }

        public void RenderRunes()
        {
            RedListView.Items.Clear();
            YellowListView.Items.Clear();
            BlueListView.Items.Clear();
            BlackListView.Items.Clear();
            foreach (SlotEntry RuneSlot in SelectedBook.SlotEntries)
            {
                foreach (runes Rune in Client.Runes)
                {
                    if (RuneSlot.RuneId == Rune.id)
                    {
                        RuneItem item = new RuneItem();
                        item.RuneImage.Source = Rune.icon;

                        item.RuneImage.Opacity = 0.4;
                        item.Margin = new Thickness(2, 2, 2, 2);
                        if (Rune.name.Contains("Mark"))
                        {
                            RedListView.Items.Add(item);
                        }
                        else if (Rune.name.Contains("Seal"))
                        {
                            YellowListView.Items.Add(item);
                        }
                        else if (Rune.name.Contains("Glyph"))
                        {
                            BlueListView.Items.Add(item);
                        }
                        else if (Rune.name.Contains("Quint"))
                        {
                            item.FontSize = item.FontSize * 3;// = new Size(item.RenderSize.Width * 3, item.RenderSize.Width * 3);
                            BlackListView.Items.Add(item);
                        }

                        item.Tag = Rune;
                        /*item.MouseWheel += item_MouseWheel;
                        item.MouseLeftButtonDown += item_MouseLeftButtonDown;
                        item.MouseRightButtonDown += item_MouseRightButtonDown;*/
                        item.MouseMove += item_MouseMove;
                        item.MouseLeave += item_MouseLeave;
                    }
                }
            }
            AvailableRuneList.Items.Clear();
            foreach (PVPNetConnect.RiotObjects.Platform.Summoner.Runes.SummonerRune rune in runes)
            {
                foreach (runes Rune in Client.Runes)
                {
                    if (Rune.id == rune.RuneId)
                    {
                        AvailableRuneList.Items.Add(Rune);
                    }
                }
            }
        }

        private void item_MouseLeave(object sender, MouseEventArgs e)
        {
            if (PlayerItem != null)
            {
                Client.MainGrid.Children.Remove(PlayerItem);
                PlayerItem = null;
            }
        }

        private void item_MouseMove(object sender, MouseEventArgs e)
        {
            RuneItem item = (RuneItem)sender;
            runes playerItem = (runes)item.Tag;
            if (PlayerItem == null)
            {
                PlayerItem = new LargeChatPlayer();
                Client.MainGrid.Children.Add(PlayerItem);

                Panel.SetZIndex(PlayerItem, 4);

                //Only load once
                PlayerItem.ProfileImage.Source = playerItem.icon;
                PlayerItem.PlayerName.Content = playerItem.name;

                PlayerItem.PlayerName.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                if (PlayerItem.PlayerName.DesiredSize.Width > 250) //Make title fit in item
                    PlayerItem.Width = PlayerItem.PlayerName.DesiredSize.Width;
                else
                    PlayerItem.Width = 250;
                PlayerItem.PlayerLeague.Content = playerItem.id;
                PlayerItem.UsingLegendary.Visibility = System.Windows.Visibility.Hidden;

                PlayerItem.PlayerWins.Content = ((string)playerItem.description.Replace("<br>", Environment.NewLine));
                PlayerItem.PlayerStatus.Text = "";
                PlayerItem.LevelLabel.Content = "";
                PlayerItem.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                PlayerItem.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            }

            Point MouseLocation = e.GetPosition(Client.MainGrid);

            double YMargin = MouseLocation.Y;

            double XMargin = MouseLocation.X;
            if (XMargin + PlayerItem.Width + 10 > Client.MainGrid.ActualWidth)
                XMargin = Client.MainGrid.ActualWidth - PlayerItem.Width - 10;

            PlayerItem.Margin = new Thickness(XMargin + 5, YMargin + 5, 0, 0);
        }

        private void RunePageListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (runes rune in Client.Runes)
            {
                //mastery.selectedRank = 0;
            }
            foreach (SpellBookPageDTO SpellPage in Client.LoginPacket.AllSummonerData.SpellBook.BookPages)
            {
                if (SpellPage.Current)
                {
                    SpellPage.Current = false;
                }
            }
            Client.LoginPacket.AllSummonerData.SpellBook.BookPages[RunePageListView.SelectedIndex].Current = true;
            SelectedBook = Client.LoginPacket.AllSummonerData.SpellBook.BookPages[RunePageListView.SelectedIndex];
            RuneTextBox.Text = SelectedBook.Name;
            RenderRunes();
        }
    }
}