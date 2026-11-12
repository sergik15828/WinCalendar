using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WinCalendar
{
    internal static class CalendarTimeZones
    {
        private static readonly Dictionary<string, TimeZoneInfo> Cache = new Dictionary<string, TimeZoneInfo>();
        internal static TimeZoneInfo Resolve(string id)
        {
            if (String.IsNullOrEmpty(id)) return TimeZoneInfo.Local;
            lock (Cache)
            {
                TimeZoneInfo cached;
                if (Cache.TryGetValue(id, out cached)) return cached;
                try { cached = TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch (TimeZoneNotFoundException)
                {
                    try
                    {
                        StringBuilder windows = new StringBuilder(128); int status = 0;
                        int length = ucal_getWindowsTimeZoneID(id, id.Length, windows, windows.Capacity, ref status);
                        cached = length > 0 && status <= 0 ? TimeZoneInfo.FindSystemTimeZoneById(windows.ToString()) : TimeZoneInfo.Local;
                    }
                    catch (DllNotFoundException) { cached = TimeZoneInfo.Local; }
                    catch (EntryPointNotFoundException) { cached = TimeZoneInfo.Local; }
                    catch (TimeZoneNotFoundException) { cached = TimeZoneInfo.Local; }
                }
                Cache[id] = cached; return cached;
            }
        }
        [DllImport("icu.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int ucal_getWindowsTimeZoneID(string id, int length, StringBuilder result, int capacity, ref int status);
    }
}
