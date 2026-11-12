using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WinCalendar
{
    internal static class GoogleReminderRules
    {
        internal static List<int> PopupMinutes(object items)
        {
            List<int> result = new List<int>();
            IEnumerable sequence = items as IEnumerable;
            if (sequence == null) return result;
            foreach (object item in sequence)
            {
                Dictionary<string, object> reminder = item as Dictionary<string, object>;
                object minutes;
                if (reminder == null || Json.Text(reminder, "method") != "popup" || !reminder.TryGetValue("minutes", out minutes)) continue;
                int value;
                if (Int32.TryParse(Convert.ToString(minutes, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                    && value >= 0 && value <= 40320) result.Add(value);
            }
            return result.Distinct().OrderByDescending(m => m).ToList();
        }
    }
}
