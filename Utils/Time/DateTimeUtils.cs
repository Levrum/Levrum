using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.Utils.Time
{
    public enum Periods { TimeSpan, HourOfDay, DayOfWeek, MonthOfYear, Day, Month, Year }

    public class DateTimeUtils
    {
        public static double GetPeriodMinutes(Periods period, DateTime start, DateTime end, int value = 0)
        {
            try
            {
                if (period == Periods.HourOfDay)
                {
                    int hours = 0;
                    DateTime newDate = start;
                    while (newDate < end)
                    {
                        if (newDate.Hour == value)
                        {
                            hours++;
                        }
                        newDate = newDate.AddHours(1);
                    }

                    return hours * 60;
                } else if (period == Periods.DayOfWeek)
                {
                    int hours = 0;
                    DateTime newDate = start;
                    while (newDate < end)
                    {
                        if (newDate.DayOfWeek == (DayOfWeek)value)
                        {
                            hours += 24;
                        }
                        newDate = newDate.AddDays(1);
                    }

                    return hours * 60;
                } else if (period == Periods.MonthOfYear)
                {
                    int hours = 0;
                    DateTime newDate = start;
                    while (newDate < end)
                    {
                        if (newDate.Month == value) {
                            hours += 24;
                        }
                        newDate = newDate.AddDays(1);
                    }

                    return hours * 60;
                } else if (period == Periods.Day)
                {
                    DateTime searchDate = GetDateTimeFromYMDKey(value);
                    if (searchDate > start && searchDate < end)
                    {
                        return 24 * 60;
                    }
                } else if (period == Periods.Month)
                {
                    DateTime searchDate = GetDateTimeFromYMKey(value);
                    if (searchDate >= start)
                    {
                        int hours = 0;
                        DateTime newDate = searchDate;
                        while (newDate < end && newDate.Month == searchDate.Month)
                        {
                            hours += 24;
                            newDate = newDate.AddDays(1);
                        }

                        return hours * 60;
                    }
                } else if (period == Periods.Year)
                {
                    if (start.Year < value && end.Year > value)
                    {
                        if (DateTime.IsLeapYear(value))
                        {
                            return 366 * 24 * 60;
                        } 
                        else
                        {
                            return 365 * 24 * 60;
                        }
                    } else if (start.Year == value)
                    {
                        int hours = 0;
                        DateTime newDate = start;
                        while (newDate < end && newDate.Year == value)
                        {
                            hours += 24;
                            newDate = newDate.AddDays(1);
                        }

                        return hours * 60;
                    } else if (value < start.Year || value > end.Year)
                    {
                        return 0;
                    } else
                    {
                        int hours = 0;
                        DateTime newDate = new DateTime(value, 1, 1);
                        while (newDate < end)
                        {
                            hours += 24;
                            newDate = newDate.AddDays(1);
                        }

                        return hours * 60;
                    }
                }

                return (end - start).TotalMinutes;
            } catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

            return double.NaN;
        }

        public static double GetTimeSpentInPeriod(Periods period, DateTime start, DateTime end, int value = 0)
        {
            // I owe Heidi like four or five really big thank yous for helping me figure this out with her code.
            DateTime searchDate, current, nextHour;
            DateTime newDate = start;
            double minutes = 0.0;

            current = start;
            nextHour = new DateTime(current.Year, current.Month, current.Day, current.Hour, 0, 0).AddHours(1);

            switch (period)
            {
                case Periods.HourOfDay:
                    if (end < nextHour)
                    {
                        if (start.Hour == value)
                        {
                            return (end - start).TotalMinutes;
                        } else
                        {
                            return 0.0;
                        }
                    }

                    while (end > nextHour)
                    {
                        if (current.Hour == value)
                        {
                            minutes += (nextHour - current).TotalMinutes;
                        }

                        current = nextHour;
                        nextHour = nextHour.AddHours(1);
                    }

                    if (end.Hour == value)
                    {
                        minutes += (end - current).TotalMinutes;
                    }

                    return minutes;
                case Periods.DayOfWeek:
                    DayOfWeek dow = (DayOfWeek)value;
                    if (end < nextHour)
                    {
                        if (start.DayOfWeek == dow)
                        {
                            return (end - start).TotalMinutes;
                        } else
                        {
                            return 0.0;
                        }
                    }

                    while (end > nextHour)
                    {
                        if (current.DayOfWeek == dow)
                        {
                            minutes += (nextHour - current).TotalMinutes;
                        }

                        current = nextHour;
                        nextHour = nextHour.AddHours(1);
                    }

                    if (end.DayOfWeek == dow)
                    {
                        minutes += (end - current).TotalMinutes;
                    }

                    return minutes;
                case Periods.MonthOfYear:
                        if (end < nextHour)
                    {
                        if (start.Month == value)
                        {
                            return (end - start).TotalMinutes;
                        } else
                        {
                            return 0.0;
                        }
                    }

                    while (end > nextHour)
                    {
                        if (current.Month == value)
                        {
                            minutes += (nextHour - current).TotalMinutes;
                        }

                        current = nextHour;
                        nextHour = nextHour.AddHours(1);
                    }

                    if (end.Month == value)
                    {
                        minutes += (end - current).TotalMinutes;
                    }

                    return minutes;
                case Periods.Day:
                    searchDate = GetDateTimeFromYMDKey(value);
                    if (end < nextHour) 
                    {
                        if (DateTimesAreSameDay(current, searchDate))
                        {
                            return (end - start).TotalMinutes;
                        } else
                        {
                            return 0.0;
                        }
                    }
                    
                    while (end > nextHour)
                    {
                        if (DateTimesAreSameDay(current, searchDate))
                        {
                            minutes += (nextHour - current).TotalMinutes;
                        }

                        current = nextHour;
                        nextHour = nextHour.AddHours(1);
                    }

                    if (DateTimesAreSameDay(end, searchDate))
                    {
                        minutes += (end - current).TotalMinutes;
                    }

                    return minutes;
                case Periods.Month:
                    searchDate = GetDateTimeFromYMKey(value);
                    if (end < nextHour)
                    {
                        if (DateTimesAreSameMonth(current, searchDate))
                        {
                            return (end - start).TotalMinutes;
                        } else
                        {
                            return 0.0;
                        }
                    }

                    while (end > nextHour)
                    {
                        if (DateTimesAreSameMonth(current, searchDate))
                        {
                            minutes += (nextHour - current).TotalMinutes;
                        }

                        current = nextHour;
                        nextHour = nextHour.AddHours(1);
                    }

                    if (DateTimesAreSameMonth(end, searchDate))
                    {
                        minutes += (end - current).TotalMinutes;
                    }

                    return minutes;
                case Periods.Year:
                    if (end < nextHour)
                    {
                        if (current.Year == value)
                        {
                            return (end - start).TotalMinutes;
                        } else
                        {
                            return 0.0;
                        }
                    }

                    while (end > nextHour)
                    {
                        if (current.Year == value)
                        {
                            minutes += (nextHour - current).TotalMinutes;
                        }

                        current = nextHour;
                        nextHour = nextHour.AddHours(1);
                    }

                    if (end.Year == value)
                    {
                        minutes += (end - current).TotalMinutes;
                    }

                    return minutes;
                case Periods.TimeSpan:
                default:
                    return (end - start).TotalMinutes;
            }
        }

        public static bool DateTimesAreSameDay(DateTime date1, DateTime date2)
        {
            return date1.Day == date2.Day && date1.Month == date2.Month && date1.Year == date2.Year;
        }

        public static bool DateTimesAreSameMonth(DateTime date1, DateTime date2)
        {
            return date1.Year == date2.Year && date1.Month == date2.Month;
        }

        public static int GetYMDKey(DateTime date)
        {
            return (date.Year * 10000) + (date.Month * 100) + date.Day;
        }

        public static int GetYearFromYMDKey(int key)
        {
            return (key / 10000);
        }

        public static int GetMonthFromYMDKey(int key)
        {
            int monthDay = key % 10000;

            return (monthDay / 100);
        }

        public static int GetDayFromYMDKey(int key)
        {
            return key % 100;
        }

        public static DateTime GetDateTimeFromYMDKey(int key)
        {
            return new DateTime(GetYearFromYMDKey(key), GetMonthFromYMDKey(key), GetDayFromYMDKey(key));
        }

        public static int GetYMKey(DateTime date)
        {
            return (date.Year * 100) + date.Month;
        }

        public static int GetMonthFromYMKey(int key)
        {
            return key % 100;
        }

        public static int GetYearFromYMKey(int key)
        {
            return (key / 100);
        }

        public static DateTime GetDateTimeFromYMKey(int key)
        {
            return new DateTime(GetYearFromYMKey(key), GetMonthFromYMKey(key), 1);
        }
    }
}
