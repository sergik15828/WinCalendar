using System;
using System.Globalization;

namespace WinCalendar
{
    internal static class DateText
    {
        internal static CultureInfo Culture { get { return UiText.Culture; } }

        internal static string WeekdayAbbreviation(DateTime date)
        {
            string[] weekdays = { "ВС", "ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ" };
            if (UiText.English) weekdays = new[] { "SU", "MO", "TU", "WE", "TH", "FR", "SA" };
            return weekdays[(int)date.DayOfWeek];
        }
    }
}
