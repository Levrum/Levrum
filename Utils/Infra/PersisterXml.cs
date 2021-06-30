using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Levrum.Utils.Infra
{

    /// <summary>
    /// The interface a custom persister needs to implement, in order to be
    /// registered with the Persister class.
    /// </summary>
    public interface ICustomPersister
    {
        bool XmlFromObj(PersisterXml persister, Object oSubj, XmlDocument oDoc, out XmlElement roElement);
        bool XmlToObj(PersisterXml persister, XmlElement oXml, out Object roSubj);
    }



    /// <summary>
    /// This class allows you to serialize objects to/from XML files, 
    /// assuming the objects adhere to certain rules.  Valid types include:
    ///   -- String, int, double and enum
    ///   -- Aggregate of valid-typed fields
    ///   -- List[T] where T is a valid type.
    /// One particular advantage of the class:  it handles the case where
    /// B inherits A, and you define a field as List[A], but stick a bunch of
    /// B's in it.   When this class reconstitutes the list, it will be typed
    /// as List[A], but will contain B instances with the proper fields.
    /// </summary>
    public class PersisterXml                                                       // jpr-MT
    {
        /// <summary>
        /// A globally-accessible singleton instance.   Static functions
        /// log errors, etc., here.
        /// </summary>
        public static PersisterXml Main = new PersisterXml();

        #region Static Functions
        /// <summary>
        /// Register a custom type.   You must implement a custom persistence handler
        /// for this type, in a separate class that implements ICustomPersister.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="oType"></param>
        /// <param name="oCustomPersister"></param>
        /// <returns></returns>
        public static bool RegisterType(Type oType,
                        ICustomPersister oCustomPersister)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                lock (m_oStaticLock)
                {
                    if (!m_oRegisteredTypes.ContainsKey(oType))                                 // jpr-MT (CDN Fixed)
                    {
                        m_oRegisteredTypes.Add(oType, oCustomPersister);                        // jpr-MT (CDN Fixed)
                    }
                    else
                    {
                        m_oRegisteredTypes[oType] = oCustomPersister;
                    }
                    return (true);
                }
            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(PersisterXml), fn, exc);
                return (false);
            }
        }

        /// <summary>
        /// Check all types derived from a specific base type within an assembly for Persister-
        /// validity.    Generate a text list of errors.
        /// </summary>
        /// <param name="oBaseType"></param>
        /// <param name="oAssembly"></param>
        /// <returns></returns>
        public static List<String> CheckAllDerivedTypesInAssembly(Type oBaseType, Assembly oAssembly)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            List<String> retlist = new List<string>();
            try
            {
                foreach (Type type in oAssembly.GetTypes())
                {
                    if (!type.IsSubclassOf(oBaseType)) continue;
                    List<String> cur_errs = new List<string>();
                    if (!IsValidType(type, cur_errs)) retlist.AddRange(cur_errs);
                }
                return (retlist);
            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(PersisterXml), fn, exc);
                retlist.Add("Exception in checker: " + exc.Message);
                return (retlist);
            }
        }

        /// <summary>
        /// Determines if a type is valid for persistence with this class.
        /// </summary>
        /// <param name="oType"></param>
        /// <returns></returns>
        public static bool IsValidType(Type oType)
        {
            return (IsValidType(oType, null));
        }
        public static bool IsValidType(Type oType, List<String> oErrMsgs)
        {
            bool result = false;
            bool pushed = false;

            Stopwatch sw1 = new Stopwatch();
            sw1.Start();

            lock (m_oStaticLock)
            {
                try
                {
                    // Protecting statics.  Do NOT want to extend to recursion below, to avoid deadly embraces.
                    // lock(m_oStaticLock)
                    {
                        Trace("Persister.IsValidType(" + oType + "): ", m_oValidationStack.Count);
                        if (m_oKnownGoodTypes.ContainsKey(oType))                                               // jpr-MT2 (CDN fixed)
                        {
                            Trace("  Previously known good...", m_oValidationStack.Count);
                            return (result = true);
                        }
                        if (m_oValidationStack.Contains(oType))                                                 // jpr-MT2 (CDN fixed)
                        {
                            Trace("  Found in stack...", m_oValidationStack.Count);
                            return (result = true);
                        }
                        m_oValidationStack.Push(oType);                                                         // jpr-MT2 (CDN fixed)
                        pushed = true;
                    }
                    if (IsValidAtomicType(oType, oErrMsgs)) return (result = true);
                    if (IsValidListType(oType)) return (result = true);
                    if (IsValidDictionaryType(oType)) return (result = true);
                    if (IsValidAggregateType(oType, oErrMsgs)) return (result = true);
                    if (IsKnownType(oType)) return (result = true);

                    if (null != oErrMsgs) oErrMsgs.Add("Type " + oType.Name + " is invalid.");
                    return (result = false);
                }
                catch (Exception exc)
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    if (null != oErrMsgs) oErrMsgs.Add("Exception: " + exc.Message);
                    return (result = Main.HandleExc(fn, exc));
                }
                finally
                {
                    if (pushed && m_oValidationStack.Count > 0) m_oValidationStack.Pop();                      // jpr-MT2 (CDN fixed)
                    if (result && (!m_oKnownGoodTypes.ContainsKey(oType)))
                    {
                        m_oKnownGoodTypes.Add(oType, typeof(PersisterXml));                                   // jpr-MT2 (CDN fixed)
                    }
                    Trace("  result=" + result, m_oValidationStack.Count);
                } // end finally

            } // end static lock

        } // end method()

        /// <summary>
        /// Determines whether the specified type is a valid aggregate type.
        /// </summary>
        /// <param name="oType"></param>
        /// <returns></returns>
        public static bool IsValidAggregateType(Type oType)
        {
            List<string> msgs = new List<string>();
            return (IsValidAggregateType(oType, msgs));
        }
        public static bool IsValidAggregateType(Type oType, List<String> oErrMsgs)
        {
            try
            {
                int valid_fieldcnt = 0;

                // Loop through fields; check each one and either accumulate
                // failures or return failure if no failure list supplied:
                bool is_ok = true;
                foreach (FieldInfo fi in oType.GetFields())
                {
                    if (!IsTargetField(fi)) continue;
                    Type ft = fi.FieldType;
                    if (!IsValidType(ft))
                    {
                        String fn = MethodBase.GetCurrentMethod().Name;
                        String serr = oType.Name + "." + fi.Name + " has invalid type: " + ft.Name;
                        if (null != oErrMsgs) oErrMsgs.Add(serr);
                        Util.HandleAppErr(typeof(PersisterXml), fn, serr);
                        is_ok = false;
                        if (null == oErrMsgs) return (false);
                    }
                    valid_fieldcnt++;
                }
                if (!is_ok) return (false);

                // If we haven't found ANY valid fields, it's a failure:
                if (valid_fieldcnt <= 0) return (false);

                // And if we've survived thus far, we're golden!
                return (true);
            }
            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                return (Main.HandleExc(fn, exc));
            }
        }

        // *WTF* No summary for this function
        public static bool IsValidListType(Type oType)
        {
            try
            {
                if (!typeof(IList).IsAssignableFrom(oType)) return (false);
                Type[] genargs = oType.GetGenericArguments();
                if (1 != genargs.Length) return (false);
                if (typeof(Object).Equals(genargs[0])) return (true);
                if (genargs[0].IsInterface) { return (true); }    // 20181123 CDN ... allow lists of interfaces as members
                return (IsValidType(genargs[0]));

            }
            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                return (Main.HandleExc(fn, exc));
            }
        }

        public static bool IsValidDictionaryType(Type oType)
        {
            try
            {
                if (!typeof(IDictionary).IsAssignableFrom(oType)) return (false);
                Type[] genargs = oType.GetGenericArguments();
                if (2 != genargs.Length) return (false);
                if (typeof(Object).Equals(genargs[0])) return (true);
                return (IsValidType(genargs[0]));

            }
            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                return (Main.HandleExc(fn, exc));
            }
        }

        /// <summary>
        /// Determines whether the specified type is globally registered as a type that
        /// can be custom-persisted.
        /// </summary>
        /// <param name="oType"></param>
        /// <returns></returns>
        public static bool IsKnownType(Type oType)
        {
            bool is_known = false;
            lock (m_oStaticLock)
            {
                is_known = m_oRegisteredTypes.ContainsKey(oType);
            }

            return (is_known);
        }

        /// <summary>
        /// Determines whether a type is a valid atomic type.
        /// </summary>
        /// <param name="oType"></param>
        /// <returns></returns>
        public static bool IsValidAtomicType(Type oType, List<String> oErrors)
        {
            // Note: if you enable additional atomic types here, be sure to handle them in XmlToAtom)_ and XmlFromAtom()

            if (typeof(String).Equals(oType)) return (true);
            if (typeof(int).Equals(oType)) return (true);
            if (typeof(double).Equals(oType)) return (true);
            if (oType.IsSubclassOf(typeof(Enum))) return (true);
            if (typeof(DateTime).IsAssignableFrom(oType)) return (true);
            if (typeof(Boolean).IsAssignableFrom(oType)) { return (true); }
            if (typeof(bool).IsAssignableFrom(oType)) { return (true); }
            if (null != oErrors) oErrors.Add("Invalid atomic type: " + oType.Name);                 // jpr-MT2
            return (false);
        }

        public HashSet<string> FieldsToIgnore = new HashSet<string>();

        /// <summary>
        /// Determines whether the field is a persistence target.  Client
        /// decides how to identify targets -- via attribute, namespace,
        /// all public, etc.
        /// </summary>
        /// <param name="oFldInfo"></param>
        /// <returns></returns>
        public static bool IsTargetField(FieldInfo oFldInfo)
        {
            if (oFldInfo.IsStatic) return (false);

            return (oFldInfo.IsPublic);     // kludge for now.   20081129 CDN.
        }

        public bool IsTargetField_InstanceFunction(FieldInfo fieldInfo)
        {
            if (fieldInfo.IsStatic)
                return false;

            if (this.FieldsToIgnore.Contains(fieldInfo.Name))
                return false;

            return fieldInfo.IsPublic;
        }

        public static void Trace(String sMsg)                                                       // jpr-MT2
        {
            //Trace(sMsg,0);
        }
        public static void Trace(String sMsg, int iIndent)                                          // jpr-MT2 (CDN fixed)
        {
            if (!ValidationTracing) return;
            String sindent = "";
            for (int i = 0; i < iIndent; i++) sindent += "  ";
        }
        #endregion // static functions

        #region Public Fields and Properties
        /// <summary>
        /// Latest message recorded.
        /// </summary>
        public String LastMessage = "";

        /// <summary>
        /// List of all messages recorded.
        /// </summary>
        public List<String> Messages = new List<string>();

        /// <summary>
        /// If an unknown field is encountered in the XML file and this flag is set to true, we ingore the field
        /// and continue to load the remainder of the object.
        /// </summary>
        public virtual bool IgnoreUnknownFieldsOnLoad { get; set; }
        #endregion

        #region Member Functions

        public PersisterXml()
        {
            this.IgnoreUnknownFieldsOnLoad = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ignoreUnknownFieldsOnLoad">Whether or not to ignore unknown fields.</param>
        public PersisterXml(bool ignoreUnknownFieldsOnLoad)
        {
            this.IgnoreUnknownFieldsOnLoad = ignoreUnknownFieldsOnLoad;
        }

        protected virtual bool HandleErr(String sContext, String sMsg)
        {
            Util.HandleAppErr(this, sContext, sMsg);
            ErrMsg("Application error at '" + sContext + "': " + sMsg);
            return (false);
        }

        protected virtual bool HandleExc(String sContext, Exception oExc)
        {
            Util.HandleExc(this, sContext, oExc);
            ErrMsg("Exception in " + sContext + ": " + oExc.Message + "\r\n" +
                oExc.StackTrace);
            return (false);
        }


        /// <summary>
        /// Record an error message.
        /// </summary>
        /// <param name="sMsg"></param>
        protected virtual void ErrMsg(String sMsg)
        {
            LastMessage = sMsg;
            Messages.Add(sMsg);

        }


        /// <summary>
        /// Convert an XML element to an object of the correct type, etc.
        /// This basically "reconstitutes" the output of XmlFromObj.
        /// </summary>
        /// <param name="oXml"></param>
        /// <returns></returns>
        public virtual Object XmlToObj(XmlElement oXml)
        {
            try
            {
                String sname = oXml.Name;
                if (ElementTagRoot == sname)
                {
                    if (1 != oXml.ChildNodes.Count)
                    {
                        String fn = MethodBase.GetCurrentMethod().Name;
                        HandleErr(fn, "Ill-formed '" + sname + "' element; "
                            + oXml.ChildNodes.Count + " child elements.");
                        return (null);
                    }
                    XmlElement elt0 = oXml.ChildNodes[0] as XmlElement;
                    if (null == elt0)
                    {
                        String fn = MethodBase.GetCurrentMethod().Name;
                        HandleErr(fn, "Unable to get first element of " + sname);
                        return (null);
                    }
                    return (XmlToObj(elt0));
                }
                else if (ElementTagAtom == sname)
                {
                    return (XmlToAtom(oXml));
                }
                else if (ElementTagList == sname)
                {
                    return (XmlToList(oXml));
                }
                else if (ElementTagDict == sname)
                {
                    return (XmlToDictionary(oXml));
                }
                else if (ElementTagAggr == sname)
                {
                    return (XmlToAggregate(oXml));
                }
                else if (ElementTagAref == sname)
                {
                    return (XmlToAggRef(oXml));
                }


                else
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, "Unrecognized element category: " + sname);
                    return (null);
                }

            }
            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                HandleExc(fn, exc);
                return (null);
            }

        }




        /// <summary>
        /// Convert XML to an atom (string, int, double ... enum in future).
        /// </summary>
        /// <param name="oXml"></param>
        /// <returns></returns>
        public virtual Object XmlToAtom(XmlElement oXml)
        {
            try
            {
                Type type = GetObjectType(oXml);
                if (null == type) return (null);

                String sval = oXml.InnerText;
                if (sval.Contains("&#"))
                {
                    sval = Util.XmlDecode(sval);
                }

                if (typeof(Int32).IsAssignableFrom(type))
                {
                    Int32 i32 = default(Int32);
                    if (!Int32.TryParse(sval, out i32))
                    {
                        String fn = MethodBase.GetCurrentMethod().Name;
                        HandleErr(fn, "Ill-formed integer: " + sval);
                        return (null);
                    }
                    return (i32);
                }

                else if (typeof(double).IsAssignableFrom(type))
                {
                    double d = default(double);
                    if (!Double.TryParse(sval, out d))
                    {
                        String fn = MethodBase.GetCurrentMethod().Name;
                        HandleErr(fn, "Ill-formed real number: " + sval);
                        return (null);
                    }
                    return (d);
                }

                else if (typeof(String).IsAssignableFrom(type))
                {
                    return (sval);
                }

                else if (typeof(DateTime).IsAssignableFrom(type))
                {
                    DateTime dtm = DateTime.MinValue;
                    if (!DateTime.TryParse(sval, out dtm))
                    {
                        String fn = MethodBase.GetCurrentMethod().Name;
                        Util.HandleAppErr(this, fn, "Ill-formed date/time: " + sval);
                    }
                    return (dtm);
                }

                else if (typeof(Type).IsAssignableFrom(type))
                {
                    Type rettype = Type.GetType(sval);
                    if (null == rettype)
                    {
                        String fn = MethodBase.GetCurrentMethod().Name;
                        Util.HandleAppErr(this, fn, "Unrecognized type designator: " + sval);
                    }
                    return (rettype);

                }


                else if (typeof(Enum).IsAssignableFrom(type))
                {
                    object enumval = Enum.Parse(type, sval);
                    return (enumval);
                }

                else if (typeof(bool).IsAssignableFrom(type) || (typeof(Boolean).IsAssignableFrom(type)))
                {
                    bool bresult;
                    if (!bool.TryParse(sval, out bresult))
                    {
                        String fn = MethodBase.GetCurrentMethod().Name;
                        Util.HandleAppErr(this, fn, "Invalid Boolean value: " + sval);
                        return (false);
                    }
                    return (bresult);
                }

                String fname = MethodBase.GetCurrentMethod().Name;
                HandleErr(fname, "Unrecognized atomic type: " + type.Name);
                return (null);

            } // end main try
            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                HandleExc(fn, exc);
                Util.HandleAppErr(this, fn, "Previous exception occurred in XML: " + oXml.OuterXml);
                return (null);
            } // end main catch()

        }  // end function()



        /// <summary>
        /// Convert XML to an atom (string, int, double ... enum in future).
        /// </summary>
        /// <param name="oXml"></param>
        /// <returns></returns>
        public virtual Object XmlToList(XmlElement oXml)
        {
            String fn = "PersisterXml.XmlToList()";
            try
            {
                String sname = oXml.Name;
                String slisttype = oXml.GetAttribute(AttTagType);
                if ((null == slisttype) || ("" == slisttype))
                {
                    HandleErr(fn, "Unable to get list type for " + sname + " element.");
                    return (null);
                }
                Type listtype = Type.GetType(slisttype);
                if (null == listtype)
                {
                    HandleErr(fn, "Unrecognized type in " + sname + " element: " +
                            slisttype);
                    return (null);
                }
                ConstructorInfo cilist = listtype.GetConstructor(new Type[] { });
                if (null == cilist)
                {
                    HandleErr(fn, "No default constructor for " + listtype);
                    return (null);
                }
                Type[] genargs = listtype.GetGenericArguments();
                if (1 != genargs.Length)
                {
                    HandleErr(fn, "Not List<T>-compatible: " + listtype);
                    return (null);
                }
                Type instance_type = genargs[0];   // default instance type.


                IList list = cilist.Invoke(new object[] { }) as IList;
                if (null == list)
                {
                    HandleErr(fn, "Constructor failed for " + listtype);
                    return (null);
                }
                if (list.Count > 0)
                {
                    object firstobj = list[0];
                    instance_type = firstobj.GetType();
                    if (!genargs[0].IsAssignableFrom(instance_type))
                    {
                        HandleErr(fn, "Instance type " + instance_type +
                            " is not assignable to list-arg type " + genargs[0]);
                        return (null);
                    }
                }


                foreach (XmlNode nchild in oXml.ChildNodes)
                {
                    XmlElement echild = nchild as XmlElement;
                    if (null == echild) continue;       // there are some text elements...
                    Object ochild = XmlToObj(echild);
                    if (null == ochild) return (null);
                    Type chtype = ochild.GetType();
                    if (!instance_type.IsAssignableFrom(chtype))
                    {
                        HandleErr(fn, "Persisted object has wrong type (" +
                            chtype + ") for list arguments (" + instance_type
                            + ")");
                        return (null);
                    }
                    list.Add(ochild);
                }

                return (list);

            } // end main try
            catch (Exception exc)
            {
                HandleExc(fn, exc);
                return (null);
            } // end main catch()

        }  // end function()


        /// <summary>
        /// Convert XML to an atom (string, int, double ... enum in future).
        /// </summary>
        /// <param name="oXml"></param>
        /// <returns></returns>
        public virtual Object XmlToDictionary(XmlElement oXml)
        {
            try
            {
                String sname = oXml.Name;
                String dictionaryTypeAtt = oXml.GetAttribute(AttTagType);
                if ((null == dictionaryTypeAtt) || ("" == dictionaryTypeAtt))
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, "Unable to get dictionary type for " + sname + " element.");
                    return (null);
                }
                Type dictionaryType = Type.GetType(dictionaryTypeAtt);
                if (null == dictionaryType)
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, "Unrecognized type in " + sname + " element: " +
                            dictionaryTypeAtt);
                    return (null);
                }
                ConstructorInfo defaultConstructor = dictionaryType.GetConstructor(new Type[] { });
                if (null == defaultConstructor)
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, "No default constructor for " + dictionaryType);
                    return (null);
                }
                Type[] genargs = dictionaryType.GetGenericArguments();
                if (2 != genargs.Length)
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, "Not Dictionary<T, T>-compatible: " + dictionaryType);
                    return (null);
                }
                Type instance_type = genargs[0];   // default instance type.


                IDictionary dictionary = defaultConstructor.Invoke(new object[] { }) as IDictionary;
                if (null == dictionary)
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, "Constructor failed for " + dictionary);
                    return (null);
                }
                //if (dictionary.Keys.Count > 0)
                //{
                //    object firstobj = list[0];
                //    instance_type = firstobj.GetType();
                //    if (!genargs[0].IsAssignableFrom(instance_type))
                //    {
                //        HandleErr(fn, "Instance type " + instance_type +
                //            " is not assignable to list-arg type " + genargs[0]);
                //        return (null);
                //    }
                //}

                for (int i = 0; i < (oXml.ChildNodes.Count / 2); i++)
                {
                    XmlElement keyElement = oXml.ChildNodes[(i * 2)] as XmlElement;
                    if (null == keyElement) continue;		// there are some text elements...
                    Object keyObject = XmlToObj(keyElement);
                    if (null == keyObject) return (null);
                    Type keyType = keyObject.GetType();
                    if (!instance_type.IsAssignableFrom(keyType))
                    {
                        String fn = MethodBase.GetCurrentMethod().Name;
                        HandleErr(fn, "Persisted object has wrong type (" +
                            keyType + ") for list arguments (" + instance_type
                            + ")");
                        return (null);
                    }

                    XmlElement valueElement = oXml.ChildNodes[(i * 2) + 1] as XmlElement;
                    if (null == valueElement) continue;		// there are some text elements...
                    Object valueObject = XmlToObj(valueElement);
                    if (null == valueObject) return (null);
                    Type valueType = valueObject.GetType();
                    if (!instance_type.IsAssignableFrom(valueType))
                    {
                        String fn = MethodBase.GetCurrentMethod().Name;
                        HandleErr(fn, "Persisted object has wrong type (" +
                            valueType + ") for list arguments (" + instance_type
                            + ")");
                        return (null);
                    }
                    dictionary.Add(keyObject, valueObject);
                }

                return (dictionary);
            }
            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                HandleExc(fn, exc);
                return (null);
            }
        }

        /// <summary>
        /// Convert XML to an atom (string, int, double ... enum in future).
        /// </summary>
        /// <param name="oXml"></param>
        /// <returns></returns>
        public virtual Object XmlToAggregate(XmlElement oXml, bool useCustomPersisters = true)
        {
            string state = "entry";
            try
            {
                state = "getting attribute";
                String stype = oXml.GetAttribute(AttTagType);
                if ((null == stype) || ("" == stype))
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, oXml.Name + " element does not contain tag '"
                        + AttTagType + "'.");
                    return (null);
                }

                state = "getting type '" + stype + "'";
                Type aggtype = Type.GetType(stype);
                if (null == aggtype)
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, "Unable to resolve type " + stype);
                    return (null);
                }

                // Custom persisters for aggregate types
                ICustomPersister persister;
                if (useCustomPersisters && m_oRegisteredTypes.TryGetValue(aggtype, out persister)) // We have a custom persister for this type, use it to get our object
                {
                    state = "custom persister for type " + aggtype.Name;
                    object obj;
                    if (!persister.XmlToObj(this, oXml, out obj))
                    {
                        throw (new Exception(String.Format("Unable generate {0} type object via custom persister from the following XML: {1}", aggtype.Name, oXml.InnerXml)));
                    }
                    else
                    {
                        return obj;
                    }
                }

                // Construct an instance:
                state = "getting constructor for type " + aggtype.Name;
                ConstructorInfo ciagg = aggtype.GetConstructor(new Type[] { });
                if (null == ciagg)
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, "Unable to get default constructor for type " + aggtype);
                    return (null);
                }
                state = "invoking constructor for type " + aggtype.Name;
                Object oagg = ciagg.Invoke(new Object[] { });
                if (null == oagg)
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, "Unable to construct an instance of " + aggtype);
                    return (null);
                }

                // ACS 2009-10-30: Put the Reference UID load before processing child nodes to prevent
                // a child node from referencing a parent node that doesn't exist in the table yet
                if (UseObjRefs)
                {
                    state = "using ObjRefs";
                    String suid = oXml.GetAttribute(AttTagUid);
                    if ((null != suid) && ("" != suid))
                    {
                        int uid = -1;
                        if (!Int32.TryParse(suid, out uid))
                        {
                            String fn = MethodBase.GetCurrentMethod().Name;
                            HandleErr(fn, "Ill-formed UID in AggregateRef: " + suid);
                            return (null);
                        }
                        if (!m_oRefTable.SetByUid(uid, oagg))
                        {
                            String fn = MethodBase.GetCurrentMethod().Name;
                            HandleErr(fn, "Unable to insert UID " + uid + " into reftable");
                            return (null);
                        }
                    } // endif(valid UID string)
                } // endif(using object refs)

                // Loop through the "<field>" elements; attempt to sub-load
                // and assign values for each one:
                foreach (XmlNode fieldnode in oXml.ChildNodes)
                {
                    Type fntype = fieldnode.GetType();
                    if (!typeof(XmlElement).IsAssignableFrom(fntype))
                    {
                        continue;
                    }
                    XmlElement efield = fieldnode as XmlElement;
                    if (ElementTagField != efield.Name)
                    {
                        String fn = MethodBase.GetCurrentMethod().Name;
                        HandleErr(fn, oXml.Name + " contains invalid '" + efield.Name
                                + " element.");
                        return (null);
                    }
                    String sfname = efield.GetAttribute(AttTagName);
                    if ((null == sfname) || ("" == sfname))
                    {
                        String fn = MethodBase.GetCurrentMethod().Name;
                        HandleErr(fn, "No " + AttTagName + " attribute for " +
                                ElementTagField + " element in " + oXml.Name);
                        return (null);
                    }
                    state = "field " + sfname;

                    FieldInfo fi = aggtype.GetField(sfname);
                    if (null == fi)
                    {
                        if (!IgnoreUnknownFieldsOnLoad)
                        {
                            String fn = MethodBase.GetCurrentMethod().Name;
                            HandleErr(fn, "Unable to find field '" + sfname + "' in aggregate " +
                                aggtype);
                        }
                        // 20090312 CDN (bug in case we've removed a field, ignore it:
                        continue;
                    }

                    // 20081202 CDN - there seem to be text nodes in here.  
                    // Take the first element, and assume that has the field value.
                    state = "found FI for field " + sfname;
                    XmlElement edata = null;
                    foreach (XmlNode gcnode in fieldnode.ChildNodes)
                    {
                        edata = gcnode as XmlElement;
                        if (null != edata) break;
                    }
                    if (null == edata)
                    {
                        // 20090513 CDN - allow null values:
                        fi.SetValue(oagg, null);
                    }

                    else // nonnull (20090513 - since we're allowing null values)
                    {
                        Object odata = XmlToObj(edata);
                        if (null == odata) return (null);
                        state = "setting field data for " + fi.Name;
                        fi.SetValue(oagg, odata);
                    }
                } // end for(each <field> element)

                state = "returning aggregate";
                return (oagg);
            } // end main try
            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                HandleExc(fn, exc);
                Util.HandleAppErr(this, fn, "Preceding exception context: " + state);
                if (null != exc.InnerException)
                {
                    Util.HandleExc(this, "INNER EXCEPTION", exc.InnerException);
                }
                return (null);
            } // end main catch()

        }  // end function()


        /// <summary>
        /// Decode an aggregate reference from the object reference table,
        /// assuming the UID is in the inner text of the element.
        /// Example:   <AggregateRef Type="Xxx">1234567</AggregateRef>
        /// would load object 1234567 from the reftable, and make sure
        /// its type was Xxx (a fully qualified type name).
        /// </summary>
        /// <param name="oXml"></param>
        /// <returns></returns>

        protected virtual Object XmlToAggRef(XmlElement oXml)
        {
            try
            {
                if (!UseObjRefs)
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, "Attempt to load object reference with references turned off.");
                    return (null);
                }

                String stype = oXml.GetAttribute(AttTagType);
                if ((null == stype) || ("" == stype))
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, oXml.Name + " element does not contain tag '"
                        + AttTagType + "'.");
                    return (null);
                }
                Type aggtype = Type.GetType(stype);
                if (null == aggtype)
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, "Unable to resolve type " + stype);
                    return (null);
                }

                String suid = oXml.InnerText;
                int uid = -1;
                if (!Int32.TryParse(suid, out uid))
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, "Ill-formed UID: " + suid);
                    return (null);
                }

                Object oref = m_oRefTable.FromUid(uid);
                if (null == oref)
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, "AggregateRef UID not found: " + uid);
                    return (null);
                }
                if (!aggtype.IsAssignableFrom(oref.GetType()))
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, "Type mismatch on UID " + uid + ": " +
                            aggtype.ToString() + " vs. " +
                            oref.GetType().ToString());
                    return (null);
                }

                return (oref);

            } // end main try
            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                HandleExc(fn, exc);
                return (null);
            } // end main catch()

        }




        protected virtual Type GetObjectType(XmlElement oXml)
        {
            try
            {
                String stype = oXml.GetAttribute(AttTagType);
                if ((null == stype) || ("" == stype))
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, "Unable to get '" + AttTagType + "' attribute from " + oXml.Name + " element.");
                    return (null);
                }

                Type t = Type.GetType(stype);
                if (null == t)
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, "Unrecognized type: " + stype);
                    return (null);
                }

                return (t);
            }

            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                HandleExc(fn, exc);
                return (null);
            }

        }


        /// <summary>
        /// Save an object to a file.   First, validate that the object's type is
        /// valid acording to Persister rules.  Then save to XML, if valid.
        /// </summary>
        /// <param name="oSubj">Object to be saved.  Must conform to Persister rules.</param>
        /// <param name="sFile">Full path to file.</param>
        /// <returns>true on b_succeeded; consult Messages, LastMessage otherwise.</returns>
        public virtual bool Save(Object oSubj, String sFile)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            StreamWriter sw = null;
            try
            {
                m_oRefTable.Clear();

                if (null == oSubj) return (HandleErr(fn, "Don't be passing me null!"));
                Type t = oSubj.GetType();
                List<String> persister_errs = new List<string>();
                if (!IsValidType(t, persister_errs))
                {
                    HandleErr(fn, "Type " + t + " does not conform to Persister rules; see following specifics");
                    foreach (String s in persister_errs) HandleErr(fn, "  Persister violation: " + s);
                    return (false);
                }

                XmlDocument doc = new XmlDocument();
                if (!XmlDocAppend(oSubj, doc)) return (false);
                String sxml = Util.PpXml(doc);

                // 20140215 CDN - bug 1410 - use a file lock table to protect the write to this file.
                // In general, we should not be saving to the same file at the same time from different
                // threads, but we need to be sure we protect against it!
                Stopwatch sw1 = new Stopwatch();
                sw1.Start();

                lock (m_oStaticLock)
                {

                    if (!m_oFileLockTable.ContainsKey(sFile)) { m_oFileLockTable.Add(sFile, new object()); }
                }
                Stopwatch sw2 = new Stopwatch();
                sw2.Start();
                lock (m_oFileLockTable[sFile])
                {
                    //Util.Benchmark(this, fn, "get-lock-6(m_oFileLockTable[" + sFile + "])", sw2);

                    sw = new StreamWriter(sFile, false);
                    sw.Write(sxml);
                    sw.Flush();
                }

                return (true);
            }

            catch (Exception exc)
            {
                HandleExc(fn, exc);
                return (false);
            }
            finally
            {
                if (null != sw) sw.Close();
            }
        }



        /// <summary>
        /// Load a specified instance from a file.   Instance must be of the correct type, or an error
        /// is returned, and the specified instance is unchanged.   Otherwise, all public fields in the
        /// specified instance are updated with the corresponding versions from the file.
        /// </summary>
        /// <param name="oInstance"></param>
        /// <param name="sFile"></param>
        /// <returns></returns>
        public virtual bool Load(Object oInstance, String sFile)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                object ofileinst = null;
                if (!Load(out ofileinst, sFile)) return (false);


                IList il_file = ofileinst as IList;
                if (null != il_file)
                {
                    IList il_inst = oInstance as IList;
                    if (null == il_inst) return (Util.HandleAppErr(this, fn, "Retrieved a list, assigning to non-list type " + oInstance.GetType()));
                    il_inst.Clear();
                    foreach (Object fileobj in il_file) il_inst.Add(fileobj);
                    return (true);
                }


                // Now, make sure the types are correct:
                Type insttype = oInstance.GetType();
                Type filetype = ofileinst.GetType();
                if (!insttype.IsAssignableFrom(filetype))
                {
                    return (Util.HandleAppErr(this, fn,
                                    "Type " + filetype + " loaded from " + sFile +
                                    " is not conformable to " + "requested type: " + insttype));
                }


                // Loop through field infos.  First, make sure they correspond, then
                // copy data, for each one:
                FieldInfo[] instinfos = insttype.GetFields();
                FieldInfo[] fileinfos = filetype.GetFields();
                for (int i = 0; i < instinfos.Length; i++)
                {
                    FieldInfo instinfo = instinfos[i];
                    if (!instinfo.IsPublic) continue;
                    if (instinfo.IsStatic) continue;
                    FieldInfo fileinfo = fileinfos[i];
                    if (instinfo.Name != fileinfo.Name)
                    {
                        return (Util.HandleAppErr(this, fn, "Field mismatch: " + instinfo.Name + " vs. " + fileinfo.Name));
                    }
                    object fileval = fileinfo.GetValue(ofileinst);
                    instinfo.SetValue(oInstance, fileval);
                } // end for(i=field index)
                return (true);

            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }


        /// <summary>
        /// Load an object from an XML file.
        /// </summary>
        /// <param name="roSubj"></param>
        /// <param name="sFile"></param>
        /// <returns></returns>
        public virtual bool Load(out Object roSubj, String sFile)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            roSubj = null;
            try
            {
                roSubj = Load(sFile);
                if (null == roSubj) return (false);
                return (true);

            }
            catch (Exception exc)
            {
                return (HandleExc(fn, exc));
            }
        }

        /// <summary>
        /// Load a single object from an XML file.
        /// </summary>
        /// <param name="sFile"></param>
        /// <returns></returns>
        public virtual Object Load(String sFile)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            StreamReader sr = null;
            try
            {
                m_oRefTable.Clear();
                String sxml = "";
                object oret = null;

                // 20140215 CDN - bug 1410 - use a file lock table to protect the write to this file.
                // In general, we should not be saving to the same file at the same time from different
                // threads, but we need to be sure we protect against it!
                Stopwatch sw1 = new Stopwatch();
                sw1.Start();

                lock (m_oStaticLock) // jpr-load
                {
                    if (!m_oFileLockTable.ContainsKey(sFile)) { m_oFileLockTable.Add(sFile, new object()); }
                }

                Stopwatch sw2 = new Stopwatch();
                sw2.Start();
                lock (m_oFileLockTable[sFile])
                {
                    //Util.Benchmark(this, fn, "get-lock-7(m_oFileLockTable["+sFile+"])", sw2);

                    sr = new StreamReader(sFile);
                    String sline = "";
                    TimeThis.Code("Pxml.Load()/read", 0.0, () =>
                    {
                        StringBuilder sb = new StringBuilder();
                        while (null != (sline = sr.ReadLine()))
                        {
                            //sxml += sline + "\r\n";
                            sb.Append(sline);
                        }
                        sxml = sb.ToString();
                    });
                    XmlDocument doc = new XmlDocument();
                    TimeThis.Code("Pxml.Load()/LoadXml", 0.0, () =>
                    {
                        doc.LoadXml(sxml);
                    });
                    XmlElement root = doc.DocumentElement;
                    TimeThis.Code("Pxml.Load()/XmlToObj()", 0.0, () =>
                    {
                        oret = XmlToObj(root);
                    });
                    sr.Close();
                    sr = null;

                } // end(file lock)


                return (oret);
            }

            catch (Exception exc)
            {
                HandleExc(fn, exc);
                return (null);
            }
            finally
            {
                if (null != sr) sr.Close();
            }
        }

        /// <summary>
        /// Turn the specified object into an XML element, and append it to
        /// the specified XML document.
        /// </summary>
        /// <param name="oSubj"></param>
        /// <param name="oDoc"></param>
        /// <returns></returns>
        public bool XmlDocAppend(Object oSubj, XmlDocument oDoc)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                XmlElement element = XmlFromObj(oSubj, oDoc);
                if (null == element) return (false);
                if (null == oDoc.DocumentElement)
                {
                    oDoc.AppendChild(oDoc.CreateElement(ElementTagRoot));
                }
                oDoc.DocumentElement.AppendChild(element);
                return (true);
            }
            catch (Exception exc)
            {
                return (HandleExc(fn, exc));
            }
        }


        /// <summary>
        /// Generate XML from an arbitrary object.
        /// </summary>
        /// <param name="oSubj"></param>
        /// <param name="oDoc"></param>
        /// <returns></returns>
        public virtual XmlElement XmlFromObj(Object oSubj, XmlDocument oDoc, bool useCustomPersister = true)
        {
            try
            {
                if (null == oSubj) return (null);
                Type t = oSubj.GetType();
                XmlElement element;
                if (useCustomPersister && IsKnownType(t))
                {
                    ICustomPersister persister;
                    if (!m_oRegisteredTypes.TryGetValue(t, out persister))
                    {
                        throw (new Exception("Unable to load persister for registered type."));
                    }

                    if (!persister.XmlFromObj(this, oSubj, oDoc, out element))
                    {
                        throw (new Exception(string.Format("CustomPersister failed to generate XML for registered type {0}", t.Name)));
                    };
                }
                else if (IsValidAtomicType(t, null)) element = XmlFromAtom(oDoc, oSubj);
                else if (IsValidAggregateType(t, null)) element = XmlFromAggregate(oDoc, oSubj);
                else if (IsValidListType(t)) element = XmlFromList(oDoc, oSubj);
                else if (IsValidDictionaryType(t)) element = XmlFromDictionary(oDoc, oSubj);
                else
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, "Invalid type: " + t);
                    return (null);
                }
                return (element);
            }
            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                HandleExc(fn, exc);
                return (null);
            }

        }

        public static string StringFromDateTime(DateTime dtm)
        {
            return dtm.ToShortDateString() + " " +
                        String.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", dtm.Hour, dtm.Minute, dtm.Second, dtm.Millisecond);
        }

        /// <summary>
        /// Return an XML structure for the specified atomic data item.
        /// </summary>
        /// <param name="oSubj"></param>
        /// <returns></returns>
        public virtual XmlElement XmlFromAtom(XmlDocument oDoc, Object oSubj)
        {
            try
            {
                Type t = oSubj.GetType();
                String sval = oSubj.ToString();         // default is the naive string representation; special cases are below.

                if (typeof(Type).IsAssignableFrom(t)) { sval = t.AssemblyQualifiedName; }
                else if (typeof(DateTime).IsAssignableFrom(t))
                {
                    DateTime dtm = (DateTime)oSubj;
                    sval = StringFromDateTime(dtm);
                }

                XmlElement elt = AtomToXml(oDoc, ElementTagAtom, t, oSubj.ToString());
                return (elt);
            }
            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                HandleExc(fn, exc);
                return (null);
            }

        }


        public virtual XmlElement XmlFromAtom(XmlDocument oDoc, String sCategory, Type oType, String sValue)
        {
            return AtomToXml(oDoc, sCategory, oType, sValue);
        }

        /// <summary>
        /// Initialize an XML element according to the category (Atom, List, etc.), 
        /// type and optional contents.    Assumes a document has already been constructed.
        /// </summary>
        /// <param name="oDoc"></param>
        /// <param name="sCategory"></param>
        /// <param name="oType"></param>
        /// <param name="sValue"></param>
        /// <returns></returns>
        private XmlElement AtomToXml(XmlDocument oDoc, String sCategory, Type oType, String sValue)
        {
            try
            {
                XmlElement elt = oDoc.CreateElement(sCategory);
                elt.Attributes.Append(oDoc.CreateAttribute(AttTagType)).Value
                    = oType.AssemblyQualifiedName;
                //= oType.Name;	// 20081201 CDN - Kludge?
                elt.InnerText = Util.XmlEncode(sValue);    // 20110927 CDN - handle, e.g., multiline strings
                return (elt);
            }
            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                HandleExc(fn, exc);
                return (null);
            }
        }

        /// <summary>
        /// Return an XML structure for the specified aggregate data item.
        /// </summary>
        /// <param name="oSubj"></param>
        /// <returns></returns>
        public virtual XmlElement XmlFromAggregate(XmlDocument oDoc, Object oSubj)
        {
            try
            {
                Type t = oSubj.GetType();
                XmlElement elt = AtomToXml(oDoc, ElementTagAggr, t, "");
                if (UseObjRefs)
                {
                    bool bnew = false;
                    int uid = m_oRefTable.ToUid(oSubj, out bnew);
                    // If it's a known UID, write '<AggRef Type="...">UID</AggRef>'
                    if (!bnew)
                    {
                        elt = AtomToXml(oDoc, ElementTagAref, t, uid.ToString());
                        return (elt);
                    }

                    // If it's a new UID, just include a UID attribute:
                    XmlAttribute uidatt = oDoc.CreateAttribute(AttTagUid);
                    uidatt.Value = uid.ToString();
                    elt.Attributes.Append(uidatt);
                }
                foreach (FieldInfo fi in t.GetFields())
                {
                    if (!IsTargetField_InstanceFunction(fi)) continue;
                    Type ft = fi.FieldType;
                    XmlElement felt = AtomToXml(oDoc, "Field", ft, "");
                    XmlAttribute fnatt = oDoc.CreateAttribute("Name");
                    felt.Attributes.Prepend(fnatt);
                    fnatt.Value = fi.Name;
                    elt.AppendChild(felt);
                    Object oval = fi.GetValue(oSubj);
                    XmlElement velt = XmlFromObj(oval, oDoc);
                    if (null != velt) felt.AppendChild(velt);
                    else
                    {
                        // 20090513 CDN - allow null values.
                        //Util.HandleAppErr(this, fn, "Couldn't export value for field " + fi.Name);
                    }
                }
                return (elt);
            }
            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                HandleExc(fn, exc);
                return (null);
            }
        }


        /// <summary>
        /// Return an XML structure for the specified list.
        /// </summary>
        /// <param name="oSubj"></param>
        /// <returns></returns>
        public virtual XmlElement XmlFromList(XmlDocument oDoc, Object oSubj)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                if (null == oSubj) return (null);
                Type t = oSubj.GetType();
                IList ilist = oSubj as IList;
                if (null == ilist)
                {
                    HandleErr(fn, "Type incompatible with IList: " + t);
                    return (null);
                }

                XmlElement elt = AtomToXml(oDoc, ElementTagList, t, "");
                Type instance_type = null;
                bool homogeneous = true;
                foreach (Object obj in ilist)
                {
                    XmlElement childelt = XmlFromObj(obj, oDoc);
                    if (null == childelt)
                    {
                        Util.HandleAppErr(this, fn, "Unable to convert object " + obj.ToString());
                        continue;
                    }
                    elt.AppendChild(childelt);
                    Type childtype = obj.GetType();
                    if (null == instance_type) instance_type = childtype;
                    else
                    {
                        homogeneous &= (childtype == instance_type);
                    }
                }

                XmlAttribute itypeatt = oDoc.CreateAttribute("InstanceType");
                itypeatt.Value = "";
                if (homogeneous && (null != instance_type))
                    itypeatt.Value = instance_type.AssemblyQualifiedName;
                elt.Attributes.Append(itypeatt);

                return (elt);

            }
            catch (Exception exc)
            {
                HandleExc(fn, exc);
                return (null);
            }
        }

        /// <summary>
        /// Return an XML structure for the specified list.
        /// </summary>
        /// <param name="oSubj"></param>
        /// <returns></returns>
        public virtual XmlElement XmlFromDictionary(XmlDocument oDoc, Object oSubj)
        {
            try
            {
                /// Confirm our dictionary object isn't null
                if (null == oSubj) return (null);
                Type t = oSubj.GetType();
                /// Confirm our object is a dictionary
                IDictionary iDict = oSubj as IDictionary;
                if (null == iDict)
                {
                    String fn = MethodBase.GetCurrentMethod().Name;
                    HandleErr(fn, "Type incompatible with IDictionary: " + t);
                    return (null);
                }
                /// Create a general tag for our dictionary object
                XmlElement dictionaryElement = AtomToXml(oDoc, ElementTagDict, t, "");
                Type keyInstanceType = null;
                bool homogeneousKeys = true;
                Type valueInstanceType = null;
                bool homogeneousValues = true;


                foreach (Object key in iDict.Keys)
                {
                    /// Create a child object of the dictionary for the key
                    XmlElement keyElement = XmlFromObj(key, oDoc);
                    if (null == keyElement)
                    {
                        String fn = MethodBase.GetCurrentMethod().Name;
                        Util.HandleAppErr(this, fn, "Unable to convert object " + key.ToString());
                        continue;
                    }
                    dictionaryElement.AppendChild(keyElement);

                    /// Check the type and see if all keys are the same element
                    Type keyType = key.GetType();
                    if (null == keyInstanceType) keyInstanceType = keyType;
                    else
                    {
                        homogeneousKeys &= (keyType == keyInstanceType);
                    }

                    /// Create a child object of the key for the value
                    Object value = iDict[key];
                    XmlElement valueElement = XmlFromObj(value, oDoc);
                    if (null == valueElement)
                    {
                        String fn = MethodBase.GetCurrentMethod().Name;
                        Util.HandleAppErr(this, fn, "Unable to convert object " + value.ToString());
                        continue;
                    }
                    dictionaryElement.AppendChild(valueElement);

                    /// Check the type and see if all keys are the same element
                    Type valueType = value.GetType();
                    if (null == valueInstanceType) valueInstanceType = valueType;
                    else
                    {
                        homogeneousValues &= (valueType == keyInstanceType);
                    }
                }

                /// If the keys or values were all the same type store that type information with them
                XmlAttribute itypeatt = oDoc.CreateAttribute("KeyInstanceType");
                itypeatt.Value = "";
                if (homogeneousKeys && (null != keyInstanceType)) itypeatt.Value = keyInstanceType.AssemblyQualifiedName;
                dictionaryElement.Attributes.Append(itypeatt);

                itypeatt = oDoc.CreateAttribute("KeyInstanceType");
                itypeatt.Value = "";
                if (homogeneousValues && (null != valueInstanceType)) itypeatt.Value = valueInstanceType.AssemblyQualifiedName;
                dictionaryElement.Attributes.Append(itypeatt);

                return (dictionaryElement);
            }
            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                HandleExc(fn, exc);
                return (null);
            }
        }


        #endregion

        #region Constants
        public static String ElementTagRoot = "Root";
        public static String ElementTagAtom = "Atom";
        public static String ElementTagList = "List";
        public static String ElementTagDict = "Dictionary";
        public static String ElementTagAggr = "Aggregate";
        public static String ElementTagAref = "AggregateRef";
        public static String ElementTagCust = "Custom";
        public static String ElementTagField = "Field";

        public static String AttTagType = "Type";
        public static String AttTagName = "Name";
        public static String AttTagUid = "Uid";
        #endregion

        #region Public Properties

        /// <summary>
        /// Is this Persister using object references?   If so, aggregates will
        /// reconstruct with singular object instances.  E.g.:
        ///     A1.B = new BKind();
        ///     A1.B.Prop = 1;
        ///     A2.B = A1.B;
        ///     A1.B.Prop = 2;
        ///     Save();  Clear(); Reload();
        ///     A2.B.Prop == 2!
        /// </summary>
        public bool UseObjRefs
        {
            get { return m_bUsingObjRefs; }
            set { m_bUsingObjRefs = value; }
        }

        #endregion

        #region Data Members



        // Statics -- access protected by m_oStaticLock:
        public static bool ValidationTracing = false;

        // Types registered by clients:
        private static Dictionary<Type, ICustomPersister> m_oRegisteredTypes = new Dictionary<Type, ICustomPersister>();              // jpr-MT (CDN fixed)

        // List of known-good types accumulated by .IsValidType() recursion - performance enhancement:
        private static Dictionary<Type, object> m_oKnownGoodTypes = new Dictionary<Type, object>();                                    // jpr-MT (CDN fixed)

        // Stacks known-valid classes as we recurse down a type/containment hierarchy:
        private static Stack<Type> m_oValidationStack = new Stack<Type>();                                                             // jpr-MT (CDN fixed)

        // Table, by file name, of lock objects to control inter-thread access to persister files
        // being read or written (consulted/updated by Save() and Load()).
        private static Dictionary<string, object> m_oFileLockTable = new Dictionary<string, object>();

        // Deprecated 20140215 CDN:
        //private static StreamWriter m_swTraceFile = null;                                                                              // jpr-MT (CDN fixed)


        private static object m_oStaticLock = new object();     // multithreaded protection


        private ObjRefTable m_oRefTable = new ObjRefTable();

        public ObjRefTable RefTable { get { return m_oRefTable; } }

        private bool m_bUsingObjRefs = true;

        #endregion

    } // end class Persister

    /// <summary>
    /// This class keeps unique references to objects.   Given an object, it provides
    /// the object's UID (which it creates and remembers, if it doesn't already "know"
    /// the object).   Given a valid UID, it returns the original object.
    /// </summary>
    public class ObjRefTable
    {
        /// <summary>
        ///  Given a valid, predefined, UID, find the object that originally generated
        /// it.
        /// </summary>
        /// <param name="iUid"></param>
        /// <returns></returns>
        public virtual Object FromUid(int iUid)
        {
            if (null == m_oKnownObjects) return (null);
            if ((iUid < 0) || (iUid >= m_oKnownObjects.Count)) return (null);
            return (m_oKnownObjects[iUid]);
        }

        /// <summary>
        /// Given an object get a UID -- either create a new one for it (if the
        /// object is unknown), or retrieve the existing one.
        /// </summary>
        /// <param name="oSubj">Object for which we want the UID</param>
        /// <param name="rbIsNew">(Optional) Tells whether the object has a new UID</param>
        /// <returns></returns>
        public virtual int ToUid(Object oSubj)
        {
            bool bnew = false;
            return (ToUid(oSubj, out bnew));
        }
        public virtual int ToUid(Object oSubj, out bool rbIsNew)
        {
            Type t = oSubj.GetType();
            int hash = oSubj.GetHashCode();
            List<ObjRefPair> hitlist = null;
            rbIsNew = false;

            // If the hashcode isn't in the LUT, or its slot has a placeholder, add a list:
            if ((!m_oLookupTable.ContainsKey(hash)) ||
                    (typeof(Placeholder).IsAssignableFrom(m_oLookupTable[hash].GetType()))
               )
            {
                hitlist = new List<ObjRefPair>();
                m_oLookupTable.Add(hash, hitlist);
            }
            else
            {
                hitlist = m_oLookupTable[hash];
            }

            // Look through the hitlist; if we find a match, return its UID:
            ObjRefPair matchpair = null;
            foreach (ObjRefPair orp in hitlist)
            {
                if (oSubj.Equals(orp.Subject))
                {
                    matchpair = orp;
                    break;
                }
            }
            if (null != matchpair) return (matchpair.Uid);

            // If we don't find a match, create a new entry in the hitlist,
            // and insert into the master object list:
            int newuid = -2;

            Stopwatch sw1 = new Stopwatch();
            sw1.Start();

            lock (m_oKnownObjects) // jpr-save
            {
                //Util.Benchmark(new object(), fn, "get-lock-19(m_oKnownObjects)", sw1);

                newuid = m_oKnownObjects.Count;
                m_oKnownObjects.Add(oSubj);
                rbIsNew = true;
                ObjRefPair newpair = new ObjRefPair(oSubj, newuid);
                hitlist.Add(newpair);
            }
            return (newuid);

        }

        /// <summary>
        /// Force-insert an object into the ORT at a given UID.
        /// </summary>
        /// <param name="iUid"></param>
        /// <param name="oSubj"></param>
        /// <returns></returns>
        public virtual bool SetByUid(int iUid, Object oSubj)
        {
            try
            {
                Placeholder p = new Placeholder();
                while (m_oKnownObjects.Count <= iUid) m_oKnownObjects.Add(p);
                m_oKnownObjects[iUid] = oSubj;

                int hash = oSubj.GetHashCode();
                List<ObjRefPair> lorp = null;
                if ((!m_oLookupTable.ContainsKey(hash)) ||
                        (typeof(Placeholder).IsAssignableFrom(m_oLookupTable[hash].GetType()))
                   )
                {
                    lorp = new List<ObjRefPair>();
                    m_oLookupTable.Add(hash, lorp);
                }
                else
                {
                    lorp = m_oLookupTable[hash];
                }

                // Remove the former denizen(s, which would be a mistake...) w/ this UID:
                foreach (ObjRefPair orp in lorp)
                {
                    if (orp.Uid == iUid) lorp.Remove(orp);
                }

                lorp.Add(new ObjRefPair(oSubj, iUid));

                // Make sure the o/r list has enough entries:
                while (iUid >= m_oKnownObjects.Count)
                {
                    m_oKnownObjects.Add(new Placeholder());
                }

                m_oKnownObjects[iUid] = oSubj;

                return (true);
            } // end main try

            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }


        ///// <summary>
        ///// Stress-test the object reference table.
        ///// </summary>
        ///// <param name="iReps"></param>
        ///// <returns></returns>
        //public static List<String> StressTest(int iReps)
        //{
        //    ObjRefTable ort = new ObjRefTable();
        //    List<String> errlist = new List<string>();
        //    int collisions = 0;
        //    Stopwatch sw = new Stopwatch();
        //    sw.Start();
        //    for (int i = 0; i < iReps; i++)
        //    {
        //        SampleClassA a1 = new SampleClassA();
        //        a1.Name = Util.RandomStr(8);
        //        int uid = ort.ToUid(a1);
        //        SampleClassA a2 = ort.FromUid(uid) as SampleClassA;
        //        if (a1.Name != a2.Name)
        //        {
        //            collisions++;
        //            if (errlist.Count < 5)
        //            {
        //                errlist.Add("Collision: " + a1.Name + " (hash " + a2.GetHashCode() + ")   with " +
        //                    a2.Name + " (hash " + a2.GetHashCode() + ")");
        //            }
        //        }
        //    } // end for (i: rep)
        //    sw.Stop();
        //    errlist.Add(collisions + " collisions out of " + iReps + " trials.");
        //    String stiming =
        //        sw.ElapsedMilliseconds + " ms elapsed; " +
        //        Math.Round(1000.0 * ((double)sw.ElapsedMilliseconds) / ((double)iReps), 4) +
        //        " usec per trial";
        //    errlist.Add(stiming);
        //    return (errlist);
        //} // end StresTest()

        public virtual bool Clear()
        {
            m_oKnownObjects.Clear();
            m_oLookupTable.Clear();
            return (true);
        }

        private class Placeholder
        {
        }

        private List<Object> m_oKnownObjects = new List<object>();

        private Dictionary<int, List<ObjRefPair>> m_oLookupTable
            = new Dictionary<int, List<ObjRefPair>>();

    }

    public class ObjRefPair
    {
        public ObjRefPair(Object oSubj, int iUid)
        {
            Uid = iUid;
            Subject = oSubj;
        }
        public Object Subject = null;
        public int Uid = 0;
    }
}
