using System;
using System.Collections.Generic;
using System.Text;


namespace Levrum.Utils.MonteCarlo
{
    public abstract class MonteCarloSamplerBase<T>
    {
        public MonteCarloDistributionBase<T> Distribution { get; set; }
        public abstract T SampleNext();

    }


}
