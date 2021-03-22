using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;

namespace Levrum.Utils.Infra
{
    /// <summary>
    /// Attribute for tagging assemblies, classes and methods that can be used in the calc framework.
    /// </summary>
    public class DynamicCalcAttribute : Attribute
    {
        public DynamicCalcAttribute()
        {
        }
    }

    public class DynamicCalcErrValueAttribute : Attribute
    {
        public DynamicCalcErrValueAttribute(char cOpCode, object oErrVal)
        {
            OpCode = cOpCode;
            ErrVal = oErrVal;
        }

        public char OpCode = '=';
        public object ErrVal = null;

        public static bool IsBadValue(MethodInfo oMethod, object oVal)
        {
            string fn = "DynamicCalcErrValueAttribute.IsBadValue";
            Type type = typeof(DynamicCalcErrValueAttribute);
            try
            {
                object[] atts = oMethod.GetCustomAttributes(typeof(DynamicCalcErrValueAttribute), true);
                if ((null == atts) || (0 == atts.Length)) { return (false); }
                DynamicCalcErrValueAttribute dceva = atts[0] as DynamicCalcErrValueAttribute;
                char opcode = dceva.OpCode;
                object testval = dceva.ErrVal;



                if ('=' == opcode)
                {
                    if (null == testval) { return (null == oVal); }   // tightened for #3686 20181207 CDN
                    if (null == oVal) { return (null==testval); }
                    if (testval.GetType() != oVal.GetType()) { return (true); }
                    return (oVal.ToString() != testval.ToString());
                }

                else if ('<' == opcode)
                {
                    IComparable tic = testval as IComparable;
                    IComparable vic = oVal as IComparable;
                    if (((null == tic) || (null == vic)) && !((null == tic) && (null == vic)))
                    {
                        Util.HandleAppErrOnce(type, fn, "Non-IComparable argument in [DynamicCalcErrValue(<," + testval.ToString() + ")");
                        return (true);
                    }
                    int comp = vic.CompareTo(tic);
                    if (comp < 0) { return (true); }
                    return (false);
                }


                
                // Need to convert to IComparable from here on out.   Error if not convertible.
                // Until then, just log an error if we get here:
                Util.HandleAppErrOnce(type, fn, "Not fully implemented (opcode '" + opcode + "' unrecognized)");

                return (true);
                
            }

            catch (Exception exc)
            {
                Util.HandleExc(type, fn, exc);
                return (false);
            }
        }


    }

    /// <summary>
    /// Used for tagging runtime parameters of dynamic calculations.   These are fields
    /// on the object that are / can be bound at runtime.
    /// </summary>
    public class DynamicCalcRuntimeParamAttribute : Attribute
    {
        public DynamicCalcRuntimeParamAttribute()
        {
        }


    }

    /// <summary>
    /// Used for tagging parameters of dynamic calculations that are to be specified OUTSIDE of the
    /// method parameter list, and supplied at runtime by the caller.   The framework does not interrogate
    /// the user for these parameters, but assumes they are set by the caller.
    /// </summary>
    public class DynamicCalcFlexParamAttribute : Attribute
    {
    }

    /// <summary>
    /// General form of a dynamic calc that supports runtime parameters.
    /// </summary>
    public interface IDynamicCalcWithRuntimeParams
    {
        bool HasRuntimeParams();
        List<string> GetRuntimeParams();
        bool GetRuntimeParamsFromUser();
        bool SetRuntimeParams(List<string> paramList);
        string PpRuntimeParams();

    }


    public interface IDynamicCalcMaster : ICloneable, IDynamicCalcWithSupportingInfo, IDynamicCalcWithRuntimeParams, IValueEnumerator, IComparable
    {
        List<AttributeDef> GetCallingParamSignature();
        string GetCapt();
        List<Type> GetFormalParamList();
        List<AttributeDef> GetFormalParamsRuntime();
        List<object> GetLatestActualParamList();
        MethodInfo GetMethodInfo();
        string GetName();
        string GetBriefName();
        string GetDescription();
    }


    /// <summary>
    /// Information about a calculation that can be performed on a specific
    /// </summary>

    public class DynamicCalcInfo<T> : IDynamicCalcMaster
    {

        /// <summary>
        /// Default constructor.  Should only be used by reflection code.  Please do not call this from client code!
        /// </summary>
        public DynamicCalcInfo()
        {
        }


