using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using Levrum.Utils;

namespace Levrum.Utils.MathAndStats
{

    /// <summary>
    /// Placeholder class
    /// </summary>
    public static class TypeExtensionMethods
    {
        /// <summary>
        /// Is this type a scalar?
        /// </summary>
        /// <param name="oType"></param>
        /// <returns></returns>
        public static bool IsScalarType(this Type oType)
        {
            if (null == oType) { return (false); }
            if (typeof(int).IsAssignableFrom(oType)) { return (true); }
            if (typeof(double).IsAssignableFrom(oType)) { return (true); }
            if (typeof(Decimal).IsAssignableFrom(oType)) { return (true); }
            if (typeof(String).IsAssignableFrom(oType)) { return (true); }
            if (typeof(Enum).IsAssignableFrom(oType)) { return (true); }
            if (typeof(DateTime).IsAssignableFrom(oType)) { return (true); }
            return (false);
        }

    }

    /// <summary>
    /// General form of something capable of reporting summary statistics
    /// </summary>
    public interface IStats
    {
        int Count { get; }
        double Max { get; }
        double Min { get; }
        double Mean { get; }
        double StdDev { get; }
        double Total { get; }
        void Clear();
    }

    public enum SummaryStatisticsTypes
    {
        Count, Average, StdDev, Minimum, Maximum, Range, Total, Percentile
    }


    /// <summary>
    /// Simple, efficient class for maintaining descriptive stats
    /// (N, mean, SD, min, max) incrementally.
    /// </summary>
    public class Stats : IStats
    {



        /// <summary>
        /// Calculates "P choose N," the statistical utility, defined as 
        /// P! / ( N! x (P-N)! ).   AKA "binomial coefficient."
        /// </summary>
        /// <param name="P"></param>
        /// <param name="N"></param>
        /// <returns></returns>
        public static double PChooseN(int P, int N)
        {
            String fn = MethodBase.GetCurrentMethod().Name;


            try
            {
                if ((N < 0) || (N > P)) return (0.0);
                if ((0 == N) || (P == N)) return (1.0);
                int plessn = P - N;
                int cnt = (N > plessn) ? plessn : N;
                if (1 == cnt) return ((double)P);
                double numerator = 1.0;
                double denominator = 1.0;

                for (int i = 1; i <= cnt; i++)
                {
                    denominator *= i;
                    int numterm = i + (P - cnt);
                    numerator *= numterm;
                }

                if (0.0 == denominator) return (-2.0);
                return (numerator / denominator);
            }

            catch (Exception exc)
            {
                LogHelper.HandleExc(typeof(Stats), fn, exc);
                return (-1.0);
            }
        }


        /// <summary>
        /// Given a "practically infinite" (see below) population, of which R proportion
        /// exhibit a characteristic (are "positive" for that characteristic), estimate
        /// the probability that a sample of size S has exactly N positive members.  A 
        /// population is "practically infinite" relative to a sample if removing the 
        /// sample, however chosen, has negligible effect on the probability of choosing
        /// a positive element at the next choice.
        /// </summary>
        /// <param name="dPopHitRate"></param>
        /// <param name="iSampleSize"></param>
        /// <param name="iNumHits"></param>
        /// <returns>double:  [0.0,1.0], representing the probability described above.  
        ///    Less than zero on error.</returns>
        public static double ProbNPositives(double dR, int iS, int iN)
        {
            String fn = MethodBase.GetCurrentMethod().Name;

            try
            {
                if (dR > 1.0)
                {
                    LogHelper.HandleAppErr(typeof(Stats), fn, "Hit ratio > 1.0: " + dR);
                    return (-2.0);
                }
                if (dR < 0.0)
                {
                    LogHelper.HandleAppErr(typeof(Stats), fn, "Hit ratio < 0.0: " + dR);
                    return (-3.0);
                }
                if (iN > iS)
                {
                    LogHelper.HandleAppErr(typeof(Stats), fn, "Hit count greater than sample size: " + iN + ", " + iS);
                    return (-4.0);
                }

                double multiplier = PChooseN(iS, iN);
                double probhits = Math.Pow(dR, iN);
                double probmisses = Math.Pow((1.0 - dR), iS - iN);
                double danswer = multiplier * probhits * probmisses;

                return (danswer);

            }

            catch (Exception exc)
            {
                LogHelper.HandleExc(typeof(Stats), fn, exc);
                return (-1.0);
            }
        }  // end ProbNPositives


