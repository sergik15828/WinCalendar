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
        internal static Bitmap Draw(DateTime date, int width, int height, bool dark, bool hover)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            CalendarPalette palette = CalendarPalette.Create(dark);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.ScaleTransform(width / (float)Width, height / (float)Height);
                g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                // Alpha 1 keeps the whole button clickable without a visible panel.
                using (GraphicsPath shape = Rounded(1, 1, Width - 2, Height - 2, 6))
                using (Brush fill = new SolidBrush(SystemInformation.HighContrast ? SystemColors.Window : Color.FromArgb(hover ? 22 : 1, palette.Ink)))
                    g.FillPath(fill, shape);
                DrawCalendar(g, palette);
                using (Brush ink = new SolidBrush(palette.Ink))
                {
                    using (Font font = new Font("Segoe UI Semibold", 14, FontStyle.Regular, GraphicsUnit.Pixel))
                    using (StringFormat format = Format()) g.DrawString(date.ToString("d MMMM", DateText.Russian), font, ink, new RectangleF(46, 6, Width - 54, 20), format);
                }
                using (Brush secondary = new SolidBrush(palette.Secondary))
                using (Font font = new Font("Segoe UI", 12, FontStyle.Regular, GraphicsUnit.Pixel))
                using (StringFormat format = Format()) g.DrawString(date.ToString("dddd", DateText.Russian), font, secondary, new RectangleF(46, 22, Width - 54, 16), format);
            }
            return bitmap;
        }
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
