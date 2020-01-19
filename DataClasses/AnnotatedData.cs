using System;
using System.Collections.Generic;
using System.Text;

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
