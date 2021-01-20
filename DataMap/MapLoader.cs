using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;

using Levrum.Data.Sources;
using Levrum.Data.Classes;
using Levrum.Data.Classes.Tools;

using Levrum.Utils;
using Levrum.Utils.Data;
using Levrum.Utils.Geography;

using NLog;
using NLogLevel = NLog.LogLevel;
using NLog.Config;
using NLog.Targets;

using Newtonsoft.Json;

namespace Levrum.Data.Map
{
    public class MapLoader
    {
        public DataSet<IncidentData> Incidents { get; protected set; } = new DataSet<IncidentData>();
        public Dictionary<string, IncidentData> IncidentsById { get; protected set; } = new Dictionary<string, IncidentData>();

        private ConcurrentBag<IncidentData> IncidentQueue { get; set; } = new ConcurrentBag<IncidentData>();

        public List<MapLoaderError> ErrorRecords = new List<MapLoaderError>(); // Dictionary<IDataSource, List<Tuple<DataMapping, Record>>> ErrorRecords = new Dictionary<IDataSource, List<Tuple<DataMapping, Record>>>();

        public List<CauseData> CauseData { get; set; } = new List<CauseData>();

        public JavascriptDebugHost DebugHost { get; set; } = new JavascriptDebugHost();

        public BackgroundWorker Worker { get; set; }
        public event MapLoaderProgressListener OnProgressUpdate;

        private const int c_numSteps = 10;

        public int JSProgressStep { get; set; } = -1;

        public Logger Logger { get; set; }

        public DataMap Map { get; set; }

        public DateTime StartDate { get; set; } = DateTime.MinValue;
        public DateTime EndDate { get; set; } = DateTime.MaxValue;

        public MapLoader()
        {
            Logger = LogManager.GetCurrentClassLogger();
        }

