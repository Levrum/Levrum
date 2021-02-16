using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using CsvHelper;

using Levrum.Utils.MathAndStats;

namespace Levrum.Utils.Data
{
    public class CsvAnalyzer
    {
        public byte[] Contents { get; set; }

        #region Constructors
        public CsvAnalyzer(string fileName)
        {
            FileInfo file = new FileInfo(fileName);
            readFileContents(file);
        }

        public CsvAnalyzer(FileInfo file)
        {
            readFileContents(file);
        }

        public CsvAnalyzer(FileStream stream)
        {
            readStreamContents(stream);
        }
        #endregion

        private void readFileContents(FileInfo file)
        {
            try
            {
                readStreamContents(file.OpenRead());
            }
            catch (Exception ex)
            {
                Contents = new byte[4];
            }
        }

        private void readStreamContents(Stream stream)
        {
            try
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    stream.CopyTo(memStream);
                    Contents = memStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Contents = new byte[4];
            }
        }

        public string GetSummary()
        {
            StringBuilder summaryBuilder = new StringBuilder();
            List<ColumnInfo> fieldInfo = GetMetadata();
            try
            {
                using (MemoryStream stream = new MemoryStream(Contents))
                using (StreamReader sr = new StreamReader(stream))
                using (CsvReader csv = new CsvReader(sr, CultureInfo.CurrentCulture))
                {
                    csv.Read();
                    csv.ReadHeader();

                    int recordCount = 0;
                    var records = csv.GetRecords<dynamic>();
                    foreach (dynamic record in records)
                    {
                        var dictionary = (IDictionary<string, object>)record;
                        recordCount++;
                        foreach (ColumnInfo field in fieldInfo)
                        {
                            if (dictionary.ContainsKey(field.Name))
                            {
                                field.Summary.IngestValue(dictionary[field.Name] as string);
                            }
                        }
                    }

                    summaryBuilder.AppendFormat("\nTotal Records: {0}\n\n", recordCount);
                }


                foreach (ColumnInfo field in fieldInfo)
                {
                    summaryBuilder.AppendFormat(field.GetSummaryHeader());
                    if (field.Summary != null)
                    {
                        summaryBuilder.AppendFormat(field.Summary.Summarize());
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return summaryBuilder.ToString();
        }

        public List<ColumnInfo> GetMetadata()
        {
            Dictionary<string, ColumnInfo> fieldInfoDictionary = new Dictionary<string, ColumnInfo>();
            try
            {
                using (MemoryStream stream = new MemoryStream(Contents))
                using (StreamReader sr = new StreamReader(stream))
                using (CsvReader csv = new CsvReader(sr, CultureInfo.CurrentCulture))
                {
                    csv.Read();
                    csv.ReadHeader();
                    string[] headerRow = csv.Context.Reader.HeaderRecord;
                    List<string> fieldNames = new List<string>(headerRow);
                    for (int i = 0; i < fieldNames.Count; i++)
                    {
                        ColumnInfo info = new ColumnInfo(fieldNames[i], i);
                        fieldInfoDictionary.Add(fieldNames[i], info);
                    }

                    var records = csv.GetRecords<dynamic>();
                    foreach (dynamic record in records)
                    {
                        foreach (var property in (IDictionary<string, object>)record)
                        {
                            ColumnInfo info;
                            if (fieldInfoDictionary.TryGetValue(property.Key, out info))
                            {
                                double d; int i; DateTime date;
                                string value = property.Value as string;
                                if (string.IsNullOrWhiteSpace(value))
                                {
                                    continue; // Allow for empty records;
                                }

                                if (!double.TryParse(value, out d))
                                {
                                    info.Type &= ~ColumnType.doubleField;
                                }
                                if (!int.TryParse(value as string, out i))
                                {
                                    info.Type &= ~ColumnType.intField;
                                }
                                if (!DateTime.TryParse(value, out date))
                                {
                                    info.Type &= ~ColumnType.dateField;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            // Clear extra possible types;
            List<ColumnInfo> fieldInfo = fieldInfoDictionary.Values.ToList();
            foreach (ColumnInfo field in fieldInfo)
            {
                if ((field.Type & ColumnType.dateField) == ColumnType.dateField)
                {
                    field.Type = ColumnType.dateField;
                    field.Summary = new DateSummary();
                }
                else if ((field.Type & ColumnType.intField) == ColumnType.intField)
                {
                    field.Type = ColumnType.intField;
                    field.Summary = new NumericSummary();
                }
                else if ((field.Type & ColumnType.doubleField) == ColumnType.doubleField)
                {
                    field.Type = ColumnType.doubleField;
                    field.Summary = new NumericSummary();
                }
                else
                {
                    field.Summary = new StringSummary();
                }
            }

            return fieldInfo;
        } // end method()

    }

} // end namespace{}
