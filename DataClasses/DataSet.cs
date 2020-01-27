using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Newtonsoft.Json;

namespace Levrum.Data.Classes
{
    public class DataSet<T> : List<T>
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

        public DataSet()
        {

        }

        public DataSet(object _parent = null)
        {
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
}
