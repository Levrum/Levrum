﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using Newtonsoft.Json;

namespace Levrum.Data.Classes
{
    public class IncidentData : AnnotatedData
    {
        [JsonIgnore]
        public string Id
        {
            get
            {
                if (Data.ContainsKey("Id"))
                {
                    object id = Data["Id"];
                    if (id is char[])
                    {
                        return new string(Data["Id"] as char[]);
                    }
                    else
                    {
                        return Data["Id"] as string;
                    }
                } else
                {
                    return string.Empty;
                }
            }
            set
            {
                Data["Id"] = value.ToCharArray();
            }
        }

        [JsonIgnore]
        public DateTime Time
        {
            get
            {
                DateTime output  = DateTime.MinValue;
                if (Data.ContainsKey("Time"))
                {
                    if (Data["Time"] is long)
                    {
                        output = new DateTime((long)Data["Time"]);
                    } else if (Data["Time"] is string)
                    {
                        DateTime.TryParse((string)Data["Time"], out output);
                    }
                    else if (Data["Time"] is DateTime)
                    {
                        return (DateTime)Data["Time"];
                    }
                }
                return output;
            }
            set
            {
                Data["Time"] = value.Ticks;
            }
        }

        [JsonIgnore]
        public string Location
        {
            get
            {
                if (Data.ContainsKey("Location"))
                {
                    object loc = Data["Location"];
                    if (loc is char[])
                    {
                        return new string(loc as char[]);
                    }
                    else
                    {
                        return Data["Location"] as string;
                    }
                }
                return string.Empty;
            }
            set
            {
                Data["Location"] = value.ToCharArray();
            }
        }

        [JsonIgnore]
        public double Longitude
        {
            get
            {
                double output = double.NaN;
                if (Data.ContainsKey("Longitude"))
                {
                    if (Data["Longitude"] is string)
                    {
                        double.TryParse(Data["Longitude"] as string, out output);
                    } else if (Data["Longitude"] is double)
                    {
                        return (double)Data["Longitude"];
                    }
                }

                return output;
            }
            set
            {
                Data["Longitude"] = value;
            }
        }

        [JsonIgnore]
        public double Latitude
        {
            get
            {
                double output = double.NaN;
                if (Data.ContainsKey("Latitude"))
                {
                    if (Data["Latitude"] is string)
                    {
                        double.TryParse(Data["Latitude"] as string, out output);
                    }
                    else if (Data["Latitude"] is double)
                    {
                        return (double)Data["Latitude"];
                    }
                }

                return output;
            }
            set
            {
                Data["Latitude"] = value;
            }
        }

        public DataSet<ResponseData> Responses { get; set; }

        public IncidentData()
        {
            Responses = new DataSet<ResponseData>(this);
        }

        // 20190629 CDN - modified parameter defaults to compile in VS2017.   DateTime
        // default is a hack suggested by https://stackoverflow.com/questions/3031110/set-default-value-for-datetime-in-optional-parameter
        public IncidentData(string id = "", DateTime? time = null, string location = "", double longitude = 0.0, double latitude = 0.0, 
            IDictionary<string, object> data = null, IEnumerable<ResponseData> responses = null)
        {
            Id = id;
            Location = location;
            Longitude = longitude;
            Latitude = latitude;

            if (time > DateTime.MinValue)
                Time = (DateTime)time;

            if (data != null)
            {
                foreach (KeyValuePair<string, object> kvp in data)
                {
                    Data.Add(kvp.Key, kvp.Value);
                }
            }

            Responses = new DataSet<ResponseData>(this);

            if (responses != null)
                Responses.AddRange(responses);

            Intern();
        }

        public IncidentData(IncidentData source)
        {
            Id = source.Id;
            Time = source.Time;
            Location = source.Location;
            Longitude = source.Longitude;
            Latitude = source.Latitude;

            foreach (KeyValuePair<string, object> kvp in source.Data)
            {
                Data.Add(kvp.Key, kvp.Value);
            }

            Responses = new DataSet<ResponseData>(this);

            Responses.AddRange(source.Responses);

            Intern();
        }

        public bool AddDataElements(object oSource, params string[] oFieldNames)
        {
            Type srctype = oSource.GetType();
            foreach (string sfield in oFieldNames)
            {
                FieldInfo fi =  srctype.GetField(sfield);
                if (null == fi) { continue; }
                object oval = fi.GetValue(oSource);
                if (null == oval) { continue; }
                Data.Add(sfield,oval);   
            }
            return(true);
        }

    }
}
