using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Newtonsoft.Json;

using Levrum.DataClasses;

namespace Levrum.DataAggregators.Test
{
    public class TimeSeriesExampleOne
    {
        List<IncidentData> Incidents { get; set; } = new List<IncidentData>();

        public TimeSeriesExampleOne()
        {

        }

        public TimeSeriesExampleOne(List<IncidentData> _incidents)
        {
            Incidents = _incidents;
        }

        public void BuildDemoData()
        {
            IncidentData data = new IncidentData("1", new DateTime(2018, 12, 25, 7, 07, 07), "Corvallis", -123.2620, 44.5646);
            data.Data["Code"] = "311";
            data.Data["Category"] = "EMS";
            data.Data["Type"] = "Alpha";
            data.Data["Area"] = "Public";
            Incidents.Add(data);
            data = new IncidentData("2", new DateTime(2018, 12, 25, 21, 09, 21), "Corvallis", -123.2620, 44.5646);
            data.Data["Code"] = "311";
            data.Data["Category"] = "EMS";
            data.Data["Type"] = "Alpha";
            data.Data["Area"] = "Residential";
            Incidents.Add(data);
            data = new IncidentData("3", new DateTime(2018, 11, 28, 15, 15, 15), "Corvallis", -123.2620, 44.5646);
            data.Data["Code"] = "311";
            data.Data["Category"] = "EMS";
            data.Data["Type"] = "Alpha";
            data.Data["Area"] = "Residential";
            Incidents.Add(data);
            data = new IncidentData("4", new DateTime(2018, 11, 20, 20, 15, 15), "Corvallis", -123.2620, 44.5646);
            data.Data["Code"] = "311";
            data.Data["Category"] = "EMS";
            data.Data["Type"] = "Alpha";
            data.Data["Area"] = "Residential";
            Incidents.Add(data);
            data = new IncidentData("5", new DateTime(2018, 10, 12, 17, 15, 15), "Corvallis", -123.2620, 44.5646);
            data.Data["Code"] = "311";
            data.Data["Category"] = "EMS";
            data.Data["Type"] = "Alpha";
            data.Data["Area"] = "Residential";
            Incidents.Add(data);
            data = new IncidentData("6", new DateTime(2018, 9, 13, 18, 15, 15), "Corvallis", -123.2620, 44.5646);
            data.Data["Code"] = "311";
            data.Data["Category"] = "EMS";
            data.Data["Type"] = "Alpha";
            data.Data["Area"] = "Residential";
            Incidents.Add(data);
            data = new IncidentData("7", new DateTime(2018, 8, 4, 13, 15, 15), "Corvallis", -123.2620, 44.5646);
            data.Data["Code"] = "311";
            data.Data["Category"] = "EMS";
            data.Data["Type"] = "Alpha";
            data.Data["Area"] = "Residential";
            Incidents.Add(data);
            data = new IncidentData("8", new DateTime(2018, 8, 10, 08, 15, 15), "Corvallis", -123.2620, 44.5646);
            data.Data["Code"] = "311";
            data.Data["Category"] = "EMS";
            data.Data["Type"] = "Alpha";
            data.Data["Area"] = "Residential";
            Incidents.Add(data);
        }




        public object RunAggregatorTest()
        {
            Dictionary<object, Dictionary<object, Dictionary<object, int>>> output = new Dictionary<object, Dictionary<object, Dictionary<object, int>>>();

            MonthAggregator<IncidentData> monthAggregator = new MonthAggregator<IncidentData>();
            PropertyInfo prop = typeof(IncidentData).GetProperty("Time");
            monthAggregator.Member = prop;
            Dictionary<object, List<IncidentData>> monthAggregate = monthAggregator.GetData(Incidents);

            
            DataKeyValueDelegate areaDelegate = new DataKeyValueDelegate("Area");
            ValueDelegateAggregator<IncidentData> areaAggregator = new ValueDelegateAggregator<IncidentData>();
            areaAggregator.Name = "Area";
            areaAggregator.ValueDelegate = areaDelegate.Delegate;

            DataKeyValueDelegate codeDelegate = new DataKeyValueDelegate("Code");
            ValueDelegateAggregator<IncidentData> codeAggregator = new ValueDelegateAggregator<IncidentData>();
            codeAggregator.Name = "Code";
            codeAggregator.ValueDelegate = codeDelegate.Delegate;