        /// <summary>
        /// Given a "practically infinite" population, of which (R/100)% are "positive"
        /// for a certain characteristic, estimate the probability that N or fewer of
        /// a random sample of size S are positive for the same characteristic.  A 
        /// population is "practically infinite" relative to a sample if removing the 
        /// sample, however chosen, has negligible effect on the probability of choosing
        /// a positive element at the next choice.
        /// </summary>
        /// <param name="dR"></param>
        /// <param name="iS"></param>
        /// <param name="iN"></param>
        /// <returns></returns>
        public static double ProbNOrFewerPos(double dR, int iS, int iN)
        {
            String fn = MethodBase.GetCurrentMethod().Name;

            try
            {
                if (dR > 1.0)
                {
                    LogHelper.HandleAppErr(typeof(Stats), fn, "Hit ratio > 1.0: " + dR);
                    return (-2.0);
                }
                if (dR < 0.0)
                {
                    LogHelper.HandleAppErr(typeof(Stats), fn, "Hit ratio < 0.0: " + dR);
                    return (-3.0);
                }
                if (iN > iS)
                {
                    LogHelper.HandleAppErr(typeof(Stats), fn, "Hit count greater than sample size: " + iN + ", " + iS);
                    return (-4.0);
                }

                double dcumprob = 0.0;
                for (int j = 0; j <= iN; j++)
                {
                    dcumprob += ProbNPositives(dR, iS, j);
                }


                return (dcumprob);

            }

            catch (Exception exc)
            {
                LogHelper.HandleExc(typeof(Stats), fn, exc);
                return (-1.0);
            }
        }

        /// <summary>
        /// Given a sample size, and number of hits within that sample size,
        /// find the "practically infinite" population background hit rate
        /// for which the probability of the number of hits within the sample
        /// size is as specified, to 3 decimal places.
        /// </summary>
        /// <param name="dProb"></param>
        /// <param name="iSampSize"></param>
        /// <param name="iSampHits"></param>
        /// <returns></returns>
        public static double FindPopHitRateAtProbForSample(double dProb, int iSampSize, int iSampHits)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                double lowx = 0.0000001;
                double highx = 1.0 - lowx;
                double epsilon = 0.001;

