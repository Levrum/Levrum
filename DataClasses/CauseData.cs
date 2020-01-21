using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Levrum.Data.Classes
{
    public class CauseData : CategoryData
    {
        public List<ICategorizedValue> NatureCodes { get { return Values; } set { Values = value; } }

        public CauseData()
        {

        }

        public static CauseData ConvertICategoryData(ICategoryData input)
        {
            CauseData output = new CauseData();
            output.Name = input.Name;
            output.Description = input.Description;
            output.Values = input.Values;
            foreach(ICategoryData child in input.Children)
            {
                output.Children.Add(ConvertICategoryData(child));
            }

            return output;
        }
    }

    public class NatureCode : ICategorizedValue
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
