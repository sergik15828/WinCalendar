using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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
                // A real user can leave preferences selected before this probe.
                // Return through its Back action to establish a calendar baseline.
                SendMessage(host, Program.CommandMessage, new IntPtr(Program.PreferencesCommand), IntPtr.Zero); Thread.Sleep(300);
                IntPtr flyout = Find("WinCalendar.Flyout", pid);
                IntPtr initialBack = BackButton(flyout); Need(initialBack != IntPtr.Zero, "Calendar baseline has a preferences Back action");
                SendMessage(initialBack, 0xf5, IntPtr.Zero, IntPtr.Zero); Thread.Sleep(150);
                SendMessage(host, Program.CommandMessage, new IntPtr(Program.PreviewReminderCommand), IntPtr.Zero);
                Thread.Sleep(700);
                Need(GetProp(date, "WinCalendar.WeatherReminderActive").ToInt64() == 1, "Widget reminder is active");
                long frames = GetProp(date, "WinCalendar.WeatherFrames").ToInt64(); Thread.Sleep(250);
                bool animated = GetProp(date, "WinCalendar.WeatherFrames").ToInt64() > frames;
                Need(animated || GetProp(date, "WinCalendar.WeatherReminderMotion").ToInt64() == 0, "Widget reminder frames advance unless reduced motion is enabled");
                long hideCount = GetProp(date, "WinCalendar.WeatherHideCount").ToInt64();
                Click(date); Thread.Sleep(400);
                WaitVisible(date, hideCount);
                flyout = Find("WinCalendar.Flyout", pid);
                Need(flyout != IntPtr.Zero && IsWindowVisible(flyout), "Click opens the existing calendar; ReminderActive=" + GetProp(date, "WinCalendar.WeatherReminderActive").ToInt64() + "; Native=" + Bounds(date) + "; BaseHeight=" + GetProp(date, "WinCalendar.WeatherBaseHeight").ToInt64());
                Rectangle calendarBounds = Bounds(flyout);
                Need(GetProp(date, "WinCalendar.WeatherReminderActive").ToInt64() == 0, "Click acknowledges and stops icon blinking");
                Click(date); Thread.Sleep(250);
                WaitVisible(date, hideCount);
                Need(!IsWindowVisible(flyout), "Second click closes the same calendar");
                bool combined = GetProp(date, "WinCalendar.WeatherCombined").ToInt64() == 1;
                if (combined)
                {
                    ClickWeather(date); Thread.Sleep(400);
                    Need(IsWindowVisible(flyout) && GetProp(flyout, "WinCalendar.WeatherDetails").ToInt64() == 1, "Weather click opens details in the same flyout");
                    Click(date); Thread.Sleep(250);
                    Need(IsWindowVisible(flyout) && GetProp(flyout, "WinCalendar.WeatherDetails").ToInt64() == 0, "Date click returns from weather to calendar");
                    Click(date); Thread.Sleep(250); Need(!IsWindowVisible(flyout), "Calendar closes after returning from weather");
                }
                SendMessage(host, Program.CommandMessage, new IntPtr(Program.PreferencesCommand), IntPtr.Zero); Thread.Sleep(500);
                Need(Find("WinCalendar.Flyout", pid) == flyout && IsWindowVisible(flyout) && Bounds(flyout).Height > calendarBounds.Height,
                    "Unified preferences enlarge the same flyout window");
                IntPtr back = BackButton(flyout); Need(back != IntPtr.Zero, "Preferences have a visible fixed Back action");
                SendMessage(back, 0xf5, IntPtr.Zero, IntPtr.Zero); Thread.Sleep(250);
                Need(Bounds(flyout).Size == calendarBounds.Size && IsWindowVisible(flyout), "Back restores the original calendar size");
                Click(date); Thread.Sleep(200);
                Need(!IsWindowVisible(flyout), "Final calendar is hidden");
                File.WriteAllText(file, "PASS installed weather date, native click, shared calendar, animated icon and acknowledgement\r\nDate=" + bounds + "\r\nIconAnimationAdvanced=" + animated + "\r\nStoppedAfterClick=True\r\nStableAfterOpenAndClose=True\r\nWeatherDetailsSameWindow=" + combined + "\r\nSystemWeatherVisible=" + (GetProp(date, "WinCalendar.SystemWeatherVisible").ToInt64() == 1) + "\r\nUnifiedPreferencesSameWindow=True\r\nFinalCalendarVisible=False");
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
            Rectangle r = Bounds(date); int height = GetProp(date, "WinCalendar.WeatherBaseHeight").ToInt32(); if (height == 0) height = r.Height;
            IntPtr point = new IntPtr(((r.Height - height / 2) << 16) | (height * 80 / 44));
            SendMessage(date, 0x201, new IntPtr(1), point); SendMessage(date, 0x202, IntPtr.Zero, point);
        }
        private static void ClickWeather(IntPtr date)
        {
            Rectangle r = Bounds(date); int height = GetProp(date, "WinCalendar.WeatherBaseHeight").ToInt32();
            IntPtr point = new IntPtr(((r.Height - height / 2) << 16) | (height * 240 / 44));
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
        private static IntPtr BackButton(IntPtr parent)
        {
            IntPtr result = IntPtr.Zero;
            EnumChildWindows(parent, delegate(IntPtr h, IntPtr unused)
            {
                if (!IsWindowVisible(h)) return true;
                StringBuilder kind = new StringBuilder(128); GetClassName(h, kind, kind.Capacity);
                if (!kind.ToString().ToUpperInvariant().Contains("BUTTON")) return true;
                StringBuilder text = new StringBuilder(64); GetWindowText(h, text, text.Capacity);
                if (text.ToString() == "Back" || text.ToString() == "\u041d\u0430\u0437\u0430\u0434") { result = h; return false; }
                return true;
            }, IntPtr.Zero); return result;
        }
        [StructLayout(LayoutKind.Sequential)] private struct NativeRect { internal int Left, Top, Right, Bottom; }
        [DllImport("user32.dll")] private static extern bool EnumWindows(Callback callback, IntPtr parameter);
        [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr h, Callback callback, IntPtr parameter);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr h, StringBuilder text, int count);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr h, StringBuilder text, int count);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetProp(IntPtr h, string name);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr h, out NativeRect r);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr h, int index);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr h, uint message, IntPtr wParam, IntPtr lParam);
    }
}
