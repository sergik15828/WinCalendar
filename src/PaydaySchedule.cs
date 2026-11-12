using System;

namespace WinCalendar
{
    internal static class PaydaySchedule
    {
        internal static DateTime Next(DateTime today, int frequency, int first, int second)
        {
            today = today.Date;
            DateTime month = new DateTime(today.Year, today.Month, 1);
            for (int offset = 0; offset < 2; offset++)
            {
                DateTime current = month.AddMonths(offset);
                DateTime one = new DateTime(current.Year, current.Month, Math.Min(Math.Max(1, Math.Min(31, first)), DateTime.DaysInMonth(current.Year, current.Month)));
                DateTime two = frequency == 2 ? new DateTime(current.Year, current.Month, Math.Min(Math.Max(1, Math.Min(31, second)), DateTime.DaysInMonth(current.Year, current.Month))) : one;
                if (one >= today && two >= today) return one < two ? one : two;
                if (one >= today) return one;
                if (two >= today) return two;
            }
            return today;
        }
        internal static string DaysLabel(int days)
        {
            if (days == 0) return UiText.Get("Сегодня");
            if (UiText.English) return days + (days == 1 ? " day" : " days");
            int last = days % 10, pair = days % 100;
            return days + (pair >= 11 && pair <= 14 ? " дней" : last == 1 ? " день" : last >= 2 && last <= 4 ? " дня" : " дней");
        }
    }
}
