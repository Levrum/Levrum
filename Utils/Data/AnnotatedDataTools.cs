using System;
using System.Collections.Generic;
using System.Text;

using Levrum.Data.Classes;

namespace Levrum.Utils.Data
{
    public class AnnotatedDataTools
    {
        public void MergeDateTime(AnnotatedData data, string outputKey, string dateKey, string timeKey)
        {
            object dateObj = null;
            object timeObj = null;
            DateTime date = DateTime.MinValue; 
            DateTime time = DateTime.MinValue;

            data.Data.TryGetValue(dateKey, out dateObj);
            data.Data.TryGetValue(timeKey, out timeObj);

            if (dateObj != null && dateObj is DateTime)
            {
                date = (DateTime)dateObj;
            }

            if (timeObj != null && timeObj is DateTime)
            {
                time = (DateTime)timeObj;
            }

            if (date == DateTime.MinValue && time != DateTime.MinValue)
            {
                data.Data[outputKey] = time;
            } else if (time == DateTime.MinValue && date != DateTime.MinValue)
            {
                data.Data[outputKey] = date;
            } else if (date != DateTime.MinValue && time != DateTime.MinValue)
            {
                data.Data[outputKey] = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond);
            }
        }
    }
}
