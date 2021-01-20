using System;
using System.Collections;
using System.Collections.Generic;
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
    }
}
