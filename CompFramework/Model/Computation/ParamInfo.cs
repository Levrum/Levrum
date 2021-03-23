using AnalysisFramework.Infrastructure;
using Levrum.Utils.Infra;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnalysisFramework.Model.Computation
{

    /// <summary>
    /// Information about a single parameter of a computation
    /// </summary>
    [Caption("Computation Parameter")]
    public class ParamInfo : NamedObj
    {
        public ParamInfo(Type oParamType, string sName, string sComments = "")
        {
            Name = sName;
            Desc = sComments;
            ParameterType = oParamType;
        }

        public Type ParameterType = typeof(object);

        /// <summary>
        /// Current bound parameter value.
        /// </summary>
        public object Value = null;


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
        public ParameterError(ParamInfo oPInfo, string sMsg)
        {
            ErrorMessage = sMsg;
            ParamInfo = oPInfo;
        }
        public string ErrorMessage = "";
        public ParamInfo ParamInfo = null;
    }
}
