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
        private List<Tuple<double, T>> _distribution;

        public Dictionary<T, int> Counts { get
            {
                return _counts;
            } 
        }

        public List<Tuple<double, T>> Distribution
        {
            get
            {
                return _distribution;
            }
        }

        public override T GetValue(double rngVal)
        {
            T retVal = default;
            if (IsReady())
            {
                rngVal = Math.Min(1, Math.Max(rngVal, 0)); //constrains to 0 and 1
                for (int i = 0; i < _distribution.Count; i++)
                {
                    double probability = _distribution[i].Item1;
                    if (rngVal <= probability)
                    {
                        retVal = _distribution[i].Item2;
                        break;
                    }
                }
            }
            return retVal;

        }

        public override bool IsReady()
        {
            return (_distribution != null);
        }

        public void CreateDistribution(List<object> data, Func<object, T> GetData)
        {
            Dictionary<T, int> counts = new Dictionary<T, int>();
            foreach (object odatum in data) 
            {
                T datum;
                try
                {
                    datum = GetData(odatum);
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
            _distribution = new List<Tuple<double, T>>(_counts.Count);
            double sum = 0;
            foreach(var kvp in orderedCounts)
            {
                double probability = (double)kvp.Value / dataSum;
                sum += probability;
                _distribution.Add(new Tuple<double,T>(sum, kvp.Key));
            }
        }
    }
}
