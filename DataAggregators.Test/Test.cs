using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using Levrum.DataAggregators;
using Levrum.DataClasses;

using Newtonsoft.Json;

namespace Levrum.DataAggregators.Test
{
    public class Test
    {
        List<IncidentData> Incidents = new List<IncidentData>();

        public Test()
        {

        }

        public void BuildDemoData()
        {
            var incident = new IncidentData("1", new DateTime(2018, 12, 25, 7, 07, 07), "Bethlehem", 35.200657, 31.705791);
            incident.Data["Code"] = "Z";
            incident.Data["Category"] = "A";
            incident.Data["Type"] = "JC";
            Incidents.Add(incident);
            incident = new IncidentData("999", new DateTime(1980, 09, 07, 21, 09, 21), "Corvallis", -123.2620, 44.5646);
            incident.Data["Code"] = "K";
            incident.Data["Category"] = "K";
            incident.Data["Type"] = "M";
            Incidents.Add(incident);
            incident = new IncidentData("456", new DateTime(1982, 03, 01, 15, 15, 15), "Corvallis", -123.2620, 44.5646);
            incident.Data["Code"] = "K";
            incident.Data["Category"] = "B";
            incident.Data["Type"] = "P";
            Incidents.Add(incident);
        }

        public void RunAggregatorTests()
        {
            Console.WriteLine("Available Aggregations:");
            foreach (string str in DataAggregators.Aggregations)
            {
                Console.WriteLine("\t{0}", str);
            }

            Console.WriteLine("\nNoAggregator:");
            NoAggregator<IncidentData> noAggTest = new NoAggregator<IncidentData>();
            Dictionary<object, List<IncidentData>> noAggTestDictionary = noAggTest.GetData(Incidents);

            Console.WriteLine(JsonConvert.SerializeObject(noAggTestDictionary));

            Console.WriteLine("\nHourOfDayAggregator:");
            HourOfDayAggregator<IncidentData> hodAggTest = new HourOfDayAggregator<IncidentData>();
            PropertyInfo prop = typeof(IncidentData).GetProperty("Time");
            hodAggTest.Member = prop;
            Dictionary<object, List<IncidentData>> hodAggTestDictionary = hodAggTest.GetData(Incidents);

            Console.WriteLine(JsonConvert.SerializeObject(hodAggTestDictionary));

            Console.WriteLine("\nDayOfWeekAggregator:");
            DayOfWeekAggregator<IncidentData> dowAggTest = new DayOfWeekAggregator<IncidentData>();
            dowAggTest.Member = prop;
            Dictionary<object, List<IncidentData>> dowAggTestDictionary = dowAggTest.GetData(Incidents);

            Console.WriteLine(JsonConvert.SerializeObject(dowAggTestDictionary));

            Console.WriteLine("\nMonthOfYearAggregator:");
            MonthOfYearAggregator<IncidentData> moyAggTest = new MonthOfYearAggregator<IncidentData>();
            moyAggTest.Member = prop;
            Dictionary<object, List<IncidentData>> moyAggTestDictionary = moyAggTest.GetData(Incidents);

            Console.WriteLine(JsonConvert.SerializeObject(moyAggTestDictionary));

            Console.WriteLine("\nCategoryAggregator:");
            CategoryAggregator<IncidentData> catAggTest = new CategoryAggregator<IncidentData>();
            catAggTest.Member = typeof(IncidentData).GetProperty("Location");
            Dictionary<object, List<IncidentData>> catAggTestDictionary = catAggTest.GetData(Incidents);

            Console.WriteLine(JsonConvert.SerializeObject(catAggTestDictionary));
            catAggTest.Categories.Add("Bethlehem");
            Dictionary<object, List<IncidentData>> nerdTestDictionary = catAggTest.GetData(Incidents);
            Console.WriteLine(JsonConvert.SerializeObject(nerdTestDictionary));
            
        }

        public void RunComboAggregatorTest()
        { 
            PropertyInfo prop = typeof(IncidentData).GetProperty("Time");
            MonthOfYearAggregator<IncidentData> moyAggTest = new MonthOfYearAggregator<IncidentData>();
            moyAggTest.Member = prop;

            CategoryAggregator<IncidentData> locAggTest = new CategoryAggregator<IncidentData>();
            locAggTest.Member = typeof(IncidentData).GetProperty("Location");

            // CategoryAggregator<IncidentData> codeAggTest = new CategoryAggregator<IncidentData>();
            // codeAggTest.Member = typeof(IncidentData).GetProperty("Code");

            List<DataAggregator<IncidentData>> aggregators = new List<DataAggregator<IncidentData>>();
            aggregators.Add(locAggTest);
            aggregators.Add(moyAggTest);
            // aggregators.Add(codeAggTest);

            Dictionary<object, object> dictionary = DataAggregator<IncidentData>.GetData(aggregators, Incidents);
            Console.WriteLine(JsonConvert.SerializeObject(dictionary));
        }
    }
}
