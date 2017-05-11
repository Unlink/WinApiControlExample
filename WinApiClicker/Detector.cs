using System;
using System.Diagnostics;
using System.Drawing;

namespace WinApiClicker
{
    public class Detector
    {
        public Process Process { get; }
        public string WindowTitle => Process.MainWindowTitle;

        public event EventHandler<Point> RectangleDetected;

        public Detector(Process process)
        {
            Process = process;
        }

        public string Detect(out Bitmap img)
        {
            //Ziskanie bitmapy z processu
            img = WinApi.PrintWindow(Process.MainWindowHandle);

            //Primitívna detekcia modrého obdlžnika
            for (var i = 0; i < img.Width; i++)
            {
                for (var j = 0; j < img.Height; j++)
                {
                    //Detekujem štvorec tak že ak najdem prvý modrý pixel, tak pozriem èi o 10 dole a doprava je ïalší modrý, ak áno tak beriem že som našiel obdlžnik
                    if (img.GetPixel(i, j) == Color.FromArgb(0, 0, 255))
                    {
                        if (img.GetPixel(i + 10, j + 10) == Color.FromArgb(0, 0, 255))
                        {
                            var p = new Point(i, j);
                            OnRectangleDetected(p);
                            return $"Found it!!!! on {i}, {j}";
                        }
                    }
                }
            }
            return "Rectangle not found";
        }

        protected virtual void OnRectangleDetected(Point e)
        {
            RectangleDetected?.Invoke(this, e);
        }
    }
}