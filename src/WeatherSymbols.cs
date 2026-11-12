using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinCalendar
{
    internal static class WeatherSymbols
    {
        internal static string Description(int code)
        {
            string text = code <= 1 ? "Ясно" : code == 2 ? "Переменная облачность" : code == 3 ? "Облачно" : code == 45 || code == 48 ? "Туман"
                : code >= 95 ? "Гроза" : (code >= 71 && code <= 77) || code == 85 || code == 86 ? "Снег" : code >= 51 && code <= 82 ? "Дождь" : "Погода";
            return UiText.Get(text);
        }
        internal static void Draw(Graphics g, RectangleF area, int code, bool day, CalendarPalette palette)
        {
            GraphicsState saved = g.Save(); g.TranslateTransform(area.X, area.Y); g.ScaleTransform(area.Width / 32, area.Height / 32);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool contrast = SystemInformation.HighContrast;
            bool dark = palette.Surface.GetBrightness() < 0.5f;
            Color sun = contrast ? palette.Ink : Color.FromArgb(255, 176, 32);
            Color sunEdge = contrast ? palette.Ink : dark ? Color.FromArgb(255, 190, 65) : Color.FromArgb(191, 104, 13);
            Color cloud = contrast ? palette.Ink : dark ? Color.FromArgb(102, 170, 255) : Color.FromArgb(60, 145, 230);
            Color cloudEdge = contrast ? palette.Ink : dark ? Color.FromArgb(135, 193, 255) : Color.FromArgb(33, 102, 180);
            if (code <= 2)
            {
                if (day)
                {
                    using (Pen ray = new Pen(sunEdge, 2))
                    { ray.StartCap = ray.EndCap = LineCap.Round; for (int i = 0; i < 8; i++) { double angle = i * Math.PI / 4; g.DrawLine(ray, 13 + (float)Math.Cos(angle) * 8, 13 + (float)Math.Sin(angle) * 8, 13 + (float)Math.Cos(angle) * 11, 13 + (float)Math.Sin(angle) * 11); } }
                    using (Brush fill = new SolidBrush(sun)) g.FillEllipse(fill, 7, 7, 12, 12);
                    using (Pen outline = new Pen(sunEdge, 1)) g.DrawEllipse(outline, 7, 7, 12, 12);
                }
                else
                    using (GraphicsPath moon = new GraphicsPath())
                    { moon.AddBezier(19, 3, 7, 1, 2, 18, 13, 23); moon.AddBezier(13, 23, 20, 26, 25, 20, 25, 17); moon.AddBezier(25, 17, 15, 20, 11, 10, 19, 3); using (Brush fill = new SolidBrush(contrast ? palette.Ink : dark ? Color.FromArgb(255, 225, 160) : Color.FromArgb(213, 149, 34))) g.FillPath(fill, moon); using (Pen edge = new Pen(sunEdge, 1)) g.DrawPath(edge, moon); }
            }
            if (code >= 2)
                using (GraphicsPath shape = new GraphicsPath())
                {
                    shape.AddBezier(7, 24, 0, 24, 0, 14, 7, 14); shape.AddBezier(7, 14, 8, 4, 22, 4, 24, 15);
                    shape.AddBezier(24, 15, 33, 14, 34, 25, 26, 25); shape.AddLine(26, 25, 7, 24);
                    using (Brush fill = new SolidBrush(cloud)) g.FillPath(fill, shape);
                    using (Pen edge = new Pen(cloudEdge, 1)) g.DrawPath(edge, shape);
                }
            if (code == 45 || code == 48)
                using (Pen mist = new Pen(cloudEdge, 1.8f)) { g.DrawLine(mist, 5, 28, 27, 28); g.DrawLine(mist, 9, 31, 24, 31); }
            else if (code >= 95)
                using (Brush fill = new SolidBrush(sun)) g.FillPolygon(fill, new[] { new Point(16, 19), new Point(11, 27), new Point(16, 27), new Point(14, 32), new Point(23, 23), new Point(18, 23), new Point(21, 19) });
            else if ((code >= 71 && code <= 77) || code == 85 || code == 86)
                using (Pen snow = new Pen(contrast ? palette.Ink : dark ? Color.FromArgb(102, 208, 255) : Color.FromArgb(8, 124, 185), 1.7f))
                    for (int x = 9; x <= 25; x += 8) { g.DrawLine(snow, x - 2, 29, x + 2, 29); g.DrawLine(snow, x, 27, x, 31); }
            else if (code >= 51 && code <= 82)
                using (Pen rain = new Pen(contrast ? palette.Ink : dark ? Color.FromArgb(93, 181, 255) : Color.FromArgb(0, 111, 214), 2))
                { rain.StartCap = rain.EndCap = LineCap.Round; for (int x = 9; x <= 25; x += 8) g.DrawLine(rain, x, 27, x - 2, 31); }
            g.Restore(saved);
        }
    }
}
