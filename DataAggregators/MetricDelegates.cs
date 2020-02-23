using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Levrum.Data.Classes;
using Levrum.Data.Classes.Tools;

using Levrum.Utils.Data;
using Levrum.Utils.Time;

namespace Levrum.Data.Aggregators
{
    public enum MetricType { IncidentData, ResponseData, TimingData }

    public static class MetricDelegates
    {
        static MetricDelegates()
        {

        }

        public static List<string> Metrics
        {
            get
            {
                List<string> names = new List<string>(); foreach (Type type in GetMetricTypes())
                {
                    MetricDelegateAttribute nameAttribute = type.GetCustomAttribute(typeof(MetricDelegateAttribute)) as MetricDelegateAttribute;
                    names.Add(nameAttribute.Name);
                }

                return names;
            }
        }

        public static MetricDelegate GetMetric(string name)
        {
            MetricDelegate metric = null;
            foreach (Type type in GetMetricTypes())
            {
                MetricDelegateAttribute nameAttribute = type.GetCustomAttribute(typeof(MetricDelegateAttribute)) as MetricDelegateAttribute;
                if (nameAttribute.Name == name)
                {
                    metric = Activator.CreateInstance(type) as MetricDelegate;
                }
            }

            return metric;
        }

        public static List<Type> GetMetricTypes()
        {
            return getTypesWithAttribute(typeof(MetricDelegateAttribute));
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

    public class MetricDelegateAttribute : Attribute
    {
        public string Name { get; set; }
        public MetricType Type { get; set; }
        public TimeMeasureType Measurement { get; set; }
    }

    public abstract class MetricDelegate
    {
        public virtual Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public virtual string[] RequiredParameters { get; protected set; } = new string[0];

        public virtual object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null, string category = "", object categoryValue = null)
        {
            return data;
        }

        protected virtual bool hasRequiredParameters()
        {
            foreach (string str in RequiredParameters)
            {
                if (!Parameters.ContainsKey(str))
                    return false;
            }

            return true;
        }
    }

    [MetricDelegate(Name = "Incident Time", Type = MetricType.IncidentData, Measurement = TimeMeasureType.Instant)]
    public class IncidentTimeMetric : MetricDelegate
    {
        public override object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null, string category = "", object categoryValue = null)
        {
            Dictionary<object, object> output = new Dictionary<object, object>();

            foreach (KeyValuePair<object, List<IncidentData>> kvp in data)
            {
                List<double> minutesOfDay = new List<double>();

                foreach (IncidentData incident in kvp.Value)
                {
                    minutesOfDay.Add(incident.Time.Hour * 60 + incident.Time.Minute + (incident.Time.Second / 60));
                }

                if (calculation == null)
                {
                    output[kvp.Key] = minutesOfDay;
                }
                else
                {
                    output[kvp.Key] = calculation.Calculate(minutesOfDay);
                }
            }

            return output;
        }
    }

