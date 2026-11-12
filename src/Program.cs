using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace WinCalendar
{
    internal static class Program
    {
        internal static readonly uint CommandMessage = RegisterWindowMessage("WinCalendar.DateOnly.v2");
        internal const string WindowMarker = "WinCalendar.DateIndicator.CommandWindow";
        internal const int RefreshCommand = 0;
        internal const int StartupOnCommand = 1;
        internal const int StartupOffCommand = 2;
        internal const int ExitCommand = 3;
        internal const int MenuCommand = 4;
        internal const int OpenCalendarCommand = 5;
        internal const int GoogleSetupCommand = 6;
        internal const int GoogleConnectCommand = 7;
        internal const int PreviewReminderCommand = 8;
        internal const int PreferencesCommand = 9;
        internal const int WeatherOnCommand = 10, WeatherOffCommand = 11;
        internal const string GoogleCalendarUrl = "https://calendar.google.com/calendar/";

        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                if (args.Length == 2 && args[0] == "--export-icon")
                {
                    CalendarIcon.Save(args[1], CalendarIcon.Create(DateTime.Today));
                    return 0;
                }
                if (args.Length == 2 && args[0] == "--self-test") return SelfTest.Run(args[1]);
                if (args.Length == 2 && args[0] == "--weather-probe") return WeatherInstalledProbe.Run(args[1]);
                if (args.Length == 2 && args[0] == "--weather-api-probe") return WeatherApiProbe.RunAsync(args[1]).GetAwaiter().GetResult();
                int command = GetLaunchCommand(args.Length == 0 ? null : args[0]);
                if (args.Length > 1 || command < 0)
                {
                    MessageBox.Show(UiText.Get("Неизвестный параметр запуска. Откройте WinDate.exe без параметров."), "WinCalendar");
                    return 2;
                }
                bool firstInstance;
                using (Mutex mutex = new Mutex(true, @"Local\WinCalendar.Desktop.Prototype", out firstInstance))
                {
                    if (!firstInstance)
                    {
                        PostCommand(command, 0);
                        return 0;
                    }
                    try
                    {
                        if (command == ExitCommand) return 0;
                        if (command == WeatherOnCommand || command == WeatherOffCommand)
                            WeatherDateWindow.IsEnabled = command == WeatherOnCommand;
                        if (command == StartupOnCommand || command == StartupOffCommand)
                            ShellIntegration.StartsWithWindows = command == StartupOnCommand;
                        UiText.Language = new PreferenceStore(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinCalendar", "Preferences.bin")).Load().Language;
                        ShellIntegration.EnsureAppShortcut();
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        DateTaskbarWindow window = new DateTaskbarWindow(true, delegate { return DateTime.Now; });
                        window.Shown += delegate
                        {
                            window.StartBackgroundServices();
                            if (command == OpenCalendarCommand) window.BeginInvoke(new Action(window.ToggleCalendar));
                            else if (command == MenuCommand) PostCommand(MenuCommand, Process.GetCurrentProcess().Id);
                            else if (command == GoogleSetupCommand) PostCommand(GoogleSetupCommand, Process.GetCurrentProcess().Id);
                            else if (command == GoogleConnectCommand) PostCommand(GoogleConnectCommand, Process.GetCurrentProcess().Id);
                            else if (command == PreviewReminderCommand) PostCommand(PreviewReminderCommand, Process.GetCurrentProcess().Id);
                            else if (command == PreferencesCommand) PostCommand(PreferencesCommand, Process.GetCurrentProcess().Id);
                        };
                        Application.Run(window);
                    }
                    finally { mutex.ReleaseMutex(); }
                }
                return 0;
            }
            catch (Exception error)
            {
                MessageBox.Show(UiText.Get("Не удалось запустить индикатор даты.\n\n") + error.Message, "WinCalendar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        internal static int GetLaunchCommand(string argument)
        {
            if (argument == null) return OpenCalendarCommand;
            if (argument == "--background") return RefreshCommand;
            if (argument == "--startup-on") return StartupOnCommand;
            if (argument == "--startup-off") return StartupOffCommand;
            if (argument == "--exit") return ExitCommand;
            if (argument == "--menu") return MenuCommand;
            if (argument == "--google-setup") return GoogleSetupCommand;
            if (argument == "--google-connect") return GoogleConnectCommand;
            if (argument == "--preview-reminder") return PreviewReminderCommand;
            if (argument == "--preferences") return PreferencesCommand;
            if (argument == "--weather-on") return WeatherOnCommand;
            if (argument == "--weather-off") return WeatherOffCommand;
            return -1;
        }

        internal static void OpenGoogleCalendar()
        {
            OpenBrowser(GoogleCalendarUrl);
        }

        internal static void OpenCalendarEvent(string url)
        {
            string safe = CalendarPage.SafeGoogleLink(url);
            if (safe != null) OpenBrowser(safe);
        }

        internal static void OpenBrowser(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Win32Exception) { BrowserUnavailable(); }
            catch (InvalidOperationException) { BrowserUnavailable(); }
            catch (System.IO.FileNotFoundException) { BrowserUnavailable(); }
        }

        private static void BrowserUnavailable()
        {
            MessageBox.Show(UiText.Get("Не удалось открыть браузер. Выберите приложение по умолчанию для HTTPS в параметрах Windows и повторите попытку."), "WinCalendar", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        internal static bool PostCommand(int command, int targetProcess)
        {
            bool posted = false;
            int caller = Process.GetCurrentProcess().Id;
            EnumWindows(delegate(IntPtr window, IntPtr parameter)
            {
                long owner = GetProp(window, WindowMarker).ToInt64();
                if (owner == 0 || (targetProcess == 0 ? owner == caller : owner != targetProcess)) return true;
                // A user-launched secondary process passes activation rights
                // to the existing host before forwarding an interactive command.
                if (command == OpenCalendarCommand || command == MenuCommand || command == GoogleSetupCommand || command == GoogleConnectCommand || command == PreferencesCommand)
                    AllowSetForegroundWindow((uint)owner);
                posted = PostMessage(window, CommandMessage, new IntPtr(command), IntPtr.Zero);
                return !posted;
            }, IntPtr.Zero);
            return posted;
        }

        private delegate bool WindowCallback(IntPtr window, IntPtr parameter);
        [DllImport("user32.dll")]
        private static extern bool AllowSetForegroundWindow(uint process);
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(WindowCallback callback, IntPtr parameter);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetProp(IntPtr window, string name);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern uint RegisterWindowMessage(string name);
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    }
}
