using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using WpfAppBar;
using WindowsInput.Native;

namespace SlimBar
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Top = -100;
            this.Left = -100;
        }

        #region Make application disappear in tasks
        [Flags]
        public enum ExtendedWindowStyles
        {
            WS_EX_TOOLWINDOW = 0x00000080,
        }

        public enum GetWindowLongFields
        {
            GWL_EXSTYLE = (-20),
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);
        #endregion

        private void SetAppbar(bool enable = true)
        {
            if (enable)
                AppBarFunctions.SetAppBar(this, ABEdge.Top, TopPanel, false);
            else
                AppBarFunctions.SetAppBar(this, ABEdge.None);
        }

        private void RefreshTime(object? sender, EventArgs e)
        {
            // TODO: Change time format according to region
            var now = DateTime.Now;
            var culture = CultureInfo.CurrentCulture.DateTimeFormat;
            string week = culture.GetAbbreviatedDayName(now.DayOfWeek);
            string date = now.ToString("d MMM");
            string time = now.ToString("HH:mm:ss");
            TimeBox.Content = $"{week} {date} {time}";
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Set window as app bar
            SetAppbar(true);

            #region Remove app in tasks
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);

            int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);

            exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
            #endregion

            // Setup timer to refresh time
            // TODO: Maybe there's a better way?
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += RefreshTime;
            timer.Interval = new TimeSpan(0, 0, 0, 0, 20);
            timer.Start();
        }

        private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Keep the top bar filled across the whole screen
            Width = SystemParameters.PrimaryScreenWidth;
        }

        /// <summary>
        /// ***Please use this button to exit program when debugging***
        /// If you close the program on VS, Appbar will not be deregistered 
        /// and hence will leave a gap on the screen.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            // Does not work with VS force shutting down app
            SetAppbar(false);
        }

        private void ActivityButton_OnClick(object sender, RoutedEventArgs e)
        {
            // TODO: This is currently buggy. Waiting for a better solution (probably an Windows API call?)
            WindowsInput.InputSimulator simulator = new WindowsInput.InputSimulator();
            simulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LWIN, VirtualKeyCode.TAB);
        }
    }
}
