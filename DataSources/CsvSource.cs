using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace Levrum.Data.Sources
{
    public class CsvSource : IDataSource
    {
        public string Name { get; set; } = "CSV File";
        public DataSourceType Type { get { return DataSourceType.CsvSource; } }
        
        public string Info { get { return string.Format("CSV Source '{0}': {1}", Name, Parameters["File"]); } }

        private FileInfo s_file = null;

        [JsonIgnore]
        public FileInfo File 
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

        public CsvSource()
        {

        }

        public CsvSource(string _fileName = "", string _name = "CSV File")
        {
            if (_fileName != string.Empty)
            {
                File = new FileInfo(_fileName);
            }
            Name = _name;
            Parameters["File"] = _fileName;
        }

        public CsvSource(FileInfo _file, string _name = "CSV File")
        {
            File = _file;
            Name = _name;
            Parameters["File"] = _file.FullName;
        }

        public object Clone()
        {
            CsvSource clone = new CsvSource(File.FullName, Name);
            foreach (string key in Parameters.Keys)
            {
                clone.Parameters[key] = Parameters[key];
            }

            return clone;
        }

        public bool Connect()
        {
            if (File == null)
            {
                if (!Parameters.ContainsKey("File"))
                {
                    return false;
                }

                File = new FileInfo(Parameters["File"]);
            }

            return true;
        }

        public void Disconnect()
        {

        }

        static readonly string[] s_tableName = new string[] { "CSV File" };

        public List<string> GetTables()
        {
            return new List<string>(s_tableName);
        }

        public List<string> GetColumns(string table)
        {
            List<string> columnNames = new List<string>();

            return columnNames;
        }

        public List<string> GetColumnValues(string table, string column)
        {
            List<string> columnValues = new List<string>();

            return columnValues;
        }

        public List<string[]> GetRecords(string table)
        {
            List<string[]> records = new List<string[]>();

            return records;
        }

        public void Dispose()
        {

        }
    }
}
