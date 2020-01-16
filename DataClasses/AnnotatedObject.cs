using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Levrum.Data.Classes
{
    public class AnnotatedObject<T> : IComparable
    {
        public T Object { get; set; } = default(T);
        public InternedDictionary<string, object> Data { get; set; } = new InternedDictionary<string, object>();

        public AnnotatedObject(T _object)
        {
            Object = _object;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            
            int value;

            AnnotatedObject<T> otherData = obj as AnnotatedObject<T>;
            if (otherData == null)
                throw new ArgumentException(string.Format("Object is not AnnotatedObject<{0}>", typeof(T)));

            if (typeof(T) is IComparable)
            {
                value = (Object as IComparable).CompareTo(obj as IComparable);
                if (value != 0) 
                {
                    return value;
                }
            }

            foreach (KeyValuePair<string, object> kvp in Data)
            {
                if (!otherData.Data.ContainsKey(kvp.Key))
                    return 1;

                object thisObject = kvp.Value;
                object thatObject = otherData.Data[kvp.Key];

                // Sort order is use object type comparator, DateTime, number, string
                if (thisObject.GetType() == thatObject.GetType() && thisObject is IComparable)
                {
                    Type objectsType = thisObject.GetType();
                    MethodInfo comparator = objectsType.GetMethod("CompareTo", new Type[] { thatObject.GetType() });
                    value = (int)comparator.Invoke(thisObject, new object[1] { thatObject });
                    if (value != 0)
                    {
                        return value;
                    }
                }

                DateTime thisObjectAsDateTime = thisObject is DateTime ? (DateTime)thisObject : DateTime.MinValue;
                DateTime thatObjectAsDateTime = thatObject is DateTime ? (DateTime)thatObject : DateTime.MinValue;

                if (thisObjectAsDateTime != DateTime.MinValue && thatObjectAsDateTime != DateTime.MinValue)
                {
                    value = DateTime.Compare(thisObjectAsDateTime, thatObjectAsDateTime);
                    if (value != 0)
                    {
                        return value;
                    }
                }

                if (thisObjectAsDateTime != DateTime.MinValue)
                {
                    return 1;
                }

                if (thatObjectAsDateTime != DateTime.MinValue)
                {
                    return -1;
                }

                double thisObjectAsDouble = double.NaN;
                double thatObjectAsDouble = double.NaN;
                if (thisObject is double || thisObject is float || thisObject is long || thisObject is int)
                {
                    thisObjectAsDouble = (double)thisObject;
                }

                if (thatObject is double || thatObject is float || thatObject is long || thatObject is int)
                {
                    thatObjectAsDouble = (double)thatObject;
                }

                if (thisObjectAsDouble != double.NaN && thatObjectAsDouble != double.NaN)
                {
                    value = thisObjectAsDouble.CompareTo(thatObjectAsDouble);
                    if (value != 0)
                    {
                        return value;
                    }
                }

                if (thisObjectAsDouble != double.NaN)
                    return 1;

                if (thatObjectAsDouble != double.NaN)
                    return -1;

                string thisObjectAsString = thisObject.ToString();
                string thatObjectAsString = thatObject.ToString();

                value = thisObjectAsString.CompareTo(thatObjectAsString);
                if (value != 0)
                {
                    return value;
                }
            }

            foreach (KeyValuePair<string, object> otherKvp in otherData.Data)
            {
                if (!Data.ContainsKey(otherKvp.Key))
                    return -1;
            }

            return Data.Count.CompareTo(otherData.Data.Count);
        }
    }
}
