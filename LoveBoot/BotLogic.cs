using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using WindowsInput;

namespace LoveBoot
{
    public class BotLogic
    {
        // If input is not working:
        // If game is run as admin, bot needs to be run as admin

#if DEBUG
        List<Image<Bgr, byte>> debugImages = new List<Image<Bgr, byte>>(); 
#endif

        public enum Signal
        {
            Bar_Space,
            Bar_Space_Alt,

            Bar_Key,
            Bar_Key_Fever,

            Key_Space,
            Key_Space_Fever,

            Key_Down,
            Key_Up,
            Key_Left,
            Key_Right,

            Key_Down_Fever,
            Key_Up_Fever,
            Key_Left_Fever,
            Key_Right_Fever,

            Key_8_Down_Left,
            Key_8_Down_Right,
            Key_8_Up_Left,
            Key_8_Up_Right,

            Key_8_Down_Left_Fever,
            Key_8_Down_Right_Fever,
            Key_8_Up_Left_Fever,
            Key_8_Up_Right_Fever,
        }

        private const string IMAGE_PATH = "Images\\";
        private const string IMAGE_EXT = ".png";

        //private const int CROP_X = 250, CROP_Y = 473, CROP_WIDTH = 524, CROP_HEIGHT = 58;
        //private const int BAR_CROP_X = 254, BAR_CROP_Y = 518, BAR_CROP_WIDTH = 524, BAR_CROP_HEIGHT = 43;

        private CropSettings keyCropSettings, barCropSettings;

        private const int KEY_COLUMNS = 4, KEY_COLUMNS_MAX = 5, KEY_COLUMNS_WIDTH = 128;
        private const int MIN_KEY_DISTANCE = 10;

        private const int TIME_BEFORE_AUTO_READY_MS = 40000; // 40s

        private const double THRESHOLD_KEY = 0.94;
        private const double THRESHOLD_BAR = 0.9;

        private WindowFinder windowFinder;
        private ImageFinder imageFinder;

        private List<PhysicalSignal>[] physicalGameState;
        private Signal[][] gameState = new Signal[KEY_COLUMNS][];
        private int lastColumnPressed = -1;

        public bool EightKeyMode = false;
        public bool AutoReady = false;

        private List<Thread> threads = new List<Thread>();

        public Overlay GameOverlay { get; private set; }

