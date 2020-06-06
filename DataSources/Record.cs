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

        public object GetValue(string ColumnName)
        {
            if (null==ColumnName) { return (null); }

            if (Data.ContainsKey(ColumnName)) {
                return Data[ColumnName];
            }

            return null;
        }

        public bool HasColumn(string columnName)
        {
            if (null==Data) { return (false); }
            return (Data.ContainsKey(columnName));
        }
    }
}
