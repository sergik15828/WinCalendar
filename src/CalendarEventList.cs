using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace WinCalendar
{
    internal sealed class CalendarEventList : ListBox
    {
        private CalendarPalette palette;
        private int dpi = 96;
        private readonly ToolTip tooltip = new ToolTip();
        private string contentKey;
        private Font titleFont, detailFont;
        private int hovered = -1;
        internal event Action<string> OpenRequested;

        internal CalendarEventList()
        {
            DrawMode = DrawMode.OwnerDrawVariable; BorderStyle = BorderStyle.None;
            IntegralHeight = false; AccessibleName = "Встречи выбранного дня";
            MouseClick += delegate(object sender, MouseEventArgs args)
            {
                int index = IndexFromPoint(args.Location);
                Cursor = index >= 0 && ((EventRow)Items[index]).Event.Url != null ? Cursors.Hand : Cursors.Default;
                if (args.Button == MouseButtons.Left && index >= 0 && Rectangle.Inflate(GetItemRectangle(index), -1, -4).Contains(args.Location)) Open(index);
            };
            MouseMove += delegate(object sender, MouseEventArgs args)
            {
                int index = IndexFromPoint(args.Location);
                Cursor = index >= 0 && ((EventRow)Items[index]).Event.Url != null ? Cursors.Hand : Cursors.Default;
                if (hovered != index) { hovered = index; Invalidate(); }
                string text = index < 0 ? "" : Items[index].ToString();
                if (tooltip.GetToolTip(this) != text) tooltip.SetToolTip(this, text);
            };
        }

        internal void SetEvents(IEnumerable<CalendarEvent> events, DateTime day, TimeZoneInfo zone, CalendarPalette colors, int scaleDpi)
        {
            EventRow[] rows = events.OrderBy(e => e.SortOrder).Select(item => new EventRow { Event = item,
                Time = item.TimeLabel(day, zone), Source = String.IsNullOrEmpty(item.CalendarName) ? "Google Calendar" : item.CalendarName }).ToArray();
            string key = day.ToString("yyyyMMdd") + String.Join("\u001e", rows.Select(r => r.Event.CalendarId + "\u001f" + r.Event.Id + r.Event.Title + r.Event.Url + r.Time + r.Source));
            bool unchanged = contentKey == key && dpi == scaleDpi;
            palette = colors; dpi = scaleDpi;
            BackColor = palette.Surface; ForeColor = palette.Ink;
            if (unchanged) { Invalidate(); return; }
            int selected = SelectedIndex, top = Items.Count == 0 ? 0 : TopIndex;
            string selectedKey = selected >= 0 ? ((EventRow)Items[selected]).Identity : null;
            string topKey = Items.Count > 0 ? ((EventRow)Items[top]).Identity : null;
            bool sameDay = contentKey != null && contentKey.StartsWith(day.ToString("yyyyMMdd"), StringComparison.Ordinal);
            contentKey = key;
            BeginUpdate(); Items.Clear();
            Items.AddRange(rows);
            if (sameDay && Items.Count > 0)
            {
                SelectedIndex = Array.FindIndex(rows, row => row.Identity == selectedKey);
                int visible = Array.FindIndex(rows, row => row.Identity == topKey);
                TopIndex = visible < 0 ? Math.Min(top, Items.Count - 1) : visible;
            }
            EndUpdate(); Invalidate();
        }

        private int Pixels(int value) { return (int)Math.Round(value * dpi / 96.0); }
        protected override void OnMeasureItem(MeasureItemEventArgs args) { args.ItemHeight = Pixels(92); }
        protected override void OnFontChanged(EventArgs args)
        {
            base.OnFontChanged(args);
            Font oldTitle = titleFont, oldDetail = detailFont;
            titleFont = new Font(Font.FontFamily, Font.Size, FontStyle.Bold, Font.Unit);
            detailFont = new Font(Font.FontFamily, Font.Size * 0.9f, FontStyle.Regular, Font.Unit);
            if (oldTitle != null) oldTitle.Dispose(); if (oldDetail != null) oldDetail.Dispose();
        }
        protected override void OnMouseLeave(EventArgs args) { hovered = -1; Invalidate(); base.OnMouseLeave(args); }
        protected override void OnDrawItem(DrawItemEventArgs args)
        {
            if (args.Index < 0 || palette == null) return;
            EventRow row = (EventRow)Items[args.Index];
            bool selected = (args.State & DrawItemState.Selected) != 0 || args.Index == hovered;
            Color background = selected ? palette.Hover : palette.EventSurface;
            using (Brush surface = new SolidBrush(palette.Surface)) args.Graphics.FillRectangle(surface, args.Bounds);
            Rectangle card = new Rectangle(args.Bounds.X + 1, args.Bounds.Y + Pixels(3), args.Bounds.Width - 3, args.Bounds.Height - Pixels(8));
            args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Rounded(card, Pixels(8)))
            using (Brush fill = new SolidBrush(background))
            using (Pen border = new Pen(palette.Rule)) { args.Graphics.FillPath(fill, path); args.Graphics.DrawPath(border, path); }
            Rectangle time = new Rectangle(card.X + Pixels(12), card.Y + Pixels(7), card.Width - Pixels(24), Pixels(18));
            Rectangle title = new Rectangle(time.X, card.Y + Pixels(27), time.Width, Pixels(34));
            Rectangle source = new Rectangle(time.X, card.Y + Pixels(64), time.Width, Pixels(16));
            TextRenderer.DrawText(args.Graphics, row.Time, detailFont ?? Font, time, palette.Accent, TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(args.Graphics, row.Event.Title, titleFont ?? Font, title, row.Event.Url == null ? palette.Secondary : palette.Ink,
                TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.WordBreak);
            TextRenderer.DrawText(args.Graphics, row.Source, detailFont ?? Font, source, palette.Secondary,
                TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            if ((args.State & DrawItemState.Focus) != 0) ControlPaint.DrawFocusRectangle(args.Graphics, Rectangle.Inflate(card, -3, -3), palette.Ink, background);
        }

        private static GraphicsPath Rounded(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath(); int d = radius * 2;
            path.AddArc(rect.Left, rect.Top, d, d, 180, 90); path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path;
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

        protected override void Dispose(bool disposing)
        {
            if (disposing) tooltip.Dispose(); base.Dispose(disposing);
            if (disposing) { if (titleFont != null) titleFont.Dispose(); if (detailFont != null) detailFont.Dispose(); }
        }
        private sealed class EventRow
        {
            internal CalendarEvent Event;
            internal string Time, Source;
            internal string Identity { get { return Event.CalendarId + "\u001f" + Event.Id + "\u001f" + Event.Start; } }
            public override string ToString() { return Time + ", " + Event.Title + ", " + Source; }
        }
    }
}