        /// <summary>
        ///  Public constructor.
        /// </summary>
        /// <param name="oDel"></param>
        /// <param name="oParamTypes"></param>
        public DynamicCalcInfo(MethodInfo oMethod, params Type[] oParamTypes)
        {
            string fn = "DynamicCalcInfo<T>.ctor()";
            try
            {
                m_oParamTypes.Clear();
                foreach (Type t in oParamTypes) { m_oParamTypes.Add(t); }
                m_oMethod = oMethod;
                Type deftype = m_oMethod.DeclaringType;
                if (m_oMethod.IsStatic)
                {
                    EvalInstance = null;
                } // endif static calculation class
                else
                {
                    ConstructorInfo cinfo = deftype.GetConstructor(new Type[] { });
                    if (null == cinfo)
                    {
                        Util.HandleAppErrOnce(this, fn, "Type " + deftype.Name + " has no default constructor, and cannot be used in the calculation framework");
                        EvalInstance = null;
                    } // endif(no default constructor)
                    EvalInstance = cinfo.Invoke(new object[] { });
                    if (null == EvalInstance)
                    {
                        Util.HandleAppErrOnce(this, fn, "Error initializing an instance of " + deftype.Name + "; some calculations may not work correctly.");
                    } // endif(failed to construct)
                } // end else(non-static)

                string scapt = CaptionAttribute.Get(oMethod);
                if (string.IsNullOrEmpty(scapt)) { scapt = oMethod.Name; }
                Name = scapt;
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
            }

        }


        public virtual MethodInfo GetMethodInfo()
        {
            return (m_oMethod);
        }


