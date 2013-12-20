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
using jabber.protocol.client;
using LegendaryClient.Logic;
using System.Windows.Threading;
using System.Threading;
using LegendaryClient.Controls;
using System.Xml;
using System.IO;
using jabber.connection;

namespace LegendaryClient.Windows
{
    /// <summary>
    /// Interaction logic for TeamQueuePage.xaml
    /// </summary>
    public partial class TeamQueuePage : Page
    {
        Message MessageData;
        int InviteId = 0;
        private Room newRoom;

        /// <summary>
        /// When invited to a team
        /// </summary>
        /// <param name="Message"></param>
        public TeamQueuePage(Message Message)
        {
            InitializeComponent();
            MessageData = Message;
            Client.InviteListView = InviteListView;

            using (XmlReader reader = XmlReader.Create(new StringReader(Message.Body)))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        #region Parse Popup

                        switch (reader.Name)
                        {
                            case "inviteId":
                                reader.Read();
                                InviteId = Convert.ToInt32(reader.Value);
                                break;
                        }

                        #endregion Parse Popup
                    }
                }
            }

            string ObfuscatedName = Client.GetObfuscatedChatroomName(InviteId.ToString(), ChatPrefixes.Arranging_Game);
            string JID = Client.GetChatroomJID(ObfuscatedName, "0", true);
            newRoom = Client.ConfManager.GetRoom(new jabber.JID(JID));
            newRoom.Nickname = Client.LoginPacket.AllSummonerData.Summoner.Name;
            newRoom.OnRoomMessage += newRoom_OnRoomMessage;
            newRoom.OnParticipantJoin += newRoom_OnParticipantJoin;
            newRoom.Join();

            Client.OnMessage += Client_OnMessage;
        }

        private void newRoom_OnParticipantJoin(Room room, RoomParticipant participant)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                TextRange tr = new TextRange(ChatText.Document.ContentEnd, ChatText.Document.ContentEnd);
                tr.Text = participant.Nick + " joined the room." + Environment.NewLine;
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Yellow);
            }));
        }

        private void newRoom_OnRoomMessage(object sender, Message msg)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                //Ignore the message that is always sent when joining
                if (msg.Body != "This room is not anonymous")
                {
                    TextRange tr = new TextRange(ChatText.Document.ContentEnd, ChatText.Document.ContentEnd);
                    tr.Text = msg.From.Resource + ": ";
                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Blue);
                    tr = new TextRange(ChatText.Document.ContentEnd, ChatText.Document.ContentEnd);
                    tr.Text = msg.InnerText.Replace("<![CDATA[", "").Replace("]]>", "") + Environment.NewLine;
                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.White);
                }
            }));
        }

        private async void Client_OnMessage(object sender, Message msg)
        {
            if (msg.Subject != null)
            {
                ChatSubjects subject = (ChatSubjects)Enum.Parse(typeof(ChatSubjects), msg.Subject, true);
                double[] Double = new double[1] { Convert.ToDouble(msg.From.User.Replace("sum", "")) };
                string[] Name = await Client.PVPNet.GetSummonerNames(Double);
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                {
                    InvitePlayer invitePlayer = null;
                    foreach (var x in Client.InviteListView.Items)
                    {
                        InvitePlayer tempInvPlayer = (InvitePlayer)x;
                        if ((string)tempInvPlayer.PlayerLabel.Content == Name[0])
                        {
                            invitePlayer = x as InvitePlayer;
                            break;
                        }
                    }

                    if (subject == ChatSubjects.PRACTICE_GAME_INVITE_ACCEPT)
                    {
                        invitePlayer.StatusLabel.Content = "Accepted";
                    }

                    if (subject == ChatSubjects.GAME_INVITE_REJECT)
                    {
                        invitePlayer.StatusLabel.Content = "Rejected";
                    }

                    if (subject == ChatSubjects.GAME_INVITE_LIST_STATUS)
                    {
                        ParseCurrentInvitees(msg.Body);
                    }
                }));
            }
        }

        private void ParseCurrentInvitees(string Message)
        {
            Client.InviteListView.Items.Clear();
            using (XmlReader reader = XmlReader.Create(new StringReader(Message)))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        switch (reader.Name)
                        {
                            case "invitee":
                                InvitePlayer invitePlayer = new InvitePlayer();
                                invitePlayer.StatusLabel.Content = Client.TitleCaseString(reader.GetAttribute("status"));
                                invitePlayer.PlayerLabel.Content = reader.GetAttribute("name");
                                Client.InviteListView.Items.Add(invitePlayer);
                                break;
                        }
                    }
                }
            }
        }

    }
}
