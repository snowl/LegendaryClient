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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LegendaryClient.Elements
{
    /// <summary>
    /// Interaction logic for WatermarkTextbox.xaml
    /// </summary>
    public partial class WatermarkTextbox : UserControl
    {
        public static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register
            (
                 "Watermark",
                 typeof(string),
                 typeof(WatermarkTextbox),
                 new PropertyMetadata(string.Empty)
            );

        public string Watermark
        {
            get { return (string)GetValue(WatermarkProperty); }
            set { SetValue(WatermarkProperty, value); }
        }

        public WatermarkTextbox()
        {
            InitializeComponent();

            this.DataContext = this;
        }

        private void WaterTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (WaterTextbox.Text.Length > 0)
            {
                var fadeLabelOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.1));
                HintLabel.BeginAnimation(Label.OpacityProperty, fadeLabelOutAnimation);
            }
            else
            {
                var fadeLabelInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.1));
                HintLabel.BeginAnimation(Label.OpacityProperty, fadeLabelInAnimation);
            }
        }
    }
}
