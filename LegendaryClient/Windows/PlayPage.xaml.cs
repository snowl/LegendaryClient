using LegendaryClient.Logic;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Timer = System.Timers.Timer;

namespace LegendaryClient.Windows
{
    /// <summary>
    /// Interaction logic for PlayPage.xaml
    /// </summary>
    public partial class PlayPage : Page
    {
        private int i = 10;
        private static Timer PingTimer;

        public PlayPage()
        {
            InitializeComponent();
            Client.IsOnPlayPage = true;
            QueueContent.Content = new QueuePage().Content;
            PingTimer = new Timer(1000);
            PingTimer.Elapsed += new ElapsedEventHandler(PingElapsed);
            PingTimer.Enabled = true;
            PingElapsed(1, null);
        }

        internal void PingElapsed(object sender, ElapsedEventArgs e)
        {
            if (i++ < 10) //Ping every 10 seconds
                return;
            i = 0;
            if (!Client.IsOnPlayPage)
                return;
            double PingAverage = HighestPingTime(Client.Region.PingAddresses);
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                PingLabel.Content = Math.Round(PingAverage).ToString() + "ms";
                if (PingAverage == 0)
                    PingLabel.Content = "Timeout";
                if (PingAverage == -1)
                    PingLabel.Content = "Ping not enabled for this region";

                BrushConverter bc = new BrushConverter();
                Brush brush = null;
                if (PingAverage > 999 || PingAverage < 1)
                    brush = (Brush)bc.ConvertFrom("#FFFF6767");
                else if (PingAverage > 110 && PingAverage < 999)
                    brush = (Brush)bc.ConvertFrom("#FFFFD667");
                else
                    brush = (Brush)bc.ConvertFrom("#FF67FF67");
                PingRectangle.Fill = brush;
            }));
        }

        internal double HighestPingTime(IPAddress[] Addresses)
        {
            double HighestPing = -1;

            if (Addresses.Length > 0)
            {
                HighestPing = 0;
            }

            foreach (IPAddress Address in Addresses)
            {
                int timeout = 120;
                Ping pingSender = new Ping();
                PingReply reply = pingSender.Send(Address.ToString(), timeout);
                if (reply.Status == IPStatus.Success)
                {
                    if (reply.RoundtripTime > HighestPing)
                    {
                        HighestPing = reply.RoundtripTime;
                    }
                }
            }
            return HighestPing;
        }

        private void CreateCustomGameButton_Click(object sender, RoutedEventArgs e)
        {
            Client.SwitchPage(new CreateCustomGamePage());
        }

        private void JoinCustomGameButton_Click(object sender, RoutedEventArgs e)
        {
            Client.SwitchPage(new CustomGameListingPage());
        }

        private void AutoAcceptCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Client.AutoAcceptQueue = (AutoAcceptCheckBox.IsChecked.HasValue) ? AutoAcceptCheckBox.IsChecked.Value : false;
        }
    }
}