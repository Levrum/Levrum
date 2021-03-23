using AnalysisFramework.Infrastructure;
using Levrum.Utils.Infra;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnalysisFramework.Model.Computation
{
    
    /// <summary>
    /// This class encapsulates a single computation, including result type, 
    /// </summary>
    public abstract class Computation : NamedObj
    {


        public List<ParamInfo> FormalParameters = new List<ParamInfo>();

        public List<ParamInfo> ActualParameters = new List<ParamInfo>();

        public Type ResultType = null;

        public abstract ComputationResult Eval();


        
        public virtual List<StatusInfo> ValidateParams(bool bRequireRuntimeParams = true)
        {
            const string fn = "Computation.ValidateParams()";
            return (StatusInfo.MakeError(fn, "Not implemented yet"));
        }

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

        public static ComputationResult Exception(object oSubject, object oContext, Exception oExc)
        {
            Util.HandleExc(oSubject, oContext, oExc);
            ComputationResult cr = new ComputationResult();
            cr.Succeeded = false;
            cr.Messages.Add("Exception in " + oContext.ToString() + ": " + oExc);
            return (cr);
        }

        public static ComputationResult NotImplementedYet(string sContext)
        {
            Util.HandleAppWarningOnce(sContext, sContext, "Is not yet fully implemented");
            ComputationResult cr = new ComputationResult();
            cr.Succeeded = false;
            cr.Messages.Add(sContext + " is not yet fully implemented");
            return (cr);
        }
    }



}
