using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace WinCalendar
{
    internal sealed class ReminderState
    {
        public DateTime LastCheckUtc { get; set; }
        public Dictionary<string, DateTime> Seen { get; set; }
        public Dictionary<string, DateTime> Pending { get; set; }
        public Dictionary<string, DateTime> Acknowledged { get; set; }
        public Dictionary<string, DateTime> Snoozed { get; set; }
        public DateTime? SnoozeAtUtc { get; set; }
        public DateTime AcknowledgedAtUtc { get; set; }
        public int TimingMode { get; set; }
    }

    internal sealed class ReminderEngine
    {
        private readonly Func<DateTimeOffset> clock;
        private readonly string path;
        private ReminderState state;
        private DateTime savedAt;
        private DateTime timingChangedUtc;
        internal bool Active { get { return state.Pending.Count > 0; } }
        internal bool CanSnooze { get { return Active || (state.Acknowledged.Count > 0 && clock().UtcDateTime - state.AcknowledgedAtUtc < TimeSpan.FromMinutes(15)); } }
        internal DateTime LastCheckUtc { get { return state.LastCheckUtc; } }

        internal ReminderEngine(string directory, string identity, Func<DateTimeOffset> now, int timingMode = 0)
        {
            clock = now;
            using (SHA256 hash = SHA256.Create())
                path = Path.Combine(directory, BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(identity))).Replace("-", "") + ".bin");
            try
            {
                if (File.Exists(path)) state = new JavaScriptSerializer().Deserialize<ReminderState>(Encoding.UTF8.GetString(
                    ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser)));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (CryptographicException) { }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
            if (state == null || state.Seen == null || state.Pending == null)
                state = new ReminderState { LastCheckUtc = now().UtcDateTime, Seen = new Dictionary<string, DateTime>(), Pending = new Dictionary<string, DateTime>() };
            if (state.Acknowledged == null) state.Acknowledged = new Dictionary<string, DateTime>();
            if (state.Snoozed == null) state.Snoozed = new Dictionary<string, DateTime>();
            SetTiming(timingMode);
        }
        internal void SetTiming(int mode)
        {
            mode = Math.Max(0, Math.Min(2, mode));
            if (state.TimingMode == mode) return;
            state.TimingMode = mode; timingChangedUtc = state.LastCheckUtc = clock().UtcDateTime; Save();
        }
        internal static IEnumerable<int> MinutesFor(CalendarEvent item, int mode)
        { return mode == 0 || item.AllDay ? (IEnumerable<int>)(item.PopupReminderMinutes ?? new List<int>()) : new[] { mode == 1 ? 5 : 2 }; }

        internal void Evaluate(IEnumerable<CalendarEvent> events, DateTime? includeSince = null)
        {
            DateTime now = clock().UtcDateTime;
            DateTime after = state.LastCheckUtc > now ? now : state.LastCheckUtc < now.AddDays(-1) ? now.AddDays(-1) : state.LastCheckUtc;
            if (includeSince.HasValue && includeSince.Value < after) after = includeSince.Value < now.AddDays(-1) ? now.AddDays(-1) : includeSince.Value;
            if (timingChangedUtc > after) after = timingChangedUtc > now ? now : timingChangedUtc;
            bool changed = false;
            if (state.SnoozeAtUtc.HasValue && state.SnoozeAtUtc.Value <= now)
            {
                foreach (var entry in state.Snoozed) state.Pending[entry.Key] = entry.Value;
                state.Snoozed.Clear(); state.SnoozeAtUtc = null; changed = true;
            }
            foreach (CalendarEvent item in events)
            {
                if (item.ReminderEnd.UtcDateTime <= now) continue;
                DateTime start = item.ReminderStart.UtcDateTime;
                foreach (int minutes in MinutesFor(item, state.TimingMode))
                {
                    DateTime due = start.AddMinutes(-minutes);
                    if (due > now || due <= after) continue;
                    string key = (item.CalendarId ?? "") + "\u001f" + item.Id + "\u001f" + start.Ticks + "\u001f" + minutes;
                    if (state.Seen.ContainsKey(key)) continue;
                    state.Seen[key] = due; state.Pending[key] = due; changed = true;
                }
            }
            state.LastCheckUtc = now;
            foreach (string key in state.Seen.Where(p => p.Value < now.AddDays(-60) && !state.Pending.ContainsKey(p.Key)).Select(p => p.Key).ToArray()) state.Seen.Remove(key);
            if (changed || now - savedAt >= TimeSpan.FromMinutes(1)) Save();
        }

        internal void Acknowledge()
        {
            if (state.Pending.Count > 0)
            {
                state.Acknowledged = new Dictionary<string, DateTime>(state.Pending);
                state.AcknowledgedAtUtc = clock().UtcDateTime;
            }
            state.Pending.Clear(); state.LastCheckUtc = clock().UtcDateTime; Save();
        }

        internal bool Snooze(int minutes)
        {
            if ((minutes != 5 && minutes != 10) || !CanSnooze) return false;
            var batch = Active ? state.Pending : state.Acknowledged;
            foreach (var entry in batch) state.Snoozed[entry.Key] = entry.Value;
            state.Pending.Clear(); state.Acknowledged.Clear();
            state.SnoozeAtUtc = clock().UtcDateTime.AddMinutes(minutes); Save(); return true;
        }

        internal void RemoveDisabled(IEnumerable<string> calendars)
        {
            HashSet<string> disabled = new HashSet<string>(calendars);
            bool changed = false;
            foreach (var collection in new[] { state.Pending, state.Acknowledged, state.Snoozed })
                foreach (string key in collection.Keys.Where(k => disabled.Contains(k.Split('\u001f')[0])).ToArray())
                { collection.Remove(key); changed = true; }
            if (state.Snoozed.Count == 0) state.SnoozeAtUtc = null;
            if (changed) Save();
        }

        private void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                byte[] bytes = Encoding.UTF8.GetBytes(Json.Serialize(state)), encrypted;
                try { encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser); }
                finally { Array.Clear(bytes, 0, bytes.Length); }
                string temporary = path + ".tmp"; File.WriteAllBytes(temporary, encrypted);
                if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
                savedAt = clock().UtcDateTime;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (CryptographicException) { }
        }
    }
}
