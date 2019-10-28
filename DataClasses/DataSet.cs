using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Levrum.DataClasses
{
    public class DataSet<T> : List<T>
    {
        public object Parent { get; set; }

        private char[] m_id;

        public string Id
        {
            get
            {
                return new string(m_id);
            }
            set
            {
                m_id = value.ToCharArray();
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
    }
}
