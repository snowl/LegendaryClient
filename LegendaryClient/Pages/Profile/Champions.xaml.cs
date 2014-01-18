using LegendaryClient.Elements;
using LegendaryClient.Logic;
using LegendaryClient.Logic.Riot.Platform;
using LegendaryClient.Logic.SQLite;
using LegendaryClient.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LegendaryClient.Pages.Profile
{
    /// <summary>
    /// Interaction logic for Champions.xaml
    /// </summary>
    public partial class Champions : Page
    {
        bool EditMode = false;
        public delegate void ClickChampionHandler(ChampionItem sender);
        public event ClickChampionHandler OnClickChampion;

        public Champions()
        {
            InitializeComponent();

            foreach (string Group in Settings.Default.ChampionGroups)
            {
                string[] Groups = Group.Split('|');
                string GroupName = Groups[0];
                string[] Champions = Groups[1].Split(',');
                ChampionGroupItem group = new ChampionGroupItem(GroupName, new List<string>(Champions));
                group.OnClickChampion += x => { if (OnClickChampion != null) OnClickChampion(x); };

                ChampionHolderPanel.Children.Insert(0, group);
            }

            foreach (ChampionDTO champ in Client.PlayerChampions)
            {
                ChampionItem championImage = new ChampionItem();
                champions champion = champions.GetChampion(champ.ChampionId);
                championImage.DataContext = champion;

                championImage.Tag = champion;
                championImage.MouseDown += championImage_MouseDown;
                ChampionPanel.Children.Add(championImage);
            }
        }

        internal void championImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (OnClickChampion != null)
                OnClickChampion((ChampionItem)sender);
        }

        internal void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            foreach (Control item in ChampionPanel.Children)
            {
                if (item is ChampionItem)
                {
                    champions champion = (champions)item.Tag;
                    Match match = Regex.Match(champion.displayName, SearchTextBox.WaterTextbox.Text, RegexOptions.IgnoreCase);
                    if (!match.Success)
                        item.Visibility = Visibility.Collapsed;
                    else
                        item.Visibility = Visibility.Visible;
                }
            }

            foreach (var item in ChampionHolderPanel.Children)
            {
                if (!(item is WrapPanel))
                {
                    Control controlItem = (Control)item;
                    if (string.IsNullOrEmpty(SearchTextBox.WaterTextbox.Text))
                        controlItem.Visibility = Visibility.Visible;
                    else
                        controlItem.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditMode = !EditMode;

            if (!EditMode)
            {
                EditButton.Content = "Edit";
                var fadeOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.1));
                HintLabel.BeginAnimation(Label.OpacityProperty, fadeOutAnimation);
                AddGroupButton.BeginAnimation(Button.OpacityProperty, fadeOutAnimation);
            }
            else
            {
                EditButton.Content = "Done";
                var fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.1));
                HintLabel.BeginAnimation(Label.OpacityProperty, fadeInAnimation);
                AddGroupButton.BeginAnimation(Button.OpacityProperty, fadeInAnimation);
            }

            List<string> SerializedGroups = new List<string>();
            foreach (var item in ChampionHolderPanel.Children)
            {
                if (item is ChampionGroupItem)
                {
                    ChampionGroupItem GroupItem = (ChampionGroupItem)item;
                    if (EditMode)
                    {
                        GroupItem.EditGrid.Visibility = Visibility.Visible;
                        GroupItem.ViewGrid.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        GroupItem.EditGrid.Visibility = Visibility.Collapsed;
                        GroupItem.ViewGrid.Visibility = Visibility.Visible;
                    }

                    GroupItem.GroupLabel.Content = GroupItem.GroupNameTextBox.Text;

                    if (GroupItem.Visibility != Visibility.Collapsed)
                    {
                        string SerializedChampionIds = "";
                        foreach (ChampionItem champItem in GroupItem.ChampionPanel.Children)
                        {
                            champions Champion = (champions)champItem.Tag;
                            SerializedChampionIds += Champion.id + ",";
                        }

                        SerializedGroups.Add(GroupItem.GroupNameTextBox.Text + "|" + SerializedChampionIds);
                    }
                }
            }

            Settings.Default.ChampionGroups = SerializedGroups.ToArray();
            Settings.Default.Save();
        }

        private void AddGroupButton_Click(object sender, RoutedEventArgs e)
        {
            ChampionGroupItem group = new ChampionGroupItem("Group", new List<string>());

            group.EditGrid.Visibility = Visibility.Visible;
            group.ViewGrid.Visibility = Visibility.Collapsed;

            ChampionHolderPanel.Children.Insert(0, group);
        }


    }
}
