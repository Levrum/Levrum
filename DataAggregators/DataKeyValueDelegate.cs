using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Levrum.DataAggregators
{
    public class DataKeyValueDelegate
    {
        public string Key { get; set; }

        public DataAggregatorValueDelegate Delegate { get { return new DataAggregatorValueDelegate(Run); } }

        public DataKeyValueDelegate()
        {

        }

        public DataKeyValueDelegate(string _key = null)
        {
            Key = _key;
        }

        public object[] Run(object dataObject)
        {
            object[] output = new object[1];

            if (string.IsNullOrWhiteSpace(Key))
                throw new MemberAccessException(string.Format("DataKeyValueDelegate run with unset Key"));

            MemberInfo[] info = dataObject.GetType().GetMember("Data");

            if (info.Length == 0)
                throw new MemberAccessException(string.Format("DataKeyValueDelegate run on object without a Data dictionary: {0}", dataObject));

            object data = null;
            if (info[0] is FieldInfo)
            {
                FieldInfo fieldInfo = info[0] as FieldInfo;
                data = fieldInfo.GetValue(dataObject);
            } else if (info[0] is PropertyInfo)
            {
                PropertyInfo propertyInfo = info[0] as PropertyInfo;
                data = propertyInfo.GetValue(dataObject);
            }

            if (!(data is Dictionary<string, object>))
                throw new MemberAccessException(string.Format("DataKeyValueDelegate run on object with Data member of invalid type ({0}): {1}", data.GetType(), dataObject));

            Dictionary<string, object> dictionary = data as Dictionary<string, object>;

            object value;
            if (!dictionary.TryGetValue(Key, out value))
                return new object[0];

            output[0] = value;

            return output;
        }
    }
}
