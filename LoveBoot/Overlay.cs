using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Utilities;

namespace LoveBoot
{
    public partial class Overlay : Form
    {
        List<PhysicalSignal>[] physicalGameState;
        private BotLogic owner;
        private int keyOffsetX, keyOffsetY;

        private Font overlayFont;
        private Brush[] overlayBrush;
        private Brush creditBrush;
        private Brush backgroundBrush;

        private WindowFinder windowFinder;

        private globalKeyboardHook keyboardHook;
        private const Keys toggleKey = Keys.F10;
        private const Keys toggleKeyModeKey = Keys.F9;
        private const Keys toggleAutoReadyKey = Keys.F8;
        private const Keys toggleVisibleKey = Keys.F7;
        private const Keys dumpKey = Keys.F3;

        private const string PROCESS_NOT_FOUND = "{0} was not found, please run {0} before opening this program.";

        private Bitmap creditImage, creditImageEnabled, creditImageDisabled;
        private const string CREDIT_IMAGE_B64 = "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";
        private const string CREDIT_STRING = "[LoveBoot v0.01]";

        private bool windowActive = false;
        private bool drawOverlay = true;

        /* http://stackoverflow.com/a/1524047 */
        public enum GWL
        {
            ExStyle = -20
        }

        public enum WS_EX
        {
            Transparent = 0x20,
            Layered = 0x80000
        }

        public enum LWA
        {
            ColorKey = 0x1,
            Alpha = 0x2
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        public static extern int GetWindowLong(IntPtr hWnd, GWL nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        public static extern int SetWindowLong(IntPtr hWnd, GWL nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetLayeredWindowAttributes")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hWnd, int crKey, byte alpha, LWA dwFlags);
        /* end http://stackoverflow.com/a/1524047 */

        public Overlay(BotLogic _owner, int _keyOffsetX, int _keyOffsetY, string process)
        {
            InitializeComponent();
            owner = _owner;
            keyOffsetX = _keyOffsetX;
            keyOffsetY = _keyOffsetY - 50;

            overlayFont = new Font(FontFamily.GenericMonospace, 10f);
            creditBrush = new SolidBrush(Color.Aqua);
            initializeOverlayBrushes();

            backgroundBrush = new SolidBrush(Color.Black);

            windowFinder = new WindowFinder();
            windowFinder.SetProcess(process);

            // todo: add hooks programatically
            keyboardHook = new globalKeyboardHook();
            keyboardHook.KeyDown += KeyboardHook_KeyDown;
            keyboardHook.HookedKeys.Add(toggleKey);
            keyboardHook.HookedKeys.Add(toggleVisibleKey);
            keyboardHook.HookedKeys.Add(toggleKeyModeKey);
            keyboardHook.HookedKeys.Add(toggleAutoReadyKey);
            keyboardHook.HookedKeys.Add(dumpKey);
            keyboardHook.hook();

            Byte[] bitmapData = Convert.FromBase64String(CREDIT_IMAGE_B64);
            using (System.IO.MemoryStream streamBitmap = new System.IO.MemoryStream(bitmapData))
            {
                creditImage = (Bitmap)Image.FromStream(streamBitmap);
            }

            creditImageEnabled = getColoredBitmap(creditImage, Color.FromArgb(0, 1, 0));
            creditImageDisabled = getColoredBitmap(creditImage, Color.FromArgb(1, 0, 0));

            this.DoubleBuffered = true;
            //owner.Start();
        }

        private void KeyboardHook_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == toggleKey) owner.Enabled = !owner.Enabled;
            if (e.KeyCode == toggleVisibleKey) drawOverlay = !drawOverlay;
            if (e.KeyCode == toggleKeyModeKey) owner.EightKeyMode = !owner.EightKeyMode;
            if (e.KeyCode == toggleAutoReadyKey) owner.AutoReady = !owner.AutoReady;

#if DEBUG
            if (e.KeyCode == dumpKey)
            {
                //owner.DumpKeyImage("keydump.png");
                owner.DumpDebugScreenshots();
            }
#endif

            e.Handled = true;

            this.Refresh();
        }

