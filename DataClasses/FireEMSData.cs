using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

namespace Levrum.DataClasses
{
    public class FireEMSIncident : IncidentData
    {
        [JsonIgnore]
        public string Category 
        {
            get
            {
                if (!Data.ContainsKey("Category"))
                    return string.Empty;

                return Data["Category"] as string;
            } 
            set
            {
                Data["Category"] = value;
            }
        }

        [JsonIgnore]
        public string Type
        {
            get
            {
                if (!Data.ContainsKey("Type"))
                    return string.Empty;

                return Data["Type"] as string;
            } 
            set
            {
                Data["Type"] = value;
            }
        }

        [JsonIgnore]
        public string Code
        {
            get
            {
                if (!Data.ContainsKey("Code"))
                    return string.Empty;

                return Data["Code"] as string;
            }
            set
            {
                Data["Code"] = value;
            }
        }

        [JsonIgnore]
        public DateTime CallReceivedTime
        {
            get
            {
                return Time;
            }
            set
            {
                Time = value;
            }
        }

        [JsonIgnore]
        public DateTime DispatchTime
        {
            get
            {
                if (!Data.ContainsKey("DispatchTime"))
                    return DateTime.MinValue;

                if (Data["DispatchTime"] is string)
                {
                    DateTime result;
                    if (DateTime.TryParse(Data["DispatchTime"] as string, out result))
                        return result;
                }
                else if (Data["DispatchTime"] is long)
                {
                    return new DateTime((long)Data["DispatchTime"]);
                }
                else if (Data["DispatchTime"] is DateTime)
                {
                    return (DateTime)Data["DispatchTime"];
                }
                
                return DateTime.MinValue;
            }
            set
            {
                Data["DispatchTime"] = value;
            }
        }
    }

    public class FireEMSResponse : ResponseData
    {
        [JsonIgnore]
        public string Unit
        {
            get
            {
                if (!Data.ContainsKey("Unit"))
                    return string.Empty;

                return Data["Unit"] as string;
            }
            set
            {
                Data["Unit"] = value;
            }
        }

        [JsonIgnore]
        public string UnitType
        {
            get
            {
                if (!Data.ContainsKey("UnitType"))
                    return string.Empty;

                return Data["UnitType"] as string;
            }
            set
            {
                Data["UnitType"] = value;
            }
        }

        [JsonIgnore]
        public string Priority
        {
            get
            {
                if (!Data.ContainsKey("Priority"))
                    return string.Empty;

                return Data["Priority"] as string;
            }
            set
            {
                Data["Priority"] = value;
            }
        }

        [JsonIgnore]
        public DateTime Assigned 
        {
            get
            {
                return getBenchmarkDateTime("Assigned");
            }
            set
            {
                setBenchmarkDateTime("Assigned", value);
            }
        }

        [JsonIgnore]
        public DateTime Enroute
        {
            get
            {
                return getBenchmarkDateTime("Enroute");
            }
            set
            {
                setBenchmarkDateTime("Enroute", value);
            }
        }

        [JsonIgnore]
        public TimeSpan TurnoutTime
        {
            get 
            { 
                return getBenchmarkTimeSpan("TurnoutTime"); 
            }
            set
            {
                setBenchmarkTimeSpan("TurnoutTime", value);
            }
        }

        [JsonIgnore]
        public DateTime OnScene
        {
            get
            {
                return getBenchmarkDateTime("OnScene");
            }
            set
            {
                setBenchmarkDateTime("OnScene", value);
            }
        }

        [JsonIgnore]
        public DateTime ClearScene
        {
            get
            {
                return getBenchmarkDateTime("ClearScene");
            }
            set
            {
                setBenchmarkDateTime("ClearScene", value);
            }
        }

        [JsonIgnore]
        public TimeSpan CommittedHours
        {
            get
            {
                return getBenchmarkTimeSpan("CommittedHours");
            }
            set
            {
                setBenchmarkTimeSpan("CommittedHours", value);
            }
        }

        [JsonIgnore]
        public TimeSpan SceneTime
        {
            get
            {
                return getBenchmarkTimeSpan("SceneTime");
            } set
            {
                setBenchmarkTimeSpan("SceneTime", value);
            }
        }

        private DateTime getBenchmarkDateTime(string benchmarkName)
        {
            BenchmarkData benchmark = (from BenchmarkData b in Benchmarks
                                       where b.Name == benchmarkName
                                       select b).FirstOrDefault();

            
            if (benchmark == default(BenchmarkData)) {
                return DateTime.MinValue;
            }

            if (!benchmark.Data.ContainsKey("DateTime"))
            {
                if(Parent != null && Parent.Time != DateTime.MinValue && benchmark.Value != Double.NaN) 
                {
                    return Parent.Time.AddMinutes(benchmark.Value);
                }
            }

            if (benchmark.Data["DateTime"] is DateTime)
            {
                return (DateTime)benchmark.Data["DateTime"];
            } else
            {
                DateTime output;
                if (DateTime.TryParse(benchmark.Data["DateTime"] as string, out output))
                {
                    return output;
                }
            }

            return DateTime.MinValue;
        }

        private void setBenchmarkDateTime(string benchmarkName, DateTime time)
        {
            BenchmarkData benchmark = (from BenchmarkData b in Benchmarks
                                       where b.Name == benchmarkName
                                       select b).FirstOrDefault();

            if (benchmark == default(BenchmarkData))
            {
                benchmark = new BenchmarkData(benchmarkName);
            }

            if (Parent != null)
            {
                IncidentData incident = Parent;
                if (incident.Time != DateTime.MinValue)
                {
                    TimeSpan timeSpan = time - incident.Time;
                    benchmark.Value = timeSpan.TotalMinutes;
                }
            }

            benchmark.Data["DateTime"] = time;
        }

        private TimeSpan getBenchmarkTimeSpan(string benchmarkName)
        {
            BenchmarkData benchmark = (from BenchmarkData b in Benchmarks
                                       where b.Name == benchmarkName
                                       select b).FirstOrDefault();

            if (benchmark == default(BenchmarkData))
            {
                return TimeSpan.Zero;
            }

            TimeSpan output;
            if (!benchmark.Data.ContainsKey("TimeSpan") || !(benchmark.Data["TimeSpan"] is TimeSpan))
            {
                if (TimeSpan.TryParse(benchmark.Data["TimeSpan"] as string, out output)) {
                    return output;
                }

                if (benchmark.Value == 0.0)
                {
                    return TimeSpan.Zero;
                } else
                {
                    int valueAsMilliseconds = (int)benchmark.Value * 60 * 1000;
                    output = new TimeSpan(0, 0, 0, 0, valueAsMilliseconds);
                    return output;
                }
            }

            return (TimeSpan)benchmark.Data["TimeSpan"];
        }

        private void setBenchmarkTimeSpan(string benchmarkName, TimeSpan timeSpan)
        {
            BenchmarkData benchmark = (from BenchmarkData b in Benchmarks
                                       where b.Name == benchmarkName
                                       select b).FirstOrDefault();

            if (benchmark == default(BenchmarkData))
            {
                benchmark = new BenchmarkData(benchmarkName);
            }

            benchmark.Value = timeSpan.TotalMinutes;
            benchmark.Data["TimeSpan"] = timeSpan;
        }
    }
}
