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

        public void SetDataValue(string key, object value)
        {
            Data[key] = value;
        }

        public object GetDataValue(string key)
        {
            return Data.GetValue(key);
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
