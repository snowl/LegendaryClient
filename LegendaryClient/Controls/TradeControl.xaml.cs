using LegendaryClient.Logic;
using LegendaryClient.Logic.Riot;
using LegendaryClient.Logic.Riot.Platform;
using System.Windows;
using System.Windows.Controls;

namespace LegendaryClient.Controls
{
    /// <summary>
    /// Interaction logic for TradeControl.xaml
    /// </summary>
    public partial class TradeControl : UserControl
    {
        public TradeControl()
        {
            InitializeComponent();
        }

        private async void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            TradeContractDTO TradeDTO = this.Tag as TradeContractDTO;
            await RiotCalls.AcceptTrade(TradeDTO.RequesterInternalSummonerName, (int)TradeDTO.RequesterChampionId);
        }

        private async void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            await RiotCalls.DeclineTrade();
            this.Visibility = System.Windows.Visibility.Hidden;
        }
    }
}