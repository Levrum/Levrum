using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.Utils.Time
{
    public enum TimeMeasureType { Instant, Duration }

    public class DateTimeMeasure
    {
        public DateTime DateTime { get; set; } = DateTime.MinValue;
        public TimeSpan TimeSpan { get; set; } = TimeSpan.Zero;
        public TimeMeasureType Type { get; set; } = TimeMeasureType.Instant;

        public DateTimeMeasure()
        {

        }

        public DateTimeMeasure(DateTime dateTime = default, TimeSpan timeSpan = default, TimeMeasureType type = TimeMeasureType.Instant)
        {
            DateTime = dateTime;
            TimeSpan = timeSpan;
            Type = type;
        }

        public static explicit operator DateTime(DateTimeMeasure measure)
        {
            return measure.DateTime;
        }

        public static explicit operator DateTimeMeasure(DateTime dateTime)
        {
            DateTimeMeasure measure = new DateTimeMeasure();
            measure.DateTime = dateTime;
            return measure;
        }

        public static explicit operator TimeSpan(DateTimeMeasure measure)
        {
            return measure.TimeSpan;
        }

        public static explicit operator DateTimeMeasure(TimeSpan timeSpan)
        {
            DateTimeMeasure measure = new DateTimeMeasure();
            measure.TimeSpan = timeSpan;
            measure.Type = TimeMeasureType.Duration;

            return measure;
        }
    }
}
