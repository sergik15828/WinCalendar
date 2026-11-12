using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WinCalendar
{
    internal static class RemainingMeetingsSelfTest
    {
        internal static void Run(string directory, Action<bool, string> check)
        {
            DateTime date = new DateTime(2026, 11, 12), now = date.AddHours(10).AddMinutes(30);
            List<CalendarEvent> events = new List<CalendarEvent>();
            foreach (int hour in new[] { 9, 10, 12 })
                events.Add(new CalendarEvent { Id = "remaining-" + hour, Title = "Синтетическая встреча " + hour, CalendarId = "fixture", CalendarName = "Команда",
                    Start = CalendarEvent.Boundary(date.AddHours(hour), TimeZoneInfo.Local).ToString("o"), End = CalendarEvent.Boundary(date.AddHours(hour + 1), TimeZoneInfo.Local).ToString("o"),
                    Url = "https://calendar.google.com/calendar/event?eid=remaining-" + hour, MeetUrl = "https://meet.google.com/abc-defg-hij" });
            using (CalendarPopup popup = new CalendarPopup(delegate { return now; }, delegate { }))
            {
                popup.ShowAt(new Point(400, 500), Screen.PrimaryScreen.WorkingArea);
                popup.Panel.SetData(new CalendarViewData { Connection = CalendarConnection.Connected, HasSnapshot = true, Events = events });
                check(popup.Panel.EventList.Items.Count == 2 && popup.Panel.EventList.Items[0].ToString().Contains("встреча 10"),
                    "Ended meeting is hidden while the ongoing and future meetings remain visible");
                Capture(popup, directory, "remaining-meetings");
                now = date.AddHours(11); popup.Panel.RefreshAgendaTime();
                check(popup.Panel.EventList.Items.Count == 1 && popup.Panel.EventList.Items[0].ToString().Contains("встреча 12"),
                    "A meeting disappears exactly at its end without a network update");
                now = date.AddHours(13); popup.Panel.RefreshAgendaTime();
                check(popup.Panel.EventList.Items.Count == 0 && popup.Panel.Controls.OfType<Label>().Any(c => c.Text == "На сегодня митов больше нет, приятного вечера"),
                    "After today's last meeting ends the requested evening message appears");
                check(!popup.Panel.Controls.OfType<CalendarDayButton>().Single(c => (DateTime)c.Tag == date).HasEvents,
                    "Calendar dot clears when the day's last meeting ends");
                Capture(popup, directory, "remaining-evening");
                popup.Panel.WeekButton.PerformClick();
                check(popup.Panel.EventList.Items.Count == 0, "Week view also excludes already ended meetings"); popup.Panel.WeekButton.PerformClick();
                popup.Panel.SetData(new CalendarViewData { Connection = CalendarConnection.Connected, HasSnapshot = true, LoadError = true, Events = events });
                check(!popup.Panel.Controls.OfType<Label>().Any(c => c.Text == "На сегодня митов больше нет, приятного вечера"),
                    "A failed refresh is not mistaken for a confirmed empty day");
            }
        }
        private static void Capture(CalendarPopup popup, string directory, string name)
        {
            foreach (int dpi in new[] { 96, 120, 144 })
            foreach (bool dark in new[] { false, true })
            {
                popup.Panel.SetDpi(dpi); popup.ViewWindow.ClientSize = popup.Panel.NaturalSize; popup.Panel.ApplyTheme(dark);
                using (Bitmap image = new Bitmap(popup.Panel.Width, popup.Panel.Height))
                { popup.Panel.DrawToBitmap(image, popup.Panel.ClientRectangle); image.Save(Path.Combine(directory, name + "-" + (dark ? "dark" : "light") + "-" + dpi + ".png"), ImageFormat.Png); }
            }
        }
    }
}
