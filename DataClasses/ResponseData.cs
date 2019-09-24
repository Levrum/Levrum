using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.DataClasses
{
    public class ResponseData
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

        public DataSet<BenchmarkData> Benchmarks { get; set; } = new DataSet<BenchmarkData>();

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
            }
            else
            {
                m_data = null;
            }

            foreach (BenchmarkData benchmark in Benchmarks)
            {
                benchmark.Intern();
            }
        }

        public ResponseData(string id = "", BenchmarkData[] benchmarks = null)
        {
            Id = id;
            if (benchmarks != null)
            {
                Benchmarks.AddRange(benchmarks);
            }
        }
    }
}
