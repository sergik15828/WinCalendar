using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinCalendar
{
    internal sealed class CalendarEventList : ListBox, IMessageFilter
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
        internal Func<DateTimeOffset> Clock = delegate { return DateTimeOffset.Now; };
        internal Func<Point> PointerPosition = delegate { return MousePosition; };
        private int wheelRemainder;

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
            Cursor = index >= 0 && !IsPast((EventRow)Items[index]) && (CalendarPage.SafeGoogleLink(((EventRow)Items[index]).Event.Url) != null
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
            if (message.Msg == 0x20a)
            {
                ScrollWheel((short)(message.WParam.ToInt64() >> 16)); message.Result = IntPtr.Zero; return;
            }
            if (message.Msg == 0x115)
            {
                ClearHover(); BeginUpdate();
                try { base.WndProc(ref message); }
                finally { EndUpdate(); }
                UpdatePointer(); return;
            }
            if (message.Msg == 0x201 || message.Msg == 0x203)
            {
                int packed = message.LParam.ToInt32();
                int hit = HitCard(new Point((short)(packed & 0xffff), (short)(packed >> 16)));
                if (hit < 0 || IsPast((EventRow)Items[hit])) return;
            }
            base.WndProc(ref message);
        }

        protected override void OnHandleCreated(EventArgs args)
        { base.OnHandleCreated(args); Application.AddMessageFilter(this); }
        protected override void OnHandleDestroyed(EventArgs args)
        { Application.RemoveMessageFilter(this); base.OnHandleDestroyed(args); }
        public bool PreFilterMessage(ref Message message)
        {
            if (message.Msg != 0x20a || !Visible || !IsHandleCreated || message.HWnd == Handle) return false;
            Control target = Control.FromChildHandle(message.HWnd);
            if (target == null || target.FindForm() != FindForm() || !ClientRectangle.Contains(PointToClient(PointerPosition()))) return false;
            SendMessage(Handle, message.Msg, message.WParam, message.LParam); return true;
        }
        internal void ScrollWheel(int delta)
        {
            if (Items.Count == 0 || SystemInformation.MouseWheelScrollLines == 0) return;
            wheelRemainder += delta; int steps = wheelRemainder / 120; wheelRemainder %= 120;
            if (steps == 0) return;
            int visibleRows = Math.Max(1, ClientSize.Height / Pixels(92));
            int stride = SystemInformation.MouseWheelScrollLines < 0 ? visibleRows : Math.Max(1, SystemInformation.MouseWheelScrollLines / 3);
            int next = Math.Max(0, Math.Min(Items.Count - visibleRows, TopIndex - steps * stride));
            if (next == TopIndex) return;
            ClearHover(); BeginUpdate();
            try { TopIndex = next; }
            finally { EndUpdate(); }
            UpdatePointer();
        }
        private void UpdatePointer()
        {
            Point point = PointToClient(PointerPosition());
            if (ClientRectangle.Contains(point)) OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0));
        }
        internal void RefreshTimeState()
        {
            if (!Visible) return;
            for (int i = 0; i < Items.Count; i++)
            {
                EventRow row = (EventRow)Items[i]; bool past = row.Event.ReminderEnd <= Clock();
                if (row.Past != past)
                {
                    row.Past = past; InvalidateCard(i);
                    if (i == hovered && past) { Cursor = Cursors.Default; tooltipDelay.Stop(); HideTooltip(); }
                }
            }
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
                Past = item.ReminderEnd <= Clock(),
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
            wheelRemainder = 0;
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
            if (HitCard(PointToClient(PointerPosition())) < 0) ClearHover();
            base.OnMouseLeave(args);
        }
        protected override void OnVisibleChanged(EventArgs args)
        { if (!Visible) ClearHover(); base.OnVisibleChanged(args); }
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
            bool past = row.Event.ReminderEnd <= Clock(); row.Past = past;
            bool selected = !past && ((args.State & DrawItemState.Selected) != 0 || args.Index == hovered);
            Color background = selected ? palette.Hover : palette.EventSurface;
            Color ink = past && !SystemInformation.HighContrast ? Mix(palette.Ink, palette.Surface, 0.42) : palette.Ink;
            Color secondary = past ? ink : palette.Secondary;
            Color accent = past ? ink : palette.Accent;
            if (past && !SystemInformation.HighContrast) background = Mix(background, palette.Surface, 0.35);
            using (Brush surface = new SolidBrush(palette.Surface)) args.Graphics.FillRectangle(surface, args.Bounds);
            Rectangle card = CardBounds(args.Bounds);
            args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Rounded(card, Pixels(8)))
            using (Brush fill = new SolidBrush(background))
            using (Pen border = new Pen(past ? Mix(palette.Rule, palette.Surface, 0.35) : palette.Rule)) { args.Graphics.FillPath(fill, path); args.Graphics.DrawPath(border, path); }
            bool meet = !past && CalendarPage.SafeMeetLink(row.Event.MeetUrl) != null;
            Rectangle time = new Rectangle(card.X + Pixels(12), card.Y + Pixels(7), card.Width - Pixels(meet ? 128 : 24), Pixels(18));
            Rectangle title = new Rectangle(time.X, card.Y + Pixels(27), card.Width - Pixels(24), Pixels(34));
            Rectangle source = new Rectangle(time.X + Pixels(10), card.Y + Pixels(64), card.Width - Pixels(row.Conflict ? 138 : 34), Pixels(16));
            TextRenderer.DrawText(args.Graphics, row.Time, detailFont ?? Font, time, accent, TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(args.Graphics, row.Title, titleFont ?? Font, title, row.Event.Url == null ? secondary : ink,
                TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.WordBreak);
            TextRenderer.DrawText(args.Graphics, row.Source, detailFont ?? Font, source, secondary,
                TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            using (Brush dot = new SolidBrush(SystemInformation.HighContrast ? palette.Ink : Mix(Preferences.ColorFor(row.Event, palette.Accent), palette.Surface, past ? 0.30 : 1)))
                args.Graphics.FillEllipse(dot, time.X, source.Y + Pixels(5), Pixels(5), Pixels(5));
            if (row.Conflict) TextRenderer.DrawText(args.Graphics, "Пересечение", detailFont ?? Font,
                new Rectangle(card.Right - Pixels(112), source.Top, Pixels(100), source.Height), accent, TextFormatFlags.Right | TextFormatFlags.SingleLine);
            if (meet)
            {
                Rectangle button = MeetBounds(args.Bounds);
                using (GraphicsPath path = Rounded(button, Pixels(5)))
                using (Brush fill = new SolidBrush(past ? Mix(palette.Accent, palette.Surface, 0.08) : palette.Accent)) args.Graphics.FillPath(fill, path);
                TextRenderer.DrawText(args.Graphics, "Войти в Meet", detailFont ?? Font, button, past ? ink : palette.OnAccent,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }
            if ((args.State & DrawItemState.Focus) != 0) ControlPaint.DrawFocusRectangle(args.Graphics, Rectangle.Inflate(card, -3, -3), palette.Ink, background);
        }

        private Rectangle MeetBounds(Rectangle row)
        { Rectangle card = CardBounds(row); return new Rectangle(card.Right - Pixels(108), card.Top + Pixels(5), Pixels(96), Pixels(24)); }

        private static Color Mix(Color foreground, Color background, double opacity)
        { return Color.FromArgb((int)(foreground.R * opacity + background.R * (1 - opacity)), (int)(foreground.G * opacity + background.G * (1 - opacity)), (int)(foreground.B * opacity + background.B * (1 - opacity))); }

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
            if (index < 0 || index >= Items.Count || IsPast((EventRow)Items[index])) return;
            string url = CalendarPage.SafeGoogleLink(((EventRow)Items[index]).Event.Url);
            if (url != null && OpenRequested != null) OpenRequested(url);
        }
        internal void OpenMeet(int index)
        {
            if (index < 0 || index >= Items.Count || IsPast((EventRow)Items[index])) return;
            string url = CalendarPage.SafeMeetLink(((EventRow)Items[index]).Event.MeetUrl);
            if (url != null && MeetRequested != null) MeetRequested(url);
        }
        private bool IsPast(EventRow row) { return row.Event.ReminderEnd <= Clock(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { tooltipDelay.Dispose(); tooltip.Dispose(); } base.Dispose(disposing);
            if (disposing) { if (titleFont != null) titleFont.Dispose(); if (detailFont != null) detailFont.Dispose(); }
        }
        private sealed class EventRow
        {
            internal CalendarEvent Event;
            internal string Time, Source, Title;
            internal bool Conflict, Past;
            internal string Identity { get { return Event.CalendarId + "\u001f" + Event.Id + "\u001f" + Event.Start; } }
            public override string ToString() { return Time + ", " + Title + ", " + Source + (Past ? ", Завершено" : "") + (Conflict ? ", Пересечение" : "") + (!Past && CalendarPage.SafeMeetLink(Event.MeetUrl) != null ? ", Войти в Meet: Alt+Enter" : ""); }
        }
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wparam, IntPtr lparam);
    }
}
