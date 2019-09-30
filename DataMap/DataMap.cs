using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using Newtonsoft.Json;

using Levrum.Data.Sources;

namespace Levrum.Data.Map
{
    public class DataMap
    {
        public string Name { get; set; } = string.Empty;

        [JsonIgnore]
        public string Path { get; set; } = string.Empty;

        public ObservableCollection<DataMapping> IncidentDataMappings { get; set; } = new ObservableCollection<DataMapping>();
        public ObservableCollection<DataMapping> ResponseDataMappings { get; set; } = new ObservableCollection<DataMapping>();
        public ObservableCollection<DataMapping> BenchmarkMappings { get; set; } = new ObservableCollection<DataMapping>();

        public ObservableCollection<IDataSource> DataSources { get; set; } = new ObservableCollection<IDataSource>();

        public DataMap(string _name)
        {
            Name = _name;
        }
    }
}
