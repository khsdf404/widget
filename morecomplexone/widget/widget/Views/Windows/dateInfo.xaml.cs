using System;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;
using System.IO;
using System.Windows.Controls;
using System.Diagnostics;

namespace widget.Views.Windows
{
    public partial class dateInfo : Window {
        public dateInfo() {
            InitializeComponent();
            // jumps hardFix
            Opacity = 0;

            var primaryMonitorArea = SystemParameters.WorkArea;
            Left = (primaryMonitorArea.Right / 2) - (600 / 2);
            Top = primaryMonitorArea.Bottom - 75 - 330;
        }

        public string clickedDate = "";
        private string currPath = Environment.CurrentDirectory;
        private string directoryName = DateTime.Now.Month + "." + (DateTime.Now.Year - 2000); // 2.22
        public IntPtr dateInfoHWND = IntPtr.Zero;
        BrushConverter bc = new BrushConverter(); 
        #region // OnLoad
        [DllImport("user32.dll", SetLastError = true)] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        private const int GWL_EX_STYLE = -20;
        private const int WS_EX_APPWINDOW = 0x00040000, WS_EX_TOOLWINDOW = 0x00000080;
        void _OnLoad_(object sender, RoutedEventArgs args) {
            dateInfoHWND = new WindowInteropHelper(this).Handle;
            SetWindowLong(dateInfoHWND, GWL_EX_STYLE, (GetWindowLong(dateInfoHWND, GWL_EX_STYLE) | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);

            DesktopAPI.WindowOnDesktopShow(dateInfoHWND);

            currDate.Text = clickedDate;
            setPurposes();
            setDayOfWeek();
            getAnek();
            // jumps hardFix
            Opacity = 1;
        }
        #endregion


        async void getAnek() {
            anekValue.Text = "Loading...";
            await System.Threading.Tasks.Task.Run(() => {
                int pageRand = new Random().Next(172) + 1;
                int anekRand = new Random().Next(25);
                string anek;
                try {
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
                catch {
                    anek = "No net connection";
                }
                Dispatcher.Invoke((Action)(() => {
                    anekValue.Text = anek;
                })); 
            }); 
        }



        #region // Purposes 
        public void setPurposes() { 
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
            if (done) {
                purposeBlock.TextDecorations = TextDecorations.Strikethrough;
                purposeBlock.Text = purposeBlock.Text.Replace("○", "◉");
            }

            purposeBlock.MouseLeftButtonUp += _TogglePurpose_;
            purposeBlock.MouseRightButtonUp += _RemovePurpose_;
            return purposeBlock;
        } 
        void _addPurpose_(object sender, System.Windows.Input.KeyEventArgs e) {
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
          
        void _TogglePurpose_(object sender, RoutedEventArgs e) {
            TextBlock thisBlock = (TextBlock)sender; 
            if (thisBlock.Text == " ") return;
            StackPanel thisParent = (StackPanel)thisBlock.Parent;
            int thisIndex = thisParent.Children.IndexOf(thisBlock); 

            string fileText = File.ReadAllText(currPath + "\\" + directoryName + "\\" + clickedDate + ".txt");
            string[] textLines = fileText.Split("\n");
            if (thisBlock.TextDecorations != TextDecorations.Strikethrough) {
                thisBlock.TextDecorations = TextDecorations.Strikethrough;
                thisBlock.Text = thisBlock.Text.Replace("○", "◉");
                textLines[thisIndex] = textLines[thisIndex].Replace("False", "True");
            }
            else {
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
            for (int i = 0; i < textLines.Length; i++) {
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
            dayOfWeek.Text = daysOfWeek[currDay];
        }
        private Button thisBtn;
        void _onGotFocus_(object sender, RoutedEventArgs e) {
            purposesInput.Text = "";
            purposesInput.Foreground = (Brush)bc.ConvertFrom("#fff");
            purposesInput.Focus();
            thisBtn = (Button)sender;
            thisBtn.Visibility = Visibility.Collapsed;
        }
        void _onLostFocus_(object sender, RoutedEventArgs e) {
            if (purposesInput.Text == "") {
                thisBtn.Visibility = Visibility.Visible;
                purposesInput.Text = "Enter new purpose...";
                purposesInput.Foreground = (Brush)bc.ConvertFrom("#6fff");
            }
        }
        void _stepBack_(object sender, RoutedEventArgs e) {
            MainWindow.widgetInfo.dateInfo.Close();
            MainWindow.widgetInfo.calendarWrap.Opacity = 1;
        }

    }
}
