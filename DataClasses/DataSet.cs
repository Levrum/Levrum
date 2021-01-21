using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Newtonsoft.Json;

namespace Levrum.Data.Classes
{
    [JsonObject]
    public class DataSet<T> : IList<T>
    {
        public object Parent { get; set; }

        public List<T> Contents { get; set; } = new List<T>();

        public T this[int index]
        {
            get { return Contents[index]; }
            set { Contents[index] = value; }
        }

        [JsonIgnore]
        public int Count
        {
            get { return Contents.Count; }
        }

        [JsonIgnore]
        public bool IsReadOnly
        {
            get { return false; }
        }

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

        [JsonProperty(Required = Required.AllowNull)]
        public InternedDictionary<string, object> Data
        {
            get
            {
                if (m_data == null)
                    m_data = new InternedDictionary<string, object>();

                return m_data;
            }

            set
            {
                m_data = value;
            }
        }

        public DataSet()
        {

        }

        public DataSet(object _parent = null)
        {
            Parent = _parent;
        }

        public DataSet(List<T> _list = null, object _parent = null)
        {
            if (_list != null)
            {
                AddRange(_list);
            }
            Parent = _parent;
        }

        public DataSet(string id = "", List<T> items = null, Dictionary<string, object> data = null, object _parent = null)
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

        public void SetItemDataValue(int index, string key, object value)
        {
            if (index >= Contents.Count)
            {
                return;
            }

            object item = Contents[index];
            if (!(item is AnnotatedData))
            {
                return;
            }

            AnnotatedData data = item as AnnotatedData;
            data.SetDataValue(key, value);
        }

        public object GetItemDataValue(int index, string key)
        {
            if (index >= Contents.Count)
            {
                return null;
            }

            object item = Contents[index];
            if (!(item is AnnotatedData))
            {
                return null;
            }

            AnnotatedData data = item as AnnotatedData;
            return data.GetDataValue(key);
        }

        #region IList<T>

        public void Add(T item)
        {
            if (Parent == null)
            {
                Contents.Add(item);
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

            Contents.Add(item);
        }

        public void AddRange(IEnumerable<T> range)
        {
            if (Parent == null)
            {
                Contents.AddRange(range);
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

            Contents.AddRange(items);
        }

        public int IndexOf(T item)
        {
            return Contents.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            Contents.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            Contents.RemoveAt(index);
        }

        #endregion

        #region ICollection<T>

        public void Clear()
        {
            Contents.Clear();
        }

        public bool Contains(T item)
        {
            return Contents.Contains(item);
        }

        public void CopyTo(T[] array, int index)
        {
            Contents.CopyTo(array, index);
        }

        public bool Remove(T item)
        {
            return Contents.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Contents.GetEnumerator();
        }

        IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Contents.GetEnumerator();
        }

        #endregion

        public string Serialize()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.All;
            settings.Formatting = Formatting.Indented;
            settings.PreserveReferencesHandling = PreserveReferencesHandling.All;

            return JsonConvert.SerializeObject(this, settings);
        }

        public void Serialize(string fileName)
        {
            using (TextWriter writer = File.CreateText(fileName))
            {
                var serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.TypeNameHandling = TypeNameHandling.Auto;
                serializer.PreserveReferencesHandling = PreserveReferencesHandling.All;
                serializer.Serialize(writer, this);
            }
        }

        public void Serialize(FileInfo file)
        {
            Serialize(file.FullName);
        }

        public static DataSet<T> Deserialize(string fileName)
        {
            return Deserialize(new FileInfo(fileName));
        }

        public static DataSet<T> Deserialize(FileInfo file)
        {
            try
            {
                using (StreamReader streamReader = new StreamReader(file.OpenRead()))
                using (JsonReader reader = new JsonTextReader(streamReader))
                {
                    var serializer = new JsonSerializer();
                    DataSet<T> output = serializer.Deserialize<DataSet<T>>(reader);
                    return output;
                }
            } catch (Exception ex)
            {
                // Try and load old-style DataSets here
                try
                {
                    using (StreamReader streamReader = new StreamReader(file.OpenRead()))
                    {
                        string json = streamReader.ReadToEnd();
                        json = json.Replace("DataSet", "DataSet016");
                        json = json.Replace("IncidentData", "IncidentData016");
                        json = json.Replace("ResponseData", "ResponseData016");
                        return DeserializeJson(json, true);
                    }
                }
                catch (Exception ex2)
                {
                    throw new Exception(string.Format("String does not contain valid DataSet JSON: {0} {1}", ex, ex2));
                }
            }
        }

        public static DataSet<T> DeserializeJson(string json, bool forceOldStyle = false)
        {
            Exception lastEx = null;
            if (!forceOldStyle) {
                try
                {
                    DataSet<T> output = JsonConvert.DeserializeObject<DataSet<T>>(json);
                    return output;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                }
            }

            // Try and load old-style DataSets here
            try
            {
                if (typeof(T) == typeof(IncidentData))
                {
                    DataSet016<IncidentData016> oldDataSet = JsonConvert.DeserializeObject<DataSet016<IncidentData016>>(json, new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.All });
                    DataSet<IncidentData> output = new DataSet<IncidentData>();
                    foreach (IncidentData016 oldIncident in oldDataSet)
                    {
                        IncidentData incident = new IncidentData(data: oldIncident.Data);
                        foreach (ResponseData016 oldResponse in oldIncident.Responses)
                        {
                            ResponseData response = new ResponseData(oldResponse.Id, oldResponse.Data, oldResponse.TimingData.ToArray());
                        }
                        output.Add(incident);
                    }
                    return output as DataSet<T>;
                } else
                {
                    throw new NotImplementedException();
                }
            } catch (Exception ex2)
            {
                throw new Exception(string.Format("String does not contain valid DataSet JSON: {0} {1}", lastEx, ex2));
            }
        }
    }
}
