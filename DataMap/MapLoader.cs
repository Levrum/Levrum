using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.ClearScript.V8;

using Levrum.Data.Sources;
using Levrum.Data.Classes;
using Levrum.Utils.Geography;

using Newtonsoft.Json;


namespace Levrum.Data.Map
{
    public class MapLoader
    {
        public DataSet<IncidentData> Incidents { get; protected set; } = new DataSet<IncidentData>();

        public Dictionary<string, IncidentData> IncidentsById { get; protected set; } = new Dictionary<string, IncidentData>();

        public Dictionary<IDataSource, List<Tuple<DataMapping, Record>>> ErrorRecords = new Dictionary<IDataSource, List<Tuple<DataMapping, Record>>>();

        public List<CauseData> CauseData { get; set; } = new List<CauseData>();

        public bool LoadMap(DataMap map)
        {
            try
            {
                foreach (IDataSource dataSource in map.DataSources)
                {
                    ErrorRecords[dataSource] = new List<Tuple<DataMapping, Record>>();
                    dataSource.Connect();
                }

                processIncidentMappings(map);
                processIncidentDataMappings(map);
                processResponseDataMappings(map);
                processBenchmarkMappings(map);

                List<ICategoryData> causeTree = new List<ICategoryData>();
                foreach (CauseData cause in map.CauseTree)
                {
                    causeTree.Add(cause);
                }
                CauseData = flattenCauseData(causeTree);

                cleanupIncidentData(map);
                cleanupResponseData(map);
                cleanupBenchmarks(map);

                calculateDerivedBenchmarks(map);
                executePostProcessing(map);
            }
            finally
            {
                foreach (IDataSource dataSource in map.DataSources)
                {
                    try
                    {
                        dataSource.Disconnect();
                        dataSource.Dispose();
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            return false;
        }

        private List<CauseData> flattenCauseData(List<ICategoryData> source)
        {
            List<CauseData> output = new List<CauseData>();
            foreach (CauseData data in source)
            {
                output.Add(data);
                output.AddRange(flattenCauseData(data.Children));
            }

            return output;
        }

        public string[] GetNatureCodeData(string natureCode)
        {
            string[] codeData = new string[3] { null, "Unknown", "Unknown" };
            CauseData parentCause = null;
            ICategorizedValue code = null;

            foreach (CauseData cause in CauseData)
            {
                code = (from n in cause.NatureCodes
                        where n.Value == natureCode
                        select n).FirstOrDefault();

                if (code != null)
                {
                    if (!string.IsNullOrWhiteSpace(code.Description))
                    {
                        codeData[0] = code.Description;
                    }
                    parentCause = cause;
                    break;
                }
            }

            if (parentCause == null)
                return codeData;

            CauseData grandparentCause = (from CauseData cause in CauseData
                                          where cause.Children.Contains(parentCause)
                                          select cause).FirstOrDefault();

            if (grandparentCause == null)
            {
                codeData[1] = parentCause.Name;
            }
            else
            {
                codeData[1] = grandparentCause.Name;
                codeData[2] = parentCause.Name;
            }

            return codeData;
        }

        private void processIncidentMappings(DataMap map)
        {
            HashSet<IDataSource> dataSources = (from mapping in map.IncidentMappings
                                                select mapping?.Column?.DataSource).ToHashSet();

            foreach (IDataSource dataSource in dataSources)
            {
                List<DataMapping> mappingsForSource = (from mapping in map.IncidentMappings
                                                       where ((null!=mapping)&&(mapping.Column.DataSource == dataSource))
                                                       select mapping).ToList();

                if (mappingsForSource.Count == 0)
                {
                    continue;
                }

                List<Record> recordsFromSource = dataSource.GetRecords();
                foreach (Record record in recordsFromSource)
                {
                    IncidentData incident;
                    object idValue = record.GetValue(dataSource.IDColumn);
                    string recordIncidentId = "";
                    if (idValue != null) 
                    {
                        recordIncidentId = idValue.ToString();
                    }

                    if (string.IsNullOrWhiteSpace(recordIncidentId))
                    {
                        ErrorRecords[dataSource].Add(new Tuple<DataMapping, Record>(null, record));
                        continue;
                    }

                    if (!IncidentsById.TryGetValue(recordIncidentId, out incident))
                    {
                        incident = new IncidentData();
                        incident.Id = recordIncidentId;
                        IncidentsById.Add(recordIncidentId, incident);
                        Incidents.Add(incident);
                    }

                    foreach (DataMapping mapping in mappingsForSource)
                    {
                        try
                        {
                            object value = record.GetValue(mapping.Column.ColumnName);
                            if (value == null)
                            {
                                ErrorRecords[dataSource].Add(new Tuple<DataMapping, Record>(mapping, record));
                                continue;
                            }

                            switch (mapping.Field)
                            {
                                case "Time":
                                    DateTime time;
                                    if (value is DateTime)
                                    {
                                        time = (DateTime)value;
                                    }
                                    else
                                    {
                                        value = value.ToString();
                                        if (!DateTime.TryParse(value as string, out time))
                                        {
                                            ErrorRecords[dataSource].Add(new Tuple<DataMapping, Record>(mapping, record));
                                            continue;
                                        }
                                    }
                                    incident.Time = time;
                                    break;
                                case "Location":
                                    incident.Location = value as string;
                                    break;
                                case "Latitude":
                                    double latitude;
                                    if (value is double)
                                    {
                                        latitude = (double)value;
                                    }
                                    else
                                    {
                                        value = value.ToString();
                                        if (!double.TryParse(value as string, out latitude))
                                        {
                                            ErrorRecords[dataSource].Add(new Tuple<DataMapping, Record>(mapping, record));
                                            continue;
                                        }
                                    }
                                    incident.Latitude = latitude;
                                    break;
                                case "Longitude":
                                    double longitude;
                                    if (value is double)
                                    {
                                        longitude = (double)value;
                                    }
                                    else
                                    {
                                        value = value.ToString();
                                        if (!double.TryParse(value as string, out longitude))
                                        {
                                            ErrorRecords[dataSource].Add(new Tuple<DataMapping, Record>(mapping, record));
                                            continue;
                                        }
                                    }
                                    incident.Longitude = longitude;
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!ErrorRecords.ContainsKey(dataSource))
                                ErrorRecords[dataSource] = new List<Tuple<DataMapping, Record>>();

                            ErrorRecords[dataSource].Add(new Tuple<DataMapping, Record>(mapping, record));
                        }
                    }
                }
            }
        }

        private void processIncidentDataMappings(DataMap map)
        {
            HashSet<IDataSource> dataSources = (from mapping in map.IncidentDataMappings
                                                select mapping.Column.DataSource).ToHashSet();

            foreach (IDataSource dataSource in dataSources)
            {
                List<DataMapping> mappingsForSource = (from mapping in map.IncidentDataMappings
                                                       where mapping.Column.DataSource == dataSource
                                                       select mapping).ToList();

                if (mappingsForSource.Count == 0)
                {
                    continue;
                }

                List<Record> recordsFromSource = dataSource.GetRecords();
                foreach (Record record in recordsFromSource)
                {
                    IncidentData incident;
                    object value = record.GetValue(dataSource.IDColumn);
                    string recordIncidentId = null;
                    if (value != null) {
                        recordIncidentId = value.ToString();
                    }

                    if (string.IsNullOrEmpty(recordIncidentId))
                    {
                        ErrorRecords[dataSource].Add(new Tuple<DataMapping, Record>(null, record));
                        continue;
                    }

                    if (!IncidentsById.TryGetValue(recordIncidentId, out incident))
                    {
                        incident = new IncidentData();
                        incident.Id = recordIncidentId;
                        IncidentsById.Add(recordIncidentId, incident);
                        Incidents.Add(incident);
                    }

                    foreach (DataMapping mapping in mappingsForSource)
                    {
                        string stringValue = record.GetValue(mapping.Column.ColumnName).ToString();
                        if (string.IsNullOrEmpty(stringValue))
                        {
                            ErrorRecords[dataSource].Add(new Tuple<DataMapping, Record>(mapping, record));
                            continue;
                        }

                        object parsedValue = getParsedValue(stringValue);

                        incident.Data[mapping.Field] = parsedValue;
                    }
                }
            }
        }

        private void processResponseDataMappings(DataMap map)
        {
            HashSet<IDataSource> dataSources = (from mapping in map.ResponseDataMappings
                                                select mapping.Column.DataSource).ToHashSet();

            foreach (IDataSource dataSource in dataSources)
            {
                List<DataMapping> mappingsForSource = (from mapping in map.ResponseDataMappings
                                                       where mapping.Column.DataSource == dataSource
                                                       select mapping).ToList();

                if (mappingsForSource.Count == 0)
                {
                    continue;
                }

                List<Record> recordsFromSource = dataSource.GetRecords();
                foreach (Record record in recordsFromSource)
                {
                    IncidentData incident;
                    object value = record.GetValue(dataSource.IDColumn);
                    string recordIncidentId = null;
                    if (value != null)
                    {
                        recordIncidentId = value.ToString();
                    }

                    if (string.IsNullOrEmpty(recordIncidentId))
                    {
                        ErrorRecords[dataSource].Add(new Tuple<DataMapping, Record>(null, record));
                        continue;
                    }

                    if (!IncidentsById.TryGetValue(recordIncidentId, out incident))
                    {
                        incident = new IncidentData();
                        incident.Id = recordIncidentId;
                        IncidentsById.Add(recordIncidentId, incident);
                        Incidents.Add(incident);
                    }


                    string recordResponseId = string.Empty;
                    if (!string.IsNullOrEmpty(dataSource.ResponseIDColumn)) {
                        object recordResponseIdValue = record.GetValue(dataSource.ResponseIDColumn);
                        if (recordResponseIdValue != null)
                            recordResponseId = recordResponseIdValue.ToString();
                    }
                    
                    ResponseData response = (from r in incident.Responses
                                             where r.Data.ContainsKey("ResponseID") && recordResponseId == r.Data["ResponseID"] as string
                                             select r).FirstOrDefault();

                    if (response == null)
                    {
                        response = new ResponseData();
                        response.Id = recordIncidentId;
                        response.Data["ResponseID"] = recordResponseId;
                        incident.Responses.Add(response);
                    }

                    foreach (DataMapping mapping in mappingsForSource)
                    {
                        string stringValue = record.GetValue(mapping.Column.ColumnName).ToString();
                        if (string.IsNullOrEmpty(stringValue))
                        {
                            ErrorRecords[dataSource].Add(new Tuple<DataMapping, Record>(mapping, record));
                            continue;
                        }

                        object parsedValue = getParsedValue(stringValue);

                        response.Data[mapping.Field] = parsedValue;
                    }
                }
            }
        }

        private void processBenchmarkMappings(DataMap map)
        {
            HashSet<IDataSource> dataSources = (from mapping in map.BenchmarkMappings
                                                select mapping.Column.DataSource).ToHashSet();

            foreach (IDataSource dataSource in dataSources)
            {
                List<DataMapping> mappingsForSource = (from mapping in map.BenchmarkMappings
                                                       where mapping.Column.DataSource == dataSource
                                                       select mapping).ToList();

                if (mappingsForSource.Count == 0)
                {
                    continue;
                }

                List<Record> recordsFromSource = dataSource.GetRecords();
                foreach (Record record in recordsFromSource)
                {
                    IncidentData incident;
                    object value = record.GetValue(dataSource.IDColumn);
                    string recordIncidentId = null;
                    if (value != null)
                    {
                        recordIncidentId = value.ToString();
                    }

                    if (string.IsNullOrEmpty(recordIncidentId))
                    {
                        ErrorRecords[dataSource].Add(new Tuple<DataMapping, Record>(null, record));
                        continue;
                    }

                    if (!IncidentsById.TryGetValue(recordIncidentId, out incident))
                    {
                        incident = new IncidentData();
                        incident.Id = recordIncidentId;
                        IncidentsById.Add(recordIncidentId, incident);
                        Incidents.Add(incident);
                    }

                    value = record.GetValue(dataSource.ResponseIDColumn);
                    string recordResponseId = "";
                    if (value != null)
                    {
                        recordResponseId = value.ToString();
                    }

                    ResponseData response = (from r in incident.Responses
                                             where r.Data.ContainsKey("ResponseID") && recordResponseId == r.Data["ResponseID"] as string
                                             select r).FirstOrDefault();
                    if (response == null)
                    {
                        response = new ResponseData();
                        response.Data["ResponseID"] = recordResponseId;
                        incident.Responses.Add(response);
                    }

                    foreach (DataMapping mapping in mappingsForSource)
                    {
                        value = record.GetValue(mapping.Column.ColumnName);
                        string stringValue = value == null ? "" : value.ToString();

                        if (string.IsNullOrEmpty(stringValue))
                        {
                            ErrorRecords[dataSource].Add(new Tuple<DataMapping, Record>(mapping, record));
                            continue;
                        }

                        object parsedValue = getParsedValue(stringValue);

                        BenchmarkData benchmark = new BenchmarkData();
                        benchmark.Name = mapping.Field;
                        if (parsedValue is double)
                        {
                            benchmark.Value = (double)parsedValue;
                        }
                        benchmark.Data["RawData"] = parsedValue;
                        response.Benchmarks.Add(benchmark);
                    }
                }
            }
        }

        private object getParsedValue(string stringValue)
        {
            object parsedValue = null;
            DateTime dateValue;
            double doubleValue;
            int intValue;

            if (DateTime.TryParse(stringValue, out dateValue))
            {
                parsedValue = dateValue;
            }
            else if (int.TryParse(stringValue, out intValue))
            {
                parsedValue = intValue;
            }
            else if (double.TryParse(stringValue, out doubleValue))
            {
                parsedValue = doubleValue;
            }
            else
            {
                parsedValue = stringValue;
            }

            return parsedValue;
        }

        private void cleanupIncidentData(DataMap map)
        {
            CoordinateConverter converter = null;
            bool convertCoordinates = map.EnableCoordinateConversion;
            try
            {
                converter = new CoordinateConverter(map.Projection);
            }
            catch (Exception ex)
            {
                convertCoordinates = false;
            }

            foreach (IncidentData incident in Incidents)
            {
                if (convertCoordinates)
                {
                    double[] xyPoint = { incident.Longitude, incident.Latitude };
                    LatitudeLongitude latLon = converter.ConvertXYToLatLon(xyPoint);
                    incident.Latitude = latLon.Latitude;
                    incident.Longitude = latLon.Longitude;
                }

                if (map.InvertLatitude)
                {
                    incident.Latitude = incident.Latitude * -1.0;
                }

                if (map.InvertLongitude)
                {
                    incident.Longitude = incident.Longitude * -1.0;
                }

                string natureCode = "Unknown";
                if (incident.Data.ContainsKey("Code"))
                {
                    natureCode = incident.Data["Code"].ToString();
                }
                string[] natureCodeData = GetNatureCodeData(natureCode);
                if (!string.IsNullOrWhiteSpace(natureCodeData[0]))
                {
                    incident.Data["CodeDescription"] = natureCodeData[0];
                }

                incident.Data["Category"] = natureCodeData[1];
                incident.Data["Type"] = natureCodeData[2];
            }
        }

        private void cleanupResponseData(DataMap map)
        {

        }

        private void cleanupBenchmarks(DataMap map)
        {
            foreach (IncidentData incident in Incidents)
            {
                DateTime baseTime = incident.Time;
                if (incident.Data.ContainsKey("FirstEffAction"))
                {
                    object firstEffAction = incident.Data["FirstEffAction"];
                    if (firstEffAction is DateTime)
                    {
                        baseTime = (DateTime)firstEffAction;
                    }
                    else
                    {
                        DateTime firstEffActionDate;
                        if (DateTime.TryParse(firstEffAction as string, out firstEffActionDate))
                        {
                            baseTime = firstEffActionDate;
                        }
                    }
                }

                foreach (ResponseData response in incident.Responses)
                {
                    BenchmarkData assigned = (from b in response.Benchmarks
                                              where b.Name == "Assigned"
                                              select b).FirstOrDefault();

                    DateTime assignedTime = DateTime.MinValue;
                    if (assigned != null)
                    {
                        if (!double.IsNaN(assigned.Value))
                        {
                            assignedTime = baseTime.AddMinutes(assigned.Value);
                        }
                        else if (assigned.Data.ContainsKey("RawData"))
                        {
                            if (assigned.Data["RawData"] is DateTime)
                            {
                                assignedTime = (DateTime)assigned.Data["RawData"];
                                assigned.Value = (assignedTime - baseTime).TotalMinutes;
                            }
                            else if (DateTime.TryParse(assigned.Data["RawData"] as string, out assignedTime))
                            {
                                assigned.Value = (assignedTime - baseTime).TotalMinutes;
                            }
                        }
                        assigned.Data["DateTime"] = assignedTime;
                    }

                    BenchmarkData responding = (from b in response.Benchmarks
                                                where b.Name == "Responding"
                                                select b).FirstOrDefault();

                    DateTime respondingTime = DateTime.MinValue;
                    if (responding != null)
                    {
                        if (!double.IsNaN(responding.Value))
                        {
                            respondingTime = baseTime.AddMinutes(responding.Value);
                        }
                        else if (responding.Data.ContainsKey("RawData"))
                        {
                            if (responding.Data["RawData"] is DateTime)
                            {
                                respondingTime = (DateTime)responding.Data["RawData"];
                                responding.Value = (respondingTime - baseTime).TotalMinutes;
                            }
                            else if (DateTime.TryParse(responding.Data["RawData"] as string, out respondingTime))
                            {
                                responding.Value = (respondingTime - baseTime).TotalMinutes;
                            }
                        }
                        responding.Data["DateTime"] = respondingTime;
                    }

                    BenchmarkData turnout = (from b in response.Benchmarks
                                             where b.Name == "TurnoutTime"
                                             select b).FirstOrDefault();
                    if (turnout == null)
                    {
                        turnout = new BenchmarkData("TurnoutTime", Math.Max(0, (respondingTime - assignedTime).TotalMinutes));
                        response.Benchmarks.Add(turnout);
                    }

                    BenchmarkData onScene = (from b in response.Benchmarks
                                             where b.Name == "OnScene"
                                             select b).FirstOrDefault();

                    DateTime onSceneTime = DateTime.MinValue;
                    if (onScene != null)
                    {
                        if (!double.IsNaN(onScene.Value))
                        {
                            onSceneTime = baseTime.AddMinutes(onScene.Value);
                        }
                        else if (onScene.Data.ContainsKey("RawData"))
                        {
                            if (onScene.Data["RawData"] is DateTime)
                            {
                                onSceneTime = (DateTime)onScene.Data["RawData"];
                                onScene.Value = (onSceneTime - baseTime).TotalMinutes;
                            }
                            else if (DateTime.TryParse(onScene.Data["RawData"] as string, out onSceneTime))
                            {
                                onScene.Value = (onSceneTime - baseTime).TotalMinutes;
                            }
                        }
                        onScene.Data["DateTime"] = onSceneTime;
                    }

                    BenchmarkData travel = (from b in response.Benchmarks
                                            where b.Name == "TravelTime"
                                            select b).FirstOrDefault();
                    if (travel == null)
                    {
                        travel = new BenchmarkData("TravelTime", Math.Max(0, (onSceneTime - respondingTime).TotalMinutes));
                        response.Benchmarks.Add(travel);
                    }

                    BenchmarkData clearScene = (from b in response.Benchmarks
                                                where b.Name == "ClearScene"
                                                select b).FirstOrDefault();

                    DateTime clearSceneTime = DateTime.MinValue;
                    if (clearScene != null)
                    {
                        if (!double.IsNaN(clearScene.Value))
                        {
                            clearSceneTime = baseTime.AddMinutes(clearScene.Value);
                        }
                        else if (clearScene.Data.ContainsKey("RawData"))
                        {
                            if (clearScene.Data["RawData"] is DateTime)
                            {
                                clearSceneTime = (DateTime)clearScene.Data["RawData"];
                                clearScene.Value = (clearSceneTime - baseTime).TotalMinutes;
                            }
                            else if (DateTime.TryParse(clearScene.Data["RawData"] as string, out clearSceneTime))
                            {
                                clearScene.Value = (clearSceneTime - baseTime).TotalMinutes;
                            }
                        }
                        clearScene.Data["DateTime"] = clearSceneTime;
                    }

                    BenchmarkData inService = (from b in response.Benchmarks
                                               where b.Name == "InService"
                                               select b).FirstOrDefault();

                    DateTime inServiceTime = DateTime.MinValue;
                    if (inService != null)
                    {
                        if (!double.IsNaN(inService.Value))
                        {
                            inServiceTime = baseTime.AddMinutes(inService.Value);
                        }
                        else if (inService.Data.ContainsKey("RawData"))
                        {
                            if (inService.Data["RawData"] is DateTime)
                            {
                                inServiceTime = (DateTime)inService.Data["RawData"];
                                inService.Value = (inServiceTime - baseTime).TotalMinutes;
                            }
                            else if (DateTime.TryParse(inService.Data["RawData"] as string, out inServiceTime))
                            {
                                inService.Value = (inServiceTime - baseTime).TotalMinutes;
                            }
                        }
                        inService.Data["DateTime"] = inServiceTime;
                    }

                    if (clearSceneTime == DateTime.MinValue)
                    {
                        clearSceneTime = inServiceTime;
                    }

                    BenchmarkData committed = (from b in response.Benchmarks
                                               where b.Name == "CommittedHours"
                                               select b).FirstOrDefault();

                    if (committed == null)
                    {
                        committed = new BenchmarkData("CommittedHours", Math.Max(0, (clearSceneTime - assignedTime).TotalMinutes));
                        response.Benchmarks.Add(committed);
                    }

                    BenchmarkData sceneTime = (from b in response.Benchmarks
                                               where b.Name == "SceneTime"
                                               select b).FirstOrDefault();

                    if (sceneTime == null && onSceneTime != DateTime.MinValue)
                    {
                        sceneTime = new BenchmarkData("SceneTime", (clearSceneTime - onSceneTime).TotalMinutes);
                        response.Benchmarks.Add(sceneTime);
                    }

                    BenchmarkData inQuarters = (from b in response.Benchmarks
                                                where b.Name == "InQuarters"
                                                select b).FirstOrDefault();

                    DateTime inQuartersTime = DateTime.MinValue;
                    if (inQuarters != null)
                    {
                        if (!double.IsNaN(inQuarters.Value))
                        {
                            inQuartersTime = baseTime.AddMinutes(inQuarters.Value);
                        }
                        else if (inQuarters.Data.ContainsKey("RawData"))
                        {
                            if (inQuarters.Data["RawData"] is DateTime)
                            {
                                inQuartersTime = (DateTime)inQuarters.Data["RawData"];
                                inQuarters.Value = (inQuartersTime - baseTime).TotalMinutes;
                            }
                            else if (DateTime.TryParse(inQuarters.Data["RawData"] as string, out inQuartersTime))
                            {
                                inQuarters.Value = (inQuartersTime - baseTime).TotalMinutes;
                            }
                        }
                        inQuarters.Data["DateTime"] = inQuartersTime;
                    }
                }
            }
        }

        private void calculateDerivedBenchmarks(DataMap map)
        {
            foreach (IncidentData incident in Incidents)
            {
                ResponseData firstArrival = null;
                ResponseData lastArrival = null;
                ResponseData firstResponse = null;
                double firstOnScene = double.MaxValue;
                double lastOnScene = 0.0;
                double firstResponding = double.MaxValue;
                foreach (ResponseData response in incident.Responses)
                {
                    double onScene = (from BenchmarkData bmk in response.Benchmarks
                                      where bmk.Name == "OnScene"
                                      select bmk.Value).FirstOrDefault();

                    if (onScene > lastOnScene || lastArrival == null)
                    {
                        lastArrival = response;
                        lastOnScene = onScene;
                    }

                    if (onScene < firstOnScene || firstArrival == null)
                    {
                        firstArrival = response;
                        firstOnScene = onScene;
                    }

                    double responding = (from BenchmarkData bmk in response.Benchmarks
                                         where bmk.Name == "Responding"
                                         select bmk.Value).FirstOrDefault();

                    if (responding < firstResponding || firstResponse == null)
                    {
                        firstResponding = responding;
                        firstResponse = response;
                    }
                }

                if (firstArrival != null)
                    firstArrival.Benchmarks.Add(new BenchmarkData("FirstArrival", firstOnScene));
                if (lastArrival != null)
                    lastArrival.Benchmarks.Add(new BenchmarkData("FullComplement", lastOnScene));
                if (firstResponse != null)
                    firstResponse.Benchmarks.Add(new BenchmarkData("FirstResponding", firstResponding));
            }
        }

        private void executePostProcessing(DataMap map)
        {
            if (map.PostProcessingScript == string.Empty)
            {
                return;
            }
            try
            {
                using (V8ScriptEngine v8 = new V8ScriptEngine())
                {
                    v8.AddHostObject("Incidents", Incidents);
                    v8.AddHostType("IncidentData", typeof(IncidentData));
                    v8.AddHostType("ResponseData", typeof(ResponseData));
                    v8.AddHostType("BenchmarkData", typeof(BenchmarkData));
                    v8.AddHostType("bool", typeof(bool));
                    v8.AddHostType("double", typeof(double));
                    v8.AddHostType("int", typeof(int));
                    v8.AddHostType("DateTime", typeof(DateTime));
                    v8.AddHostType("TimeSpan", typeof(TimeSpan));

                    v8.Execute(map.PostProcessingScript);
                }
            } catch (Exception ex)
            {

            }
        }
    }
}
