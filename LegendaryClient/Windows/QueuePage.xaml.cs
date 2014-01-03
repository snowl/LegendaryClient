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
using System.Xml;

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
        }


        private void testbutton_Click(object sender, RoutedEventArgs e)
        {
            Client.Message("sum222908", "<body><inviteId>8649134</inviteId><userName>Snowl</userName><profileIconId>576</profileIconId><gameType>NORMAL_GAME</gameType><groupId></groupId><seasonRewards>-1</seasonRewards><mapId>1</mapId><queueId>2</queueId><gameMode>classic_pvp</gameMode><gameDifficulty></gameDifficulty></body>", ChatSubjects.GAME_INVITE);

            jabber.protocol.client.Message x = new jabber.protocol.client.Message(new XmlDocument());
            x.Body = "<body><inviteId>8649134</inviteId><userName>Snowl</userName><profileIconId>576</profileIconId><gameType>NORMAL_GAME</gameType><groupId></groupId><seasonRewards>-1</seasonRewards><mapId>1</mapId><queueId>2</queueId><gameMode>classic_pvp</gameMode><gameDifficulty></gameDifficulty></body>";
            x.From = new jabber.JID("sum222900");

            Client.SwitchPage(new TeamQueuePage(x, true));
            this.Visibility = System.Windows.Visibility.Hidden;
        }
    }
}
