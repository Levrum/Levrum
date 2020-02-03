using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;

// using CsvHelper;

using Levrum.Utils;
using Levrum.Utils.Data;
using Levrum.Utils.MathAndStats;

namespace Levrum.Data.Classes.Tools
{
    public static class IncidentDataTools
    {
        public static string[] s_ignoredIncidentDataFields = new string[] { "Responses" };
        public static string[] s_ignoredResponseDataFields = new string[] { "TimingData" };

        public static void CreateCsvs(DataSet<IncidentData> incidents, string incidentFile, string responseFile)
        {
            const string fn = "IncidentDataTools.CreateCsvs()";
            Type dtype = typeof(IncidentDataTools);
            try
            {
                HashSet<string> incidentDataFields = new HashSet<string>();
                HashSet<string> responseDataFields = new HashSet<string>();
                HashSet<string> benchmarkNames = new HashSet<string>();
                foreach (IncidentData incident in incidents)
                {
                    foreach (string key in incident.Data.Keys)
                    {
                        if (!s_ignoredIncidentDataFields.Contains(key))
                        {
                            incidentDataFields.Add(key);
                        }
                    }
                    foreach (ResponseData response in incident.Responses)
                    {
                        foreach (string key in response.Data.Keys)
                        {
                            if (!s_ignoredResponseDataFields.Contains(key))
                            {
                                responseDataFields.Add(key);
                            }
                        }
                        foreach (TimingData benchmark in response.TimingData)
                        {
                            benchmarkNames.Add(benchmark.Name);
                        }
                    }
                }

                List<dynamic> incidentRecords = new List<dynamic>();
                List<dynamic> responseRecords = new List<dynamic>();
                foreach (IncidentData incident in incidents)
                {
                    dynamic incidentRecord = new ExpandoObject();
                    foreach (string field in incidentDataFields)
                    {
                        IDictionary<string, object> inc_dict = incidentRecord as IDictionary<string, object>;
                        if (inc_dict.ContainsKey(field))
                        {
                            LogHelper.LogErrOnce(fn, "Fieldname '" + field + "' is apparently duplicated in the incident data map");
                        }
                        else if (incident.Data.ContainsKey(field))
                        {
                            inc_dict.Add(field, incident.Data[field]);
                        }
                        else
                        {
                            inc_dict.Add(field, string.Empty);
                        }
                    }

                    foreach (ResponseData response in incident.Responses)
                    {
                        dynamic responseRecord = new ExpandoObject();
                        responseRecord.Id = incident.Id;
                        IDictionary<string, object> rsp_dict = responseRecord as IDictionary<string, object>;
                        foreach (string field in responseDataFields)
                        {
                            if (rsp_dict.ContainsKey(field))
                            {
                                LogHelper.LogErrOnce(fn, "Fieldname '" + field + "' is apparently duplicated in the response data map");
                            }
                            else if (response.Data.ContainsKey(field))
                            {
                                rsp_dict.Add(field, response.Data[field]);
                            }
                            else
                            {
                                rsp_dict.Add(field, string.Empty);
                            }
                        }

                        foreach (string benchmarkName in benchmarkNames)
                        {
                            TimingData benchmark = (from bmk in response.TimingData
                                                    where bmk.Name == benchmarkName
                                                    select bmk).FirstOrDefault();
                            if (rsp_dict.ContainsKey(benchmarkName))
                            {
                                LogHelper.LogErrOnce(fn, "Benchmark '" + benchmarkName + "' is apparently duplicated in the benchmark data map");
                            }
                            else if (benchmark != null)
                            {
                                object value;
                                if (benchmark.Data.ContainsKey("DateTime"))
                                {
                                    value = benchmark.Data["DateTime"];
                                }
                                else
                                {
                                    value = benchmark.Value;
                                }
                                rsp_dict.Add(benchmarkName, value);
                            }
                            else
                            {
                                rsp_dict.Add(benchmarkName, string.Empty);
                            }
                        }
                        responseRecords.Add(responseRecord);
                    }
                    incidentRecords.Add(incidentRecord);
                }

                using (StringWriter writer = new StringWriter())
                {
                    using (CsvHelper.CsvWriter csv = new CsvHelper.CsvWriter(writer))
                    {
                        csv.WriteRecords(incidentRecords);
                    }
                    File.WriteAllText(incidentFile, writer.ToString());
                }

                using (StringWriter writer = new StringWriter())
                {
                    using (CsvHelper.CsvWriter csv = new CsvHelper.CsvWriter(writer))
                    {
                        csv.WriteRecords(responseRecords);
                    }
                    File.WriteAllText(responseFile, writer.ToString());
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Error converting Incident Data to CSVs", true);
            }
        }

        public static HashSet<string> GetUnitsFromIncidents(List<IncidentData> incidents)
        {
            HashSet<string> units = new HashSet<string>();

            foreach (IncidentData incident in incidents)
            {
                foreach (ResponseData response in incident.Responses)
                {
                    if (response.Data.ContainsKey("Unit"))
                    {
                        units.Add(response.Data["Unit"] as string);
                    }
                }
            }

            return units;
        }
    }
}
