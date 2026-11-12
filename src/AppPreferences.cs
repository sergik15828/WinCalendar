using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace WinCalendar
{
    internal sealed class CalendarChoice
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public override string ToString() { return Name ?? "Календарь"; }
    }

    internal sealed class AppPreferences
    {
        public bool PrivateMode { get; set; }
        public bool SoundEnabled { get; set; }
        public int SoundStyle { get; set; }
        public int Volume { get; set; }
        public bool QuietEnabled { get; set; }
        public int QuietStart { get; set; }
        public int QuietEnd { get; set; }
        public int PulseStrength { get; set; }
        public bool ShowNextTooltip { get; set; }
        public List<string> DisabledCalendars { get; set; }
        public Dictionary<string, string> CalendarColors { get; set; }
        public List<CalendarChoice> Calendars { get; set; }
        public AppPreferences()
        {
            SoundEnabled = true; Volume = 100; QuietStart = 22 * 60; QuietEnd = 8 * 60;
            PulseStrength = 1; ShowNextTooltip = true;
            DisabledCalendars = new List<string>(); CalendarColors = new Dictionary<string, string>(); Calendars = new List<CalendarChoice>();
        }
        internal void Normalize()
        {
            Volume = Math.Max(0, Math.Min(100, Volume)); SoundStyle = Math.Max(0, Math.Min(2, SoundStyle));
            PulseStrength = Math.Max(0, Math.Min(2, PulseStrength));
            QuietStart = Math.Max(0, Math.Min(1439, QuietStart)); QuietEnd = Math.Max(0, Math.Min(1439, QuietEnd));
            if (DisabledCalendars == null) DisabledCalendars = new List<string>();
            if (CalendarColors == null) CalendarColors = new Dictionary<string, string>();
            if (Calendars == null) Calendars = new List<CalendarChoice>();
        }
        internal bool Includes(CalendarEvent item) { return !DisabledCalendars.Contains(item.CalendarId ?? ""); }
        internal bool IsQuiet(DateTime time)
        {
            int minute = time.Hour * 60 + time.Minute;
            return QuietEnabled && (QuietStart == QuietEnd || (QuietStart < QuietEnd ? minute >= QuietStart && minute < QuietEnd : minute >= QuietStart || minute < QuietEnd));
        }
        internal Color ColorFor(CalendarEvent item, Color fallback)
        {
            string color; if (!CalendarColors.TryGetValue(item.CalendarId ?? "", out color)) color = item.CalendarColor;
            return ValidColor(color) ? ColorTranslator.FromHtml(color) : fallback;
        }
        internal static bool ValidColor(string value)
        { return value != null && System.Text.RegularExpressions.Regex.IsMatch(value, "^#[0-9a-fA-F]{6}$"); }
    }

    internal sealed class PreferenceStore
    {
        private readonly string path;
        internal PreferenceStore(string file) { path = file; }
        internal AppPreferences Load()
        {
            try
            {
                if (File.Exists(path))
                {
                    byte[] plain = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
                    try { AppPreferences result = new JavaScriptSerializer().Deserialize<AppPreferences>(Encoding.UTF8.GetString(plain)); if (result != null) { result.Normalize(); return result; } }
                    finally { Array.Clear(plain, 0, plain.Length); }
                }
            }
            catch (IOException) { } catch (UnauthorizedAccessException) { } catch (CryptographicException) { }
            catch (ArgumentException) { } catch (InvalidOperationException) { }
            return new AppPreferences();
        }
        internal void Save(AppPreferences value)
        {
            value.Normalize(); Directory.CreateDirectory(Path.GetDirectoryName(path));
            byte[] plain = Encoding.UTF8.GetBytes(Json.Serialize(value)), encrypted;
            try { encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser); }
            finally { Array.Clear(plain, 0, plain.Length); }
            string temp = path + ".tmp"; File.WriteAllBytes(temp, encrypted);
            if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
        }
    }
}
