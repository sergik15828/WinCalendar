using System;
using System.Collections;
using System.Collections.Generic;

namespace WinCalendar
{
    internal sealed class GoogleCalendarEntry
    {
        internal string Id, Name;
    }

    internal sealed class GoogleCalendarList
    {
        internal readonly List<GoogleCalendarEntry> Calendars = new List<GoogleCalendarEntry>();
        internal string NextPage;

        internal static GoogleCalendarList Parse(string json)
        {
            Dictionary<string, object> root = Json.Object(json);
            GoogleCalendarList list = new GoogleCalendarList { NextPage = Json.Text(root, "nextPageToken") };
            object items;
            if (!root.TryGetValue("items", out items)) return list;
            foreach (object item in (IEnumerable)items)
            {
                Dictionary<string, object> data = item as Dictionary<string, object>;
                if (data == null || Flag(data, "deleted") || Flag(data, "hidden")) continue;
                string role = Json.Text(data, "accessRole");
                if (role != "owner" && role != "writer" && role != "reader" && role != "writerWithoutPrivateAccess") continue;
                string id = Json.Text(data, "id");
                if (String.IsNullOrEmpty(id)) continue;
                // Include readable additional calendars, even if their display
                // checkbox differs between Google Calendar browser sessions.
                string name = Json.Text(data, "summaryOverride") ?? Json.Text(data, "summary");
                list.Calendars.Add(new GoogleCalendarEntry { Id = id, Name = name ?? "Google Calendar" });
            }
            return list;
        }

        private static bool Flag(Dictionary<string, object> data, string key)
        {
            object value; return data.TryGetValue(key, out value) && Object.Equals(value, true);
        }
    }
}
