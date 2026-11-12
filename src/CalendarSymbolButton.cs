using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinCalendar
{
    internal sealed class CalendarSymbolButton : Button
    {
        internal CalendarPalette Palette;
        internal bool IsClose, HasAttention;
        private bool hover;
        internal CalendarSymbolButton()
        {
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
        protected override void OnMouseEnter(EventArgs args) { hover = true; Invalidate(); base.OnMouseEnter(args); }
        protected override void OnMouseLeave(EventArgs args) { hover = false; Invalidate(); base.OnMouseLeave(args); }
        protected override void OnPaint(PaintEventArgs args)
        {
            if (Palette == null) return;
            args.Graphics.Clear(hover ? Palette.Hover : Palette.Surface);
            args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = Width / 32f, x = Width / 2f, y = Height / 2f;
            using (Pen pen = new Pen(Palette.Ink, 1.4f * scale))
            {
                if (IsClose)
                {
                    args.Graphics.DrawLine(pen, x - 5 * scale, y - 5 * scale, x + 5 * scale, y + 5 * scale);
                    args.Graphics.DrawLine(pen, x + 5 * scale, y - 5 * scale, x - 5 * scale, y + 5 * scale);
                }
                else
                {
                    PointF[] teeth = new PointF[32];
                    for (int i = 0; i < teeth.Length; i++)
                    {
                        double angle = (i / 4 * 45 + new[] { -16, -10, 10, 16 }[i % 4]) * Math.PI / 180;
                        float radius = (i % 4 == 0 || i % 4 == 3 ? 6 : 8) * scale;
                        teeth[i] = new PointF(x + (float)Math.Cos(angle) * radius, y + (float)Math.Sin(angle) * radius);
                    }
                    args.Graphics.DrawPolygon(pen, teeth);
                    args.Graphics.DrawEllipse(pen, x - 2.6f * scale, y - 2.6f * scale, 5.2f * scale, 5.2f * scale);
                }
            }
            if (HasAttention && !IsClose)
                using (Brush brush = new SolidBrush(Palette.Accent)) args.Graphics.FillEllipse(brush, Width - 7 * scale, 3 * scale, 4 * scale, 4 * scale);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(args.Graphics, Rectangle.Inflate(ClientRectangle, -3, -3), Palette.Ink, Palette.Surface);
        }
    }
}
