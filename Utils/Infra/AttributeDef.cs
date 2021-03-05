using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Levrum.Utils.Infra
{

    public interface IValidatable
    {
        List<string> GetValidationErrors();
    }


    public class AttributeDef : IValidatable
    {

        public AttributeDef()
        {

        }

        /// <summary>
        /// This function retrieves attribute definition data relevant to a specific type.
        /// </summary>
        /// <param name="oType"></param>
        /// <returns></returns>
        public static List<AttributeDef> GetAttributesForType(Type oType,string sRelatesTo)
        {
            const string fn = "AttributeDef.GetAttributesForType()";
            List<AttributeDef> retlist = new List<AttributeDef>();
            try
            {
                int index = 0;
                foreach (FieldInfo fi in oType.GetFields())
                {
                    if (!fi.IsPublic) { continue;  }
                    Type ftype = fi.FieldType;
                    if (!AttributeDef.IsValidAtomicType(ftype)) { continue;  }
                    AttributeDef adef = new AttributeDef(oType, fi);
                    adef.DataType = fi.FieldType;
                    adef.Ordinal = index++;
                    if (!string.IsNullOrEmpty(sRelatesTo)) { adef.RelatesTo = sRelatesTo; }
                    retlist.Add(adef);
                }

                foreach (PropertyInfo pi in oType.GetProperties())
                {
                    if (!(pi.GetMethod.IsPublic&&pi.SetMethod.IsPublic)) { continue;  }
                    Type ptype = pi.PropertyType;
                    if (!AttributeDef.IsValidAtomicType(ptype)) { continue;  }
                    AttributeDef adef = new AttributeDef(oType, pi);
                    adef.Ordinal = index++;
                    if (!string.IsNullOrEmpty(sRelatesTo)) { adef.RelatesTo = sRelatesTo; }
                    retlist.Add(adef);
                }

                return (retlist);
            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(AttributeDef), fn, exc);
                return (retlist);
            }
        }

        private static bool IsValidAtomicType(Type oType)
        {
            bool type_ok =
                (typeof(Int64).IsAssignableFrom(oType)) ||
                (typeof(string).IsAssignableFrom(oType)) ||
                (typeof(double).IsAssignableFrom(oType)) ||
                (typeof(DateTime).IsAssignableFrom(oType));
            return (type_ok);
        }

        public AttributeDef(string sName, Type oType, int iOrdinal)
        {
            Name = sName;
            DataType = oType;
            Ordinal = iOrdinal;

        }

        public AttributeDef(Type oType, MemberInfo oMemberInfo)
        {
            m_oDeclaringType = oType;
            m_oMember = oMemberInfo;
            Name = oType.Name + "." + oMemberInfo.Name;
            FieldInfo fi = oMemberInfo as FieldInfo;
            if (null!=fi) { DataType = fi.FieldType;  }
            PropertyInfo pi = oMemberInfo as PropertyInfo;
            if (null!=pi) { DataType = pi.PropertyType; }

        }

        public AttributeDef(string sFieldName, Type oType, string sRelatesTo, bool bIsRequired)
        {
            this.Name = sFieldName;
            this.DataType = oType;
            this.RelatesTo = sRelatesTo;
            this.IsRequired = bIsRequired;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            //sb.Append(Ordinal.ToString().PadLeft(3, ' ') + ".) ");
            sb.Append(Name);
            string stype = DataType.Name;
            sb.Append(" [" + stype + "]");
            sb.Append("   Relates to: " + RelatesTo);
            if (!string.IsNullOrEmpty(Description)) { sb.Append("\r\n   " + Description);  }
            return (sb.ToString());
        }

        /// <summary>
        /// Can this attribute be assigned from another?   Checks for type compatibility ...
        /// e.g., string is assignable from everyting, double from any int, etc.
        /// </summary>
        /// <param name="oRhs"></param>
        /// <returns></returns>
        public bool CanBeAssignedFrom(AttributeDef oRhs)
        {
            const string fn = "AttributeDef.CanBeAssignedFrom()";
            try
            {
                if (null == oRhs) { return (false); }
                if (DataType.IsAssignableFrom(typeof(string))) { return (true); }
                if (DataType.IsAssignableFrom(oRhs.DataType)) { return (true); }
                if (typeof(double).Name == DataType.Name)
                {
                    if (typeof(Int64).IsAssignableFrom(oRhs.DataType)) { return (true); }
                }
                if (typeof(Int64).IsAssignableFrom(DataType))
                {
                    if (typeof(Int64).IsAssignableFrom(oRhs.DataType)) { return (true); }
                    if (typeof(double).IsAssignableFrom(oRhs.DataType)) { return (true); }
                }
                if (typeof(Int32).IsAssignableFrom(DataType))
                {
                    if (typeof(Int32).IsAssignableFrom(oRhs.DataType)) { return (true); }
                    if (typeof(Int64).IsAssignableFrom(oRhs.DataType)) { return (true); }
                }
                return (false);

            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }

        public static List<AttributeDef> SelectAttsWithMatchingTypes(List<AttributeDef> oAdefs, Type oDestType)
        {
            const string fn = "AttributeDef.SelectAttsWithMatchingtypes()";
            List<AttributeDef> retlist = new List<AttributeDef>();
            try
            {
                Type[][] valid_type_matrix = new Type[][]
                    {
                        new Type[] { typeof(DateTime), typeof(DateTime) },
                        new Type[] { typeof(Int64), typeof(Int64), typeof(int), typeof(Int32) },
                        new Type[] { typeof(int), typeof(int), typeof(Int32) },
                        new Type[] { typeof(double), typeof(double), typeof(Int64), typeof(int), typeof(Int32)},
                        new Type[] { typeof(string), typeof(string), typeof(int), typeof(DateTime), typeof(double), typeof(Int64)}
                    };

                Type[] dest_vector = null;
                foreach (Type[] dv in valid_type_matrix)
                {
                    if (oDestType.IsAssignableFrom(dv[0]))
                    {
                        dest_vector = dv;
                        break;
                    }
                }


                foreach (AttributeDef adef in oAdefs)
                {
                    for (int i = 1; i < dest_vector.Length; i++)
                    {
                        if (dest_vector[i].IsAssignableFrom(adef.DataType))
                        {
                            retlist.Add(adef);
                            break;
                        } // endif(one of the source types matched the adef's type)
                    } // end for(i=destination type vector index)

                } // end foreach(attribute definition)

                return (retlist);
            }
            catch(Exception exc)
            {
                Util.HandleExc(typeof(AttributeDef), fn, exc);
                return (retlist);
            }
        }

        public string Name = "";

        public string Description = "";
        public Type DataType
        {
            get
            {
                if ((null == m_oDataType) && (!string.IsNullOrEmpty(DataTypeName)))
                {
                    m_oDataType = Type.GetType(DataTypeName);
                }
                return (m_oDataType);
            }
            set
            {
                m_oDataType = value;
                DataTypeName = (null!=m_oDataType) ? m_oDataType.FullName : "";
            }
        }
        public string DataTypeName = "";
        private Type m_oDataType = null;
        public int Ordinal = -1;
        public string RelatesTo = "Call";  // can also be "Response";
        
        private Type m_oDeclaringType = null;
        private MemberInfo m_oMember = null;

        public bool IsRequired = false;

        /// <summary>
        /// Cached string value.  This can be retrieved with the GetCachedValue{<>}() operator.
        /// </summary>
        public string CachedStringValue { get; internal set; }
        public string CsFieldName
        {
            get
            {
                return(Util.MakeFieldName(this.Name));
            }
        }



        public string GetCachedValue()
        {
            return (CachedStringValue);
        }

        public T GetCachedValue<T>()
        {
            const string fn = "Attributedef.GetCachedValue<T>()";
            try
            {
                if (string.IsNullOrEmpty(CachedStringValue)) { return (default(T)); }
                if (typeof(DateTime).IsAssignableFrom(typeof(T)))
                {
                    DateTime dtm = DateTime.MinValue;
                    if (!DateTime.TryParse(CachedStringValue, out dtm)) { return (default(T)); }
                    return ((T)(object)dtm);
                }
                else if (typeof(double).IsAssignableFrom(typeof(T)))
                {
                    double d = double.MinValue;
                    if (!double.TryParse(CachedStringValue, out d)) { return (default(T)); }
                    return ((T)(object)d);
                }
                else if (typeof(int).IsAssignableFrom(typeof(T)))
                {
                    int i = int.MinValue;
                    if (!int.TryParse(CachedStringValue, out i)) { return (default(T)); }
                    return ((T)(object)i);
                }
                else if (typeof(string).IsAssignableFrom(typeof(T)))
                {
                    return ((T)(object)CachedStringValue);
                }
                else
                {
                    Util.HandleAppErrOnce(this, fn, "Unable to marshal type " + typeof(T).Name + " for field " + this.Name);
                    return (default(T));
                }
            } // end main try

            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (default(T));
            }
        }


        public virtual bool GetValue(List<string> oFieldValues, out object oResult)
        {
            const string fn = "AttributeDef.GetValue()";
            oResult = null;
            try
            {
                string sval = GetStringVal(oFieldValues);
                if (null == sval) { return (false); }
                if (typeof(DateTime).IsAssignableFrom(DataType))
                {
                    DateTime dtm;
                    if (!DateTime.TryParse(sval, out dtm)) { return (false); }
                    oResult = dtm;
                    return (true);
                }
                else if (typeof(Int64).IsAssignableFrom(DataType))
                {
                    Int64 i;
                    if (!Int64.TryParse(sval, out i)) { return (false); }
                    oResult = i;
                    return (true);
                }
                else if (typeof(double).IsAssignableFrom(DataType))
                {
                    double d = 0.0;
                    if (!double.TryParse(sval, out d)) { return (false); }
                    oResult = d;
                    return (true);
                }
                else if (typeof(string).IsAssignableFrom(DataType))
                {
                    oResult = sval;
                    return (true);
                }
                else
                {
                    Util.HandleAppErrOnce(this,fn,"Type is not supported: " + DataType.Name);
                    return(false);
                }
            }
            catch (Exception exc)
            {
                Util.HandleExc(this,fn,exc);
                return(false);
            }
        }


        public string GetStringVal(List<string> oFields)
        {
            if (null == oFields) { return (""); }
            if ((Ordinal < 0) || (Ordinal >= oFields.Count)) { return (""); }
            return (oFields[Ordinal]);

        }

        public List<string> GetValidationErrors()
        {
            List<string> retlist = new List<string>();
            if (string.IsNullOrEmpty(Name)) { retlist.Add("Attribute name is required"); }
            if (null==DataType) { retlist.Add("Data type is required"); }
            return (retlist);
        }
    } // end class Attributedef
} // end namespace