using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WinApiClicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            WindowListBox.ItemsSource = WinApi.ProcessesWindowList().Select(p => new WindowInfo(p));
        }

        private void RunOnWindow(object sender, RoutedEventArgs e)
        {
            if (WindowListBox.SelectedItem != null)
            {
                var selectedProc = (WindowInfo) WindowListBox.SelectedItem;

                var detector = new Detector(selectedProc.Process);
                var detectorWindow = new DetectorWindow(detector);
                detectorWindow.Show();
                this.Close();
            }
        }

        struct WindowInfo
        {
            private Process _proc;

            public WindowInfo(Process proc)
            {
                _proc = proc;
            }

            public string MainWindowTitle => _proc.MainWindowTitle;
            public BitmapImage Icon => WinApi.GetSmallWindowIcon(_proc.MainWindowHandle);
            public Process Process => _proc;
        }
    }
}
