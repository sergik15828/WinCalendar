using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WinCalendar
{
    internal static class AgendaInteractionSelfTest
    {
        internal static void Run(string directory, Action<bool, string> check)
        {
            DateTime date = new DateTime(2026, 11, 12);
            List<CalendarEvent> events = new List<CalendarEvent>();
            for (int i = 0; i < 18; i++)
                events.Add(new CalendarEvent { Id = "scroll-" + i, CalendarId = "fixture", CalendarName = "Синтетический календарь",
                    Title = "Встреча " + (i + 1) + " — проверка прокрутки", Start = CalendarEvent.Boundary(date.AddHours(i), TimeZoneInfo.Local).ToString("o"),
                    End = CalendarEvent.Boundary(date.AddHours(i + 1), TimeZoneInfo.Local).ToString("o"),
                    Url = "https://calendar.google.com/calendar/event?eid=scroll-" + i, MeetUrl = "https://meet.google.com/abc-defg-hij" });
            using (CalendarPopup popup = new CalendarPopup(delegate { return date.AddHours(10); }, delegate { }))
            {
                popup.ShowAt(new Point(400, 500), Screen.PrimaryScreen.WorkingArea);
                popup.Panel.SetData(new CalendarViewData { Connection = CalendarConnection.Connected, HasSnapshot = true, Events = events });
                CalendarEventList list = popup.Panel.EventList;
                foreach (int dpi in new[] { 96, 120, 144 })
                foreach (bool dark in new[] { false, true })
                {
                    popup.Panel.SetDpi(dpi); popup.ViewWindow.ClientSize = popup.Panel.NaturalSize; popup.Panel.ApplyTheme(dark);
                    list.TopIndex = 0;
                    list.ScrollWheel(-30); list.ScrollWheel(-30); list.ScrollWheel(-30);
                    check(list.TopIndex == 0, "Precision wheel deltas accumulate without premature scroll at " + dpi + " DPI");
                    list.ScrollWheel(-30);
                    int expected = SystemInformation.MouseWheelScrollLines < 0 ? Math.Max(1, list.ClientSize.Height / (92 * dpi / 96)) : Math.Max(1, SystemInformation.MouseWheelScrollLines / 3);
                    if (SystemInformation.MouseWheelScrollLines == 0) expected = 0;
                    check(list.TopIndex == expected, "One wheel notch scrolls the expected card count at " + dpi + " DPI");
                    for (int i = 0; i < 30; i++) list.ScrollWheel(-120);
                    check(list.TopIndex < events.Count && list.HitCard(new Point(list.Width / 2, 20 * dpi / 96)) == list.TopIndex,
                        "Scrolling to the end preserves visual and clickable row alignment at " + dpi + " DPI");
                    Save(popup.Panel, directory, "agenda-scroll-end-" + (dark ? "dark" : "light") + "-" + dpi + ".png");
                    for (int i = 0; i < 30; i++) list.ScrollWheel(120);
                    check(list.TopIndex == 0, "Scrolling back reaches the first card without overshoot at " + dpi + " DPI");
                    list.TopIndex = 8;
                    Save(popup.Panel, directory, "agenda-past-future-" + (dark ? "dark" : "light") + "-" + dpi + ".png");
                    check(list.Items[8].ToString().Contains("Завершено") && !list.Items[10].ToString().Contains("Завершено"),
                        "Past events are identified while ongoing and future events remain active");
                }
                list.TopIndex = 0;
                list.PointerPosition = delegate { return list.PointToScreen(new Point(list.Width / 2, 24)); };
                Message wheel = Message.Create(popup.Panel.TodayButton.Handle, 0x20a, new IntPtr(-120L << 16), IntPtr.Zero);
                bool focused = popup.Panel.TodayButton.Focused;
                check(list.PreFilterMessage(ref wheel) && list.TopIndex > 0 && popup.Panel.TodayButton.Focused == focused,
                    "Wheel over the agenda scrolls even when another calendar control holds focus");
                int top = list.TopIndex;
                list.PointerPosition = delegate { return list.PointToScreen(new Point(-20, -20)); };
                check(!list.PreFilterMessage(ref wheel) && list.TopIndex == top, "Wheel outside the agenda is not intercepted");
                list.TopIndex = 0;
                popup.Panel.SetData(new CalendarViewData { Connection = CalendarConnection.Connected, HasSnapshot = true, Events = events });
                check(list.TopIndex == 0, "Refreshing the same agenda does not move its scroll position");
                DateTimeOffset clock = events[0].ReminderEnd.AddSeconds(-1); list.Clock = delegate { return clock; };
                list.RefreshTimeState(); clock = clock.AddSeconds(2); list.RefreshTimeState();
                check(list.Items[0].ToString().Contains("Завершено"), "An event becomes muted after its end without waiting for network refresh");
            }
        }
        private static void Save(Control control, string directory, string name)
        { using (Bitmap bitmap = new Bitmap(control.Width, control.Height)) { control.DrawToBitmap(bitmap, control.ClientRectangle); bitmap.Save(Path.Combine(directory, name), ImageFormat.Png); } }
    }
}
