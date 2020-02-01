using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;

using CsvHelper;

using Levrum.Data.Classes;
using Levrum.Utils.MathAndStats;

namespace Levrum.Utils.Data
{
    public static class IncidentDataTools
    {
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
                        incidentDataFields.Add(key);
                    }
                    foreach (ResponseData response in incident.Responses)
                    {
                        foreach (string key in response.Data.Keys)
                        {
                            responseDataFields.Add(key);
                        }
                        foreach (TimingData benchmark in response.TimingData)
                        {
                            benchmarkNames.Add(benchmark.Name);
                        }
                    }
                }

                List<ExpandoObject> incidentRecords = new List<ExpandoObject>();
                List<ExpandoObject> responseRecords = new List<ExpandoObject>();
                foreach (IncidentData incident in incidents)
                {
                    dynamic incidentRecord = new ExpandoObject();
                    incidentRecord.Id = incident.Id;
                    incidentRecord.Time = incident.Time;
                    incidentRecord.Location = incident.Location;
                    incidentRecord.Latitude = incident.Latitude;
                    incidentRecord.Longitude = incident.Longitude;
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


                if (!CsvAnalyzer.SaveExpandosAsCsv(incidentFile, incidentRecords, false))
                {
                    Util.HandleAppErr(dtype, fn, "Error saving incident data to " + incidentFile);
                }
                //using (StringWriter writer = new StringWriter())
                //{
                //    using (CsvWriter csv = new CsvWriter(writer))
                //    {
                //        foreach (ExpandoObject irec in incidentRecords)
                //        {
                //            csv.WriteRecord<ExpandoObject>(irec);
                //        }
                //        //csv.WriteRecords(incidentRecords);
                //    }
                //    File.WriteAllText(incidentFile, writer.ToString());
                //}

                if (!CsvAnalyzer.SaveExpandosAsCsv(responseFile, responseRecords,false))
                {
                    Util.HandleAppErr(dtype, fn, "Error saving response data to " + responseFile);
                }
                //using (StringWriter writer = new StringWriter())
                //{
                //    using (CsvWriter csv = new CsvWriter(writer))
                //    {
                //        csv.WriteRecords(responseRecords);
                //    }
                //    File.WriteAllText(responseFile, writer.ToString());
                //}
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
                    if (response.Data.ContainsKey("Unit")) {
                        units.Add(response.Data["Unit"] as string);
                    }
                }
            }

            return units;
        }
    }
}
