using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

namespace Levrum.Data.Map
{
    public class DataMap
    {
        public string Name { get; set; } = string.Empty;

        [JsonIgnore]
        public string Path { get; set; } = string.Empty;

        [JsonIgnore]
        public bool SaveNeeded { get; set; } = true;

        public DataMap(string _name)
        {
            Name = _name;
        }
    }
}
