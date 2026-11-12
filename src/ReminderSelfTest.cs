using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinCalendar
{
    internal static class ReminderSelfTest
    {
        internal static async Task RunAsync(string directory, Action<bool, string> check)
        {
            check(TaskbarAnchor.IsOwnButtonName("Приложение WinCalendar закреплено")
                && TaskbarAnchor.IsOwnButtonName("Thursday, 12 November — WinCalendar")
                && TaskbarAnchor.IsOwnButtonName("WinDate - pinned")
                && !TaskbarAnchor.IsOwnButtonName("OtherWinCalendarApp") && !TaskbarAnchor.IsOwnButtonName(null),
                "Reminder anchor recognizes localized pinned-app descriptions without matching partial app names");
            string prefix = "{\"defaultReminders\":[{\"method\":\"popup\",\"minutes\":10},{\"method\":\"email\",\"minutes\":30}],\"items\":[";
            string suffix = ",\"start\":{\"dateTime\":\"2026-11-12T10:00:00+03:00\"},\"end\":{\"dateTime\":\"2026-11-12T11:00:00+03:00\"}}]}";
            CalendarEvent inherited = CalendarPage.Parse(prefix + "{\"id\":\"inherited\",\"reminders\":{\"useDefault\":true}" + suffix).Events.Single();
            check(inherited.PopupReminderMinutes.SequenceEqual(new[] { 10 }), "Event inherits calendar popup reminder and ignores email delivery");
            CalendarEvent custom = CalendarPage.Parse(prefix + "{\"id\":\"custom\",\"reminders\":{\"useDefault\":false,\"overrides\":[{\"method\":\"popup\",\"minutes\":5},{\"method\":\"popup\",\"minutes\":0}]}" + suffix).Events.Single();
            check(custom.PopupReminderMinutes.SequenceEqual(new[] { 5, 0 }), "Custom event reminders replace calendar defaults exactly");
            CalendarEvent disabled = CalendarPage.Parse(prefix + "{\"id\":\"disabled\",\"reminders\":{\"useDefault\":false}" + suffix).Events.Single();
            check(disabled.PopupReminderMinutes.Count == 0, "An event with reminders disabled does not pulse");
            check(CalendarTimeZones.Resolve("Europe/Moscow").GetUtcOffset(new DateTime(2026, 11, 12)) == TimeSpan.FromHours(3)
                && CalendarTimeZones.Resolve("America/New_York").GetUtcOffset(new DateTime(2026, 11, 12)) == TimeSpan.FromHours(-5), "Windows ICU resolves Google calendar time zones including DST rules");

            string statePath = Path.Combine(directory, "reminder-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                DateTimeOffset now = CalendarEvent.Timestamp("2026-11-12T09:49:00+03:00");
                inherited.CalendarId = "fixture-calendar";
                ReminderEngine engine = new ReminderEngine(statePath, "fixture-account", delegate { return now; });
                engine.Evaluate(new[] { inherited });
                check(!engine.Active, "Reminder waits until the exact configured event time");
                now = now.AddMinutes(1); engine.Evaluate(new[] { inherited });
                check(engine.Active, "Inherited reminder fires ten minutes before the event");
                now = now.AddHours(3); engine.Evaluate(new[] { inherited });
                check(engine.Active, "Reminder remains active after the meeting until acknowledged");
                ReminderEngine restored = new ReminderEngine(statePath, "fixture-account", delegate { return now; });
                check(restored.Active, "Unacknowledged reminder survives process restart in encrypted state");
                restored.Acknowledge(); restored.Evaluate(new[] { inherited });
                check(!restored.Active, "Click acknowledgement stops the reminder and prevents replay");

                now = CalendarEvent.Timestamp("2026-11-12T09:54:00+03:00");
                ReminderEngine two = new ReminderEngine(statePath, "second-fixture", delegate { return now; });
                two.Evaluate(new[] { custom }); now = now.AddMinutes(1); two.Evaluate(new[] { custom });
                check(two.Active, "Per-event five-minute override triggers without using the ten-minute default");
                two.Acknowledge(); now = now.AddMinutes(5); two.Evaluate(new[] { custom });
                check(two.Active, "A second configured reminder can trigger after the first is acknowledged");
                now = CalendarEvent.Timestamp("2026-11-12T09:49:00+03:00");
                ReminderEngine quiet = new ReminderEngine(statePath, "quiet-fixture", delegate { return now; });
                quiet.Evaluate(new[] { disabled }); now = now.AddMinutes(10); quiet.Evaluate(new[] { disabled });
                check(!quiet.Active, "Disabled event reminders never produce an animation");
                ReminderEngine fresh = new ReminderEngine(statePath, "fresh-fixture", delegate { return now; });
                fresh.Evaluate(new[] { inherited });
                check(!fresh.Active, "First launch does not replay all old reminders");
                now = CalendarEvent.Timestamp("2026-11-12T09:48:00+03:00");
                DateTime fetchedAt = now.UtcDateTime;
                ReminderEngine late = new ReminderEngine(statePath, "late-fetch-fixture", delegate { return now; });
                late.Evaluate(new CalendarEvent[0]); now = now.AddMinutes(2).AddSeconds(30); late.Evaluate(new CalendarEvent[0]);
                late.Evaluate(new[] { inherited }, fetchedAt);
                check(late.Active, "A newly fetched reminder due between background refreshes is not lost");
                now = CalendarEvent.Timestamp("2026-11-12T04:59:00Z");
                CalendarEvent allDay = new CalendarEvent { Id = "all-day-fixture", AllDay = true, Start = "2026-11-13", End = "2026-11-14",
                    CalendarTimeZone = "America/New_York", PopupReminderMinutes = new List<int> { 1440 } };
                ReminderEngine day = new ReminderEngine(statePath, "all-day-fixture", delegate { return now; });
                day.Evaluate(new[] { allDay }); now = now.AddMinutes(1); day.Evaluate(new[] { allDay });
                check(day.Active, "All-day reminder uses the event calendar's time zone rather than Windows local midnight");
            }
            finally { if (Directory.Exists(statePath)) Directory.Delete(statePath, true); }

            CaptureFrames(directory);
            int soundCount = 0;
            using (ReminderOverlay overlay = new ReminderOverlay(delegate { soundCount++; }))
            {
                long style = GetWindowLongPtr(overlay.Handle, -20).ToInt64();
                check((style & 0x080800a0) == 0x080800a0 && (style & 0x40000) == 0 && !overlay.ShowInTaskbar,
                    "Reminder accent is layered, click-through, non-activating and has no taskbar button");
                check(SendMessage(overlay.Handle, 0x84, IntPtr.Zero, IntPtr.Zero).ToInt64() == -1, "Reminder hit test passes clicks to the pinned icon underneath");
                DateTime iconWritten = File.Exists(ShellIntegration.IconPath) ? File.GetLastWriteTimeUtc(ShellIntegration.IconPath) : DateTime.MinValue;
                Task anchor = TaskbarAnchor.RefreshAsync(); await Task.WhenAny(anchor, Task.Delay(4000));
                overlay.SetActive(true); await Task.Delay(140); overlay.SetActive(true);
                check(soundCount == 1, "Reminder sounds once when its pulse starts, without repeating during animation");
                overlay.SetActive(false); overlay.SetActive(true); overlay.SetActive(false);
                check(soundCount == 2, "A new reminder pulse can sound again after acknowledgement");
                check(!overlay.Active && !overlay.Visible, "Stopping a reminder removes its overlay immediately");
                check(!File.Exists(ShellIntegration.IconPath) || File.GetLastWriteTimeUtc(ShellIntegration.IconPath) == iconWritten,
                    "Animation does not rewrite the pinned ICO or invalidate Explorer's icon cache");
            }
        }

        private static void CaptureFrames(string directory)
        {
            using (Bitmap sheet = new Bitmap(320, 160))
            using (Graphics graphics = Graphics.FromImage(sheet))
            using (Bitmap icon = CalendarIcon.Draw(new DateTime(2026, 11, 12), 32))
            {
                graphics.Clear(Color.FromArgb(238, 240, 244));
                graphics.FillRectangle(Brushes.Black, 0, 80, 320, 80);
                for (int row = 0; row < 2; row++)
                for (int column = 0; column < 4; column++)
                {
                    graphics.DrawImageUnscaled(icon, column * 80 + 24, row * 80 + 24);
                    using (Bitmap frame = ReminderOverlay.DrawFrame(64, column / 4.0)) graphics.DrawImageUnscaled(frame, column * 80 + 8, row * 80 + 8);
                }
                sheet.Save(Path.Combine(directory, "reminder-animation-frames.png"), ImageFormat.Png);
            }
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    }
}