    [MetricDelegate(Name = "Dispatch Time", Type = MetricType.TimingData, Measurement = TimeMeasureType.Instant)]
    public class DispatchTimeMetric : MetricDelegate
    {
        public override object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null, string category = "", object categoryValue = null)
        {
            Dictionary<object, object> output = new Dictionary<object, object>();

            foreach (KeyValuePair<object, List<IncidentData>> kvp in data)
            {
                List<double> dispatchTimes = new List<double>();

                foreach (IncidentData incident in kvp.Value)
                {
                    foreach (ResponseData response in incident.Responses)
                    {
                        double dispatchTime = (from TimingData bmk in response.TimingData
                                               where bmk.Name == "Assigned"
                                               select bmk.Value).FirstOrDefault();

                        if (dispatchTime == default(double))
                            continue;

                        dispatchTimes.Add(dispatchTime);
                    }
                }

                if (calculation == null)
                {
                    output[kvp.Key] = dispatchTimes;
                }
                else
                {
                    output[kvp.Key] = calculation.Calculate(dispatchTimes);
                }
            }

            return output;
        }
    }

    [MetricDelegate(Name = "Turnout Time", Type = MetricType.TimingData, Measurement = TimeMeasureType.Instant)]
    public class TurnoutTimeMetric : MetricDelegate
    {
        public override object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null, string category = "", object categoryValue = null)
        {
            Dictionary<object, object> output = new Dictionary<object, object>();

            foreach (KeyValuePair<object, List<IncidentData>> kvp in data)
            {
                List<double> turnoutTimes = new List<double>();

                foreach (IncidentData incident in kvp.Value)
                {
                    foreach (ResponseData response in incident.Responses)
                    {
                        double turnoutTime = (from TimingData bmk in response.TimingData
                                              where bmk.Name == "TurnoutTime"
                                              select bmk.Value).FirstOrDefault();

                        if (turnoutTime == default)
                            continue;

                        turnoutTimes.Add(turnoutTime);
                    }
                }

                if (calculation == null)
                {
                    output[kvp.Key] = turnoutTimes;
                }
                else
                {
                    output[kvp.Key] = calculation.Calculate(turnoutTimes);
                }
            }

            return output;
        }
    }

    [MetricDelegate(Name = "Travel Time", Type = MetricType.TimingData, Measurement = TimeMeasureType.Duration)]
    public class TravelTimeMetric : MetricDelegate
    {
        public override object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null, string category = "", object categoryValue = null)
        {
            Dictionary<object, object> output = new Dictionary<object, object>();

            foreach (KeyValuePair<object, List<IncidentData>> kvp in data)
            {
                List<double> travelTimes = new List<double>();

                foreach (IncidentData incident in kvp.Value)
                {
                    foreach (ResponseData response in incident.Responses)
                    {
                        double sceneTime = (from TimingData bmk in response.TimingData
                                            where bmk.Name == "OnScene"
                                            select bmk.Value).FirstOrDefault();

                        if (sceneTime == default(double))
                            continue;

                        double turnoutTime = (from TimingData bmk in response.TimingData
                                              where bmk.Name == "TurnoutTime"
                                              select bmk.Value).FirstOrDefault();

                        double travelTime = sceneTime - turnoutTime;
                        travelTimes.Add(travelTime);
                    }
                }

                if (calculation == null)
                {
                    output[kvp.Key] = travelTimes;
                }
                else
                {
                    output[kvp.Key] = calculation.Calculate(travelTimes);
                }
            }

            return output;
        }
    }

    [MetricDelegate(Name = "Initial Response", Type = MetricType.TimingData, Measurement = TimeMeasureType.Instant)]
    public class InitialResponseTimeMetric : MetricDelegate
    {
        public override object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null, string category = "", object categoryValue = null)
        {
            Dictionary<object, object> output = new Dictionary<object, object>();

            foreach (KeyValuePair<object, List<IncidentData>> datum in data)
            {
                List<double> firstArrivals = new List<double>();

                foreach (IncidentData incident in datum.Value)
                {
                    double firstArrival = double.MaxValue;
                    foreach (ResponseData response in incident.Responses)
                    {
                        double firstArrivalBmk = (from TimingData bmk in response.TimingData
                                                  where bmk.Name == "FirstArrival"
                                                  select bmk.Value).FirstOrDefault();

                        if (firstArrivalBmk != default)
                        {
                            firstArrival = firstArrivalBmk;
                            break;
                        }

                        double onScene = (from TimingData bmk in response.TimingData
                                          where bmk.Name == "OnScene"
                                          select bmk.Value).FirstOrDefault();

                        if (onScene < firstArrival)
                        {
                            firstArrival = onScene;
                        }
                    }

                    if (firstArrival != double.MaxValue)
                    {
                        firstArrivals.Add(firstArrival);
                    }
                }

                if (firstArrivals.Count == 0)
                    continue;

                if (calculation == null)
                {
                    output[datum.Key] = firstArrivals;
                }
                else
                {
                    output[datum.Key] = calculation.Calculate(firstArrivals);
                }
            }

            return output;
        }
    }

    [MetricDelegate(Name = "Full Complement", Type = MetricType.TimingData, Measurement = TimeMeasureType.Instant)]
    public class FullComplementTimeMetric : MetricDelegate
    {
        public override object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null, string category = "", object categoryValue = null)
        {
            Dictionary<object, object> output = new Dictionary<object, object>();

            foreach (KeyValuePair<object, List<IncidentData>> datum in data)
            {
                List<double> fullComplements = new List<double>();

                foreach (IncidentData incident in datum.Value)
                {
                    double fullComplement = double.MinValue;
                    foreach (ResponseData response in incident.Responses)
                    {
                        double fullComplementBmk = (from TimingData bmk in response.TimingData
                                                    where bmk.Name == "FullComplement"
                                                    select bmk.Value).FirstOrDefault();

                        if (fullComplementBmk != default)
                        {
                            fullComplement = fullComplementBmk;
                            break;
                        }

                        double onScene = (from TimingData bmk in response.TimingData
                                          where bmk.Name == "OnScene"
                                          select bmk.Value).FirstOrDefault();

                        if (onScene > fullComplement)
                        {
                            fullComplement = onScene;
                        }
                    }

                    if (fullComplement != double.MinValue)
                    {
                        fullComplements.Add(fullComplement);
                    }
                }

                if (fullComplements.Count == 0)
                    continue;

                if (calculation == null)
                {
                    output[datum.Key] = fullComplements;
                }
                else
                {
                    output[datum.Key] = calculation.Calculate(fullComplements);
                }
            }

            return output;
        }
    }

    [MetricDelegate(Name = "Scene Time", Type = MetricType.TimingData, Measurement = TimeMeasureType.Duration)]
    public class SceneTimeMetric : MetricDelegate
    {
        public override object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null, string category = "", object categoryValue = null)
        {
            Dictionary<object, object> output = new Dictionary<object, object>();

            foreach (KeyValuePair<object, List<IncidentData>> kvp in data)
            {
                List<double> sceneTimes = new List<double>();

                foreach (IncidentData incident in kvp.Value)
                {
                    foreach (ResponseData response in incident.Responses)
                    {
                        double onScene = (from TimingData bmk in response.TimingData
                                          where bmk.Name == "OnScene"
                                          select bmk.Value).FirstOrDefault();

                        if (onScene == default)
                            continue;

                        double clearScene = (from TimingData bmk in response.TimingData
                                             where bmk.Name == "ClearScene"
                                             select bmk.Value).FirstOrDefault();

                        if (clearScene == default)
                            continue;

                        sceneTimes.Add(clearScene - onScene);
                    }
                }

                if (calculation == null)
                {
                    output[kvp.Key] = sceneTimes;
                }
                else
                {
                    output[kvp.Key] = calculation.Calculate(sceneTimes);
                }
            }

            return output;
        }
    }

    [MetricDelegate(Name = "Committed Time", Type = MetricType.TimingData, Measurement = TimeMeasureType.Duration)]
    public class CommittedTimeMetric : MetricDelegate
    {
        public override object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null, string category = "", object categoryValue = null)
        {
            Dictionary<object, object> output = new Dictionary<object, object>();

            foreach (KeyValuePair<object, List<IncidentData>> datum in data)
            {
                List<double> committedTime = new List<double>();

                foreach (IncidentData incident in datum.Value)
                {
                    foreach (ResponseData response in incident.Responses)
                    {
                        double committedHours = (from TimingData bmk in response.TimingData
                                                 where bmk.Name == "CommittedHours"
                                                 select bmk.Value).FirstOrDefault();

                        if (committedHours != default)
                        {
                            committedTime.Add(committedHours * 60.0);
                            continue;
                        }

                        double dispatched = (from TimingData bmk in response.TimingData
                                             where bmk.Name == "Assigned"
                                             select bmk.Value).FirstOrDefault();

                        if (dispatched == default)
                            continue;

                        double clearScene = (from TimingData bmk in response.TimingData
                                             where bmk.Name == "ClearScene"
                                             select bmk.Value).FirstOrDefault();

                        if (clearScene == default)
                            continue;

                        committedTime.Add(clearScene - dispatched);
                    }
                }

                if (committedTime.Count == 0)
                    continue;

                if (calculation == null)
                {
                    output[datum.Key] = committedTime;
                }
                else
                {
                    output[datum.Key] = calculation.Calculate(committedTime);
                }
            }

            return output;
        }
    }

    [MetricDelegate(Name = "Utilization", Type = MetricType.TimingData, Measurement = TimeMeasureType.Duration)]
    public class UtilizationMetric : MetricDelegate
    {
        public override string[] RequiredParameters { get; protected set; } = { "Start Date", "End Date" };

        public string getAggregationFromData(Dictionary<object, List<IncidentData>> data)
        {
            List<object> keys = data.Keys.ToList();
            if (keys.Count == 0)
            {
                return string.Empty;
            }
            else if (keys[0] is string)
            {
                bool isDoW = true;
                bool isMoY = true;
                // Check for Day of Week and Month of Year
                foreach (object key in keys)
                {
                    if (!DayOfWeekAggregator<IncidentData>.DayOfWeekKeys.Contains(key))
                    {
                        isDoW = false;
                    }

                    if (!MonthOfYearAggregator<IncidentData>.MonthOfYearKeys.Contains(key))
                    {
                        isMoY = false;
                    }
                }

                if (isDoW)
                {
                    return "DayOfWeek";
                }
                else if (isMoY)
                {
                    return "MonthOfYear";
                }

                // Check for Unit
                List<IncidentData> incidents = new List<IncidentData>();
                foreach (List<IncidentData> list in data.Values)
                {
                    incidents.AddRange(list);
                }

                HashSet<string> stringKeys = new HashSet<string>();
                foreach (object key in keys)
                {
                    stringKeys.Add(key as string);
                }

                HashSet<string> unitNames = IncidentDataTools.GetUnitsFromIncidents(incidents);
                bool isUnit = true;
                foreach (string unitName in unitNames)
                {
                    if (!stringKeys.Contains(unitName))
                    {
                        isUnit = false;
                    }
                }
                if (isUnit)
                {
                    return "Unit";
                }
            }
            else if (keys[0] is int)
            {
                // Check for Hour of Day
                if (keys.Count == 24)
                {
                    bool isHoD = true;
                    try
                    {
                        for (int i = 0; i < 24; i++)
                        {
                            isHoD = (int)keys[i] == i;
                        }
                    }
                    catch (Exception ex)
                    {
                        isHoD = false;
                    }

                    if (isHoD)
                    {
                        return "HourOfDay";
                    }
                }

                // Check for Year
                bool isYear = true;
                try
                {
                    foreach (object key in keys)
                    {
                        int value = (int)key;
                        if (value < 1950 || value > 2050) // If you are using this after 2050 I question the nature of all of reality
                        {
                            isYear = false;
                            break;
                        }
                    }

                    if (isYear)
                    {
                        return "Year";
                    }
                }
                catch (Exception ex)
                {

                }

                // Check for Month
                try
                {
                    bool isMonth = true;
                    foreach (object key in keys)
                    {
                        int value = (int)key;
                        int year, month;
                        year = DateTimeUtils.GetYearFromYMKey(value);
                        month = DateTimeUtils.GetMonthFromYMKey(value);
                        if (year < 1950 || year > 2050 || month <= 0 || month > 12)
                        {
                            isMonth = false;
                            break;
                        }
                    }

                    if (isMonth)
                    {
                        return "Month";
                    }
                } catch (Exception ex)
                {

                }

                // Check for Day
                try
                {
                    bool isDay = true;
                    foreach (object key in keys)
                    {
                        int value = (int)key;
                        int year, month, day;
                        year = DateTimeUtils.GetYearFromYMDKey(value);
                        month = DateTimeUtils.GetMonthFromYMDKey(value);
                        day = DateTimeUtils.GetDayFromYMDKey(value);
                        if (year < 1950 || year > 2050 || month <= 0 || month > 12 || day <= 0 || day > 31)
                        {
                            isDay = false;
                            break;
                        }
                    }

                    if (isDay)
                    {
                        return "Day";
                    }
                } catch (Exception ex)
                {

                }
            }

            return "Unknown";
        }

        public Periods GetAggregationPeriod(string aggregation)
        {
            switch (aggregation.Replace(" ", string.Empty).ToLower())
            {
                case "day":
                    return Periods.Day;
                case "month":
                    return Periods.Month;
                case "year":
                    return Periods.Year;
                case "dayofweek":
                    return Periods.DayOfWeek;
                case "hourofday":
                    return Periods.HourOfDay;
                case "monthofyear":
                    return Periods.MonthOfYear;
                //case "code":
                //case "type":
                //case "category":
                //    return Periods.PercentageOfTotal;
                default:
                    return Periods.TimeSpan;
            }
        }

        public int GetPeriodIntValue(string aggregation, object key)
        {
            if (key is int)
            {
                return (int)key;
            } else
            {
                switch(aggregation.Replace(" ", string.Empty).ToLower())
                {
                    case "dayofweek":
                        return DayOfWeekAggregator<IncidentData>.DayOfWeekKeys.IndexOf(key);
                    case "monthofyear":
                        return MonthOfYearAggregator<IncidentData>.MonthOfYearKeys.IndexOf(key) + 1;
                    default:
                        return 0;
                }
            }
        }

        public override object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null, string category = "", object categoryValue = null)
        {
            Dictionary<object, object> output = new Dictionary<object, object>();
            if (!hasRequiredParameters())
                return output;

            DateTime endDate;
            DateTime startDate;
            if (Parameters["End Date"] is DateTime)
            {
                endDate = (DateTime)Parameters["End Date"];
            }
            else if (!DateTime.TryParse(Parameters["End Date"].ToString(), out endDate))
            {
                return output;
            }

            if (Parameters["Start Date"] is DateTime)
            {
                startDate = (DateTime)Parameters["Start Date"];
            }
            else if (!DateTime.TryParse(Parameters["Start Date"].ToString(), out startDate))
            {
                return output;
            }

            string aggregation = getAggregationFromData(data);
            Periods aggregationPeriod = GetAggregationPeriod(aggregation);
            Periods categoryPeriod = GetAggregationPeriod(category);

            Dictionary<string, double> totalUnitUtilization = new Dictionary<string, double>();
            Dictionary<object, Dictionary<string, double>> utilizations = new Dictionary<object, Dictionary<string, double>>();
            foreach (KeyValuePair<object, List<IncidentData>> datum in data)
            {
                int aggregationPeriodInt = GetPeriodIntValue(aggregation, datum.Key);
                double aggregationMinutes = DateTimeUtils.GetPeriodMinutes(aggregationPeriod, startDate, endDate, aggregationPeriodInt);
                int categoryPeriodInt = GetPeriodIntValue(category, categoryValue);
                double categoryMinutes = DateTimeUtils.GetPeriodMinutes(categoryPeriod, startDate, endDate, categoryPeriodInt);


                Dictionary<string, double> unitUtilizationForAggregation = utilizations[datum.Key] = new Dictionary<string, double>();
                foreach (IncidentData incident in datum.Value)
                {
                    foreach (ResponseData response in incident.Responses)
                    {
                        if (!response.Data.ContainsKey("Unit"))
                        {
                            continue;
                        }

                        string unit = response.Data["Unit"].ToString();

                        Periods period = categoryPeriod;
                        int periodInt = categoryPeriodInt;
                        if (categoryPeriod == Periods.TimeSpan)
                        {
                            period = aggregationPeriod;
                            periodInt = aggregationPeriodInt;
                        }

                        double committedTime = getCommittedTimeFromResponse(response, incident.Time, period, periodInt);

                        if (!totalUnitUtilization.Keys.Contains(unit))
                        {
                            totalUnitUtilization.Add(unit, committedTime);
                        } else
                        {
                            totalUnitUtilization[unit] += committedTime;
                        }

                        if (!unitUtilizationForAggregation.Keys.Contains(unit))
                        {
                            unitUtilizationForAggregation.Add(unit, committedTime);
                        }
                        else
                        {
                            unitUtilizationForAggregation[unit] += committedTime;
                        }
                    }
                }
            }

            foreach (object key in data.Keys) {
                int aggregationPeriodInt = GetPeriodIntValue(aggregation, key);
                double aggregationMinutes = DateTimeUtils.GetPeriodMinutes(aggregationPeriod, startDate, endDate, aggregationPeriodInt);
                int categoryPeriodInt = GetPeriodIntValue(category, categoryValue);
                double categoryMinutes = DateTimeUtils.GetPeriodMinutes(categoryPeriod, startDate, endDate, categoryPeriodInt);

                Dictionary<string, double> unitUtilizationForAggregation = utilizations[key];

                if (aggregation == "Unit" || category == "Unit") {
                    double periodMinutes = categoryPeriod == Periods.TimeSpan ? aggregationMinutes : categoryMinutes;
                    if (unitUtilizationForAggregation.ContainsKey(key.ToString()))
                    {
                        output[key] = (unitUtilizationForAggregation[key.ToString()] / periodMinutes) * 100.0;
                    } else if (unitUtilizationForAggregation.ContainsKey(categoryValue as string))
                    {
                        output[key] = (unitUtilizationForAggregation[categoryValue as string] / periodMinutes) * 100.0;
                    }
                    else
                    {
                        output[key] = 0.0;
                    }
                }
                else if (aggregationPeriod == Periods.TimeSpan)
                {
                    double timespanMinutesByNumUnits = unitUtilizationForAggregation.Keys.Count * aggregationMinutes;
                    double totalCommittedTime = unitUtilizationForAggregation.Values.Sum();

                    output[key] = (totalCommittedTime / timespanMinutesByNumUnits) * 100.0;
                }
            }

            if (aggregation == "Unknown" || string.IsNullOrEmpty(category))
            {
                output = sortOutput(output);
            }
            return output;
        }

        private double getCommittedTimeFromResponse(ResponseData response, DateTime time, Periods period, int value)
        {
            double committedTime = 0.0;
            double dispatched = (from TimingData bmk in response.TimingData
                                 where bmk.Name == "Assigned"
                                 select bmk.Value).FirstOrDefault();

            double clearScene = (from TimingData bmk in response.TimingData
                                 where bmk.Name == "ClearScene"
                                 select bmk.Value).FirstOrDefault();

            double committedHours = (from TimingData bmk in response.TimingData
                                     where bmk.Name == "CommittedHours"
                                     select bmk.Value).FirstOrDefault();

            if (period == Periods.TimeSpan)
            {
                if (committedHours != default)
                {
                    committedTime = committedHours * 60.0;
                }
                else if (dispatched != default && clearScene != default)
                {
                    committedTime = clearScene - dispatched;
                }
            }
            else
            {
                DateTime assignedTime;
                if (dispatched != default)
                {
                    assignedTime = time.AddMinutes(dispatched);
                } else
                {
                    assignedTime = time;
                }
                  
                DateTime clearTime;
                if (clearScene == default)
                {
                    clearTime = time.AddHours(committedHours);
                } else
                {
                    clearTime = time.AddMinutes(clearScene);
                }
                    

                return DateTimeUtils.GetTimeSpentInPeriod(period, assignedTime, clearTime, value);
            }

            return committedTime;
        }

        private Dictionary<object, object> sortOutput(Dictionary<object, object> output)
        {
            List<KeyValuePair<object, object>> unsortedOutput = output.ToList();

            unsortedOutput.Sort(
                delegate (KeyValuePair<object, object> pair1,
                KeyValuePair<object, object> pair2)
                {
                    return Convert.ToDouble(pair1.Value).CompareTo(Convert.ToDouble(pair2.Value)) * -1;
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
}

