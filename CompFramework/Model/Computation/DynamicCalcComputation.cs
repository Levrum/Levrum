using Levrum.Utils.Infra;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AnalysisFramework.Model.Computation
{
    /// <summary>
    /// Wrapper for DynamicCalcs within the Computation context.
    /// </summary>
    public class DynamicCalcComputation : Computation
    {
        public override ComputationResult Eval()
        {
            const string fn = "DynamicCalcComputation.Eval()";
            try
            {
                return (ComputationResult.NotImplementedYet(fn));
            }
            catch(Exception exc)
            {
                return (ComputationResult.Exception(this, fn, exc));
            }
        }
    

        /// <summary>
        /// Generate a DynamicCalcComputation objet from a pre-existing, lower-level DynamicCalc<>.
        /// </summary>
        /// <param name="oDci"></param>
        /// <returns></returns>
        public static DynamicCalcComputation FromMethodInfo(MethodInfo oMethodInfo)
        {
            const string fn = "DynamicCalcComputation.FromMethodInfo()";
            try
            {
                DynamicCalcComputation ret = new DynamicCalcComputation();
                ret.ResultType = oMethodInfo.ReturnType;
                ret.Name = CaptionAttribute.Get(oMethodInfo);
                ret.Help = DocUtil.GetText<DocAttribute>(oMethodInfo);

                foreach(ParameterInfo pi in oMethodInfo.GetParameters())
                {

                    string sdesc = DocUtil.GetText<DescAttribute>(pi);
                    ParamInfo fparam = new ParamInfo(pi.ParameterType, CaptionAttribute.Get(pi), sdesc );
                    fparam.Help = DocUtil.GetText<DocAttribute>(pi);
                    string slink = DocUtil.GetText<HelpLinkAttribute>(pi);
                    if (!string.IsNullOrEmpty(slink)) { fparam.Help += "\r\nMore information: " + slink;  }
                    fparam.Value = null;
                    ret.FormalParameters.Add(fparam);
                }
                return (ret);
            }
            catch(Exception exc)
            {
                Util.HandleExc(typeof(DynamicCalcComputation), fn, exc);
                return (null);
            }
        } // end method()


        /// <summary>
        /// Given an assembly, loads all identified [DynamicCalc]s in the assembly and wraps them in DynamicCalcComputation objects.
        /// </summary>
        /// <param name="oAssembly"></param>
        /// <returns></returns>
        public static List<DynamicCalcComputation> WrapDynamicCalcsFromAssembly(Assembly oAssembly)
        {
            const string fn = "DynamicCalcComputation.WrapDynamicCalcsFromAssembly()";
            List<DynamicCalcComputation> retlist = new List<DynamicCalcComputation>();
            try
            {
                foreach (Type type in oAssembly.GetTypes())
                {
                    if (!DynamicCalcAttribute.IsPresent(type)) { continue; }
                    foreach (MethodInfo mi in type.GetMethods())    // This will eventually iterate assemblies/classes/methods
                    {
                        if (!mi.IsPublic) { continue; }
                        if (!DynamicCalcAttribute.IsPresent(mi)) { continue; }

                        DynamicCalcComputation dcc = DynamicCalcComputation.FromMethodInfo(mi);
                        retlist.Add(dcc);

                    } // end foreach(method in type)

                } // end foreach(type in assembly)

                return (retlist);

            } // end main try{}
            catch (Exception exc)
            {
                Util.HandleExc(typeof(DynamicCalcComputation), fn, exc);
                return (retlist);
            }
        } // end method()

        /// <summary>
        /// Generate a text representation of the computation:
        /// </summary>
        /// <returns></returns>
        public string Prettyprint()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Computation: " + Name);
            sb.AppendLine("  Produces: " + PrettifyType(ResultType));
            sb.AppendLine("  Parameters: ");
            foreach (ParamInfo pi in FormalParameters)
            {
                sb.AppendLine("    " + pi.Name + " (" + pi.ParameterType?.Name + ")");
                string sdesc = pi.Desc;
                string shelp = pi.Help;
                if (!string.IsNullOrEmpty(sdesc)) { sb.AppendLine("      Description: " + sdesc); }
                if (!string.IsNullOrEmpty(shelp)) { sb.AppendLine("      Help: " + shelp); }
            }
            return (sb.ToString());
        }

        private string PrettifyType(Type oType)
        {
            if (null==oType) { return (""); }

            if (oType.IsGenericType)
            {
                return (PrettifyGenericType(oType));
            } // endif(generic type)

            return (CaptionAttribute.Get(oType));
        }

        private string PrettifyGenericType(Type oType)
        {
            string scleanname = oType.Name;
            if (scleanname.Contains("`")) { scleanname = scleanname.Split("`".ToCharArray())[0]; }
            string sgen = scleanname + "<";
            int ngen = 0;
            foreach (Type gtype in oType.GetGenericArguments())
            {
                if (0 < ngen++) { sgen += ","; }
                if (gtype.IsGenericType) { sgen += PrettifyGenericType(gtype); }
                else { sgen += CaptionAttribute.Get(gtype); }
            }
            sgen += ">";
            return (sgen);

        }
    } // end class {}

} // end namespace{}
