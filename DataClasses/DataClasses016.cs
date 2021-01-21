using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Newtonsoft.Json;

namespace Levrum.Data.Classes
{
    public class DataSet016<T> : List<T>
    {
        public object Parent { get; set; }

        [JsonIgnore]
        public string Id
        {
            get
            {
                if (Data.ContainsKey("Id"))
                {
                    return Data["Id"] as string;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                Data["Id"] = value;
            }
        }

        private InternedDictionary<string, object> m_data = null;

        public InternedDictionary<string, object> Data
        {
            get
            {
                if (m_data == null)
                    m_data = new InternedDictionary<string, object>();

                return m_data;
            }

            protected set
            {
                m_data = value;
            }
        }

        public DataSet016()
        {

        }

        public DataSet016(object _parent = null)
        {
            Parent = _parent;
        }

        public DataSet016(List<T> _list = null, object _parent = null)
        {
            if (_list != null)
            {
                AddRange(_list);
            }
            Parent = _parent;
        }

        public DataSet016(string id = "", List<T> items = null, Dictionary<string, object> data = null, object _parent = null)
        {
            Id = id;
            if (items != null)
            {
                AddRange(items);
            }

            if (data != null)
            {
                foreach (KeyValuePair<string, object> kvp in data)
                {
                    Data.Add(kvp.Key, kvp.Value);
                }
            }

            Parent = _parent;
        }

        public new void Add(T item)
        {
            if (Parent == null)
            {
                base.Add(item);
                return;
            }
            PropertyInfo[] properties = item.GetType().GetProperties();
            PropertyInfo info = (from PropertyInfo p in properties
                                 where p.Name is "Parent"
                                 select p).FirstOrDefault();

            if (info != default(PropertyInfo))
            {
                Type parentType = Parent.GetType();
                if (info.PropertyType == parentType)
                {
                    info.SetValue(item, Parent);
                }
            }

            base.Add(item);
        }

        public new void AddRange(IEnumerable<T> range)
        {
            if (Parent == null)
            {
                base.AddRange(range);
                return;
            }

            Type type = typeof(T);

            PropertyInfo[] properties = type.GetProperties();
            PropertyInfo info = (from PropertyInfo p in properties
                                 where p.Name is "Parent"
                                 select p).FirstOrDefault();

            T[] items = range.ToArray();
            if (info != default(PropertyInfo))
            {
                Type parentType = Parent.GetType();
                if (info.PropertyType == parentType)
                {
                    foreach (T item in items)
                    {
                        info.SetValue(item, Parent);
                    }
                }
            }

            base.AddRange(items);
        }

        public void SetItemDataValue(int index, string key, object value)
        {
            if (index >= Count)
            {
                return;
            }

            object item = this[index];
            if (!(item is AnnotatedData))
            {
                return;
            }

            AnnotatedData data = item as AnnotatedData;
            data.SetDataValue(key, value);
        }

        public object GetItemDataValue(int index, string key)
        {
            if (index >= Count)
            {
                return null;
            }

            object item = this[index];
            if (!(item is AnnotatedData))
            {
                return null;
            }

            AnnotatedData data = item as AnnotatedData;
            return data.GetDataValue(key);
        }

    }

    public class IncidentData016 : AnnotatedData
    {
        [JsonIgnore]
        public string Id
        {
            get
            {
                if (Data.ContainsKey("Id"))
                {
                    return Data["Id"] as string;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                Data["Id"] = value;
            }
        }

        public DateTime Time
        {
            get
            {
                DateTime output = DateTime.MinValue;
                if (Data.ContainsKey("Time"))
                {
                    if (Data["Time"] is long)
                    {
                        output = new DateTime((long)Data["Time"]);
                    }
                    else if (Data["Time"] is string)
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
                Data["Time"] = value;
            }
        }

        public string Location
        {
            get
            {
                if (Data.ContainsKey("Location"))
                {
                    return Data["Location"] as string;
                }
                return string.Empty;
            }
            set
            {
                Data["Location"] = value;
            }
        }

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
                    }
                    else if (Data["Longitude"] is double)
                    {
                        return (double)Data["Longitude"];
                    }
                    else if (Data["Longitude"] is int)
                    {
                        return Convert.ToDouble((int)Data["Longitude"]);
                    }
                }

                return output;
            }
            set
            {
                Data["Longitude"] = value;
            }
        }

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
                    else if (Data["Latitude"] is int)
                    {
                        return Convert.ToDouble((int)Data["Latitude"]);
                    }
                }

                return output;
            }
            set
            {
                Data["Latitude"] = value;
            }
        }

        public DataSet016<ResponseData016> Responses
        {
            get
            {
                DataSet016<ResponseData016> output = new DataSet016<ResponseData016>(this);
                if (!Data.ContainsKey("Responses") || !(Data["Responses"] is DataSet016<ResponseData016>))
                {
                    Data["Responses"] = output;
                    return output;
                }

                return Data["Responses"] as DataSet016<ResponseData016>;
            }
            set
            {
                Data["Responses"] = value;
            }
        }
    }

    public class ResponseData016 : AnnotatedData
    {
        public string Id
        {
            get
            {
                if (Data.ContainsKey("Id"))
                {
                    return Data["Id"] as string;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                Data["Id"] = value;
            }
        }

        public DataSet016<TimingData> Benchmarks { get { return TimingData; } set { TimingData = value; } }

        [JsonIgnore]
        public DataSet016<TimingData> TimingData
        {
            get
            {
                DataSet016<TimingData> output = new DataSet016<TimingData>(this);
                if (!Data.ContainsKey("Benchmarks") || !(Data["Benchmarks"] is DataSet016<TimingData>))
                {
                    Data["Benchmarks"] = output;
                    return output;
                }

                return Data["Benchmarks"] as DataSet016<TimingData>;
            }
            set
            {
                Data["Benchmarks"] = value;
            }
        }

        public ResponseData016()
        {

        }

        public ResponseData016(string id = "", InternedDictionary<string, object> data = null, TimingData[] benchmarks = null)
        {
            Id = id;
            TimingData = new DataSet016<TimingData>(this);

            foreach (KeyValuePair<string, object> kvp in data)
            {
                if (!Data.ContainsKey(kvp.Key))
                {
                    Data.Add(kvp.Key, kvp.Value);
                }
                else
                {
                    Data[kvp.Key] = kvp.Value;
                }
            }

            if (benchmarks != null)
            {
                TimingData.AddRange(benchmarks);
            }
        }

        public TimingData GetTimingDataByName(string name)
        {
            TimingData output = (from TimingData t in TimingData
                                 where t.Name == name
                                 select t).FirstOrDefault();

            return output;
        }
    }
}
