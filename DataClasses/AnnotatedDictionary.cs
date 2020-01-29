using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.Data.Classes
{
    public class AnnotatedDictionary<T1, T2> : Dictionary<T1, T2>
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
    }
}
