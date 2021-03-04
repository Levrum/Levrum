using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Linq;


namespace Levrum.Utils.MonteCarlo
{
    public class EmpiricalDistribution<T> : MonteCarloDistributionBase<T>
    {

        private Dictionary<T, int> _counts;
        private SortedList<double, T> Distribution;

        public override T GetValue(double rngVal)
        {
            T retVal = default;
            if (IsReady())
            {
                rngVal = Math.Min(1, Math.Max(rngVal, 0)); //constrains to 0 and 1
                for (int i = 0; i < Distribution.Count; i++)
                {
                    double key = Distribution.Keys[i];
                    if (rngVal <= key)
                    {
                        retVal = Distribution.Values[i];
                        break;
                    }
                }
            }
            return retVal;

        }

        public override bool IsReady()
        {
            return (Distribution == null);
        }

        public void CreateDistribution(List<object> data, Func<object, T> GetData)
        {
            Dictionary<T, int> counts = new Dictionary<T, int>();
            foreach (object odatum in data) 
            {
                T datum;
                try
                {
                    datum = GetData(data);
                }catch(Exception e)
                {
                    continue;
                }
                if (!counts.ContainsKey(datum))
                {
                    counts.Add(datum, 0);
                }
                counts[datum]++;
            }
            CreateDistribution(counts);

        }

        public void CreateDistribution(List<T> data)
        {
            Dictionary<T, int> counts = new Dictionary<T, int>();
            foreach (T datum in data)
            {
                if (!counts.ContainsKey(datum))
                {
                    counts.Add(datum, 0);
                }
                counts[datum]++;
            }
            CreateDistribution(counts);
        }

        public void CreateDistribution(Dictionary<T, int> DataCounts)
        {
            _counts = DataCounts;
            int dataSum = _counts.Values.Sum();
            var orderedCounts = _counts.OrderBy(x => x.Value);
            Distribution = new SortedList<double, T>(_counts.Count);
            double sum = 0;
            foreach(var kvp in orderedCounts)
            {
                double probability = kvp.Value / dataSum;
                sum += probability;
                Distribution.Add(sum, kvp.Key);
            }
        }
    }
}
