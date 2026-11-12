using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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
            directory = Path.GetFullPath(directory);
            Directory.CreateDirectory(directory);
            int code = 0;
            try
            {
                Check(Program.GetLaunchCommand(null) == Program.OpenCalendarCommand, "Icon click opens Google Calendar");
                Check(Program.GetLaunchCommand("--background") == Program.RefreshCommand, "Background startup does not open the browser");
                Check(Program.GetLaunchCommand("--menu") == Program.MenuCommand, "Settings menu stays available separately");
                TestIcons(directory);
                TestDateOnlyHost(directory);
                foreach (string file in Directory.GetFiles(directory, "calendar-*.png")) File.Delete(file);
                Results.Add("PASS all date-only checks; no calendar window is used.");
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
                    Check(window.RefreshDate(false) && window.CurrentDate == date && window.Text.Contains(date.ToString("d MMMM yyyy", DateText.Russian)) && IsIconic(handle),
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
    }
}
