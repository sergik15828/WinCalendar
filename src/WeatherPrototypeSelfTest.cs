using System;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace WinCalendar
{
    internal static class WeatherPrototypeSelfTest
    {
        internal static void Run(string directory, Action<bool, string> check)
        {
            Rectangle bar = new Rectangle(0, 1032, 1920, 48), left = new Rectangle(8, 1032, 140, 48), right = new Rectangle(1600, 1032, 140, 48);
            Rectangle date = WeatherPlacement.Place(bar, left, new[] { left }, 160, 44);
            check(date.Left > left.Right && bar.Contains(date), "Weather date fits beside a left weather button without covering it");
            date = WeatherPlacement.Place(bar, right, new[] { right }, 160, 44);
            check(date.Right < right.Left && bar.Contains(date), "Weather date fits beside a right weather button toward free space");
            check(WeatherPlacement.Place(bar, left, new[] { left, new Rectangle(148, 1032, 1772, 48) }, 44).IsEmpty,
                "Crowded taskbar suppresses the date instead of covering an application button");
            check(WeatherPlacement.Place(bar, Rectangle.Empty, new Rectangle[0], 44).IsEmpty, "Missing weather hides prototype instead of guessing a position");
            Rectangle shifted = new Rectangle(-1920, -48, 1920, 48), weather = new Rectangle(-1912, -48, 140, 48);
            check(shifted.Contains(WeatherPlacement.Place(shifted, weather, new[] { weather }, 44)), "Weather placement supports negative display coordinates");
            check(Program.GetLaunchCommand("--weather-on") == Program.WeatherOnCommand && Program.GetLaunchCommand("--weather-off") == Program.WeatherOffCommand,
                "Weather prototype has explicit reversible launch commands");
            check(WeatherPlacement.IsWeather("AugmentedEntryPointButton") && WeatherPlacement.IsWeather("DynamicContent2")
                && !WeatherPlacement.IsWeather("TaskbarItem") && !WeatherPlacement.IsWeather("Appid:Microsoft.BingWeather")
                && !WeatherPlacement.IsWeather("Weather forecast") && !WeatherPlacement.IsWeather("Погода") && !WeatherPlacement.IsWeather(null),
                "Weather locator uses shell IDs and rejects app IDs or titles containing weather");
            Rectangle saved = TaskbarAnchor.Prototype;
            try
            {
                TaskbarAnchor.Prototype = new Rectangle(200, 1034, 44, 44);
                check(TaskbarAnchor.Current == TaskbarAnchor.Prototype, "Calendar and reminders share the weather date anchor");
            }
            finally { TaskbarAnchor.Prototype = saved; }
            foreach (int dpi in new[] { 96, 120, 144 })
            foreach (bool dark in new[] { false, true })
            foreach (bool hover in new[] { false, true })
            {
                using (Bitmap frame = WeatherWidgetRenderer.Draw(new DateTime(2026, 11, 12), 160 * dpi / 96, 44 * dpi / 96, dark, hover))
                using (Bitmap image = new Bitmap(frame.Width, frame.Height))
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.Clear(dark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(243, 243, 243));
                    g.DrawImageUnscaled(frame, 0, 0);
                    image.Save(Path.Combine(directory, "weather-widget-" + (dark ? "dark" : "light") + (hover ? "-hover-" : "-rest-") + dpi + ".png"), ImageFormat.Png);
                }
            }
            TaskbarAnchor.Prototype = saved;
            WeatherPlacement native = WeatherPlacement.Find();
            File.WriteAllText(Path.Combine(directory, "weather-placement.txt"), native == null ? "WeatherPlacementFound=False" : "WeatherPlacementFound=True\r\nTaskbar=" + native.Bar + "\r\nDate=" + native.DateBounds + "\r\nExposed=" + WeatherPlacement.TaskbarExposed(native));
        }
    }
}