        private Bitmap getColoredBitmap(Bitmap original, Color newColor)
        {
            Bitmap newBitmap = new Bitmap(original);

            for (int x = 0; x < newBitmap.Width; x++)
            {
                for (int y = 0; y < newBitmap.Height; y++)
                {
                    Color newPixelColor = multiplyColor(newBitmap.GetPixel(x, y), newColor);
                    newBitmap.SetPixel(x, y, newPixelColor);
                }
            }

            return newBitmap;
        }

        private Color multiplyColor(Color a, Color b)
        {
            return Color.FromArgb(a.R * b.R, a.G * b.G, a.B * b.B);
        }

        private void initializeOverlayBrushes()
        {
            BotLogic.Signal[] allSignals = (BotLogic.Signal[])Enum.GetValues(typeof (BotLogic.Signal));
            overlayBrush = new Brush[allSignals.Length];

            foreach (BotLogic.Signal signal in allSignals)
            {
                string signalAsText = signal.ToString();
                Color signalColor = Color.White;

                if (signalAsText.Contains("8"))
                {
                    signalColor = Color.DarkGoldenrod; // popular color
                }
                else if(signalAsText.Contains("Up"))
                {
                    signalColor = Color.DeepPink;
                }
                else if (signalAsText.Contains("Left"))
                {
                    signalColor = Color.MediumPurple;
                }
                else if (signalAsText.Contains("Right"))
                {
                    signalColor = Color.Lime;
                }
                else if (signalAsText.Contains("Down"))
                {
                    signalColor = Color.DeepSkyBlue;
                }
                else if (signalAsText.Contains("Space"))
                {
                    signalColor = Color.Yellow;
                }

                if (signalAsText.Contains("8"))
                {
                    signalColor = Color.FromArgb(signalColor.R, Math.Min(signalColor.G + 75, 255), Math.Min(signalColor.B + 100, 255));
                }

                if (signalAsText.Contains("Fever"))
                {
                    signalColor = Color.FromArgb(255, signalColor.G, signalColor.B);
                }

                overlayBrush[(int)signal] = new SolidBrush(signalColor);
            }
        }


        private void Overlay_Shown(object sender, EventArgs e)
        {
            // make form click-through (from internet somewhere)
            int wl = GetWindowLong(this.Handle, GWL.ExStyle);
            wl = wl | (int)WS_EX.Layered | (int)WS_EX.Transparent;
            SetWindowLong(this.Handle, GWL.ExStyle, wl);
            //SetLayeredWindowAttributes(this.Handle, 0, 255, LWA.Alpha);

            tmrOverlay.Enabled = true;
        }

        private void Overlay_FormClosing(object sender, FormClosingEventArgs e)
        {
            tmrOverlay.Enabled = false;
            keyboardHook.unhook();
            owner.Stop();

            // attempt to force exit if the threads never stop
            Application.Exit();
            
        }

        private string signalToFriendlyName(BotLogic.Signal signal)
        {
            string newName = signal.ToString().Replace("Key_", "").Replace("_Fever", "");

            if (!newName.Contains("_")) return newName; // fine for normal, 4-key names

            newName =
                newName.Replace("Up", "U")
                    .Replace("Down", "D")
                    .Replace("Left", "L")
                    .Replace("Right", "R")
                    .Replace("_", "");

            return newName;
        }

        private string getStatusString()
        {
            const string genericStatus = "{0}: {1}";
            const string toggleStatus = "{0}: {1} ({2})";
            const string separator = ", ";

            string formatedToggleStatus = String.Format(genericStatus, "F7", "Overlay");
            formatedToggleStatus += separator + String.Format(toggleStatus, "F8", "Auto Ready", owner.AutoReady ? "On" : "Off");
            formatedToggleStatus += separator + String.Format(toggleStatus, "F9", "Key Mode", owner.EightKeyMode ? "8" : "4");
            formatedToggleStatus += separator + String.Format(genericStatus, "F10", "Bot");

            return String.Format("{0} {1}", CREDIT_STRING, formatedToggleStatus);
        }