        /// <summary>
        /// This sets a flexible parameter on the current instance, given only the parameter value.
        /// It searches the current instance's class definition for a single [DynamicCalcFlexParam]
        /// field whose type matches the type of the supplied value.   If no such field, or more than one,
        /// is found, the method fails and returns false.   Otherwise the field is set to the supplied value.
        /// </summary>
        /// <param name="oValue"></param>
        /// <returns></returns>
        public virtual bool SetFlexParam(object oValue)
        {
            string fn = "DynamicCalcInfo<T>.SetFlexParam()";
            try
            {
                if (null == oValue) { return (Util.HandleAppErr(this, fn, "Unable to set a null value for a flex-param")); }
                FieldInfo fisel = null;
                Type vtype = oValue.GetType();
                foreach (FieldInfo fi in EvalInstance.GetType().GetFields())
                {
                    object[] atts = fi.GetCustomAttributes(typeof(DynamicCalcFlexParamAttribute), true);
                    if ((null == atts) || (0 == atts.Length)) { continue; } // skip non-flex params
                    if (!fi.FieldType.IsAssignableFrom(vtype)) { continue; } // skip non-conformable fields
                    if (null != fisel)
                    {
                        return (Util.HandleAppErr(this, fn, "You cannot assign flex params by value for type " + vtype.Name + " because there are more than one flex-params bearing this type."));
                    }
                    fisel = fi;
                } // end foreach(field info)

                // Now we know we've selected a single, unique field with the correct type, so we set it:
                fisel.SetValue(EvalInstance, oValue);

                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }


        /// <summary>
        /// Set a flex parameter by name.   The evaluation instance's class definition must
        /// contain a public field of the specified name and correct type to receive the 
        /// value supplied.
        /// </summary>
        /// <param name="sParamName"></param>
        /// <param name="oValue"></param>
        /// <returns></returns>
        public virtual bool SetFlexParamByName(string sParamName, object oValue)
        {
            string fn = "DynamicCalcInfo<T>.SetFlexParamByName()";
            try
            {
                if (null == EvalInstance) { return (Util.HandleAppErr(this, fn, "Null EvalInstance")); }
                Type defining_type = EvalInstance.GetType();
                FieldInfo fi = defining_type.GetField(sParamName);
                if (null == fi)
                {
                    return (Util.HandleAppErr(this, fn, "Unknown field: '" + sParamName + "'"));
                }

                Type ftype = fi.FieldType;
                if ((null == oValue) && (!ftype.IsByRef))
                {
                    return (Util.HandleAppErr(this, fn, "Attempt to set field '" + sParamName + "' to null;  type " + ftype.Name + " may not be nullable."));
                }

                if (!ftype.IsAssignableFrom(oValue.GetType()))
                {
                    return(Util.HandleAppErr(this,fn,"Cannot set field '" + sParamName + "' to value of type " + oValue.GetType().Name));
                }

                fi.SetValue(EvalInstance,oValue);
                return(true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }

        /// <summary>
        /// Does this calculation have any runtime parameters?
        /// </summary>
        /// <returns></returns>
        public virtual bool HasRuntimeParams()
        {
            try
            {
                if (null == m_oMethod) { return (false); }
                Type defining_type = m_oMethod.DeclaringType;
                foreach (FieldInfo fi in defining_type.GetFields())
                {
                    if (!fi.IsPublic) { continue; }
                    object[] atts = fi.GetCustomAttributes(typeof(DynamicCalcRuntimeParamAttribute), true);
                    if ((null != atts) && (atts.Length > 0)) 
                    {
                        if (!IsValidRuntimeParam(fi))
                        {
                            string fn = MethodBase.GetCurrentMethod().Name;
                            Util.HandleAppErrOnce(this, fn, "Field " + fi.Name + " in type " + fi.FieldType.Name + " is not a valid runtime parameter");
                            return (false);
                        }
                        return (true); 
                    }
                }

                return (false);
            }
            catch (Exception exc)
            {
                string fn = MethodBase.GetCurrentMethod().Name;
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }

        /// <summary>
        /// Is a specific field a valid runtime parameter?
        /// </summary>
        /// <param name="fi"></param>
        /// <returns></returns>
        private bool IsValidRuntimeParam(FieldInfo fi)
        {
            try
            {
                Type ftype = fi.FieldType;
                if (typeof(int).IsAssignableFrom(ftype)) { return (true); }
                if (typeof(double).IsAssignableFrom(ftype)) { return (true); }
                if (typeof(string).IsAssignableFrom(ftype)) { return (true); }
                if (typeof(DateTime).IsAssignableFrom(ftype)) { return (true); }
                if (typeof(Enum).IsAssignableFrom(ftype)) { return (true); }

                // 20180702 CDN - allow runtime params with custom attributes
                object[] cdsatts = fi.GetCustomAttributes(typeof(CustomDataSourceAttribute), true);
                if ((null != cdsatts) && (0 != cdsatts.Length)) { return (true); }

                return (false);
            }
            catch (Exception exc)
            {
                string fn = MethodBase.GetCurrentMethod().Name;
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }

        /// <summary>
        /// Query the user for any unbound runtime parameters.
        /// </summary>
        /// <returns></returns>
        public virtual bool GetRuntimeParamsFromUser()
        {
            return (GetRuntimeParamsFromUser(null));
        }

        public virtual bool GetRuntimeParamsFromUser(List<PdciRuntimeParam> oRtParams)
        {
            const string fn = "DynamicCalcInfo.GetRuntimeParamsFromUser()";
            Util.HandleAppErrOnce(this,fn,"Not implemented in .Net Standard yet!");
            return(false);
        }

        //public virtual bool GetRuntimeParamsFromUser(List<PdciRuntimeParam> oRtParams)
        //{
        //    string fn = MethodBase.GetCurrentMethod().Name;
        //    try
        //    {
        //        Type defining_type = m_oMethod.DeclaringType;
        //        foreach (FieldInfo fi in defining_type.GetFields())
        //        {
        //            if (!fi.IsPublic) { continue; }
        //            object [] atts = fi.GetCustomAttributes(typeof(DynamicCalcRuntimeParamAttribute),true);
        //            if ((null==atts)||(0==atts.Length)) { continue;  }
        //            if (!IsValidRuntimeParam(fi))
        //            {
        //                Util.HandleAppErrOnce(this,fn,"Invalid runtime parameter: " + fi.DeclaringType.Name + "." + 
        //                                        fi.Name);
        //                continue;
        //            }

        //            // Know we've got a good one.   Get a string value from the user:
        //            string caption = CaptionAttribute.Get(fi);
        //            Type ftype = fi.FieldType;
        //            object oval = null;
        //            string sval = "";
        //            string semsg = "";

        //            List<string> enum_strings = new List<string>();
        //            object [] c_atts = fi.GetCustomAttributes(typeof(StringEnumAttribute),true);
        //            if ((null != c_atts) && (0 != c_atts.Length))
        //            {
        //                StringEnumAttribute enumatt = c_atts[0] as StringEnumAttribute;
        //                enum_strings.AddRange(enumatt.EnumValues);
        //            }

        //            // 20180702 CDN - special case for custom data sources:
        //            object [] cdsatts = fi.GetCustomAttributes(typeof(CustomDataSourceAttribute), true);
        //            if ((null != cdsatts) && (cdsatts.Length > 0))
        //            {
        //                CustomDataSourceAttribute cdsatt = cdsatts[0] as CustomDataSourceAttribute;
        //                GenericListLookupForm gllf = new GenericListLookupForm();
        //                IEnumerable choices = CustomDataSourceAttribute.GetValues(cdsatt, fi);
        //                gllf.Fill(choices);
        //                gllf.Text = "Please select one of the options below for " + caption;
        //                gllf.ShowDialog();
        //                object selection = gllf.ReturnValue;
        //                if (null == selection) { return (false); }
        //                oval = selection;
        //            }
        //            // Special case for enums:
        //            if (typeof(Enum).IsAssignableFrom(ftype))
        //            {
        //                oval = SingleValueForm.GetEnum(ftype, this.Name + " Parameters:", caption);
        //            }

        //            else if (enum_strings.Count > 0)
        //            {
        //                sval = SingleValueForm.GetInstance<string>(enum_strings, this.Name, "Please choose " + caption + " for " + this.Name);
        //            }
        //            // If the custom field type is a name string, let's use it
        //            else if (ftype == typeof(string) && fi.Name.ToUpper().Contains("CUSTOMFIELDNAME"))
        //            {
        //                sval = oval.ToString();
        //            }
        //            // For everything else, we read a string:
        //            else
        //            {
        //                sval = SingleValueForm.GetStr(this.Name + " Parameters: ", caption);
        //            }


        //            // Case on the type and parse:
        //            if (null == oval)
        //            {
        //                if (typeof(int).IsAssignableFrom(ftype))
        //                {
        //                    int n = 0;
        //                    if (!int.TryParse(sval, out n)) { semsg = "Invalid integer: " + sval; }
        //                    oval = n;
        //                } // end elsif(integer)
        //                else if (typeof(double).IsAssignableFrom(ftype))
        //                {
        //                    double d = 0.0;
        //                    if (!double.TryParse(sval, out d)) { semsg = "Invalid double: " + sval; }
        //                    oval = d;
        //                } // end elsif(double)
        //                else if (typeof(DateTime).IsAssignableFrom(ftype))
        //                {
        //                    DateTime dtm = DateTime.MinValue;
        //                    if (!DateTime.TryParse(sval, out dtm)) { semsg = "Invalid date/time: " + sval; }
        //                    oval = dtm;
        //                } // end elsif(date-time)
        //                else if (typeof(string).IsAssignableFrom(ftype))
        //                {
        //                    oval = sval;
        //                } // end elsif(string)
        //                else
        //                {
        //                    Util.HandleAppErrOnce(this, fn, "Unable to set runtime parameter " + fi.DeclaringType.Name + "." +
        //                                            fi.Name + ": unknown type " + ftype.Name);
        //                    return (false);
        //                } // end else(unknown parameter type)
        //            } // endif(haven't obtained object yet)

        //            fi.SetValue(EvalInstance,oval);

        //            if (null != oRtParams)
        //            {
        //                PdciRuntimeParam rtparam = new PdciRuntimeParam();
        //                rtparam.DefiningClass = defining_type.AssemblyQualifiedName;
        //                rtparam.FieldName = fi.Name;
        //                if (!String.IsNullOrEmpty(oval.ToString()))
        //                {
        //                    rtparam.FieldValue = oval.ToString();
        //                }
        //            }
        //        } // end foreach(field)

        //        return(true);
        //    }
        //    catch (Exception exc)
        //    {
        //        Util.HandleExc(this, fn, exc);
        //        return (false);
        //    }

        //} // end method()

        public override string ToString()
        {
            return(Serialize());
            //return (this.Name);
        }


        /// <summary>
        /// Find a specific calculation object by name.
        /// </summary>
        /// <param name="sName"></param>
        /// <param name="oParamTypes"></param>
        /// <returns></returns>
        public static DynamicCalcInfo<T> FindCalcByName(string sName, params Type[] oParamTypes)
        {
            if (string.IsNullOrEmpty(sName)) { return (null); }
            List<DynamicCalcInfo<T>> allcalcs = new List<DynamicCalcInfo<T>>();
            allcalcs.AddRange(DynamicCalcInfo<T>.GetAvailableCalcs(oParamTypes));
            foreach (DynamicCalcInfo<T> calc in allcalcs)  // strict equality
            {
                if ((calc.Name == sName) || (calc.BriefName == sName)) { return (calc); }
            }


            foreach (DynamicCalcInfo<T> calc in allcalcs)  // case-insensitive substring
            {
                if (calc.Name.ToUpper().Contains(sName.ToUpper()) || (calc.BriefName.ToUpper().Contains(sName.ToUpper())))
                {
                    return (calc);
                }
            }

            return (null);
        }

        /// <summary>
        /// Find all available calculations matching a given return type and parameter set.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="oParamTypes"></param>
        /// <returns></returns>
        public static IEnumerable<DynamicCalcInfo<T>> GetAvailableCalcs(params Type[] oParamTypes)
        {
            List<DynamicCalcInfo<T>> retlist = new List<DynamicCalcInfo<T>>();


            //C3mCfBmkTester tester = new C3mCfBmkTester();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                object[] atts = assembly.GetCustomAttributes(typeof(DynamicCalcAttribute), false);
                if ((null == atts) || (0 == atts.Length)) { continue; }
                foreach (Type type in assembly.GetTypes())
                {
                    object[] typeatts = type.GetCustomAttributes(typeof(DynamicCalcAttribute), false);
                    if ((null == typeatts) || (0 == typeatts.Length)) { continue; }
                    foreach (MethodInfo mi in type.GetMethods())    // This will eventually iterate assemblies/classes/methods
                    {
                        if (!mi.IsPublic) { continue; }
                        object[] methodatts = mi.GetCustomAttributes(typeof(DynamicCalcAttribute), true);
                        if ((null == methodatts) || (0 == methodatts.Length)) { continue; }

                        if (!HasCorrectSignature(mi, oParamTypes)) { continue; }

                        DynamicCalcInfo<T> info = new DynamicCalcInfo<T>(mi, oParamTypes);

                        // Name and description:
                        info.Name = CaptionAttribute.Get(mi);
                        info.Description = "";

                        // Get the brief title from the [BriefTitle] attribute if defined; else cut
                        // down the calc name:
                        object [] brieftitles = mi.GetCustomAttributes(typeof(BriefTitleAttribute), false);
                        if ((null != brieftitles) && (0 != brieftitles.Length))
                        {
                            BriefTitleAttribute bta = brieftitles[0] as BriefTitleAttribute;
                            info.BriefName = bta.Text;
                        }
                        else
                        {
                            string[] name_pcs = info.Name.Split(" ".ToCharArray());
                            foreach (string spc in name_pcs)
                            {
                                if (spc.Length > 0)
                                {
                                    info.BriefName = spc;
                                    break;
                                }
                            }
                        }

                        retlist.Add(info);
                    }
                }
            }




            return (retlist);

        }

        /// <summary>
        /// Set the name and description from the caption, etc.   Use the {RtParamName} convention
        /// to substitute runtime parameters in from the caption.
        /// </summary>
        /// <param name="mi"></param>
        public string GetCapt()
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                string scaption = CaptionAttribute.Get(m_oMethod);
                string sfinal = "";
                string spending = "";
                int bracket_level = 0;
                for (int i = 0; i < scaption.Length; i++)
                {
                    char c = scaption[i];
                    if ('{' == c) 
                    { 
                        bracket_level++; 
                        spending += c;
                    }
                    else if ('}' == c)
                    {
                        spending += c;
                        bracket_level--;
                        if (0 == bracket_level)
                        {
                            string subst = SubstituteRtParam(spending,m_oMethod);
                            sfinal += subst;
                            spending = "";
                        }
                    }
                    else // not a curly brace
                    {
                        if (0 == bracket_level) { sfinal += c; }
                        else { spending += c; }
                    }
                      
                } // end for(i = character index)
                return (sfinal);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return ("*error*");
            }
        }

        /// <summary>
        /// Attempt to substitute run-time parameter values for a "pending" string -- one that has been
        /// parsed out with curly-braces.
        /// </summary>
        /// <param name="sPending"></param>
        /// <returns></returns>
        private string SubstituteRtParam(string sPending, MethodInfo oMethod)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                string trimmed = sPending.TrimStart("{".ToCharArray()).TrimEnd("}".ToCharArray());
                Type defclass = oMethod.DeclaringType;
                FieldInfo fi = defclass.GetField(trimmed);
                if (null == fi) { return (sPending); }    // if there happen to be curly-braces but no matching field, just return the string verbatim
                object val = fi.GetValue(this.EvalInstance);
                if (null == val) { return (sPending); }
                return (val.ToString());
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return ("");
            }
        }

