using System; 
using System.Windows; 
using System.Runtime.InteropServices;
using System.Windows.Interop;  
using System.Windows.Media;
using System.Windows.Threading; 
using System.Collections.Generic;
using System.Linq; 
using System.Diagnostics;
using SimpleWifi; 
using System.IO; 
using Windows.Devices.Radios;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows.Controls;
using widget.Views.Windows; 

namespace widget {
    public partial class MainWindow : Window { 
        public MainWindow() {
            InitializeComponent();
            var primaryMonitorArea = SystemParameters.WorkArea;
            Left = (primaryMonitorArea.Right / 2) - (Width / 2);
            Top = primaryMonitorArea.Bottom - 75 - 90;
            _Clock_();
            _WifiStatusUpdate_();
            _BtStatusUpdate_();
             

            string ExePath = Directory.GetCurrentDirectory() + @"\widget.exe -silent";
            RegistryKey reg; 
            reg = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run\\");
            reg.SetValue("widget", ExePath); 
        } 

        int speed = 1; double step = 0.05;
        double max_opacity = 1; double min_opacity = 0.25;
        string enabled_color = "#fff"; string disabled_color = "#454545";
        BrushConverter bc = new BrushConverter();
        public static widgetInfo widgetInfo = new widgetInfo();

        #region // _OnLoad_
        // hide from alt+tab
        // show window only on desktop 
        [DllImport("user32.dll", SetLastError = true)] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        private const int GWL_EX_STYLE = -20;
        private const int WS_EX_APPWINDOW = 0x00040000, WS_EX_TOOLWINDOW = 0x00000080;
        void _OnLoad_(object sender, RoutedEventArgs args) {
            var helper = new WindowInteropHelper(this).Handle;
            SetWindowLong(helper, GWL_EX_STYLE, (GetWindowLong(helper, GWL_EX_STYLE) | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);

            DesktopAPI.WindowOnDesktopShow(helper); 
        }
        #endregion
         

        #region   // wifi
        void _WifiStatusUpdate_()
        {
            Wifi wifi = new Wifi();

            DispatcherTimer wifiTimer = new DispatcherTimer();
            wifiTimer.Tick += new EventHandler(wifiTimer_Tick);
            wifiTimer.Interval = TimeSpan.FromMilliseconds(1000);
            wifiTimer.Start();
            void wifiTimer_Tick(object sender, EventArgs e)
            {
                string ConnectionStatus = "Disconnected";
                try
                {
                    List<AccessPoint> accessPointsList = wifi.GetAccessPoints().OrderByDescending(x => x.SignalStrength).ToList();
                    foreach (AccessPoint accessPoint in accessPointsList)
                    {
                        if (accessPoint.IsConnected)
                            ConnectionStatus = "Connected";
                    }
                    _WifiAnimate_(ConnectionStatus);
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    ConnectionStatus = "Unavialable";
                    _WifiAnimate_(ConnectionStatus);
                }
            }
        }
        void _WifiAnimate_(string ConnectionStatus)
        {
            DispatcherTimer animTimer = new DispatcherTimer();
            if (wifi_icon.Opacity <= max_opacity && ConnectionStatus == "Connected")
            {
                animTimer.Tick += new EventHandler(animTimer_Tick);
                animTimer.Interval = TimeSpan.FromMilliseconds(speed);
                animTimer.Start();
                void animTimer_Tick(object sender, EventArgs e)
                {
                    wifi_icon.Opacity += step;
                    WifiLine.Background = (Brush)bc.ConvertFrom(enabled_color);
                    if (wifi_icon.Opacity >= max_opacity) { animTimer.Stop(); }
                }
            }
            if (wifi_icon.Opacity > min_opacity && (ConnectionStatus == "Disconnected" || ConnectionStatus == "Unavialable"))
            {
                animTimer.Tick += new EventHandler(animTimer_Tick);
                animTimer.Interval = TimeSpan.FromMilliseconds(speed);
                animTimer.Start();
                void animTimer_Tick(object sender, EventArgs e)
                {
                    wifi_icon.Opacity -= step;
                    WifiLine.Background = (Brush)bc.ConvertFrom(disabled_color);
                    if (wifi_icon.Opacity <= min_opacity) { animTimer.Stop(); }
                }
            }
        }
        void _ToggleWifi_(object sender, RoutedEventArgs e)
        {
            Wifi wifi = new Wifi();
            List<AccessPoint> accessPointsList = wifi.GetAccessPoints().
                OrderByDescending(x => x.SignalStrength).ToList();
            AccessPoint accessPoint = accessPointsList[0];
            try
            {
                if (accessPoint != null && !accessPoint.IsConnected)
                    accessPoint.Connect(new AuthRequest(accessPoint));
                else if (wifi.ConnectionStatus == WifiStatus.Connected)
                    wifi.Disconnect();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return;
            }
        }
        void _WifiSettings_(object sender, RoutedEventArgs e)
        {
            Process p = new Process();
            ProcessStartInfo procStartInfo = new ProcessStartInfo("ms-settings:network-wifisettings");
            procStartInfo.UseShellExecute = true;
            p.StartInfo = procStartInfo;
            p.Start();
        }
        #endregion

        #region // bluetooth    

        void _BtStatusUpdate_() {
            DispatcherTimer BtTimer = new DispatcherTimer();
            BtTimer.Tick += new EventHandler(_BtTimerTick_);
            BtTimer.Interval = TimeSpan.FromMilliseconds(1000);
            BtTimer.Start();

            void _BtTimerTick_(object sender, EventArgs e)
            {
                bool ConnectionStatus = false;
                async Task<bool> GetBluetoothIsEnabledAsync()
                {
                    var radios = await Radio.GetRadiosAsync();
                    var bluetoothRadio = radios.FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth);
                    ConnectionStatus = bluetoothRadio.State == RadioState.On;
                    _BtAnimate_(ConnectionStatus); 
                    return bluetoothRadio.State == RadioState.On;
                }
                GetBluetoothIsEnabledAsync();
            }
        }
        void _BtAnimate_(bool ConnectionStatus)
        {
            DispatcherTimer animTimer = new DispatcherTimer();
            animTimer.Interval = TimeSpan.FromMilliseconds(speed);
            if (bt_icon.Opacity <= max_opacity && ConnectionStatus)
            {
                animTimer.Tick += new EventHandler(animTimer_Tick);
                animTimer.Start();
                void animTimer_Tick(object sender, EventArgs e)
                {
                    bt_icon.Opacity += step;
                    BtLine.Background = (Brush)bc.ConvertFrom(enabled_color);
                    if (bt_icon.Opacity >= max_opacity) { animTimer.Stop(); }
                }
            }
            if (bt_icon.Opacity > min_opacity && !ConnectionStatus)
            {
                animTimer.Tick += new EventHandler(animTimer_Tick);
                animTimer.Start();
                void animTimer_Tick(object sender, EventArgs e)
                {
                    bt_icon.Opacity -= step;
                    BtLine.Background = (Brush)bc.ConvertFrom(disabled_color);
                    if (bt_icon.Opacity <= min_opacity) { animTimer.Stop(); }
                }
            }
        }
        void _ToggleBt_(object sender, RoutedEventArgs e)
        {
            async Task<bool> GetBluetoothIsEnabledAsync()
            {
                var radios = await Radio.GetRadiosAsync();
                var bluetoothRadio = radios.FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth);
                if (bluetoothRadio.State == RadioState.On)
                    await bluetoothRadio.SetStateAsync(RadioState.Off);
                else
                    await bluetoothRadio.SetStateAsync(RadioState.On);
                return true;
            }
            GetBluetoothIsEnabledAsync();
        }
        void _BtSettings_(object sender, RoutedEventArgs e)
        {
            Process p = new Process();
            ProcessStartInfo procStartInfo = new ProcessStartInfo("ms-settings:bluetooth");
            procStartInfo.UseShellExecute = true;
            p.StartInfo = procStartInfo;
            p.Start();
        }
        #endregion

