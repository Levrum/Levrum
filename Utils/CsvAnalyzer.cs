using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using CsvHelper;

using Levrum.Utils.MathAndStats;

namespace Levrum.Utils
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
            List<FieldInfo> fieldInfo = GetMetadata();
            try
            {
                using (MemoryStream stream = new MemoryStream(Contents))
                using (StreamReader sr = new StreamReader(stream))
                using (CsvReader csv = new CsvReader(sr))
                {
                    csv.Read();
                    csv.ReadHeader();

                    int recordCount = 0;
                    var records = csv.GetRecords<dynamic>();
                    foreach (dynamic record in records)
                    {
                        var dictionary = (IDictionary<string, object>)record;
                        recordCount++;
                        foreach (FieldInfo field in fieldInfo)
                        {
                            if (dictionary.ContainsKey(field.Name))
                            {
                                field.Summary.IngestValue(dictionary[field.Name] as string);
                            }
                        }
                    }

                    summaryBuilder.AppendFormat("\nTotal Records: {0}\n\n", recordCount);
                }

                
                foreach(FieldInfo field in fieldInfo)
                {
                    summaryBuilder.AppendFormat(field.GetSummaryHeader());
                    if (field.Summary != null)
                    {
                        summaryBuilder.AppendFormat(field.Summary.Summarize());
                    }
                }
            } catch (Exception ex)
            {

            }

            return summaryBuilder.ToString();
        }

        public List<FieldInfo> GetMetadata()
        {
            Dictionary<string, FieldInfo> fieldInfoDictionary = new Dictionary<string, FieldInfo>();
            try
            {
                using (MemoryStream stream = new MemoryStream(Contents))
                using (StreamReader sr = new StreamReader(stream))
                using (CsvReader csv = new CsvReader(sr))
                {
                    csv.Read();
                    csv.ReadHeader();
                    string[] headerRow = csv.Context.HeaderRecord;
                    List<string> fieldNames = new List<string>(headerRow);
                    for (int i = 0; i < fieldNames.Count; i++)
                    {
                        FieldInfo info = new FieldInfo(fieldNames[i], i);
                        fieldInfoDictionary.Add(fieldNames[i], info);
                    }

                    var records = csv.GetRecords<dynamic>();
                    foreach (dynamic record in records)
                    {
                        foreach (var property in (IDictionary<string, object>)record)
                        {
                            FieldInfo info;
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
                                    info.Type &= ~FieldType.doubleField;
                                }
                                if (!int.TryParse(value as string, out i))
                                {
                                    info.Type &= ~FieldType.intField;
                                }
                                if (!DateTime.TryParse(value, out date))
                                {
                                    info.Type &= ~FieldType.dateField;
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
            List<FieldInfo> fieldInfo = fieldInfoDictionary.Values.ToList();
            foreach (FieldInfo field in fieldInfo)
            {
                if ((field.Type & FieldType.dateField) == FieldType.dateField)
                {
                    field.Type = FieldType.dateField;
                    field.Summary = new DateSummary();
                }
                else if ((field.Type & FieldType.intField) == FieldType.intField)
                {
                    field.Type = FieldType.intField;
                    field.Summary = new NumericSummary();
                }
                else if ((field.Type & FieldType.doubleField) == FieldType.doubleField)
                {
                    field.Type = FieldType.doubleField;
                    field.Summary = new NumericSummary();
                } else
                {
                    field.Summary = new StringSummary();
                }
            }

            return fieldInfo;
        }
    }

    public class FieldInfo
    {
        public string Name { get; set; } = string.Empty;
        public FieldType Type { get; set; } = FieldType.stringField;
        public int Ordinal { get; set; } = -1;
        public FieldSummary Summary { get; set; } = null;

        public static Dictionary<FieldType, string> s_typeNames = new Dictionary<FieldType, string> { { FieldType.stringField, "String" }, { FieldType.intField, "Integer" }, { FieldType.doubleField, "Double" }, { FieldType.dateField, "Date/Time" } };

        public string GetSummaryHeader()
        {
            return string.Format("==================================\n[{0}] '{1}' ({2}):\n", Ordinal.ToString().PadLeft(2), Name, s_typeNames[Type]);
        }

        public FieldInfo() { }

        public FieldInfo(string _name = "", int _ordinal = 0)
        {
            Name = _name;
            Type = FieldType.stringField | FieldType.intField | FieldType.doubleField | FieldType.dateField;
            Ordinal = _ordinal;
        }

        public FieldInfo(string _name, FieldType _type, int _ordinal)
        {
            Name = _name;
            Type = _type;
            Ordinal = _ordinal;
        }
    }

    public abstract class FieldSummary
    {
        public int Valid { get; set; } = 0;
        public int Invalid { get; set; } = 0;
        public abstract bool IngestValue(string value);
        public abstract string Summarize();
    }

    public class NumericSummary : FieldSummary
    {
        public OrderedStats Stats = new OrderedStats();
        public int Zeros { get; set; } = 0;
        public int Negatives { get; set; } = 0;

        public override bool IngestValue(string value)
        {
            double d;
            if (!double.TryParse(value, out d))
            {
                Invalid++;
                return false;
            }
            if (d == 0.0) Zeros++;
            else if (d < 0) Negatives++;
            Valid++;
            Stats.AddObs(d);

            return true;
        }

        public override string Summarize()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("    Total N=" + Stats.Count + ";  Valid: " + Valid + ";  Invalid: " + Invalid);
            sb.AppendLine("    Zero values: " + Zeros + ";  Negative values: " + Negatives);
            sb.Append("    Mean: " + Math.Round(Stats.Mean, 3) + "; ");
            sb.Append("    SD: " + Math.Round(Stats.StdDev, 3) + "; ");
            sb.Append("    Min: " + Math.Round(Stats.Min, 3) + "; ");
            sb.Append("    Max: " + Math.Round(Stats.Max, 3) + "; ");
            sb.AppendLine();
            sb.Append("    Percentiles: 10%=" + Math.Round(Stats.GetPercentile(10.0), 3) + "; ");
            sb.Append("    25%=" + Math.Round(Stats.GetPercentile(25.0), 3) + "; ");
            sb.Append("    50%=" + Math.Round(Stats.GetPercentile(50.0), 3) + "; ");
            sb.Append("    75%=" + Math.Round(Stats.GetPercentile(75.0), 3) + "; ");
            sb.Append("    90%=" + Math.Round(Stats.GetPercentile(90.0), 3) + "; ");
            sb.AppendLine();
            sb.AppendLine();

            return (sb.ToString());
        }
    }

    public class DateSummary : FieldSummary
    {
        public int NonzeroSeconds { get; set; } = 0;
        public Stats StatsRel19700101 = new Stats();

        public override string Summarize()
        {
            int n = StatsRel19700101.Count;
            double pct_nonzero = 0.0;
            if (n > 0) { pct_nonzero = Math.Round(100.0 * (((double)this.NonzeroSeconds) / ((double)n)), 2); }
            string min = Format(Back2Dtm(StatsRel19700101.Min));
            string max = Format(Back2Dtm(StatsRel19700101.Max));
            string mean = Format(Back2Dtm(StatsRel19700101.Mean));
            return string.Format("    Total N={0}; Valid: {1}; Invalid: {2}; Min={3}; Max={4}; Avg={5}; Nonzero Seconds:{6} ({7})\n", n, Valid, Invalid, min, max, mean, NonzeroSeconds, pct_nonzero);
        }

        public string Format(DateTime oDtm)
        {
            return (string.Format("{0:D4}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}",
                    oDtm.Year, oDtm.Month, oDtm.Day, oDtm.Hour, oDtm.Minute, oDtm.Second));
        }

        public DateTime Back2Dtm(double dDelta)
        {
            if ((double.IsInfinity(dDelta)) || (double.IsNaN(dDelta))) { return (DateTime.MaxValue); }
            if (double.MinValue == dDelta) { return (DateTime.MinValue); }
            if (double.MaxValue == dDelta) { return (DateTime.MaxValue); }
            DateTime newdtm = m_dtmRef.AddDays(dDelta);
            return (newdtm);
        }

        private DateTime m_dtmRef
            = new DateTime(1970, 1, 1, 0, 0, 0);

        public override bool IngestValue(string sVal)
        {
            DateTime dtm = new DateTime();

            if (!DateTime.TryParse(sVal, out dtm))
            {
                Invalid++;
                return (false);
            }

            Valid++;
            double delta = dtm.Subtract(m_dtmRef).TotalDays;
            StatsRel19700101.AddObs(delta);
            int seconds = dtm.Second;
            if (0 != seconds) { NonzeroSeconds++; }

            return (true);
        }
    }

    public class StringSummary : FieldSummary
    {
        public int MaxFreqs { get; set; } = 25;
        public Dictionary<string, int> Freqs { get; } = new Dictionary<string, int>();
        public int Count { get; set; } = 0;
        public int Nulls { get; set; } = 0;
        public int Empties { get; set; } = 0;
        public int Blanks { get; set; } = 0;

        public StringSummary(int iMaxFreqs = 25)
        {
            MaxFreqs = iMaxFreqs;
        }

        public override bool IngestValue(string sVal)
        {
            if (null == sVal)
            {
                Nulls++;
                sVal = "{NULL}";
            }
            else if ("" == sVal) { Empties++; }
            else if (string.IsNullOrWhiteSpace(sVal)) { Blanks++; }
            if (!Freqs.ContainsKey(sVal)) { Freqs.Add(sVal, 0); }
            Freqs[sVal]++;
            Count++;
            return true;
        }

        public override string Summarize()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("    N = {0};  Null: {1}; Empty: {2}; Blank: {3}\n", Count, Nulls, Empties, Blanks);

                // Reverse-order frequency sort:
                SortedDictionary<int, List<string>> rev = new SortedDictionary<int, List<string>>();
                foreach (string skey in Freqs.Keys)
                {
                    int n = Freqs[skey];
                    int nkey = -n;
                    if (!rev.ContainsKey(nkey)) { rev.Add(nkey, new List<string>()); }
                    List<string> slist = rev[nkey];
                    slist.Add(skey);
                }

                // Now, prettyprint it:
                int maxlines = MaxFreqs;
                sb.AppendFormat("    Value Frequencies (Top {0}):\n", maxlines);
                int nprinted = 0;
                foreach (int nkey in rev.Keys)
                {
                    foreach (string sval in rev[nkey])
                    {
                        sb.AppendFormat("        {0}{1}\n", (sval + ": ").PadRight(30, ' '), (-nkey).ToString().PadLeft(8, ' '));
                        if ((++nprinted) >= maxlines)
                        {
                            sb.AppendLine("    --- Additional values omitted ---");
                            break;
                        }
                    } // end foreach(string at this frequency level)
                    if (nprinted >= maxlines) { break; }
                }
                sb.AppendLine();
                return sb.ToString();
            }
            catch (Exception exc)
            {
                return (null);
            }
        } // end method
    }


    public enum FieldType { nullField = 0, stringField = 1, doubleField = 2, intField = 4, dateField = 8 };
}
