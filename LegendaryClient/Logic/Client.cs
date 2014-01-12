using jabber;
using jabber.client;
using jabber.connection;
using jabber.protocol.client;
using jabber.protocol.iq;
using LegendaryClient.Logic.Region;
using LegendaryClient.Logic.Riot;
using LegendaryClient.Logic.Riot.Platform;
using LegendaryClient.Logic.SQLite;
using LegendaryClient.Pages;
using RtmpSharp.Messaging;
using RtmpSharp.Net;
using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;

namespace LegendaryClient.Logic
{
    internal static class Client
    {
        #region Chat
        internal static JabberClient ChatClient;
        internal static RosterManager RostManager;
        internal static PresenceManager PresManager;
        internal static ConferenceManager ConfManager;
        internal static List<ChatPlayerItem> Players;
        internal delegate void UpdatePlayerHandler(object sender, ChatPlayerItem e);
        internal static event UpdatePlayerHandler OnUpdatePlayer;
        internal static bool PlayerIsRanked;
        internal static bool IsAway = false;
        internal static bool HasInitalTransmit = false;
        internal static PresenceType CurrentPresence;
        internal static string CurrentStatus;

        internal static void SetPresence()
        {
            if (ChatClient.IsAuthenticated)
            {
                /* ********************
                 * Show behaviour in official client
                 * away -> Red text, "Away" status, red ball
                 * dnd -> Yellow text, yellow ball
                 * chat -> Green text, green ball
                 * anything else -> Green text, yellow ball
                 * ********************/
                ChatClient.Presence(CurrentPresence, GetPresence(), (IsAway ? "away" : "chat"), 0);
            }
        }

        internal static string GetPresence()
        {
            return "<body>" +
                "<profileIcon>" + LoginPacket.AllSummonerData.Summoner.ProfileIconId + "</profileIcon>" +
                "<level>" + LoginPacket.AllSummonerData.SummonerLevel.Level + "</level>" +
                "<wins>" + 999 + "</wins>" +
                (PlayerIsRanked ?
                "<queueType /><rankedLosses>0</rankedLosses><rankedRating>0</rankedRating><tier>UNRANKED</tier>" + //Unused?
                "<rankedLeagueName>" + Context.LeagueName + "</rankedLeagueName>" +
                "<rankedLeagueDivision>" + Context.Tier.Split(' ')[1] + "</rankedLeagueDivision>" +
                "<rankedLeagueTier>" + Context.Tier.Split(' ')[0] + "</rankedLeagueTier>" +
                "<rankedLeagueQueue>RANKED_SOLO_5x5</rankedLeagueQueue>" +
                "<rankedWins>" + 999 + "</rankedWins>" : "") +
                "<gameStatus>outOfGame</gameStatus>" +
                "<statusMsg>" + CurrentStatus + "∟</statusMsg>" + //Look for "∟" to recognize that LegendaryClient - not shown on normal client
            "</body>";
        }

        internal static void RostManager_OnRosterEnd(object sender)
        {
            SetPresence();
        }