                while (Math.Abs((highx - lowx) / ((highx + lowx) / 2)) > epsilon)
                {
                    double curx = (highx + lowx) / 2.0;
                    double cury = ProbNOrFewerPos(curx, iSampSize, iSampHits);
                    if (cury < dProb) highx = curx;
                    else if (cury > dProb) lowx = curx;
                    else highx = lowx = curx;
                }
                return ((highx + lowx) / 2.0);
            }
            catch (Exception exc)
            {
                LogHelper.HandleExc(typeof(Stats), fn, exc);
                return (-1.0);
            }
        }

        /// <summary>
        /// Add a single observation.
        /// </summary>
        /// <param name="dObs"></param>
        public virtual void AddObs(Double dObs)
        {
            m_iCount++;
            m_dSum += dObs;
            m_dSumSq += (dObs * dObs);
            if (dObs < m_dMin) m_dMin = dObs;
            if (dObs > m_dMax) m_dMax = dObs;
        }

        public virtual bool AddStats(Stats oOperand)
        {
            m_iCount += oOperand.m_iCount;
            m_dSum += oOperand.m_dSum;
            m_dSumSq += oOperand.m_dSumSq;
            if (oOperand.m_dMin < m_dMin) { m_dMin = oOperand.m_dMin; }
            if (oOperand.m_dMax > m_dMax) { m_dMax = oOperand.m_dMax; }
            return (true);
        }



        /// <summary>
        /// Remove an observation from the statistic.   Note: after this method is called,
        /// Max and Min will no longer be valid.
        /// </summary>
        /// <param name="dObs"></param>
        public virtual void RemoveObs(Double dObs)
        {
            if (m_iCount <= 0) throw (new InvalidOperationException("Attempted to remove an observation from an empty statistics collection"));
            m_iCount--;
            m_dSum -= dObs;
            m_dSumSq -= (dObs * dObs);
            m_dMin = Double.NaN;
            m_dMax = Double.NaN;
        }


        /// <summary>
        /// Clear the statistics back to all zeroes.
        /// </summary>
        public virtual void Clear()
        {
            m_iCount = 0;
            m_dSum = 0.0;
            m_dSumSq = 0.0;
            m_dMin = Double.MaxValue;
            m_dMax = Double.MinValue;
        }


        /// <summary>
        /// Calculate the the (directional) number of standard deviations from the mean for a particular value.
        /// E.g.:  mu=10.0, sigma=1.5, dValue=7.0 ==> this function returns -2.0.
        /// </summary>
        /// <param name="dValue"></param>
        /// <returns></returns>
        public virtual double StdDevsFromMean(double dValue)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                double diff = dValue - this.Mean;
                if (0.0 == StdDev) return (0.0);
                return (diff / StdDev);
            }
            catch (Exception exc)
            {
                LogHelper.HandleExc(this, fn, exc);
                return (0.0);
            }
        }


        /// <summary>
        /// Count of observations.
        /// </summary>
        public virtual int Count
        {
            get { return (m_iCount); }
        }


        /// <summary>
        /// Total of all observations.
        /// </summary>
        public virtual double Total
        {
            get { return (m_dSum); }
        }


        /// <summary>
        /// Sum of squares of all observations.
        /// </summary>
        public virtual double SumOfSquares
        {
            get { return (m_dSumSq); }
        }

        /// <summary>
        /// Estimate of mean.
        /// </summary>
        public virtual double Mean
        {
            get
            {
                if (0 == m_iCount) return (0.0);
                return (m_dSum / m_iCount);
            }
        }

        /// <summary>
        /// Estimate of standard deviation.
        /// </summary>
        public virtual double StdDev
        {
            get
            {
                if (Count <= 1) return (0.0);
                double xbar = Mean;
                double var = (Count * xbar * xbar)
                        - (2 * xbar * m_dSum)
                        + m_dSumSq;
                double stdev = Math.Sqrt(var / (Count - 1));
                return (stdev);
            }
        }


        /// <summary>
        /// Minimum value seen so far.
        /// </summary>
        public virtual double Min
        {
            get
            {
                return (m_dMin);
            }
        }

        /// <summary>
        /// Maximum value seen so far.
        /// </summary>
        public virtual double Max
        {
            get
            {
                return (m_dMax);
            }
        }



        public virtual double GetStatistic(double dOptionalArg1, SummaryStatisticsTypes qStatistic)
        {
            try
            {
                if (SummaryStatisticsTypes.Count == qStatistic) { return (this.Count); }
                else if (SummaryStatisticsTypes.Average == qStatistic) { return (this.Mean); }
                else if (SummaryStatisticsTypes.Maximum == qStatistic) { return (this.Max); }
                else if (SummaryStatisticsTypes.Minimum == qStatistic) { return (this.Min); }
                else if (SummaryStatisticsTypes.Range == qStatistic) { return (this.Max - this.Min); }
                else if (SummaryStatisticsTypes.StdDev == qStatistic) { return (this.StdDev); }
                else if (SummaryStatisticsTypes.Total == qStatistic) { return (this.Total); }
                else
                {
                    string fn = MethodBase.GetCurrentMethod().Name;
                    LogHelper.HandleAppErr(this, fn, "Stats class does not support statistic: " + qStatistic.ToString());
                    return (0);
                }
            }
            catch (Exception exc)
            {
                string fn = MethodBase.GetCurrentMethod().Name;
                LogHelper.HandleExc(this, fn, exc);
                return (0);
            }
        }


        private int m_iCount = 0;
        private double m_dSum = 0.0;
        private double m_dSumSq = 0.0;
        private double m_dMin = double.MaxValue;
        private double m_dMax = double.MinValue;
    }



    /// <summary>
    /// A statistics class capable of providing summary statistics or distribution statistics (e.g., median, percentile).
    /// </summary>
    public class OrderedStats : Stats
    {

        /// <summary>
        /// Get the median of the collection.
        /// </summary>
        /// <returns></returns>
        public virtual double GetMedian()
        {
            return (GetPercentile(50.0));
        }

        /// <summary>
        /// Get a specified percentile.   (Percentile is specified as a double in [0.0,100.0]).
        /// </summary>
        /// <param name="dPercentile"></param>
        /// <returns></returns>
        public virtual double GetPercentile(double dPercentile)
        {
            try
            {
                if (dPercentile > 100.0) { dPercentile = 100.0; }
                if (dPercentile < 0.0) { dPercentile = 0.0; }

                int count_below = 0;
                int tot_count = this.Count;

                double target_n = (dPercentile / 100.0) * tot_count;
                double prevval = double.NaN;

                foreach (double dcurval in m_oFrequencies.Keys)
                {
                    int curcount = m_oFrequencies[dcurval];
                    int next_count_below = curcount + count_below;
                    if (next_count_below > target_n)
                    {
                        if (double.IsNaN(prevval)) { return (dcurval); }
                        double dratio = (target_n - ((double)count_below)) /
                            (((double)next_count_below) - ((double)count_below));
                        double answer = prevval + (dratio * (dcurval - prevval));
                        return (answer);
                    }
                    count_below += curcount;
                    prevval = dcurval;
                }

                return (prevval);       // If we get to the end, we return the last one in the collection.
            }
            catch (Exception exc)
            {
                string fn = MethodBase.GetCurrentMethod().Name;
                LogHelper.HandleExc(this, fn, exc);
                return (0.0);
            }

        }


        public override double GetStatistic(double dParam, SummaryStatisticsTypes qStat)
        {
            if (SummaryStatisticsTypes.Percentile == qStat)
            {
                return (GetPercentile(dParam));
            }
            else { return (base.GetStatistic(dParam, qStat)); }
        }

        /// <summary>
        /// Add a statistics collection.   If the collection to be added is NOT an OrderedStats, an error message will be 
        /// generated, and the operation will complete -- however, while summary stats (mean, etc) will be OK,
        /// distribution statistics (median, percentiles) will NOT be accurate.   
        /// </summary>
        /// <param name="oOperand"></param>
        /// <returns></returns>
        public override bool AddStats(Stats oOperand)
        {
            try
            {
                if (base.AddStats(oOperand)) { return (false); }

                OrderedStats os = oOperand as OrderedStats;
                if (null == os)
                {
                    string fn = MethodBase.GetCurrentMethod().Name;
                    LogHelper.HandleAppErr(this, fn, "Attempting to add non-OrderedStats");
                }

                foreach (double dval in os.m_oFrequencies.Values)
                {
                    int n = m_oFrequencies[dval];
                    this.AddObs(dval, n);
                }
                return (true);
            }
            catch (Exception exc)
            {
                string fn = MethodBase.GetCurrentMethod().Name;
                LogHelper.HandleExc(this, fn, exc);
                return (false);
            }

        }

        /// <summary>
        /// Remove a single observation.
        /// </summary>
        /// <param name="dObs"></param>
        public override void RemoveObs(double dObs)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                base.RemoveObs(dObs);
                if (!m_oFrequencies.ContainsKey(dObs))
                {
                    LogHelper.HandleAppErr(this, fn, "Observation " + dObs + " cannot be removed, because it is not present");
                    return;
                }
                m_oFrequencies[dObs]--;
            }
            catch (Exception exc)
            {
                LogHelper.HandleExc(this, fn, exc);
            }

        }


        /// <summary>
        /// Clear the stats.
        /// </summary>
        public override void Clear()
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                base.Clear();
                m_oFrequencies.Clear();
            }
            catch (Exception exc)
            {
                LogHelper.HandleExc(this, fn, exc);
            }
        }

        public override void AddObs(double dObs)
        {
            AddObs(dObs, 1);
        }

        public virtual void AddObs(double dObs, int iRepeatCount)
        {
            try
            {
                if (!m_oFrequencies.ContainsKey(dObs)) { m_oFrequencies.Add(dObs, 0); }
                m_oFrequencies[dObs] += iRepeatCount;
                base.AddObs(dObs);
            }
            catch (Exception exc)
            {
                string fn = MethodBase.GetCurrentMethod().Name;
                LogHelper.HandleExc(this, fn, exc);
            }
        }


        private SortedDictionary<double, int> m_oFrequencies = new SortedDictionary<double, int>();
    } // end class

}
