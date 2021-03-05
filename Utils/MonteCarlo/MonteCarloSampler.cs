using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.Utils.MonteCarlo
{
    public class MonteCarloSampler<T> : MonteCarloSamplerBase<T>
    {
        Random rng { get; set; }
        public MonteCarloSampler(MonteCarloDistributionBase<T> distribution)
        {
            this.Distribution = distribution;
            rng = new Random();
        }
        public override T SampleNext()
        {
            double sampleValue = rng.NextDouble();
            T toReturn = this.Distribution.GetValue(sampleValue);
            return toReturn;
        }

        public static MonteCarloSampler<T> CreateEmpiricalSampler(Dictionary<T, int> DataCounts)
        {
            EmpiricalDistribution<T> dist = new EmpiricalDistribution<T>();
            dist.CreateDistribution(DataCounts);
            MonteCarloSampler<T> sampler = null;
            if (dist.IsReady())
            {
                sampler = new MonteCarloSampler<T>(dist);
            }
            return sampler;
        }
    }
}
