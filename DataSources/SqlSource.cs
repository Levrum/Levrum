using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

namespace Levrum.Data.Sources
{
    public class SqlSource : IDataSource
    {
        public string Name { get; set; } = "SQL Source";
        public DataSourceType Type { get { return DataSourceType.SqlSource; } }

        public string Info 
        { 
            get 
            {
                string server = Parameters.ContainsKey("Server") ? Parameters["Server"] : "Undefined";

                return string.Format("{0}: {1} [Database: {2} User: {3}]", Name, server, Parameters["Database"], Parameters["User"] ); 
            } 
        }

        static readonly string[] s_requiredParameters = new string[] { "Server", "Port", "User", "Password", "Database" };

        [JsonIgnore]
        public List<string> RequiredParameters { get { return new List<string>(s_requiredParameters); } }
        public Dictionary<string, string> Parameters { get; set; }

        public SqlSource()
        {

        }

        public SqlSource(string _name = "SQL Source")
        {
            Name = _name;
        }

        public object Clone()
        {
            SqlSource clone = new SqlSource(Name);
            foreach (string key in Parameters.Keys)
            {
                clone.Parameters[key] = Parameters[key];
            }

            return clone;
        }

        public void Dispose()
        {

        }

        public bool Connect()
        {
            return true;
        }

        public void Disconnect()
        {

        }

        public List<string> GetTables()
        {
            List<string> tables = new List<string>();

            return tables;
        }

        public List<string> GetColumns(string table)
        {
            List<string> columns = new List<string>();

            return columns;
        }

        public List<string> GetColumnValues(string table, string column)
        {
            List<string> values = new List<string>();

            return values;
        }

        public List<string[]> GetRecords(string table)
        {
            List<string[]> records = new List<string[]>();

            return records;
        }
    }
}
