using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Levrum.Utils;
using Newtonsoft.Json;

namespace Levrum.Data.Sources
{
    public class DailyDigestXmlSource : IDataSource
    {
        public string Name { get; set; } = "Daily Digest XML Directory";
        public DataSourceType Type { get { return DataSourceType.DailyDigestXmlSource; } }

        [JsonIgnore]
        public string Info { get { return string.Format($"Daily Digest XML Source '{Name}': {Parameters["Directory"]}"); } }

        public string IncidentNode { get; set; } = "";
        public string IDColumn { get; set; } = "";
        public string ResponseNode { get; set; }
        public string ResponseIDColumn { get; set; } = "";

        public string DateColumn { get;set; } = "";

        static readonly string[] s_requiredParameters = new string[] { "Directory" };

        [JsonIgnore]
        public List<string> RequiredParameters { get { return new List<string>(s_requiredParameters); } }

        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        public string ErrorMessage { get { return m_sErrorMessage; } }
        private string m_sErrorMessage = "Error messages not implemented for the type DailyDigestXmlSource";

        private DirectoryInfo s_directory = null;

        [JsonIgnore]
        public DirectoryInfo DailyDigestDirectory
        {
            get
            {
                if (s_directory == null && (Parameters.ContainsKey("Directory") && !string.IsNullOrWhiteSpace(Parameters["Directory"])))
                {
                    s_directory = new DirectoryInfo(Parameters["Directory"]);
                }
                return s_directory;
            }
            set
            {
                s_directory = value;
                Parameters["Directory"] = s_directory.FullName;
            }
        }
        
        public DailyDigestXmlSource()
        {

        }

        public DailyDigestXmlSource(string _directoryName = "", string _name = "Daily Digest XML Directory")
        {
            if (_directoryName != string.Empty)
            {
                DailyDigestDirectory = new DirectoryInfo(_directoryName);
            }
            Name = _name;
            Parameters["Directory"] = _directoryName;
        }

        public DailyDigestXmlSource(DirectoryInfo _directory, string _name = "Daily Digest XML Directory")
        {
            DailyDigestDirectory = _directory;
            Name = _name;
            Parameters["Directory"] = _directory.FullName;
        }

        public object Clone()
        {
            DailyDigestXmlSource clone = new DailyDigestXmlSource(DailyDigestDirectory.FullName, Name);
            clone.IncidentNode = IncidentNode;
            clone.IDColumn = IDColumn;
            clone.ResponseNode = ResponseNode;
            clone.ResponseIDColumn = ResponseIDColumn;

            foreach (string key in Parameters.Keys)
            {
                clone.Parameters[key] = Parameters[key];
            }

            return clone;
        }

        public bool Connect()
        {
            if (DailyDigestDirectory == null)
            {
                if (!Parameters.ContainsKey("Directory"))
                {
                    return false;
                }

                DailyDigestDirectory = new DirectoryInfo(Parameters["Directory"]);
            }

            return DailyDigestDirectory.Exists;
        }

        public void Disconnect()
        {
            
        }

        public void Dispose()
        {
            
        }

        public List<string> GetColumns()
        {
            try
            {
                var firstFile = DailyDigestDirectory.EnumerateFiles().First();
                using (Stream stream = firstFile.OpenRead())
                {
                    List<string> columns = XmlUtils.GetColumns(stream);
                    return columns;
                }
            }catch (Exception ex)
            {
                LogHelper.LogException(ex, "Exception in GetColumns");
                return new List<string>();
            }
        }

        public List<string> GetColumnValues(string column)
        {
            try
            {
                List<string> columnValues = new List<string>();
                var xmlFiles = DailyDigestDirectory.GetFiles();
                foreach (FileInfo file in xmlFiles)
                {
                    using (Stream stream = file.OpenRead())
                    {
                        List<string> values = XmlUtils.GetColumnValues(column, stream);
                        columnValues.AddRange(values);
                    }
                }

                return columnValues;
            } catch (Exception ex)
            {
                LogHelper.LogException(ex, "Exception in GetColumnValues");
                return new List<string>();
            }
        }

        public List<Record> GetRecords() {
            return GetRecords(DateTime.MinValue, DateTime.MaxValue);
        }

        public List<Record> GetRecords(DateTime startDate, DateTime endDate)
        {
            try
            {
                List<Record> records = new List<Record>();
                var xmlFiles = DailyDigestDirectory.GetFiles();
                foreach (FileInfo file in xmlFiles)
                {
                    using (Stream stream = file.OpenRead())
                    {
                        List<Record> fileRecords = XmlUtils.GetRecords(stream, IncidentNode, ResponseNode);
                        records.AddRange(fileRecords);
                    }
                }

                return records;
            } catch (Exception ex)
            {
                LogHelper.LogException(ex, "Exception in GetRecords");
                return new List<Record>();
            }
        }
    }
}
