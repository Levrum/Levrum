using System;
using System.Collections.Generic;
using System.Text;


namespace Levrum.Utils.MonteCarlo
{
    public abstract class MonteCarloDistributionBase<T>
    {
        public abstract bool IsReady();
        public abstract T GetValue(double value);
    }
}
