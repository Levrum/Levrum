using System;
using System.Collections.Generic;
using System.Text;

using Levrum.Data.Sources;

namespace Levrum.Data.Map
{
    public class DataMapping
    {
        public string Field { get; set; } = string.Empty;
        public IColumnMapping Column { get; set; } = null;
    }
}
