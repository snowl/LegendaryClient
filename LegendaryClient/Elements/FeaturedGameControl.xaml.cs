using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace LegendaryClient.Elements
{
    /// <summary>
    /// Interaction logic for FeaturedGameControl.xaml
    /// </summary>
    public partial class FeaturedGameControl : UserControl
    {
        public FeaturedGameControl()
        {
            InitializeComponent();
        }

        private void FeaturedGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            var moveAnimation = new DoubleAnimation(FeaturedGrid.ActualHeight, 200, TimeSpan.FromSeconds(0.25));
            FeaturedGrid.BeginAnimation(Rectangle.HeightProperty, moveAnimation);

            moveAnimation = new DoubleAnimation(TeamOneListView.ActualHeight, 200, TimeSpan.FromSeconds(0.25));
            TeamOneListView.BeginAnimation(Rectangle.HeightProperty, moveAnimation);
            moveAnimation = new DoubleAnimation(TeamOneListView.ActualWidth, 60, TimeSpan.FromSeconds(0.25));
            TeamOneListView.BeginAnimation(Rectangle.WidthProperty, moveAnimation);
        }

        private void FeaturedGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            var moveAnimation = new DoubleAnimation(FeaturedGrid.ActualHeight, 70, TimeSpan.FromSeconds(0.25));
            FeaturedGrid.BeginAnimation(Rectangle.HeightProperty, moveAnimation);

            moveAnimation = new DoubleAnimation(TeamOneListView.ActualHeight, 50, TimeSpan.FromSeconds(0.25));
            TeamOneListView.BeginAnimation(Rectangle.HeightProperty, moveAnimation);
            moveAnimation = new DoubleAnimation(TeamOneListView.ActualWidth, 350, TimeSpan.FromSeconds(0.25));
            TeamOneListView.BeginAnimation(Rectangle.WidthProperty, moveAnimation);
        }
    }
}