using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WinCalendar
{
    internal static class ReminderTimingSelfTest
    {
        internal static void Run(string directory, Action<bool, string> check)
        {
            string root = Path.Combine(directory, "timing-" + Guid.NewGuid().ToString("N"));
            DateTimeOffset now = CalendarEvent.Timestamp("2026-11-12T09:49:00+03:00");
            CalendarEvent item = new CalendarEvent { Id = "timing-fixture", CalendarId = "fixture", Start = "2026-11-12T10:00:00+03:00", End = "2026-11-12T11:00:00+03:00", PopupReminderMinutes = new List<int> { 10 } };
            try
            {
                ReminderEngine original = new ReminderEngine(root, "event", () => now);
                original.Evaluate(new[] { item }); now = now.AddMinutes(1); original.Evaluate(new[] { item });
                check(original.Active, "Event-settings timing retains the configured Google ten-minute reminder");
                now = CalendarEvent.Timestamp("2026-11-12T09:54:59+03:00");
                ReminderEngine five = new ReminderEngine(root, "five", () => now, 1); five.Evaluate(new[] { item });
                check(!five.Active, "Fixed five-minute mode does not fire before its exact deadline");
                now = now.AddSeconds(1); five.Evaluate(new[] { item });
                check(five.Active && item.PopupReminderMinutes.SequenceEqual(new[] { 10 }), "Fixed five-minute mode fires locally without changing the event's Google reminder");
                five.Acknowledge(); now = now.AddMinutes(1); five.SetTiming(2); five.Evaluate(new[] { item }, now.AddHours(-1).UtcDateTime);
                check(!five.Active, "Changing reminder timing does not replay past reminders from an older cached snapshot");
                now = CalendarEvent.Timestamp("2026-11-12T09:57:59+03:00"); five.Evaluate(new[] { item }); bool before = five.Active;
                now = now.AddSeconds(1); five.Evaluate(new[] { item });
                check(!before && five.Active, "Fixed two-minute mode fires exactly two minutes before the meeting");
                five.Acknowledge(); now = now.AddMinutes(1); five.SetTiming(0); five.Evaluate(new[] { item }, now.AddHours(-1).UtcDateTime);
                check(!five.Active, "Returning to event settings does not replay an already passed Google deadline");
                now = CalendarEvent.Timestamp("2026-11-12T09:57:59+03:00");
                CalendarEvent silent = new CalendarEvent { Id = "local-only-fixture", Start = item.Start, End = item.End, PopupReminderMinutes = new List<int>() };
                ReminderEngine local = new ReminderEngine(root, "local", () => now, 2); local.Evaluate(new[] { silent }); now = now.AddSeconds(1); local.Evaluate(new[] { silent });
                check(local.Active && silent.PopupReminderMinutes.Count == 0, "Explicit local timing can remind for a timed event without Google popup reminders");
                check(ReminderEngine.MinutesFor(new CalendarEvent { AllDay = true, PopupReminderMinutes = new List<int> { 1440 } }, 2).SequenceEqual(new[] { 1440 }),
                    "All-day events keep their original Google reminder instead of a near-midnight meeting override");
                PreferenceStore store = new PreferenceStore(Path.Combine(root, "preferences.bin"));
                store.Save(new AppPreferences { ReminderTiming = 2 });
                check(store.Load().ReminderTiming == 2, "Selected reminder timing persists across application restarts");
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
    }
}
