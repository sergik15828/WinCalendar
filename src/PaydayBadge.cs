using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace WinCalendar
{
    internal sealed class PaydayBadge : Control
    {
        private int days;
        private CalendarPalette palette;
        internal void UpdateValue(DateTime today, AppPreferences preferences, CalendarPalette colors)
        {
            DateTime next = PaydaySchedule.Next(today, preferences.PaydayFrequency, preferences.PaydayFirst, preferences.PaydaySecond);
            days = (next - today.Date).Days; palette = colors;
            AccessibleName = (days == 0 ? UiText.Get("Зарплата") + " " + UiText.Get("Сегодня") : PaydaySchedule.DaysLabel(days) + " " + UiText.Get("до зарплаты")) + ", " + next.ToString("d MMMM", DateText.Culture);
            Invalidate();
        }
        internal PaydayBadge()
        { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); AccessibleRole = AccessibleRole.StaticText; TabStop = false; }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (palette == null) return;
            Graphics g = e.Graphics; g.Clear(palette.Surface); g.ScaleTransform(Width / 112f, Height / 44f);
            g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            Color gold = SystemInformation.HighContrast ? SystemColors.Highlight : Color.FromArgb(232, 178, 66);
            Color edge = SystemInformation.HighContrast ? SystemColors.WindowText : Color.FromArgb(155, 105, 24);
            using (GraphicsPath bag = new GraphicsPath())
            {
                bag.AddBezier(10, 15, 3, 20, 1, 27, 5, 33); bag.AddBezier(5, 33, 10, 38, 23, 37, 27, 32);
                bag.AddBezier(27, 32, 30, 26, 25, 19, 21, 15); bag.CloseFigure();
                using (Brush fill = new SolidBrush(gold)) g.FillPath(fill, bag);
                using (Pen pen = new Pen(edge, 1.1f)) g.DrawPath(pen, bag);
            }
            using (GraphicsPath top = new GraphicsPath())
            {
                top.AddPolygon(new[] { new PointF(9, 7), new PointF(15, 9), new PointF(23, 6), new PointF(20, 14), new PointF(11, 14) });
                using (Brush fill = new SolidBrush(gold)) g.FillPath(fill, top);
                using (Pen pen = new Pen(edge, 1.1f)) g.DrawPath(pen, top);
            }
            using (Pen pen = new Pen(edge, 1.3f))
            {
                pen.StartCap = pen.EndCap = LineCap.Round; g.DrawLine(pen, 10, 15, 22, 15);
                g.DrawBezier(pen, 20, 22, 10, 17, 10, 26, 16, 26); g.DrawBezier(pen, 16, 26, 23, 26, 21, 33, 11, 30); g.DrawLine(pen, 16, 19, 16, 33);
            }
            using (Font main = new Font("Segoe UI Semibold", 12, FontStyle.Regular, GraphicsUnit.Pixel))
            using (Font small = new Font("Segoe UI", 10.5f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (Brush ink = new SolidBrush(palette.Ink))
            using (Brush muted = new SolidBrush(palette.Secondary))
            using (StringFormat format = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap })
            { g.DrawString(PaydaySchedule.DaysLabel(days), main, ink, new PointF(36, 8), format); g.DrawString(days == 0 ? UiText.Get("зарплата") : UiText.Get("до зарплаты"), small, muted, new PointF(36, 24), format); }
        }
    }
}
