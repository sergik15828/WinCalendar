using System;

namespace WinCalendar
{
    internal sealed class CalendarState
    {
        internal static readonly DateTime Minimum = new DateTime(2, 1, 1);
        internal static readonly DateTime Maximum = new DateTime(9998, 12, 31);
        internal DateTime Today { get; private set; }
        internal DateTime Selected { get; private set; }
        internal DateTime Month { get; private set; }

        internal CalendarState(DateTime today) { Today = today.Date; Select(today); }

        internal void Select(DateTime date)
        {
            Selected = date.Date < Minimum ? Minimum : date.Date > Maximum ? Maximum : date.Date;
            Month = new DateTime(Selected.Year, Selected.Month, 1);
        }

        internal void MoveMonth(int amount)
        {
            int index = (Month.Year - 1) * 12 + Month.Month - 1 + amount;
            int minimum = (Minimum.Year - 1) * 12;
            int maximum = (Maximum.Year - 1) * 12 + 11;
            index = Math.Max(minimum, Math.Min(maximum, index));
            int year = index / 12 + 1, month = index % 12 + 1;
            Select(new DateTime(year, month, Math.Min(Selected.Day, DateTime.DaysInMonth(year, month))));
        }

        internal void GoToToday() { Select(Today); }

        internal void UpdateToday(DateTime today)
        {
            bool following = Selected == Today;
            Today = today.Date;
            if (following) GoToToday();
        }

        internal DateTime[] Days()
        {
            int offset = ((int)Month.DayOfWeek + 6) % 7;
            DateTime start = Month.AddDays(-offset);
            DateTime[] days = new DateTime[42];
            for (int i = 0; i < days.Length; i++) days[i] = start.AddDays(i);
            return days;
        }
    }
}
