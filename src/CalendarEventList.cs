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
        internal event Action<string> MeetRequested;
        internal AppPreferences Preferences = new AppPreferences();
        internal bool Weekly;

        private readonly Timer tooltipDelay = new Timer { Interval = 650 };
        private Point pointer;
        private bool tooltipVisible;

        internal CalendarEventList()
        {
            DrawMode = DrawMode.OwnerDrawVariable; BorderStyle = BorderStyle.None;
            IntegralHeight = false; AccessibleName = "Встречи выбранного дня";
            tooltip.UseAnimation = false; tooltip.UseFading = false; tooltip.ShowAlways = true;
            tooltipDelay.Tick += delegate
            {
                tooltipDelay.Stop();
                if (hovered < 0 || !Visible || HitCard(pointer) != hovered) return;
                tooltipVisible = true;
                tooltip.Show(Items[hovered].ToString(), this, pointer.X + Pixels(16), pointer.Y + Pixels(24), 8000);
            };
        }

        private Rectangle CardBounds(Rectangle row)
        { return new Rectangle(row.X + 1, row.Y + Pixels(3), row.Width - 3, row.Height - Pixels(8)); }

        internal int HitCard(Point point)
        {
            if (!ClientRectangle.Contains(point)) return -1;
            int index = IndexFromPoint(point);
            if (index < 0) return -1;
            using (GraphicsPath path = Rounded(CardBounds(GetItemRectangle(index)), Pixels(8)))
                return path.IsVisible(point) ? index : -1;
        }

        private void HideTooltip()
        {
            if (!tooltipVisible) return;
            tooltipVisible = false; tooltip.Hide(this);
        }

        private void InvalidateCard(int index)
        {
            if (index >= 0 && index < Items.Count) Invalidate(GetItemRectangle(index));
        }

        protected override void OnMouseMove(MouseEventArgs args)
        {
            int index = HitCard(args.Location);
            Cursor = index >= 0 && (CalendarPage.SafeGoogleLink(((EventRow)Items[index]).Event.Url) != null
                || (CalendarPage.SafeMeetLink(((EventRow)Items[index]).Event.MeetUrl) != null && MeetBounds(GetItemRectangle(index)).Contains(args.Location))) ? Cursors.Hand : Cursors.Default;
            pointer = args.Location;
            if (hovered != index)
            {
                tooltipDelay.Stop(); HideTooltip();
                int previous = hovered; hovered = index;
                InvalidateCard(previous); InvalidateCard(index);
                if (index >= 0) tooltipDelay.Start();
            }
            base.OnMouseMove(args);
        }

        protected override void OnMouseClick(MouseEventArgs args)
        {
            int index = HitCard(args.Location);
            if (args.Button == MouseButtons.Left && index >= 0)
            {
                EventRow row = (EventRow)Items[index];
                if (MeetBounds(GetItemRectangle(index)).Contains(args.Location) && CalendarPage.SafeMeetLink(row.Event.MeetUrl) != null) OpenMeet(index);
                else Open(index);
            }
            base.OnMouseClick(args);
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x201 || message.Msg == 0x203)
            {
                int packed = message.LParam.ToInt32();
                if (HitCard(new Point((short)(packed & 0xffff), (short)(packed >> 16))) < 0) return;
            }
            base.WndProc(ref message);
        }

        private void ClearHover()
        {
            tooltipDelay.Stop(); HideTooltip();
            int previous = hovered; hovered = -1; Cursor = Cursors.Default; InvalidateCard(previous);
        }

        internal void SetEvents(IEnumerable<CalendarEvent> events, DateTime day, TimeZoneInfo zone, CalendarPalette colors, int scaleDpi)
        {
            CalendarEvent[] all = events.ToArray();
            EventRow[] rows = all.OrderBy(e => Weekly ? DisplayDay(e, day, zone).Ticks : 0).ThenBy(e => e.SortOrder).Select(item => new EventRow { Event = item,
                Title = Preferences.PrivateMode ? "Занято" : item.Title,
                Conflict = all.Any(other => other != item && MeetingTools.Overlaps(item, other)),
                Time = (Weekly ? DisplayDay(item, day, zone).ToString("ddd d MMM", DateText.Russian) + " · " : "") + item.TimeLabel(Weekly ? DisplayDay(item, day, zone) : day, zone),
                Source = Preferences.PrivateMode ? "Календарь" : String.IsNullOrEmpty(item.CalendarName) ? "Google Calendar" : item.CalendarName }).ToArray();
            string key = day.ToString("yyyyMMdd") + Weekly + String.Join("\u001e", rows.Select(r => r.Event.CalendarId + "\u001f" + r.Event.Id + r.Title + r.Event.Url + r.Event.MeetUrl + r.Time + r.Source + r.Conflict));
            bool unchanged = contentKey == key && dpi == scaleDpi;
            palette = colors; dpi = scaleDpi;
            BackColor = palette.Surface; ForeColor = palette.Ink;
            if (unchanged) { Invalidate(); return; }
            int selected = SelectedIndex, top = Items.Count == 0 ? 0 : TopIndex;
            string selectedKey = selected >= 0 ? ((EventRow)Items[selected]).Identity : null;
            string topKey = Items.Count > 0 ? ((EventRow)Items[top]).Identity : null;
            bool sameDay = contentKey != null && contentKey.StartsWith(day.ToString("yyyyMMdd"), StringComparison.Ordinal);
            contentKey = key;
            ClearHover();
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
        private static DateTime DisplayDay(CalendarEvent item, DateTime first, TimeZoneInfo zone)
        {
            DateTime date = item.AllDay ? CalendarEvent.Date(item.Start) : TimeZoneInfo.ConvertTime(item.ReminderStart, zone).Date;
            return date < first ? first : date;
        }
        protected override void OnMeasureItem(MeasureItemEventArgs args) { args.ItemHeight = Pixels(92); }
        protected override void OnFontChanged(EventArgs args)
        {
            base.OnFontChanged(args);
            Font oldTitle = titleFont, oldDetail = detailFont;
            titleFont = new Font(Font.FontFamily, Font.Size, FontStyle.Bold, Font.Unit);
            detailFont = new Font(Font.FontFamily, Font.Size * 0.9f, FontStyle.Regular, Font.Unit);
            if (oldTitle != null) oldTitle.Dispose(); if (oldDetail != null) oldDetail.Dispose();
        }
        protected override void OnMouseLeave(EventArgs args)
        {
            // Native tooltip windows can report mouse-leave while the pointer
            // still rests over this card. Keep its hover stable in that case.
            if (HitCard(PointToClient(MousePosition)) < 0) ClearHover();
            base.OnMouseLeave(args);
        }
        protected override void OnVisibleChanged(EventArgs args)
        { if (!Visible) ClearHover(); base.OnVisibleChanged(args); }
        protected override void OnMouseWheel(MouseEventArgs args)
        { ClearHover(); base.OnMouseWheel(args); }
        protected override void OnDrawItem(DrawItemEventArgs args)
        {
            if (args.Index < 0 || palette == null) return;
            using (BufferedGraphics buffer = BufferedGraphicsManager.Current.Allocate(args.Graphics, args.Bounds))
            {
                buffer.Graphics.ResetTransform();
                DrawCard(new DrawItemEventArgs(buffer.Graphics, args.Font,
                    new Rectangle(Point.Empty, args.Bounds.Size), args.Index, args.State));
                buffer.Render(args.Graphics);
            }
        }

        private void DrawCard(DrawItemEventArgs args)
        {
            EventRow row = (EventRow)Items[args.Index];
            bool selected = (args.State & DrawItemState.Selected) != 0 || args.Index == hovered;
            Color background = selected ? palette.Hover : palette.EventSurface;
            using (Brush surface = new SolidBrush(palette.Surface)) args.Graphics.FillRectangle(surface, args.Bounds);
            Rectangle card = CardBounds(args.Bounds);
            args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Rounded(card, Pixels(8)))
            using (Brush fill = new SolidBrush(background))
            using (Pen border = new Pen(palette.Rule)) { args.Graphics.FillPath(fill, path); args.Graphics.DrawPath(border, path); }
            bool meet = CalendarPage.SafeMeetLink(row.Event.MeetUrl) != null;
            Rectangle time = new Rectangle(card.X + Pixels(12), card.Y + Pixels(7), card.Width - Pixels(meet ? 104 : 24), Pixels(18));
            Rectangle title = new Rectangle(time.X, card.Y + Pixels(27), card.Width - Pixels(24), Pixels(34));
            Rectangle source = new Rectangle(time.X + Pixels(10), card.Y + Pixels(64), card.Width - Pixels(row.Conflict ? 138 : 34), Pixels(16));
            TextRenderer.DrawText(args.Graphics, row.Time, detailFont ?? Font, time, palette.Accent, TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(args.Graphics, row.Title, titleFont ?? Font, title, row.Event.Url == null ? palette.Secondary : palette.Ink,
                TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.WordBreak);
            TextRenderer.DrawText(args.Graphics, row.Source, detailFont ?? Font, source, palette.Secondary,
                TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            using (Brush dot = new SolidBrush(SystemInformation.HighContrast ? palette.Ink : Preferences.ColorFor(row.Event, palette.Accent)))
                args.Graphics.FillEllipse(dot, time.X, source.Y + Pixels(5), Pixels(5), Pixels(5));
            if (row.Conflict) TextRenderer.DrawText(args.Graphics, "Пересечение", detailFont ?? Font,
                new Rectangle(card.Right - Pixels(112), source.Top, Pixels(100), source.Height), palette.Accent, TextFormatFlags.Right | TextFormatFlags.SingleLine);
            if (meet)
            {
                Rectangle button = MeetBounds(args.Bounds);
                using (GraphicsPath path = Rounded(button, Pixels(5)))
                using (Brush fill = new SolidBrush(palette.Hover)) args.Graphics.FillPath(fill, path);
                TextRenderer.DrawText(args.Graphics, "Войти в Meet", detailFont ?? Font, button, palette.Ink,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }
            if ((args.State & DrawItemState.Focus) != 0) ControlPaint.DrawFocusRectangle(args.Graphics, Rectangle.Inflate(card, -3, -3), palette.Ink, background);
        }

        private Rectangle MeetBounds(Rectangle row)
        { Rectangle card = CardBounds(row); return new Rectangle(card.Right - Pixels(92), card.Top + Pixels(5), Pixels(80), Pixels(23)); }

        private static GraphicsPath Rounded(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath(); int d = radius * 2;
            path.AddArc(rect.Left, rect.Top, d, d, 180, 90); path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path;
        }

        protected override void OnKeyDown(KeyEventArgs args)
        {
            if (args.KeyCode == Keys.Enter && SelectedIndex >= 0) { if (args.Alt) OpenMeet(SelectedIndex); else Open(SelectedIndex); args.Handled = args.SuppressKeyPress = true; }
            base.OnKeyDown(args);
        }

        internal void Open(int index)
        {
            string url = CalendarPage.SafeGoogleLink(((EventRow)Items[index]).Event.Url);
            if (url != null && OpenRequested != null) OpenRequested(url);
        }
        internal void OpenMeet(int index)
        {
            string url = CalendarPage.SafeMeetLink(((EventRow)Items[index]).Event.MeetUrl);
            if (url != null && MeetRequested != null) MeetRequested(url);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { tooltipDelay.Dispose(); tooltip.Dispose(); } base.Dispose(disposing);
            if (disposing) { if (titleFont != null) titleFont.Dispose(); if (detailFont != null) detailFont.Dispose(); }
        }
        private sealed class EventRow
        {
            internal CalendarEvent Event;
            internal string Time, Source, Title;
            internal bool Conflict;
            internal string Identity { get { return Event.CalendarId + "\u001f" + Event.Id + "\u001f" + Event.Start; } }
            public override string ToString() { return Time + ", " + Title + ", " + Source + (Conflict ? ", Пересечение" : "") + (CalendarPage.SafeMeetLink(Event.MeetUrl) != null ? ", Войти в Meet: Alt+Enter" : ""); }
        }
    }
}
