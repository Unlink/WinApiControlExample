using System;
using System.Drawing;
using System.Windows;
using System.Windows.Threading;

namespace WinApiClicker
{
    /// <summary>
    /// Interaction logic for DetectorWindow.xaml
    /// </summary>
    public partial class DetectorWindow : Window
    {
        public Detector Detector { get; }

        public DetectorWindow(Detector detector)
        {
            Detector = detector;
            InitializeComponent();

            Message.Text = "Running detector on: "+Detector.WindowTitle;

            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += (s, o) =>
            {
                Bitmap bitmap;
                var result = Detector.Detect(out bitmap);
                Log.AppendText(result+"\n");
                Log.CaretIndex = Log.Text.Length;
                Log.ScrollToEnd();
                Screenshot.Source = BitmapConverter.BitmapToImageSource(bitmap);
            };
            dispatcherTimer.Interval = new TimeSpan(0, 0, 2);
            dispatcherTimer.Start();

            Detector.RectangleDetected += (s, p) =>
            {
                //WinApi.ClickOnPoint(Detector.Process.MainWindowHandle, new System.Drawing.Point(p.X+5,p.Y+5));
                WinApi.ClickOnPoint2(Detector.Process.MainWindowHandle, new System.Drawing.Point(p.X+5,p.Y+5));
            };
        }
    }
}
