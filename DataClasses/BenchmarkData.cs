using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.DataClasses
{
    public class BenchmarkData
    {
        public ResponseData Parent { get; set; }

        private string m_name = string.Empty;
        private string m_details = string.Empty;

        public string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                m_name = string.Intern(value);
            }
        }

        public double Value { get; set; } = Double.NaN;

        public string Details
        {
            get
            {
                return m_details;
            }
            set
            {
                m_details = string.Intern(value);
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

        public void Intern()
        {
            if (m_data != null && m_data.Count > 0)
            {
                m_data.Intern();
            } else
            {
                m_data = null;
            }
        }

        public BenchmarkData(string name = "", double value = Double.NaN, string details = "")
        {
            Name = name;
            Value = value;
            Details = details;
        }
    }
}
