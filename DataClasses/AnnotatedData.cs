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

        public bool SetDataValue(string key, object value)
        {
            try
            {
                if (!Data.ContainsKey(key)) { Data.Add(key, value); }
                else { Data[key] = value; }
                return (true);
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc, "Exception setting data value", false);
                return (false);
            }
        }

        public bool SetDataValues(string[] keys, object[] values)
        {
            try
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    object value = null;
                    if (i < values.Length)
                    {
                        value = values[i];
                    }
                    if (!Data.ContainsKey(keys[i]) && value != null) { Data.Add(keys[i], value); }
                    else if (Data.ContainsKey(keys[i]))
                    {
                        if (value != null)
                        {
                            Data[keys[i]] = value;
                        }
                        else
                        {
                            Data.Remove(keys[i]);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Exception setting data values", false);
                return false;
            }
        }

        public void SetDataDateTime(string key, int year = 1901, int month = 1, int day = 1, int hour = 0, int minute = 0, int second = 0, int millisecond = 0)
        {
            DateTime value = new DateTime(year, month, day, hour, minute, second, millisecond);
            if (!Data.ContainsKey(key)) { Data.Add(key, value); }
            else { Data[key] = value; }
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