        private void Overlay_Paint(object sender, PaintEventArgs e)
        {
            if (!windowFinder.IsWindowActive() || !drawOverlay) return;

            const int _CREDIT_TEXT_MARGIN_X = 2;
            const int _CREDIT_TEXT_MARGIN_Y = 5;

            // draw black bar on top of window title bar. form should be click through, does not stop dragging of original window
            string statusString = getStatusString();
            SizeF statusStringSize = e.Graphics.MeasureString(statusString, overlayFont);

            WindowFinder.Rect titlebarRect = windowFinder.GetEstimatedTitlebarLocation();
            int titlebarWidth = titlebarRect.Right - titlebarRect.Left;
            int titlebarHeight = titlebarRect.Bottom - titlebarRect.Top - 2; // - 2 to account for window edges.

            e.Graphics.FillRectangle(backgroundBrush, 0, 0, Math.Max(titlebarWidth, statusStringSize.Width + _CREDIT_TEXT_MARGIN_X * 2), Math.Max(titlebarHeight, statusStringSize.Height + _CREDIT_TEXT_MARGIN_Y * 2));
            e.Graphics.DrawString(statusString, overlayFont, creditBrush, _CREDIT_TEXT_MARGIN_X, _CREDIT_TEXT_MARGIN_Y);

            // gaben face top right
            int gabenSize = titlebarHeight;
            e.Graphics.DrawImage(owner.Enabled ? creditImageEnabled : creditImageDisabled, new Rectangle(this.Width - gabenSize, 0, gabenSize, gabenSize));

            // only draw keys if the array has been initialized (may be no keys)
            if (physicalGameState == null || physicalGameState.Length <= 0) return;

            bool atLeastOne = false;

            for (int i = 0; i < physicalGameState.Length; i++)
            {
                if (physicalGameState[i].Count > 0)
                {
                    atLeastOne = true;
                    break;
                }
            }

            // only draw keys if there is at least one key to draw
            if (!atLeastOne) return;

            e.Graphics.FillRectangle(backgroundBrush, 0, keyOffsetY - 5, this.Width, 30);

            for (int i = 0; i < physicalGameState.Length; i++)
            {
                foreach (PhysicalSignal ps in physicalGameState[i])
                {
                    e.Graphics.DrawString(signalToFriendlyName(ps.Type), overlayFont, overlayBrush[(int)ps.Type], keyOffsetX + ps.PositionX, keyOffsetY);
                }
            }
        }

        private void Overlay_Load(object sender, EventArgs e)
        {
            if (!windowFinder.ProcessFound)
            {
                MessageBox.Show(String.Format(PROCESS_NOT_FOUND, windowFinder.ProcessName));
                this.Close();
                return;
            }

#if !DEBUG
            MessageBox.Show("Using this in real games negatively affects other players.\r\n" +
                            "If the bot is activated, it will send an in-game message every couple of minutes.\r\n" +
                            "If you do not agree or do not want this to happen, do not activate the bot.");
#endif

            fixPosition();
            this.Refresh();
        }

        private void fixPosition()
        {
            // position this form over our found form, size and location
            if (!windowFinder.ProcessFound) return;

            bool windowActive = windowFinder.IsWindowActive();
            if (windowActive)
            {
                // toggle topmost does not break form focus (too much)
                this.TopMost = true;
                this.TopMost = false;
            }

            WindowFinder.Rect windowRect = windowFinder.GetWindowLocation();
            if (this.Location.X != windowRect.Left || this.Location.Y != windowRect.Top)
            {
                this.Size = new Size(windowRect.Right - windowRect.Left, windowRect.Bottom - windowRect.Top);
                this.Location = new Point(windowRect.Left, windowRect.Top);
            }
        }

        private int tmrOverlayTicks = 0;
        private void tmrOverlay_Tick(object sender, EventArgs e)
        {
            if (!windowFinder.ProcessFound) this.Close(); // attempts to close when the process isn't found, does not really work

            if (tmrOverlayTicks % 5 == 0)
            {
                fixPosition();
                tmrOverlayTicks = 0;
            }

            bool refresh = false;

            // redraw if window state changed
            bool newWindowActiveState = windowFinder.IsWindowActive();
            if (newWindowActiveState != windowActive)
            {
                windowActive = newWindowActiveState;
                refresh = true;
            }

            // redraw if key state changed
            List<PhysicalSignal>[] newGameState = owner.GetPhysicalGameState();
            if (newGameState != physicalGameState)
            {
                physicalGameState = newGameState;
                refresh = true;
            }
            if(refresh) this.Refresh();

            tmrOverlayTicks++;
        }
    }
}
