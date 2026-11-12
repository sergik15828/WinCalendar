using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace WinCalendar
{
    internal static class SelfTest
    {
        private static readonly List<string> Results = new List<string>();
        private static void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            Results.Add("PASS " + name);
        }

        internal static int Run(string directory)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            int result = 1;
            using (ApplicationContext context = new ApplicationContext())
            {
                EventHandler run = null;
                run = async delegate
                {
                    Application.Idle -= run;
                    result = await RunChecksAsync(directory);
                    context.ExitThread();
                };
                Application.Idle += run;
                Application.Run(context);
            }
            return result;
        }

        private static async Task<int> RunChecksAsync(string directory)
        {
            directory = Path.GetFullPath(directory);
            Directory.CreateDirectory(directory);
            int code = 0;
            try
            {
                Check(Program.GetLaunchCommand(null) == Program.OpenCalendarCommand, "Icon click requests the calendar popup");
                Check(Program.GetLaunchCommand("--background") == Program.RefreshCommand, "Background startup does not open the browser");
                Check(Program.GetLaunchCommand("--menu") == Program.MenuCommand, "Settings menu stays available separately");
                TestIcons(directory);
                TestDateOnlyHost(directory);
                TestCalendar(directory);
                await GoogleSelfTest.RunAsync(directory, Check);
                await FeaturesSelfTest.RunAsync(directory, Check);
                AgendaInteractionSelfTest.Run(directory, Check);
                RemainingMeetingsSelfTest.Run(directory, Check);
                WeatherPrototypeSelfTest.Run(directory, Check);
                await ReminderSelfTest.RunAsync(directory, Check);
                ReminderTimingSelfTest.Run(directory, Check);
                await WidgetUsabilitySelfTest.RunAsync(directory, Check);
                UnifiedPreferencesSelfTest.Run(directory, Check);
                await WeatherSelfTest.RunAsync(directory, Check);
                Results.Add("PASS all checks; popup uses a tool window, background startup stays hidden.");
            }
            catch (Exception error)
            {
                Results.Add("FAIL " + error.ToString()); code = 1;
            }
            File.WriteAllLines(Path.Combine(directory, "self-test.txt"), Results, new UTF8Encoding(false));
            return code;
        }

        private static void TestIcons(string directory)
        {
            DateTime fixture = new DateTime(2026, 11, 12);
            byte[] icon = CalendarIcon.Create(fixture);
            using (MemoryStream stream = new MemoryStream(icon))
            {
                IconBitmapDecoder decoder = new IconBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                Check(decoder.Frames.Count == 8, "ICO contains eight native DPI sizes");
                int[] expected = { 16, 20, 24, 32, 40, 48, 64, 128 };
                Check(decoder.Frames.Select(f => f.PixelWidth).OrderBy(s => s).SequenceEqual(expected), "All icon sizes decode in WPF");
            }
            Check(DateText.WeekdayAbbreviation(fixture) == "ЧТ", "Russian weekday label on icon");
            Check(!icon.SequenceEqual(CalendarIcon.Create(new DateTime(2026, 12, 12))), "Weekday label changes even if day number is the same");
            string path = Path.Combine(directory, "Calendar.test.ico");
            CalendarIcon.Save(path, icon);
            CalendarIcon.Save(path, CalendarIcon.Create(fixture.AddDays(1)));
            Check(File.ReadAllBytes(path).SequenceEqual(CalendarIcon.Create(fixture.AddDays(1))), "Atomic icon replacement changes date bytes");
            Check(!File.Exists(path + ".tmp"), "Atomic icon write leaves no temporary file");
            string shortcut = Path.Combine(directory, "WinCalendar.test.lnk");
            ShellIntegration.CreateAppShortcut(shortcut, path);
            Check(ShellIntegration.GetShortcutAppId(shortcut) == null, "Shortcut uses the native executable identity");
            ShellFileInfo info = new ShellFileInfo();
            SHGetFileInfo(shortcut, 0, ref info, (uint)Marshal.SizeOf(typeof(ShellFileInfo)), 0x100);
            Check(info.Icon != IntPtr.Zero, "Windows shell loads the shortcut icon");
            try
            {
                using (Icon nativeIcon = Icon.FromHandle(info.Icon))
                using (Bitmap nativeImage = nativeIcon.ToBitmap())
                {
                    int redPixels = 0, darkPixels = 0;
                    for (int y = 0; y < nativeImage.Height; y++)
                        for (int x = 0; x < nativeImage.Width; x++)
                        {
                            Color pixel = nativeImage.GetPixel(x, y);
                            if (pixel.A > 128 && pixel.R > 140 && pixel.R > pixel.G * 2 && pixel.R > pixel.B * 2) redPixels++;
                            if (pixel.A > 128 && pixel.R < 80 && pixel.G < 80 && pixel.B < 80) darkPixels++;
                        }
                    Check(redPixels > 8 && darkPixels > 8, "Shell-resolved shortcut has weekday strip and day ink instead of generic document");
                    nativeImage.Save(Path.Combine(directory, "shortcut-shell-icon.png"), ImageFormat.Png);
                }
            }
            finally { DestroyIcon(info.Icon); }
            using (Bitmap sheet = new Bitmap(600, 164))
            using (Graphics g = Graphics.FromImage(sheet))
            using (Font font = new Font("Segoe UI", 10))
            {
                g.Clear(Color.FromArgb(245, 245, 245));
                g.FillRectangle(Brushes.Black, 0, 82, 600, 82);
                int x = 16;
                foreach (int size in new[] { 16, 20, 24, 32, 40, 48, 64 })
                {
                    using (Bitmap bitmap = CalendarIcon.Draw(fixture, size))
                    {
                        g.DrawImageUnscaled(bitmap, x, 4);
                        g.DrawString(size.ToString(), font, Brushes.DimGray, x, 66);
                        g.DrawImageUnscaled(bitmap, x, 88);
                    }
                    x += size + 24;
                }
                sheet.Save(Path.Combine(directory, "icon-sizes.png"), ImageFormat.Png);
            }
            using (Bitmap bitmap = CalendarIcon.Draw(fixture, 256)) bitmap.Save(Path.Combine(directory, "icon-large.png"), ImageFormat.Png);
        }

        private static void TestDateOnlyHost(string directory)
        {
            DateTime clock = new DateTime(2026, 11, 12, 12, 0, 0);
            DateTaskbarWindow window = new DateTaskbarWindow(false, delegate { return clock; });
            Check(window.ShowInTaskbar && window.WindowState == FormWindowState.Minimized, "Default launch is a minimized taskbar button");
            window.ShowInTaskbar = false;
            try
            {
                window.Show();
                IntPtr handle = window.Handle;
                Check(IsIconic(handle), "Native taskbar host starts minimized");
                Check(!window.ShowInTaskbar && GetWindow(handle, 4) != IntPtr.Zero, "Background host has a hidden owner and no running-app taskbar button");
                Check(window.Controls.Count == 0, "No calendar content exists");
                Check(!window.Calendar.Visible, "Background startup does not show the calendar popup");
                Check(window.Text.Contains("12 ноября 2026"), "Taskbar title contains full date");

                SendMessage(handle, 0x0112, new IntPtr(0xf120), IntPtr.Zero);
                Pump();
                Check(IsIconic(handle), "Taskbar restore command does not open a window");
                ShowWindow(handle, 9);
                Pump();
                Check(IsIconic(handle), "Direct native restore stays minimized");
                window.WindowState = FormWindowState.Normal;
                Pump();
                Check(IsIconic(handle), "Managed restore stays minimized");
                object sameDayIcon = window.Icon;
                clock = clock.AddHours(2);
                Check(!window.RefreshDate(false) && Object.ReferenceEquals(sameDayIcon, window.Icon), "Same-day check preserves icon");
                clock = new DateTime(2026, 11, 13, 0, 0, 1);
                Check(Program.PostCommand(Program.RefreshCommand, System.Diagnostics.Process.GetCurrentProcess().Id), "Command locates the hidden background host");
                Pump();
                Check(window.CurrentDate == clock.Date && !Object.ReferenceEquals(sameDayIcon, window.Icon) && IsIconic(handle), "Repeated launch refreshes date without opening a window");
                foreach (DateTime date in new[] { new DateTime(2026, 12, 31), new DateTime(2027, 1, 1), new DateTime(2024, 2, 28), new DateTime(2024, 2, 29), new DateTime(2024, 3, 1) })
                {
                    clock = date;
                    Check(window.RefreshDate(false) && window.CurrentDate == date && window.Text.Contains(date.ToString("d MMMM yyyy", DateText.Culture)) && IsIconic(handle),
                        "Local date and icon update without a window: " + date.ToString("yyyy-MM-dd"));
                }
                Check(ShellIntegration.GetWindowAppId(handle) == null, "Window keeps the native executable identity");
                window.Close();
                Check(!window.Visible, "Taskbar Close exits the indicator");
            }
            finally
            {
                if (!window.IsDisposed) window.Close();
            }
        }

        private static void TestCalendar(string directory)
        {
            foreach (DateTime month in new[] { new DateTime(2024, 2, 1), new DateTime(2026, 2, 1), new DateTime(2026, 11, 1), new DateTime(2026, 12, 1) })
            {
                CalendarState state = new CalendarState(month);
                DateTime[] days = state.Days();
                Check(days.Length == 42 && days[0].DayOfWeek == DayOfWeek.Monday && days[41].DayOfWeek == DayOfWeek.Sunday,
                    "Monday-first six-week calendar: " + month.ToString("yyyy-MM"));
                Check(days.Count(d => d.Year == month.Year && d.Month == month.Month) == DateTime.DaysInMonth(month.Year, month.Month),
                    "All month days present once: " + month.ToString("yyyy-MM"));
            }
            CalendarState leap = new CalendarState(new DateTime(2024, 1, 31)); leap.MoveMonth(1);
            Check(leap.Selected == new DateTime(2024, 2, 29), "Month navigation clamps to leap day");
            leap.MoveMonth(10);
            Check(leap.Month == new DateTime(2024, 12, 1), "Month navigation crosses calendar grid boundaries");
            leap.GoToToday(); leap.UpdateToday(new DateTime(2024, 2, 1));
            Check(leap.Selected == new DateTime(2024, 2, 1), "Open calendar follows today at midnight");
            leap.Select(new DateTime(2025, 5, 17)); leap.UpdateToday(new DateTime(2024, 2, 2));
            Check(leap.Selected == new DateTime(2025, 5, 17), "Day rollover preserves a browsed date");

            Rectangle[] workAreas = { new Rectangle(0, 0, 1920, 1020), new Rectangle(-1920, 0, 1920, 1040),
                new Rectangle(40, 0, 1880, 1080), new Rectangle(0, 48, 1920, 1032), new Rectangle(0, 0, 320, 400) };
            foreach (Rectangle work in workAreas)
                foreach (Point anchor in new[] { new Point(work.Left, work.Bottom + 20), new Point(work.Right, work.Top - 10), new Point(work.Left - 20, work.Top + work.Height / 2) })
                    Check(work.Contains(CalendarPopup.Place(new Size(344, 468), anchor, work)), "Popup stays within monitor working area: " + work);

            DateTime fixture = new DateTime(2026, 11, 12);
            int googleOpens = 0;
            using (CalendarPopup popup = new CalendarPopup(delegate { return fixture; }, delegate { googleOpens++; }))
            {
                Rectangle work = Screen.PrimaryScreen.WorkingArea;
                Point anchor = new Point(work.Right - 100, work.Bottom + 8);
                popup.ShowAt(anchor, work); Pump();
                Check(popup.Visible, "Calendar opens in one native flyout");
                long style = GetWindowLongPtr(popup.Handle, -20).ToInt64();
                Check((style & 0x80) != 0 && (style & 0x40000) == 0, "Calendar popup is a tool window without an app taskbar button");
                Check(popup.Panel.Controls.OfType<CalendarDayButton>().Count() == 42, "Calendar renders 42 date buttons");
                Check(popup.Panel.Controls.OfType<CalendarDayButton>().All(b => !String.IsNullOrEmpty(b.AccessibleName)), "Date buttons have full accessible labels");
                popup.Panel.NextButton.PerformClick();
                Check(popup.Panel.State.Month == new DateTime(2026, 12, 1), "Next month button changes visible month");
                popup.Panel.PreviousButton.PerformClick();
                Check(popup.Panel.State.Month == new DateTime(2026, 11, 1), "Previous month button changes visible month");
                popup.Panel.Controls.OfType<CalendarDayButton>().Single(b => (DateTime)b.Tag == new DateTime(2026, 11, 19)).PerformClick();
                Check(popup.Panel.State.Selected == new DateTime(2026, 11, 19) && popup.Panel.State.Today == fixture, "Selecting a date keeps today's date unchanged");
                popup.Panel.TodayButton.PerformClick();
                Check(popup.Panel.State.Selected == fixture, "Today button returns to current date");
                popup.Panel.FocusSelectedDay();
                CalendarKey(popup.Panel, Keys.Right);
                Check(popup.Panel.State.Selected == fixture.AddDays(1), "Keyboard arrows select another day");
                CalendarKey(popup.Panel, Keys.PageDown);
                Check(popup.Panel.State.Month == new DateTime(2026, 12, 1), "Page Down changes calendar month");
                CalendarKey(popup.Panel, Keys.Home);
                Check(popup.Panel.State.Selected == fixture, "Home returns to today");
                foreach (int dpi in new[] { 96, 120, 144 })
                foreach (bool dark in new[] { false, true })
                {
                    popup.Panel.SetDpi(dpi);
                    popup.ViewWindow.ClientSize = popup.Panel.NaturalSize; popup.Panel.ApplyTheme(dark); popup.Panel.Refresh(); Pump();
                    using (Bitmap image = new Bitmap(popup.Panel.Width, popup.Panel.Height))
                    {
                        popup.Panel.DrawToBitmap(image, popup.Panel.ClientRectangle);
                        image.Save(Path.Combine(directory, "calendar-" + (dark ? "dark" : "light") + (dpi == 96 ? "" : "-" + dpi) + ".png"), ImageFormat.Png);
                    }
                    Check(popup.Panel.Controls.Cast<Control>().All(c => popup.Panel.ClientRectangle.Contains(c.Bounds)), "Calendar controls fit in " + (dark ? "dark" : "light") + " theme at " + dpi + " DPI");
                    Check(popup.Panel.Size == new Size(CalendarPanel.LogicalWidth * dpi / 96, CalendarPanel.LogicalHeight * dpi / 96), "Calendar geometry follows " + dpi + " DPI");
                }
                popup.Panel.ShowSettings(true); popup.Panel.GoogleButton.PerformClick(); Pump();
                Check(googleOpens == 1 && !popup.Visible, "Google Calendar button opens browser action and closes popup");
                popup.Toggle(anchor); Pump();
                Check(popup.Visible, "Calendar can reopen after dismissal");
                popup.Toggle(anchor); Pump();
                Check(!popup.Visible, "Second icon command closes calendar");
                popup.ShowAt(anchor, work); Pump();
                CalendarKey(popup.Panel, Keys.Escape); Pump();
                Check(!popup.Visible, "Escape closes calendar");
            }
        }

        private static void CalendarKey(CalendarPanel panel, Keys key)
        {
            MethodInfo method = typeof(CalendarPanel).GetMethod("ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic);
            object[] arguments = { Message.Create(panel.Handle, 0x100, new IntPtr((int)key), IntPtr.Zero), key };
            Check((bool)method.Invoke(panel, arguments), "Calendar handles " + key);
        }

        private static void Pump()
        {
            Application.DoEvents();
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ShellFileInfo
        {
            public IntPtr Icon;
            public int Index;
            public uint Attributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string Display;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string Type;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(string path, uint attributes, ref ShellFileInfo info, uint size, uint flags);
        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr icon);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr window, int command);
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr window, uint command);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
    }
}
