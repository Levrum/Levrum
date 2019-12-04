using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.Data.Classes
{
    public class ResponseData : AnnotatedData
    {
        public IncidentData Parent { get; set; } = null;

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

        public DataSet<BenchmarkData> Benchmarks { get; set; }

        public ResponseData()
        {
            Benchmarks = new DataSet<BenchmarkData>(this);
        }

        public ResponseData(string id = "", BenchmarkData[] benchmarks = null)
        {
            Id = id;
            Benchmarks = new DataSet<BenchmarkData>(this);

            if (benchmarks != null)
            {
                Benchmarks.AddRange(benchmarks);
            }
        }
    }
}
