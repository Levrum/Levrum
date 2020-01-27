using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Levrum.Data.Classes;

namespace Levrum.Data.Aggregators
{
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
    }

    public abstract class MetricDelegate
    {
        public virtual Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public virtual string[] RequiredParameters { get; protected set; } = new string[0];

        public virtual object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null)
        {
            return data;
        }

        protected virtual bool hasRequiredParameters()
        {
            foreach(string str in RequiredParameters)
            {
                if (!Parameters.ContainsKey(str))
                    return false;
            }

            return true;
        }
    }

    [MetricDelegate(Name="Incident Time")]
    public class IncidentTimeMetric : MetricDelegate
    {
        public override object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null)
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

    [MetricDelegate(Name="Dispatch Time")]
    public class DispatchTimeMetric : MetricDelegate
    {
        public override object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null)
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
                } else
                {
                    output[kvp.Key] = calculation.Calculate(dispatchTimes);
                }
            }

            return output;
        }
    }

    [MetricDelegate(Name="Turnout Time")]
    public class TurnoutTimeMetric : MetricDelegate
    {
        public override object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null)
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

    [MetricDelegate(Name="Travel Time")]
    public class TravelTimeMetric : MetricDelegate
    {
        public override object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null)
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

    [MetricDelegate(Name="Initial Response")]
    public class InitialResponseTimeMetric : MetricDelegate
    {
        public override object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null)
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

    [MetricDelegate(Name="Full Complement")]
    public class FullComplementTimeMetric : MetricDelegate
    {
        public override object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null)
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

    [MetricDelegate(Name="Scene Time")]
    public class SceneTimeMetric : MetricDelegate
    {
        public override object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null)
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

    [MetricDelegate(Name="Committed Time")]
    public class CommittedTimeMetric : MetricDelegate
    {
        public override object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null)
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

    [MetricDelegate(Name="Utilization")]
    public class UtilizationMetric : MetricDelegate
    {
        public override string[] RequiredParameters { get; protected set; } = { "Start Date", "End Date" };

        public override object Calculate(Dictionary<object, List<IncidentData>> data, CalculationDelegate calculation = null)
        {
            Dictionary<object, object> output = new Dictionary<object, object>();
            if (!hasRequiredParameters())
                return output;

            DateTime endDate;
            DateTime startDate;
            if (Parameters["End Date"] is DateTime)
            {
                endDate = (DateTime)Parameters["End Date"];
            } else if (!DateTime.TryParse(Parameters["End Date"].ToString(), out endDate))
            {
                return output;
            }

            if (Parameters["Start Date"] is DateTime)
            {
                startDate = (DateTime)Parameters["Start Date"];
            } else if (!DateTime.TryParse(Parameters["Start Date"].ToString(), out startDate))
            {
                return output;
            }

            double timespanMinutes = (endDate - startDate).TotalMinutes;
            foreach (KeyValuePair<object, List<IncidentData>> datum in data)
            {
                Dictionary<string, double> utilizationByUnit = new Dictionary<string, double>();

                foreach (IncidentData incident in datum.Value)
                {
                    foreach (ResponseData response in incident.Responses)
                    {
                        if (!response.Data.ContainsKey("Unit"))
                        {
                            continue;
                        }

                        string unit = response.Data["Unit"].ToString();

                        double committedTime = 0.0;

                        double committedHours = (from TimingData bmk in response.TimingData
                                                 where bmk.Name == "CommittedHours"
                                                 select bmk.Value).FirstOrDefault();

                        if (committedHours != default)
                        {
                            committedTime = committedHours * 60.0;
                        }
                        else
                        {
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

                            committedTime = clearScene - dispatched;
                        }

                        if (!utilizationByUnit.Keys.Contains(unit))
                        {
                            utilizationByUnit.Add(unit, committedTime);
                        }
                        else
                        {
                            utilizationByUnit[unit] += committedTime;
                        }
                    }
                }

                if (utilizationByUnit.ContainsKey(datum.Key.ToString()))
                {
                    output[datum.Key] = (utilizationByUnit[datum.Key.ToString()] / timespanMinutes) * 100.0;
                }
                else
                {
                    double timespanMinutesByNumUnits = utilizationByUnit.Keys.Count * timespanMinutes;
                    double totalCommittedTime = utilizationByUnit.Values.Sum();

                    output[datum.Key] = (totalCommittedTime / timespanMinutesByNumUnits) * 100.0;
                }
            }

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

