using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;


namespace Levrum.Data.Sources
{
    [JsonConverter(typeof(IDataSourceConverter))]
    public interface IDataSource : ICloneable, IDisposable
    {
        string Name { get; set; }
        DataSourceType Type { get; }
        string Info { get; }

        List<string> RequiredParameters { get; }
        Dictionary<string, string> Parameters { get; set; }
        
        bool Connect();
        void Disconnect();
        List<string> GetTables();
        List<string> GetColumns(string table);
        List<string> GetColumnValues(string table, string column);
        List<string[]> GetRecords(string table);
    }

    public enum DataSourceType { CsvSource, SqlSource };


    public class IDataSourceConcreteClassConverter : DefaultContractResolver
    {
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(IDataSource).IsAssignableFrom(objectType) && !objectType.IsAbstract)
            {
                return null;
            }
            return base.ResolveContractConverter(objectType);
        }
    }

    public class IDataSourceConverter : JsonConverter
    {
        static JsonSerializerSettings s_serializerSettings = new JsonSerializerSettings() { ContractResolver = new IDataSourceConcreteClassConverter() };

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(IDataSource));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            int index = jo["Type"].Value<int>();
            try
            {
                DataSourceType type = (DataSourceType)index;
                if (type == DataSourceType.CsvSource)
                {
                    return JsonConvert.DeserializeObject<CsvSource>(jo.ToString(), s_serializerSettings);
                } else if (type == DataSourceType.SqlSource)
                {
                    return JsonConvert.DeserializeObject<SqlSource>(jo.ToString(), s_serializerSettings);
                } else
                {
                    throw new NotImplementedException();
                }
            } catch (Exception ex)
            {
                throw new NotImplementedException();
            }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
