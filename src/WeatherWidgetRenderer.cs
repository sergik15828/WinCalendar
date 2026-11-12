using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WinCalendar
{
    internal static class WeatherWidgetRenderer
    {
        internal const int Width = 160, Height = 44;
        internal const int CombinedWidth = 320;
        internal const int ReminderHeadroom = 28;
        internal static bool DarkTaskbar()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    return key == null || !Object.Equals(key.GetValue("SystemUsesLightTheme"), 1);
            }
            catch (UnauthorizedAccessException) { return true; }
            catch (System.Security.SecurityException) { return true; }
        }
        internal static Bitmap Draw(DateTime date, int width, int height, bool dark, bool hover, bool reminder = false, double phase = 0, WeatherView weather = null, bool showWeather = false)
        {
            int headroom = reminder && TaskbarAnchor.ReminderMotion ? (int)Math.Round(ReminderHeadroom * height / (double)Height) : 0;
            Bitmap bitmap = new Bitmap(width, height + headroom, PixelFormat.Format32bppPArgb);
            CalendarPalette palette = CalendarPalette.Create(dark);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.TranslateTransform(0, headroom);
                int logicalWidth = showWeather ? CombinedWidth : Width;
                g.ScaleTransform(width / (float)logicalWidth, height / (float)Height);
                g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                // Alpha 1 keeps the whole button clickable without a visible panel.
                using (GraphicsPath shape = Rounded(1, 1, logicalWidth - 2, Height - 2, 6))
                using (Brush fill = new SolidBrush(SystemInformation.HighContrast ? SystemColors.Window : Color.FromArgb(hover ? 22 : 1, palette.Ink)))
                    g.FillPath(fill, shape);
                if (reminder && TaskbarAnchor.ReminderMotion)
                {
                    int amplitude = TaskbarAnchor.ReminderStrength == 0 ? 16 : TaskbarAnchor.ReminderStrength == 2 ? 30 : 24;
                    double hop = phase < 0.28 ? Arc(phase / 0.28) : phase < 0.47 ? 0.42 * Arc((phase - 0.28) / 0.19)
                        : phase < 0.59 ? 0.14 * Arc((phase - 0.47) / 0.12) : 0;
                    float lift = (float)(amplitude * hop);
                    GraphicsState iconState = g.Save(); g.TranslateTransform(0, -lift); DrawCalendar(g, palette); g.Restore(iconState);
                }
                else DrawCalendar(g, palette);
                if (reminder && !TaskbarAnchor.ReminderMotion)
                    using (Pen attention = new Pen(palette.Accent, 2)) g.DrawRectangle(attention, 10, 5, 29, 33);
                using (Brush ink = new SolidBrush(palette.Ink))
                {
                    using (Font font = DateFont())
                    using (StringFormat format = Format()) g.DrawString(date.ToString("d MMMM", DateText.Culture), font, ink, new RectangleF(46, 6, Width - 54, 20), format);
                }
                using (Brush secondary = new SolidBrush(palette.Secondary))
                using (Font font = new Font("Segoe UI", 12, FontStyle.Regular, GraphicsUnit.Pixel))
                using (StringFormat format = Format()) g.DrawString(date.ToString("dddd", DateText.Culture), font, secondary, new RectangleF(46, 22, Width - 54, 16), format);
                if (showWeather)
                {
                    weather = weather ?? new WeatherView();
                    using (Pen divider = new Pen(Color.FromArgb(45, palette.Ink), 1)) g.DrawLine(divider, 166, 11, 166, 33);
                    WeatherSymbols.Draw(g, new RectangleF(179, 9, 30, 30), weather.Usable ? weather.Snapshot.Code : 3, !weather.Usable || weather.Snapshot.IsDay, palette);
                    using (Brush ink = new SolidBrush(weather.Stale ? palette.Secondary : palette.Ink))
                    using (Font font = DateFont())
                    using (StringFormat format = Format()) g.DrawString(weather.Location == null ? UiText.Get("Погода") : weather.Temperature, font, ink, new RectangleF(218, 6, 96, 20), format);
                    using (Brush muted = new SolidBrush(palette.Secondary))
                    using (Font font = new Font("Segoe UI", 12, FontStyle.Regular, GraphicsUnit.Pixel))
                    using (StringFormat format = Format()) g.DrawString(weather.Status, font, muted, new RectangleF(218, 22, 96, 16), format);
                }
            }
            return bitmap;
        }
        internal static Font DateFont() { return new Font("Segoe UI Semibold", 14, FontStyle.Regular, GraphicsUnit.Pixel); }
        private static double Arc(double time) { return 4 * time * (1 - time); }
        private static void DrawCalendar(Graphics g, CalendarPalette palette)
        {
            bool contrast = SystemInformation.HighContrast;
            Color paper = contrast ? SystemColors.Window : Color.FromArgb(255, 252, 250);
            Color header = contrast ? SystemColors.Highlight : Color.FromArgb(196, 43, 54);
            using (GraphicsPath page = Rounded(12, 9, 25, 27, 4))
            {
                using (Brush fill = new SolidBrush(paper)) g.FillPath(fill, page);
                using (Pen edge = new Pen(contrast ? SystemColors.WindowText : Color.FromArgb(85, palette.Ink), 0.75f)) g.DrawPath(edge, page);
                GraphicsState saved = g.Save();
                g.SetClip(page);
                using (Brush fill = new SolidBrush(header)) g.FillRectangle(fill, 12, 9, 25, 8);
                g.Restore(saved);
            }
            using (Pen binding = new Pen(contrast ? SystemColors.WindowText : Color.FromArgb(225, 227, 232), 1.8f))
            {
                binding.StartCap = binding.EndCap = LineCap.Round;
                g.DrawLine(binding, 19, 7, 19, 12); g.DrawLine(binding, 30, 7, 30, 12);
            }
            for (int row = 0; row < 2; row++)
            for (int column = 0; column < 3; column++)
                using (Brush cell = new SolidBrush(contrast ? SystemColors.WindowText : row == 0 && column == 1 ? header : Color.FromArgb(152, 155, 164)))
                    g.FillRectangle(cell, 17 + column * 5, 22 + row * 6, 3, 3);
        }
        private static StringFormat Format()
        { return new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter, LineAlignment = StringAlignment.Center }; }
        private static GraphicsPath Rounded(float x, float y, float width, float height, float radius)
        {
            GraphicsPath path = new GraphicsPath(); float d = radius * 2;
            path.AddArc(x, y, d, d, 180, 90); path.AddArc(x + width - d, y, d, d, 270, 90);
            path.AddArc(x + width - d, y + height - d, d, d, 0, 90); path.AddArc(x, y + height - d, d, d, 90, 90); path.CloseFigure(); return path;
        }
    }
}
