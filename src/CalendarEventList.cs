using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WinCalendar
{
    internal sealed class CalendarEventList : ListBox
    {
        private CalendarPalette palette;
        private int dpi = 96;
        private readonly ToolTip tooltip = new ToolTip();
        internal event Action<string> OpenRequested;

        internal CalendarEventList()
        {
            DrawMode = DrawMode.OwnerDrawVariable; BorderStyle = BorderStyle.None;
            IntegralHeight = false; AccessibleName = "Встречи выбранного дня";
            MouseClick += delegate(object sender, MouseEventArgs args)
            {
                int index = IndexFromPoint(args.Location);
                if (args.Button == MouseButtons.Left && index >= 0) Open(index);
            };
            MouseMove += delegate(object sender, MouseEventArgs args)
            {
                int index = IndexFromPoint(args.Location);
                string text = index < 0 ? "" : Items[index].ToString();
                if (tooltip.GetToolTip(this) != text) tooltip.SetToolTip(this, text);
            };
        }

        internal void SetEvents(IEnumerable<CalendarEvent> events, DateTime day, TimeZoneInfo zone, CalendarPalette colors, int scaleDpi)
        {
            palette = colors; dpi = scaleDpi;
            BackColor = palette.Surface; ForeColor = palette.Ink;
            BeginUpdate(); Items.Clear();
            foreach (CalendarEvent item in events.OrderBy(e => e.SortOrder))
                Items.Add(new EventRow { Event = item, Time = item.TimeLabel(day, zone) });
            EndUpdate(); Invalidate();
        }

        private int Pixels(int value) { return (int)Math.Round(value * dpi / 96.0); }
        protected override void OnMeasureItem(MeasureItemEventArgs args) { args.ItemHeight = Pixels(54); }
        protected override void OnDrawItem(DrawItemEventArgs args)
        {
            if (args.Index < 0 || palette == null) return;
            EventRow row = (EventRow)Items[args.Index];
            bool selected = (args.State & DrawItemState.Selected) != 0;
            Color background = selected ? palette.Hover : palette.Surface;
            using (Brush brush = new SolidBrush(background)) args.Graphics.FillRectangle(brush, args.Bounds);
            Rectangle time = new Rectangle(args.Bounds.X + Pixels(6), args.Bounds.Y + Pixels(2), args.Bounds.Width - Pixels(12), Pixels(20));
            Rectangle title = new Rectangle(time.X, args.Bounds.Y + Pixels(22), time.Width, Pixels(28));
            TextRenderer.DrawText(args.Graphics, row.Time, Font, time, palette.Secondary, TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(args.Graphics, row.Event.Title, Font, title, row.Event.Url == null ? palette.Secondary : palette.Ink,
                TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            if ((args.State & DrawItemState.Focus) != 0) ControlPaint.DrawFocusRectangle(args.Graphics, Rectangle.Inflate(args.Bounds, -1, -1), palette.Ink, background);
        }

        protected override void OnKeyDown(KeyEventArgs args)
        {
            if (args.KeyCode == Keys.Enter && SelectedIndex >= 0) { Open(SelectedIndex); args.Handled = args.SuppressKeyPress = true; }
            base.OnKeyDown(args);
        }

        internal void Open(int index)
        {
            string url = CalendarPage.SafeGoogleLink(((EventRow)Items[index]).Event.Url);
            if (url != null && OpenRequested != null) OpenRequested(url);
        }

        protected override void Dispose(bool disposing) { if (disposing) tooltip.Dispose(); base.Dispose(disposing); }
        private sealed class EventRow
        {
            internal CalendarEvent Event;
            internal string Time;
            public override string ToString() { return Time + ", " + Event.Title; }
        }
    }
}
