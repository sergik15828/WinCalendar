using System;
using System.Globalization;

namespace WinCalendar
{
    internal static class DateText
    {
        internal static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

        internal static string WeekdayAbbreviation(DateTime date)
        {
            string[] weekdays = { "ВС", "ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ" };
            return weekdays[(int)date.DayOfWeek];
        }
    }
}
