using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Newtonsoft.Json;

namespace Levrum.Data.Classes
{
    public class BenchmarkData : TimingData
    {
        BenchmarkData()
        {
            Console.WriteLine("BenchmarkData is deprecated and will be removed in a future version. Please switch to TimingData.");
            Debug.WriteLine("BenchmarkData is deprecated and will be removed in a future version. Please switch to TimingData.");
#if DEBUG
            throw new Exception("BenchmarkData is deprecated and will be removed in a future version. Please switch to TimingData.");
#endif
        }
    }

    public class TimingData : AnnotatedData
    {
        public string Name
        {
            get
            {
                if (Data.ContainsKey("Name"))
                {
                    return Data["Name"] as string;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                Data["Name"] = value;
            }
        }

        public double Value
        {
            get
            {
                if (Data.ContainsKey("Value"))
                {
                    
                    object value = Data["Value"];
                    if (value is double || value is int)
                    {
                        return (double)value;
                    } else if (value is string)
                    {
                        double output;
                        if (double.TryParse(Data["Value"] as string, out output))
                        {
                            return output;
                        } else
                        {
                            return double.NaN;
                        }
                    } else
                    {
                        return double.NaN;
                    }
                }
                else
                {
                    return double.NaN;
                }
            }
            set
            {
                Data["Value"] = value;
            }
        }

        public string Details
        {
            get
            {
                if (Data.ContainsKey("Details"))
                {
                    return Data["Details"] as string;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                Data["Details"] = value;
            }
        }

        [JsonIgnore]
        public DateTime DateTime
        {
            get
            {
                if (Data.ContainsKey("DateTime"))
                {
                    var obj = Data["DateTime"];
                    if (obj is DateTime)
                    {
                        return (DateTime)Data["DateTime"];
                    } else if (obj is string)
                    {
                        DateTime output;
                        if (DateTime.TryParse(obj as string, out output))
                        {
                            return output;
                        }
                    }
                }
                return DateTime.MinValue;
            }
            set
            {
                Data["DateTime"] = value;
            }
        }

        [JsonIgnore]
        public object RawData
        {
            get
            {
                object output = null;
                Data.TryGetValue("RawData", out output);
                return output;
            }
            set
            {
                Data["RawData"] = value;
            }
        }

        public TimingData()
        {

        }

        public TimingData(string name = "", double value = Double.NaN, string details = "")
        {
            Name = name;
            Value = value;
            Details = details;
        }

        public bool HasAbsoluteTimestamp()
        {
            if (null==this.Data) { return (false); }
            if (!Data.ContainsKey("RawData")) { return (false); }
            object oval = Data["RawData"];
            return (oval is DateTime);
        }

        public DateTime GetAbsoluteTimestamp()
        {
            DateTime errval = DateTime.MinValue;
            if (!HasAbsoluteTimestamp()) { return (errval); }
            DateTime retval = (DateTime)Data["RawData"];
            return (retval);
        }
    }
}
