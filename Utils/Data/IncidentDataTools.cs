using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;

using CsvHelper;

using Levrum.Data.Classes;

namespace Levrum.Utils.Data
{
    public static class IncidentDataTools
    {
        public static void CreateCsvs(DataSet<IncidentData> incidents, string incidentFile, string responseFile)
        {
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

                List<dynamic> incidentRecords = new List<dynamic>();
                List<dynamic> responseRecords = new List<dynamic>();
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
                        if (incident.Data.ContainsKey(field))
                        {
                            ((IDictionary<string, object>)incidentRecord).Add(field, incident.Data[field]);
                        }
                        else
                        {
                            ((IDictionary<string, object>)incidentRecord).Add(field, string.Empty);
                        }
                    }

                    foreach (ResponseData response in incident.Responses)
                    {
                        dynamic responseRecord = new ExpandoObject();
                        responseRecord.Id = incident.Id;
                        foreach (string field in responseDataFields)
                        {
                            if (response.Data.ContainsKey(field))
                            {
                                ((IDictionary<string, object>)responseRecord).Add(field, response.Data[field]);
                            }
                            else
                            {
                                ((IDictionary<string, object>)responseRecord).Add(field, string.Empty);
                            }
                        }

                        foreach (string benchmarkName in benchmarkNames)
                        {
                            TimingData benchmark = (from bmk in response.TimingData
                                                       where bmk.Name == benchmarkName
                                                       select bmk).FirstOrDefault();

                            if (benchmark != null)
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
                                ((IDictionary<string, object>)responseRecord).Add(benchmarkName, value);
                            }
                            else
                            {
                                ((IDictionary<string, object>)responseRecord).Add(benchmarkName, string.Empty);
                            }
                        }
                        responseRecords.Add(responseRecord);
                    }
                    incidentRecords.Add(incidentRecord);
                }

                using (StringWriter writer = new StringWriter())
                {
                    using (CsvWriter csv = new CsvWriter(writer))
                    {
                        csv.WriteRecords(incidentRecords);
                    }
                    File.WriteAllText(incidentFile, writer.ToString());
                }

                using (StringWriter writer = new StringWriter())
                {
                    using (CsvWriter csv = new CsvWriter(writer))
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
                    if (response.Data.ContainsKey("Unit")) {
                        units.Add(response.Data["Unit"] as string);
                    }
                }
            }

            return units;
        }
    }
}
