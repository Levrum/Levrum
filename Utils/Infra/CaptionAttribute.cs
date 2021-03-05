using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Levrum.Utils.Infra
{
    /// <summary>
    /// Attribute tagging caption of a field or class.  Synax:  [Caption("my caption goes here")].  Captions
    /// often show up in the UI, so be clear, be nice and be brief!
    /// </summary>
    public class CaptionAttribute : Attribute
    {
        public CaptionAttribute(String sCaption)
        {
            Caption = sCaption;
        }
        public String Caption = "";


        public static String Get(Type oType)
        {
            if (null == oType) { return (""); }
            object[] atts = oType.GetCustomAttributes(typeof(CaptionAttribute), false);
            if ((null == atts) || (0 == atts.Length))
            {
                return (oType.Name);
            }
            CaptionAttribute attrib_instance = atts[0] as CaptionAttribute;
            if (null == attrib_instance) { return (oType.Name); }
            return (attrib_instance.Caption);
        }

        public static void ClearCaptionLookUp()
        {
            m_oFieldCaptionLookup.Clear();
        }


        public static bool Set(Type oDefType, string sField, string sNewValue)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                FieldInfo fi = oDefType.GetField(sField);
                if (null == fi) { return (Util.HandleAppErrOnce(typeof(CaptionAttribute), fn, "Field '" + sField + "' is not defined on type " + oDefType.Name)); }
                return (Set(fi, sNewValue));
            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(CaptionAttribute), fn, exc);
                return (false);
            }
        }

        /// <summary>
        /// Dynamically set the caption on a field at runtime.  Hopefully.
        /// </summary>
        /// <param name="oField"></param>
        /// <param name="sNewValue"></param>
        /// <returns></returns>
        public static bool Set(FieldInfo oField, string sNewValue)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            Type type = typeof(CaptionAttribute);
            try
            {
                string fname = oField.Name;
                Type deftype = oField.DeclaringType;
                string fullname = deftype.FullName + "." + fname;
                lock (m_oFieldCaptionLookup)
                {
                    if (!m_oFieldCaptionLookup.ContainsKey(fullname)) { m_oFieldCaptionLookup.Add(fullname, sNewValue); }
                    else { m_oFieldCaptionLookup[fullname] = sNewValue; }
                }
                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(type, fn, exc);
                return (false);
            }
        }

        /// <summary>
        /// Lookup tables for caption "overrides" for specific types and fields.
        /// </summary>
        private static SortedDictionary<string, string> m_oFieldCaptionLookup = new SortedDictionary<string, string>();         // jpr-MT2
        private static SortedDictionary<string, string> m_oTypeCaptionLookup = new SortedDictionary<string, string>();          // jpr-MT2


        /// <summary>
        /// Get the caption attribute for a type.
        /// </summary>
        /// <param name="oInfo"></param>
        /// <returns></returns>
        public static String Get(MemberInfo oInfo)
        {
            if (null == oInfo) return ("");
            object[] atts = oInfo.GetCustomAttributes(typeof(CaptionAttribute), false);
            if ((null == atts) || (0 == atts.Length))
            {
                return (oInfo.Name);
            }

            CaptionAttribute catt = atts[0] as CaptionAttribute;
            if (null == catt) return (oInfo.Name);

            return (catt.Caption);
        } // end Get(Type)



        /// <summary>
        /// Get the caption attribute for a type.
        /// </summary>
        /// <param name="oInfo"></param>
        /// <returns></returns>
        public static String Get(ParameterInfo oInfo)
        {
            if (null == oInfo) return ("");
            object[] atts = oInfo.GetCustomAttributes(typeof(CaptionAttribute), false);
            if ((null == atts) || (0 == atts.Length))
            {
                return (oInfo.Name);
            }

            CaptionAttribute catt = atts[0] as CaptionAttribute;
            if (null == catt) return (oInfo.Name);

            return (catt.Caption);
        } // end Get(Type)


        /// <summary>
        /// Get the caption associated with a field.
        /// </summary>
        /// <param name="oFldInfo"></param>
        /// <returns></returns>
        public static String Get(FieldInfo oFldInfo)
        {
            if (null == oFldInfo) return ("");

            // 2013124 CDN - check for 'overrides' (set by the Set() method on a FieldInfo):
            string fname = oFldInfo.Name;
            Type deftype = oFldInfo.DeclaringType;
            string fullname = deftype.FullName + "." + fname;
            if (m_oFieldCaptionLookup.ContainsKey(fullname))
            {
                return (m_oFieldCaptionLookup[fullname]);
            }


            object[] atts = oFldInfo.GetCustomAttributes(typeof(CaptionAttribute), false);
            if ((null == atts) || (0 == atts.Length))
            {
                return (oFldInfo.Name);
            }

            CaptionAttribute catt = atts[0] as CaptionAttribute;
            if (null == catt) return (oFldInfo.Name);

            return (catt.Caption);

        }

    } // end class
}
