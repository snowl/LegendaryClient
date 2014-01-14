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

        void SelectQueue(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            FadeLabel SelectedQueueLabel = (FadeLabel)sender;
            string[] QueueInfo = ((string)SelectedQueueLabel.Tag).Split('|');
            int QueueId = Convert.ToInt32(QueueInfo[0]);
            int MapId = Convert.ToInt32(QueueInfo[1]);

            GameQueueConfig config = Array.Find(OpenQueues, x => x.Id == QueueId);
            BaseMap map = BaseMap.GetMap(MapId);

            QueueLabel.Content = Client.InternalQueueToPretty(config.CacheName);
            MapLabel.Content = map.DisplayName;

            var fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.25));
            QueueGrid.BeginAnimation(Grid.OpacityProperty, fadeInAnimation);
        }


    }
}