        public bool LoadMap(DataMap map)
        {
            try
            {
                Map = map;
                foreach (IDataSource dataSource in map.DataSources)
                {
                    OnProgressUpdate?.Invoke(this, string.Format("Connecting to data source '{0}'...", dataSource.Name), 0.0); 
                    bool connect_ok = dataSource.Connect();
                    if (connect_ok)
                    {
                        OnProgressUpdate?.Invoke(this, string.Format("Connected to data source '{0}'.", dataSource.Name), 0.0);
                    }
                    else
                    {
                        OnProgressUpdate?.Invoke(this, string.Format("Error connecting to data source '{0}': {1}", dataSource.Name, dataSource.ErrorMessage), 0.0);
                        return (false);
                    }
                }

                Incidents = new DataSet<IncidentData>();

                processIncidentDataMappings();

                if (Cancelling())
                    return false;

                processResponseDataMappings();

                if (Cancelling())
                    return false;

                processBenchmarkMappings();

                if (Cancelling())
                    return false;

                List<ICategoryData> causeTree = new List<ICategoryData>();
                foreach (CauseData cause in map.CauseTree)
                {
                    causeTree.Add(cause);
                }
                CauseData = flattenCauseData(causeTree);

                executePostLoading();

                cleanupIncidentData();

                if (Cancelling())
                    return false;

                cleanupResponseData();

                if (Cancelling())
                    return false;

                cleanupBenchmarks();

                if (Cancelling())
                    return false;

                processGeoSources();

                if (Cancelling())
                    return false;

                calculateDerivedBenchmarks();

                if (Cancelling())
                    return false;

                executePostProcessing();

                if (Cancelling())
                    return false;

                DateTime firstIncident = DateTime.MaxValue;
                DateTime lastIncident = DateTime.MinValue;
                foreach (IncidentData incident in Incidents)
                {
                    if (incident.Time != DateTime.MinValue)
                    {
                        firstIncident = incident.Time < firstIncident ? incident.Time : firstIncident;
                        lastIncident = incident.Time > lastIncident ? incident.Time : lastIncident;
                    }
                }

                Incidents.Data.SetValue("FirstIncident", firstIncident);
                Incidents.Data.SetValue("LastIncident", lastIncident);
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
            return true;
        }

        public bool LoadMapAndAppend(DataMap map, DataSet<IncidentData> lastIncidents)
        {
            if (StartDate == DateTime.MinValue && lastIncidents.Data.ContainsKey("LastIncident") && lastIncidents.Data["LastIncident"] is DateTime)
            {
                StartDate = (DateTime)lastIncidents.Data["LastIncident"];
                StartDate = StartDate.AddMilliseconds(1.0);
            }

            if (EndDate == DateTime.MaxValue)
            {
                EndDate = DateTime.Now;
            }

            bool success = LoadMap(map);
            if (!success)
            {
                return false;
            }

            DataSet<IncidentData> loaderOutput = Incidents;
            Incidents = new DataSet<IncidentData>();
            Incidents.AddRange(lastIncidents);
            Incidents.AddRange(loaderOutput);

            DateTime firstIncident = DateTime.MaxValue;
            DateTime lastIncident = DateTime.MinValue;
            foreach (IncidentData incident in Incidents)
            {
                if (incident.Time != DateTime.MinValue) {
                    firstIncident = incident.Time < firstIncident ? incident.Time : firstIncident;
                    lastIncident = incident.Time > lastIncident ? incident.Time : lastIncident;
                }
            }

            Incidents.Data.SetValue("FirstIncident", firstIncident);
            Incidents.Data.SetValue("LastIncident", lastIncident);

            return true;
        }
        
        public bool Cancelling()
        {
            if (Worker == null)
            {
                return false;
            }
            else
            {
                return Worker.CancellationPending;
            }
        }

        private DateTime m_lastUpdateTime = DateTime.MinValue;

        private void updateProgress(int step, string message, double completionPercentage, bool forceUpdate = false)
        {
            if (Cancelling())
            {
                message = "Cancelling operation...";
                OnProgressUpdate?.Invoke(this, message, 100);
            }
            else
            {
                DateTime now = DateTime.Now;
                TimeSpan timeSinceUpdate = now - m_lastUpdateTime;
                if (timeSinceUpdate.TotalMilliseconds < 5 && !forceUpdate)
                {
                    return;
                }

                m_lastUpdateTime = now;
                message = string.Format("Step {0}/{1}: {2}", step, c_numSteps, message);
                OnProgressUpdate?.Invoke(this, message, completionPercentage);
            }
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

        public void AddErrorRecord(MapLoaderErrorType type, IDataSource dataSource, DataMapping mapping, Record record, string details = "")
        {
            ErrorRecords.Add(new MapLoaderError(type, dataSource, mapping, record, details));
        }

        private void processIncidentMappings()
        {
            /// This function is deprecated and here only for reference
            /// All incident mappings are now processed as IncidentData mappings
            HashSet<IDataSource> dataSources = (from mapping in Map.IncidentMappings
                                                select mapping?.Column?.DataSource).ToHashSet();

            updateProgress(1, string.Format("Loading incident records from {0} data sources", dataSources.Count), 0, true);

            int numSources = dataSources.Count;
            int completedSources = 0;
            foreach (IDataSource dataSource in dataSources)
            {
                List<DataMapping> mappingsForSource = (from mapping in Map.IncidentMappings
                                                       where (null != mapping) && (mapping.Column.DataSource == dataSource)
                                                       select mapping).ToList();

                if (mappingsForSource.Count == 0)
                {
                    continue;
                }

                double progressPerSource = 100 / numSources;
                double progress = completedSources * progressPerSource;
                updateProgress(1, string.Format("Getting incident records from data source {0}", dataSource.Name), progress);

                List<Record> recordsFromSource = dataSource.GetRecords();
                int recordNumber = 0;
                foreach (Record record in recordsFromSource)
                {
                    if (Cancelling())
                        return;

                    recordNumber++;

                    double recordProgress = progressPerSource / recordsFromSource.Count;
                    updateProgress(1, string.Format("Processing incident record {0} out of {1} from data source {2}", recordNumber, recordsFromSource.Count, dataSource.Name), progress + (recordProgress * recordNumber), recordNumber == recordsFromSource.Count);
                    IncidentData incident;
                    object idValue = record.GetValue(dataSource.IDColumn);
                    string recordIncidentId = "";
                    if (idValue != null)
                    {
                        recordIncidentId = idValue.ToString();
                    }

                    if (string.IsNullOrWhiteSpace(recordIncidentId))
                    {
                        AddErrorRecord(MapLoaderErrorType.NullIncidentId, dataSource, null, record);
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
                                AddErrorRecord(MapLoaderErrorType.NullValue, dataSource, mapping, record);
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
                                            AddErrorRecord(MapLoaderErrorType.BadValue, dataSource, mapping, record);
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
                                            AddErrorRecord(MapLoaderErrorType.BadValue, dataSource, mapping, record);
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
                                            AddErrorRecord(MapLoaderErrorType.BadValue, dataSource, mapping, record);
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
                            AddErrorRecord(MapLoaderErrorType.LoaderException, dataSource, mapping, record, string.Format("{0}\n{1}", ex.Message, ex.StackTrace));
                        }
                    }
                }
            }
        }

        private void processIncidentDataMappings()
        {
            HashSet<IDataSource> dataSources = (from mapping in Map.IncidentDataMappings
                                                select mapping.Column.DataSource).ToHashSet();

            updateProgress(1, string.Format("Loading incident data records from {0} data sources", dataSources.Count), 0, true);

            int numSources = dataSources.Count;
            int completedSources = 0;
            foreach (IDataSource dataSource in dataSources)
            {
                List<DataMapping> mappingsForSource = (from mapping in Map.IncidentDataMappings
                                                       where mapping.Column.DataSource == dataSource
                                                       select mapping).ToList();

                if (mappingsForSource.Count == 0)
                {
                    continue;
                }

                double progressPerSource = 100 / numSources;
                double progress = completedSources * progressPerSource;
                updateProgress(1, string.Format("Getting incident data records from data source {0}", dataSource.Name), progress);
                List<Record> recordsFromSource = dataSource.GetRecords(StartDate, EndDate);
                int recordNumber = 0;
                foreach (Record record in recordsFromSource)
                {
                    if (Cancelling())
                        return;

                    recordNumber++;

                    double recordProgress = progressPerSource / recordsFromSource.Count;
                    updateProgress(1, string.Format("Processing incident data record {0} out of {1} from data source {2}", recordNumber, recordsFromSource.Count, dataSource.Name), progress + (recordProgress * recordNumber), recordNumber == recordsFromSource.Count);
                    IncidentData incident;
                    object value = record.GetValue(dataSource.IDColumn);
                    string recordIncidentId = null;
                    if (value != null)
                    {
                        recordIncidentId = value.ToString();
                    }

                    if (string.IsNullOrEmpty(recordIncidentId))
                    {
                        AddErrorRecord(MapLoaderErrorType.NullIncidentId, dataSource, null, record);
                        continue;
                    }

                    if (!IncidentsById.TryGetValue(recordIncidentId, out incident))
                    {
                        incident = new IncidentData();
                        incident.Id = recordIncidentId;
                        IncidentsById.Add(recordIncidentId, incident);
                        Incidents.Add(incident);
                    }

                    int nmapping = 0;
                    foreach (DataMapping mapping in mappingsForSource)
                    {
                        nmapping++;
                        string scolname = mapping?.Column?.ColumnName;
                        if (string.IsNullOrEmpty(scolname)) { throw (new Exception("Unable to identify column name in incident mapping #" + nmapping)); }
                        if (!record.HasColumn(scolname)) 
                        {
                            AddErrorRecord(MapLoaderErrorType.NullValue, dataSource, mapping, record); // throw (new Exception("Incident record does not contain column '" + scolname + "' (#" + nmapping + ")"));  
                            continue;
                        }

                        string stringValue = record.GetValue(mapping.Column.ColumnName).ToString();
                        if (string.IsNullOrEmpty(stringValue))
                        {
                            AddErrorRecord(MapLoaderErrorType.NullValue, dataSource, mapping, record);
                            continue;
                        }

                        object parsedValue = getParsedValue(stringValue);

                        incident.Data[mapping.Field] = parsedValue;
                    }
                }
            }
        }

        private void processResponseDataMappings()
        {
            HashSet<IDataSource> dataSources = (from mapping in Map.ResponseDataMappings
                                                select mapping.Column.DataSource).ToHashSet();

            updateProgress(2, string.Format("Loading response data records from {0} data sources", dataSources.Count), 0, true);

            int numSources = dataSources.Count;
            int completedSources = 0;
            foreach (IDataSource dataSource in dataSources)
            {
                List<DataMapping> mappingsForSource = (from mapping in Map.ResponseDataMappings
                                                       where mapping.Column.DataSource == dataSource
                                                       select mapping).ToList();

                if (mappingsForSource.Count == 0)
                {
                    continue;
                }


                if (string.IsNullOrEmpty(dataSource.ResponseIDColumn))
                {
                    AddErrorRecord(MapLoaderErrorType.NoResponseIdColumn, dataSource, null, null);
                    continue;
                }

                double progressPerSource = 100 / numSources;
                double progress = completedSources * progressPerSource;
                updateProgress(2, string.Format("Getting response data records from data source {0}", dataSource.Name), progress);
                List<Record> recordsFromSource = dataSource.GetRecords(StartDate, EndDate);
                int recordNumber = 0;
                foreach (Record record in recordsFromSource)
                {
                    if (Cancelling())
                        return;

                    recordNumber++;

                    double recordProgress = progressPerSource / recordsFromSource.Count;
                    updateProgress(2, string.Format("Processing response data record {0} out of {1} from data source {2}", recordNumber, recordsFromSource.Count, dataSource.Name), progress + (recordProgress * recordNumber), recordNumber == recordsFromSource.Count);
                    IncidentData incident;
                    object value = record.GetValue(dataSource.IDColumn);
                    string recordIncidentId = null;
                    if (value != null)
                    {
                        recordIncidentId = value.ToString();
                    }

                    if (string.IsNullOrEmpty(recordIncidentId))
                    {
                        AddErrorRecord(MapLoaderErrorType.NullIncidentId, dataSource, null, record);
                        continue;
                    }

                    
                    if (!IncidentsById.TryGetValue(recordIncidentId, out incident))
                    {
                        // Creating incidents for orphaned response data breaks incremental updates.
                        if (StartDate != DateTime.MinValue || EndDate != DateTime.MaxValue) { 
                            continue;
                        }
                        
                        incident = new IncidentData();
                        incident.Id = recordIncidentId;
                        IncidentsById.Add(recordIncidentId, incident);
                        Incidents.Add(incident);
                    }                    

                    string recordResponseId = string.Empty;
                    object recordResponseIdValue = record.GetValue(dataSource.ResponseIDColumn);
                    if (recordResponseIdValue != null)
                    {
                        recordResponseId = recordResponseIdValue.ToString();
                    }
                    else
                    {
                        AddErrorRecord(MapLoaderErrorType.NullResponseId, dataSource, null, record);
                        continue;
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

                    int nmapping = 0;
                    foreach (DataMapping mapping in mappingsForSource)
                    {
                        nmapping++;
                        string scolname = mapping?.Column?.ColumnName;
                        if (string.IsNullOrEmpty(scolname)) { throw (new Exception("Unable to identify column name in response mapping #" + nmapping)); }
                        if (!record.HasColumn(scolname)) { throw (new Exception("Response record does not contain column '" + scolname + "' (#" + nmapping + ")")); }

                        string stringValue = record.GetValue(mapping.Column.ColumnName).ToString();
                        if (string.IsNullOrEmpty(stringValue))
                        {
                            AddErrorRecord(MapLoaderErrorType.NullValue, dataSource, mapping, record);
                            continue;
                        }

                        object parsedValue = getParsedValue(stringValue);

                        response.Data[mapping.Field] = parsedValue;
                    }
                }
            }
        }

        private void processBenchmarkMappings()
        {
            HashSet<IDataSource> dataSources = (from mapping in Map.BenchmarkMappings
                                                select mapping.Column.DataSource).ToHashSet();

            updateProgress(3, string.Format("Loading response timing records from {0} data sources", dataSources.Count), 0, true);

            int numSources = dataSources.Count;
            int completedSources = 0;
            foreach (IDataSource dataSource in dataSources)
            {
                List<DataMapping> mappingsForSource = (from mapping in Map.BenchmarkMappings
                                                       where mapping.Column.DataSource == dataSource
                                                       select mapping).ToList();

                if (mappingsForSource.Count == 0)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(dataSource.ResponseIDColumn))
                {
                    AddErrorRecord(MapLoaderErrorType.NoResponseIdColumn, dataSource, null, null);
                    continue;
                }


                double progressPerSource = 100 / numSources;
                double progress = completedSources * progressPerSource;
                updateProgress(3, string.Format("Getting response timing records from data source {0}", dataSource.Name), progress);
                List<Record> recordsFromSource = dataSource.GetRecords(StartDate, EndDate);
                int recordNumber = 0;
                foreach (Record record in recordsFromSource)
                {
                    if (Cancelling())
                        return;

                    recordNumber++;

                    double recordProgress = progressPerSource / recordsFromSource.Count;
                    updateProgress(3, string.Format("Processing response timing record {0} out of {1} from data source {2}", recordNumber, recordsFromSource.Count, dataSource.Name), progress + (recordProgress * recordNumber), recordNumber == recordsFromSource.Count);
                    IncidentData incident;
                    object value = record.GetValue(dataSource.IDColumn);
                    string recordIncidentId = null;
                    if (value != null)
                    {
                        recordIncidentId = value.ToString();
                    }

                    if (string.IsNullOrEmpty(recordIncidentId))
                    {
                        AddErrorRecord(MapLoaderErrorType.NullIncidentId, dataSource, null, record);
                        continue;
                    }

                    if (!IncidentsById.TryGetValue(recordIncidentId, out incident))
                    {
                        // Creating incidents for orphaned response data breaks incremental updates.
                        if (StartDate != DateTime.MinValue || EndDate != DateTime.MaxValue)
                        {
                            continue;
                        }

                        incident = new IncidentData();
                        incident.Id = recordIncidentId;
                        IncidentsById.Add(recordIncidentId, incident);
                        Incidents.Add(incident);
                    }

                    /*
                    value = record.GetValue(dataSource.ResponseIDColumn);
                    string recordResponseId = "";
                    if (value != null)
                    {
                        recordResponseId = value.ToString();
                    } else
                    {
                        AddErrorRecord(MapLoaderErrorType.NullResponseId, dataSource, null, record);
                        continue;
                    }
                    */

                    string recordResponseId = string.Empty;
                    object recordResponseIdValue = record.GetValue(dataSource.ResponseIDColumn);
                    if (recordResponseIdValue != null)
                    {
                        recordResponseId = recordResponseIdValue.ToString();
                    }
                    else
                    {
                        AddErrorRecord(MapLoaderErrorType.NullResponseId, dataSource, null, record);
                        continue;
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
                            AddErrorRecord(MapLoaderErrorType.NullValue, dataSource, mapping, record);
                            continue;
                        }

                        object parsedValue = getParsedValue(stringValue);

                        TimingData benchmark = new TimingData();
                        benchmark.Name = mapping.Field;
                        if (parsedValue is double)
                        {
                            benchmark.Value = (double)parsedValue;
                        }
                        else if (parsedValue is DateTime)
                        {
                            benchmark.Data["DateTime"] = parsedValue; // This ensures data will be written as absolute timestamps in the output CSV files
                        }
                        benchmark.Data["RawData"] = parsedValue;
                        response.TimingData.Add(benchmark);
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

        private void cleanupIncidentData()
        {
            CoordinateConverter converter = null;
            bool convertCoordinates = Map.EnableCoordinateConversion;
            try
            {
                converter = new CoordinateConverter(Map.Projection);
            }
            catch (Exception ex)
            {
                convertCoordinates = false;
            }
            updateProgress(5, string.Format("Processing incident data and nature codes for {0} incidents", Incidents.Count), 0, true);

            int incidentNum = 0;
            List<string> locationPieces = new List<string>();
            foreach (IncidentData incident in Incidents)
            {
                if (Cancelling())
                    return;

                incidentNum++;
                double progress = ((double)incidentNum / (double)Incidents.Count) * 100;
                updateProgress(5, string.Format("Cleaning incident {0} of {1}", incidentNum, Incidents.Count), progress, incidentNum == Incidents.Count);
                if (convertCoordinates)
                {
                    if (incident.Data.ContainsKey("X") && incident.Data.ContainsKey("Y"))
                    {
                        object xObj = incident.Data["X"];
                        object yObj = incident.Data["Y"];
                        double xCoord = double.NaN;
                        double yCoord = double.NaN;

                        bool convertFailed = false; 
                        try
                        {
                            if (xObj is double)
                            {
                                xCoord = (double)xObj;
                            } else if (xObj is int)
                            {
                                xCoord = Convert.ToDouble(xObj);
                            }  else if (xObj is string)
                            {
                                xCoord = double.Parse(xObj as string);
                            }

                            if (yObj is double)
                            {
                                yCoord = (double)yObj;
                            } else if (yObj is int)
                            {
                                yCoord = Convert.ToDouble(yObj);
                            } else if (yObj is string)
                            {
                                yCoord = double.Parse(xObj as string);
                            }
                        } catch (Exception ex)
                        {
                            convertFailed = true;
                        }

                        if (!convertFailed)
                        {
                            double[] xyPoint = { xCoord, yCoord };
                            LatitudeLongitude latLon = converter.ConvertXYToLatLon(xyPoint);
                            incident.Latitude = latLon.Latitude;
                            incident.Longitude = latLon.Longitude;
                        }
                    }
                }

                if (incident.Time == DateTime.MinValue)
                {
                    if (incident.Data.ContainsKey("DispatchDate") && incident.Data["DispatchDate"] is DateTime && incident.Data.ContainsKey("DispatchTime"))
                    {
                        incident.Time = (DateTime)incident.Data["DispatchDate"];
                        if (incident.Data["DispatchTime"] is DateTime)
                        {
                            DateTime dispatchTime = (DateTime)incident.Data["DispatchTime"];
                            incident.Time = new DateTime(incident.Time.Year, incident.Time.Month, incident.Time.Day, dispatchTime.Hour, dispatchTime.Minute, dispatchTime.Second, dispatchTime.Millisecond);
                        } else if (incident.Data["DispatchTime"] is int)
                        {
                            string timeStr = incident.Data["DispatchTime"].ToString();
                            int seconds = 0, minutes = 0, hours = 0;
                            if (timeStr.Length <= 2) 
                            {
                                seconds = int.Parse(timeStr);
                            } else if (timeStr.Length <= 4)
                            {
                                int minuteDigits = timeStr.Length - 2;
                                seconds = int.Parse(timeStr.Substring(minuteDigits, 2));
                                minutes = int.Parse(timeStr.Substring(0, minuteDigits));
                            } else if (timeStr.Length <= 6)
                            {
                                int hourDigits = timeStr.Length - 4;
                                seconds = int.Parse(timeStr.Substring(hourDigits + 2, 2));
                                minutes = int.Parse(timeStr.Substring(hourDigits, 2));
                                hours = int.Parse(timeStr.Substring(hourDigits));
                            }
                            incident.Time = new DateTime(incident.Time.Year, incident.Time.Month, incident.Time.Day, hours, minutes, seconds, 0);
                        }
                    }
                }
                
                if (Map.InvertLatitude)
                {
                    incident.Latitude = incident.Latitude * -1.0;
                }

                if (Map.InvertLongitude)
                {
                    incident.Longitude = incident.Longitude * -1.0;
                }

                if (incident.Location != null)
                {
                    incident.Location = incident.Location.Trim();
                } 
                else if (string.IsNullOrWhiteSpace(incident.Location))
                {
                    if (incident.Data.ContainsKey("StreetNumber") || incident.Data.ContainsKey("StreetName")) 
                    {
                        locationPieces.Clear();
                        if (incident.Data.ContainsKey("StreetNumber")) 
                        {
                            string streetNumber = incident.Data["StreetNumber"].ToString().Trim();
                            if (!string.IsNullOrWhiteSpace(streetNumber))
                            {
                                locationPieces.Add(streetNumber);
                            }
                        }

                        if (incident.Data.ContainsKey("StreetName"))
                        {
                            string streetName = incident.Data["StreetName"].ToString().Trim();
                            if (!string.IsNullOrWhiteSpace(streetName))
                            {
                                locationPieces.Add(streetName);
                            }
                        }
                        
                        if (incident.Data.ContainsKey("StreetType"))
                        {
                            string streetType = incident.Data["StreetType"].ToString().Trim();
                            if (!string.IsNullOrWhiteSpace(streetType))
                            {
                                locationPieces.Add(streetType);
                            }
                        }

                        if (incident.Data.ContainsKey("AptNumber"))
                        {
                            string apartmentNumber = incident.Data["AptNumber"].ToString().Trim();
                            if (!string.IsNullOrWhiteSpace(apartmentNumber))
                            {
                                locationPieces.Add(apartmentNumber);
                            }
                        }

                        switch (locationPieces.Count) 
                        { 
                            case 1:
                                incident.Location = locationPieces[0];
                                break;
                            case 2:
                                incident.Location = string.Format("{0} {1}", locationPieces[0], locationPieces[1]);
                                break;
                            case 3:
                                incident.Location = string.Format("{0} {1} {2}", locationPieces[0], locationPieces[1], locationPieces[2]);
                                break;
                            case 4:
                                incident.Location = string.Format("{0} {1} {2} {3}", locationPieces[0], locationPieces[1], locationPieces[2], locationPieces[3]);
                                break;
                            default:
                                incident.Location = null;
                                break;
                        }
                    }
                }

                string natureCode = "Unknown";
                if (incident.Data.ContainsKey("Code"))
                {
                    natureCode = incident.Data["Code"].ToString().Trim();
                }
                if (Map.CauseTree != null && Map.CauseTree.Count != 0)
                {
                    string[] natureCodeData = GetNatureCodeData(natureCode);
                    if (!string.IsNullOrWhiteSpace(natureCodeData[0]))
                    {
                        incident.Data["CodeDescription"] = natureCodeData[0].Trim();
                    }

                    incident.Data["Category"] = natureCodeData[1].Trim();
                    incident.Data["Type"] = natureCodeData[2].Trim();
                }
            }

            var sortedIncidents = Incidents.OrderBy(i => i.Time).ToList();
            Incidents.Clear();
            Incidents.AddRange(sortedIncidents);
        }

        private void cleanupResponseData()
        {
            updateProgress(6, string.Format("Processing response data for {0} incidents", Incidents.Count), 0, true);
        }

        string[] c_predefinedTimingDataFields = new string[] { "InQuarters", "SceneTime", "ComittedHours", "InService", "Hospital", "Transport", "ClearScene", "TravelTime", "OnScene", "TurnoutTime", "Responding", "Assigned" };

        private void cleanupBenchmarks()
        {
            updateProgress(7, string.Format("Processing response timings for {0} incidents", Incidents.Count), 0, true);
            int incidentNum = 0;
            foreach (IncidentData incident in Incidents)
            {
                if (Cancelling())
                    return;

                incidentNum++;
                double progress = ((double)incidentNum / (double)Incidents.Count) * 100;
                updateProgress(6, string.Format("Processing response timings for incident {0} of {1}", incidentNum, Incidents.Count), progress, incidentNum == Incidents.Count);
                DateTime baseTime = incident.Time;
                if (baseTime == DateTime.MinValue)
                {
                    if (incident.Data.ContainsKey("CallReceived"))
                    {
                        if (incident.Data["CallReceived"] is DateTime)
                        {
                            baseTime = (DateTime)incident.Data["CallReceived"];
                        } else {
                            DateTime.TryParse(incident.Data["CallReceived"] as string, out baseTime);
                        }
                    }

                    if (baseTime == DateTime.MinValue)
                    {
                        ErrorRecords.Add(new MapLoaderError(MapLoaderErrorType.BadValue, null, null, null, string.Format("Unable to generate benchmarks for Incident '{0}', no incident Time.", incident.Id)));
                        continue;
                    }
                }

                if (incident.Data.ContainsKey("FirstAction"))
                {
                    object firstEffAction = incident.Data["FirstAction"];
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
                    TimingData assigned = (from b in response.TimingData
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

                    TimingData responding = (from b in response.TimingData
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

                    TimingData turnout = (from b in response.TimingData
                                          where b.Name == "TurnoutTime"
                                          select b).FirstOrDefault();
                    if (turnout == null)
                    {
                        turnout = new TimingData("TurnoutTime", Math.Max(0, (respondingTime - assignedTime).TotalMinutes));
                        response.TimingData.Add(turnout);
                    }

                    TimingData onScene = (from b in response.TimingData
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

                    TimingData travel = (from b in response.TimingData
                                         where b.Name == "TravelTime"
                                         select b).FirstOrDefault();
                    if (travel == null)
                    {
                        travel = new TimingData("TravelTime", Math.Max(0, (onSceneTime - respondingTime).TotalMinutes));
                        response.TimingData.Add(travel);
                    }
                    
                    TimingData clearScene = (from b in response.TimingData
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

                    TimingData transport = (from b in response.TimingData
                                            where b.Name == "Transport"
                                            select b).FirstOrDefault();

                    DateTime transportTime = DateTime.MinValue;
                    if (transport != null)
                    {
                        incident.Data["TransportFlag"] = "Y";
                        if (!double.IsNaN(transport.Value))
                        {
                            transportTime = baseTime.AddMinutes(transport.Value);
                        }
                        else if (transport.Data.ContainsKey("RawData"))
                        {
                            if (transport.Data["RawData"] is DateTime)
                            {
                                transportTime = (DateTime)transport.Data["RawData"];
                                transport.Value = (transportTime - baseTime).TotalMinutes;
                            }
                            else if (DateTime.TryParse(transport.Data["RawData"] as string, out transportTime))
                            {
                                transport.Value = (transportTime - baseTime).TotalMinutes;
                            }
                        }
                        if (transportTime != DateTime.MinValue)
                        {
                            transport.Data["DateTime"] = transportTime;
                            if (Map.TransportAsClearScene == true)
                            {
                                if (clearScene != null)
                                {
                                    response.TimingData.Remove(clearScene);
                                }
                                clearScene = new TimingData("ClearScene", transport.Value);
                                clearScene.Data["DateTime"] = transportTime;
                                clearScene.Data["RawData"] = transport.Data["RawData"];
                                response.TimingData.Add(clearScene);
                            }
                        }
                    }

                    TimingData hospital = (from b in response.TimingData
                                           where b.Name == "Hospital"
                                           select b).FirstOrDefault();

                    DateTime hospitalTime = DateTime.MinValue;
                    if (hospital != null)
                    {
                        incident.Data["TransportFlag"] = "Y";
                        if (!double.IsNaN(hospital.Value)) {
                            hospitalTime = baseTime.AddMinutes(hospital.Value);
                        } else if (hospital.Data.ContainsKey("RawData"))
                        {
                            if (hospital.Data["RawData"] is DateTime)
                            {
                                hospitalTime = (DateTime)hospital.Data["RawData"];
                                hospital.Value = (hospitalTime - baseTime).TotalMinutes;
                            } else if (DateTime.TryParse(hospital.Data["RawData"] as string, out hospitalTime))
                            {
                                hospital.Value = (hospitalTime - baseTime).TotalMinutes;
                            }
                        }
                        if (hospitalTime != DateTime.MinValue)
                        {
                            hospital.Data["DateTime"] = hospitalTime;
                        }
                    }

                    TimingData inService = (from b in response.TimingData
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

                    TimingData committed = (from b in response.TimingData
                                            where b.Name == "CommittedHours"
                                            select b).FirstOrDefault();

                    if (committed == null)
                    {
                        committed = new TimingData("CommittedHours", Math.Max(0, (clearSceneTime - assignedTime).TotalMinutes) / 60);
                        response.TimingData.Add(committed);
                    }

                    TimingData sceneTime = (from b in response.TimingData
                                            where b.Name == "SceneTime"
                                            select b).FirstOrDefault();

                    if (sceneTime == null && onSceneTime != DateTime.MinValue)
                    {
                        sceneTime = new TimingData("SceneTime", (clearSceneTime - onSceneTime).TotalMinutes);
                        response.TimingData.Add(sceneTime);
                    }

                    TimingData inQuarters = (from b in response.TimingData
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

                    foreach (TimingData timingData in response.TimingData)
                    {
                        if (c_predefinedTimingDataFields.Contains(timingData.Name))
                        {
                            continue;
                        }

                        if(double.IsNaN(timingData.Value))
                        {
                            if (timingData.Data.ContainsKey("DateTime")) {
                                DateTime date = timingData.Data["DateTime"] is DateTime ? (DateTime)timingData.Data["DateTime"] : DateTime.MinValue;
                                if (date == DateTime.MinValue && DateTime.TryParse(timingData.Data["DateTime"].ToString(), out date)) {
                                    timingData.Value = (date - baseTime).TotalMinutes;
                                }
                            }  else if (timingData.Data.ContainsKey("RawData"))
                            {
                                DateTime date = timingData.Data["RawData"] is DateTime ? (DateTime)timingData.Data["RawData"] : DateTime.MinValue;
                                if (date == DateTime.MinValue && DateTime.TryParse(timingData.Data["RawData"].ToString(), out date))
                                {
                                    timingData.Value = (date - baseTime).TotalMinutes;
                                }
                                else
                                {
                                    double minutes;
                                    if (double.TryParse(timingData.Data["RawData"].ToString(), out minutes))
                                    {
                                        timingData.Value = minutes;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private HashSet<IDataSource> m_geoSources = new HashSet<IDataSource>();

        private void geoSourceThread()
        {
            IncidentData incident;
            while (IncidentQueue.TryTake(out incident))
            {
                try
                {
                    if (Cancelling())
                        return;

                    double incidentCount = Incidents.Count;
                    double incidentNum = incidentCount - (double)IncidentQueue.Count;
                    double progress = (incidentNum / incidentCount) * 100;
                    updateProgress(8, string.Format("Processing geographic data for incident {0} of {1}", incidentNum, incidentCount), progress, incidentNum == Incidents.Count);
                    foreach (IDataSource dataSource in m_geoSources)
                    {
                        GeoSource geoSource = (GeoSource)dataSource;
                        Dictionary<string, object> attributes = geoSource.GetPropertiesForLatLon(incident.Latitude, incident.Longitude);
                        if (attributes.Count == 0)
                        {
                            continue;
                        }

                        List<DataMapping> incidentMappings = (from mapping in Map.IncidentDataMappings
                                                              where mapping.Column?.DataSource == geoSource
                                                              select mapping).ToList();

                        List<DataMapping> responseMappings = (from mapping in Map.ResponseDataMappings
                                                              where mapping.Column?.DataSource == geoSource
                                                              select mapping).ToList();

                        List<DataMapping> benchmarkMappings = (from mapping in Map.BenchmarkMappings
                                                               where mapping.Column?.DataSource == geoSource
                                                               select mapping).ToList();

                        foreach (DataMapping mapping in incidentMappings)
                        {
                            object value;
                            if (attributes.TryGetValue(mapping.Column.ColumnName, out value))
                            {
                                incident.Data[mapping.Field] = value;
                            }
                        }

                        List<KeyValuePair<string, object>> responseAttributes = new List<KeyValuePair<string, object>>();
                        foreach (DataMapping mapping in responseMappings)
                        {
                            object value;
                            if (attributes.TryGetValue(mapping.Column.ColumnName, out value))
                            {
                                responseAttributes.Add(new KeyValuePair<string, object>(mapping.Field, value));
                            }
                        }

                        List<KeyValuePair<string, object>> bmkAttributes = new List<KeyValuePair<string, object>>();
                        foreach (DataMapping mapping in benchmarkMappings)
                        {
                            object value;
                            if (attributes.TryGetValue(mapping.Column.ColumnName, out value))
                            {
                                bmkAttributes.Add(new KeyValuePair<string, object>(mapping.Field, value));
                            }
                        }

                        foreach (ResponseData response in incident.Responses)
                        {
                            foreach (var attr in responseAttributes)
                            {
                                response.Data[attr.Key] = attr.Value;
                            }

                            foreach (TimingData bmk in response.TimingData)
                            {
                                foreach (var attr in bmkAttributes)
                                {
                                    bmk.Data[attr.Key] = attr.Value;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogMessage(Utils.LogLevel.Warn, string.Format("Exception processing geographic data for incident {0}", incident.Id), ex);
                }
            }
        }

        private void processGeoSources()
        {
            try
            {
                HashSet<IDataSource> incidentGeoSources = (from mapping in Map.IncidentDataMappings
                                                           where mapping.Column?.DataSource is GeoSource
                                                           select mapping.Column.DataSource).ToHashSet();

                HashSet<IDataSource> responseGeoSources = (from mapping in Map.ResponseDataMappings
                                                           where mapping.Column?.DataSource is GeoSource
                                                           select mapping.Column.DataSource).ToHashSet();

                HashSet<IDataSource> benchmarkGeoSources = (from mapping in Map.IncidentDataMappings
                                                            where mapping.Column?.DataSource is GeoSource
                                                            select mapping.Column.DataSource).ToHashSet();

                m_geoSources = new HashSet<IDataSource>(incidentGeoSources);
                foreach (IDataSource dataSource in responseGeoSources)
                {
                    m_geoSources.Add(dataSource);
                }

                foreach (IDataSource dataSource in benchmarkGeoSources)
                {
                    m_geoSources.Add(dataSource);
                }

                if (m_geoSources.Count == 0)
                {
                    return;
                }

                foreach (IDataSource dataSource in m_geoSources)
                {
                    GeoSource geoSource = dataSource as GeoSource;
                    if (geoSource != null)
                    {
                        geoSource.Load();
                    }
                }

                updateProgress(8, string.Format("Processing geographic data from {0} sources for {1} incidents", m_geoSources.Count, Incidents.Count), 0, true);

                foreach (IncidentData incident in Incidents)
                {
                    IncidentQueue.Add(incident);
                }

                int threadCount = Environment.ProcessorCount - 1;
                Thread[] threads = new Thread[threadCount];

                for (int i = 0; i < threadCount; i++)
                {
                    Thread thread = new Thread(geoSourceThread);
                    threads[i] = thread;
                    thread.Start();
                }

                bool running = true;
                while (running)
                {
                    for (int i = 0; i < threadCount; i++)
                    {
                        if (threads[i].IsAlive)
                        {
                            break;
                        }
                        running = false;
                    }
                    Thread.Sleep(1000);
                }

                /*
                foreach (IncidentData incident in Incidents) { 
                    if (Cancelling())
                        return;

                    incidentNum++;
                    double progress = ((double)incidentNum / (double)Incidents.Count) * 100;
                    updateProgress(8, string.Format("Processing geographic data for incident {0} of {1}", incidentNum, Incidents.Count), progress, incidentNum == Incidents.Count);
                    foreach (IDataSource dataSource in allGeoSources)
                    {
                        GeoSource geoSource = (GeoSource)dataSource;
                        Dictionary<string, object> attributes = geoSource.GetPropertiesForLatLon(incident.Latitude, incident.Longitude);
                        if (attributes.Count == 0)
                        {
                            continue;
                        }

                        List<DataMapping> incidentMappings = (from mapping in map.IncidentDataMappings
                                                              where mapping.Column?.DataSource == geoSource
                                                              select mapping).ToList();

                        List<DataMapping> responseMappings = (from mapping in map.ResponseDataMappings
                                                              where mapping.Column?.DataSource == geoSource
                                                              select mapping).ToList();

                        List<DataMapping> benchmarkMappings = (from mapping in map.BenchmarkMappings
                                                               where mapping.Column?.DataSource == geoSource
                                                               select mapping).ToList();

                        foreach (DataMapping mapping in incidentMappings)
                        {
                            object value;
                            if (attributes.TryGetValue(mapping.Column.ColumnName, out value))
                            {
                                incident.Data[mapping.Field] = value;
                            }
                        }

                        List<KeyValuePair<string, object>> responseAttributes = new List<KeyValuePair<string, object>>();
                        foreach (DataMapping mapping in responseMappings)
                        {
                            object value;
                            if (attributes.TryGetValue(mapping.Column.ColumnName, out value))
                            {
                                responseAttributes.Add(new KeyValuePair<string, object>(mapping.Field, value));
                            }
                        }

                        List<KeyValuePair<string, object>> bmkAttributes = new List<KeyValuePair<string, object>>();
                        foreach (DataMapping mapping in benchmarkMappings)
                        {
                            object value;
                            if (attributes.TryGetValue(mapping.Column.ColumnName, out value))
                            {
                                bmkAttributes.Add(new KeyValuePair<string, object>(mapping.Field, value));
                            }
                        }

                        foreach (ResponseData response in incident.Responses)
                        {
                            foreach (var attr in responseAttributes)
                            {
                                response.Data[attr.Key] = attr.Value;
                            }

                            foreach (BenchmarkData bmk in response.Benchmarks)
                            {
                                foreach (var attr in bmkAttributes)
                                {
                                    bmk.Data[attr.Key] = attr.Value;
                                }
                            }
                        }
                    }
                }
                */
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Exception processing geographic data", true);
            }
        }

        private void calculateDerivedBenchmarks()
        {
            updateProgress(9, string.Format("Calculating derived response timings for {0} incidents", Incidents.Count), 0);
            int incidentNum = 0;
            foreach (IncidentData incident in Incidents)
            {
                if (Cancelling())
                    return;

                incidentNum++;
                double progress = ((double)incidentNum / (double)Incidents.Count) * 100;
                updateProgress(9, string.Format("Calculating derived response timings for incident {0} of {1}", incidentNum, Incidents.Count), progress, incidentNum == Incidents.Count);
                ResponseData firstArrival = null;
                ResponseData lastArrival = null;
                ResponseData firstResponse = null;
                double firstOnScene = double.MaxValue;
                double lastOnScene = 0.0;
                double firstResponding = double.MaxValue;
                foreach (ResponseData response in incident.Responses)
                {
                    double onScene = (from TimingData bmk in response.TimingData
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

                    double responding = (from TimingData bmk in response.TimingData
                                         where bmk.Name == "Responding"
                                         select bmk.Value).FirstOrDefault();

                    if (responding < firstResponding || firstResponse == null)
                    {
                        firstResponding = responding;
                        firstResponse = response;
                    }
                }

                if (firstArrival != null)
                    firstArrival.TimingData.Add(new TimingData("FirstArrival", firstOnScene));
                if (lastArrival != null)
                    lastArrival.TimingData.Add(new TimingData("FullComplement", lastOnScene));
                if (firstResponse != null)
                    firstResponse.TimingData.Add(new TimingData("FirstResponding", firstResponding));
            }
        }

        public void UpdateJSProgress(string message, string percentage)
        {
            double value = 0;
            double.TryParse(percentage, out value);
            UpdateJSProgress(message, value);
        }

        public void UpdateJSProgress(string message, double percentage)
        {
            updateProgress(JSProgressStep, message, percentage);
        }

        public void CollectNativeGarbage()
        {
            GC.Collect();
        }

        private class ProgressInfo
        {
            public int Number { get; set; } = 0;
            public int Count { get; set; } = 0;
            public double Progress
            {
                get
                {
                    if (Count == 0)
                        return 100.0;

                    return ((double)Number / (double)Count) * 100.0;
                }
            }
        }

        private void setupScriptEngine(V8ScriptEngine engine)
        {
            engine.AddHostObject("Engine", engine);
            engine.AddHostObject("XHost", new ExtendedHostFunctions());
            engine.AddHostObject("Tools", new AnnotatedDataTools());
            engine.AddHostObject("Incidents", Incidents);
            engine.AddHostObject("Debug", DebugHost);
            engine.AddHostObject("Logger", Logger);
            engine.AddHostObject("MapLoader", this);
            engine.AddHostType("object", typeof(object));
            engine.AddHostType("bool", typeof(bool));
            engine.AddHostType("double", typeof(double));
            engine.AddHostType("int", typeof(int));
            engine.AddHostType("string", typeof(string));
            engine.AddHostType("DateTime", typeof(DateTime));
            engine.AddHostType("TimeSpan", typeof(TimeSpan));
            engine.AddHostType("LogLevel", typeof(NLogLevel));
            engine.AddHostType("IncidentData", typeof(IncidentData));
            engine.AddHostType("IncidentDataSet", typeof(DataSet<IncidentData>));
            engine.AddHostType("ResponseData", typeof(ResponseData));
            engine.AddHostType("ResponseDataSet", typeof(DataSet<ResponseData>));
            engine.AddHostType("TimingData", typeof(TimingData));
            engine.AddHostType("TimingDataSet", typeof(DataSet<TimingData>));
            engine.AddHostType("MapLoaderErrorType", typeof(MapLoaderErrorType));
            engine.AddHostType("MapLoaderError", typeof(MapLoaderError));
        }

        private void executePostLoading()
        {
            if (Cancelling())
                return;

            if (!string.IsNullOrWhiteSpace(Map.PostProcessingScript))
            {
                JSProgressStep = 4;
                updateProgress(JSProgressStep, string.Format("Executing post-loading script"), 0, true);
                try
                {
                    using (V8ScriptEngine v8 = new V8ScriptEngine())
                    {
                        ProgressInfo pInfo = new ProgressInfo();
                        pInfo.Count = Incidents.Count;
                        pInfo.Number = 0;

                        setupScriptEngine(v8);
                        v8.AddHostObject("ProgressInfo", pInfo);
                        v8.Execute(Map.PostProcessingScript);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogMessage(Utils.LogLevel.Warn, string.Format("Exception running post-loading script"), ex);
                    DebugHost.WriteLine(ex.Message);
                    DebugHost.WriteLine(ex.StackTrace);
                }
            }
        }

        private void executePostProcessing()
        {
            if (Cancelling())
                return;

            JSProgressStep = 10;
            if (!string.IsNullOrWhiteSpace(Map.PerIncidentScript))
            {
                updateProgress(JSProgressStep, string.Format("Executing per incident script"), 0, true);
                List<List<IncidentData>> chunks = new List<List<IncidentData>>();
                List<IncidentData> newChunk = new List<IncidentData>();
                chunks.Add(newChunk);
                foreach (IncidentData incident in Incidents)
                {
                    if (newChunk.Count >= 1000)
                    {
                        newChunk = new List<IncidentData>();
                        chunks.Add(newChunk);
                    }
                    newChunk.Add(incident);
                }
                ProgressInfo pInfo = new ProgressInfo();
                pInfo.Count = Incidents.Count;
                pInfo.Number = 0;
                int n_pperrs = 0;
                LogHelper.LogMessage(Utils.LogLevel.Trace, "Starting Per Incident script execution");
                int numChunk = 0;
                foreach (List<IncidentData> chunk in chunks)
                {
                    DateTime chunkStart = DateTime.Now;
                    using (V8ScriptEngine v8 = new V8ScriptEngine())
                    {
                        setupScriptEngine(v8);
                        v8.AddHostObject("ProgressInfo", pInfo);
                        V8Script script = v8.Compile(Map.PerIncidentScript);
                        foreach (IncidentData incident in chunk)
                        {
                            if (Cancelling())
                            {
                                return;
                            }
                            pInfo.Number++;

                            updateProgress(JSProgressStep, string.Format("Executing per incident script for incident {0} of {1}", pInfo.Number, pInfo.Count), pInfo.Progress, pInfo.Number >= pInfo.Count - 20);
                            v8.AddHostObject("Incident", incident);
                            try
                            {
                                v8.Execute(script);
                                // v8.CollectGarbage(true);
                            }
                            catch (Exception ex)
                            {
                                LogHelper.LogMessage(Utils.LogLevel.Warn, string.Format("Exception running per incident script for incident {0}", incident.Id), ex);
                                n_pperrs++;
                            }
                        }
                    }
                    TimeSpan chunkTime = DateTime.Now - chunkStart;
                    LogHelper.LogMessage(Utils.LogLevel.Trace, string.Format("Chunk {0} completed in {1} seconds", numChunk, chunkTime.TotalSeconds));
                    numChunk++;
                    GC.Collect();
                }

                LogHelper.LogMessage(Utils.LogLevel.Trace, "Finished Per Incident script execution");

                if (n_pperrs > 0)
                {
                    //LogHelper.LogMessage(Utils.LogLevel.Error, "The post-processing script generated " + n_pperrs + " errors.  Please see the event log, which is in some cryptic place that will be difficult to find.");
                    LogHelper.DisplayMessageIfPossible("The post-processing script generated " + n_pperrs + " errors.  Please see the event log.");
                }
            }

            if (Cancelling())
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(Map.FinalProcessingScript))
            {
                updateProgress(JSProgressStep, string.Format("Executing final processing script"), 0, true);

                try
                {
                    using (V8ScriptEngine v8 = new V8ScriptEngine())
                    {
                        ProgressInfo pInfo = new ProgressInfo();
                        pInfo.Count = Incidents.Count;
                        pInfo.Number = 0;

                        setupScriptEngine(v8);
                        v8.AddHostObject("ProgressInfo", pInfo);
                        v8.Execute(Map.FinalProcessingScript);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogMessage(Utils.LogLevel.Warn, string.Format("Exception running final processing script"), ex);
                    DebugHost.WriteLine(ex.Message);
                    DebugHost.WriteLine(ex.StackTrace);
                }
            }
        }
    }

    public delegate void MapLoaderProgressListener(MapLoader sender, string message, double completionPercentage);

    public class MapLoaderError
    {
        public MapLoaderErrorType ErrorType { get; set; }
        public IDataSource DataSource { get; set; }
        public DataMapping Mapping { get; set; }
        public Record Record { get; set; }
        public string Details { get; set; }

        public MapLoaderError(MapLoaderErrorType type, IDataSource dataSource, DataMapping mapping, Record record, string details = "")
        {
            ErrorType = type;
            DataSource = dataSource;
            Mapping = mapping;
            Record = record;
            if (!string.IsNullOrEmpty(details))
            {
                Details = details;
            }
        }

        public MapLoaderError(MapLoaderErrorType type, string details = "")
        {
            ErrorType = type;
            if (!string.IsNullOrEmpty(details))
            {
                Details = details;
            }
        }
    }

    public enum MapLoaderErrorType { NullIncidentId, NoResponseIdColumn, NullResponseId, NullValue, BadValue, MergeConflict, LoaderException };
}
