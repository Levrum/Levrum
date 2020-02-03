using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;

using Levrum.Data.Classes;

using Levrum.Utils.Time;

namespace Levrum.Data.Aggregators
{
    public static class DataAggregators
    {
        static DataAggregators()
        {

        }

        public static List<string> Aggregations
        {
            get
            {
                List<string> names = new List<string>();
                foreach (Type type in GetAggregatorTypes())
                {
                    DataAggregatorAttribute nameAttribute = type.GetCustomAttribute(typeof(DataAggregatorAttribute)) as DataAggregatorAttribute;
                    names.Add(nameAttribute.Name);
                }

                return names;
            }
        }

        public static DataAggregator<T> GetAggregator<T>(string name)
        {
            DataAggregator<T> aggregator = null;
            foreach (Type type in GetAggregatorTypes())
            {
                DataAggregatorAttribute nameAttribute = type.GetCustomAttribute(typeof(DataAggregatorAttribute)) as DataAggregatorAttribute;
                if (nameAttribute.Name == name)
                {
                    Type aggregatorType = type.MakeGenericType(typeof(T));
                    aggregator = (DataAggregator<T>)Activator.CreateInstance(aggregatorType);
                }
            }

            return aggregator;
        }

        public static List<Type> GetAggregatorTypes()
        {
            return getTypesWithAttribute(typeof(DataAggregatorAttribute));
        }

        private static List<Type> getTypesWithAttribute(Type attributeClass = null)
        {
            List<Type> output = new List<Type>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (attributeClass != null && typeof(Attribute).IsAssignableFrom(attributeClass))
                    {
                        Attribute attribute = type.GetCustomAttribute(attributeClass);
                        if (attribute == null)
                            continue;
                    }

                    output.Add(type);
                }
            }

