using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace LePork
{
    public partial class MainWindow : Window
    {
        private const string AccessCode = "3126";

        private readonly string LePorkFolder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments),
                "LePork");

        private readonly string FortniteBackupFolder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments),
                "LePork",
                "FortniteBackup");

        private readonly string StartupBackupFile;

        private DispatcherTimer statsTimer;

        private bool isFullscreen;

        private double savedLeft;
        private double savedTop;
        private double savedWidth;
        private double savedHeight;

        private const uint DigcfPresent = 0x00000002;
        private const uint DigcfDeviceInterface = 0x00000010;

        private const uint GenericRead = 0x80000000;
        private const uint GenericWrite = 0x40000000;

        private const uint FileShareRead = 0x00000001;
        private const uint FileShareWrite = 0x00000002;

        private const uint OpenExisting = 3;

        private const uint IoctlHidGetPollFrequency = 0x000B0198;
        private const uint IoctlHidSetPollFrequency = 0x000B019C;

        private static readonly Guid HidInterfaceGuid =
            new Guid(
                "4D1E55B2-F16F-11CF-88CB-001111000030");

        private readonly string[] StartupNames =
        {
            "Discord",
            "Spotify",
            "Steam",
            "EpicGamesLauncher",
            "Epic Games Launcher",
            "Riot Client",
            "RiotClient",
            "Battle.net",
            "BattleNet",
            "Telegram",
            "Microsoft Teams",
            "Teams",
            "Skype"
        };

        private sealed class HidPollingDevice
        {
            public string DevicePath;
            public string DisplayName;
            public int CurrentIntervalMs;
        }

        public MainWindow()
        {
            InitializeComponent();

            Directory.CreateDirectory(
                LePorkFolder);

            Directory.CreateDirectory(
                FortniteBackupFolder);

            StartupBackupFile =
                Path.Combine(
                    LePorkFolder,
                    "StartupBackup.txt");

            SetElementVisibility(
                "LoginView",
                Visibility.Visible);

            SetElementVisibility(
                "MainView",
                Visibility.Collapsed);

            StartStatsTimer();
        }

        // =========================================================
        // XAML HELPERS
        // =========================================================

        private object GetElement(string name)
        {
            return FindName(name);
        }

        private T GetElement<T>(string name)
            where T : class
        {
            return GetElement(name) as T;
        }

        private void SetElementVisibility(
            string name,
            Visibility visibility)
        {
            FrameworkElement element =
                GetElement<FrameworkElement>(name);

            if (element != null)
            {
                element.Visibility =
                    visibility;
            }
        }

        private void SetElementText(
            string name,
            string text)
        {
            System.Windows.Controls.TextBlock textBlock =
                GetElement<System.Windows.Controls.TextBlock>(
                    name);

            if (textBlock != null)
            {
                textBlock.Text = text;
            }
        }

        private void SetTopBarHeight(double height)
        {
            RowDefinition row =
                GetElement<RowDefinition>(
                    "TopBarRow");

            if (row != null)
            {
                row.Height =
                    new GridLength(height);
            }
        }

        // =========================================================
        // LOGIN
        // =========================================================

        private void Login_Click(
            object sender,
            RoutedEventArgs e)
        {
            System.Windows.Controls.PasswordBox box =
                GetElement<System.Windows.Controls.PasswordBox>(
                    "AccessCodeBox");

            if (box == null)
                return;

            if (box.Password == AccessCode)
            {
                SetElementText(
                    "LoginStatus",
                    "");

                SetElementVisibility(
                    "LoginView",
                    Visibility.Collapsed);

                SetElementVisibility(
                    "MainView",
                    Visibility.Visible);

                ShowPanel(
                    "HomePanel",
                    "Home",
                    "One-click optimization");

                SetStatus(
                    "LePork ready.");
            }
            else
            {
                SetElementText(
                    "LoginStatus",
                    "Incorrect access code.");

                box.Clear();
                box.Focus();
            }
        }

        // =========================================================
        // F11
        // =========================================================

        private void Window_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                ToggleFullscreen();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape &&
                isFullscreen)
            {
                ExitFullscreen();
                e.Handled = true;
            }
        }

        private void ToggleFullscreen()
        {
            if (isFullscreen)
                ExitFullscreen();
            else
                EnterFullscreen();
        }

        private void EnterFullscreen()
        {
            savedLeft = Left;
            savedTop = Top;
            savedWidth = Width;
            savedHeight = Height;

            isFullscreen = true;

            SetElementVisibility(
                "TopBar",
                Visibility.Collapsed);

            SetTopBarHeight(0);

            WindowState =
                WindowState.Normal;

            Left = 0;
            Top = 0;

            Width =
                SystemParameters.PrimaryScreenWidth;

            Height =
                SystemParameters.PrimaryScreenHeight;
        }

        private void ExitFullscreen()
        {
            isFullscreen = false;

            SetElementVisibility(
                "TopBar",
                Visibility.Visible);

            SetTopBarHeight(62);

            WindowState =
                WindowState.Normal;

            Left = savedLeft;
            Top = savedTop;
            Width = savedWidth;
            Height = savedHeight;
        }

        // =========================================================
        // TITLE BAR
        // =========================================================

        private void TopBar_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (isFullscreen)
                return;

            if (e.ClickCount == 2)
            {
                Maximize_Click(
                    sender,
                    e);

                return;
            }

            if (e.LeftButton ==
                MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch
                {
                }
            }
        }

        private void Minimize_Click(
            object sender,
            RoutedEventArgs e)
        {
            WindowState =
                WindowState.Minimized;
        }

        private void Maximize_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (isFullscreen)
                return;

            if (WindowState ==
                WindowState.Maximized)
            {
                WindowState =
                    WindowState.Normal;
            }
            else
            {
                WindowState =
                    WindowState.Maximized;
            }
        }

        private void Close_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        // =========================================================
        // STATS
        // =========================================================

        private void StartStatsTimer()
        {
            statsTimer =
                new DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromSeconds(1)
                };

            statsTimer.Tick += async (sender, e) =>
            {
                SetElementText(
                    "CpuText",
                    await GetCpuUsageAsync());

                SetElementText(
                    "RamText",
                    GetMemoryUsage());
            };

            statsTimer.Start();
        }

        private async Task<string>
            GetCpuUsageAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (
                        Process process =
                        Process.GetCurrentProcess())
                    {
                        TimeSpan startCpu =
                            process.TotalProcessorTime;

                        DateTime start =
                            DateTime.UtcNow;

                        Thread.Sleep(150);

                        process.Refresh();

                        TimeSpan endCpu =
                            process.TotalProcessorTime;

                        DateTime end =
                            DateTime.UtcNow;

                        double cpu =
                            (
                                endCpu -
                                startCpu
                            ).TotalMilliseconds;

                        double elapsed =
                            (
                                end -
                                start
                            ).TotalMilliseconds;

                        if (elapsed <= 0)
                            return "--%";

                        double usage =
                            cpu /
                            (
                                Environment.ProcessorCount *
                                elapsed
                            ) *
                            100.0;

                        usage =
                            Math.Max(
                                0,
                                Math.Min(
                                    100,
                                    usage));

                        return usage.ToString("0") +
                               "%";
                    }
                }
                catch
                {
                    return "--%";
                }
            });
        }

        private string GetMemoryUsage()
        {
            try
            {
                using (
                    Process process =
                    Process.GetCurrentProcess())
                {
                    long mb =
                        process.WorkingSet64 /
                        1024 /
                        1024;

                    return mb + " MB";
                }
            }
            catch
            {
                return "-- MB";
            }
        }

        // =========================================================
        // NAVIGATION
        // =========================================================

        private void Home_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowPanel(
                "HomePanel",
                "Home",
                "One-click optimization");
        }

        private void Performance_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowPanel(
                "PerformancePanel",
                "Performance",
                "System performance optimization");
        }

        private void KBM_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowPanel(
                "KBMPanel",
                "Keyboard and Mouse",
                "Input optimization");
        }

        private void Controller_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowPanel(
                "ControllerPanel",
                "Controller",
                "Controller optimization");
        }

        private void Polling_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowPanel(
                "PollingPanel",
                "Polling Rate",
                "Highest supported polling rate");
        }

        private void Network_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowPanel(
                "NetworkPanel",
                "Network",
                "Network optimization");

            UpdateNetworkStatus();
        }

        private void Windows_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowPanel(
                "WindowsPanel",
                "Windows",
                "Windows optimization and repair");
        }

        private void Fortnite_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowPanel(
                "FortnitePanel",
                "Fortnite",
                "Fortnite optimization");

            UpdateFortniteStatus();
        }

        private void Advanced_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowPanel(
                "AdvancedPanel",
                "Advanced",
                "Advanced optimization");
        }

        private void ShowPanel(
            string panelName,
            string title,
            string subtitle)
        {
            string[] panels =
            {
                "HomePanel",
                "PerformancePanel",
                "KBMPanel",
                "ControllerPanel",
                "PollingPanel",
                "NetworkPanel",
                "WindowsPanel",
                "FortnitePanel",
                "AdvancedPanel"
            };

            foreach (string panel in panels)
            {
                SetElementVisibility(
                    panel,
                    Visibility.Collapsed);
            }

            SetElementVisibility(
                panelName,
                Visibility.Visible);

            SetElementText(
                "PageTitle",
                title);

            SetElementText(
                "PageSubtitle",
                subtitle);
        }

        private void SetStatus(string text)
        {
            SetElementText(
                "StatusText",
                text);
        }

        // =========================================================
        // APPLY ALL
        // =========================================================

        private void ApplyAll_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                SetStatus(
                    "Creating backups...");

                BackupAll();

                ApplyPerformanceOptimization();
                ApplyWindowsOptimization();
                ApplyMouseOptimization();
                ApplyStartupOptimization();
                ApplyNetworkOptimization();
                ApplyPollingOptimization();
                ApplyFortniteOptimization();

                SetStatus(
                    "✓ All normal LePork optimizations applied.");

                MessageBox.Show(
                    "LePork finished applying its normal supported optimizations.\n\n" +
                    "System repair commands are separate so they do not run automatically.",
                    "LePork",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                SetStatus(
                    "Some optimizations could not be applied.");

                MessageBox.Show(
                    ex.Message,
                    "LePork",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        // =========================================================
        // PERFORMANCE
        // =========================================================

        private void PerformanceApply_Click(
            object sender,
            RoutedEventArgs e)
        {
            BackupPowerSettings();

            ApplyPerformanceOptimization();

            SetStatus(
                "✓ Performance optimization applied.");
        }

        private void ApplyPerformanceOptimization()
        {
            RunCommand(
                "powercfg.exe",
                "/setactive SCHEME_MIN");

            RunCommand(
                "powercfg.exe",
                "/change monitor-timeout-ac 0");

            RunCommand(
                "powercfg.exe",
                "/change standby-timeout-ac 0");

            SetRegistryValue(
                Registry.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\GameDVR",
                "AppCaptureEnabled",
                0,
                RegistryValueKind.DWord);

            SetRegistryValue(
                Registry.CurrentUser,
                @"System\GameConfigStore",
                "GameDVR_Enabled",
                0,
                RegistryValueKind.DWord);
        }

        private void GameModeApply_Click(
            object sender,
            RoutedEventArgs e)
        {
            ApplyGameMode();

            SetStatus(
                "✓ Game Mode enabled.");
        }

        private void ApplyGameMode()
        {
            SetRegistryValue(
                Registry.CurrentUser,
                @"Software\Microsoft\GameBar",
                "AllowAutoGameMode",
                1,
                RegistryValueKind.DWord);

            SetRegistryValue(
                Registry.CurrentUser,
                @"Software\Microsoft\GameBar",
                "AutoGameModeEnabled",
                1,
                RegistryValueKind.DWord);
        }

        private void VisualApply_Click(
            object sender,
            RoutedEventArgs e)
        {
            ApplyVisualOptimization();

            SetStatus(
                "✓ Visual optimization applied.");
        }

        private void ApplyVisualOptimization()
        {
            SetRegistryValue(
                Registry.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                "VisualFXSetting",
                2,
                RegistryValueKind.DWord);

            SetRegistryValue(
                Registry.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                "TaskbarAnimations",
                0,
                RegistryValueKind.DWord);

            SetRegistryValue(
                Registry.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "EnableTransparency",
                0,
                RegistryValueKind.DWord);
        }

        private void StartupApply_Click(
            object sender,
            RoutedEventArgs e)
        {
            string result =
                ApplyStartupOptimization();

            SetStatus(result);
        }

        // =========================================================
        // KBM
        // =========================================================

        private void InputApply_Click(
            object sender,
            RoutedEventArgs e)
        {
            BackupMouseSettings();

            ApplyMouseOptimization();

            string polling =
                ApplyPollingOptimization();

            SetElementText(
                "KbmStatus",
                "✓ KBM optimization applied.\n\n" +
                polling);

            SetStatus(
                "✓ KBM optimization applied.");
        }

        private void KBMApply_Click(
            object sender,
            RoutedEventArgs e)
        {
            BackupMouseSettings();

            ApplyMouseOptimization();

            string polling =
                ApplyPollingOptimization();

            SetElementText(
                "KbmStatus",
                "✓ KBM optimization applied.\n\n" +
                polling);

            SetStatus(
                "✓ KBM optimization applied.");
        }

        private void MouseAccel_Click(
            object sender,
            RoutedEventArgs e)
        {
            BackupMouseSettings();
            ApplyMouseOptimization();

            SetElementText(
                "KbmStatus",
                "✓ Windows mouse acceleration disabled.");

            SetStatus(
                "✓ Mouse acceleration disabled.");
        }

        private void ApplyMouseOptimization()
        {
            string path =
                @"Control Panel\Mouse";

            SetRegistryValue(
                Registry.CurrentUser,
                path,
                "MouseSpeed",
                "0",
                RegistryValueKind.String);

            SetRegistryValue(
                Registry.CurrentUser,
                path,
                "MouseThreshold1",
                "0",
                RegistryValueKind.String);

            SetRegistryValue(
                Registry.CurrentUser,
                path,
                "MouseThreshold2",
                "0",
                RegistryValueKind.String);
        }

        // =========================================================
        // CONTROLLER
        // =========================================================

        private void ControllerApply_Click(
            object sender,
            RoutedEventArgs e)
        {
            ApplyControllerOptimization();

            string polling =
                ApplyPollingOptimization();

            SetElementText(
                "ControllerStatus",
                "✓ Controller optimization applied.\n\n" +
                polling);

            SetStatus(
                "✓ Controller optimization applied.");
        }

        private void ApplyControllerOptimization()
        {
            SetRegistryValue(
                Registry.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\GameDVR",
                "AppCaptureEnabled",
                0,
                RegistryValueKind.DWord);
        }

        // =========================================================
        // POLLING
        // =========================================================

        private void PollingApply_Click(
            object sender,
            RoutedEventArgs e)
        {
            string result =
                ApplyPollingOptimization();

            SetElementText(
                "PollingResult",
                result);

            SetElementText(
                "KbmStatus",
                result);

            SetElementText(
                "ControllerStatus",
                result);

            SetStatus(
                "✓ Polling scan completed.");
        }

        private string ApplyPollingOptimization()
        {
            List<HidPollingDevice> devices =
                EnumerateHidPollingDevices();

            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                "MAX POLLING RATE");

            result.AppendLine(
                "----------------");

            result.AppendLine();

            if (devices.Count == 0)
            {
                result.AppendLine(
                    "No HID device accepted the");
                result.AppendLine(
                    "generic Windows polling query.");

                result.AppendLine();

                result.AppendLine(
                    "The device may use vendor-specific");
                result.AppendLine(
                    "firmware or a proprietary driver.");

                return result.ToString();
            }

            int changed = 0;
            int limited = 0;

            foreach (
                HidPollingDevice device
                in devices)
            {
                result.AppendLine(
                    device.DisplayName);

                if (device.CurrentIntervalMs > 0)
                {
                    result.AppendLine(
                        "Current: ~" +
                        CalculateHz(
                            device.CurrentIntervalMs) +
                        " Hz");
                }
                else
                {
                    result.AppendLine(
                        "Current: unavailable");
                }

                bool success =
                    TrySetPollingFrequency(
                        device.DevicePath,
                        1);

                if (success)
                {
                    changed++;

                    result.AppendLine(
                        "Requested: 1 ms (~1000 Hz) ✓");
                }
                else
                {
                    limited++;

                    result.AppendLine(
                        "Driver did not accept request.");
                }

                result.AppendLine();
            }

            result.AppendLine(
                "Accepted: " +
                changed);

            result.AppendLine(
                "Driver-limited: " +
                limited);

            result.AppendLine();

            result.AppendLine(
                "True 2K/4K/8K can require");
            result.AppendLine(
                "vendor firmware/driver support.");

            return result.ToString();
        }

        private int CalculateHz(
            int intervalMs)
        {
            if (intervalMs <= 0)
                return 0;

            return (int)Math.Round(
                1000.0 /
                intervalMs);
        }

        // =========================================================
        // HID ENUMERATION
        // =========================================================

        private List<HidPollingDevice>
            EnumerateHidPollingDevices()
        {
            List<HidPollingDevice> devices =
                new List<HidPollingDevice>();

            Guid localGuid =
                HidInterfaceGuid;

            IntPtr set =
                SetupDiGetClassDevs(
                    ref localGuid,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    DigcfPresent |
                    DigcfDeviceInterface);

            if (set ==
                new IntPtr(-1))
            {
                return devices;
            }

            try
            {
                uint index = 0;

                while (true)
                {
                    SP_DEVICE_INTERFACE_DATA data =
                        new SP_DEVICE_INTERFACE_DATA();

                    data.cbSize =
                        Marshal.SizeOf(
                            typeof(
                                SP_DEVICE_INTERFACE_DATA));

                    bool success =
                        SetupDiEnumDeviceInterfaces(
                            set,
                            IntPtr.Zero,
                            ref localGuid,
                            index,
                            ref data);

                    if (!success)
                        break;

                    string path =
                        GetInterfacePath(
                            set,
                            ref data);

                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        int interval =
                            GetPollingFrequency(path);

                        if (interval >= 0)
                        {
                            devices.Add(
                                new HidPollingDevice
                                {
                                    DevicePath =
                                        path,

                                    DisplayName =
                                        GetDeviceDisplayName(
                                            path),

                                    CurrentIntervalMs =
                                        interval
                                });
                        }
                    }

                    index++;
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(
                    set);
            }

            return devices;
        }

        private string GetInterfacePath(
            IntPtr set,
            ref SP_DEVICE_INTERFACE_DATA data)
        {
            uint required = 0;

            SetupDiGetDeviceInterfaceDetail(
                set,
                ref data,
                IntPtr.Zero,
                0,
                ref required,
                IntPtr.Zero);

            if (required == 0)
                return "";

            IntPtr buffer =
                Marshal.AllocHGlobal(
                    checked((int)required));

            try
            {
                Marshal.WriteInt32(
                    buffer,
                    IntPtr.Size == 8 ? 8 : 5);

                bool success =
                    SetupDiGetDeviceInterfaceDetail(
                        set,
                        ref data,
                        buffer,
                        required,
                        ref required,
                        IntPtr.Zero);

                if (!success)
                    return "";

                IntPtr pointer =
                    IntPtr.Add(
                        buffer,
                        4);

                return
                    Marshal.PtrToStringUni(
                        pointer)
                    ?? "";
            }
            finally
            {
                Marshal.FreeHGlobal(
                    buffer);
            }
        }

        private string GetDeviceDisplayName(
            string path)
        {
            string upper =
                path.ToUpperInvariant();

            int vid =
                upper.IndexOf("VID_");

            int pid =
                upper.IndexOf("PID_");

            if (vid >= 0 &&
                pid > vid)
            {
                int length =
                    Math.Min(
                        17,
                        path.Length - vid);

                return
                    "HID Device " +
                    path.Substring(
                        vid,
                        length);
            }

            return
                "HID polling device";
        }

        // =========================================================
        // HID GET / SET
        // =========================================================

        private int GetPollingFrequency(
            string path)
        {
            using (
                SafeFileHandle handle =
                CreateHidHandle(path))
            {
                if (handle == null ||
                    handle.IsInvalid)
                {
                    return -1;
                }

                IntPtr output =
                    Marshal.AllocHGlobal(
                        sizeof(uint));

                try
                {
                    uint returned;

                    bool success =
                        DeviceIoControl(
                            handle,
                            IoctlHidGetPollFrequency,
                            IntPtr.Zero,
                            0,
                            output,
                            sizeof(uint),
                            out returned,
                            IntPtr.Zero);

                    if (!success ||
                        returned < sizeof(uint))
                    {
                        return -1;
                    }

                    uint interval =
                        unchecked(
                            (uint)Marshal.ReadInt32(
                                output));

                    if (interval >
                        int.MaxValue)
                    {
                        return -1;
                    }

                    return (int)interval;
                }
                catch
                {
                    return -1;
                }
                finally
                {
                    Marshal.FreeHGlobal(
                        output);
                }
            }
        }

        private bool TrySetPollingFrequency(
            string path,
            uint milliseconds)
        {
            using (
                SafeFileHandle handle =
                CreateHidHandle(path))
            {
                if (handle == null ||
                    handle.IsInvalid)
                {
                    return false;
                }

                IntPtr input =
                    Marshal.AllocHGlobal(
                        sizeof(uint));

                try
                {
                    Marshal.WriteInt32(
                        input,
                        unchecked(
                            (int)milliseconds));

                    uint returned;

                    return DeviceIoControl(
                        handle,
                        IoctlHidSetPollFrequency,
                        input,
                        sizeof(uint),
                        IntPtr.Zero,
                        0,
                        out returned,
                        IntPtr.Zero);
                }
                catch
                {
                    return false;
                }
                finally
                {
                    Marshal.FreeHGlobal(
                        input);
                }
            }
        }

        private SafeFileHandle CreateHidHandle(
            string path)
        {
            return CreateFile(
                path,
                GenericRead |
                GenericWrite,
                FileShareRead |
                FileShareWrite,
                IntPtr.Zero,
                OpenExisting,
                0,
                IntPtr.Zero);
        }

        // =========================================================
        // STARTUP
        // =========================================================

        private string ApplyStartupOptimization()
        {
            int changed = 0;

            try
            {
                BackupStartupSettings();

                using (
                    RegistryKey runKey =
                    Registry.CurrentUser.OpenSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Run",
                        true))
                {
                    using (
                        RegistryKey approvedKey =
                        Registry.CurrentUser.CreateSubKey(
                            @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"))
                    {
                        if (runKey != null &&
                            approvedKey != null)
                        {
                            foreach (
                                string name
                                in runKey.GetValueNames())
                            {
                                object value =
                                    runKey.GetValue(name);

                                string command =
                                    value == null
                                        ? ""
                                        : value.ToString();

                                bool match =
                                    StartupNames.Any(
                                        item =>
                                            name.IndexOf(
                                                item,
                                                StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            command.IndexOf(
                                                item,
                                                StringComparison.OrdinalIgnoreCase) >= 0);

                                if (!match)
                                    continue;

                                approvedKey.SetValue(
                                    name,
                                    new byte[]
                                    {
                                        0x03,
                                        0x00,
                                        0x00,
                                        0x00,
                                        0x00,
                                        0x00,
                                        0x00,
                                        0x00,
                                        0x00,
                                        0x00,
                                        0x00,
                                        0x00
                                    },
                                    RegistryValueKind.Binary);

                                changed++;
                            }
                        }
                    }
                }

                return
                    "✓ Startup optimization applied. " +
                    changed +
                    " matching entries disabled.";
            }
            catch (Exception ex)
            {
                return
                    "Startup optimization failed: " +
                    ex.Message;
            }
        }

        private void BackupStartupSettings()
        {
            try
            {
                StringBuilder backup =
                    new StringBuilder();

                backup.AppendLine(
                    "LePork Startup Backup");

                backup.AppendLine(
                    DateTime.Now.ToString());

                using (
                    RegistryKey key =
                    Registry.CurrentUser.OpenSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Run"))
                {
                    if (key != null)
                    {
                        backup.AppendLine();
                        backup.AppendLine("[Run]");

                        foreach (
                            string name
                            in key.GetValueNames())
                        {
                            object value =
                                key.GetValue(name);

                            backup.AppendLine(
                                name +
                                "=" +
                                (value ?? ""));
                        }
                    }
                }

                File.WriteAllText(
                    StartupBackupFile,
                    backup.ToString());
            }
            catch
            {
            }
        }

        // =========================================================
        // WINDOWS
        // =========================================================

        private void WindowsApply_Click(
            object sender,
            RoutedEventArgs e)
        {
            ApplyWindowsOptimization();

            SetStatus(
                "✓ Windows optimization applied.");
        }

        private void ApplyWindowsOptimization()
        {
            ApplyGameMode();
            ApplyVisualOptimization();

            SetRegistryValue(
                Registry.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\GameDVR",
                "AppCaptureEnabled",
                0,
                RegistryValueKind.DWord);

            SetRegistryValue(
                Registry.CurrentUser,
                @"System\GameConfigStore",
                "GameDVR_Enabled",
                0,
                RegistryValueKind.DWord);

            if (IsAdministrator())
            {
                SetRegistryValue(
                    Registry.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                    "HwSchMode",
                    2,
                    RegistryValueKind.DWord);
            }
        }

        private bool IsAdministrator()
        {
            try
            {
                using (
                    WindowsIdentity identity =
                    WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal =
                        new WindowsPrincipal(identity);

                    return principal.IsInRole(
                        WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        // =========================================================
        // SYSTEM REPAIR
        // =========================================================

        private async void SfcScan_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetElementText(
                "WindowsRepairStatus",
                "Running SFC /scannow...");

            SetStatus(
                "Running System File Checker...");

            bool success =
                await RunElevatedCommandAsync(
                    "sfc.exe",
                    "/scannow");

            if (success)
            {
                SetElementText(
                    "WindowsRepairStatus",
                    "✓ SFC /scannow completed.");

                SetStatus(
                    "✓ SFC scan completed.");
            }
            else
            {
                SetElementText(
                    "WindowsRepairStatus",
                    "SFC failed, was cancelled, or returned an error.");

                SetStatus(
                    "SFC did not complete successfully.");
            }
        }

        private async void RestoreHealth_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetElementText(
                "WindowsRepairStatus",
                "Running DISM RestoreHealth...");

            SetStatus(
                "Repairing Windows image...");

            bool success =
                await RunElevatedCommandAsync(
                    "DISM.exe",
                    "/Online /Cleanup-Image /RestoreHealth");

            if (success)
            {
                SetElementText(
                    "WindowsRepairStatus",
                    "✓ DISM RestoreHealth completed.");

                SetStatus(
                    "✓ Windows image repair completed.");
            }
            else
            {
                SetElementText(
                    "WindowsRepairStatus",
                    "DISM failed, was cancelled, or returned an error.");

                SetStatus(
                    "DISM did not complete successfully.");
            }
        }

        // =========================================================
        // ELEVATED COMMAND
        // =========================================================

        private async Task<bool>
            RunElevatedCommandAsync(
                string fileName,
                string arguments)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo info =
                        new ProcessStartInfo
                        {
                            FileName =
                                fileName,

                            Arguments =
                                arguments,

                            UseShellExecute =
                                true,

                            Verb =
                                "runas"
                        };

                    using (
                        Process process =
                        Process.Start(info))
                    {
                        if (process == null)
                            return false;

                        process.WaitForExit();

                        return
                            process.ExitCode == 0;
                    }
                }
                catch
                {
                    return false;
                }
            });
        }

        // =========================================================
        // NETWORK
        // =========================================================

        private void NetworkApply_Click(
            object sender,
            RoutedEventArgs e)
        {
            ApplyNetworkOptimization();

            UpdateNetworkStatus();

            SetStatus(
                "✓ Network optimization applied.");
        }

        private void DnsApply_Click(
            object sender,
            RoutedEventArgs e)
        {
            RunCommand(
                "ipconfig.exe",
                "/flushdns");

            SetStatus(
                "✓ DNS cache flushed.");
        }

        private async void DisableAutotuning_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetStatus(
                "Disabling global TCP autotuning...");

            bool success =
                await RunElevatedCommandAsync(
                    "netsh.exe",
                    "interface tcp set global autotuninglevel=disabled");

            if (success)
            {
                SetStatus(
                    "✓ Global TCP autotuning disabled.");
            }
            else
            {
                SetStatus(
                    "Could not disable global TCP autotuning.");
            }
        }

        private async void NormalAutotuning_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetStatus(
                "Setting global TCP autotuning to normal...");

            bool success =
                await RunElevatedCommandAsync(
                    "netsh.exe",
                    "interface tcp set global autotuninglevel=normal");

            if (success)
            {
                SetStatus(
                    "✓ Global TCP autotuning set to normal.");
            }
            else
            {
                SetStatus(
                    "Could not set global TCP autotuning.");
            }
        }

        private void ApplyNetworkOptimization()
        {
            RunCommand(
                "ipconfig.exe",
                "/flushdns");

            RunCommand(
                "netsh.exe",
                "interface tcp set global rss=enabled");

            RunCommand(
                "netsh.exe",
                "interface tcp set global autotuninglevel=normal");
        }

        private async void UpdateNetworkStatus()
        {
            try
            {
                NetworkInterface[] active =
                    NetworkInterface
                        .GetAllNetworkInterfaces()
                        .Where(
                            n =>
                                n.OperationalStatus ==
                                OperationalStatus.Up &&
                                n.NetworkInterfaceType !=
                                NetworkInterfaceType.Loopback)
                        .ToArray();

                if (active.Length == 0)
                {
                    SetElementText(
                        "NetworkStatus",
                        "No active network adapter detected.");

                    return;
                }

                StringBuilder status =
                    new StringBuilder();

                foreach (
                    NetworkInterface adapter
                    in active)
                {
                    status.AppendLine(
                        adapter.Name);

                    status.AppendLine(
                        "Link speed: " +
                        (
                            adapter.Speed /
                            1000000
                        ) +
                        " Mbps");

                    status.AppendLine();
                }

                try
                {
                    using (
                        Ping ping =
                        new Ping())
                    {
                        PingReply reply =
                            await ping.SendPingAsync(
                                "1.1.1.1",
                                1000);

                        if (reply.Status ==
                            IPStatus.Success)
                        {
                            status.AppendLine(
                                "Ping: " +
                                reply.RoundtripTime +
                                " ms");
                        }
                        else
                        {
                            status.AppendLine(
                                "Ping: unavailable");
                        }
                    }
                }
                catch
                {
                    status.AppendLine(
                        "Ping: unavailable");
                }

                SetElementText(
                    "NetworkStatus",
                    status.ToString());
            }
            catch
            {
                SetElementText(
                    "NetworkStatus",
                    "Unable to read network status.");
            }
        }

        // =========================================================
        // FORTNITE
        // =========================================================

        private string GetFortniteConfigPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "FortniteGame",
                "Saved",
                "Config",
                "WindowsClient");
        }

        private string GetFortniteSettingsFile()
        {
            return Path.Combine(
                GetFortniteConfigPath(),
                "GameUserSettings.ini");
        }

        private void FortniteApply_Click(
            object sender,
            RoutedEventArgs e)
        {
            ApplyFortniteOptimization();

            UpdateFortniteStatus();

            SetStatus(
                "✓ Fortnite optimization completed.");
        }

        private void ApplyFortniteOptimization()
        {
            string file =
                GetFortniteSettingsFile();

            if (!File.Exists(file))
            {
                SetElementText(
                    "FortniteStatus",
                    "GameUserSettings.ini was not found.");

                return;
            }

            try
            {
                string backup =
                    CreateFortniteBackup(file);

                List<string> lines =
                    File.ReadAllLines(file)
                        .ToList();

                const string section =
                    "[/Script/FortniteGame.FortGameUserSettings]";

                SetIniValue(
                    lines,
                    section,
                    "bUseVSync",
                    "False");

                SetIniValue(
                    lines,
                    section,
                    "bMotionBlur",
                    "False");

                SetIniValue(
                    lines,
                    section,
                    "bUseDynamicResolution",
                    "False");

                SetIniValue(
                    lines,
                    section,
                    "bRayTracing",
                    "False");

                SetIniValue(
                    lines,
                    section,
                    "bShowFPS",
                    "True");

                SetIniValue(
                    lines,
                    section,
                    "FrameRateLimit",
                    "0.000000");

                SetIniValue(
                    lines,
                    section,
                    "FullscreenMode",
                    "0");

                SetIniValue(
                    lines,
                    section,
                    "LastConfirmedFullscreenMode",
                    "0");

                SetIniValue(
                    lines,
                    section,
                    "PreferredFullscreenMode",
                    "0");

                string[] scalability =
                {
                    "sg.AntiAliasingQuality",
                    "sg.ShadowQuality",
                    "sg.GlobalIlluminationQuality",
                    "sg.ReflectionQuality",
                    "sg.PostProcessQuality",
                    "sg.EffectsQuality",
                    "sg.FoliageQuality",
                    "sg.ShadingQuality"
                };

                foreach (string key in scalability)
                {
                    SetIniValue(
                        lines,
                        "[ScalabilityGroups]",
                        key,
                        "0");
                }

                File.WriteAllLines(
                    file,
                    lines);

                SetElementText(
                    "FortniteStatus",
                    "✓ Fortnite optimized.\nBackup: " +
                    Path.GetFileName(backup));
            }
            catch (Exception ex)
            {
                SetElementText(
                    "FortniteStatus",
                    "Fortnite optimization failed:\n" +
                    ex.Message);
            }
        }

        private void SetIniValue(
            List<string> lines,
            string section,
            string key,
            string value)
        {
            int sectionIndex = -1;

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Trim().Equals(
                        section,
                        StringComparison.OrdinalIgnoreCase))
                {
                    sectionIndex = i;
                    break;
                }
            }

            if (sectionIndex < 0)
            {
                lines.Add("");
                lines.Add(section);
                lines.Add(
                    key +
                    "=" +
                    value);

                return;
            }

            int end =
                lines.Count;

            for (
                int i = sectionIndex + 1;
                i < lines.Count;
                i++)
            {
                string current =
                    lines[i].Trim();

                if (current.StartsWith("[") &&
                    current.EndsWith("]"))
                {
                    end = i;
                    break;
                }
            }

            string prefix =
                key + "=";

            for (
                int i = sectionIndex + 1;
                i < end;
                i++)
            {
                if (lines[i]
                    .TrimStart()
                    .StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] =
                        key +
                        "=" +
                        value;

                    return;
                }
            }

            lines.Insert(
                end,
                key +
                "=" +
                value);
        }

        private string CreateFortniteBackup(
            string file)
        {
            Directory.CreateDirectory(
                FortniteBackupFolder);

            string timestamp =
                DateTime.Now.ToString(
                    "yyyyMMdd_HHmmss");

            string backup =
                Path.Combine(
                    FortniteBackupFolder,
                    "GameUserSettings_" +
                    timestamp +
                    ".ini");

            File.Copy(
                file,
                backup,
                true);

            return backup;
        }

        private void FortniteBackup_Click(
            object sender,
            RoutedEventArgs e)
        {
            string file =
                GetFortniteSettingsFile();

            if (!File.Exists(file))
            {
                SetElementText(
                    "FortniteStatus",
                    "GameUserSettings.ini was not found.");

                return;
            }

            string backup =
                CreateFortniteBackup(file);

            SetElementText(
                "FortniteStatus",
                "✓ Backup created:\n" +
                Path.GetFileName(backup));

            SetStatus(
                "✓ Fortnite backup created.");
        }

        private void FortniteRestore_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                string latest =
                    Directory.GetFiles(
                            FortniteBackupFolder,
                            "GameUserSettings_*.ini")
                        .OrderByDescending(
                            File.GetLastWriteTime)
                        .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(latest))
                {
                    SetStatus(
                        "No Fortnite backup was found.");

                    return;
                }

                File.Copy(
                    latest,
                    GetFortniteSettingsFile(),
                    true);

                SetElementText(
                    "FortniteStatus",
                    "✓ Latest Fortnite backup restored.");

                SetStatus(
                    "✓ Fortnite backup restored.");
            }
            catch
            {
                SetStatus(
                    "Fortnite restore failed.");
            }
        }

        private void UpdateFortniteStatus()
        {
            SetElementText(
                "FortniteStatus",
                File.Exists(
                    GetFortniteSettingsFile())
                    ? "✓ GameUserSettings.ini detected."
                    : "GameUserSettings.ini not detected.");
        }

        // =========================================================
        // BACKUPS
        // =========================================================

        private void BackupAll()
        {
            BackupMouseSettings();
            BackupPowerSettings();
            BackupStartupSettings();
            BackupFortniteIfPresent();
        }

        private void BackupMouseSettings()
        {
            try
            {
                string path =
                    Path.Combine(
                        LePorkFolder,
                        "MouseBackup.txt");

                using (
                    RegistryKey key =
                    Registry.CurrentUser.OpenSubKey(
                        @"Control Panel\Mouse"))
                {
                    if (key == null)
                        return;

                    StringBuilder backup =
                        new StringBuilder();

                    foreach (
                        string name
                        in key.GetValueNames())
                    {
                        object value =
                            key.GetValue(name);

                        backup.AppendLine(
                            name +
                            "=" +
                            (value ?? ""));
                    }

                    File.WriteAllText(
                        path,
                        backup.ToString());
                }
            }
            catch
            {
            }
        }

        private void BackupPowerSettings()
        {
            try
            {
                File.WriteAllText(
                    Path.Combine(
                        LePorkFolder,
                        "PowerBackup.txt"),
                    "LePork Power Backup\n" +
                    DateTime.Now);
            }
            catch
            {
            }
        }

        private void BackupFortniteIfPresent()
        {
            string file =
                GetFortniteSettingsFile();

            if (File.Exists(file))
            {
                CreateFortniteBackup(file);
            }
        }

        // =========================================================
        // RESTORE
        // =========================================================

        private void RestoreAll_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                RunCommand(
                    "powercfg.exe",
                    "/setactive SCHEME_BALANCED");

                RestoreMouseDefaults();

                SetStatus(
                    "✓ LePork changes restored.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "LePork",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void RestoreMouseDefaults()
        {
            string path =
                @"Control Panel\Mouse";

            SetRegistryValue(
                Registry.CurrentUser,
                path,
                "MouseSpeed",
                "1",
                RegistryValueKind.String);

            SetRegistryValue(
                Registry.CurrentUser,
                path,
                "MouseThreshold1",
                "6",
                RegistryValueKind.String);

            SetRegistryValue(
                Registry.CurrentUser,
                path,
                "MouseThreshold2",
                "10",
                RegistryValueKind.String);
        }

        // =========================================================
        // REGISTRY
        // =========================================================

        private void SetRegistryValue(
            RegistryKey root,
            string path,
            string name,
            object value,
            RegistryValueKind kind)
        {
            try
            {
                using (
                    RegistryKey key =
                    root.CreateSubKey(path))
                {
                    if (key != null)
                    {
                        key.SetValue(
                            name,
                            value,
                            kind);
                    }
                }
            }
            catch
            {
            }
        }

        // =========================================================
        // COMMAND
        // =========================================================

        private string RunCommand(
            string fileName,
            string arguments)
        {
            try
            {
                ProcessStartInfo info =
                    new ProcessStartInfo
                    {
                        FileName =
                            fileName,

                        Arguments =
                            arguments,

                        UseShellExecute =
                            false,

                        CreateNoWindow =
                            true,

                        RedirectStandardOutput =
                            true,

                        RedirectStandardError =
                            true
                    };

                using (
                    Process process =
                    Process.Start(info))
                {
                    if (process == null)
                        return "";

                    string output =
                        process.StandardOutput.ReadToEnd();

                    process.WaitForExit();

                    return output;
                }
            }
            catch
            {
                return "";
            }
        }

        // =========================================================
        // ADVANCED
        // =========================================================

        private void TaskManager_Click(
            object sender,
            RoutedEventArgs e)
        {
            StartProgram("taskmgr.exe");
        }

        private void DeviceManager_Click(
            object sender,
            RoutedEventArgs e)
        {
            StartProgram("devmgmt.msc");
        }

        private void Cmd_Click(
            object sender,
            RoutedEventArgs e)
        {
            StartProgram("cmd.exe");
        }

        private void StartProgram(
            string program)
        {
            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName =
                            program,

                        UseShellExecute =
                            true
                    });
            }
            catch
            {
                SetStatus(
                    "Could not start " +
                    program);
            }
        }

        // =========================================================
        // NATIVE HID API
        // =========================================================

        [StructLayout(
            LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        [DllImport(
            "setupapi.dll",
            SetLastError = true)]
        private static extern IntPtr
            SetupDiGetClassDevs(
                ref Guid ClassGuid,
                IntPtr Enumerator,
                IntPtr hwndParent,
                uint Flags);

        [DllImport(
            "setupapi.dll",
            SetLastError = true)]
        private static extern bool
            SetupDiEnumDeviceInterfaces(
                IntPtr DeviceInfoSet,
                IntPtr DeviceInfoData,
                ref Guid InterfaceClassGuid,
                uint MemberIndex,
                ref SP_DEVICE_INTERFACE_DATA
                    DeviceInterfaceData);

        [DllImport(
            "setupapi.dll",
            SetLastError = true)]
        private static extern bool
            SetupDiGetDeviceInterfaceDetail(
                IntPtr DeviceInfoSet,
                ref SP_DEVICE_INTERFACE_DATA
                    DeviceInterfaceData,
                IntPtr DeviceInterfaceDetailData,
                uint DeviceInterfaceDetailDataSize,
                ref uint RequiredSize,
                IntPtr DeviceInfoData);

        [DllImport(
            "setupapi.dll",
            SetLastError = true)]
        private static extern bool
            SetupDiDestroyDeviceInfoList(
                IntPtr DeviceInfoSet);

        [DllImport(
            "kernel32.dll",
            CharSet = CharSet.Unicode,
            SetLastError = true)]
        private static extern SafeFileHandle
            CreateFile(
                string lpFileName,
                uint dwDesiredAccess,
                uint dwShareMode,
                IntPtr lpSecurityAttributes,
                uint dwCreationDisposition,
                uint dwFlagsAndAttributes,
                IntPtr hTemplateFile);

        [DllImport(
            "kernel32.dll",
            SetLastError = true)]
        private static extern bool
            DeviceIoControl(
                SafeFileHandle hDevice,
                uint dwIoControlCode,
                IntPtr lpInBuffer,
                uint nInBufferSize,
                IntPtr lpOutBuffer,
                uint nOutBufferSize,
                out uint lpBytesReturned,
                IntPtr lpOverlapped);
    }
}