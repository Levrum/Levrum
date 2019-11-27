using System;
using System.Collections.Generic;
using System.Text;

using Levrum.Data.Sources;

namespace Levrum.Data.Map
{
    public class ColumnMapping
    {
        public string ColumnName { get; set; }
        public IDataSource DataSource { get; set; }
    }
}
