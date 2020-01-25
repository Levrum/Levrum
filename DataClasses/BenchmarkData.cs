using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.Data.Classes
{
    public class BenchmarkData : AnnotatedData
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

        public BenchmarkData(string name = "", double value = Double.NaN, string details = "")
        {
            Name = name;
            Value = value;
            Details = details;
        }


        public bool HasAbsoluteTimestamp()
        {
            if (null==this.Data) { return (false); }
            if (!Data.ContainsKey("RawData")) { return (false); }
            object oval = Data["RawData"];
            return (oval is DateTime);
        }
        public DateTime GetAbsoluteTimestamp()
        {
            DateTime errval = DateTime.MinValue;
            if (!HasAbsoluteTimestamp()) { return (errval); }
            DateTime retval = (DateTime)Data["RawData"];
            return (retval);
        }
    }
}
