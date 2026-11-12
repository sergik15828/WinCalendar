using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace WinCalendar
{
    // Integration check restricted to windows marked by this application.
    // Does not inspect calendar contents, credentials or other applications.
    internal static class WeatherInstalledProbe
    {
        internal static int Run(string directory)
        {
            Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, "weather-installed.txt");
            IntPtr host = Find(Program.WindowMarker, 0), date = IntPtr.Zero;
            int pid = host == IntPtr.Zero ? 0 : GetProp(host, Program.WindowMarker).ToInt32();
            try
            {
                Need(pid != 0, "Installed host is running");
                date = Find("WinCalendar.WeatherDate", pid);
                Need(date != IntPtr.Zero && IsWindowVisible(date), "Weather date is visible");
                long style = GetWindowLongPtr(date, -20).ToInt64();
                Need((style & 0x08000080) == 0x08000080 && (style & 0x40000) == 0, "Date is a nonactivating toolwindow with no taskbar button");
                Rectangle bounds = Bounds(date);
                // Hide an already open own flyout so the check is repeatable.
                IntPtr flyout = Find("WinCalendar.Flyout", pid);
                if (flyout != IntPtr.Zero && IsWindowVisible(flyout)) { Click(date); Thread.Sleep(250); }
                SendMessage(host, Program.CommandMessage, new IntPtr(Program.PreviewReminderCommand), IntPtr.Zero);
                Thread.Sleep(700);
                IntPtr pulse = Find("WinCalendar.ReminderAccent", pid);
                Need(pulse != IntPtr.Zero && IsWindowVisible(pulse), "Reminder preview is visible");
                Rectangle accent = Bounds(pulse);
                Need(Math.Abs(accent.Left + accent.Width / 2 - bounds.Left - bounds.Height * 24 / 44) <= 2 && Math.Abs(accent.Top + accent.Height / 2 - bounds.Top - bounds.Height / 2) <= 2,
                    "Reminder pulse is centered on the widget calendar symbol");
                long hideCount = GetProp(date, "WinCalendar.WeatherHideCount").ToInt64();
                Click(date); Thread.Sleep(400);
                WaitVisible(date, hideCount);
                flyout = Find("WinCalendar.Flyout", pid);
                Need(flyout != IntPtr.Zero && IsWindowVisible(flyout), "Click opens the existing calendar");
                Need(!IsWindowVisible(pulse), "Click acknowledges and hides reminder pulse");
                Click(date); Thread.Sleep(250);
                WaitVisible(date, hideCount);
                Need(!IsWindowVisible(flyout), "Second click closes the same calendar");
                File.WriteAllText(file, "PASS installed weather date, native click, shared calendar, reminder anchor and acknowledgement\r\nDate=" + bounds + "\r\nReminder=" + accent + "\r\nStableAfterOpenAndClose=True\r\nFinalCalendarVisible=False");
                return 0;
            }
            catch (Exception error)
            {
                // Clear the diagnostic preview even if an assertion failed.
                if (host != IntPtr.Zero) { SendMessage(host, Program.CommandMessage, new IntPtr(Program.OpenCalendarCommand), IntPtr.Zero); Thread.Sleep(200); }
                File.WriteAllText(file, "FAIL " + error.Message); return 1;
            }
        }
        private static void Need(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void WaitVisible(IntPtr date, long before)
        {
            for (int i = 0; i < 50; i++)
            { Thread.Sleep(50); Need(IsWindowVisible(date) && GetProp(date, "WinCalendar.WeatherHideCount").ToInt64() == before, "Widget stays visible after calendar click"); }
        }
        private static void Click(IntPtr date)
        {
            Rectangle r = Bounds(date); IntPtr point = new IntPtr((r.Height / 2 << 16) | r.Width / 2);
            SendMessage(date, 0x201, new IntPtr(1), point); SendMessage(date, 0x202, IntPtr.Zero, point);
        }
        private static Rectangle Bounds(IntPtr h) { NativeRect r; GetWindowRect(h, out r); return Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom); }
        private static IntPtr Find(string marker, int pid)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows(delegate(IntPtr h, IntPtr unused)
            {
                long value = GetProp(h, marker).ToInt64();
                if (value != 0 && (pid == 0 || value == pid)) { found = h; return false; }
                return true;
            }, IntPtr.Zero); return found;
        }
        private delegate bool Callback(IntPtr h, IntPtr parameter);
        [StructLayout(LayoutKind.Sequential)] private struct NativeRect { internal int Left, Top, Right, Bottom; }
        [DllImport("user32.dll")] private static extern bool EnumWindows(Callback callback, IntPtr parameter);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetProp(IntPtr h, string name);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr h, out NativeRect r);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr h, int index);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr h, uint message, IntPtr wParam, IntPtr lParam);
    }
}
