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
                if (Parameters.ContainsKey("CompressedContents"))
                {
                    string compressedContents = Parameters["CompressedContents"];
                    string xmlContents = LZString.decompressFromUTF16(compressedContents);

                    stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlContents));
                }
                else
                {
                    stream = XmlFile.OpenRead();
                }
                if ((!XmlFile.Directory.Exists) || (!XmlFile.Exists)) { return (new List<string>()); }
                XDocument doc = XDocument.Load(stream);
                var allElements = doc.Descendants();
                HashSet<string> output = new HashSet<string>();
                foreach (var element in allElements)
                {
                    output.Add(element.Name.ToString());
                }
                return output.ToList();
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
                if (Parameters.ContainsKey("CompressedContents"))
                {
                    string compressedContents = Parameters["CompressedContents"];
                    string xmlContents = LZString.decompressFromUTF16(compressedContents);

                    stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlContents));
                }
                else
                {
                    stream = XmlFile.OpenRead();
                }

                List<string> values = new List<string>();
                XDocument doc = XDocument.Load(stream);
                var nodes = doc.Descendants(column);
                foreach(var node in nodes)
                {
                    values.Add(node.Value);
                }

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

        public List<Record> GetRecords()
        {
            Stream stream = null;
            try
            {
                if (Parameters.ContainsKey("CompressedContents"))
                {
                    string compressedContents = Parameters["CompressedContents"];
                    string xmlContents = LZString.decompressFromUTF16(compressedContents);

                    stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlContents));
                }
                else
                {
                    stream = XmlFile.OpenRead();
                }

                List<Record> records = new List<Record>();
                XDocument doc = XDocument.Load(stream);
                var incidentNodes = doc.Descendants(IncidentNode);
                List<string> columns = GetColumns();
                foreach (var incidentNode in incidentNodes)
                {
                    var incidentChildren = incidentNode.Elements();
                    var responseNodes = incidentNode.Descendants(ResponseNode);
                    foreach (var responseNode in responseNodes)
                    {
                        // var responseChildren = responseNode.Elements();
                        Dictionary<string, string> values = new Dictionary<string, string>();
                        foreach (var column in columns)
                        {
                            var responseColumns = responseNode.Descendants(column).ToList();
                            if (responseColumns.Count < 1)
                            {
                                var incidentColumns = incidentNode.Descendants(column).ToList();
                                if (incidentColumns.Count == 0)
                                    values[column] = "NULL";
                                else if (incidentColumns.Count == 1)
                                {
                                    values[column] = incidentColumns[0].Value;
                                }
                                else
                                {
                                    values[column] = "MULTIPLE VALUES";
                                }
                            } else if (responseColumns.Count == 1)
                            {
                                values[column] = responseColumns[0].Value;
                            } else
                            {
                                values[column] = "MULTIPLE VALUES";
                            }
                        }
                        Record record = new Record();
                        foreach(KeyValuePair<string, string> kvp in values)
                        {
                            record.AddValue(kvp.Key, kvp.Value);
                        }
                        records.Add(record);
                    }
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
