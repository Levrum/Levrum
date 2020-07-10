using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

namespace Levrum.Data.Classes
{
    public class ResponseData : AnnotatedData
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

        public DataSet<TimingData> Benchmarks { get { return TimingData; } set { TimingData = value; } }

        [JsonIgnore]
        public DataSet<TimingData> TimingData
        {
            get
            {
                DataSet<TimingData> output = new DataSet<TimingData>(this);
                if (!Data.ContainsKey("Benchmarks") || !(Data["Benchmarks"] is DataSet<TimingData>))
                {
                    Data["Benchmarks"] = output;
                    return output;
                }

                return Data["Benchmarks"] as DataSet<TimingData>;
            }
            set
            {
                Data["Benchmarks"] = value;
            }
        }

        public ResponseData()
        {

        }

        public ResponseData(string id = "", TimingData[] benchmarks = null)
        {
            Id = id;
            TimingData = new DataSet<TimingData>(this);

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
