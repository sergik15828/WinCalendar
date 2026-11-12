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
    }

    internal sealed class ReminderEngine
    {
        private readonly Func<DateTimeOffset> clock;
        private readonly string path;
        private ReminderState state;
        private DateTime savedAt;
        internal bool Active { get { return state.Pending.Count > 0; } }
        internal DateTime LastCheckUtc { get { return state.LastCheckUtc; } }

        internal ReminderEngine(string directory, string identity, Func<DateTimeOffset> now)
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
        }

        internal void Evaluate(IEnumerable<CalendarEvent> events, DateTime? includeSince = null)
        {
            DateTime now = clock().UtcDateTime;
            DateTime after = state.LastCheckUtc > now ? now : state.LastCheckUtc < now.AddDays(-1) ? now.AddDays(-1) : state.LastCheckUtc;
            if (includeSince.HasValue && includeSince.Value < after) after = includeSince.Value < now.AddDays(-1) ? now.AddDays(-1) : includeSince.Value;
            bool changed = false;
            foreach (CalendarEvent item in events)
            {
                if (item.PopupReminderMinutes == null || item.ReminderEnd.UtcDateTime <= now) continue;
                DateTime start = item.ReminderStart.UtcDateTime;
                foreach (int minutes in item.PopupReminderMinutes)
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
            state.Pending.Clear(); state.LastCheckUtc = clock().UtcDateTime; Save();
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