        private int currMonth = DateTime.Now.Month;
        void _Clock_() {
            DispatcherTimer ClockTimer = new DispatcherTimer();
            ClockTimer.Tick += new EventHandler(_ClockTimerTick_);
            ClockTimer.Interval = TimeSpan.FromMilliseconds(1000);
            ClockTimer.Start();
            void _ClockTimerTick_(object sender, EventArgs e) { 
                if (clock.Text == "23:59" && DateTime.Now.ToString("HH:mm") == "00:00") {
                    if (widgetInfo != null) {
                        widgetInfo.Close();
                        widgetInfo = new widgetInfo();
                        widgetInfo.Show();
                        widgetInfo._ToggleInfo_();
                    }
                }
                string[] months = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
                clock.Text = DateTime.Now.ToString("HH:mm");
                date.Text = months[DateTime.Now.Month - 1] + ", " + DateTime.Now.Day; 

                widgetInfo.calendarMonth.Text = months[DateTime.Now.Month - 1];  
                if (currMonth != DateTime.Now.Month) {
                    currMonth = DateTime.Now.Month;
                    widgetInfo.createMonthTable();
                }
            }  
        } 
        void _ToggleInfo_(object sender, RoutedEventArgs e) {
            widgetInfo.Show();
            widgetInfo._ToggleInfo_();
        }
         
         
        #region // Music 
        private MediaPlayer soundtrack = new MediaPlayer();
        private DispatcherTimer lt;
        private DispatcherTimer rt;
        static StreamReader sr = new StreamReader("path.txt");
        static string music_fold = sr.ReadLine(); 
        double music_vol = 0.02; 

