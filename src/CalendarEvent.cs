using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Script.Serialization;

namespace WinCalendar
{
    internal sealed class CalendarEvent
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public bool AllDay { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public string CalendarId { get; set; }
        public string CalendarName { get; set; }
        public string CalendarTimeZone { get; set; }
        public List<int> PopupReminderMinutes { get; set; }

        internal DateTimeOffset ReminderStart
        {
            get { return AllDay ? Boundary(Date(Start), CalendarTimeZones.Resolve(CalendarTimeZone)) : Timestamp(Start); }
        }
        internal DateTimeOffset ReminderEnd
        {
            get { return AllDay ? Boundary(Date(End), CalendarTimeZones.Resolve(CalendarTimeZone)) : Timestamp(End); }
        }

        internal bool OccursOn(DateTime date, TimeZoneInfo zone)
        {
            if (AllDay) return date.Date >= Date(Start) && date.Date < Date(End);
            DateTimeOffset start = Timestamp(Start), end = Timestamp(End);
            DateTimeOffset lower = Boundary(date.Date, zone), upper = Boundary(date.Date.AddDays(1), zone);
            return start < upper && (end > lower || (end == start && start >= lower));
        }

        internal string TimeLabel(DateTime date, TimeZoneInfo zone)
        {
            if (AllDay) return "Весь день";
            DateTime start = TimeZoneInfo.ConvertTime(Timestamp(Start), zone).DateTime;
            DateTime end = TimeZoneInfo.ConvertTime(Timestamp(End), zone).DateTime;
            string from = start.Date < date.Date ? "Ранее" : start.ToString("HH:mm", CultureInfo.InvariantCulture);
            string to = end.Date > date.Date && end.TimeOfDay != TimeSpan.Zero ? "далее" : end.ToString("HH:mm", CultureInfo.InvariantCulture);
            return from + " — " + to;
        }

        internal long SortOrder { get { return AllDay ? Int64.MinValue : Timestamp(Start).UtcTicks; } }
        internal static DateTime Date(string value) { return DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture); }
        internal static DateTimeOffset Timestamp(string value) { return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.None); }
        internal static DateTimeOffset Boundary(DateTime date, TimeZoneInfo zone)
        {
            date = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
            while (zone.IsInvalidTime(date)) date = date.AddMinutes(30);
            return new DateTimeOffset(date, zone.GetUtcOffset(date));
        }
    }

    internal sealed class CalendarPage
    {
        internal readonly List<CalendarEvent> Events = new List<CalendarEvent>();
        internal string NextPage;
        internal static CalendarPage Parse(string json, List<int> fallbackReminders = null)
        {
            Dictionary<string, object> root = Json.Object(json);
            CalendarPage page = new CalendarPage { NextPage = Json.Text(root, "nextPageToken") };
            object defaults;
            List<int> defaultMinutes = root.TryGetValue("defaultReminders", out defaults) ? GoogleReminderRules.PopupMinutes(defaults) : fallbackReminders ?? new List<int>();
            object items;
            if (!root.TryGetValue("items", out items)) return page;
            foreach (object item in (IEnumerable)items)
            {
                Dictionary<string, object> data = item as Dictionary<string, object>;
                if (data == null || Json.Text(data, "status") == "cancelled") continue;
                object rawStart, rawEnd;
                if (!data.TryGetValue("start", out rawStart) || !data.TryGetValue("end", out rawEnd)) continue;
                Dictionary<string, object> start = rawStart as Dictionary<string, object>, end = rawEnd as Dictionary<string, object>;
                if (start == null || end == null) continue;
                bool allDay = start.ContainsKey("date");
                string from = Json.Text(start, allDay ? "date" : "dateTime"), to = Json.Text(end, allDay ? "date" : "dateTime");
                try
                {
                    if (allDay) { if (CalendarEvent.Date(to) <= CalendarEvent.Date(from)) continue; }
                    else if (CalendarEvent.Timestamp(to) < CalendarEvent.Timestamp(from)) continue;
                }
                catch (FormatException) { continue; }
                catch (ArgumentNullException) { continue; }
                string title = Json.Text(data, "summary");
                object rawReminders;
                Dictionary<string, object> reminders = data.TryGetValue("reminders", out rawReminders) ? rawReminders as Dictionary<string, object> : null;
                object useDefault, overrides;
                bool custom = reminders != null && reminders.TryGetValue("useDefault", out useDefault) && Object.Equals(useDefault, false);
                List<int> minutes = custom ? (reminders.TryGetValue("overrides", out overrides) ? GoogleReminderRules.PopupMinutes(overrides) : new List<int>()) : new List<int>(defaultMinutes);
                page.Events.Add(new CalendarEvent { Id = Json.Text(data, "id"), Title = String.IsNullOrWhiteSpace(title) ? "Без названия" : title,
                    Url = SafeGoogleLink(Json.Text(data, "htmlLink")), AllDay = allDay, Start = from, End = to, PopupReminderMinutes = minutes });
            }
            return page;
        }

        internal static string SafeGoogleLink(string link)
        {
            Uri uri;
            if (!Uri.TryCreate(link, UriKind.Absolute, out uri) || uri.Scheme != "https" || uri.UserInfo.Length != 0 || !uri.IsDefaultPort) return null;
            if (uri.Host != "calendar.google.com" && uri.Host != "www.google.com") return null;
            if (uri.Host == "www.google.com" && !uri.AbsolutePath.StartsWith("/calendar/", StringComparison.Ordinal)) return null;
            return uri.AbsoluteUri;
        }
    }

    internal static class Json
    {
        internal static Dictionary<string, object> Object(string value) { return new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(value); }
        internal static string Text(Dictionary<string, object> data, string key)
        {
            object value; return data.TryGetValue(key, out value) ? value as string : null;
        }
        internal static string Serialize(object value) { return new JavaScriptSerializer().Serialize(value); }
    }
}
