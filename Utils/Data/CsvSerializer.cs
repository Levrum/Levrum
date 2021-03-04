using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Text;

using Levrum.Utils.Stats;

namespace Levrum.Utils.Data
{
    public class CsvSerializer
    {
        /// <summary>
        /// Allows saving stuff to CSV with the option to append.
        /// </summary>
        /// <param name="sCsvFile"></param>
        /// <param name="oStuffToSave"></param>
        /// <param name="bAppend"></param>
        /// <returns></returns>
        public static bool SaveObjectsAsCsv(String sCsvFile, IEnumerable oStuffToSave, bool bAppend)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            StreamWriter sw = null;
            Type dtype = typeof(CsvAnalyzer);
            try
            {
                // Get the 0'th element and errorcheck:
                Object obj0 = null;
                foreach (object obj in oStuffToSave)
                {
                    obj0 = obj;
                    break;
                }
                if (null == obj0) { return (LogHelper.HandleAppErr(dtype, fn, "Nothing to save")); }

                // Set up field list and header record:
                Type type = obj0.GetType();
                List<FieldInfo> fields = new List<FieldInfo>();
                StringBuilder sb = new StringBuilder();
                int nfields = 0;
                foreach (FieldInfo fi in type.GetFields())
                {
                    if (!fi.IsPublic) { continue; }
                    if (fi.IsStatic) { continue; }
                    if (!fi.FieldType.IsScalarType()) { continue; }
                    fields.Add(fi);
                    if (nfields > 0) { sb.Append(","); }
                    //sb.Append(Csvify(CaptionAttribute.Get(fi))); // this might be nice to port someday
                    sb.Append(Csvify(fi.Name));   // temporary solution
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
                        sb.Append(Csvify(val));
                        nfields++;
                    } // end foreach(field)
                    sw.WriteLine(sb.ToString());
                } // end foreach(object)

                // We're done (close the file in the finally block):
                return (true);

            } // end main try
            catch (Exception exc)
            {
                LogHelper.HandleExc(dtype, fn, exc);
                return (false);
            }
            finally
            {
                if (null != sw) { sw.Close(); }
            }
        }


        /// <summary>
        /// Save a collection of ExpandoObjects to a CSV file
        /// </summary>
        /// <param name="sCsvFile"></param>
        /// <param name="oStuffToSave"></param>
        /// <param name="bAppend"></param>
        /// <returns></returns>
        public static bool SaveExpandosAsCsv(String sCsvFile, IEnumerable<ExpandoObject> oStuffToSave, bool bAppend)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            StreamWriter sw = null;
            Type dtype = typeof(CsvAnalyzer);
            try
            {

                Dictionary<string, int> field_lut = new Dictionary<string, int>();
                int nseen = 0;
                foreach (ExpandoObject expando in oStuffToSave)
                {
                    nseen++;
                    IDictionary<string, object> exdict = expando as IDictionary<string, object>;
                    foreach (string skey in exdict.Keys)
                    {
                        object val = exdict[skey];
                        if ((null != val) && (!val.GetType().IsScalarType())) { continue; } // skip collections and compound objects
                        if (!field_lut.ContainsKey(skey)) { field_lut.Add(skey, field_lut.Count); }
                    }
                    if (nseen > 500) { break; }
                }

                // field_lut now contains all keys seen in the first N records,  with order of appearance; re-sort by order so the
                // header and data records will be consistent:
                string[] ordered_keys = new string[field_lut.Count];
                foreach (string skey in field_lut.Keys) { ordered_keys[field_lut[skey]] = skey; }

                // Build the header record:
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < ordered_keys.Length; i++)
                {
                    if (i > 0) { sb.Append(","); }
                    sb.Append(Csvify(ordered_keys[i]));
                }


                // Open file and write header record (if file didn't previously exist):
                bool previously_existed = File.Exists(sCsvFile);
                sw = new StreamWriter(sCsvFile, bAppend);
                if ((!previously_existed) || (!bAppend)) { sw.WriteLine(sb.ToString()); }

                // Write data records:
                foreach (ExpandoObject obj in oStuffToSave)
                {
                    sb.Clear();
                    for (int i = 0; i < ordered_keys.Length; i++)
                    {
                        if (i > 0) { sb.Append(","); }
                        string sfld = ordered_keys[i];
                        object oval = "";
                        IDictionary<string, object> dict = obj as IDictionary<string, object>;
                        if ((null != dict) && (dict.ContainsKey(sfld))) { oval = dict[sfld]; }
                        string sval = Csvify(oval);
                        sb.Append(sval);
                    }
                    string srec = sb.ToString();
                    sw.WriteLine(srec);
                } // end foreach(object)

                // We're done (close the file in the finally block):
                return (true);

            } // end main try
            catch (Exception exc)
            {
                LogHelper.HandleExc(dtype, fn, exc);
                return (false);
            }
            finally
            {
                if (null != sw) { sw.Close(); }
            }
        } // end method()



        /// <summary>
        /// Turn a datum into a string suitable for writing to a CSV file.
        /// </summary>
        /// <param name="oData"></param>
        /// <returns></returns>
        public static string Csvify(Object oData)
        {
            if (null == oData) return ("");
            Type t = oData.GetType();
            if (typeof(int).IsAssignableFrom(t)) return (oData.ToString());
            if (typeof(double).IsAssignableFrom(t)) return (oData.ToString());
            String sdata = oData.ToString().TrimEnd();
            string sdata2 = "";
            foreach (char c in sdata)
            {
                if ('\"' == c) { sdata2 += "\"\""; } // escaping double quotes per RFC
                else { sdata2 += c; }        // otherwise copy the character verbatim
            }
            String sret = "\"" + sdata2 + "\"";
            return (sret);
        }



    } // end class{}
}
