using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinCalendar
{
    internal static class FeaturesSelfTest
    {
        internal static async Task RunAsync(string directory, Action<bool, string> check)
        {
            string body = "\"id\":\"fixture\",\"summary\":\"Синтетическая встреча\",\"start\":{\"dateTime\":\"2026-11-12T10:00:00+03:00\"},\"end\":{\"dateTime\":\"2026-11-12T11:00:00+03:00\"}";
            CalendarEvent direct = CalendarPage.Parse("{\"items\":[{" + body + ",\"hangoutLink\":\"https://meet.google.com/abc-defg-hij\"}]}").Events.Single();
            CalendarEvent conference = CalendarPage.Parse("{\"items\":[{" + body + ",\"conferenceData\":{\"entryPoints\":[{\"entryPointType\":\"video\",\"uri\":\"https://meet.google.com/abc-defg-hij\"}]}}]}").Events.Single();
            check(direct.MeetUrl == conference.MeetUrl && direct.MeetUrl != null, "Meet links load from both Google conference representations");
            check(CalendarPage.Parse("{\"items\":[{" + body + ",\"location\":\"meet.google.com/abc-defg-hij\"}]}").Events.Single().MeetUrl == direct.MeetUrl,
                "Meet links also load from a meeting URL entered as event location");
            check(CalendarPage.SafeMeetLink("https://meet.google.com.evil.example/abc-defg-hij") == null
                && CalendarPage.SafeMeetLink("http://meet.google.com/abc-defg-hij") == null
                && CalendarPage.SafeMeetLink("https://user@meet.google.com/abc-defg-hij") == null,
                "Meet actions reject other hosts, plain HTTP and credentials in URLs");

            AppPreferences options = new AppPreferences { QuietEnabled = true };
            check(options.IsQuiet(new DateTime(2026, 11, 12, 22, 0, 0)) && options.IsQuiet(new DateTime(2026, 11, 13, 7, 59, 0))
                && !options.IsQuiet(new DateTime(2026, 11, 13, 8, 0, 0)), "Quiet hours span midnight with an exclusive end boundary");
            options.QuietEnd = options.QuietStart;
            check(options.IsQuiet(new DateTime(2026, 11, 12, 12, 0, 0)), "Equal quiet-hour endpoints mean silence all day");
            byte[] muted = ReminderChime.CreateWave(2, 0);
            check(muted.Skip(44).All(sample => sample == 0) && !ReminderChime.CreateWave(0, 100).SequenceEqual(ReminderChime.CreateWave(1, 100))
                && !ReminderChime.CreateWave(1, 100).SequenceEqual(ReminderChime.CreateWave(2, 100)), "Three chimes differ and zero volume produces silent PCM");
            int normalPeak = Peak(ReminderChime.CreateWave(0, 100)), halfPeak = Peak(ReminderChime.CreateWave(0, 50));
            check(Math.Abs(normalPeak / 2 - halfPeak) < 2, "Application volume scales PCM independently of Windows master volume");

            DateTime date = new DateTime(2026, 11, 12);
            CalendarEvent a = Event("a", date.AddHours(10), date.AddHours(11));
            CalendarEvent b = Event("b", date.AddHours(10).AddMinutes(30), date.AddHours(11).AddMinutes(30));
            CalendarEvent adjacent = Event("adjacent", date.AddHours(11), date.AddHours(12));
            check(MeetingTools.Overlaps(a, b) && !MeetingTools.Overlaps(a, adjacent), "Conflict detection finds overlaps but excludes back-to-back meetings");
            CalendarEvent copy = Event("copy", date.AddHours(10), date.AddHours(11)); a.ICalUid = copy.ICalUid = "fixture-ical";
            check(!MeetingTools.Overlaps(a, copy), "Copies of one meeting instance in different calendars are not conflicts");
            DateTimeOffset now = a.ReminderStart.AddMinutes(15);
            check(MeetingTools.Next(new[] { adjacent, a }, now) == a
                && !MeetingTools.NextLabel(a, now, true).Contains(a.Title), "Next meeting includes an ongoing event and respects privacy");

            string path = Path.Combine(directory, "features-" + Guid.NewGuid().ToString("N"));
            try
            {
                options = Options(); options.PrivateMode = true; options.Volume = 42; options.DisabledCalendars.Add("b");
                PreferenceStore store = new PreferenceStore(Path.Combine(path, "preferences.bin")); store.Save(options);
                AppPreferences restored = store.Load();
                check(restored.PrivateMode && restored.Volume == 42 && restored.DisabledCalendars.Contains("b")
                    && !System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(path, "preferences.bin"))).Contains("Команда"),
                    "Preferences, calendars and colors persist with current-user encryption");
                DateTimeOffset clock = a.ReminderStart.AddMinutes(-1);
                a.PopupReminderMinutes = new List<int> { 0 }; a.CalendarId = "a";
                ReminderEngine engine = new ReminderEngine(path, "snooze-fixture", delegate { return clock; });
                engine.Evaluate(new[] { a }); clock = a.ReminderStart; engine.Evaluate(new[] { a }); engine.Acknowledge();
                check(engine.CanSnooze && engine.Snooze(5) && !engine.Active, "An acknowledged reminder can be snoozed locally for five minutes");
                ReminderEngine restarted = new ReminderEngine(path, "snooze-fixture", delegate { return clock; });
                clock = clock.AddMinutes(4); restarted.Evaluate(new CalendarEvent[0]); check(!restarted.Active, "Snooze stays silent until its deadline after restart");
                clock = clock.AddMinutes(1); restarted.Evaluate(new CalendarEvent[0]);
                check(restarted.Active && restarted.Snooze(10), "Persisted snooze fires at its deadline and can be deferred ten more minutes");
                CalendarEvent incoming = Event("incoming", date.AddHours(10).AddMinutes(6), date.AddHours(11)); incoming.PopupReminderMinutes = new List<int> { 0 };
                clock = clock.AddMinutes(1); restarted.Evaluate(new[] { incoming });
                check(restarted.Active, "A new reminder is not suppressed while another batch is snoozed");
                restarted.Acknowledge(); clock = clock.AddMinutes(9); restarted.Evaluate(new CalendarEvent[0]);
                check(restarted.Active, "Acknowledging another meeting does not discard an existing snooze");
                restarted.RemoveDisabled(new[] { "a" }); check(!restarted.Active, "Disabling a calendar removes its pending reminder and snooze");
                RangeSource ranges = new RangeSource();
                using (CalendarSync sync = new CalendarSync(ranges, new CalendarCache(Path.Combine(path, "ranges"))))
                {
                    await sync.ShowAsync(date, date.AddDays(7), TimeZoneInfo.Local, false);
                    await sync.ShowAsync(date, date.AddDays(14), TimeZoneInfo.Local, false);
                    check(ranges.Calls == 2, "Extending an agenda range fetches new data rather than reusing an incomplete cached range");
                }
            }
            finally { if (Directory.Exists(path)) Directory.Delete(path, true); }

            using (CalendarEventList list = new CalendarEventList { Clock = delegate { return a.ReminderStart.AddMinutes(-1); } })
            {
                a.MeetUrl = direct.MeetUrl; string opened = null;
                list.SetEvents(new[] { a }, date, TimeZoneInfo.Local, CalendarPalette.Create(false), 96);
                list.MeetRequested += delegate(string url) { opened = url; }; list.OpenMeet(0);
                check(opened == direct.MeetUrl, "Meet action dispatches the video-call URL independently of the event link");
                list.Preferences = new AppPreferences { PrivateMode = true };
                list.SetEvents(new[] { a }, date, TimeZoneInfo.Local, CalendarPalette.Create(false), 96);
                check(!list.Items[0].ToString().Contains(a.Title) && !list.Items[0].ToString().Contains(a.CalendarName),
                    "Private card tooltip and accessibility text hide meeting and calendar names");
            }

            int quietSounds = 0;
            using (ReminderOverlay overlay = new ReminderOverlay(delegate { quietSounds++; }))
            {
                overlay.Preferences = new AppPreferences { QuietEnabled = true, QuietStart = 0, QuietEnd = 0 };
                overlay.SetActive(true); check(overlay.Active && quietSounds == 0, "Quiet hours retain visual attention without playing audio");
                overlay.SetActive(false); overlay.Preferences.QuietEnabled = false; overlay.Preferences.Volume = 0;
                overlay.SetActive(true); check(quietSounds == 0, "Muted application volume suppresses reminder playback"); overlay.SetActive(false);
            }

            Capture(directory, check);
            Rectangle workArea = Screen.PrimaryScreen.WorkingArea;
            Rectangle anchor = new Rectangle(workArea.Left + 160, workArea.Bottom, 55, 60);
            Point cursor = new Point(anchor.Left + 20, anchor.Top + 20);
            DateTime hoverClock = DateTime.UtcNow;
            using (TaskbarMeetingTip tip = new TaskbarMeetingTip(delegate { return anchor; }, delegate { return cursor; }, delegate { return hoverClock; }))
            {
                IntPtr foreground = GetForegroundWindow();
                tip.UpdateTip("Через 12 мин · Синтетическая встреча", true); check(!tip.Visible, "Taskbar meeting tooltip waits for deliberate hover");
                hoverClock = hoverClock.AddSeconds(1); tip.UpdateTip("Через 12 мин · Синтетическая встреча", true);
                long flags = GetWindowLongPtr(tip.Handle, -20).ToInt64();
                check(tip.Visible && (flags & 0x08000080) == 0x08000080 && (flags & 0x40000) == 0 && GetForegroundWindow() == foreground,
                    "Native next-meeting tooltip shows without taking foreground focus or adding a taskbar button");
                Save(tip, directory, "features-taskbar-tooltip.png");
                Rectangle bounds = tip.Bounds; int moves = 0; tip.LocationChanged += delegate { moves++; };
                tip.UpdateTip("Через 12 мин · Синтетическая встреча", true);
                check(tip.Bounds == bounds && moves == 0 && !tip.Bounds.IntersectsWith(anchor), "Taskbar tooltip stays still and does not cover the pinned icon");
                cursor = new Point(anchor.Right + 20, anchor.Top); tip.UpdateTip("Занято", true);
                check(!tip.Visible, "Leaving the date icon immediately hides its next-meeting tooltip");
            }
            await Task.FromResult(0);
        }

        private static int Peak(byte[] wave)
        { int peak = 0; for (int i = 44; i < wave.Length; i += 2) peak = Math.Max(peak, Math.Abs((int)BitConverter.ToInt16(wave, i))); return peak; }

        private static CalendarEvent Event(string id, DateTime start, DateTime end)
        {
            return new CalendarEvent { Id = id, CalendarId = id == "b" ? "b" : "a", CalendarName = id == "b" ? "Личный календарь" : "Команда",
                CalendarColor = id == "b" ? "#7950B3" : "#2563A6", Title = "Встреча — синтетический пример " + id,
                Start = CalendarEvent.Boundary(start, TimeZoneInfo.Local).ToString("o"), End = CalendarEvent.Boundary(end, TimeZoneInfo.Local).ToString("o"),
                Url = "https://calendar.google.com/calendar/event?eid=fixture-" + id, MeetUrl = "https://meet.google.com/abc-defg-hij" };
        }
        private static AppPreferences Options()
        {
            AppPreferences value = new AppPreferences();
            value.Calendars.Add(new CalendarChoice { Id = "a", Name = "Команда", Color = "#2563A6" });
            value.Calendars.Add(new CalendarChoice { Id = "b", Name = "Личный календарь", Color = "#7950B3" });
            return value;
        }
        private static void Capture(string directory, Action<bool, string> check)
        {
            DateTime date = new DateTime(2026, 11, 12);
            List<CalendarEvent> events = new List<CalendarEvent> { Event("a", date.AddHours(10), date.AddHours(11)), Event("b", date.AddHours(10).AddMinutes(30), date.AddHours(11).AddMinutes(30)), Event("next", date.AddDays(1).AddHours(9), date.AddDays(1).AddHours(10)) };
            using (CalendarPopup popup = new CalendarPopup(delegate { return date; }, delegate { }))
            {
                popup.ShowAt(new Point(400, 500), Screen.PrimaryScreen.WorkingArea);
                AppPreferences options = Options(); popup.Panel.SetPreferences(options);
                popup.Panel.SetData(new CalendarViewData { Connection = CalendarConnection.Connected, HasSnapshot = true, Events = events, Message = "Календари Google" });
                popup.Panel.SetUpcoming(events, true);
                popup.Panel.WeekButton.PerformClick(); check(popup.Panel.EventList.Items.Count == 3, "Week agenda includes the next day's meeting");
                popup.Panel.WeekButton.PerformClick(); check(popup.Panel.EventList.Items.Count == 2, "Day mode restores only the selected day's meetings");
                options.DisabledCalendars.Add("b"); popup.Panel.SetPreferences(options);
                check(popup.Panel.EventList.Items.Count == 1, "Calendar selection filters displayed cards");
                options.DisabledCalendars.Clear(); popup.Panel.SetPreferences(options);
                CalendarEvent outside = Event("outside", date.AddDays(7).AddHours(10), date.AddDays(7).AddHours(11));
                var withOutside = events.Concat(new[] { outside }).ToList();
                popup.Panel.SetData(new CalendarViewData { Connection = CalendarConnection.Connected, HasSnapshot = true, Events = withOutside });
                popup.Panel.WeekButton.PerformClick(); check(popup.Panel.EventList.Items.Count == 3, "Week range excludes the eighth day");
                popup.Panel.WeekButton.PerformClick(); popup.Panel.SetData(new CalendarViewData { Connection = CalendarConnection.Connected, HasSnapshot = true, Events = events });
                foreach (int dpi in new[] { 96, 120, 144 })
                foreach (bool dark in new[] { false, true })
                {
                    popup.Panel.SetDpi(dpi); popup.ViewWindow.ClientSize = popup.Panel.NaturalSize; popup.Panel.ApplyTheme(dark); Application.DoEvents();
                    Save(popup.Panel, directory, "features-agenda-" + (dark ? "dark" : "light") + "-" + dpi + ".png");
                    popup.OpenPreferences(); popup.PreferencesPage.BeginEdit(options); popup.PreferencesPage.SetDpi(dpi, dark);
                    for (int page = 0; page < 3; page++)
                    {
                        popup.PreferencesPage.ShowPage(page); popup.PreferencesPage.SetDpi(dpi, dark);
                        check(CalendarPanel.Descendants(popup.PreferencesPage).All(c => c.Parent.ClientRectangle.Contains(c.Bounds)),
                            "Preferences page " + page + " controls fit at " + dpi + " DPI, dark=" + dark);
                        Save(popup.PreferencesPage, directory, "features-settings-" + page + "-" + (dark ? "dark" : "light") + "-" + dpi + ".png");
                    }
                    popup.ShowCalendarSettings(); popup.Panel.ShowSettings(false);
                }
                options.PrivateMode = true; popup.Panel.SetPreferences(options); popup.Panel.SetDpi(96); popup.ViewWindow.ClientSize = popup.Panel.NaturalSize;
                popup.Panel.ApplyTheme(false); Save(popup.Panel, directory, "features-private.png");
                popup.Panel.WeekButton.PerformClick(); options.PrivateMode = false; popup.Panel.SetPreferences(options);
                Save(popup.Panel, directory, "features-week.png");
                popup.OpenPreferences(); popup.PreferencesPage.BeginEdit(options);
                CalendarPanel.Descendants(popup.PreferencesPage).OfType<CheckBox>().Single(c => c.Text.StartsWith("Приватный", StringComparison.Ordinal)).Checked = true;
                CalendarPanel.Descendants(popup.PreferencesPage).OfType<ComboBox>().Single(c => c.AccessibleName == "Мелодия напоминания").SelectedIndex = 2;
                CalendarPanel.Descendants(popup.PreferencesPage).OfType<TrackBar>().Single().Value = 45;
                popup.PreferencesPage.Controls.OfType<Button>().Single(button => button.Text == "Сохранить").PerformClick();
                check(popup.PreferencesPage.Draft.PrivateMode && popup.PreferencesPage.Draft.SoundStyle == 2 && popup.PreferencesPage.Draft.Volume == 45
                    && !popup.Panel.EventList.Items[0].ToString().Contains(events[0].Title), "Preferences Save applies edited controls and masks existing meeting metadata immediately");
                check(popup.Visible && popup.Panel.SettingsVisible, "Saving preferences returns to settings in the existing flyout");
            }
        }
        private static void Save(Control control, string directory, string name)
        { using (Bitmap image = new Bitmap(control.Width, control.Height)) { control.DrawToBitmap(image, control.ClientRectangle); image.Save(Path.Combine(directory, name), ImageFormat.Png); } }
        private sealed class RangeSource : ICalendarSource
        {
            internal int Calls;
            public bool Connected { get { return true; } }
            public string CacheIdentity { get { return "feature-range-fixture"; } }
            public Task<List<CalendarEvent>> LoadAsync(DateTime first, DateTime end, TimeZoneInfo zone, System.Threading.CancellationToken cancellation)
            { Calls++; return Task.FromResult(new List<CalendarEvent>()); }
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
    }
}
