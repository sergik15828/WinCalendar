using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace WinCalendar
{
    internal sealed class CalendarSnapshot
    {
        public DateTime UpdatedUtc { get; set; }
        public List<CalendarEvent> Events { get; set; }
    }

    internal sealed class CalendarCache
    {
        private readonly string directory;
        internal CalendarCache(string path) { directory = path; }

        private string PathFor(string identity, DateTime first, TimeZoneInfo zone)
        {
            string key = identity + ":" + first.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture) + ":" + zone.Id;
            using (SHA256 hash = SHA256.Create())
                return Path.Combine(directory, BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-", "") + ".bin");
        }

        internal CalendarSnapshot Read(string identity, DateTime first, TimeZoneInfo zone)
        {
            if (String.IsNullOrEmpty(identity)) return null;
            try
            {
                string path = PathFor(identity, first, zone);
                if (!File.Exists(path)) return null;
                byte[] plaintext = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
                try { return new JavaScriptSerializer().Deserialize<CalendarSnapshot>(Encoding.UTF8.GetString(plaintext)); }
                finally { Array.Clear(plaintext, 0, plaintext.Length); }
            }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
            catch (CryptographicException) { return null; }
            catch (ArgumentException) { return null; }
            catch (InvalidOperationException) { return null; }
        }

        internal bool Write(string identity, DateTime first, TimeZoneInfo zone, CalendarSnapshot snapshot)
        {
            if (String.IsNullOrEmpty(identity)) return false;
            try
            {
                Directory.CreateDirectory(directory);
                byte[] plaintext = Encoding.UTF8.GetBytes(Json.Serialize(snapshot));
                byte[] encrypted;
                try { encrypted = ProtectedData.Protect(plaintext, null, DataProtectionScope.CurrentUser); }
                finally { Array.Clear(plaintext, 0, plaintext.Length); }
                string path = PathFor(identity, first, zone), temporary = path + ".tmp";
                File.WriteAllBytes(temporary, encrypted);
                if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
                // Bound retained private event data without changing current-month data.
                foreach (string file in Directory.GetFiles(directory, "*.bin"))
                    if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-60)) File.Delete(file);
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (CryptographicException) { return false; }
        }

        internal void Clear()
        {
            if (!Directory.Exists(directory)) return;
            foreach (string file in Directory.GetFiles(directory, "*.bin*")) File.Delete(file);
        }
    }
}
