using System;
using System.Collections.Generic;
using System.Text;

using Levrum.Data.Sources;

using Newtonsoft.Json;

namespace Levrum.Data.Map
{
    public class ColumnMapping
    {
        public string ColumnName { get; set; }
        public IDataSource DataSource { get; set; }

        [JsonIgnore]
        public string ShortColumnName 
        { 
            get
            {
                if(ColumnName.Length <= 30)
                {
                    return ColumnName;
                }
                
                if (ColumnName.IndexOf('/') != -1)
                {
                    string[] nodeNames = ColumnName.Split('/');
                    return string.Format(".../{0}", nodeNames[nodeNames.Length - 1]);
                }

                return string.Format("{0}...{1}", ColumnName.Substring(0, 10), ColumnName.Substring(ColumnName.Length - 17));
            } 
        }
    }
}
