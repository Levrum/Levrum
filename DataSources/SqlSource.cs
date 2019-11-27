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
                string user = Parameters.ContainsKey("User") ? Parameters["User"] : "Undefined";
                string server = Parameters.ContainsKey("Server") ? Parameters["Server"] : "Undefined";
                string port = Parameters.ContainsKey("Port") ? Parameters["Port"] : "Undefined";
                string database = Parameters.ContainsKey("Database") ? Parameters["Database"] : "Undefined";
                string table = Parameters.ContainsKey("Table") ? Parameters["Table"] : "Undefined";

                return string.Format("{0}: {1}@{2},{3} [{4}].[{5}]", Name, user, server, port, database, table);
            } 
        }

        public string IDColumn { get; set; } = "";
        public string ResponseIDColumn { get; set; } = "";

        static readonly string[] s_requiredParameters = new string[] { "Server", "Port", "User", "Password", "Database", "Table" };

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

        public List<string> GetColumns()
        {
            List<string> columns = new List<string>();

            return columns;
        }

        public List<string> GetColumnValues(string column)
        {
            List<string> values = new List<string>();

            return values;
        }

        public List<Record> GetRecords()
        {
            List<Record> records = new List<Record>();

            return records;
        }
    }
}
