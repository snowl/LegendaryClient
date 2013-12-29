using LegendaryClient.Controls;
using LegendaryClient.Logic;
using LegendaryClient.Logic.SQLite;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using LegendaryClient.Logic.Riot.Platform;
using LegendaryClient.Logic.Riot;

namespace LegendaryClient.Windows.Profile
{
    /// <summary>
    /// Interaction logic for Champions.xaml
    /// </summary>
    public partial class Champions : Page
    {
        private List<ChampionDTO> ChampionList;
        private bool NoFilterOnLoad;

        public Champions()
        {
            InitializeComponent();
        }

        public async void Update()
        {
            ChampionDTO[] champList = await RiotCalls.GetAvailableChampions();

            ChampionList = new List<ChampionDTO>(champList);

            ChampionList.Sort(delegate(ChampionDTO x, ChampionDTO y)
            {
                int IsFav = champions.GetChampion(y.ChampionId).IsFavourite.CompareTo(champions.GetChampion(x.ChampionId).IsFavourite);
                if (IsFav != 0) return IsFav;
                else return champions.GetChampion(x.ChampionId).displayName.CompareTo(champions.GetChampion(y.ChampionId).displayName);
            });

            FilterChampions();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterChampions();
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!NoFilterOnLoad) //Don't filter when content is first loaded
            {
                NoFilterOnLoad = true;
                return;
            }
            FilterChampions();
        }

        private void FilterChampions()
        {
            ChampionSelectListView.Items.Clear();

            List<ChampionDTO> tempList = ChampionList.ToList();

            if (!String.IsNullOrEmpty(SearchTextBox.Text))
            {
                tempList = tempList.Where(x => champions.GetChampion(x.ChampionId).displayName.ToLower().Contains(SearchTextBox.Text.ToLower())).ToList();
            }

            bool AllChampions = false;
            bool OwnedChampions = false;
            bool NotOwnedChampions = false;
            bool AvaliableChampions = false;

            switch ((string)FilterComboBox.SelectedValue)
            {
                case "All":
                    AllChampions = true;
                    break;

                case "Owned":
                    OwnedChampions = true;
                    break;

                case "Not Owned":
                    NotOwnedChampions = true;
                    break;

                default:
                    AvaliableChampions = true;
                    break;
            }

            foreach (ChampionDTO champ in tempList)
            {
                if ((AvaliableChampions && (champ.Owned || champ.FreeToPlay)) ||
                    (AllChampions) ||
                    (OwnedChampions && champ.Owned) ||
                    (NotOwnedChampions && !champ.Owned))
                {
                    ProfileChampionImage championImage = new ProfileChampionImage();
                    champions champion = champions.GetChampion(champ.ChampionId);
                    championImage.DataContext = champion;

                    if (champion.IsFavourite)
                        championImage.FavoriteImage.Visibility = System.Windows.Visibility.Visible;

                    if (champ.FreeToPlay)
                        championImage.FreeToPlayLabel.Visibility = System.Windows.Visibility.Visible;

                    if (!champ.Owned && !champ.FreeToPlay)
                        championImage.ChampImage.Opacity = 0.5;

                    championImage.Tag = champ.ChampionId;
                    ChampionSelectListView.Items.Add(championImage);
                }
            }
        }

        private void ChampionSelectListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChampionSelectListView.SelectedIndex != -1)
            {
                ProfileChampionImage selectedChampion = (ProfileChampionImage)ChampionSelectListView.SelectedItem;
                Client.OverlayContainer.Content = new ChampionDetailsPage((int)selectedChampion.Tag).Content;
                Client.OverlayContainer.Visibility = Visibility.Visible;
            }
        }
    }
}