        void _LMBClickRecognizer_(object sender, RoutedEventArgs e)  {
            bool IsDouble() {
                if (lt == null)
                    return false;
                lt.Tick -= _SingleClick_;
                lt = null;
                return true;
            }
            void _SingleClick_(object sender, EventArgs e) {
                IsDouble();
                TimeSpan curr_pos = soundtrack.Position;
                DispatcherTimer PlayChecker = new DispatcherTimer();
                PlayChecker.Tick += new EventHandler(_PlayCheckerTick_);
                PlayChecker.Interval = TimeSpan.FromMilliseconds(50);
                PlayChecker.Start();
                void _PlayCheckerTick_(object sender, EventArgs e) {
                    if (curr_pos == soundtrack.Position) {
                        soundtrack.Play();
                        music_icon.Opacity = 1;
                        MusicLine.Background = (Brush)bc.ConvertFrom(enabled_color);
                    }
                    else if (curr_pos != soundtrack.Position) {
                        soundtrack.Pause();
                        music_icon.Opacity = 0.3;
                        MusicLine.Background = (Brush)bc.ConvertFrom(disabled_color);

                    }
                    PlayChecker.Stop();
                }
            }
            void _ClickRecognizer_()
            {
                Uri Random_Song()
                {
                    List<string> MusicList = new List<string>();
                    DirectoryInfo dir = new DirectoryInfo(music_fold);
                    foreach (FileInfo files in dir.GetFiles("*.mp3"))
                        MusicList.Add(files.Name);
                    foreach (FileInfo files in dir.GetFiles("*.opus"))
                        MusicList.Add(files.Name);

                    Random rndConstr = new Random();
                    int rnd = rndConstr.Next(0, MusicList.Count);
                    Uri file = new Uri(music_fold + MusicList[rnd]);
                    
                    return file;
                }
                if (soundtrack.Source == null)
                { 
                    soundtrack.Open(Random_Song());  
                }
                soundtrack.MediaEnded += new EventHandler(_NextTrack_);
                void _NextTrack_(object sender, EventArgs e)
                {
                    soundtrack.Close();
                    soundtrack = new MediaPlayer();
                    soundtrack.Open(Random_Song());
                    soundtrack.Play();
                    soundtrack.MediaEnded += new EventHandler(_NextTrack_);
                    Trace.WriteLine("hahaha");
                } 
                if (IsDouble())
                {
                    soundtrack.Open(Random_Song());
                    soundtrack.Play();
                    music_icon.Opacity = 1;
                    MusicLine.Background = (Brush)bc.ConvertFrom(enabled_color);
                }
                else
                {
                    lt = new DispatcherTimer(
                                TimeSpan.FromMilliseconds(300),
                                DispatcherPriority.Normal,
                                _SingleClick_,
                                Dispatcher);
                }
            }
            // Trace.WriteLine(MusicList.Count);
            _ClickRecognizer_();
        } 
         
