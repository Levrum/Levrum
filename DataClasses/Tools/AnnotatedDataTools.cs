using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.Data.Classes.Tools
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

        public void CreateDateTime(AnnotatedData data, object outputKey, object dateValue, object timeValue)
        {
            DateTime dateTime;
            if (dateValue != null && timeValue != null)
            {
                if (DateTime.TryParse(string.Format("{0} {1}", dateValue.ToString(), timeValue.ToString()), out dateTime))
                {
                    data.Data[outputKey.ToString()] = dateTime;
                }
            } else if (dateValue != null)
            {
                if (DateTime.TryParse(dateValue.ToString(), out dateTime))
                {
                    data.Data[outputKey.ToString()] = dateTime;
                }
            } else if (timeValue != null)
            {
                if (DateTime.TryParse(timeValue.ToString(), out dateTime))
                {
                    data.Data[outputKey.ToString()] = dateTime;
                }
            }
        }


        public object CreateDateTime(object dateValue, object timeValue)
        {
            DateTime dateTime;
            if (DateTime.TryParse(string.Format("{0} {1}", dateValue.ToString(), timeValue.ToString()), out dateTime))
            {
                return dateTime;
            }
            else
            {
                return null;
            }
        }
    }
}
