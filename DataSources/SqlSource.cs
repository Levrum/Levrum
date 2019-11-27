﻿using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Data.Sql;
using Microsoft.Data.SqlClient;

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
                string query = Parameters.ContainsKey("Query") ? Parameters["Query"] : "Undefined";
                if (table == "Undefined" && query != "Undefined")
                {
                    table = "Query";
                }

                return string.Format("{0}: {1}@{2},{3} [{4}].[{5}]", Name, user, server, port, database, table);
            } 
        }

        public string IDColumn { get; set; } = "";
        public string ResponseIDColumn { get; set; } = "";

        static readonly string[] s_requiredParameters = new string[] { "Server", "Port", "User", "Password", "Database" };

        [JsonIgnore]
        public List<string> RequiredParameters { get { return new List<string>(s_requiredParameters); } }
        public Dictionary<string, string> Parameters { get; set; }

        protected SqlConnection m_connection = null;
        protected bool m_connected = false;

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
            Disconnect();
        }

        public bool Connect()
        {
            if (m_connected && testConnection())
                return true;
            try
            {
                SqlConnectionStringBuilder sqlcsb = new SqlConnectionStringBuilder();
                sqlcsb.UserID = Parameters["User"];
                sqlcsb.Password = Parameters["Password"];
                sqlcsb.DataSource = string.Format("{0},{1}", Parameters["Server"], Parameters["Port"]);
                sqlcsb.InitialCatalog = Parameters["Database"];

                m_connection = new SqlConnection();
                m_connection.ConnectionString = sqlcsb.ConnectionString;
                m_connection.Open();

                m_connected = true;

                return true;
            } catch (Exception ex)
            {
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (m_connection != null)
                {
                    m_connection.Close();
                }
            } finally
            {
                m_connected = false;
            }
        }

        private bool testConnection()
        {
            if (m_connection != null) {
                switch (m_connection.State)
                {
                    case System.Data.ConnectionState.Open:
                    case System.Data.ConnectionState.Fetching:
                    case System.Data.ConnectionState.Executing:
                        return true;
                    default:
                        return false;
                }
            } else 
            { 
                return false; 
            }
        }

        public List<string> GetTables()
        {
            List<string> output = new List<string>();
            try
            {
                Connect();
                SqlCommand cmd = new SqlCommand(string.Format("SELECT table_name FROM INFORMATION_SCHEMA.TABLES WHERE table_schema LIKE '%{0}%'", Parameters["Database"]), m_connection);
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        object value = dr.GetValue(0);
                        if (value != null)
                        {
                            output.Add(value.ToString());
                        }
                    }
                }
            } finally
            {

            }
            return output;
        }

        public List<string> GetColumns()
        {
            if (Parameters.ContainsKey("Table")) 
            {
                return getColumnsFromTable();
            } else if (Parameters.ContainsKey("Query"))
            {
                return getColumnsFromQuery();
            } else
            {
                return new List<string>();
            }
        }

        private List<string> getColumnsFromTable()
        {
            List<string> output = new List<string>();
            try
            {
                SqlCommand cmd = new SqlCommand(string.Format("SELECT column_name FROM INFORMATION_SCHEMA.COLUMNS WHERE table_name LIKE '%{0}%'", Parameters["Table"]));
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        object value = dr.GetValue(0);
                        if (value != null)
                        {
                            output.Add(value.ToString());
                        }
                    }
                }
            } finally
            {

            }
            return output;
        }

        private List<string> getColumnsFromQuery()
        {
            List<string> output = new List<string>();
            try
            {
                SqlCommand cmd = new SqlCommand(string.Format("SELECT name FROM sys.dm_exec_describe_first_result_set('{0}', NULL, 0);", Parameters["Query"]));
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        object value = dr.GetValue(0);
                        if (value != null)
                        {
                            output.Add(value.ToString());
                        }
                    }
                }
            } finally
            {

            }
            return output;
        }

        public List<string> GetColumnValues(string column)
        {
            List<string> output = new List<string>();
            try
            {
                string source = string.Empty;
                if (Parameters.ContainsKey("Table"))
                {
                    source = Parameters["Table"];
                } else if (Parameters.ContainsKey("Query"))
                {
                    source = Parameters["Query"];
                }

                if (string.IsNullOrWhiteSpace(source))
                {
                    return output;
                }

                SqlCommand cmd = new SqlCommand(string.Format("SELECT {0} FROM ({1}) AS A", column, source));
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        object value = dr.GetValue(0);
                        if (value != null)
                        {
                            output.Add(value.ToString());
                        }
                    }
                }
            } finally
            {

            }
            return output;
        }

        public List<Record> GetRecords()
        {
            List<Record> output = new List<Record>();
            try
            {

            } finally
            {

            }
            return output;
        }
    }
}
