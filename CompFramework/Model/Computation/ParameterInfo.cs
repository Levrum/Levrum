using AnalysisFramework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnalysisFramework.Model.Computation
{

    /// <summary>
    /// Information about a single parameter of a computation
    /// </summary>
    public class ParameterInfo : NamedObj
    {

        public Type ParameterType = typeof(object);
    }


    public class NumericRangeAttribute: Attribute
    {
        public NumericRangeAttribute(double oArg1, double oArg2)
        {
            Arg1 = oArg1;
            Arg2 = oArg2;
        }

        public double Arg1;
        public double Arg2;

        public bool IsValid(double oValue)
        {
            return (false);
        }

    }

    public class ParameterError
    {
        public ParameterError(ParameterInfo oPInfo, string sMsg)
        {
            ErrorMessage = sMsg;
            ParamInfo = oPInfo;
        }
        public string ErrorMessage = "";
        public ParameterInfo ParamInfo = null;
    }
}
