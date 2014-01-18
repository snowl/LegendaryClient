using LegendaryClient.Logic;
using LegendaryClient.Pages;
using System;
using System.IO;
using System.Reflection;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Animation;

namespace LegendaryClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Client.ExecutingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Client.MainHolder = Container;
            Client.Win = this;

            if (Properties.Settings.Default.Status != "")
                Client.CurrentStatus = Properties.Settings.Default.Status;

            if (Properties.Settings.Default.ChampionGroups == null)
                Properties.Settings.Default.ChampionGroups = new string[0];

            Client.OverlayContainer = OverlayContainer;
            Client.OverlayGrid = OverlayGrid;

            Client.PingTimer = new Timer(10000);
            Client.PingTimer.Elapsed += new ElapsedEventHandler(Client.PingElapsed);
            Client.PingTimer.Enabled = true;

            //Wait half a second before starting, makes it look sleek. This is a hack tho (but it's pretty!)
            var waitAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.5));
            waitAnimation.Completed += (o, e) =>
            {
                Container.Content = new LoginPage().Content;
            };
            Container.BeginAnimation(ContentControl.OpacityProperty, waitAnimation);
        }
    }

    public class FocusVisualTreeChanger
    {
        public static bool GetIsChanged(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsChangedProperty);
        }

        public static void SetIsChanged(DependencyObject obj, bool value)
        {
            obj.SetValue(IsChangedProperty, value);
        }

        public static readonly DependencyProperty IsChangedProperty =
            DependencyProperty.RegisterAttached("IsChanged", typeof(bool), typeof(FocusVisualTreeChanger), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, IsChangedCallback));

        private static void IsChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (true.Equals(e.NewValue))
            {
                FrameworkContentElement contentElement = d as FrameworkContentElement;
                if (contentElement != null)
                {
                    contentElement.FocusVisualStyle = null;
                    return;
                }

                FrameworkElement element = d as FrameworkElement;
                if (element != null)
                {
                    element.FocusVisualStyle = null;
                }
            }
        }
    }
}