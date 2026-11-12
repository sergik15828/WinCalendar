using System;
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
                if (args.Length > 1 || (args.Length == 1 && args[0] != "--background" && args[0] != "--startup-on" && args[0] != "--startup-off" && args[0] != "--exit"))
                {
                    MessageBox.Show("Неизвестный параметр запуска. Откройте WinCalendar.exe без параметров.", "WinCalendar");
                    return 2;
                }
                bool firstInstance;
                int command = args.Length == 0 || args[0] == "--background" ? RefreshCommand :
                    args[0] == "--startup-on" ? StartupOnCommand : args[0] == "--startup-off" ? StartupOffCommand : ExitCommand;
                using (Mutex mutex = new Mutex(true, @"Local\WinCalendar.Desktop.Prototype", out firstInstance))
                {
                    if (!firstInstance)
                    {
                        PostCommand(args.Length == 0 ? MenuCommand : command, 0);
                        return 0;
                    }
                    try
                    {
                        if (command == ExitCommand) return 0;
                        if (command == StartupOnCommand || command == StartupOffCommand)
                            ShellIntegration.StartsWithWindows = command == StartupOnCommand;
                        ShellIntegration.EnsureAppShortcut();
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        DateTaskbarWindow window = new DateTaskbarWindow(true, delegate { return DateTime.Now; });
                        Application.Run(window);
                    }
                    finally { mutex.ReleaseMutex(); }
                }
                return 0;
            }
            catch (Exception error)
            {
                MessageBox.Show("Не удалось запустить индикатор даты.\n\n" + error.Message, "WinCalendar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        internal static bool PostCommand(int command, int targetProcess)
        {
            bool posted = false;
            int caller = Process.GetCurrentProcess().Id;
            EnumWindows(delegate(IntPtr window, IntPtr parameter)
            {
                long owner = GetProp(window, WindowMarker).ToInt64();
                if (owner == 0 || (targetProcess == 0 ? owner == caller : owner != targetProcess)) return true;
                posted = PostMessage(window, CommandMessage, new IntPtr(command), IntPtr.Zero);
                return !posted;
            }, IntPtr.Zero);
            return posted;
        }

        private delegate bool WindowCallback(IntPtr window, IntPtr parameter);
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
