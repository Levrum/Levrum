using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using Newtonsoft.Json;

using Levrum.Data.Classes;
using Levrum.Data.Sources;
using Levrum.Utils.Data;

namespace Levrum.Data.Map
{
    public class DataMap
    {
        public string Name { get; set; } = string.Empty;

        [JsonIgnore]
        public string Path { get; set; } = string.Empty;

        public bool EnableCoordinateConversion { get; set; } = false;

        public ObservableCollection<DataMapping> IncidentMappings { get; set; } = new ObservableCollection<DataMapping>();
        public ObservableCollection<DataMapping> IncidentDataMappings { get; set; } = new ObservableCollection<DataMapping>();
        public ObservableCollection<DataMapping> ResponseDataMappings { get; set; } = new ObservableCollection<DataMapping>();
        public ObservableCollection<DataMapping> BenchmarkMappings { get; set; } = new ObservableCollection<DataMapping>();

        public ObservableCollection<IDataSource> DataSources { get; set; } = new ObservableCollection<IDataSource>();

        public string ResponseIdColumn { get; set; } = string.Empty;

        public string Projection { get; set; } = string.Empty;

        public bool InvertLongitude { get; set; } = false;
        public bool InvertLatitude { get; set; } = false;

        public List<CauseData> CauseTree { get; set; } = new List<CauseData>();

        public string PostProcessingScript { get; set; } = string.Empty;

        public DataMap(string _name)
        {
            Name = _name;
        }

        public static string[] s_defaultIncidentFields = new string[] { "ID", "Time", "Latitude", "Longitude", "Location" };

        public static DataMap GetEmptyDefaultDataMap(string _name)
        {
            DataMap map = new DataMap(_name);

            map.IncidentMappings.Add(new DataMapping("ID", ColumnType.stringField));
            map.IncidentMappings.Add(new DataMapping("Time", ColumnType.dateField));
            map.IncidentMappings.Add(new DataMapping("Location", ColumnType.stringField));
            map.IncidentMappings.Add(new DataMapping("Latitude", ColumnType.doubleField));
            map.IncidentMappings.Add(new DataMapping("Longitude", ColumnType.doubleField));
            map.IncidentDataMappings.Add(new DataMapping("Code", ColumnType.stringField));
            map.IncidentDataMappings.Add(new DataMapping("FirstEffAction", ColumnType.dateField));

            map.ResponseDataMappings.Add(new DataMapping("Unit", ColumnType.stringField));
            
            map.BenchmarkMappings.Add(new DataMapping("Assigned", ColumnType.dateField));
            map.BenchmarkMappings.Add(new DataMapping("Responding", ColumnType.dateField));
            map.BenchmarkMappings.Add(new DataMapping("Arrival", ColumnType.dateField));
            map.BenchmarkMappings.Add(new DataMapping("ClearScene", ColumnType.dateField));
            map.BenchmarkMappings.Add(new DataMapping("PatientTransfer", ColumnType.dateField));
            map.BenchmarkMappings.Add(new DataMapping("InService", ColumnType.dateField));
            map.BenchmarkMappings.Add(new DataMapping("InQuarters", ColumnType.dateField));

            return map;
        }
    }
}
