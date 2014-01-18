using LegendaryClient.Logic;
using LegendaryClient.Logic.Riot.Platform;
using LegendaryClient.Logic.SQLite;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace LegendaryClient.Elements
{
    /// <summary>
    /// Interaction logic for ChampionGroupItem.xaml
    /// </summary>
    public partial class ChampionGroupItem : UserControl
    {
        List<string> GroupChampions;
        public delegate void ClickChampionHandler(ChampionItem sender);
        public event ClickChampionHandler OnClickChampion;

        public ChampionGroupItem(string GroupName, List<string> Champions)
        {
            InitializeComponent();

            GroupChampions = Champions;

            foreach (ChampionDTO champ in Client.PlayerChampions)
            {
                if (champ.Owned) //dont not show not unowned champions that the user doesnt not unown
                {
                    champions champion = champions.GetChampion(champ.ChampionId);
                    if (Champions.Contains(champ.ChampionId.ToString()))
                    {
                        ChampionItem championImage = new ChampionItem();
                        championImage.DataContext = champion;

                        championImage.Tag = champion;
                        championImage.MouseDown += championImage_MouseDown;
                        ChampionPanel.Children.Add(championImage);
                    }
                    else
                    {
                        ChampionComboBox.Items.Add(champion.displayName);
                    }
                }
            }

            GroupNameTextBox.Text = GroupName;
            GroupLabel.Content = GroupName;
        }

        private void AddChampionButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (ChampionComboBox.SelectedItem != null)
            {
                string ChampionName = (string)ChampionComboBox.SelectedItem;
                champions champ = champions.GetDisplayChampion(ChampionName);
                GroupChampions.Add(champ.id.ToString());

                ChampionItem championImage = new ChampionItem();
                championImage.DataContext = champ;

                championImage.Tag = champ;
                championImage.MouseDown += championImage_MouseDown;
                ChampionPanel.Children.Add(championImage);

                ChampionComboBox.Items.Remove(ChampionComboBox.SelectedItem);
            }
        }

        private void RemoveGroupButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Visibility = System.Windows.Visibility.Collapsed;
        }

        void championImage_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (EditGrid.Visibility == Visibility.Visible)
            {
                ChampionItem champItem = (ChampionItem)sender;
                champions championData = (champions)champItem.Tag;
                GroupChampions.Remove(championData.id.ToString());

                ChampionPanel.Children.Remove((UIElement)sender);

                ChampionComboBox.Items.Clear();

                foreach (ChampionDTO champ in Client.PlayerChampions)
                {
                    champions champion = champions.GetChampion(champ.ChampionId);
                    if (!GroupChampions.Contains(((int)champ.ChampionId).ToString()))
                    {
                        ChampionComboBox.Items.Add(champion.displayName);
                    }
                }
            }
            else
            {
                if (OnClickChampion != null)
                    OnClickChampion((ChampionItem)sender);
            }
        }
    }
}
