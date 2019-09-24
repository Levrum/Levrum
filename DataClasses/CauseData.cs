using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.DataClasses
{
    public class CauseData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<CauseData> Children { get; set; } = new List<CauseData>();
        public List<string> NatureCodes { get; set; } = new List<string>();

        public CauseData()
        {

        }
    }
}
