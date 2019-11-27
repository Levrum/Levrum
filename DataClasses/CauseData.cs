using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.Data.Classes
{
    public class CauseData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<CauseData> Children { get; set; } = new List<CauseData>();
        public List<NatureCode> NatureCodes { get; set; } = new List<NatureCode>();

        public CauseData()
        {

        }
    }

    public class NatureCode
    {
        public string Value { get; set; }
        public string Description { get; set; }

        public static implicit operator string(NatureCode code)
        {
            return code.Value;
        }

        public static implicit operator NatureCode(string _value)
        {
            NatureCode code = new NatureCode();
            code.Value = _value;
            return code;
        }
    }
}