            foreach (KeyValuePair<object, List<IncidentData>> monthEntry in monthAggregate)
            {
                if (!output.ContainsKey(monthEntry.Key))
                    output.Add(monthEntry.Key, new Dictionary<object, Dictionary<object, int>>());

                Dictionary<object, List<IncidentData>> areaAggregate = areaAggregator.GetData(monthEntry.Value);
                foreach (KeyValuePair<object, List<IncidentData>> areaEntry in areaAggregate)
                {
                    if (!output[monthEntry.Key].ContainsKey(areaEntry.Key))
                        output[monthEntry.Key].Add(areaEntry.Key, new Dictionary<object, int>());

                    Dictionary<object, List<IncidentData>> codeAggregate = codeAggregator.GetData(areaEntry.Value);
                    foreach (KeyValuePair<object, List<IncidentData>> codeEntry in codeAggregate)
                    {
                        if (!output[monthEntry.Key][areaEntry.Key].ContainsKey(codeEntry.Key)) {
                            output[monthEntry.Key][areaEntry.Key][codeEntry.Key] = codeEntry.Value.Count;
                        } else
                        {
                            output[monthEntry.Key][areaEntry.Key][codeEntry.Key] += codeEntry.Value.Count;
                        }
                    }
                }
            }

            return output;
        }

        public object RunGetAggregatedDataTest()
        {
            List<DataAggregator<IncidentData>> aggregators = new List<DataAggregator<IncidentData>>();

            MonthAggregator<IncidentData> monthAggregator = new MonthAggregator<IncidentData>();
            PropertyInfo prop = typeof(IncidentData).GetProperty("Time");
            monthAggregator.Member = prop;
            aggregators.Add(monthAggregator);

            DataKeyValueDelegate areaDelegate = new DataKeyValueDelegate("Area");
            ValueDelegateAggregator<IncidentData> areaAggregator = new ValueDelegateAggregator<IncidentData>();
            areaAggregator.Name = "Area";
            areaAggregator.ValueDelegate = areaDelegate.Delegate;
            aggregators.Add(areaAggregator);

            DataKeyValueDelegate codeDelegate = new DataKeyValueDelegate("Code");
            ValueDelegateAggregator<IncidentData> codeAggregator = new ValueDelegateAggregator<IncidentData>();
            codeAggregator.Name = "Code";
            codeAggregator.ValueDelegate = codeDelegate.Delegate;
            aggregators.Add(codeAggregator);

            return DataAggregator<IncidentData>.GetAggregatedData(aggregators, Incidents);
        }

        public object RunGroupByTest()
        {
            List<DataAggregator<IncidentData>> aggregators = new List<DataAggregator<IncidentData>>();

            MonthAggregator<IncidentData> monthAggregator = new MonthAggregator<IncidentData>();
            PropertyInfo prop = typeof(IncidentData).GetProperty("Time");
            monthAggregator.Member = prop;
            aggregators.Add(monthAggregator);

            DataKeyValueDelegate areaDelegate = new DataKeyValueDelegate("Area");
            ValueDelegateAggregator<IncidentData> areaAggregator = new ValueDelegateAggregator<IncidentData>();
            areaAggregator.Name = "Area";
            areaAggregator.ValueDelegate = areaDelegate.Delegate;
            aggregators.Add(areaAggregator);

            DataKeyValueDelegate codeDelegate = new DataKeyValueDelegate("Code");
            ValueDelegateAggregator<IncidentData> codeAggregator = new ValueDelegateAggregator<IncidentData>();
            codeAggregator.Name = "Code";
            codeAggregator.ValueDelegate = codeDelegate.Delegate;
            aggregators.Add(codeAggregator);
            var data = DataAggregator<IncidentData>.GetAggregatedData(aggregators, Incidents);
            var test = data.GroupBy(
                x => x.AggregatorValues.GetValueOrDefault("Area"),
                x => x.AggregatorValues.GetValueOrDefault("Code"), (area, codes) => new
                {
                    Area = area,
                    Count = codes.Count(),
                    CodeCount = getCodeCounts(codes.ToList())
                }); ;

            return test;
        }

        public Dictionary<object, int> getCodeCounts(List<object> codes)
        {
            Dictionary<object, int> codeCounts = new Dictionary<object, int>();
            foreach (object code in codes)
            {
                if (!codeCounts.ContainsKey(code))
                    codeCounts[code] = 1;
                else
                    codeCounts[code] = codeCounts[code] + 1;
            }

            return codeCounts;
        }
    }
}
