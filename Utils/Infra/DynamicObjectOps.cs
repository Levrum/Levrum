using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;

namespace Levrum.Utils.Infra
{


    /// <summary>
    /// Dynamic typing extension methods on Object -- prettyprinting from metadata, field set and get
    /// from runtime fieldnames, etc.
    /// </summary>
    public static class DynamicObjectExtensions
    {




        /// <summary>
        /// Prettyprint all public fields of a generic object in "Name: Value" format, applying a specified prefix.
        /// </summary>
        /// <param name="oSubj"></param>
        /// <param name="sPrefix"></param>
        /// <returns></returns>
        public static String PrettyprintPublicFields(this Object oSubj, String sPrefix)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                Type type = oSubj.GetType();
                StringBuilder sb = new StringBuilder();
                foreach (FieldInfo fi in type.GetFields())
                {
                    if (!fi.IsPublic) { continue; }
                    object val = fi.GetValue(oSubj);
                    sb.AppendLine("  " + fi.Name + ": " + val.ToString());
                }
                return (sb.ToString());
            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(DynamicObjectExtensions), fn, exc);
                return ("");
            }

        }

        /// <summary>
        /// Get the value of an object field dynamically.
        /// </summary>
        /// <param name="oSubj"></param>
        /// <param name="sFieldName"></param>
        /// <param name="bCaseInsensitive">Is this fieldname to be considered case-insensitive?</param>
        /// <param name="bIsOptional">Is the specified field optional?  If true, error messages are suppressed for missing fields.</param>
        /// <returns></returns>
        /// 

        public static object GetFieldDynamic(this Object oSubj, String sFieldName)
        {
            return (GetFieldDynamic(oSubj, sFieldName, false, true));
        }

        public static object GetFieldDynamic(this Object oSubj, String sFieldName, bool bCaseInsensitive, bool bIsRequired)
        {
            String fn = "Util.DynamicObjectExtensions";
            try
            {

                // Look for the desired field.  If found, all good.  Otherwise, if the caller wants us to,
                // we loop through all fields in the subject class and try a case-insensitive match; if
                // we get a hit, we cache the type-insensitive name/field and use the field.
                Type type = oSubj.GetType();
                FieldInfo fi = type.GetField(sFieldName);
                if ((null == fi) && (bCaseInsensitive))
                {
                    string sfn = (type.FullName + "." + sFieldName).ToUpper();
                    if (m_oCaseInsensitiveFieldLookup.ContainsKey(sfn)) { fi = m_oCaseInsensitiveFieldLookup[sfn]; }
                    else if (!m_oCaseInsensitiveFieldsNotFound.ContainsKey(sfn))
                    {
                        foreach (FieldInfo fi2 in type.GetFields())
                        {
                            if (fi2.Name.ToUpper() == sFieldName.ToUpper())
                            {
                                fi = fi2;
                                sfn = (type.FullName + "." + fi2.Name).ToUpper();
                                m_oCaseInsensitiveFieldLookup.Add(sfn, fi);
                                break;
                            } // endif(found the field in a different case)
                        } // end foreach(fi2 - field in whole type)
                        if (null == fi) { m_oCaseInsensitiveFieldsNotFound.Add(sfn, sfn); }     // shortcut the full fieldlist loop search
                    } // end elsif (field key not in lookup AND we haven't tried before)

                } // endif(can't find field AND doing case-insensitive match)

                // If it's still null, it's an error condition and we report it as such if desired:
                if (null == fi)
                {

                    if (bIsRequired)
                    {
                        Util.HandleAppErrOnce(typeof(DynamicObjectExtensions), fn, "Field '" + sFieldName + "' not found in type " + type.Name);
                    }
                    return (null);
                }
                object ret = fi.GetValue(oSubj);
                return (ret);

            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(DynamicObjectExtensions), fn, exc);
                return (null);
            }

        }


        /// <summary>
        /// Get a type-safe value for a field, dynamically, in a way that will not throw
        /// an exception, but will return default() for the specified type if the requested
        /// field is not present, the type is not convertible, or another error occurs.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="oSubj"></param>
        /// <param name="sFieldName"></param>
        /// <param name="bCaseSensitive">Is the fieldname case-sensitive?  DEFAULT:  false</param>
        /// <returns></returns>
        public static T SafeGet<T>(this object oSubj, string sFieldName)
        {
            return (SafeGet<T>(oSubj, sFieldName, false, false));
        }
        public static T SafeGet<T>(this Object oSubj, String sFieldName, bool bCaseSensitive, bool bRequired)
        {
            string fn = "DynamicObjectExtensions.SafeGet<T>";
            Type type = typeof(DynamicObjectExtensions);
            T defaultval = default(T);
            try
            {

                object oval = oSubj.GetFieldDynamic(sFieldName, !bCaseSensitive, bRequired);

                // Special case if user wants a string -- return empty string on error:
                if (typeof(string).IsAssignableFrom(typeof(T)))
                {
                    defaultval = (T)(object)"";
                    if (null == oval) { return (defaultval); }
                    T stringret = (T)(object)oval.ToString();
                    return (stringret);
                }

                if (null == oval) { return (defaultval); }
                Type vtype = oval.GetType();
                if (!typeof(T).IsAssignableFrom(vtype))
                {
                    Util.HandleAppErrOnce(type, fn, "Unable to convert " + vtype.Name + " to " + typeof(T).Name);
                    return (defaultval);
                }

                T ret = (T)oval;
                return (ret);
            }
            catch (Exception exc)
            {
                Util.HandleExc(type, fn, exc);
                return (defaultval);
            }
        }


        /// <summary>
        /// Case-insensitive lookup for fieldnames.   Format is ["NAMESPACE.NS.CLASSNAME.FIELDNAME"] = FieldInfo.
        /// </summary>
        private static Dictionary<string, FieldInfo> m_oCaseInsensitiveFieldLookup = new Dictionary<string, FieldInfo>();

        /// <summary>
        /// Dictionary of fields we have tried and failed to find on a case-insensitve basis.
        /// </summary>
        private static Dictionary<string, string> m_oCaseInsensitiveFieldsNotFound = new Dictionary<string, string>();


        /// <summary>
        /// Is the subject type (this) assignable to one or more of the specified candidate types?
        /// Example:  if (!curType.IsOneOf(typeof(string),typeof(int),typeof(double))) { return; }
        /// </summary>
        /// <param name="oSubjType"></param>
        /// <param name="oCandidateTypes"></param>
        /// <returns></returns>
        public static bool IsOneOf(this Type oSubjType, params Type[] oCandidateTypes)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            Type type = typeof(DynamicObjectExtensions);
            try
            {
                foreach (Type ctype in oCandidateTypes)
                {
                    if (ctype.IsAssignableFrom(oSubjType)) { return (true); }
                }
                return (false);
            }
            catch (Exception exc)
            {
                Util.HandleExc(type, fn, exc);
                return (false);
            }
        }


        /// <summary>
        /// Get a FieldInfo from a data collection, given a field name.
        /// </summary>
        /// <param name="oData"></param>
        /// <param name="sFieldName"></param>
        /// <returns></returns>
        public static FieldInfo GetFieldFromData(this List<object> oData, String sFieldName)
        {
            Type type = typeof(DynamicObjectExtensions);
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                if ((null == oData) || (0 == oData.Count))
                {
                    Util.HandleAppErr(type, fn, "No data supplied");
                    return (null);
                }
                object obj0 = oData[0];
                if (null == obj0)
                {
                    Util.HandleAppErr(type, fn, "First datum is null");
                    return (null);
                }
                Type type0 = obj0.GetType();
                FieldInfo fi = type0.GetField(sFieldName);
                if (null == fi)
                {
                    Util.HandleAppErr(type, fn, "Field '" + sFieldName + "' not found in type " + type0.Name);
                }
                return (fi);
            }
            catch (Exception exc)
            {

                Util.HandleExc(type, fn, exc);
                return (null);
            }
        }

        /// <summary>
        /// Dynamically set the value of a field (by name) in an object whose type may not be known at compile time.
        /// </summary>
        /// <param name="oSubj"></param>
        /// <param name="sFieldName"></param>
        /// <param name="oValue"></param>
        /// <returns></returns>
        public static bool SetFieldDynamic(this Object oSubj, String sFieldName, object oValue)
        {
            const string fn = "DynamicObjectExtensions.SetFieldDynamic()";
            try
            {
                Type type = oSubj.GetType();
                FieldInfo fi = type.GetField(sFieldName);
                if (null == fi)
                {
                    return (
      Util.HandleAppErrOnce(typeof(DynamicObjectExtensions), fn, "Field '" + sFieldName + "' not found in type " + type.Name));
                }
                fi.SetValue(oSubj, oValue);
                return (true);

            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(DynamicObjectExtensions), fn, exc);
                return (false);
            }

        }
    }


    /// <summary>
    /// Extension methods for List[object] or IEnumerable[object].
    /// </summary>
    public static class ObjectListExtensions
    {

        public static List<object> GetDistinctValues(this IEnumerable<object> oTargetList, FieldInfo oField)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            Type type = typeof(ObjectListExtensions);
            List<object> retlist = new List<object>();
            try
            {
                SortedDictionary<object, object> lut = new SortedDictionary<object, object>();
                foreach (object obj in oTargetList)
                {
                    object val = oField.GetValue(obj);
                    if (null == val) { continue; }
                    if (!lut.ContainsKey(val)) { lut.Add(val, obj); }
                }
                retlist.AddRange(lut.Keys);
                return (retlist);
            }
            catch (Exception exc)
            {
                Util.HandleExc(type, fn, exc);
                return (retlist);
            }
        }
    }
}
