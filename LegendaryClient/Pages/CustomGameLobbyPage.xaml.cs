using LegendaryClient.Logic;
using LegendaryClient.Logic.Riot;
using LegendaryClient.Logic.Riot.Platform;
using RtmpSharp.Messaging;
using System;
using System.Windows.Controls;
namespace LegendaryClient.Pages
{
    /// <summary>
    /// Interaction logic for CustomGameLobbyPage.xaml
    /// </summary>
    public partial class CustomGameLobbyPage : Page
    {
        GameDTO LatestDTO;

        public CustomGameLobbyPage()
        {
            InitializeComponent();
            Client.IsInGame = true;

            Client.RtmpConnection.MessageReceived += GameLobby_OnMessageReceived;
        }

        private void GameLobby_OnMessageReceived(object sender, MessageReceivedEventArgs message)
        {
            if (message.Body.GetType() == typeof(GameDTO))
            {
                LatestDTO = (GameDTO)message.Body;

                Client.RunOnUIThread(new Action(() =>
                {
                    NameLabel.Content = LatestDTO.Name;

                    if (LatestDTO.GameState == "CHAMP_SELECT" || LatestDTO.GameState == "PRE_CHAMP_SELECT")
                    {
                        Client.SwitchPage<ChampSelectPage>(true, new object[1] { LatestDTO });
                    }
                }));
            }
        }

        private async void SwitchTeamButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await RiotCalls.SwitchTeams(LatestDTO.Id);
        }


    }
}