        /// <summary>
        /// Does this method have the signature specified by the given parameter types?
        /// </summary>
        /// <param name="mi"></param>
        /// <param name="oParamTypes"></param>
        /// <returns></returns>
        private static bool HasCorrectSignature(MethodInfo mi, Type[] oParamTypes)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                if (!typeof(T).IsAssignableFrom(mi.ReturnType)) { return (false); }

                ParameterInfo[] parameters = mi.GetParameters();
                if ((null == oParamTypes) && (null != parameters)) { return (false); }
                if ((null == parameters) && (null != oParamTypes)) { return (false); }
                if (oParamTypes.Length != parameters.Length) { return (false); }
                for (int i = 0; i < oParamTypes.Length; i++)
                {
                    Type paramtype = parameters[i].ParameterType;
                    if (!oParamTypes[i].IsAssignableFrom(paramtype)) { return (false); }
                }

                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(DynamicCalcInfo<T>), fn, exc);
                return (false);
            }
        }

        /// <summary>
        /// Evaluate the calculation on a specific set of parameters.
        /// </summary>
        /// <param name="oParams"></param>
        /// <returns></returns>
        public virtual T Eval(params object[] oParams)
        {
            const string fn = "DynamicCalcInfo.Eval()";
            try
            {

                if (CheckParams)
                {
                    if (!ValidateParams(m_oMethod, oParams)) { return (default(T)); }
                } // endif(CheckParams)

                m_oCurrentParams.Clear();
                foreach (object param in oParams) { m_oCurrentParams.Add(param); }
                
                object val = m_oMethod.Invoke(EvalInstance, oParams);


                this.m_bCalcSucceeded = !DynamicCalcErrValueAttribute.IsBadValue(m_oMethod,val);

                if (null == val) { return (default(T)); }



                Type valtype = val.GetType();
                if (!typeof(T).IsAssignableFrom(valtype))
                {
                    Util.HandleAppErr(this, fn, "Indirect execution type mismatch: expected " + typeof(T).Name + ", found " + valtype.Name);
                    return (default(T));
                }

                T retval = (T)val;
                return (retval);

            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (default(T));
            }
        }

        private bool ValidateParams(MethodInfo oMethod, params object[] oParams)
        {
            const string fn = "DynamicCalcInfo.ValidateParams()";
            try
            {
                if (null == oParams)
                {
                    if (0 != m_oParamTypes.Count)
                    {
                        Util.HandleAppErr(this, fn, "C3mCalc '" + Name + "':  no parameters supplied, expected " + m_oParamTypes.Count);
                        return (false);
                    }
                } // endif(no params)
                else if (oParams.Length != m_oParamTypes.Count)
                {
                    Util.HandleAppErr(this, fn, "Wrong # parameters in C3mCalc '" + Name + "':  expected " +
                                m_oParamTypes.Count + ", found " + oParams.Length);
                    return (false);
                } // end elsif(wrong # params)

                // Know we have the right number, so check individual argument types:
                for (int i = 0; i < m_oParamTypes.Count; i++)
                {
                    Type ptype = m_oParamTypes[i];
                    object argn = oParams[i];
                    if ((null == argn) && (ptype.IsValueType))
                    {
                        Util.HandleAppErr(this, fn, "C3m Calc '" + Name + "': argument " + (i + 1) + " is null, which isn't OK");
                        return (false);
                    }
                    Type typen = argn.GetType();
                    if (!ptype.IsAssignableFrom(typen))
                    {
                        Util.HandleAppErr(this, fn, "C3m Calc '" + Name + "': argument " + (i + 1) + " -- expected " + ptype.Name + ", found " + typen.Name);
                        return (false);
                    }
                } // end for(i: param)

                return (true);

            } // end main try
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }

        private List<Type> m_oParamTypes = new List<Type>();

        private List<object> m_oCurrentParams = new List<object>();

        public string GetName() { return (Name); }
        public string Name = "";

        public string GetBriefName() { return (BriefName); }
        public string BriefName = "";

        public string GetDescription() { return (Description); }
        public string Description = "";


        /// <summary>
        /// Did the calculation succeed?
        /// </summary>
        public virtual bool CalcSucceeded
        {
            get { return (m_bCalcSucceeded); }
        }
        private bool m_bCalcSucceeded = false;

        /// <summary>
        /// Should we perform parameter checking for this instance?
        /// Good to leave this "true" during development, and change to "false"
        /// once fully tested, for performance?
        /// </summary>
        public bool CheckParams = true;

        /// <summary>
        /// Get the list of types comprising the formal parameters.
        /// </summary>
        /// <returns></returns>
        public List<Type> GetFormalParamList()
        {
            List<Type> retlist = new List<Type>();
            if (null == m_oParamTypes) { return (retlist); }
            foreach (Type t in m_oParamTypes) { retlist.Add(t); }
            return (retlist);
        }

        /// <summary>
        /// Get the actual parameters from the last invocation of this dynamic calculation object.
        /// If the object has never been invoked, the list will be empty.
        /// </summary>
        /// <returns></returns>
        public List<object> GetLatestActualParamList()
        {
            List<object> aparams = new List<object>();
            foreach (object oparam in m_oCurrentParams) { aparams.Add(oparam); }
            return (aparams);
        }


        /// <summary>
        /// The object instance on which the method will be called.   This allows runtime parameter binding.
        /// </summary>
        public object EvalInstance
        {
            get { return(m_oEvalInstance); }
            set { m_oEvalInstance = value; }
        }


        private object m_oEvalInstance = null;
        private MethodInfo m_oMethod = null;




        /// <summary>
        /// Does this dynamic calculation support a flex-parameter with the given name?
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool SupportsFlexParam(string sName)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                if (null == EvalInstance) { return (Util.HandleAppErrOnce(this, fn, "Null eval-instance in " + this.ToString())); }
                Type etype = EvalInstance.GetType();
                foreach (FieldInfo fi in etype.GetFields())
                {
                    object[] atts = fi.GetCustomAttributes(typeof(DynamicCalcFlexParamAttribute),true);
                    if ((null == atts) || (0 == atts.Length)) { continue; }
                    if (fi.Name == sName) { return (true); }
                }

                return (false); // if we look through all fields with no match, we've failed.
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }

        /// <summary>
        /// Provide a string of the form "name1=value1, name2=value2,..." showing current parameter bindings.
        /// </summary>
        /// <returns></returns>
        public string PpRuntimeParams()
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                StringBuilder sb = new StringBuilder();
                Type defining_type = m_oMethod.DeclaringType;
                int nparams = 0;
                foreach (FieldInfo fi in defining_type.GetFields())
                {
                    if (nparams > 1) { sb.Append(", "); }
                    nparams++;
                    if (!fi.IsPublic) { continue; }
                    object[] atts = fi.GetCustomAttributes(typeof(DynamicCalcRuntimeParamAttribute), true);
                    if ((null == atts) || (0 == atts.Length)) { continue; }
                    object oval = fi.GetValue(this.m_oEvalInstance);
                    string sval = "{undefined}";
                    if (null != oval) { sval = oval.ToString(); }
                    String pname = CaptionAttribute.Get(fi);
                    sb.Append(pname + "=" + sval);
                }
                return (sb.ToString());
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return ("*error*");
            }
        }


        /// <summary>
        /// Retrieve a list of formal parameter types necessary to invoke the
        /// Eval() method on the current calculation.
        /// </summary>
        /// <returns></returns>
        public List<AttributeDef> GetCallingParamSignature()
        {
            const string fn = "DynamicCalcINfo.GetCallingParamSignature()";
            List<AttributeDef> retlist = new List<AttributeDef>();
            try
            {
                int iordinal = 0;
                foreach (Type type in m_oParamTypes)
                {
                    retlist.Add(new AttributeDef("Param " + iordinal,type,iordinal));
                    iordinal++;
                }
                return(retlist);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this,fn,exc);
                return(retlist);
            }
        }

        /// <summary>
        /// Retrieve a list of formal RUNTIME parameters for this calculation, as
        /// a list of attribute definitions.   Runtime parameters are dynamic calc parameters
        /// defined as instance-level public fields.
        /// </summary>
        /// <returns></returns>
        public List<AttributeDef> GetFormalParamsRuntime()
        {
            List<AttributeDef> outList = new List<AttributeDef>();
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                Type type = m_oMethod.DeclaringType;
                int iord = 0;
                foreach (FieldInfo fi in type.GetFields())
                {
                    if (!fi.IsPublic)
                        continue;
                    object[] atts = fi.GetCustomAttributes(typeof(DynamicCalcRuntimeParamAttribute), true);
                    if (atts == null || atts.Length == 0) { continue; }
                    AttributeDef adef = new AttributeDef(fi.Name,fi.FieldType, iord++);
                    object oval = fi.GetValue(this.m_oEvalInstance);
                    string sval = "";
                    if (null!= oval) { sval = oval.ToString(); }
                    adef.CachedStringValue = sval;
                    outList.Add(adef);
                } // end foreach(field)
                return(outList);
            }
            catch (Exception ex)
            {
                Util.HandleExc(this, fn, ex);
                return(outList);
            }
            
        }

        /// <summary>
        /// Get a list of the string values of the runtime parameters for this
        /// calculation object.
        /// </summary>
        /// <returns></returns>
        public List<string> GetRuntimeParams()
        {
            List<string> outList = new List<string>();
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                Type type = m_oMethod.DeclaringType;
                foreach(FieldInfo fi in type.GetFields())
                {
                    if (!fi.IsPublic)
                        continue;
                    object[] atts = fi.GetCustomAttributes(typeof(DynamicCalcRuntimeParamAttribute), true);
                    if (atts == null || atts.Length == 0)
                        continue;
                    object oval = fi.GetValue(this.m_oEvalInstance);
                    if(null == oval) { outList.Add(""); }
                    else
                    {
                        outList.Add(oval.ToString());
                    }
                }
            } catch (Exception ex) {
                Util.HandleExc(this, fn, ex);
            }
            return outList;
        }

        public void SetRuntimeParams(params string[] oParams)
        {
            List<string> plist = new List<string>();
            plist.AddRange(oParams);
            SetRuntimeParams(plist);
        }

        public bool SetRuntimeParams(List<string> paramList)
        {
            const string fn = "DynamicCalcInfo<T>.SetRuntimeParams()";
            string[] paramArray = paramList.ToArray();
            int i = 0;
            bool success = true;

            Type type = m_oMethod.DeclaringType;
            foreach (FieldInfo fi in type.GetFields())
            {

                if (i >= paramArray.Length)
                {
                    Util.HandleAppErrOnce(this,fn,"Calculation '" + this.Name + "': not enough parameters supplied");
                    return(false);
                }

                if (!fi.IsPublic)
                    continue;
                object[] atts = fi.GetCustomAttributes(typeof(DynamicCalcRuntimeParamAttribute), true);
                if (atts == null || atts.Length == 0) { continue; }

                object oval = null;

                Type fieldType = fi.FieldType;
                if(typeof(Enum).IsAssignableFrom(fieldType))
                {
                    var enumVal = from object obj in Enum.GetValues(fieldType)
                                  where obj.ToString() == paramArray[i]
                                  select obj;

                    oval = enumVal.FirstOrDefault();
                }
                else if (typeof(int).IsAssignableFrom(fieldType))
                {
                    int n = 0;
                    if (!int.TryParse(paramArray[i], out n))
                        success &= Util.HandleAppWarningOnce(this, "DynamicCalcInfo.SetRuntimeParams", string.Format("Unable to set int param from string {0}", paramArray[i]));
                    oval = n;
                }
                else if (typeof(double).IsAssignableFrom(fieldType))
                {
                    double d = 0.0d;
                    if(!double.TryParse(paramArray[i], out d))
                        success &= Util.HandleAppWarningOnce(this, "DynamicCalcInfo.SetRuntimeParams", string.Format("Unable to set double param from string {0}", paramArray[i]));
                    oval = d;
                }
                else if (typeof(DateTime).IsAssignableFrom(fieldType))
                {
                    DateTime dtm = DateTime.MinValue;
                    if(!DateTime.TryParse(paramArray[i], out dtm))
                        success &= Util.HandleAppWarningOnce(this, "DynamicCalcInfo.SetRuntimeParams", string.Format("Unable to set DateTime param from string {0}", paramArray[i]));
                    oval = dtm;
                }
                else if (typeof(string).IsAssignableFrom(fieldType))
                {
                    oval = paramArray[i];
                }

                fi.SetValue(EvalInstance, oval);
                i++;
            } // end foreach()
            return(success);
        } // end method()

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                Type[] ptypes = new Type[m_oParamTypes.Count];
                for (int i = 0; i < ptypes.Length; i++) { ptypes[i] = m_oParamTypes[i]; }
                DynamicCalcInfo<T> newinst = new DynamicCalcInfo<T>(m_oMethod, ptypes);
                newinst.EvalInstance = this.EvalInstance;
                newinst.BriefName = this.BriefName;
                newinst.CheckParams = this.CheckParams;
                newinst.Description = this.Description;
                newinst.m_bCalcSucceeded = this.m_bCalcSucceeded;
                newinst.m_oCurrentParams = new List<object>();
                if (null!=this.m_oCurrentParams) { m_oCurrentParams.AddRange(this.m_oCurrentParams); }
                newinst.Name = this.Name;
                return (newinst);


            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (null);
            }
        }


        /// <summary>
        /// Clone a list of DynamicCalcInfo instances.
        /// </summary>
        /// <param name="m_oStatsCalcs"></param>
        /// <returns></returns>
        public static List<DynamicCalcInfo<double>> CloneList(List<DynamicCalcInfo<double>> oList)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            List<DynamicCalcInfo<double>> retlist = new List<DynamicCalcInfo<double>>();
            try
            {
                foreach (DynamicCalcInfo<double> dci in oList)
                {
                    DynamicCalcInfo<double> dcic = dci.Clone() as DynamicCalcInfo<double>;
                    if (null != dcic) { retlist.Add(dcic); }
                    else { Util.HandleAppErrOnce(typeof(DynamicCalcInfo<double>),fn,"One or more objects failed to clone"); }
                }
                return(retlist);
            }
            catch (Exception)
            {
                return (retlist);
            }

        }

        /// <summary>
        /// "Side effect" data resulting from the calculation.   This is specific to the calculation being performed;
        /// the caller needs to know something about the semantics of the calculation in order to use this data.
        /// </summary>
        public object SupportingData
        {
            get
            {
                if (null == EvalInstance) { return (null); }
                IDynamicCalcWithSupportingInfo idcwsi = EvalInstance as IDynamicCalcWithSupportingInfo;
                if (null == idcwsi) { return (null); }
                return (idcwsi.SupportingData);
            }
        }

        /// <summary>
        /// Get all instances of this particular kind of DynamicCalc.   
        /// </summary>
        /// <param name="oParams">Array of types qualifying the DynamicCalc object</param>
        /// <returns></returns>
        public IEnumerable GetValues(params object[] oParams)
        {
            string fn = "DynamicCalcInfo<T>.GetValues()";
            List<string> retlist = new List<string>();
            try
            {
                Type[] argtypes = new Type[oParams.Length];
                for (int i=0; i<argtypes.Length; i++)
                {
                    argtypes[i] = (Type)oParams[i];
                }
                IEnumerable<DynamicCalcInfo<T>> dclist = DynamicCalcInfo<T>.GetAvailableCalcs(argtypes);
                foreach (DynamicCalcInfo<T> dci in dclist)
                {
                    retlist.Add(dci.Name);
                    //retlist.Add(dci);
                }
                return (retlist);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (null);
            }
        }

        /// <summary>
        /// Serialize a calculation into the form
        ///   "CalcName: Param1,Param2,..." -- where everything right of the ':' is
        /// in CSV format.
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            const string fn = "DynamicCalcInfo<T>.Serialize()";
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(Name);
                if (HasRuntimeParams())
                {
                    sb.Append(": ");
                    List<string> rtparams = GetRuntimeParams();
                    for (int i = 0; i < rtparams.Count; i++)
                    {
                        if (i > 0) { sb.Append(","); }
                        string spc = rtparams[i];
                        if (spc.Contains(",") || spc.Contains(" ")) { spc = Util.Csvify(spc); }
                        sb.Append(Util.Csvify(rtparams[i])); ;
                    }
                }
                return(sb.ToString());
            }
            catch (Exception exc)
            {
                Util.HandleExc(this,fn, exc);
                return("");
            }
        }

        public static DynamicCalcInfo<T> DeSerialize(string sSerialized)
        {
            const string fn = "DynamicCalcInfo<T>.DeSerialize()";
            Type type = typeof(DynamicCalcInfo<T>);
            try
            {
                int index = sSerialized.IndexOf(':');
                string calcname = sSerialized.Substring(0, index);
                int trailing_len = sSerialized.Length - (index+1);
                string sparams = sSerialized.Substring(index+1, trailing_len).TrimStart();

                List<string> lparams = Util.ParseCsv(sparams);
                DynamicCalcInfo<T> dci = DynamicCalcInfo<T>.FindCalcByName(calcname);
                if (null == dci)
                {
                    Util.HandleAppErrOnce(type,fn,"Unable to resolve calculation " + sSerialized);
                    return(null);
                }

                List<string> aparams = Util.ParseCsv(sparams);
                if (dci.SetRuntimeParams(aparams))
                {
                    Util.HandleAppErrOnce(type,fn,"Unable to bind parameters for calculation: " + sSerialized);
                    return(null);
                }

                return(dci);

                
            }
            catch (Exception exc)
            {
                Util.HandleExc(type,fn, exc);
                return(null);
            }
        }

        public int CompareTo(object obj)
        {
            DynamicCalcInfo<T> rhs = obj as DynamicCalcInfo<T>;
            if (null== rhs) { return(-1); }
            return(this.Name.CompareTo(rhs.Name));
        }

    } // end class

    /// <summary>
    /// Interface for calc classes that contain supporting data.
    /// </summary>
    public interface IDynamicCalcWithSupportingInfo
    {
        object SupportingData { get; }
    }



    public class PdciRuntimeParam
    {
        public string DefiningClass = "";
        public string FieldName = "";
        public string FieldValue = "";
    }

}
