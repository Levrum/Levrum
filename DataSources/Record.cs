using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Levrum.Data.Sources
{
    public class Record
    {
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public List<string> Columns { get { return Data.Keys.ToList(); } }
        public List<object> Values { get { return Data.Values.ToList(); } }

        public void AddValue(string column, object value)
        {
            Data[column] = value;
        }

        public object GetValue(string column)
        {
            if (Data.ContainsKey(column)) {
                return Data[column];
            }

            return null;
        }
    }
}
