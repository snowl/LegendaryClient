using LegendaryClient.Logic;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace LegendaryClient.Windows
{
    /// <summary>
    /// Interaction logic for QueuePage.xaml
    /// </summary>
    public partial class QueuePage : Page
    {
        public QueuePage()
        {
            InitializeComponent();
            Client.OnMessage += Client_OnMessage;
        }

        void Client_OnMessage(object sender, jabber.protocol.client.Message e)
        {
            if (e.Subject == null)
                return;

            ChatSubjects subject = (ChatSubjects)Enum.Parse(typeof(ChatSubjects), e.Subject, true);

            if (subject == ChatSubjects.GAME_INVITE_ACCEPT)
                Client.Message("sum222908", "<body><inviteId>8649138254</inviteId><userName>Snowl</userName><profileIconId>576</profileIconId><gameType>NORMAL_GAME</gameType><groupId></groupId><seasonRewards>-1</seasonRewards><mapId>1</mapId><queueId>2</queueId><gameMode>classic_pvp</gameMode><gameDifficulty></gameDifficulty></body>", ChatSubjects.GAME_INVITE_ACCEPT_ACK);
            else
            {
                ;
            }
        
        }

        private void testbutton_Click(object sender, RoutedEventArgs e)
        {
            Client.Message("sum222908", "<body><inviteId>8649138254</inviteId><userName>Snowl</userName><profileIconId>576</profileIconId><gameType>NORMAL_GAME</gameType><groupId></groupId><seasonRewards>-1</seasonRewards><mapId>1</mapId><queueId>2</queueId><gameMode>classic_pvp</gameMode><gameDifficulty></gameDifficulty></body>", ChatSubjects.GAME_INVITE);
        }
    }
}
