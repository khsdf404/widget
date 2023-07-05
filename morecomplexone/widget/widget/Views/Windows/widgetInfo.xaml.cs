using System;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading; 
using System.Linq;
using System.Diagnostics; 
using System.IO; 
using System.Threading.Tasks; 
using System.Windows.Controls; 

namespace widget.Views.Windows {
    public partial class widgetInfo : Window {


        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);
        private static IntPtr CreateLParam(int LoWord, int HiWord) {
            return (IntPtr)((HiWord << 16) | (LoWord & 0xffff));
        }
        [DllImport("user32.dll")]
        static extern IntPtr PostMessage(IntPtr hWnd, int msg, int wParam, string lParam);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName); 
        [DllImport("User32")]
        private static extern int ShowWindow(IntPtr hwnd, int nCmdShow);
        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        private static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd); 
        [DllImport("user32.dll")]
        static extern bool GetLayeredWindowAttributes(IntPtr hwnd, out uint crKey, out byte bAlpha, out uint dwFlags);
        [DllImport("user32")]
        private static extern int SetLayeredWindowAttributes(IntPtr hWnd, byte crey, byte alpha, int flags);  
        [DllImport("user32")]
        private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdBlt, uint nFlagsc);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public dateInfo dateInfo;
        BrushConverter bc = new BrushConverter();
        private string currPath = Environment.CurrentDirectory;

        public widgetInfo() {
            InitializeComponent();
            var primaryMonitorArea = SystemParameters.WorkArea;
            Left = (primaryMonitorArea.Right / 2) - (600 / 2) + 1;
            Top = primaryMonitorArea.Bottom - 75 - 330;
  
            createMonthTable();
        }
        #region // OnLoad
        [DllImport("user32.dll", SetLastError = true)] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        private const int GWL_EX_STYLE = -20;
        private const int WS_EX_APPWINDOW = 0x00040000, WS_EX_TOOLWINDOW = 0x00000080;
        void _OnLoad_(object sender, RoutedEventArgs args) {
            var helper = new WindowInteropHelper(this).Handle;
            SetWindowLong(helper, GWL_EX_STYLE, (GetWindowLong(helper, GWL_EX_STYLE) | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);

            DesktopAPI.WindowOnDesktopShow(helper);


            rightWrap.MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler((object sender, System.Windows.Input.MouseButtonEventArgs e) => {
                if (dateInfo != null) {
                    Dispatcher.Invoke((Action)(() => { 
                        dateInfo.Focus();
                    }));
                }
            });
            SpotifyStart();  
        }
        #endregion  

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);
        [DllImport("user32.dll")]
        static extern int MapVirtualKey(int uCode, uint uMapType);
        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo(); 
        // нужный мусор  
        public struct HardwareInput
        {
            public readonly uint uMsg;
            public readonly ushort wParamL;
            public readonly ushort wParamH;
        }
        // нужный немусор
        public struct Input
        {
            public int type;
            public InputUnion Union;
        }
        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public MouseInput mouse;
            [FieldOffset(0)] public KeyboardInput keyboard;
            [FieldOffset(0)] public readonly HardwareInput hi;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardInput
        {
            public ushort KeyCode;
            public ushort ScanCode;
            public uint dwFlags;
            public readonly uint time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct MouseInput
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public readonly uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }


        public static void KeyboardEvent(int key, uint flag) {
            Input[] Input = {
                new Input {
                    type = 1, // Keyboard
                    Union = new InputUnion {
                        keyboard = new KeyboardInput {
                            KeyCode = (ushort) key,
                            ScanCode = (ushort) MapVirtualKey(key, 0),
                            dwFlags = flag,
                            dwExtraInfo = GetMessageExtraInfo()
                        }
                    }
                }
            };
            SendInput(1, Input, Marshal.SizeOf(typeof(Input)));
        }







        #region // Spotify
        bool spotifyUsing = false;
        Process spotifyProcess = null;
        IntPtr spotifyHWND = IntPtr.Zero;
        bool wasHidden = false;
        bool durationChanging = false;
        Process ActualSpotifyProcess() {
            return Process.GetProcessById(spotifyProcess.Id);
        }

        async void SpotifyStart() {
            // TODO: check if 
            // Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
            // @"\appdata\local\microsoft\windowsapps\spotify.exe");
            // exists 
            spotifyUsing = true; 
            CheckSpotifyIsOpen();
            bool isHidden = !IsWindowVisible(spotifyHWND);
            bool isMinimized = IsIconic(spotifyHWND);
            byte bAlpha; uint crKey; uint dwFlags;
                GetLayeredWindowAttributes(spotifyHWND, out crKey, out bAlpha, out dwFlags); 
            if (isHidden)
                ForceGetSpotify();
            if (isHidden || isMinimized || (dwFlags != 0))
                SetInvincible();
            string spotifyTitle = ActualSpotifyProcess().MainWindowTitle;
            SetTrackName(spotifyTitle); 
            wasHidden = isHidden || isMinimized || (dwFlags != 0);
            SetSpotifyImage(wasHidden);
            SetPlayImage(spotifyTitle); 
            SetDuration();
            SetSpotifyWindowScaling();
            await Task.Run(() => { 
                Dispatcher.Invoke((Action)(() => {
                    TranslateTransform offsetPoint = new TranslateTransform();
                    trackAnim.RenderTransform = offsetPoint;
                    Grid thisGrid = (Grid)trackBlock.Parent;
                    Border thisParent = (Border)thisGrid.Parent;
                    offsetPoint.X = thisParent.ActualWidth;
                    AnimMovingTrack(0, 0);
                }));
            });
            spotifyUsing = false;
            SetDurationSlide();
            spotifyTimer(); 
        }
        void CheckSpotifyIsOpen() {
            if (spotifyProcess == null || spotifyProcess.HasExited)
                ForceGetSpotify();
        }
        Process FindProcess(string processName) {
            System.Diagnostics.Process[] process = System.Diagnostics.Process.GetProcesses();
            foreach (System.Diagnostics.Process p in process)
                if (p.ProcessName == processName) return p;
            return null;
        }
        void ForceGetSpotify() {
            // reset force trigger 
            spotifyProcess = FindProcess("Spotify");
            spotifyHWND = spotifyProcess == null ?
                    IntPtr.Zero :
                    spotifyProcess.MainWindowHandle;
            if (spotifyHWND != IntPtr.Zero) return;
            Process.Start("explorer",
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                @"\appdata\local\microsoft\windowsapps\spotify.exe");
            while (spotifyHWND == IntPtr.Zero) {
                spotifyProcess = FindProcess("Spotify");
                spotifyHWND = spotifyProcess == null ?
                    IntPtr.Zero :
                    spotifyProcess.MainWindowHandle;
            }
        }

        // Setters n' Getters
        void SetVisible() {
            ShowWindow(spotifyHWND, 5); 
            SetForegroundWindow(spotifyHWND);  
            SetWindowLong(spotifyHWND, -20, GetWindowLong(spotifyHWND, -20) & ~0x00000080);
            wasHidden = false;
        }
        void SetInvincible() {  
            if (IsIconic(spotifyHWND))
                ShowWindow(spotifyHWND, 0);
            ShowWindow(spotifyHWND, 4);
            // set 0 opacitty 
            SetWindowLong(spotifyHWND, -20, GetWindowLong(spotifyHWND, -20) | 0x00080000);
            SetLayeredWindowAttributes(spotifyHWND, 0, 0, 0x00000002); 
            // hide from taskbar
            SetWindowLong(spotifyHWND, -20, GetWindowLong(spotifyHWND, -20) | 0x00000080); 
            wasHidden = true;
        }
          
        void SetTrackName(string trackName) { 
            Dispatcher.Invoke((Action)(() => {
                trackBlock.Text = (trackName == "Spotify Premium" || trackName == "Spotify" || trackName == "") ?
                        (trackBlock.Text == " ") ?
                            "Nothing's playing" :
                            trackBlock.Text :
                        trackName;
                trackAnim.Text = trackBlock.Text;
            }));
        } 
        async void AnimMovingTrack(int which, double startWidth) {  
            TranslateTransform offsetPoint = new TranslateTransform(); 
            if (which == 0) trackBlock.RenderTransform = offsetPoint; 
            else trackAnim.RenderTransform = offsetPoint;  

            Grid thisGrid = (Grid)trackBlock.Parent;
            Border thisParent = (Border)thisGrid.Parent; 
            double areaWidth = 153;
            double oldActualWidth = trackBlock.ActualWidth;
            int speed = 15; 
            bool stopPoint = true;
            offsetPoint.X = areaWidth;  
            double xOffset = areaWidth; 
            while (-1 * trackBlock.ActualWidth < xOffset) {
                // track changed
                if (oldActualWidth != trackBlock.ActualWidth && startWidth != trackBlock.ActualWidth) {  
                    offsetPoint.X = 1000;
                    Debug.WriteLine(which);
                    AnimMovingTrack(1 - which, trackBlock.ActualWidth);
                    break;
                }
                if (stopPoint && -1 * (trackBlock.ActualWidth - 8 * areaWidth / 16) > xOffset) { 
                    stopPoint = false;
                    Debug.WriteLine("Started" + ", " + which);
                    AnimMovingTrack(1 - which, startWidth);
                }
                thisParent.Width = trackBlock.ActualWidth; 
                xOffset -= 1;
                offsetPoint.X = xOffset;
                await Task.Delay(speed);
            }
            // thisParent.Width = areaWidth;
             offsetPoint.X = areaWidth;
        }
        void GetTrackTime(string trackName) {
            trackName = trackName.Replace("-", " - ");
            string searchFormat = "";
            foreach (string word in trackName.Split(" "))
                searchFormat += word + "+";
            Uri uri = new Uri("https://ru.hitmotop.com/search?q=" + searchFormat);
            string html = new System.Net.WebClient().DownloadString(uri);
            string firstTime = html
                .Split("<div class=\"track__fulltime\">")[1]
                .Split("</div>")[0];
        }

        void SetSpotifyImage(bool isHidden) {
            string resourcesPath = "/views/windows/images/";
            string reverseAddition = !isHidden ?
                "-reverse"
                : "";
            Dispatcher.Invoke((Action)(() => {
                spotifyImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                new Uri(resourcesPath + "share" + reverseAddition + ".png", UriKind.Relative));
            }));
        }

        void SetButtonOpacity(Border imageParent, double currOpacity) {
            Dispatcher.Invoke((Action)(() => {
                imageParent.Opacity = currOpacity;
            }));
        }
        void SetPlayImage(string windowTitle) {
            int nextHeight;
            string resourcesPath = "/views/windows/images/";
            if (windowTitle == "Spotify" || windowTitle == "Spotify Premium" || windowTitle == " ") {
                nextHeight = 60;
                resourcesPath += "play.png";
            }
            else {
                nextHeight = 68;
                resourcesPath += "pause.png";
            }
            Dispatcher.Invoke((Action)(() => {
                pauseImage.Height = nextHeight;
                pauseImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                new Uri(resourcesPath, UriKind.Relative));
            }));
        }

        RECT lpRect = new RECT();
        static System.Drawing.Bitmap bmpPicture = new System.Drawing.Bitmap(1, 1);
        static System.Drawing.Graphics gfxPicture = System.Drawing.Graphics.FromImage(bmpPicture); 
        double GetCurrentDuration() {
            const int constDarkGrey = 83;
            const int constLightGrey = 179;  
            GetWindowRect(spotifyHWND, out lpRect);
            int spotifyWidth = lpRect.Right - lpRect.Left;
            int spotifyHeight = lpRect.Bottom - lpRect.Top;
            bmpPicture = new System.Drawing.Bitmap(spotifyWidth, spotifyHeight);
            gfxPicture = System.Drawing.Graphics.FromImage(bmpPicture);
            IntPtr graphicHWND = gfxPicture.GetHdc();
            PrintWindow(spotifyHWND, graphicHWND, 0);
            gfxPicture.ReleaseHdc(graphicHWND);
            // progressBar stats   
            double approximConst = -0.021478 * spotifyWidth + 44.178634;
            int x0 = spotifyWidth / 3 + (int)approximConst;
            int x1 = spotifyWidth * 2 / 3 - (int)approximConst;
            int x = x0 + (x1 - x0) / 2;
            int barWidth = x1 - x0;
            int progressBarBottomOffset = 24;
            if (bmpPicture.GetPixel(spotifyWidth - 45, spotifyHeight - 15).G > 100)
                progressBarBottomOffset += 25;
            int y = spotifyHeight - progressBarBottomOffset; 
             

            // binarySearch
            while (x > x0 + 1 && x < x1 - 1 && x != x1) {
                if (bmpPicture.GetPixel(x, y).G != constDarkGrey || 
                    bmpPicture.GetPixel(x, y).R != constDarkGrey) { 
                    x = x + (x1 - x) / 2; 
                    continue;
                } 
                x = x - (x1 - x) / 2;
                x1 = x1 - Convert.ToInt32((x1 - x) / 1.5);
            }
            if (bmpPicture.GetPixel(x-1, y).R != constLightGrey)
                x -= 6; 
            return Convert.ToDouble(x - x0) / barWidth * 100;
        }
        void SetDuration() {
            if (durationChanging)
                return;
            Dispatcher.Invoke((Action)(() => {
                TranslateTransform offsetPoint = new TranslateTransform();
                durationBar.Width = 1.9 * Math.Abs(GetCurrentDuration() - 1);
                offsetPoint.X = durationBar.Width;
                durationCircle.RenderTransform = offsetPoint;
            }));
        }
        void SetSpotifyDuration() {
            if (wasHidden)  
                SetLayeredWindowAttributes(spotifyHWND, 0, 1, 0x00000002);
            CheckSpotifyIsOpen();
            SetForegroundWindow(spotifyHWND);
            GetWindowRect(spotifyHWND, out lpRect);
            int spotifyWidth = lpRect.Right - lpRect.Left;
            int spotifyHeight = lpRect.Bottom - lpRect.Top;
            double approximConst = -0.021478 * spotifyWidth + 44.178634;
            int x0 = spotifyWidth / 3 + (int)approximConst;
            int x1 = spotifyWidth * 2 / 3 - (int)approximConst; 
            int progressBarBottomOffset = 23;
            if (bmpPicture.GetPixel(spotifyWidth - 45, spotifyHeight - 15).G > 100)
                progressBarBottomOffset += 25;
            int y = spotifyHeight - progressBarBottomOffset; 
            double x = x0 + (x1 - x0)*(durationBar.ActualWidth / durationBorder.ActualWidth);

            SendMessage(spotifyHWND, 0x201, 0x0001, CreateLParam((int)x, y));
            SendMessage(spotifyHWND, 0x202, 0x0001, CreateLParam((int)x, y));
            SendMessage(spotifyHWND, 0x201, 0x0001, CreateLParam(spotifyWidth-10, spotifyHeight-10));
            SendMessage(spotifyHWND, 0x202, 0x0001, CreateLParam(spotifyWidth-10, spotifyHeight-10));

            if (wasHidden)
                SetInvincible(); 
        } 
        void DurationMoveEvent(object sender, System.Windows.Input.MouseEventArgs e) {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) {
                durationChanging = true;
                if (e.GetPosition(durationBorder).X / durationBorder.ActualWidth > 1 || e.GetPosition(durationBorder).X < 3) {
                    spotifyWrap.MouseMove -= DurationMoveEvent;
                    SetSpotifyDuration();
                    durationChanging = false;
                    return;
                }
                Dispatcher.Invoke((Action)(() => {
                    TranslateTransform offsetPoint = new TranslateTransform();
                    durationBar.Width = 1.9 * Math.Abs(100 * e.GetPosition(durationBorder).X / durationBorder.ActualWidth - 1);
                    offsetPoint.X = durationBar.Width;
                    durationCircle.RenderTransform = offsetPoint;
                }));
            }
        }
        void SetDurationSlide() {
            durationBorder.MouseDown += new System.Windows.Input.MouseButtonEventHandler((object sender, System.Windows.Input.MouseButtonEventArgs e) => {
                spotifyWrap.MouseMove += DurationMoveEvent;
            });
            MouseUp += new System.Windows.Input.MouseButtonEventHandler((object sender, System.Windows.Input.MouseButtonEventArgs e) => {
                if (durationChanging) {
                    spotifyWrap.MouseMove -= DurationMoveEvent;
                    SetSpotifyDuration();
                    durationChanging = false;
                }
            });
            MouseLeave += new System.Windows.Input.MouseEventHandler((object sender, System.Windows.Input.MouseEventArgs e) => {
                if (durationChanging) {
                    spotifyWrap.MouseMove -= DurationMoveEvent;
                    SetSpotifyDuration();
                    durationChanging = false;
                }
            });
        }




        void SetSpotifyWindowScaling() {   
            int spotifyWidth = lpRect.Right - lpRect.Left;
            int spotifyHeight = lpRect.Bottom - lpRect.Top; 
            int progressBarBottomOffset = 97;
            if (bmpPicture.GetPixel(spotifyWidth - 45, spotifyHeight - 15).G > 100)
                progressBarBottomOffset += 25;
            int y = spotifyHeight - progressBarBottomOffset;
             
            if (bmpPicture.GetPixel(20, y).R == 40)
                return;

            SetForegroundWindow(spotifyHWND); 
            KeyboardEvent(162, 0); 
            KeyboardEvent(48, 0); 
            KeyboardEvent(48, 0x02); 
            KeyboardEvent(162, 0x02); 
        }

        // Control's funcs
        async void _spotifyPlay_(object sender, RoutedEventArgs e) { 
            if (spotifyUsing) return;
            spotifyUsing = true;
            Button thisBtn = (Button)sender;
            Grid thisGrid = (Grid)thisBtn.Parent;
            Border thisParent = (Border)thisGrid.Parent;
            await Task.Run(() => {
                SetButtonOpacity(thisParent, 0.2);
                if (dateInfo != null)
                {
                    Dispatcher.Invoke((Action)(() => {
                        dateInfo.Focus();
                    }));
                }
            });
            await Task.Run(() => {
                CheckSpotifyIsOpen();
                int x = (lpRect.Right - lpRect.Left) / 2;
                int y = (lpRect.Bottom - lpRect.Top) - 60;
                if (bmpPicture.GetPixel((lpRect.Right - lpRect.Left) - 45, (lpRect.Bottom - lpRect.Top) - 15).G > 100)
                    y -= 25;
                CheckSpotifyIsOpen();   
                SendMessage(spotifyHWND, 0x201, 0x0001, CreateLParam(x, y));
                SendMessage(spotifyHWND, 0x202, 0x0001, CreateLParam(x, y));

                SetTrackName(ActualSpotifyProcess().MainWindowTitle);
                SetPlayImage(ActualSpotifyProcess().MainWindowTitle); 
                SetButtonOpacity(thisParent, 1);
                spotifyUsing = false; 
            });
        }
        async void _spotifyNext_(object sender, RoutedEventArgs e) {
            if (spotifyUsing) return;
            spotifyUsing = true;
            Button thisBtn = (Button)sender;
            Grid thisGrid = (Grid)thisBtn.Parent;
            Border thisParent = (Border)thisGrid.Parent;
            await Task.Run(() => {
                SetButtonOpacity(thisParent, 0.2);
                if (dateInfo != null)
                {
                    Dispatcher.Invoke((Action)(() => {
                        dateInfo.Focus();
                    }));
                }
            });
            await Task.Run(() => { 
                CheckSpotifyIsOpen();

                int x = (lpRect.Right - lpRect.Left) / 2 + 40;
                int y = (lpRect.Bottom - lpRect.Top) - 60;
                if (bmpPicture.GetPixel((lpRect.Right - lpRect.Left) - 45, (lpRect.Bottom - lpRect.Top) - 15).G > 100)
                    y -= 25;
                CheckSpotifyIsOpen();
                SendMessage(spotifyHWND, 0x201, 0x0001, CreateLParam(x, y));
                SendMessage(spotifyHWND, 0x202, 0x0001, CreateLParam(x, y));

                SetTrackName(ActualSpotifyProcess().MainWindowTitle);
                SetPlayImage(ActualSpotifyProcess().MainWindowTitle); 
                SetButtonOpacity(thisParent, 1);
                spotifyUsing = false;
            });
        }
        async void _spotifyPrev_(object sender, RoutedEventArgs e) {
            if (spotifyUsing) return;
            spotifyUsing = true;
            Button thisBtn = (Button)sender;
            Grid thisGrid = (Grid)thisBtn.Parent;
            Border thisParent = (Border)thisGrid.Parent;
            await Task.Run(() => {
                SetButtonOpacity(thisParent, 0.2);
                if (dateInfo != null)
                {
                    Dispatcher.Invoke((Action)(() => {
                        dateInfo.Focus();
                    }));
                }
            });
            await Task.Run(async () => {
                CheckSpotifyIsOpen();

                int x = (lpRect.Right - lpRect.Left) / 2;
                int y = (lpRect.Bottom - lpRect.Top) - 60;
                if (bmpPicture.GetPixel((lpRect.Right - lpRect.Left) - 45, (lpRect.Bottom - lpRect.Top) - 15).G > 100)
                    y -= 25;
                CheckSpotifyIsOpen();
                SendMessage(spotifyHWND, 0x201, 0x0001, CreateLParam(x, y));
                SendMessage(spotifyHWND, 0x202, 0x0001, CreateLParam(x, y));

                SetTrackName(ActualSpotifyProcess().MainWindowTitle);
                SetPlayImage(ActualSpotifyProcess().MainWindowTitle); 
                SetButtonOpacity(thisParent, 1);
                spotifyUsing = false;
            });
        } 
        void _openSpotify_(object sender, RoutedEventArgs e) {
            if (dateInfo != null)
            {
                Dispatcher.Invoke((Action)(() => {
                    dateInfo.Focus();
                }));
            }
            if (!wasHidden) {
                SetSpotifyImage(true);
                SetInvincible(); 
                return;
            }
            SetSpotifyImage(false);
            CheckSpotifyIsOpen();
            SetVisible(); 
        }

        
        void spotifyTimer() {
            DispatcherTimer sTimer = new DispatcherTimer();
            sTimer.Interval = TimeSpan.FromMilliseconds(1000);
            sTimer.Tick += new EventHandler((object sender, EventArgs e) => {  
                if (spotifyUsing) return;
                if (spotifyProcess == null || spotifyProcess.HasExited) {
                    SetSpotifyImage(true);
                    SetPlayImage("Spotify");
                    return;
                }; 
                bool spotifyMinimized = IsIconic(spotifyHWND);
                bool spotifyHidden = !IsWindowVisible(spotifyHWND); 
                if (spotifyHidden)
                    ForceGetSpotify();
                if (spotifyHidden || spotifyMinimized)
                    SetInvincible();
                if (!wasHidden)
                    if (GetForegroundWindow() == spotifyHWND)
                        SetSpotifyWindowScaling();
                string spotifyTitle = ActualSpotifyProcess().MainWindowTitle;
                SetTrackName(spotifyTitle);
                SetSpotifyImage(wasHidden);
                SetPlayImage(spotifyTitle);
                if (!spotifyMinimized)
                    SetDuration(); 
            });
            sTimer.Start();
        }  
        #endregion


        public async void _ToggleInfo_() {
            var primaryMonitorArea = SystemParameters.WorkArea;
            DispatcherTimer animTimer = new DispatcherTimer();
            double step = 20;
            if (HugeInfo.Height == 0) { 
                animTimer.Tick += new EventHandler(animTimer_Tick);
                animTimer.Interval = TimeSpan.FromMilliseconds(1);
                animTimer.Start();
                InfoZanaveska.Height = 240;
                HugeInfo.Opacity = 0;
                await Task.Delay(1);
                void animTimer_Tick(object sender, EventArgs e) {
                    HugeInfo.Height += step;
                    HugeInfo.Opacity += step / 240;
                    if (HugeInfo.Height == 240)
                        animTimer.Stop();
                } 
                await Task.Run(getWeather);
            }
            else if (HugeInfo.Height == 240) {
                animTimer.Tick += new EventHandler(animTimer_Tick);
                animTimer.Interval = TimeSpan.FromMilliseconds(1);
                animTimer.Start();
                if (dateInfo != null)
                    dateInfo.Close();
                calendarWrap.Opacity = 1;
                void animTimer_Tick(object sender, EventArgs e) {
                    HugeInfo.Height -= step;
                    HugeInfo.Opacity -= step / 240.0;
                    if (HugeInfo.Height == 0) {
                        animTimer.Stop();
                        InfoZanaveska.Height = 0;
                        Hide();
                    }
                }
            }
            else return;
        }

         
        void getWeather() { 
            Uri uri = new Uri("https://yandex.ru/pogoda/moscow?lat=55.801025&lon=37.805267");
            string html, currTemp, currSky, currWind, currHumidity;
            try {
                html = new System.Net.WebClient().DownloadString(uri); 
                currTemp = html
                    .Split("<div class=\"temp fact__temp fact__temp_size_s\"")[1]
                    .Split("<span class=\"temp__value temp__value_with-unit\">")[1]
                    .Split("</span>")[0]
                    + "°";
                currSky = html
                    .Split("<div class=\"temp fact__temp fact__temp_size_s\"")[1]
                    .Split("<img")[1]
                    .Split("src=\"")[1].Split("\">")[0]
                    .Split("/icons/")[1].Split("/")[2].Split(".svg")[0]; 
                if (html.Split("Штиль").Length > 1)
                    currWind = " " + "0м/с";
                else { 
                    currWind = " " + html
                        .Split("<span class=\"wind-speed\">")[1]
                        .Split("</span>")[0]
                        .Replace(",", ".") + " м/с";
                }
                currHumidity = " " + html
                    .Split("<div class=\"term term_orient_v fact__humidity\"")[1]
                    .Split("</i>")[1]
                    .Split("</div>")[0];
            }
            catch  {
                currTemp = ""; currSky = " "; currWind = ""; currHumidity = "";
            }


            Dispatcher.Invoke((Action)(() => {
                temperatureVal.Text = currTemp;
                windSpeed.Text = currWind;
                humidityValue.Text = currHumidity;

                string imgPath = "/views/windows/images/yandexsvgs/"+ "not-found" + ".png";
                if (sourceExists("views/windows/images/yandexsvgs/" + currSky + ".png"))
                    imgPath = "/views/windows/images/yandexsvgs/" + currSky + ".png"; 
                skyImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri(imgPath, UriKind.Relative)
                ); 
            }));
        }
        bool sourceExists(string resource) {
            string[] GetResourceNames() {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string resName = assembly.GetName().Name + ".g.resources";
                using (var stream = assembly.GetManifestResourceStream(resName)) {
                    using (var reader = new System.Resources.ResourceReader(stream)) {
                        return reader.Cast<System.Collections.DictionaryEntry>().Select(entry =>
                                 (string)entry.Key).ToArray();
                    }
                }
            }
            foreach (var name in GetResourceNames())
                if (name == resource)
                    return true;
            return false; 
        }
        void _OpenWeather_(object sender, RoutedEventArgs e) {
            Process.Start("explorer", "https://yandex.ru/pogoda/"); 
        }

        void createDailyJSON() {
            string currPath = Environment.CurrentDirectory;
            string directoryName = DateTime.Now.Month + "." + (DateTime.Now.Year - 2000); // 02.22

            if (!Directory.Exists(currPath + "\\" + directoryName)) {
                Directory.CreateDirectory(currPath + "\\" + directoryName);
                string chillDay = "Morning abs.&%False\nMorning 30 push-ups.&%False\nCardio.&%False\nEvening abs.&%False\nEvening 30 push-ups.&%False"; 
                string mondayP = "Forearm.&%False\nBack.&%False";
                string tuesdayP = chillDay;
                string wednesdayP = "Triceps.&%False\nChest.&%False";
                string thursdayP = chillDay;
                string fridayP = "Legs.&%False\nShoulders.&%False";
                string saturdayP = "Biceps.&%False\nLowerBack.&%False";
                string sundayP = chillDay;

                string[] workout = { mondayP, tuesdayP, wednesdayP, thursdayP, fridayP, saturdayP, sundayP };
                int startPos = dayOfWeek(1, DateTime.Now.Month, DateTime.Now.Year);
                for (int i = 0; i < amountOfDays(DateTime.Now.Month, DateTime.Now.Year); i++) {
                    //File.Create(currPath + "\\" + directoryName + "\\" + (i+1) + ".txt"); 
                    File.WriteAllText(currPath + "\\" + directoryName + "\\" + (i + 1) + ".txt", workout[startPos % 7]);
                    startPos++;
                }
            }
        }
        public int dayOfWeek(int date, int month, int year) {
            int monthCode = 0;
            if (month == 1 || month == 10)
                monthCode = 1;
            if (month == 2 || month == 3 || month == 11)
                monthCode = 4;
            if (month == 12 || month == 9)
                monthCode = 6;
            if (month == 4 || month == 7)
                monthCode = 0;
            if (month == 5)
                monthCode = 2;
            if (month == 8)
                monthCode = 3;
            if (month == 6)
                monthCode = 5;
            int shortYear = year - 2000;
            int yearCode = (6 + shortYear + shortYear / 4) % 7;

            int weeksDay = (5 + date + monthCode + yearCode) % 7;
            return weeksDay;
        }
        int amountOfDays(int month, int year) {
            int amountOfDays = 0;
            if (month % 2 == 1)
                amountOfDays = 31;
            else
                amountOfDays = 30;
            if (month == 2) {
                if (year % 4 == 0)
                    amountOfDays = 29;
                else
                    amountOfDays = 28;
            }
            return amountOfDays;
        }


        public void createMonthTable() {
            createDailyJSON();

            int weeksDay = dayOfWeek(1, DateTime.Now.Month, DateTime.Now.Year);
            int neededRows = 1 + (int)Math.Ceiling((31.0 - (8.0 - (double)weeksDay)) / 7.0);

            double tableWidth = 598.0 * 3.5 / 5.5 / 7.0 + 1;
            double tableHieght = ((240.0 * 10.0 / 11.7) - 1) / (double)neededRows;

            int dateCounter = 1;
            int dateLimit = amountOfDays(DateTime.Now.Month, DateTime.Now.Year);


            Grid test = new Grid();
            for (int i = 0; i < 7; i++) test.ColumnDefinitions.Add(new ColumnDefinition());
            for (int i = 0; i < neededRows; i++) test.RowDefinitions.Add(new RowDefinition());
            calendarTable.Orientation = Orientation.Vertical;
            for (int i = 0; i < neededRows; i++) {
                for (int k = 0; k < 7; k++) {
                    if (i == 0 && k < weeksDay) continue;

                    Label calendarDate = new Label();
                    calendarDate.FontSize = 22;
                    calendarDate.Content = Convert.ToString(dateCounter);
                    calendarDate.HorizontalAlignment = HorizontalAlignment.Center;
                    calendarDate.Foreground = (Brush)bc.ConvertFrom("#a000");
                    calendarDate.FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Views/Windows/fonts/#Merienda");

                    Frame calendarCase = new Frame();
                    calendarCase.Height = tableHieght;
                    calendarCase.Width = tableWidth;
                    calendarCase.BorderBrush = (Brush)bc.ConvertFrom("#7000");
                    calendarCase.Content = calendarDate;
                    calendarCase.MouseUp += calendarClick;

                    calendarCase.Background = (Brush)bc.ConvertFrom("#55ffffff");
                    if (DateTime.Now.Day == dateCounter) {
                        calendarCase.Background = (Brush)bc.ConvertFrom("#fff");
                        calendarDate.Foreground = (Brush)bc.ConvertFrom("#000");
                    }
                    if (DateTime.Now.Day < dateCounter) {
                        calendarCase.Background = (Brush)bc.ConvertFrom("#1fff");
                        calendarCase.MouseEnter += calendarEnter;
                        calendarCase.MouseLeave += calendarLeave;
                        calendarDate.Foreground = (Brush)bc.ConvertFrom("#afff");
                    }
                    calendarCase.BorderThickness = new Thickness(1, 0, 0, 1);
                    if (k % 7 == 0) { 
                        calendarCase.BorderThickness = new Thickness(0, 0, 0, 1); 
                    }
                    if (i == 1 && k < weeksDay) {
                        calendarCase.BorderThickness = new Thickness(1, 1, 0, 1);
                        calendarCase.Margin = new Thickness(0, -1, 0, 0); 
                    }
                    if (k % 7 == 0 && i == 1 && k < weeksDay)
                        calendarCase.BorderThickness = new Thickness(0, 1, 0, 1);
                    if (dateCounter == dateLimit) {
                        calendarCase.Width += 1;
                        calendarCase.BorderThickness = new Thickness(0, 0, 1, 0);
                    } 
                    Grid.SetRow(calendarCase, i);
                    Grid.SetColumn(calendarCase, k);
                    test.Children.Add(calendarCase);
                    dateCounter++;
                    if (dateCounter > dateLimit)
                        break;
                }
            }
            calendarTable.Children.Add(test);
        } 
        void calendarEnter(object sender, System.Windows.Input.MouseEventArgs e) { 
            Frame thisFrame = (Frame)sender;
            thisFrame.Background = (Brush)bc.ConvertFrom("#44ffffff");
            thisFrame.Opacity = 1; 
        }
        void calendarLeave(object sender, System.Windows.Input.MouseEventArgs e) {
            Frame thisFrame = (Frame)sender;
            thisFrame.Background = (Brush)bc.ConvertFrom("#11ffffff"); 
        }
        void calendarClick(object sender, System.Windows.Input.MouseEventArgs e) {
            dateInfo = new dateInfo();
            Frame thisFrame = (Frame)sender;
            Label senderLabel = (Label)thisFrame.Content;
            clickedDate = (string)senderLabel.Content; 
            openDate(); 
            calendarWrap.Opacity = 0;
            dateWrap.Visibility = Visibility.Visible; 
        }



        #region // dateInfo
        public string clickedDate = "";
        void openDate() {
            currDate.Text = clickedDate;
            setPurposes();
            setDayOfWeek();
            getAnek();
        }


        async void getAnek() {
            anekValue.Text = "Loading...";
            await System.Threading.Tasks.Task.Run(() => {
                int pageRand = new Random().Next(172) + 1;
                int anekRand = new Random().Next(25);
                string anek;
                try
                {
                    Uri uri = new Uri("https://humornet.ru/anekdot/korotkie/page/" + pageRand + "/");

                    string html = new System.Net.WebClient().DownloadString(uri);
                    anek = html
                        .Split("<section id=\"content\">")[1]
                        .Split("</section>")[0]
                        .Split("</article>")[anekRand]
                        .Split("<div class=\"text\">")[1]
                        .Split("</div>")[0]
                        .Replace("<p>", "").Replace("</p>", ""); // happens sometimes
                }
                catch
                {
                    anek = "No net connection";
                }
                Dispatcher.Invoke((Action)(() => {
                    anekValue.Text = anek;
                }));
            });
        }



        #region // Purposes   
        private string directoryName = DateTime.Now.Month + "." + (DateTime.Now.Year - 2000); // 2.22
        public void setPurposes() { 
            purposeList.Children.Clear();
            string fileText = File.ReadAllText(currPath + "\\" + directoryName + "\\" + clickedDate + ".txt");
            string[] textLines = fileText.Split("\n");

            foreach (string line in textLines)
                purposeList.Children.Add(newPurpose(line.Split("&%")[0], line.Split("&%")[1] != "False"));
        }

        TextBlock newPurpose(string text, bool done) {
            TextBlock purposeBlock = new TextBlock();

            int emptyCount = 0;
            foreach (TextBlock children in purposeList.Children)
                if (children.Text == " ")
                    emptyCount++;

            purposeBlock.Text = (text == "--empty") ?
                " " :
                "○ " + (purposeList.Children.Count + 1 - emptyCount) + ". " + text;
            purposeBlock.FontSize = 16;
            purposeBlock.TextWrapping = TextWrapping.Wrap;
            purposeBlock.Foreground = (Brush)bc.ConvertFrom("#fff");
            purposeBlock.Margin = new Thickness(2, 0, 0, 0);
            purposeBlock.TextDecorations = null;
            purposeBlock.Text = purposeBlock.Text.Replace("◉", "○");
            if (done)
            {
                purposeBlock.TextDecorations = TextDecorations.Strikethrough;
                purposeBlock.Text = purposeBlock.Text.Replace("○", "◉");
            }

            purposeBlock.MouseLeftButtonUp += _TogglePurpose_;
            purposeBlock.MouseRightButtonUp += _RemovePurpose_;
            return purposeBlock;
        }
        void _addPurpose_(object sender, System.Windows.Input.KeyEventArgs e)
        {
            TextBox thisInput = (TextBox)sender;
            string purposeText = thisInput.Text;
            if (e.Key == System.Windows.Input.Key.Return && purposeText.Length != 0) {
                purposeList.Children.Add(newPurpose(purposeText, false));
                purposeScroll.ScrollToEnd();
                thisInput.Text = "";
                // adding in file  
                string fileText = File.ReadAllText(currPath + "\\" + directoryName + "\\" + clickedDate + ".txt")
                    + "\n" + purposeText + "&%False";

                File.WriteAllText(currPath + "\\" + directoryName + "\\" + clickedDate + ".txt", fileText);
            }
        }

        void _TogglePurpose_(object sender, RoutedEventArgs e)
        {
            TextBlock thisBlock = (TextBlock)sender;
            if (thisBlock.Text == " ") return;
            StackPanel thisParent = (StackPanel)thisBlock.Parent;
            int thisIndex = thisParent.Children.IndexOf(thisBlock);

            string fileText = File.ReadAllText(currPath + "\\" + directoryName + "\\" + clickedDate + ".txt");
            string[] textLines = fileText.Split("\n");
            if (thisBlock.TextDecorations != TextDecorations.Strikethrough)
            {
                thisBlock.TextDecorations = TextDecorations.Strikethrough;
                thisBlock.Text = thisBlock.Text.Replace("○", "◉");
                textLines[thisIndex] = textLines[thisIndex].Replace("False", "True");
            }
            else
            {
                thisBlock.TextDecorations = null;
                thisBlock.Text = thisBlock.Text.Replace("◉", "○");
                textLines[thisIndex] = textLines[thisIndex].Replace("True", "False");
            }
            string toFile = "";
            foreach (string line in textLines)
                toFile += line + "\n";
            toFile = toFile.Substring(0, toFile.Length - 1);
            File.WriteAllText(currPath + "\\" + directoryName + "\\" + clickedDate + ".txt", toFile);
        }
        void _RemovePurpose_(object sender, RoutedEventArgs e) {
            TextBlock thisBlock = (TextBlock)sender;
            StackPanel thisParent = (StackPanel)thisBlock.Parent;
            // remove from file 
            string fileText = File.ReadAllText(currPath + "\\" + directoryName + "\\" + clickedDate + ".txt");
            string[] textLines = fileText.Split("\n");
            string toFile = "";
            for (int i = 0; i < textLines.Length; i++)
            {
                if (i != thisParent.Children.IndexOf(thisBlock))
                    toFile += textLines[i] + "\n";
            }
            toFile = toFile.Substring(0, toFile.Length - 1);
            File.WriteAllText(currPath + "\\" + directoryName + "\\" + clickedDate + ".txt", toFile);
            // remove from page  
            thisParent.Children.Clear();
            setPurposes();

        }
        #endregion

        void setDayOfWeek() {
            int currDay = MainWindow.widgetInfo.dayOfWeek(Convert.ToInt32(clickedDate), DateTime.Now.Month, DateTime.Now.Year);
            string[] daysOfWeek = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            dayBlock.Text = daysOfWeek[currDay];
        }
        private Button thisBtn;
        void _onGotFocus_(object sender, RoutedEventArgs e)
        {
            purposesInput.Text = "";
            purposesInput.Foreground = (Brush)bc.ConvertFrom("#fff");
            purposesInput.Focus();
            thisBtn = (Button)sender;
            thisBtn.Visibility = Visibility.Collapsed;
        }
        void _onLostFocus_(object sender, RoutedEventArgs e)
        {
            if (purposesInput.Text == "")
            {
                thisBtn.Visibility = Visibility.Visible;
                purposesInput.Text = "Enter new purpose...";
                purposesInput.Foreground = (Brush)bc.ConvertFrom("#6fff");
            }
        }
        void _stepBack_(object sender, RoutedEventArgs e) {
            dateWrap.Visibility = Visibility.Collapsed;  
            calendarWrap.Opacity = 1;
        }

        #endregion


    }
}
