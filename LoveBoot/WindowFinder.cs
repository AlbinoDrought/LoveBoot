using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Management.Instrumentation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Emgu.CV.Structure;

namespace LoveBoot
{
    public class WindowFinder
    {
        // http://stackoverflow.com/a/9669149

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string strClassName, string strWindowName);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        private string processName;
        private Process process;

        public string ProcessName
        {
            get { return processName; }
        }

        public IntPtr MainWindowHandle
        {
            get { return process.MainWindowHandle; }
        }

        public bool ProcessFound
        {
            get
            {
                return process != null && !process.HasExited;
            }
        }

        private Process getProcess(string _processName)
        {
            Process[] processesByName = Process.GetProcessesByName(_processName);

            if (processesByName.Length <= 0)
            {
                throw new Exception("Process " + _processName + " not found");
            }

            return processesByName[0];
        }

        public bool SetProcess(string _processName)
        {
            processName = _processName;

            try
            {
                process = getProcess(_processName);
                return true;
            }
            catch (Exception)
            {
                process = null;
                return false;
            }
        }

        public Rect GetClientWindowLocation(Rect r = new Rect())
        {
            if(!this.ProcessFound) throw new Exception("No process found!");

            GetClientRect(process.MainWindowHandle, out r);

            return r;
        }

        public Rect GetEstimatedTitlebarLocation()
        {
            if (!this.ProcessFound) throw new Exception("No process found!");

            Rect windowRect = GetWindowLocation();
            Rect clientRect = GetClientWindowLocation();

            Rect r = new Rect()
            {
                Bottom = windowRect.Bottom - clientRect.Bottom,
                Left = windowRect.Left,
                Right = windowRect.Right,
                Top = windowRect.Top
            };

            return r;
        }



        public Rect GetWindowLocation(Rect r = new Rect()) // pass r to save memory allocation
        {
            if (!this.ProcessFound) throw new Exception("No process found!");

            GetWindowRect(process.MainWindowHandle, ref r);

            return r;
        }

        public bool IsWindowActive()
        {
            return process != null && process.MainWindowHandle != null && GetForegroundWindow() == process.MainWindowHandle;
        }

        public Bitmap GetScreenshot(bool crop = false, Rectangle cropRectangle = new Rectangle())
        {
            Rect windowRect = GetWindowLocation();

            int windowWidth = windowRect.Right - windowRect.Left;
            int windowHeight = windowRect.Bottom - windowRect.Top;

            int screenshotWidth = crop ? cropRectangle.Width : windowWidth;
            int screenshotHeight = crop ? cropRectangle.Height : windowHeight;

            Bitmap bmpScreenCapture = new Bitmap(screenshotWidth,
                screenshotHeight);

            using (Graphics g = Graphics.FromImage(bmpScreenCapture))
            {
                Point copyPoint = crop
                    ? new Point(windowRect.Left + cropRectangle.Left, windowRect.Top + cropRectangle.Top)
                    : new Point(windowRect.Left, windowRect.Top);

                g.CopyFromScreen(copyPoint,
                                 new Point(0, 0),
                                 bmpScreenCapture.Size);
            }


            return bmpScreenCapture;
        }

        public void SendKeystroke(ushort k, int sleep = 0)
        {
            const uint WM_KEYDOWN = 0x100;
            const uint WM_KEYUP = 0x101;

            IntPtr result3 = SendMessage(process.MainWindowHandle, WM_KEYDOWN, ((IntPtr)k), (IntPtr)0);

            if(sleep > 0) System.Threading.Thread.Sleep(sleep);

            result3 = SendMessage(process.MainWindowHandle, WM_KEYUP, ((IntPtr)k), (IntPtr)0);
        }
    }
}
