using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;
using CsvHelper;
using CsvHelper.Expressions;

using Levrum.Utils;

namespace Levrum.Data.Sources
{
    public class CsvSource : IDataSource
    {
        public string Name { get; set; } = "CSV File";
        public DataSourceType Type { get { return DataSourceType.CsvSource; } }

        [JsonIgnore]
        public string Info { get { return string.Format("CSV Source '{0}': {1}", Name, Parameters["File"]); } }

        public string IDColumn { get; set; } = "";
        public string ResponseIDColumn { get; set; } = "";

        private FileInfo s_file = null;

        [JsonIgnore]
        public FileInfo CsvFile
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
                CsvFile = new FileInfo(_fileName);
            }
            Name = _name;
            Parameters["File"] = _fileName;
        }

        public CsvSource(FileInfo _file, string _name = "CSV File")
        {
            CsvFile = _file;
            Name = _name;
            Parameters["File"] = _file.FullName;
        }

        public object Clone()
        {
            CsvSource clone = new CsvSource(CsvFile.FullName, Name);
            clone.IDColumn = IDColumn;
            clone.ResponseIDColumn = ResponseIDColumn;

            foreach (string key in Parameters.Keys)
            {
                clone.Parameters[key] = Parameters[key];
            }

            return clone;
        }

        public bool Connect()
        {
            if (CsvFile == null)
            {
                if (!Parameters.ContainsKey("File"))
                {
                    return false;
                }

                CsvFile = new FileInfo(Parameters["File"]);
            }

            return CsvFile.Exists;
        }

        public void Disconnect()
        {

        }

        public List<string> GetColumns()
        {
            const string fn = "CsvSource.GetColumns()";
            Stream stream = null;
            try
            {
                if (Parameters.ContainsKey("CompressedContents"))
                {
                    string compressedContents = Parameters["CompressedContents"];
                    string csvContents = LZString.decompressFromUTF16(compressedContents);

                    stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContents));
                }
                else
                {
                    stream = CsvFile.OpenRead();
                }
                if ((!CsvFile.Directory.Exists) || (!CsvFile.Exists)) { return (new List<string>()); }
                    using (StreamReader sr = new StreamReader(stream))
                    using (CsvReader csvReader = new CsvReader(sr))
                    {
                        csvReader.Read();
                        csvReader.ReadHeader();
                        return new List<string>(csvReader.Context.HeaderRecord);
                    }
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
                    string csvContents = LZString.decompressFromUTF16(compressedContents);

                    stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContents));
                }
                else
                {
                    stream = CsvFile.OpenRead();
                }

                List<string> values = new List<string>();
                using (StreamReader sr = new StreamReader(stream))
                using (CsvReader csvReader = new CsvReader(sr))
                {
                    csvReader.Read();
                    csvReader.ReadHeader();
                    while (csvReader.Read())
                    {
                        values.Add(csvReader.GetField(column));
                    };
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
                    string csvContents = LZString.decompressFromUTF16(compressedContents);

                    stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContents));
                }
                else
                {
                    stream = CsvFile.OpenRead();
                }

                List<Record> records = new List<Record>();
                using (StreamReader sr = new StreamReader(stream))
                using (CsvReader csvReader = new CsvReader(sr))
                {
                    csvReader.Read();
                    csvReader.ReadHeader();
                    while (csvReader.Read())
                    {
                        Record record = new Record();
                        foreach (string column in csvReader.Context.HeaderRecord)
                        {
                            record.AddValue(column, csvReader.GetField(column));
                        }
                        records.Add(record);
                    };
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