        internal static bool ChatClient_OnInvalidCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        internal static void RostManager_OnRosterItem(object sender, Item ri)
        {
            ChatPlayerItem Item = Players.Find(x => x.Id == ri.JID.User);
            if (Item == null)
            {
                ChatPlayerItem player = new ChatPlayerItem();
                player.Id = ri.JID.User;
                player.Group = "Online";
                player.Username = ri.Nickname;
                using (XmlReader reader = XmlReader.Create(new StringReader(ri.OuterXml)))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            switch (reader.Name)
                            {
                                case "group":
                                    reader.Read();
                                    string TempGroup = reader.Value;
                                    if (TempGroup != "**Default")
                                        player.Group = TempGroup;
                                    break;
                            }
                        }
                    }
                }
                Players.Add(player);
            }
        }

        internal static void PresManager_OnPrimarySessionChange(object sender, JID bare)
        {
            ChatPlayerItem Player = Players.Find(x => x.Id == bare.User);
            if (Player != null)
            {
                Player.IsOnline = false;
                if (Player.Username == null)
                    return;
                Presence[] s = PresManager.GetAll(bare);
                if (s.Length == 0) //Gone offline!
                {
                    if (OnUpdatePlayer != null)
                        OnUpdatePlayer(null, Player);
                    return;
                }
                string Presence = s[0].Status;
                if (Presence == null || Presence == "online")
                    return;

                Player = ParsePresence(Player, Presence);
                Player.IsOnline = true;

                if (s[0].Show == "away" || s[0].Show == "dnd")
                    Player.IsAway = true;
                else
                    Player.IsAway = false;

                if (String.IsNullOrWhiteSpace(Player.Status))
                    Player.Status = "Online";

                if (OnUpdatePlayer != null)
                    OnUpdatePlayer(null, Player);
            }
        }

        internal static ChatPlayerItem ParsePresence(ChatPlayerItem Player, string Presence)
        {
            Player.RawPresence = Presence; //For debugging
            using (XmlReader reader = XmlReader.Create(new StringReader(Presence)))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        #region Parse Presence

                        switch (reader.Name)
                        {
                            case "profileIcon":
                                reader.Read();
                                Player.ProfileIcon = Convert.ToInt32(reader.Value);
                                break;

                            case "level":
                                reader.Read();
                                Player.Level = Convert.ToInt32(reader.Value);
                                break;

                            case "wins":
                                reader.Read();
                                Player.Wins = Convert.ToInt32(reader.Value);
                                break;

                            case "leaves":
                                reader.Read();
                                Player.Leaves = Convert.ToInt32(reader.Value);
                                break;

                            case "rankedWins":
                                reader.Read();
                                Player.RankedWins = Convert.ToInt32(reader.Value);
                                break;

                            case "timeStamp":
                                reader.Read();
                                Player.Timestamp = Convert.ToInt64(reader.Value);
                                break;

                            case "statusMsg":
                                reader.Read();
                                Player.Status = reader.Value;
                                if (Player.Status.EndsWith("∟"))
                                {
                                    Player.UsingLegendary = true;
                                    Player.Status = Player.Status.Replace("∟", "");
                                }
                                break;

                            case "gameStatus":
                                reader.Read();
                                Player.GameStatus = reader.Value;
                                break;

                            case "skinname":
                                reader.Read();
                                Player.Champion = reader.Value;
                                break;

                            case "rankedLeagueName":
                                reader.Read();
                                Player.LeagueName = reader.Value;
                                break;

                            case "rankedLeagueTier":
                                reader.Read();
                                Player.LeagueTier = reader.Value;
                                break;

                            case "rankedLeagueDivision":
                                reader.Read();
                                Player.LeagueDivision = reader.Value;
                                break;
                        }

                        #endregion Parse Presence
                    }
                }
            }
            return Player;
        }

        internal static void Message(string To, string Message, ChatSubjects Subject)
        {
            Message msg = new Message(ChatClient.Document);
            msg.Type = MessageType.normal;
            msg.To = To + "@pvp.net";
            msg.Subject = ((ChatSubjects)Subject).ToString();
            msg.Body = Message;
            ChatClient.Write(msg);
        }

        internal static string GetObfuscatedChatroomName(string Subject, string Type)
        {
            int bitHack = 0;
            byte[] data = System.Text.Encoding.UTF8.GetBytes(Subject);
            byte[] result;
            SHA1 sha = new SHA1CryptoServiceProvider();
            result = sha.ComputeHash(data);
            string obfuscatedName = "";
            int incrementValue = 0;
            while (incrementValue < result.Length)
            {
                bitHack = result[incrementValue];
                obfuscatedName = obfuscatedName + Convert.ToString(((uint)(bitHack & 240) >> 4), 16);
                obfuscatedName = obfuscatedName + Convert.ToString(bitHack & 15, 16);
                incrementValue = incrementValue + 1;
            }
            obfuscatedName = Regex.Replace(obfuscatedName, @"/\s+/gx", "");
            obfuscatedName = Regex.Replace(obfuscatedName, @"/[^a-zA-Z0-9_~]/gx", "");
            return Type + "~" + obfuscatedName;
        }

        internal static string GetChatroomJID(string ObfuscatedChatroomName, string password, bool IsTypePublic)
        {
            if (!IsTypePublic)
                return ObfuscatedChatroomName + "@sec.pvp.net";

            if (String.IsNullOrEmpty(password))
                return ObfuscatedChatroomName + "@lvl.pvp.net";

            return ObfuscatedChatroomName + "@conference.pvp.net";
        }
        #endregion

        #region League
        internal static String ExecutingDirectory = "";
        internal static SQLiteConnection SQLiteDatabase;
        internal static List<champions> Champions;
        internal static List<championSkins> ChampionSkins;
        internal static List<items> Items;
        internal static List<masteries> Masteries;
        internal static List<runes> Runes;
        internal static RtmpClient RtmpConnection;
        internal static LoginDataPacket LoginPacket;
        internal static List<GameTypeConfigDTO> GameConfigs;
        internal static BaseRegion Region;
        internal static Session PlayerSession;
        internal static ClientDataContext Context;
        internal static ChampionDTO[] PlayerChampions;

        internal static System.Timers.Timer HeartbeatTimer;
        internal static int HeartbeatCount;

        internal static void StartHeartbeat()
        {
            HeartbeatTimer = new System.Timers.Timer();
            HeartbeatTimer.Elapsed += new ElapsedEventHandler(DoHeartbeat);
            HeartbeatTimer.Interval = 120000; // in milliseconds
            HeartbeatTimer.Start();
        }

        internal async static void DoHeartbeat(object sender, ElapsedEventArgs e)
        {
            if (!RtmpConnection.IsDisconnected)
            {
                string result = await RiotCalls.PerformLCDSHeartBeat(Convert.ToInt32(LoginPacket.AllSummonerData.Summoner.AcctId), PlayerSession.Token, HeartbeatCount,
                            DateTime.Now.ToString("ddd MMM d yyyy HH:mm:ss 'GMT-0700'"));

                HeartbeatCount++;
            }
        }

        internal static void OnMessageReceived(object sender, MessageReceivedEventArgs message)
        {

        }
        #endregion League

        #region Client
        internal static Window Win;
        internal static ContentControl MainHolder;
        internal static ContentControl MainContainer;
        internal static Type CurrentPage;
        internal static List<Page> CachedPages = new List<Page>();

        internal static void SwitchPage<T>(bool Fade = false)
        {
            Page instance = (Page)Activator.CreateInstance(typeof(T));
            CurrentPage = typeof(T);

            //Only cache some pages
            if (typeof(T) == typeof(HomePage) ||
                typeof(T) == typeof(ProfilePage))
            {
                bool FoundPage = false;
                foreach (Page p in CachedPages)
                {
                    if (p.GetType() == typeof(T))
                    {
                        instance = p;
                        FoundPage = true;
                    }
                }

                if (!FoundPage)
                    CachedPages.Add(instance);
            }

            if (Fade)
            {
                var fadeOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.25));
                fadeOutAnimation.Completed += (x, y) =>
                {
                    MainContainer.Content = instance.Content;
                    var fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.25));
                    MainContainer.BeginAnimation(ContentControl.OpacityProperty, fadeInAnimation);
                };
                MainContainer.BeginAnimation(ContentControl.OpacityProperty, fadeOutAnimation);
            }
            else
            {
                MainContainer.Content = instance.Content;
            }
        }

        internal static List<T> GetInstances<T>()
        {
            return (from t in Assembly.GetExecutingAssembly().GetTypes()
                    where t.BaseType == (typeof(T)) && t.GetConstructor(Type.EmptyTypes) != null
                    select (T)Activator.CreateInstance(t)).ToList();
        }

        internal static Size MeasureTextSize(string text, FontFamily fontFamily, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, double fontSize)
        {
            FormattedText ft = new FormattedText(text,
                                                 CultureInfo.CurrentCulture,
                                                 FlowDirection.LeftToRight,
                                                 new Typeface(fontFamily, fontStyle, fontWeight, fontStretch),
                                                 fontSize,
                                                 Brushes.Black);
            return new Size(ft.Width, ft.Height);
        }

        internal static void RunOnUIThread(Action function)
        {
            MainHolder.Dispatcher.BeginInvoke(DispatcherPriority.Input, function);
        }

        internal static void FocusClient()
        {
            if (Win.WindowState == WindowState.Minimized)
                Win.WindowState = WindowState.Normal;

            Win.Activate();
            Win.Topmost = true;
            Win.Topmost = false;
            Win.Focus();
        }

        public static String TitleCaseString(String s)
        {
            if (s == null) return s;

            String[] words = s.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length == 0) continue;

                Char firstChar = Char.ToUpper(words[i][0]);
                String rest = "";

                if (words[i].Length > 1)
                    rest = words[i].Substring(1).ToLower();

                words[i] = firstChar + rest;
            }
            return String.Join(" ", words);
        }

        public static BitmapSource ToWpfBitmap(System.Drawing.Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);

                stream.Position = 0;
                BitmapImage result = new BitmapImage();
                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                return result;
            }
        }

        public static DateTime JavaTimeStampToDateTime(double javaTimeStamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(Math.Round(javaTimeStamp / 1000)).ToLocalTime();
            return dtDateTime;
        }

        public static BitmapImage GetImage(string Address)
        {
            Uri UriSource = new Uri(Address, UriKind.RelativeOrAbsolute);

            if (!File.Exists(Address) && !Address.StartsWith("/LegendaryClient;component"))
                UriSource = new Uri("/LegendaryClient;component/NONE.png", UriKind.RelativeOrAbsolute);

            return new BitmapImage(UriSource);
        }
        #endregion Client
    }

    public class ChatPlayerItem
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public int ProfileIcon { get; set; }
        public int Level { get; set; }
        public int Wins { get; set; }
        public int RankedWins { get; set; }
        public int Leaves { get; set; }
        public string LeagueTier { get; set; }
        public string LeagueDivision { get; set; }
        public string LeagueName { get; set; }
        public string GameStatus { get; set; }
        public long Timestamp { get; set; }
        public bool Busy { get; set; }
        public string Champion { get; set; }
        public string Status { get; set; }
        public string RawPresence { get; set; }
        public string Group { get; set; }
        public bool UsingLegendary { get; set; }
        public bool IsOnline { get; set; }
        public bool IsAway { get; set; }
        public List<string> Messages = new List<string>();
    }

    internal class ClientDataContext : INotifyPropertyChanged
    {
        private String _Tier;
        internal String Tier
        {
            get { return _Tier; }
            set
            {
                _Tier = value;
                Notify("Tier");
            }
        }
        private String _LeagueName;
        internal String LeagueName
        {
            get { return _LeagueName; }
            set
            {
                _LeagueName = value;
                Notify("LeagueName");
            }
        }
        private String _CurrentLP;
        internal String CurrentLP
        {
            get { return _CurrentLP; }
            set
            {
                _CurrentLP = value;
                Notify("CurrentLP");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}