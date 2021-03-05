using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Levrum.Utils.Infra
{

    public class PersisterAscii
    {
        public PersisterAscii()
        {
        }


        /// <summary>
        /// Delimiter characters, in decreasing order of significance.
        /// </summary>
        public String DelimiterChars = "|^~";

        /// <summary>
        /// Allows saving homogeneous lists of objects to a CSV file.   Forces overwrite.
        /// </summary>
        /// <param name="sCsvFile"></param>
        /// <param name="oStuffToSave"></param>
        /// <param name="bAppend"></param>
        /// <returns></returns>
        public static bool SaveAsCsv(String sCsvFile, IEnumerable oStuffToSave)
        {
            return (SaveAsCsv(sCsvFile, oStuffToSave, false));
        }



        /// <summary>
        /// Allows saving stuff to CSV with the option to append.
        /// </summary>
        /// <param name="sCsvFile"></param>
        /// <param name="oStuffToSave"></param>
        /// <param name="bAppend"></param>
        /// <returns></returns>
        public static bool SaveAsCsv(String sCsvFile, IEnumerable oStuffToSave, bool bAppend)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            StreamWriter sw = null;
            try
            {
                // Get the 0'th element and errorcheck:
                Object obj0 = null;
                foreach (object obj in oStuffToSave)
                {
                    obj0 = obj;
                    break;
                }
                if (null == obj0) { return (Util.HandleAppErr(typeof(PersisterAscii), fn, "Nothing to save")); }

                // Set up field list and header record:
                Type type = obj0.GetType();
                List<FieldInfo> fields = new List<FieldInfo>();
                StringBuilder sb = new StringBuilder();
                int nfields = 0;
                foreach (FieldInfo fi in type.GetFields())
                {
                    if (!fi.IsPublic) { continue; }
                    if (fi.IsStatic) { continue; }
                    if (!Util.IsScalarType(fi.FieldType)) { continue; }
                    fields.Add(fi);
                    if (nfields > 0) { sb.Append(","); }
                    sb.Append(Util.Csvify(CaptionAttribute.Get(fi)));
                    nfields++;
                } // end foreach(field)

                // Open file and write header record (if file didn't previously exist):
                bool previously_existed = File.Exists(sCsvFile);
                sw = new StreamWriter(sCsvFile, bAppend);
                if (!previously_existed) { sw.WriteLine(sb.ToString()); }

                // Write data records:
                foreach (Object obj in oStuffToSave)
                {
                    sb.Clear();
                    nfields = 0;
                    foreach (FieldInfo fi in fields)
                    {
                        object val = fi.GetValue(obj);
                        if (nfields > 0) { sb.Append(","); }
                        sb.Append(Util.Csvify(val));
                        nfields++;
                    } // end foreach(field)
                    sw.WriteLine(sb.ToString());
                } // end foreach(object)

                // We're done (close the file in the finally block):
                return (true);

            } // end main try
            catch (Exception exc)
            {
                Util.HandleExc(typeof(PersisterAscii), fn, exc);
                return (false);
            }
            finally
            {
                if (null != sw) { sw.Close(); }
            }
        }


        /// <summary>
        /// Save objects to an ASCII file.
        /// </summary>
        /// <param name="sFile"></param>
        /// <param name="oContents"></param>
        /// <returns></returns>
        public virtual bool Save(String sFile, IEnumerable oContents)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(sFile, false);

                // Step 1 -- find all classes:
                Dictionary<Type, List<object>> type_lut = new Dictionary<Type, List<object>>();
                foreach (Object obj in oContents)
                {
                    Type type = obj.GetType();


                    if (!type_lut.ContainsKey(type))
                    {
                        // Make sure the default constructor works!
                        ConstructorInfo ci = type.GetConstructor(new Type[] { });
                        if (null == ci)
                        {
                            Util.HandleAppErr(this, fn, "Type " + type.Name + " does not have a default constructor;  cannot be saved.");
                            return (false);
                        }
                        object test_inst = ci.Invoke(new object[] { });
                        if ((null == test_inst) || (!type.IsAssignableFrom(test_inst.GetType())))
                        {
                            Util.HandleAppErr(this, fn, "Unable to create a valid instance of " + type.Name + " via default constructor");
                            return (false);
                        }

                        type_lut.Add(type, new List<object>());
                    }
                    type_lut[type].Add(obj);
                }

                // Step 2: write to serialiation file --
                foreach (Type type in type_lut.Keys)
                {
                    sw.WriteLine(SerializeType(type));
                    foreach (Object inst in type_lut[type])
                    {
                        sw.WriteLine(SerializeInst(type, inst));
                    }
                }


                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
            finally
            {
                if (null != sw) { sw.Close(); }
            }
        } // end method()




        /// <summary>
        /// Serialize a single instance to a string.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="inst"></param>
        /// <returns></returns>
        public string SerializeInst(Type type, object inst)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                String d0 = DelimiterChars.Substring(0, 1);
                String d1 = DelimiterChars.Substring(1, 1);
                StringBuilder sb = new StringBuilder();
                sb.Append("Instance" + d0 + type.Name + d0);


                foreach (FieldInfo fi in type.GetFields())
                {

                    if (!IsValidField(fi)) { continue; }
                    object val = fi.GetValue(inst);
                    String sval = "";
                    if (null != val) { sval = val.ToString(); }
                    if (sval.Contains(d0))
                    {
                        Util.HandleAppErrOnce(this, fn, "Value of " + fi.Name + " contains delimiter: " + sval);
                        sval = "";
                    }
                    sb.Append(sval + d0);

                }
                return (sb.ToString());
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return ("");
            }
        }


        /// <summary>
        /// Serialize a type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string SerializeType(Type type)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                StringBuilder sb = new StringBuilder();
                String d0 = DelimiterChars.Substring(0, 1);
                String d1 = DelimiterChars.Substring(1, 1);
                sb.Append("Type" + d0);
                sb.Append(type.Name + d0);
                sb.Append(type.AssemblyQualifiedName + d0);
                foreach (FieldInfo fi in type.GetFields())
                {
                    if (!IsValidField(fi)) { continue; }
                    sb.Append(fi.Name + d1 + fi.FieldType.FullName + d0);
                }
                return (sb.ToString());
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return ("");
            }
        }


        /// <summary>
        /// Is a specific field one that can be loaded?
        /// </summary>
        /// <param name="fi"></param>
        /// <returns></returns>
        private bool IsValidField(FieldInfo fi)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                if (!fi.IsPublic) { return (false); }
                Type ftype = fi.FieldType;
                if (typeof(int).IsAssignableFrom(ftype)) { return (true); }
                if (typeof(long).IsAssignableFrom(ftype)) { return (true); }
                if (typeof(double).IsAssignableFrom(ftype)) { return (true); }
                if (typeof(decimal).IsAssignableFrom(ftype)) { return (true); }
                if (typeof(DateTime).IsAssignableFrom(ftype)) { return (true); }
                if (typeof(Enum).IsAssignableFrom(ftype)) { return (true); }
                if (typeof(String).IsAssignableFrom(ftype)) { return (true); }
                return (false);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }

        public virtual bool Load(String sFile, List<Object> oContents)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            StreamReader sr = null;
            try
            {
                sr = new StreamReader(sFile);

                String srec = null;
                Type curtype = null;
                int errors = 0;
                while (null != (srec = sr.ReadLine()))
                {
                    if (srec.StartsWith("Type"))
                    {
                        curtype = DeSerializeType(srec);
                        if (null == curtype) { errors++; }
                    }
                    else if (srec.StartsWith("Instance"))
                    {
                        if (null == curtype) { continue; }
                        object inst = DeSerializeInst(curtype, srec);
                        if (null != inst) { oContents.Add(inst); }
                        else { errors++; }
                    }
                }

                return (0 == errors);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (true);
            }
            finally
            {
                if (null != sr) { sr.Close(); }
            }
        }



        /// <summary>
        /// De-serialize a single instance.
        /// </summary>
        /// <param name="oType"></param>
        /// <param name="sRec"></param>
        /// <returns></returns>
        public object DeSerializeInst(Type oType, string sRec)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                Object instance = null;
                String d0 = DelimiterChars.Substring(0, 1);
                String[] pcs = sRec.Split(d0.ToCharArray());
                if (pcs.Length < 2)
                {
                    Util.HandleAppErr(this, fn, "Invalid instance record: " + sRec);
                    return (null);
                }
                String typename = pcs[1];
                if (oType.Name != typename)
                {
                    Util.HandleAppErr(this, fn, "Instance record type mismatch: expected " + oType.Name + ", found " + typename);
                    return (null);
                }

                // Construct the instance:
                ConstructorInfo ci = oType.GetConstructor(new Type[] { });
                if (null == ci)
                {
                    Util.HandleAppErrOnce(this, fn, "Default constructor unavailable for type " + oType.Name + ";  removed since data was saved?");
                    return (null);
                }
                instance = ci.Invoke(new object[] { });

                // Loop through values and assign to fields:
                int nhdr_fields = 2;
                int ndata_fields = (pcs.Length - nhdr_fields) - 1;   // (-1) because last piece is null.
                for (int i = 0; i < ndata_fields; i++)
                {
                    int field_index = i;
                    int data_index = i + nhdr_fields;
                    String sval = pcs[data_index];
                    if (field_index >= m_oFieldCache.Length)
                    {
                        Util.HandleAppErrOnce(this, fn, "Too many fields in instance record for type " + oType.Name);
                        break;
                    }
                    FieldInfo fi = m_oFieldCache[field_index];
                    if (null == fi)
                    {
                        Util.HandleAppErrOnce(this, fn, "Field " + field_index + " in type " + oType + " is undefined; ignoring");
                        continue;
                    }

                    SetFieldValue(instance, fi, sval);

                }

                return (instance);
            } // end main try()
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (null);
            }
        }

        /// <summary>
        /// Set the value for a specific field from a string representation, using appropriate
        /// conversion for field type.    Supports strings, numeric types, DateTime and enums.
        /// </summary>
        /// <param name="oInstance"></param>
        /// <param name="fi"></param>
        /// <param name="sval"></param>
        private bool SetFieldValue(object oInstance, FieldInfo oField, string sVal)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                Type ftype = oField.FieldType;
                String fname = oField.Name;
                object oval = null;
                if (typeof(int).IsAssignableFrom(ftype))
                {
                    int ival = 0;
                    if (!Int32.TryParse(sVal, out ival)) { return (Util.HandleAppErrOnce(this, fn, "Invalid integer(s) value in field " + fname + ": " + sVal)); }
                    oval = ival;
                }
                else if (typeof(long).IsAssignableFrom(ftype))
                {
                    long lval = 0L;
                    if (!long.TryParse(sVal, out lval)) { return (Util.HandleAppErrOnce(this, fn, "Invalid longword value in field " + fname + ": " + sVal)); }
                    oval = lval;
                }
                else if (typeof(double).IsAssignableFrom(ftype))
                {
                    double dval = 0.0;
                    if (!Double.TryParse(sVal, out dval)) { return (Util.HandleAppErrOnce(this, fn, "Invalid double(s) in field " + fname + ": " + sVal)); }
                    oval = dval;
                }
                else if (typeof(decimal).IsAssignableFrom(ftype))
                {
                    decimal dval = 0.0M;
                    if (!Decimal.TryParse(sVal, out dval)) { return (Util.HandleAppErrOnce(this, fn, "Invalid decimal(s) in field " + fname + ": " + sVal)); }
                    oval = dval;
                }
                else if (typeof(DateTime).IsAssignableFrom(ftype))
                {
                    DateTime dtm = DateTime.MinValue;
                    if (!DateTime.TryParse(sVal, out dtm)) { return (Util.HandleAppErrOnce(this, fn, "Invalid date/time(s) in field " + fname + ": " + sVal)); }
                    oval = dtm;
                }
                else if (typeof(Enum).IsAssignableFrom(ftype))
                {
                    oval = Enum.Parse(ftype, sVal);
                }
                else if (typeof(String).IsAssignableFrom(ftype))
                {
                    oval = sVal;
                }
                else
                {
                    Util.HandleAppErrOnce(this, fn, "Type " + ftype.Name + " is not a supported atomic type.");
                    return (false);
                }

                oField.SetValue(oInstance, oval);
                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        } // end method()


        /// <summary>
        /// De-serialize a type record into a type instance.
        /// </summary>
        /// <param name="sRec"></param>
        /// <returns></returns>
        public Type DeSerializeType(string sRec)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                String d0 = DelimiterChars.Substring(0, 1);
                String[] pcs = sRec.Split(d0.ToCharArray());
                if (pcs.Length < 3)
                {
                    Util.HandleAppErr(this, fn, "Invalid type record: " + sRec);
                    return (null);
                }
                Type type = Type.GetType(pcs[2]);       // resolve type from assembly-qualified name
                if (null == type)
                {
                    Util.HandleAppErr(this, fn, "Unknown type '" + pcs[1] + "':  " + pcs[2]);
                    return (null);
                }


                // Cache fields from serialized metadata; ensure that they match the current definition of the type.
                int nhdr_fields = 3;
                String d1 = DelimiterChars.Substring(1, 1);
                int nfields = pcs.Length - nhdr_fields;
                m_oFieldCache = new FieldInfo[nfields];
                for (int i = 0; i < nfields; i++) { m_oFieldCache[i] = null; }
                for (int i = nhdr_fields; i < pcs.Length - 1; i++)
                {
                    int field_index = i - nhdr_fields;
                    if (String.IsNullOrEmpty(pcs[i]))
                    {
                        Util.HandleAppErrOnce(this, fn, "Field " + field_index + " is undefined for type " + type);
                        continue;
                    }
                    String fielddef = pcs[i];
                    String[] subpieces = fielddef.Split(d1.ToCharArray());
                    if (2 > subpieces.Length)
                    {
                        Util.HandleAppErrOnce(this, fn, "Ill-formed field definition: " + fielddef + " in type " + type.Name);
                        return (null);
                    }
                    String fname = subpieces[0];
                    String ftype = subpieces[1];
                    FieldInfo fi = type.GetField(fname);
                    if (null == fi)
                    {
                        Util.HandleAppErrOnce(this, fn, "Field " + fname + " no longer found in type " + type.Name + "; will be ignored.");
                    }
                    else if (fi.FieldType.FullName != ftype)
                    {
                        Util.HandleAppErrOnce(this, fn, "Type mismatch for field " + fname + ": expected " + ftype + ", found " + fi.FieldType.Name);
                    }
                    else
                    {
                        m_oFieldCache[field_index] = fi;
                    }
                }

                return (type);

            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (null);
            }
        }




        private FieldInfo[] m_oFieldCache = null;
    } // end class
}
