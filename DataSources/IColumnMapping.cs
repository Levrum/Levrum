using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.Data.Sources
{
    public interface IColumnMapping
    {
        string Column { get; set; }
        IDataSource DataSource { get; set; }
    }
}
