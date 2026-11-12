using System;
using System.Collections.Generic;
using System.Linq;

namespace WinCalendar
{
    internal static class MeetingTools
    {
        internal static CalendarEvent Next(IEnumerable<CalendarEvent> events, DateTimeOffset now)
        { return events.Where(e => !e.AllDay && e.ReminderEnd > now).OrderBy(e => e.ReminderStart).FirstOrDefault(); }
        internal static bool Overlaps(CalendarEvent left, CalendarEvent right)
        {
            if (left.AllDay || right.AllDay) return false;
            if (!String.IsNullOrEmpty(left.ICalUid) && left.ICalUid == right.ICalUid && left.ReminderStart == right.ReminderStart) return false;
            return left.ReminderStart < right.ReminderEnd && right.ReminderStart < left.ReminderEnd;
        }
        internal static string NextLabel(CalendarEvent item, DateTimeOffset now, bool privacy)
        {
            if (item == null) return UiText.Get("Ближайших встреч нет");
            DateTime start = item.ReminderStart.ToLocalTime().DateTime;
            string when = item.ReminderStart <= now ? UiText.Get("Сейчас") : start.Date == now.LocalDateTime.Date ? UiText.Get("Через ") + Math.Max(1, (int)Math.Ceiling((item.ReminderStart - now).TotalMinutes)) + UiText.Get(" мин") : start.ToString("d MMM, HH:mm", DateText.Culture);
            return when + " · " + (privacy ? UiText.Get("Занято") : item.Title);
        }
    }
}
