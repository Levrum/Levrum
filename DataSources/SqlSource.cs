﻿using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Data.Sql;
using Microsoft.Data.SqlClient;

using Newtonsoft.Json;

using Levrum.Utils;

namespace Levrum.Data.Sources
{
    public class SqlSource : IDataSource
    {
        public string Name { get; set; } = "SQL Source";
        public DataSourceType Type { get { return DataSourceType.SqlSource; } }

        [JsonIgnore]
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

        public string DateColumn { get; set; } = "";

        static readonly string[] s_requiredParameters = new string[] { "Server", "Port", "User", "Password", "Database" };

        [JsonIgnore]
        public List<string> RequiredParameters { get { return new List<string>(s_requiredParameters); } }
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        protected SqlConnection m_connection = null;
        protected bool m_connected = false;

        private List<Record> m_cachedRecords = null;
        private static AESCryptor s_cryptor = new AESCryptor() { StringPermutation = "DataSource.SqlSource" };

        [JsonIgnore]
        public string Password
        {
            get
            {
                if (Parameters.ContainsKey("PWEncrypted") && Parameters.ContainsKey("Password"))
                {
                    return s_cryptor.Decrypt(Parameters["Password"]);
                } else if (Parameters.ContainsKey("Password"))
                {
                    return Parameters["Password"];
                } else
                {
                    return string.Empty;
                }
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Parameters["Password"] = string.Empty;
                }
                else
                {
                    Parameters["Password"] = s_cryptor.Encrypt(value);
                    Parameters["PWEncrypted"] = "true";
                }
            }
        }

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
            clone.IDColumn = IDColumn;
            clone.ResponseIDColumn = ResponseIDColumn;

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
            string secure_cs = "";
            try
            {
                SqlConnectionStringBuilder sqlcsb = new SqlConnectionStringBuilder();
                sqlcsb.UserID = Parameters["User"];
                sqlcsb.Password = Password;
                sqlcsb.IntegratedSecurity = Parameters.ContainsKey("IntegratedSecurity");

                string dataSource = "";
                if (!Parameters.ContainsKey("Port") || string.IsNullOrWhiteSpace(Parameters["Port"]))
                    dataSource = Parameters["Server"];
                else
                    dataSource = string.Format("{0},{1}", Parameters["Server"], Parameters["Port"]);

                secure_cs = "Server=" + dataSource + "; User=" + Parameters["User"] + "; PW=???????; ";
                if (string.IsNullOrWhiteSpace(dataSource))
                {
                    return false;
                }

                sqlcsb.DataSource = dataSource;
                sqlcsb.InitialCatalog = Parameters["Database"];
                secure_cs += "Database=" + Parameters["Database"];

                m_connection = new SqlConnection();
                m_connection.ConnectionString = sqlcsb.ConnectionString;
                m_connection.Open();

                m_connected = true;

                return true;
            }
            catch (SqlException ex)
            {
                m_sErrorMessage = "SQL error attempting connection '" + secure_cs + "': " + ex.Message;
                return (false);
            }
            catch (Exception ex)
            {
                m_sErrorMessage = "Generic exception attempting connection '" + secure_cs + "': " + ex.Message;
                return false;
            }
        }

        public string ErrorMessage {  get { return (m_sErrorMessage); } }
        private string m_sErrorMessage = "";

        public void Disconnect()
        {
            m_cachedRecords = null;

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
                if (!testConnection())
                {
                    return output;
                }

                SqlCommand cmd = new SqlCommand(string.Format("SELECT table_name FROM INFORMATION_SCHEMA.TABLES;"), m_connection);
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
                Connect();
                if (!testConnection())
                {
                    return output;
                }

                SqlCommand cmd = new SqlCommand(string.Format("SELECT column_name FROM INFORMATION_SCHEMA.COLUMNS WHERE table_name LIKE '{0}'", Parameters["Table"]), m_connection);
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
                Connect();
                if (!testConnection())
                {
                    return output;
                }

                string query = Parameters["Query"].Replace("'", "''");
                string lcQuery = query.ToLowerInvariant();
                int startIndex = lcQuery.IndexOf("!!startdate!!");
                if (startIndex != -1)
                {
                    query = query.Remove(startIndex, "!!startdate!!".Length);
                    query = query.Insert(startIndex, 
                        string.Format("DATETIMEFROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, {6})",
                        DateTime.MinValue.Year,
                        DateTime.MinValue.Month,
                        DateTime.MinValue.Day,
                        DateTime.MinValue.Hour,
                        DateTime.MinValue.Minute,
                        DateTime.MinValue.Second,
                        DateTime.MinValue.Millisecond));
                }

                lcQuery = query.ToLowerInvariant();
                int endIndex = lcQuery.IndexOf("!!enddate!!");
                if (endIndex != -1)
                {
                    query = query.Remove(endIndex, "!!enddate!!".Length);
                    query = query.Insert(endIndex,
                        string.Format("DATETIMEFROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, {6})",
                        DateTime.MaxValue.Year,
                        DateTime.MaxValue.Month,
                        DateTime.MaxValue.Day,
                        DateTime.MaxValue.Hour,
                        DateTime.MaxValue.Minute,
                        DateTime.MaxValue.Second,
                        DateTime.MaxValue.Millisecond));
                }

                SqlCommand cmd = new SqlCommand(string.Format("SELECT name FROM sys.dm_exec_describe_first_result_set('{0}', NULL, 0);", query), m_connection);
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
            }
            catch (Exception ex)
            {

            }
            finally
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
                
                Connect();
                if (!testConnection())
                {
                    return output;
                }

                SqlCommand cmd = new SqlCommand(string.Format("SELECT {0} FROM ({1}) AS A", column, source), m_connection);
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
            return GetRecords(DateTime.MinValue, DateTime.MaxValue);
        }

        public List<Record> GetRecords(DateTime startDate, DateTime endDate)
        {
            List<Record> output = new List<Record>();
            if (m_cachedRecords != null)
            {
                return m_cachedRecords;
            }
            try
            {
                if (Parameters.ContainsKey("Table"))
                {
                    output = getRecordsFromTable(startDate, endDate);
                } else if (Parameters.ContainsKey("Query"))
                {
                    output = getRecordsFromQuery(startDate, endDate);
                }
            } finally
            {

            }
            m_cachedRecords = output;
            return output;
        }

        private List<Record> getRecordsFromTable(DateTime startDate, DateTime endDate)
        {
            List<Record> output = new List<Record>();
            try
            {
                Connect();
                if (!testConnection())
                {
                    return output;
                }

                SqlCommand cmd;

                if (!string.IsNullOrWhiteSpace(DateColumn) && (startDate != DateTime.MinValue || endDate != DateTime.MaxValue))
                {
                    cmd = new SqlCommand(
                        string.Format("SELECT * FROM {0} WHERE {1} >= DATETIMEFROMPARTS({2}, {3}, {4}, {5}, {6}, {7}, {8}) AND {1} <= DATETIMEFROMPARTS({9}, {10}, {11}, {12}, {13}, {14}, {15})",
                        Parameters["Table"], DateColumn, startDate.Year, startDate.Month, startDate.Day, startDate.Hour, startDate.Minute, startDate.Second, startDate.Millisecond,
                        endDate.Year, endDate.Month, endDate.Day, endDate.Hour, endDate.Minute, endDate.Second, endDate.Millisecond), m_connection);
                }
                else
                {
                    cmd = new SqlCommand(string.Format("SELECT * FROM {0}", Parameters["Table"]), m_connection);
                }
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Record record = new Record();
                        for (int i = 0; i < dr.FieldCount; i++)
                        {
                            record.AddValue(dr.GetName(i), dr.GetValue(i));
                        }
                        output.Add(record);
                    }
                }
            }
            finally
            {

            }
            return output;
        }

        private List<Record> getRecordsFromQuery(DateTime startDate, DateTime endDate)
        {
            List<Record> output = new List<Record>();
            try
            {
                Connect();
                if (!testConnection())
                {
                    return output;
                }

                SqlCommand cmd = null;

                string query = Parameters["Query"];

                string lcQuery = query.ToLowerInvariant();
                int startIndex = lcQuery.IndexOf("!!startdate!!");
                if (startIndex != -1)
                {
                    if (startDate == DateTime.MinValue)
                    {
                        startDate = new DateTime(1900, 1, 1);
                    }

                    query = query.Remove(startIndex, "!!startdate!!".Length);
                    query = query.Insert(startIndex,
                            string.Format("DATETIMEFROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, {6})",
                        startDate.Year,
                        startDate.Month,
                        startDate.Day,
                        startDate.Hour,
                        startDate.Minute,
                        startDate.Second,
                        startDate.Millisecond));
                }

                lcQuery = query.ToLowerInvariant();
                int endIndex = lcQuery.IndexOf("!!enddate!!");
                if (endIndex != -1)
                {
                    if (endDate == DateTime.MaxValue)
                    {
                        endDate = new DateTime(2100, 1, 1);
                    }

                    query = query.Remove(endIndex, "!!enddate!!".Length);
                    query = query.Insert(endIndex,
                            string.Format("DATETIMEFROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, {6})", 
                        endDate.Year, 
                        endDate.Month, 
                        endDate.Day, 
                        endDate.Hour, 
                        endDate.Minute, 
                        endDate.Second, 
                        endDate.Millisecond));
                }

                cmd = new SqlCommand(query, m_connection);
                cmd.CommandTimeout = 120;
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Record record = new Record();
                        for (int i = 0; i < dr.FieldCount; i++)
                        {
                            record.AddValue(dr.GetName(i), dr.GetValue(i));
                        }
                        output.Add(record);
                    }
                }
            } finally
            {

            }
            return output;
        }
    }
}
