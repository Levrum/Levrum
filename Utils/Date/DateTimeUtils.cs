using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.Utils.Date
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
                    if (searchDate > start)
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
