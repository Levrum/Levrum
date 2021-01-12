using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Newtonsoft.Json;
using CsvHelper;
using CsvHelper.Expressions;

using Levrum.Utils;

namespace Levrum.Data.Sources
{
    public class XmlSource : IDataSource
    {
        public string Name { get; set; } = "XML File";
        public DataSourceType Type { get { return DataSourceType.XmlSource; } }

        [JsonIgnore]
        public string Info { get { return string.Format("XML Source '{0}': {1}", Name, Parameters["File"]); } }

        public string IncidentNode { get; set; } = "";
        public string IDColumn { get; set; } = "";
        public string ResponseNode { get; set; } = "";
        public string ResponseIDColumn { get; set; } = "";
        
        public string DateColumn { get;set; } = "";

        public string ErrorMessage {  get { return (m_sErrorMessage); } }
        private string m_sErrorMessage = "Error messages not implemented for type XmlSource";

        private FileInfo s_file = null;

        [JsonIgnore]
        public FileInfo XmlFile
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

        public XmlSource()
        {

        }

        public XmlSource(string _fileName = "", string _name = "XML File")
        {
            if (_fileName != string.Empty)
            {
                XmlFile = new FileInfo(_fileName);
            }
            Name = _name;
            Parameters["File"] = _fileName;
        }

        public XmlSource(FileInfo _file, string _name = "XML File")
        {
            XmlFile = _file;
            Name = _name;
            Parameters["File"] = _file.FullName;
        }

        public object Clone()
        {
            XmlSource clone = new XmlSource(XmlFile.FullName, Name);
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
            if (XmlFile == null)
            {
                if (!Parameters.ContainsKey("File"))
                {
                    return false;
                }

                XmlFile = new FileInfo(Parameters["File"]);
            }

            return XmlFile.Exists;
        }

        public void Disconnect()
        {

        }

        public List<string> GetColumns()
        {
            const string fn = "XmlSource.GetColumns()";
            Stream stream = null;
            try
            {
                Parameters.TryGetValue("CompressedContents", out string compressedContents);
                stream = XmlUtils.GetXmlStream(compressedContents, XmlFile);

                var columns = XmlUtils.GetColumns(stream);
                return columns;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Exception in GetColumns");
                return (new List<string>());
            } finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        public List<string> GetColumnValues(string column)
        {
            Stream stream = null;
            try
            {
                Parameters.TryGetValue("CompressedContents", out string compressedContents);
                stream = XmlUtils.GetXmlStream(compressedContents, XmlFile);

                List<string> values = XmlUtils.GetColumnValues(column, stream);

                return values;
            } catch (Exception ex)
            {
                LogHelper.LogException(ex, "Exception in GetColumnValues");
                return new List<string>();
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        public List<Record> GetRecords() {
            return GetRecords(DateTime.MinValue, DateTime.MaxValue);
        }

        public List<Record> GetRecords(DateTime startDate, DateTime endDate)
        {
            Stream stream = null;
            try
            {
                Parameters.TryGetValue("CompressedContents", out string compressedContents);
                stream = XmlUtils.GetXmlStream(compressedContents, XmlFile);

                List<Record> records = new List<Record>();
                foreach (Record record in XmlUtils.GetRecords(stream, IncidentNode, ResponseNode))
                {
                    object dateColumnValue;
                    if (record.Data.TryGetValue(DateColumn, out dateColumnValue) && dateColumnValue is string)
                    {
                        DateTime recordDate;
                        if (DateTime.TryParse(dateColumnValue as string, out recordDate) && (recordDate < startDate || recordDate > endDate))
                        {
                            // The date parsed okay and was too early or too late so don't include it.
                            continue;
                        }
                    }
                    // Either we couldn't determine the date or it was okay. If you want to exclude dates we can't parse, you'll need to rewrite the section above this.
                    records.Add(record);
                }
                return records;
            } catch (Exception ex)
            {
                LogHelper.LogException(ex, "Exception in GetRecords");
                return new List<Record>();
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        public void Dispose()
        {

        }
    }
}
