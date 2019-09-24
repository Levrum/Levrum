using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Levrum.DataClasses
{
    public class DataSet<T> : List<T>
    {
        private char[] m_id;

        public string Id
        {
            get
            {
                return new string(m_id);
            }
            set
            {
                m_id = value.ToCharArray();
            }
        }

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

        public DataSet()
        {

        }

        public DataSet(string id = "", List<T> items = null, Dictionary<string, object> data = null)
        {
            Id = id;
            if (items != null)
            {
                AddRange(items);
            }

            if (data != null)
            {
                foreach (KeyValuePair<string, object> kvp in data)
                {
                    Data.Add(kvp.Key, kvp.Value);
                }
            }
        }
    }
}
