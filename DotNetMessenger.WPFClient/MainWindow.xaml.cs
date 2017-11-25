using System;
using System.Windows.Controls;

namespace DotNetMessenger.WPFClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private bool _autoScroll = true;
        private const double Tolerance = 0.1;

        private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;
            // User scroll event : set or unset auto-scroll mode
            if (Math.Abs(e.ExtentHeightChange) < Tolerance)
            {
                // Content unchanged : user scroll event
                _autoScroll = Math.Abs(scrollViewer.VerticalOffset - scrollViewer.ScrollableHeight) < Tolerance;
            }

            // Content scroll event : auto-scroll eventually
            if (_autoScroll && Math.Abs(e.ExtentHeightChange) > Tolerance)
            {   // Content changed and auto-scroll mode set
                // Autoscroll
                scrollViewer.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
            }
        }
    }
}
