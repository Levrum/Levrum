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

        string IDColumn { get; set; }
        string ResponseIDColumn { get; set; }

        List<string> RequiredParameters { get; }
        Dictionary<string, string> Parameters { get; set; }

        bool Connect();
        void Disconnect();
        List<string> GetColumns();
        List<string> GetColumnValues(string column);
        List<Record> GetRecords();
    }

    public enum DataSourceType { CsvSource, SqlSource, EmergencyReportingSource, GeoSource };

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
        static JsonSerializerSettings s_serializerSettings;

        static IDataSourceConverter()
        {
            s_serializerSettings = new JsonSerializerSettings();
            s_serializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.All;
            s_serializerSettings.ContractResolver = new IDataSourceConcreteClassConverter();
        }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(IDataSource));
        }

        public Dictionary<int, object> JsonObjectRefs { get; private set; } = new Dictionary<int, object>();

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (jo.First.Path == "$ref")
            {
                return JsonObjectRefs[jo.Value<int>("$ref")];
            }

            int index = jo["Type"].Value<int>();
            try
            {
                DataSourceType type = (DataSourceType)index;
                object output;
                if (type == DataSourceType.CsvSource)
                {
                    output = JsonConvert.DeserializeObject<CsvSource>(jo.ToString(), s_serializerSettings);
                }
                else if (type == DataSourceType.SqlSource)
                {
                    output = JsonConvert.DeserializeObject<SqlSource>(jo.ToString(), s_serializerSettings);
                }
                else
                {
                    throw new NotImplementedException();
                }
                int value = jo.Value<int>("$id");
                JsonObjectRefs[value] = output;
                return output;
            }
            catch (Exception ex)
            {
                // Do stuff and things
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
