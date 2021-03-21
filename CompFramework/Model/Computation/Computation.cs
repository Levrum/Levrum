using AnalysisFramework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnalysisFramework.Model.Computation
{
    public class Computation : NamedObj
    {
        
    }


    /// <summary>
    /// This attribute is used for tagging methods for exposure via the "Computable" API.
    /// E.g.:
    ///     [Computable][Caption("Fast Fourier Transform")]
    ///     [Help(@"This computation applies the 'FFTW' algorithm to a series of X/Y points
    ///             and generates a series of the N most significant spectra magnitudes based on the nSpectra parameter.
    ///             For more info:  https://computation-help.levrum.com/levrum/MathComps/fftw")]
    ///     public List<Tuple<double,double>> ComputeFftw(List<Tuple<double,double>> oInputData, int nSpectra)
    ///     {
    ///         List<Tuple<double,double>> retlist = new List<Tuple<double,double>>();
    ///         // implementation left as an exercise for the reader...   ;^)
    ///         return(retlist);
    ///     }
    /// </summary>
    public class ConputableAttribute : Attribute
    {

    }


    public class ComputationResult
    {
        public object Data = null;
        public bool Succeeded = false;
        public List<string> Messages = new List<string>();
    }



    public class ComputationExamples
    {
        
        public static List<Tuple<double,double>> Bernoulli([NumericRange(1,20)] int nApprox)
        {
            List<Tuple<double, double>> retlist = new List<Tuple<double, double>>();

            Random r = new Random();
            for (double x=-1; x<=1; x += 0.02)
            {
                double y = 0.0;
                for (int i=0; i<nApprox; i++) { y += (r.Next() - 0.5) / nApprox; }
                retlist.Add(new Tuple<double, double>(x, y));

            }
            return (retlist);
        }
    }
}
