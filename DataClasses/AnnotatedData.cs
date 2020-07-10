using System;
using System.Collections.Generic;
using System.Text;

using Levrum.Utils;

namespace Levrum.Data.Classes
{
    public abstract class AnnotatedData
    {
        private InternedDictionary<string, object> m_data = null;

        public InternedDictionary<string, object> Data
        {
            get
            {
                if (m_data == null)
                    m_data = new InternedDictionary<string, object>();

                return m_data;
            }

            protected set
            {
                m_data = value;
            }
        }

        public void RemoveDataValue(string key)
        {
            if (Data.ContainsKey(key))
            {
                Data.Remove(key);
            }
        }

        public object GetDataValue(string key)
        {
            return Data.GetValue(key);
        }

        public object[] GetDataValues(string[] keys)
        {
            object[] output = new object[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                object value;
                output[i] = Data.TryGetValue(keys[i], out value) ? value : null;
            }

            return output;
        }

        public void SetDataValue(string key, object value)
        {
            try
            {
                Data[key] = value;
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc, "Exception setting data value", false);
            }
        }

        public void SetDataValues(string[] keys, object[] values)
        {
            try
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    var key = keys[i];
                    if (key == null)
                    {
                        continue;
                    }
                    object value = null;
                    if (i < values.Length)
                    {
                        value = values[i];
                    }
                    if (!Data.ContainsKey(key) && value != null) { Data.Add(key, value); }
                    else if (Data.ContainsKey(key))
                    {
                        if (value != null)
                        {
                            Data[key] = value;
                        }
                        else
                        {
                            Data.Remove(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Exception setting data values", false);
            }
        }

        public void SetDataDateTime(string key, int year = 1901, int month = 1, int day = 1, int hour = 0, int minute = 0, int second = 0, int millisecond = 0)
        {
            DateTime value = new DateTime(year, month, day, hour, minute, second, millisecond);
            if (!Data.ContainsKey(key)) { Data.Add(key, value); }
            else { Data[key] = value; }
        }

        public object[] GetDataDateTimeComponents(string key)
        {
            object[] output = new object[7] { 0, 0, 0, 0, 0, 0, 0 };

            DateTime value = DateTime.MinValue;
            if (Data.ContainsKey(key))
            {
                if (Data[key] is DateTime)
                {
                    value = (DateTime)Data[key];
                }
                DateTime.TryParse(Data[key].ToString(), out value);
                if (value != DateTime.MinValue)
                {
                    output[0] = value.Year;
                    output[1] = value.Month;
                    output[2] = value.Day;
                    output[3] = value.Hour;
                    output[4] = value.Minute;
                    output[5] = value.Second;
                    output[6] = value.Millisecond;
                }
            }

            return output;
        }

        public void Intern()
        {
            if (m_data != null && m_data.Count > 0)
            {
                m_data.Intern();
            }
            else
            {
                m_data = null;
            }
        }
    }
}