            return output;
        }
    }

    public class DataAggregatorAttribute : Attribute
    {
        public string Name { get; set; }
    }

    public delegate object[] DataAggregatorValueDelegate(object obj);

    public class AggregatedData<T> : AnnotatedObject<T>
    {
        public Dictionary<string, object> AggregatorValues 
        { 
            get { return Data; }  
        }

        public AggregatedData(T _data) : base(_data)
        {

        }
    }

    public enum DataAggregatorCategoryType { Hour, Day, Month, Year, Unit, Category }

    [DataAggregator(Name = "Base")]
    public abstract class DataAggregator<T>
    {
        public virtual Type MemberType { get; protected set; } = typeof(object);
        public MemberInfo Member { get; set; } = null;
        public List<object> Categories { get; set; } = new List<object>();
        public virtual List<object> SortedKeys { get; } = null;

        private string m_name = "";

        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(m_name))
                {
                    DataAggregatorAttribute nameAttribute = GetType().GetCustomAttribute(typeof(DataAggregatorAttribute)) as DataAggregatorAttribute;
                    return nameAttribute.Name;
                }

                return m_name;
            }
            set
            {
                m_name = value;
            }
        }

        public DataAggregator()
        {

        }

        public DataAggregator(MemberInfo _member = null, List<object> _categories = null)
        {
            Member = _member;
            Categories = _categories;
        }

        public virtual Dictionary<object, List<T>> GetData(List<T> data)
        {
            Dictionary<object, List<T>> output = new Dictionary<object, List<T>>();
            output["All"] = data;

            return output;
        }

        public static AnnotatedObject<Dictionary<object, object>> GetData(List<DataAggregator<T>> aggregators, List<T> data)
        {
            List<AggregatedData<T>> aggregatedData = GetAggregatedData(aggregators, data);
            List<string> aggregatorNames = new List<string>();

            foreach (DataAggregator<T> aggregator in aggregators)
            {
                string name = aggregator.Name;
                int i = 1;
                while (aggregatorNames.Contains(name))
                {
                    name = aggregator.Name + i;
                    i++;
                }
                aggregatorNames.Add(name);
            }

            Dictionary<string, HashSet<object>> aggregatorCategories = new Dictionary<string, HashSet<object>>();
            foreach (string aggregatorName in aggregatorNames)
            {
                List<object> categories = (from AggregatedData<T> datum in aggregatedData
                                           select datum.AggregatorValues.ContainsKey(aggregatorName) ? datum.AggregatorValues[aggregatorName] : null).ToList();

                aggregatorCategories[aggregatorName] = new HashSet<object>();
                foreach (object category in categories)
                {
                    aggregatorCategories[aggregatorName].Add(category);
                }
            }

            // Loop through aggregators and build dictionary<object, object> tree
            string[] aggregatorNamesArray = aggregatorNames.ToArray();
            
            return buildSubDictionaries(new List<KeyValuePair<string, object>>(), aggregatorCategories, aggregatedData);
        }

        private static AnnotatedObject<Dictionary<object, object>> buildSubDictionaries(
            List<KeyValuePair<string, object>> parentCategories,
            Dictionary<string, HashSet<object>> subCategories,
            List<AggregatedData<T>> aggregatedData)
        {
            AnnotatedObject<Dictionary<object, object>> output = new AnnotatedObject<Dictionary<object, object>>(new Dictionary<object, object>());
            var dictionary = output.Object;

            KeyValuePair<string, HashSet<object>> categories = subCategories.First();
            Dictionary<string, HashSet<object>> remainingSubCategories = new Dictionary<string, HashSet<object>>(subCategories);
            remainingSubCategories.Remove(categories.Key);
            foreach (object category in categories.Value)
            {
                List<KeyValuePair<string, object>> newParentCategories = new List<KeyValuePair<string, object>>(parentCategories);
                newParentCategories.Add(new KeyValuePair<string, object>(categories.Key, category));
                if (remainingSubCategories.Count > 0)
                {
                    dictionary[category] = buildSubDictionaries(newParentCategories, remainingSubCategories, aggregatedData);
                }
                else
                {
                    List<AggregatedData<T>> filteredData = aggregatedData;
                    foreach (KeyValuePair<string, object> parentCategory in newParentCategories)
                    {
                        filteredData = (from AggregatedData<T> datum in filteredData
                                        where datum.AggregatorValues.ContainsKey(parentCategory.Key) &&
                                        datum.AggregatorValues[parentCategory.Key] == parentCategory.Value
                                        select datum).ToList();
                    }

                    dictionary[category] = (from AggregatedData<T> datum in filteredData
                                            select datum.Data).ToList();
                }
            }

            return output;
        }

        public static List<AggregatedData<T>> GetAggregatedData(List<DataAggregator<T>> aggregators, List<T> data)
        {
            Dictionary<T, AggregatedData<T>> aggregatedDataDictionary = new Dictionary<T, AggregatedData<T>>();
            foreach (T datum in data)
            {
                aggregatedDataDictionary[datum] = new AggregatedData<T>(datum);
            }

            HashSet<string> usedNames = new HashSet<string>();
            foreach (DataAggregator<T> dataAggregator in aggregators)
            {
                Dictionary<object, List<T>> aggregation = dataAggregator.GetData(data);
                string name = dataAggregator.Name;
                int i = 1;
                while (usedNames.Contains(name))
                {
                    name = dataAggregator.Name + i;
                    i++;
                }
                usedNames.Add(name);

                foreach (KeyValuePair<object, List<T>> kvp in aggregation)
                {
                    foreach (T datum in kvp.Value)
                    {
                        AggregatedData<T> aggregatedData;
                        if (!aggregatedDataDictionary.ContainsKey(datum))
                            aggregatedDataDictionary.Add(datum, new AggregatedData<T>(datum));

                        aggregatedData = aggregatedDataDictionary[datum];
                        aggregatedData.AggregatorValues.Add(name, kvp.Key);
                    }
                }
            }

            return new List<AggregatedData<T>>(aggregatedDataDictionary.Values);
        }

        public object GetValue(T datum)
        {
            if (Member == null)
                throw new MemberAccessException("Aggregator created with null Member. Member must contain MemberInfo object describing a field in order to use GetValue.");

            if (Member is FieldInfo)
            {
                FieldInfo fieldInfo = Member as FieldInfo;
                return fieldInfo.GetValue(datum);
            }
            else if (Member is PropertyInfo)
            {
                PropertyInfo propertyInfo = Member as PropertyInfo;
                return propertyInfo.GetValue(datum);
            }

            return null;
        }

        protected Dictionary<object, List<T>> sortByNumericKey(Dictionary<object, List<T>> output)
        {
            List<KeyValuePair<object, List<T>>> unsortedOutput = output.ToList();
            unsortedOutput.Sort(
                delegate (KeyValuePair<object, List<T>> pair1,
                KeyValuePair<object, List<T>> pair2)
                {
                    return Convert.ToDouble(pair1.Key).CompareTo(Convert.ToDouble(pair2.Key));
                }
            );

            output.Clear();
            foreach (KeyValuePair<object, List<T>> kvp in unsortedOutput)
            {
                output.Add(kvp.Key, kvp.Value);
            }

            return output;
        }

        protected Dictionary<object, object> sortByAlphaKey(Dictionary<object, object> output)
        {
            List<KeyValuePair<object, object>> unsortedOutput = output.ToList();
            unsortedOutput.Sort(
                delegate (KeyValuePair<object, object> pair1,
                KeyValuePair<object, object> pair2)
                {
                    return Convert.ToString(pair1.Key).CompareTo(Convert.ToString(pair2.Key));
                }
            );

            output.Clear();
            foreach (KeyValuePair<object, object> kvp in unsortedOutput)
            {
                output.Add(kvp.Key, kvp.Value);
            }

            return output;
        }
    }

    [DataAggregator(Name = "None")]
    public class NoAggregator<T> : DataAggregator<T>
    {

    }

    [DataAggregator(Name = "Hour of Day")]
    public class HourOfDayAggregator<T> : DataAggregator<T>
    {
        public override Type MemberType { get; protected set; } = typeof(DateTime);

        public override Dictionary<object, List<T>> GetData(List<T> data)
        {
            Dictionary<object, List<T>> output = new Dictionary<object, List<T>>();
            if (Member == null)
                throw new MemberAccessException("HourOfDayAggregator created with null Member. Member must contain a MemberInfo object describing a DateTime.");

            for (int i = 0; i < 24; i++)
            {
                if (Categories.Count > 0 && !Categories.Contains(i.ToString()) && !Categories.Contains(i))
                    continue;

                output[i] = new List<T>();
            }

            foreach (T datum in data)
            {
                object value = GetValue(datum);
                if (!(value is DateTime))
                    continue;

                DateTime date = (DateTime)value;
                if (Categories.Count > 0 && !Categories.Contains(date.Hour.ToString()) && !Categories.Contains(date.Hour))
                    continue;

                output[date.Hour].Add(datum);
            }

            return output;
        }
    }

    [DataAggregator(Name = "Day of Week")]
    public class DayOfWeekAggregator<T> : DataAggregator<T>
    {
        static DayOfWeekAggregator()
        {
            foreach (string dayName in Enum.GetNames(typeof(DayOfWeek)))
            {
                DayOfWeekKeys.Add(dayName.Substring(0, 3));
            }
        }

        public static List<object> DayOfWeekKeys = new List<object>();

        public override Type MemberType { get; protected set; } = typeof(DateTime);

        public override List<object> SortedKeys { get { return DayOfWeekKeys; } }

        public override Dictionary<object, List<T>> GetData(List<T> data)
        {
            Dictionary<object, List<T>> output = new Dictionary<object, List<T>>();
            if (Member == null)
                throw new MemberAccessException("DayOfWeekAggregator created with null Member. Member must contain a MemberInfo object describing a DateTime.");

            foreach (string dayName in Enum.GetNames(typeof(DayOfWeek)))
            {
                if (Categories.Count > 0 && !Categories.Contains(dayName) && !Categories.Contains(dayName.Substring(0, 3)))
                    continue;

                output[dayName.Substring(0, 3)] = new List<T>();
            }

            foreach (T datum in data)
            {
                object value = GetValue(datum);
                if (!(value is DateTime))
                    continue;

                DateTime date = (DateTime)value;
                string dayOfWeek = date.DayOfWeek.ToString().Substring(0, 3);

                if (Categories.Count > 0 && !Categories.Contains(date.DayOfWeek.ToString()) && !Categories.Contains(dayOfWeek))
                    continue;

                if (!output.ContainsKey(dayOfWeek))
                    output[dayOfWeek] = new List<T>();

                output[dayOfWeek].Add(datum);
            }

            return output;
        }
    }

    [DataAggregator(Name = "Month of Year")]
    public class MonthOfYearAggregator<T> : DataAggregator<T>
    {
        static MonthOfYearAggregator()
        {
            for (int i = 1; i <= 12; i++)
            {
                string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i);
                string month = monthName.Substring(0, 3);

                MonthOfYearKeys.Add(month);
            }
        }

        public static List<object> MonthOfYearKeys = new List<object>();

        public override Type MemberType { get; protected set; } = typeof(DateTime);

        public override List<object> SortedKeys { get { return MonthOfYearKeys; } }

        public override Dictionary<object, List<T>> GetData(List<T> data)
        {
            Dictionary<object, List<T>> output = new Dictionary<object, List<T>>();
            if (Member == null)
                throw new MemberAccessException("MonthOfYearAggregator created with null Member. Member must contain a MemberInfo object describing a DateTime.");

            for (int i = 1; i <= 12; i++)
            {
                string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i);
                string month = monthName.Substring(0, 3);
                if (Categories.Count > 0 && !Categories.Contains(monthName) && !Categories.Contains(month))
                    continue;

                output[month] = new List<T>();
            }

            foreach (T datum in data)
            {
                object value = GetValue(datum);
                if (!(value is DateTime))
                    continue;

                DateTime date = (DateTime)value;
                string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(date.Month);
                string month = monthName.Substring(0, 3);
                if (Categories.Count > 0 && !Categories.Contains(monthName) && !Categories.Contains(month))
                    continue;

                if (!output.ContainsKey(month))
                    output[month] = new List<T>();

                output[month].Add(datum);
            }

            return output;
        }
    }

    [DataAggregator(Name = "Day")]
    public class DayAggregator<T> : DataAggregator<T>
    {
        public override Type MemberType { get; protected set; } = typeof(DateTime);

        public override Dictionary<object, List<T>> GetData(List<T> data)
        {
            Dictionary<object, List<T>> output = new Dictionary<object, List<T>>();
            if (Member == null)
                throw new MemberAccessException("DayAggregator created with null Member. Member must contain a MemberInfo object describing a DateTime.");

            foreach (T datum in data)
            {
                object value = GetValue(datum);
                if (!(value is DateTime))
                    continue;

                DateTime date = (DateTime)value;
                int key = DateTimeUtils.GetYMDKey(date);
                if (!output.ContainsKey(key))
                    output[key] = new List<T>();

                output[key].Add(datum);
            }

            return sortByNumericKey(output);
        }

        public static int GetYearFromKey(int key)
        {
            return DateTimeUtils.GetYearFromYMDKey(key);
        }

        public static int GetMonthFromKey(int key)
        {
            return DateTimeUtils.GetMonthFromYMDKey(key);
        }

        public static int GetDayFromKey(int key)
        {
            return DateTimeUtils.GetDayFromYMDKey(key);
        }
    }

    [DataAggregator(Name = "Month")]
    public class MonthAggregator<T> : DataAggregator<T>
    {
        public override Type MemberType { get; protected set; } = typeof(DateTime);

        public override Dictionary<object, List<T>> GetData(List<T> data)
        {
            Dictionary<object, List<T>> output = new Dictionary<object, List<T>>();
            if (Member == null)
                throw new MemberAccessException("MonthAggregator created with null Member. Member must contain a MemberInfo object describing a DateTime.");

            foreach (T datum in data)
            {
                object value = GetValue(datum);
                if (!(value is DateTime))
                    continue;

                DateTime date = (DateTime)value;
                int key = DateTimeUtils.GetYMKey(date);
                if (!output.ContainsKey(key))
                    output[key] = new List<T>();

                output[key].Add(datum);
            }

            return sortByNumericKey(output);
        }

        public static int GetMonthFromKey(int key)
        {
            return DateTimeUtils.GetMonthFromYMKey(key);
        }

        public static int GetYearFromKey(int key)
        {
            return DateTimeUtils.GetYearFromYMKey(key);
        }
    }

    [DataAggregator(Name = "Year")]
    public class YearAggregator<T> : DataAggregator<T>
    {
        public override Type MemberType { get; protected set; } = typeof(DateTime);

        public override Dictionary<object, List<T>> GetData(List<T> data)
        {
            Dictionary<object, List<T>> output = new Dictionary<object, List<T>>();
            if (Member == null)
                throw new MemberAccessException("YearAggregator created with null Member. Member must contain a MemberInfo object describing a DateTime.");

            foreach (T datum in data)
            {
                object value = GetValue(datum);
                if (!(value is DateTime))
                    continue;

                DateTime date = (DateTime)value;
                if (!output.ContainsKey(date.Year))
                    output[date.Year] = new List<T>();

                output[date.Year].Add(datum);
            }

            return sortByNumericKey(output);
        }
    }

    [DataAggregator(Name = "Category")]
    public class CategoryAggregator<T> : DataAggregator<T>
    {
        public override Dictionary<object, List<T>> GetData(List<T> data)
        {
            Dictionary<object, List<T>> output = new Dictionary<object, List<T>>();
            if (Member == null)
                throw new MemberAccessException("CategoryAggregator created with null Member. Member must contain a MemberInfo object for the field on which to aggregate.");

            foreach (T datum in data)
            {
                object value = GetValue(datum);

                if (Categories.Count > 0)
                {
                    if (!Categories.Contains(value))
                        continue;
                }

                if (!output.ContainsKey(value))
                    output[value] = new List<T>();

                output[value].Add(datum);
            }

            return output;
        }
    }

    [DataAggregator(Name = "Delegate")]
    public class ValueDelegateAggregator<T> : DataAggregator<T>
    {
        public DataAggregatorValueDelegate ValueDelegate { get; set; }

        public override Dictionary<object, List<T>> GetData(List<T> data)
        {
            Dictionary<object, List<T>> output = new Dictionary<object, List<T>>();
            if (ValueDelegate == null)
                throw new MemberAccessException("ValueDelegateAggregator created with null ValueDelegate. ValueDelegate must contain a DataAggregatorValueDelegate that obtains a value on which to aggregate.");

            foreach (T datum in data)
            {
                object[] values = ValueDelegate(datum);

                foreach (object value in values)
                {
                    if (Categories.Count > 0)
                    {
                        if (!Categories.Contains(value))
                            continue;
                    }

                    if (!output.ContainsKey(value))
                        output[value] = new List<T>();

                    output[value].Add(datum);
                }
            }

            return output;
        }
    }
}
