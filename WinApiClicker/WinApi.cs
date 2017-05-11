using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace WinApiClicker
{
    class WinApi
    {
        /// <summary>
        /// Vráti zoznam procesov s oknami
        /// </summary>
        /// <returns></returns>
        public static List<Process> ProcessesWindowList()
        {
            return Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle)).ToList();
        }

        //Importy z win32 api pre zobrazenie ikonky programu
        //https://stackoverflow.com/questions/304109/getting-the-icon-associated-with-a-running-application
        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("user32.dll", EntryPoint = "GetClassLong")]
        static extern uint GetClassLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
        static extern IntPtr GetClassLong64(IntPtr hWnd, int nIndex);

        /// <summary>
        /// 64 bit version maybe loses significant 64-bit specific information
        /// </summary>
        static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 4)
                return new IntPtr((long)GetClassLong32(hWnd, nIndex));
            else
                return GetClassLong64(hWnd, nIndex);
        }

        // Konštanty pre ziskanie ikonky programu
        static uint WM_GETICON = 0x007f;
        static IntPtr ICON_SMALL2 = new IntPtr(2);
        static IntPtr IDI_APPLICATION = new IntPtr(0x7F00);
        static int GCL_HICON = -14;

        public static BitmapImage GetSmallWindowIcon(IntPtr hWnd)
        {
            try
            {
                IntPtr hIcon = default(IntPtr);

                hIcon = SendMessage(hWnd, WM_GETICON, ICON_SMALL2, IntPtr.Zero);

                if (hIcon == IntPtr.Zero)
                    hIcon = GetClassLongPtr(hWnd, GCL_HICON);

                if (hIcon == IntPtr.Zero)
                    hIcon = LoadIcon(IntPtr.Zero, (IntPtr)0x7F00/*IDI_APPLICATION*/);

                if (hIcon != IntPtr.Zero)
                    return BitmapConverter.BitmapToImageSource(new Bitmap(Icon.FromHandle(hIcon).ToBitmap(), 16, 16));
                else
                    return null;
            }
            catch (Exception)
            {
                return null;
            }
        }


        //Získanie bitmapy z bežiaceho procesu
        //https://stackoverflow.com/questions/891345/get-a-screenshot-of-a-specific-application
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public int Height => bottom - top;

            public int Width => right - left;
        }

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out WinApi.Rect lpRect);
        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

        /// <summary>
        /// Window to bitmap
        /// </summary>
        public static Bitmap PrintWindow(IntPtr hwnd)
        {
            WinApi.Rect rc;
            GetWindowRect(hwnd, out rc);

            Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            Graphics gfxBmp = Graphics.FromImage(bmp);
            IntPtr hdcBitmap = gfxBmp.GetHdc();

            PrintWindow(hwnd, hdcBitmap, 0);

            gfxBmp.ReleaseHdc(hdcBitmap);
            gfxBmp.Dispose();

            return bmp;
        }

        //Kliknutie do okna
        //Táto časť kodu používa pohyb myši a nalsledne kliknutie
        //https://stackoverflow.com/questions/10355286/programmatically-mouse-click-in-another-window

        [DllImport("user32.dll")]
        internal static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        internal static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetCursorPos(int x, int y);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };
        public static Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }

#pragma warning disable 649
        internal struct INPUT
        {
            public UInt32 Type;
            public MOUSEKEYBDHARDWAREINPUT Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
        }

        internal struct MOUSEINPUT
        {
            public Int32 X;
            public Int32 Y;
            public UInt32 MouseData;
            public UInt32 Flags;
            public UInt32 Time;
            public IntPtr ExtraInfo;
        }

#pragma warning restore 649


        public static void ClickOnPoint(IntPtr wndHandle, Point clientPoint)
        {
            Win32Point oldPos = new Win32Point();
            GetCursorPos(ref oldPos);

            /// get screen coordinates
            ClientToScreen(wndHandle, ref clientPoint);

            /// set cursor on coords, and press mouse
            SetCursorPos(clientPoint.X, clientPoint.Y);

            var inputMouseDown = new INPUT();
            inputMouseDown.Type = 0; /// input type mouse
            inputMouseDown.Data.Mouse.Flags = 0x0002; /// left button down

            var inputMouseUp = new INPUT();
            inputMouseUp.Type = 0; /// input type mouse
            inputMouseUp.Data.Mouse.Flags = 0x0004; /// left button up

            var inputs = new INPUT[] { inputMouseDown, inputMouseUp };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

            /// return mouse 
            SetCursorPos(oldPos.X, oldPos.Y);
        }


        //Druhý spôsob kliknutia do okna, nehýbe myšou a okno nemusí byť na rozdiel od prechádzajúceho príkladu zobrazené
        //Tj. ClickOnPoint bude fungovať len ak bude dané okno navrchu inak click nebude fungovať, toto funguje aj keď je okno prekryté
        //Oveľa kratší ako ten prechdadzajúci
        public static void ClickOnPoint2(IntPtr processMainWindowHandle, Point p)
        {
            uint WM_LBUTTONDOWN = 0x0201;
            uint WM_LBUTTONUP = 0x0202;
            IntPtr lParam = (IntPtr)((p.Y << 16) + p.X);
            WinApi.SendMessage(processMainWindowHandle, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
            WinApi.SendMessage(processMainWindowHandle, WM_LBUTTONUP, IntPtr.Zero, lParam);
        }
    }
}