        void _RMBClickRecognizer_(object sender, RoutedEventArgs e) 
        {
            bool IsDouble()
            {
                if (rt == null)
                    return false;
                rt.Tick -= _SingleClick_;
                rt = null;
                return true;
            }
            void _SingleClick_(object sender, EventArgs e)
            {
                IsDouble();
                Process.Start("explorer.exe", music_fold);

            }
            void _ClickRecognizer_()
            {
                if (IsDouble())
                {
                    System.Windows.Forms.FolderBrowserDialog music_folder = new System.Windows.Forms.FolderBrowserDialog();
                    music_folder.ShowNewFolderButton = false;
                    if (music_folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        if (music_fold!= music_folder.SelectedPath + @"\") 
                        {
                            music_fold = music_folder.SelectedPath + @"\"; 
                            try
                            {
                                StreamWriter sw = new StreamWriter("path.txt");
                                sw.WriteLine(music_fold);
                                sw.Close();
                                Trace.WriteLine("yay: ");
                            }
                            catch (Exception e)
                            {
                                Trace.WriteLine("Exception: " + e.Message);
                            }
                            finally
                            {
                                Trace.WriteLine("Executing finally block.");
                            } 
                            List<string> MusicList = new List<string>();
                            DirectoryInfo dir = new DirectoryInfo(music_fold);
                            foreach (FileInfo files in dir.GetFiles("*.mp3"))
                                MusicList.Add(files.Name);
                            foreach (FileInfo files in dir.GetFiles("*.opus"))
                                MusicList.Add(files.Name);

                            Random rndConstr = new Random();
                            int rnd = rndConstr.Next(0, MusicList.Count);
                            Uri file = new Uri(music_fold + MusicList[rnd]);
                            soundtrack.Open(file);
                            soundtrack.Play();
                        } 
                    }
                    
                }
                else
                {
                    rt = new DispatcherTimer(
                                TimeSpan.FromMilliseconds(300),
                                DispatcherPriority.Normal,
                                _SingleClick_,
                                Dispatcher);
                }
            }

            _ClickRecognizer_();
        }

        void _MusicVolume_(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {

            if (music_vol >= 0 && music_vol <= 1)
                music_vol -= (e.Delta * 0.0005);
            if (music_vol < 0 && e.Delta < 0)
                music_vol = 0;
            if (music_vol > 1 && e.Delta > 0)
                music_vol = 1;
            soundtrack.Volume = music_vol;
        }

        #endregion

        void _RecBinFuncs_(object sender, RoutedEventArgs e) {
            System.Diagnostics.Process.Start("explorer.exe", "shell:RecycleBinFolder");

            RecBin.Opacity = 0.55;
            RecBinLine.Background = (Brush)bc.ConvertFrom(disabled_color);
            DispatcherTimer clickTimer = new DispatcherTimer();
            clickTimer.Tick += new EventHandler(_clickTimerTick_);
            clickTimer.Interval = TimeSpan.FromMilliseconds(55);
            clickTimer.Start();
            void _clickTimerTick_(object sender, EventArgs e) {
                RecBin.Opacity = 1;
                RecBinLine.Background = (Brush)bc.ConvertFrom(enabled_color);
                clickTimer.Stop();
            }
        } 

    }

}
