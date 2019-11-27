using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

using Levrum.Data.Sources;
using Levrum.Utils.Data;

namespace Levrum.Data.Map
{
    public class DataMapping
    {
        public string Field { get; set; } = string.Empty;
        public ColumnType ColumnType { get; set; } = ColumnType.stringField;
        public ColumnMapping Column { get; set; } = new ColumnMapping();

        [JsonIgnore]
        public string Info
        {
            get
            {
                string tabs;
                if (Field.Length < 9)
                    tabs = "\t\t\t";
                else if (Field.Length < 17)
                    tabs = "\t\t";
                else
                    tabs = "\t";

                return string.Format("{0}{1}{2}: {3}", Field, tabs, Column.DataSource.Name, Column.ColumnName);
            }
        }

        public DataMapping()
        {

        }

        public DataMapping(string _field)
        {
            Field = _field;
        }

        public DataMapping(string _field, ColumnType _type)
        {
            Field = _field;
            ColumnType = _type;
        }

        public DataMapping(string _field, ColumnType _type, ColumnMapping _column)
        {
            Field = _field;
            ColumnType = _type;
            Column = _column;
        }
    }
}
