using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace Levrum.Data.Sources
{
    public class GeoSource : IDataSource
    {
        public string Name { get; set; }
        public DataSourceType Type { get; }

        [JsonIgnore]
        public string Info { get { return string.Format("Geo Source '{0}': {1}", Name, Parameters["File"]); } }

        public string IDColumn { get; set; }
        public string ResponseIDColumn { get; set; }

        private FileInfo s_file = null;

        [JsonIgnore]
        public FileInfo GeoFile
        {
            get
            {
                if (s_file == null && (Parameters.ContainsKey("File") && !string.IsNullOrWhiteSpace(Parameters["File"])))
                {
                    s_file = new FileInfo(Parameters["File"]);
                }
                return s_file;
            }
            set
            {
                s_file = value;
                Parameters["File"] = s_file.FullName;
            }
        }

        static readonly string[] s_requiredParameters = new string[] { "File" };

        [JsonIgnore]
        public List<string> RequiredParameters { get { return new List<string>(s_requiredParameters); } }
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        public bool Connect()
        {
            return true;
        }

        public void Disconnect()
        {

        }

        public List<string> GetColumns()
        {
            return new List<string>();
        }

        public List<string> GetColumnValues(string column)
        {
            return new List<string>();
        }

        public List<Record> GetRecords()
        {
            return new List<Record>();
        }

        public void Dispose()
        {

        }

        public object Clone()
        {
            return new GeoSource();
        }
    }
}
