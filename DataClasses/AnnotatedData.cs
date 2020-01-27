using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.Data.Classes
{
    public abstract class AnnotatedData
    {
        public AnnotatedData Parent { get; set; } = null;

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

        public bool SetDataValue(string key, object value)
        {
            const string fn = "AnnotatedData.SetDataValue()";
            try
            {
                if (!Data.ContainsKey(key)) { Data.Add(key, value); }
                else { Data[key] = value; }
                return (true);
            }
            catch (Exception exc)
            {
                // Event logging goes here
                return (false);
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