        public BotLogic(string[] processNames)
        {
            Initialize();

            string foundProcess = "";

            foreach(string processName in processNames)
            {
                if (windowFinder.SetProcess(processName)) foundProcess = processName;
            }

            if(foundProcess.Length > 0)
            {
                GameOverlay = new Overlay(this, keyCropSettings.X, keyCropSettings.Y, foundProcess);
            }
            else if (MessageBox.Show("No process found, is LoveBeat running?", Application.ProductName, MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
            {
                using (ProcessPicker p = new ProcessPicker())
                {
                    DialogResult dr = p.ShowDialog();
                    if (dr != DialogResult.OK) return;

                    string processName = p.PickedProcessName;
                    windowFinder.SetProcess(processName);
                    GameOverlay = new Overlay(this, keyCropSettings.X, keyCropSettings.Y, processName);
                }
            }
        }

        public BotLogic(string processName)
        {
            Initialize();
            windowFinder.SetProcess(processName);
            GameOverlay = new Overlay(this, keyCropSettings.X, keyCropSettings.Y, processName);
        }

        private void Initialize()
        {
            const string KEY_CROP_FILENAME = "key.lvb";
            const int KEY_CROP_X = 250, KEY_CROP_Y = 473, KEY_CROP_WIDTH = 524, KEY_CROP_HEIGHT = 58;
            const string BAR_CROP_FILENAME = "bar.lvb";
            const int BAR_CROP_X = 254, BAR_CROP_Y = 518, BAR_CROP_WIDTH = 524, BAR_CROP_HEIGHT = 43;

            windowFinder = new WindowFinder();
            imageFinder = new ImageFinder(0.9);

            // load all images based on Signal types. throws error if any of these are not found
            foreach (Signal s in Enum.GetValues(typeof(Signal)))
            {
                string subImagePath = String.Format("{0}{1}{2}", IMAGE_PATH, s.ToString(), IMAGE_EXT);
                imageFinder.SubImages.Add(s, new Image<Bgr, byte>(subImagePath));
            }

            // init gamestate
            for (int i = 0; i < gameState.Length; i++)
            {
                gameState[i] = new Signal[KEY_COLUMNS_MAX];
            }

            keyCropSettings = CropSettings.Load(KEY_CROP_FILENAME, KEY_CROP_X, KEY_CROP_Y, KEY_CROP_WIDTH, KEY_CROP_HEIGHT);
            keyCropSettings.SaveIfNotExist(KEY_CROP_FILENAME);

            barCropSettings = CropSettings.Load(BAR_CROP_FILENAME, BAR_CROP_X, BAR_CROP_Y, BAR_CROP_WIDTH, BAR_CROP_HEIGHT);
            barCropSettings.SaveIfNotExist(BAR_CROP_FILENAME);
        }

        private bool _Enabled = false;

        public bool Enabled
        {
            get { return _Enabled; }
            set
            {
                _Enabled = value;
                if (_Enabled)
                {
                    Start();
                }
                else
                {
                    Stop();
                }
            }
        }

        public void EnableOverlay()
        {
            GameOverlay.Show();
        }

        public void DisableOverlay()
        {
            GameOverlay.Hide();
        }

        private Image<Bgr, byte> getCroppedScreenshot(CropSettings cs)
        {
            try
            {
                // TODO: determine crop height on the fly
                Image<Bgr, byte> convertedScreenshot;
                using (Bitmap screenshot = windowFinder.GetScreenshot(true,
                    new Rectangle(cs.X, cs.Y, cs.Width, cs.Height)))
                {
                    convertedScreenshot = new Image<Bgr, byte>(screenshot);
                }

                return convertedScreenshot;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private Image<Bgr, byte> getCroppedKeyScreenshot()
        {
            return getCroppedScreenshot(keyCropSettings);
        }

        private Image<Bgr, byte> getCroppedBarScreenshot()
        {
            return getCroppedScreenshot(barCropSettings);
        }

        private void InitializeImageFinder(ImageFinder imgFinder, string startsWith = "")
        {
            foreach (Signal s in Enum.GetValues(typeof(Signal)))
            {
                if (startsWith.Length > 0 && !s.ToString().StartsWith(startsWith)) continue;

                string subImagePath = String.Format("{0}{1}{2}", IMAGE_PATH, s.ToString(), IMAGE_EXT);
                imgFinder.SubImages.Add(s, new Image<Bgr, byte>(subImagePath));
            }
        }

        public void Start()
        {
            _Enabled = true;

            stopThreads();

            threads.Clear();

            Thread pressThread = new Thread(new ThreadStart(PressThread));
            Thread stateThread = new Thread(new ThreadStart(StateThread));
            Thread spamThread = new Thread(new ThreadStart(SpamThread));

            pressThread.Start();
            stateThread.Start();
            spamThread.Start();

            threads.Add(pressThread);
            threads.Add(stateThread);
            threads.Add(spamThread);
        }

        public void Stop()
        {
            _Enabled = false;
            stopThreads();
        }

        private void stopThreads()
        {
            foreach (Thread t in threads)
            {
                if (!t.IsAlive) continue;
                t.Abort();
            }
        }

        public void DumpKeyImage(string path)
        {
            using (Image <Bgr, byte> screen = getCroppedKeyScreenshot())
            {
                screen.Save(path);
            }
        }

        public Image<Bgr, byte> GetBarImage()
        {
            return getCroppedBarScreenshot();
        }

        private void PressKeys(int columnToPress)
        {
            Signal[] column = gameState[columnToPress];

            for (int i = 0; i < column.Length; i++)
            {
                switch (column[i])
                {
                    case Signal.Key_Down:
                    case Signal.Key_Down_Fever:
                        windowFinder.SendKeystroke((ushort)VirtualKeyCode.DOWN);
                        break;
                    case Signal.Key_Left:
                    case Signal.Key_Left_Fever:
                        windowFinder.SendKeystroke((ushort)VirtualKeyCode.LEFT);
                        break;
                    case Signal.Key_Right:
                    case Signal.Key_Right_Fever:
                        windowFinder.SendKeystroke((ushort)VirtualKeyCode.RIGHT);
                        break;
                    case Signal.Key_Up:
                    case Signal.Key_Up_Fever:
                        windowFinder.SendKeystroke((ushort)VirtualKeyCode.UP);
                        break;

                    // 8 key
                    case Signal.Key_8_Down_Left:
                    case Signal.Key_8_Down_Left_Fever:
                        windowFinder.SendKeystroke((ushort)VirtualKeyCode.NUMPAD1);
                        break;
                    case Signal.Key_8_Down_Right:
                    case Signal.Key_8_Down_Right_Fever:
                        windowFinder.SendKeystroke((ushort)VirtualKeyCode.NUMPAD3);
                        break;
                    case Signal.Key_8_Up_Left:
                    case Signal.Key_8_Up_Left_Fever:
                        windowFinder.SendKeystroke((ushort)VirtualKeyCode.NUMPAD7);
                        break;
                    case Signal.Key_8_Up_Right:
                    case Signal.Key_8_Up_Right_Fever:
                        windowFinder.SendKeystroke((ushort)VirtualKeyCode.NUMPAD9);
                        break;

                    default:
                        continue;
                }

                System.Threading.Thread.Sleep(5);
            }
        }

        public List<PhysicalSignal>[] GetPhysicalGameState()
        {
            return physicalGameState;
        }

#if DEBUG
        public void DumpDebugScreenshots()
        {
            const string folderName = "DebugImages\\";
            const string ext = ".png";

            for(int i = 0; i < debugImages.Count; i++)
            {
                debugImages[i].Save(folderName + i + ext);
            }
        }

        private void addDebugImage(Image<Bgr, byte> original, KeyValuePair<object, Rectangle[]> match)
        {
            Image<Bgr, byte> copy = original.Copy();
            debugImages.Add(copy.Copy());
            copy.Draw(match.Value[0], new Bgr(getDebugColor((Signal)match.Key)));
            debugImages.Add(copy);
        }

        private Color getDebugColor(Signal signal)
        {
            string signalAsText = signal.ToString();
            Color signalColor = Color.White;

            if (signalAsText.Contains("Up"))
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

            if (signalAsText.Contains("Fever"))
            {
                signalColor = Color.FromArgb(255, signalColor.G, signalColor.B);
            }

            return signalColor;
        }
#endif

        public void SpamThread()
        {
#if DEBUG
     return;
#endif
            // put this in to protect against ruining the game
            // please do not publically release without it
            // it is not meant as "real" protection, and can be easily disabled (run in debug mode, or strip this code)

            const int MIN_DELAY = 60000; // 1m
            const int MAX_DELAY = 120000; // 2m

            string[] phrases = new string[]
            {
                "I am using a bot.",
                "I'm playing with LoveBoot!",
                "Report me!",
                "LoveBoot was here",
                "ban me @boo",
            };

            string[] channels = new string[]
            {
                "/s",
                "/f"
            };

            Random r = new Random();

            while (_Enabled)
            {
                System.Threading.Thread.Sleep(r.Next(MIN_DELAY, MAX_DELAY));

                if (!_Enabled) continue;

                string phrase = phrases[r.Next(0, phrases.Length)];

                foreach (string channel in channels)
                {
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    InputSimulator.SimulateTextEntry(channel);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);

                    System.Threading.Thread.Sleep(50);

                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    InputSimulator.SimulateTextEntry(phrase);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                }
            }
        }

        public void PressThread()
        {
            ImageFinder barImageFinder = new ImageFinder(THRESHOLD_BAR);
            InitializeImageFinder(barImageFinder, "Bar_");

            // if the press-key signal is found, presses the keys found by the state thread

            while (_Enabled)
            {
                using (Image<Bgr, byte> bar_image_source = getCroppedBarScreenshot())
                {
                    if (bar_image_source == null) continue;

                    Dictionary<object, Rectangle[]> matches = barImageFinder.FindAllMatches(bar_image_source, "", false);

                    foreach (KeyValuePair<object, Rectangle[]> pairs in matches)
                    {
                        if (pairs.Value.Length <= 0) continue;

                        Signal matchSignal = (Signal) pairs.Key;

                        int barColumn = pairs.Value[0].Left / KEY_COLUMNS_WIDTH;
                        if (barColumn == lastColumnPressed) continue;

                        if (matchSignal == Signal.Bar_Key || matchSignal == Signal.Bar_Key_Fever)
                        {
                            lastColumnPressed = barColumn;
                            PressKeys(barColumn);

#if DEBUG
                            addDebugImage(bar_image_source, pairs);
#endif

                            System.Threading.Thread.Sleep(50);

                            break;
                        }
                        else // todo: check bar_space explicitly
                        {
                            if (
                                !(gameState[barColumn].Contains(Signal.Key_Space) ||
                                  gameState[barColumn].Contains(Signal.Key_Space_Fever))) continue; // don't press space if there is no space to be pressed, attempt to ignore false positive

                            lastColumnPressed = barColumn;
                            if(matchSignal == Signal.Bar_Space_Alt) System.Threading.Thread.Sleep((pairs.Value[0].Top * 3) / 2); // inaccurate
                            windowFinder.SendKeystroke((ushort)VirtualKeyCode.SPACE);

#if DEBUG
                            addDebugImage(bar_image_source, pairs);
#endif

                            System.Threading.Thread.Sleep(100);
                            break;
                        }
                    }
                }
            }
        }

        public void StateThread()
        {
            ImageFinder stateImageFinder = new ImageFinder(THRESHOLD_KEY);
            InitializeImageFinder(stateImageFinder, "Key_");

            // continously updates key state by recognizing visible keys

            // stopwatch used for autoready
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (_Enabled)
            {
                List<PhysicalSignal>[] newGameState = new List<PhysicalSignal>[KEY_COLUMNS];

                for (int i = 0; i < newGameState.Length; i++)
                {
                    newGameState[i] = new List<PhysicalSignal>();
                }

                using (Image<Bgr, byte> key_image_source = getCroppedKeyScreenshot())
                {
                    if (key_image_source == null) continue;

                    Dictionary<object, Rectangle[]> matches = stateImageFinder.FindAllMatches(key_image_source, "", false, EightKeyMode ? "" : "Key_8_"); // ignore key_8 if eight key mode is not enabled

                    bool addedAtLeastOne = false;

                    foreach (KeyValuePair<object, Rectangle[]> pairs in matches)
                    {
                        if (pairs.Value.Length <= 0) continue;

                        foreach (Rectangle match in pairs.Value)
                        {
                            PhysicalSignal physicalSignal = new PhysicalSignal()
                            {
                                PositionX = match.Left,
                                PositionY = match.Top,
                                Type = (Signal) pairs.Key
                            };

                            // estimate column by match location
                            // possibility this may be off if key_image off-center
                            int column = match.Left / KEY_COLUMNS_WIDTH;
                            if (column > KEY_COLUMNS - 1) column = KEY_COLUMNS - 1;

                            newGameState[column].Add(physicalSignal);
                            addedAtLeastOne = true;
                        }
                    }

                    if (addedAtLeastOne)
                    {
                        stopwatch.Restart();
                    }
                    else if(AutoReady && stopwatch.ElapsedMilliseconds > TIME_BEFORE_AUTO_READY_MS)
                    {
                        windowFinder.SendKeystroke((ushort)VirtualKeyCode.F5);
                        stopwatch.Reset(); // reset ensures bot does not auto ready more than once (un-readying). can be bad if it misses ready moment, if cooldown is too short
                    }
                }

                Signal[][] rawGameState = new Signal[KEY_COLUMNS][];
                List<PhysicalSignal>[] oldPhysicalGameState = physicalGameState;

                for (int i = 0; i < newGameState.Length; i++)
                {
                    if (newGameState[i].Count > 0 && oldPhysicalGameState[i].Count > 1 && oldPhysicalGameState[i].Count > newGameState[i].Count)
                    {
                        // TEST: replaces gamestates per column if old game state had >=2 keys and this state has less, but not 0 (attempt to "fix" keys occasionally not being recognized in high bpm songs due to particles)
                        // similar to caching
                        // works
                        // TODO: proper
                        newGameState[i] = oldPhysicalGameState[i];
                    }

                    PhysicalSignal[] physicalSignalArray = newGameState[i].ToArray();
                    Array.Sort(physicalSignalArray);

                    rawGameState[i] = new Signal[KEY_COLUMNS_MAX];
                    for (int i2 = 0; i2 < physicalSignalArray.Length; i2++)
                    {
                        rawGameState[i][i2] = physicalSignalArray[i2].Type;
                    }
                }

                physicalGameState = newGameState;
                gameState = rawGameState;
            }
        }
    }

    public class PhysicalSignal : IComparable<PhysicalSignal>
    {
        public BotLogic.Signal Type;
        public int PositionX;
        public int PositionY;

        public int CompareTo(PhysicalSignal that)
        {
            return this.PositionX.CompareTo(that.PositionX);
        }
    }

    public class CropSettings
    {
        public int X, Y, Width, Height;

        public static CropSettings Load(string filename, int _X, int _Y, int _Width, int _Height)
        {
            string[] lines;

            try
            {
                lines = System.IO.File.ReadAllLines(filename);
            }
            catch (Exception)
            {
                lines = new string[0];
            }

            int[] parsedLines = new int[0];

            if (lines.Length >= 4)
            {
                try
                {
                    parsedLines = new int[] { Convert.ToInt32(lines[0]), Convert.ToInt32(lines[1]), Convert.ToInt32(lines[2]), Convert.ToInt32(lines[3]) };
                }
                catch (Exception)
                {
                    parsedLines = new int[0];
                }
            }

            if (parsedLines.Length < 4)
            {
                parsedLines = new int[] { _X, _Y, _Width, _Height };
            }

            return new CropSettings()
            {
                X = parsedLines[0],
                Y = parsedLines[1],
                Width = parsedLines[2],
                Height = parsedLines[3]
            };
        }

        public void SaveIfNotExist(string filename)
        {
            if(!File.Exists(filename)) this.Save(filename);
        }

        public void Save(string filename)
        {
            string[] lines = new string[]
            {
                this.X.ToString(),
                this.Y.ToString(),
                this.Width.ToString(),
                this.Height.ToString()
            };

            File.WriteAllLines(filename, lines);
        }
    }
}
