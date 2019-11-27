using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Levrum.Data.Aggregators
{
    public static class CalculationDelegates
    {
        static CalculationDelegates()
        {

        }

        public static List<string> Calculations
        {
            get
            {
                List<string> names = new List<string>();
                foreach (Type type in GetCalculationTypes())
                {
                    CalculationDelegateAttribute nameAttribute = type.GetCustomAttribute(typeof(CalculationDelegateAttribute)) as CalculationDelegateAttribute;
                    names.Add(nameAttribute.Name);
                }

                return names;
            }
        }

        public static CalculationDelegate GetCalculation(string name)
        {
            if (name.ToLower() == "none")
            {
                return null;
            }

            CalculationDelegate calculation = null;
            foreach (Type type in GetCalculationTypes())
            {
                CalculationDelegateAttribute nameAttribute = type.GetCustomAttribute(typeof(CalculationDelegateAttribute)) as CalculationDelegateAttribute;
                if (nameAttribute.Name == name)
                {
                    calculation = Activator.CreateInstance(type) as CalculationDelegate;
                }
            }

            return calculation;
        }

        public static List<Type> GetCalculationTypes()
        {
            return getTypesWithAttribute(typeof(CalculationDelegateAttribute));
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

    public class CalculationDelegateAttribute : Attribute
    {
        public string Name { get; set; }
    }

    public abstract class CalculationDelegate
    {
        public virtual Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        public virtual string[] RequiredParameters { get; protected set; } = new string[0];

        public CalculationDelegate()
        {

        }

        public CalculationDelegate(Dictionary<string, object> _parameters = null)
        {
            Parameters = _parameters;
        }

        public virtual double Calculate(List<object> data)
        {
            return data.Count;
        }

        public virtual double Calculate(List<int> data)
        {
            return Calculate(getObjectList(data));
        }

        public virtual double Calculate(List<long> data)
        {
            return Calculate(getObjectList(data));
        }

        public virtual double Calculate(List<float> data)
        {
            return Calculate(getObjectList(data));
        }

        public virtual double Calculate(List<double> data)
        {
            return Calculate(getObjectList(data));
        }

        protected virtual List<object> getObjectList<T>(List<T> data)
        {
            List<object> objs = new List<object>();
            foreach (T obj in data)
            {
                objs.Add(obj);
            }

            return objs;
        }
    }

    [CalculationDelegate(Name = "Count")]
    public class CountCalculation : CalculationDelegate
    {

    }

    [CalculationDelegate(Name = "None")]
    public class NoCalculation : CalculationDelegate
    {

    }

    [CalculationDelegate(Name = "Mean")]
    public class MeanCalculation : CalculationDelegate
    {
        public override double Calculate(List<object> data)
        {
            double sum = 0.0;
            int count = 0;
            foreach (object datum in data)
            {
                try
                {
                    sum += Convert.ToDouble(datum);
                    count++;
                } catch (Exception ex)
                {

                }
            }

            if (count == 0)
                return -1.0;

            return sum / count;
        }
    }

    [CalculationDelegate(Name = "Median")]
    public class MedianCalculation : CalculationDelegate
    {
        public override double Calculate(List<object> data)
        {
            if (data.Count == 0)
                return 0.0;


            List<double> doubles = new List<double>();
            foreach (object datum in data)
            {
                try
                {
                    doubles.Add(Convert.ToDouble(datum));
                }
                catch (Exception ex)
                {

                }
            }

            doubles.Sort();

            int index = (int)Math.Ceiling((doubles.Count - 1) / 2.0);

            try
            {
                return Convert.ToDouble(doubles[index]);
            } catch (Exception ex)
            {
                return -1.0;
            }
        }
    }

    [CalculationDelegate(Name = "Sum")]
    public class SumCalculation : CalculationDelegate
    {
        public override double Calculate(List<object> data)
        {
            double result = 0.0;
            foreach (object datum in data)
            {
                try
                {
                    result += Convert.ToDouble(datum);
                } catch (Exception ex)
                {

                }
            }

            return result;
        }
    }

    [CalculationDelegate(Name = "Percentile")]
    public class PercentileCalculation : CalculationDelegate
    {
        public override string[] RequiredParameters { get; protected set; } = { "Percentile" };

        public override double Calculate(List<object> data)
        {
            double percentile = 0.0;
            try
            {
                percentile = Convert.ToDouble((from KeyValuePair<string, object> kvp in Parameters
                                               where kvp.Key.ToLower() == "percentile"
                                               select kvp.Value).FirstOrDefault()) / 100.0;
            } catch (Exception ex)
            {
                return -1.0;
            }

            List<double> doubles = new List<double>();
            foreach (object datum in data)
            {
                try
                {
                    doubles.Add(Convert.ToDouble(datum));
                } catch (Exception ex)
                {

                }
            }

            doubles.Sort();

            int index = (int)Math.Ceiling((doubles.Count - 1) * percentile);

            try
            {
                return Convert.ToDouble(doubles[index]);
            } catch (Exception ex)
            {
                return -1.0;
            }
        }
    }

    [CalculationDelegate(Name="Maximum")]
    public class MaximumCalculation : CalculationDelegate
    {
        public override double Calculate(List<object> data)
        {
            double maxValue = 0.0;
            foreach (object obj in data)
            {
                try
                {
                    maxValue = Math.Max(maxValue, Convert.ToDouble(obj));
                } catch (Exception ex)
                {

                }
            }

            return maxValue;
        }
    }

    [CalculationDelegate(Name="Minimum")]
    public class MinimumCalculation: CalculationDelegate
    {
        public override double Calculate(List<object> data)
        {
            double minValue = double.MaxValue;
            foreach (object obj in data)
            {
                try
                {
                    minValue = Math.Min(minValue, Convert.ToDouble(obj));
                } catch (Exception ex)
                {

                }
            }

            if (minValue == double.MaxValue)
                return -1;

            return minValue;
        }
    }

    [CalculationDelegate(Name="Standard Deviation")]
    public class StandardDeviationCalculation : CalculationDelegate
    {
        public override double Calculate(List<object> data)
        {
            int count = 0;
            double sum = 0.0;
            double sumSquares = 0.0;
            foreach (object datum in data)
            {
                try
                {
                    double value = Convert.ToDouble(datum);
                    sum += value;
                    sumSquares += (value * value);
                    count++;
                } catch (Exception ex)
                {

                }
            }

            if (count == 1)
                return 0.0;

            double mean = sum / count;
            double var = (count * mean * mean) - (2 * mean * sum) + sumSquares;
            return Math.Sqrt(var / (count - 1));
        }
    }

    [CalculationDelegate(Name="Mean +/- SDs")]
    public class MeanPlusMinusSDsCalculation : CalculationDelegate
    {
        public override string[] RequiredParameters { get; protected set; } = { "Num SDs" };

        public override double Calculate(List<object> data)
        {
            double numSds = 0.0;
            try
            {
                numSds = Convert.ToDouble((from KeyValuePair<string, object> kvp in Parameters
                                               where kvp.Key.ToLower() == "num sds"
                                               select kvp.Value).FirstOrDefault()) / 100.0;
            }
            catch (Exception ex)
            {
                return double.NaN;
            }

            MeanCalculation meanCalc = new MeanCalculation();
            double mean = meanCalc.Calculate(data);
            if (mean == -1.0)
                return double.NaN;

            StandardDeviationCalculation sdCalc = new StandardDeviationCalculation();
            double sd = sdCalc.Calculate(data);

            return mean + (sd * numSds);
        }
    }
}
