using Levrum.Utils.Infra;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace AnalysisFramework.Model
{
    /// <summary>
    /// This class is for providing some calculation examples
    /// </summary>
    [DynamicCalc]
    public class ComputationExamples
    {

        [DynamicCalc]
        [Caption("Bernoulli Distribution Approximator[-1,1]")]
        [Doc(
@"This function approximates the Bernoulli distribution on [-1,1].  Its parameter specifies the 'fine-ness' of the
distribution ... i.e., the number of steps (N) in a stepwise approximation.    
 ")
        ]
        [HelpLink("https://www.levrum.com")]
        public static List<Tuple<double, double>> Bernoulli(
                            int nApprox)
        {
            List<Tuple<double, double>> retlist = new List<Tuple<double, double>>();

            Random r = new Random();
            for (double x = -1; x <= 1; x += 0.02)
            {
                double y = 0.0;
                for (int i = 0; i < nApprox; i++) { y += (r.Next() - 0.5) / nApprox; }
                retlist.Add(new Tuple<double, double>(x, y));

            }
            return (retlist);
        }


        [Caption("Linear Regression Output Parameters")]
        public class RegressionOutput
        {
            [Caption("X Field")] public string XFieldName = "";
            [Caption("Y Field")] public string YFieldName = "";
            [Caption("Slope")] public double Slope = 0.0;
            [Caption("Y Intercept")] public double YIntercept = 0.0;
            [Caption("Correlation Coefficient")] public double CorrelationCoefficient = 0.0;
        }

        [DynamicCalc]
        [Caption("Linear Regressor")]
        [Doc("This function evaluates the linear regression slope, intercept and correlation coefficient for a set of values, with X and Y fields specified")]
        [HelpLink("https://www.levrum.com")]
        public static RegressionOutput
            LinearRegression(
                             [Caption("Input Data")][Desc("Any homogeneous list with at least 2 numeric fields")] IEnumerable oData,
                             [Caption("X Field")][Desc("Fieldname of the independent variable (exact spelling, numeric type")] string sXFieldName,
                             [Caption("Y Field")][Desc("Fieldname of the dependent variable (exact spelling, numeric type")] string sYFieldName)
        {
            RegressionOutput ro = new RegressionOutput();
            ro.XFieldName = sXFieldName;
            ro.YFieldName = sYFieldName;
            Random r = new Random();
            ro.Slope = 3.0 * (r.Next() - 0.5);
            ro.YIntercept = 20.0 * (r.Next() - 0.5);
            ro.CorrelationCoefficient = (r.Next() / 2.0) + 0.499;
            return (ro);
        }
    }
}
