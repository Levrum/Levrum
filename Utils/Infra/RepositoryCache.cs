using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Levrum.Utils.Infra
{
    /// <summary>
    /// In-memory repository of objects by type.
    /// </summary>
    public class RepositoryCache
    {
        /// <summary>
        /// Add an object to the cache, according to its type.
        /// </summary>
        /// <param name="oSubj"></param>
        /// <returns></returns>
        public virtual bool Add(Object oSubj)
        {
            Type subjtype = oSubj.GetType();
            TypeList list = GetTypeList(subjtype);
            if (null == list)
            {
                list = new TypeList();
                list.TypeName = subjtype.AssemblyQualifiedName;
                Contents.Add(list);
            }
            list.Instances.Add(oSubj);
            return (true);
        }


        /// <summary>
        /// Does the repository contain the specified object?
        /// </summary>
        /// <param name="oSubj"></param>
        /// <returns></returns>
        public virtual bool Contains(Object oSubj)
        {
            Type subjtype = oSubj.GetType();
            TypeList list = GetTypeList(subjtype);
            if (null == list) return (false);
            foreach (Object curobj in list.Instances)
            {
                if (curobj.Equals(oSubj)) return (true);
            }
            return (false);

        }

        /// <summary>
        /// Makes a copy of the RepositoryCache. Objects inside the cache are not copied, only the cache's TypeLists. 
        /// Thus, modifying an object in the original RepositoryCache will affect the object in the copy.
        /// </summary>
        /// <returns></returns>
        public virtual RepositoryCache Copy()
        {
            RepositoryCache outCache = new RepositoryCache();
            foreach (TypeList list in Contents)
            {
                TypeList newList = new TypeList();
                newList.TypeName = list.TypeName;
                foreach (object item in list.Instances)
                {
                    newList.Instances.Add(item);
                }
            }

            return outCache;
        }


        /// <summary>
        /// Remove a single object, regardless of type.  
        /// </summary>
        /// <param name="oSubj"></param>
        /// <returns></returns>
        public virtual bool Remove(Object oSubj)
        {
            string fn = "RepositoryCache.Remove()";
            try
            {
                Type subjtype = oSubj.GetType();
                TypeList list = GetTypeList(subjtype);
                if (null == list)
                {
                    Util.HandleAppErr(this, fn, "Instance " + oSubj.ToString() + " not found");
                    return (false);
                }
                list.Instances.Remove(oSubj);
                return (true);

            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }

        /// <summary>
        /// Remove all references to an object.   Optionally, remove the object itself, as well.
        /// </summary>
        /// <param name="oSubj"></param>
        /// <returns></returns>
        public virtual bool RemoveAllReferences(object oSubj, bool bRemoveThisObject)
        {
            try
            {
                List<ObjRefInfo> refs = FindAllReferences(oSubj);

                foreach (ObjRefInfo curref in refs)
                {
                    // 20130611 CDN - bugfix #547:
                    //curref.ReferringField.SetValue(curref.Referror, null);
                    RemoveValue(curref);
                }

                if ((bRemoveThisObject) && (!Remove(oSubj))) { return (false); }

                return (true);
            }
            catch (Exception exc)
            {
                string fn = MethodBase.GetCurrentMethod().Name;
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }

        /// <summary>
        /// Remove a single value from a field (either IList or scalar).
        /// </summary>
        /// <param name="curref"></param>
        /// <returns></returns>
        private bool RemoveValue(ObjRefInfo curref)
        {
            string fn = "RepositoryCache.RemoveValue()";
            try
            {
                FieldInfo fi = curref.ReferringField;
                Type ftype = fi.FieldType;
                object target = curref.Referent;
                object referror = curref.Referror;
                if (typeof(IList).IsAssignableFrom(ftype))
                {
                    IList ilist = fi.GetValue(curref.Referror) as IList;
                    List<int> removal_indices = new List<int>();
                    for (int i = 0; i < ilist.Count; i++)
                    {
                        if (ilist[i].Equals(target)) { removal_indices.Add(i); }
                    }
                    for (int j = removal_indices.Count - 1; j >= 0; j--) { ilist.RemoveAt(removal_indices[j]); }
                    Util.Dbg(8, this, fn, "Removed " + removal_indices.Count + " items from field " + fi.Name);
                }
                else if (Util.IsScalarType(ftype))
                {
                    Util.Dbg(8, this, fn, "Removing '" + referror.ToString() + "' field " + fi.Name + " = " + target.ToString());
                    fi.SetValue(referror, null);
                }
                else // assuming this has fields:
                {
                    Util.Dbg(8, this, fn, "Removing '" + referror.ToString() + "' field " + fi.Name + " = " + target.ToString());
                    fi.SetValue(referror, null);
                }

                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }


        /// <summary>
        /// Clear entire repository.
        /// </summary>
        /// <returns></returns>
        public virtual bool Clear()
        {
            String fn = MethodBase.GetCurrentMethod().Name;

            try
            {
                Contents.Clear();
                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }


        /// <summary>
        /// Find the typelist associated witha specific type.
        /// </summary>
        /// <param name="oType"></param>
        /// <returns></returns>
        protected virtual TypeList GetTypeList(Type oType)
        {
            foreach (TypeList tl in Contents)
            {
                if (tl.SubjectType.Equals(oType)) return (tl);
            }
            return (null);
        }

        /// <summary>
        /// Get all instances of a type known to the repository.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual List<Object> GetInstances(Type oType)
        {
            List<Object> deflist = new List<Object>();
            try
            {
                TypeList tl = GetTypeList(oType);
                if (null == tl) return (deflist);
                deflist = tl.Instances;
                return (deflist);
            }
            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                Util.HandleExc(this, fn, exc);
                return (deflist);
            }
        }

        public virtual List<T> GetInstances<T>()
        {
            TypeList tl = GetTypeList(typeof(T));
            if (tl == null)
                return new List<T>();

            List<T> outList = tl.Instances.Cast<T>().ToList();
            if (outList == null) { return new List<T>(); }

            return outList;
        }

        /// <summary>
        /// Get all instances of the specified type, as well as all derived types.
        /// </summary>
        /// <param name="oType"></param>
        /// <returns></returns>
        public virtual List<Object> GetInstancesIncludingDerived(Type oType)
        {
            List<Object> retlist = new List<Object>();
            try
            {
                List<Type> target_types = new List<Type>();
                foreach (TypeList tl in this.Contents)
                {
                    Type curtype = Type.GetType(tl.TypeName);
                    if (oType.IsAssignableFrom(curtype)) target_types.Add(curtype);
                }

                foreach (Type t in target_types)
                {
                    TypeList tl = GetTypeList(t);
                    if (null == tl) continue;
                    retlist.AddRange(tl.Instances);
                }
                return (retlist);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, "RepositoryCache.GetInstancesIncludingDerived()", exc);
                return (retlist);
            }
        }



        /// <summary>
        /// Find all references to a particular object in the repository.
        /// </summary>
        /// <param name="oTarget"></param>
        /// <returns></returns>
        public virtual List<ObjRefInfo> FindAllReferences(Object oTarget)
        {
            try
            {

                List<ObjRefInfo> retlist = new List<ObjRefInfo>();
                foreach (TypeList tl in this.Contents)
                {
                    foreach (object inst in tl.Instances)
                    {
                        if (!DeepFindRefs(retlist, inst, oTarget)) { break; }
                    }
                }
                return (retlist);


                //List<ObjRefInfo> buttonlist = new List<ObjRefInfo>();
                //Type targettype = oTarget.GetType();
                //foreach (TypeList tl in Contents)
                //{
                //    // Get the list of fields that COULD contain the object:
                //    List<FieldInfo> candidate_fields = new List<FieldInfo>();
                //    Type curtype = tl.SubjectType;
                //    foreach (FieldInfo fi in curtype.GetFields())
                //    {
                //        if ((fi.IsStatic)||(!fi.IsPublic)) continue;
                //        if (FieldCanHaveValue(fi, targettype)) { candidate_fields.Add(fi); }
                //        else if (Util.IsAggregateType(fi.FieldType)) 
                //        {
                //            foreach (object aggreg in tl.Instances)
                //            {
                //                DeepFindRefs(buttonlist, aggreg, oTarget);
                //            }
                //        } // bugfix #547

                //    }

                //    // If there are no such fields, ignore the type altogether:
                //    if (0==candidate_fields.Count) continue;

                //    // Now, we have one or more 'hits' in fields, so we check each candidate field for each instance:
                //    foreach (object oinst in tl.Instances)
                //    {
                //        foreach(FieldInfo instfield in candidate_fields)
                //        {
                //            if (FieldHasValue(oinst, instfield, oTarget))
                //            {
                //                ObjRefInfo newobjref = new ObjRefInfo(oinst, instfield, oTarget);
                //                buttonlist.Add(newobjref);
                //            } // endif(field has the value)
                //        } // end foreach(candidate field within instance)
                //    } // end foreach(instance)
                //} // end foreach(typelist)
                //return(buttonlist);
            } // end main try

            catch (Exception exc)
            {
                Util.HandleExc(this, "RepositoryCache.FindAllReferences()", exc);
                return (null);
            }
        }

        /// <summary>
        /// Perform a deep reference-find on an aggregate object.
        /// </summary>
        /// <param name="buttonlist"></param>
        /// <param name="aggreg"></param>
        /// <param name="oTarget"></param>
        /// <returns></returns>
        private bool DeepFindRefs(List<ObjRefInfo> oRetList, object oAggreg, object oTarget)
        {
            try
            {
                Stack<object> stack = new Stack<object>();
                return (DeepFindRefs(oRetList, oAggreg, oTarget, stack));
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, "RepositoryCache.DeepFindRefs()", exc);
                return (false);
            }
        }

        private bool DeepFindRefs(List<ObjRefInfo> oRetList, object oAggreg, object oTarget, Stack<object> oSearchStack)
        {
            oSearchStack.Push(oAggreg);
            try
            {
                HashSet<object> stack_copy = new HashSet<object>(oSearchStack.ToArray());

                // This is useful for testing specific use cases:
                //if (oAggreg.ToString().ToUpper().Contains("%CDN"))
                //{
                //    Util.Dbg(9, this, fn, "Found aggregate " + oAggreg.ToString());
                //}

                if (null == oAggreg) { return (true); }
                Type type = oAggreg.GetType();
                FieldInfo[] fields = type.GetFields();
                Type tgt_type = oTarget.GetType();
                foreach (FieldInfo fi in fields)
                {
                    if ((fi.IsStatic) || (!fi.IsPublic)) { continue; }
                    Type ftype = fi.FieldType;
                    object field_val = fi.GetValue(oAggreg);
                    if (null == field_val) { continue; }

                    // If this field could actually contain the target, we check that:
                    if (ftype.IsAssignableFrom(tgt_type))
                    {
                        if (field_val.Equals(oTarget)) { oRetList.Add(new ObjRefInfo(oAggreg, fi, oTarget)); }
                    }

                    // If the field is an aggregate itself, recurse (note that we allow both the previous case
                    // and this one, in case an object contains itself at some level):
                    if (Util.IsAggregateType(ftype))
                    {
                        if (null != oAggreg)
                        {
                            // Speed up DeepFindRefs (bug #3587) This MAY have reintroduced bug #716 based on the following comment from 2013
                            if (stack_copy.Contains(field_val))
                                return true;

                            /*
                            // Avoid infinite recursion (bug #716).   20130617 CDN:
                            for (int i = stack_copy.Length - 1; i > 0; i--)
                            {
                                if (stack_copy[i].Equals(field_val)) { return (true); }
                            } */

                            if (!DeepFindRefs(oRetList, field_val, oTarget, oSearchStack)) { return (false); }
                        }
                    } // end else(aggregate field)

                    // If it's a list, recurse on each element:
                    else if (typeof(IList).IsAssignableFrom(ftype))
                    {
                        IList ilist = fi.GetValue(oAggreg) as IList;
                        foreach (object element in ilist)
                        {
                            Type eltype = element.GetType();
                            if (!Util.IsAggregateType(eltype)) { break; }   // don't need to recurse on non-aggregate list elements

                            // Speed up DeepFindRefs (bug #3587) This MAY have reintroduced bug #716 based on the following comment from 2013
                            if (stack_copy.Contains(element))
                                return true;

                            /*
                            // Avoid infinite recursion (bug #716).   20130617 CDN:
                            for (int i = stack_copy.Length - 1; i > 0; i--)
                            {
                                if (stack_copy[i].Equals(element)) { return (true); }
                            } */



                            if (element.Equals(oTarget))
                            {
                                oRetList.Add(new ObjRefInfo(oAggreg, fi, oTarget));
                            }
                            else if (!DeepFindRefs(oRetList, element, oTarget, oSearchStack)) { return (false); }
                        }
                    }



                } // end foreach(field)

                return (true);
            } // end main try

            catch (Exception exc)
            {
                Util.HandleExc(this, "RepositoryCache.DeepFindRefs()", exc);
                return (false);
            }

            finally
            {
                oSearchStack.Pop();
            }
        } // end FindAllReferences()


        /// <summary>
        /// Can a field, in the abstract, have a specific value?
        /// </summary>
        /// <param name="oFldInfo"></param>
        /// <param name="oTarget"></param>
        /// <returns></returns>
        public virtual bool FieldCanHaveValue(FieldInfo oFldInfo, Object oTarget)
        {
            return (FieldCanHaveValue(oFldInfo, oTarget.GetType()));
        }

        /// <summary>
        /// Can a field, in the abstract, have a value of a specific type?  Seems like a silly question, but
        /// we need to handle both scalar and list cases.
        /// </summary>
        /// <param name="oFldInfo"></param>
        /// <param name="oTargetType"></param>
        /// <returns></returns>
        public virtual bool FieldCanHaveValue(FieldInfo oFldInfo, Type oTargetType)
        {
            if (oFldInfo == null)
            {
                throw new ArgumentNullException(nameof(oFldInfo));
            }

            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                Type ftype = oFldInfo.FieldType;

                // Simple type first - non-generic:
                if (!ftype.IsGenericType)
                {
                    return (ftype.IsAssignableFrom(oTargetType));
                }

                // Other possible case is a List<T> where T.IsAssignableFrom(our type):
                else if (typeof(IList).IsAssignableFrom(ftype))
                {
                    Type[] typeargs = ftype.GetGenericArguments();
                    if (typeargs[0].IsAssignableFrom(oTargetType)) return (true);
                }
                return (false);

            } // end main try
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="oInst"></param>
        /// <param name="oFldValue"></param>
        /// <param name="oTarget"></param>
        /// <returns></returns>
        public virtual bool FieldHasValue(Object oInst, FieldInfo oFieldInfo, Object oTarget)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                Type tgttype = oTarget.GetType();

                // Easy case first -- scalar comparison:
                Type ftype = oFieldInfo.FieldType;
                if (!ftype.IsGenericType)
                {
                    object oval = oFieldInfo.GetValue(oInst);
                    return (oTarget.Equals(oval));
                }

                // Now, the case List<T> where T.IsAssignableFrom(target-type):
                else if (typeof(IList).IsAssignableFrom(ftype))
                {
                    IList ilist = oFieldInfo.GetValue(oInst) as IList;
                    if (ilist.Contains(oTarget)) return (true);
                    else return (false);
                }

                return (false);

            } // end main try
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        } // end FieldHasValue()

        /// <summary>
        /// List of contents of this repository.
        /// </summary>
        public List<TypeList> Contents = new List<TypeList>();


        /// <summary>
        /// Get a list of all types known to this repository.
        /// </summary>
        /// <returns></returns>
        public List<Type> GetKnownTypes()
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            List<Type> retlist = new List<Type>();
            try
            {
                foreach (TypeList tl in this.Contents)
                {
                    retlist.Add(tl.SubjectType);
                }
                return (retlist);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (retlist);
            }
        }

        /// <summary>
        /// Clear all contents belonging to a specified (exact) type.
        /// </summary>
        /// <param name="oType"></param>
        /// <returns></returns>
        public virtual bool ClearInstancesOfType(Type oType)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                foreach (TypeList tl in this.Contents)
                {
                    if (tl.SubjectType == oType)
                    {
                        tl.Instances.Clear();
                        return (true);
                    }
                }
                return (false); // specified type not found.
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }

        public int GetTotalElementCount()
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                int n = 0;
                foreach (TypeList tl in this.Contents)
                {
                    n += tl.Instances.Count;
                }
                return (n);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (-1);
            }
        }

        public TypeList GetTypeList<T1>()
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                foreach (TypeList tl in this.Contents)
                {
                    if (typeof(T1).IsAssignableFrom(tl.SubjectType))
                    {
                        return (tl);
                    }
                }
                return (null);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (null);
            }
        }

        public int GetCount()
        {
            if (null == Contents) { return (0); }
            int totcnt = 0;
            foreach (TypeList tl in Contents)
            {
                if (null == tl.Instances) { continue; }
                totcnt += tl.Instances.Count;
            }
            return (totcnt);
        }
    } // end class





    /// <summary>
    /// Information on a reference to an object:  identity of the referror, plus info on the referring field.
    /// </summary>
    public class ObjRefInfo
    {

        public ObjRefInfo(Object oReferror, FieldInfo oFldInfo, Object oReferent)
        {
            Referror = oReferror;
            ReferringField = oFldInfo;
            Referent = oReferent;
        }


        public virtual String Prettyprint()
        {
            String sobj = (null != Referror) ? Referror.ToString() : "<null>";
            String sfield = (null != ReferringField) ? ReferringField.Name : "<none>";
            String sval = (null != Referent) ? Referent.ToString() : "<null>";
            String spp = sobj + "." + sfield + "=" + sval;
            return (spp);
        }


        /// <summary>
        /// Clear (i.e., set to default value) the reference.
        /// </summary>
        /// <returns></returns>
        public virtual bool Clear()
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {

                if (null == Referent) { return (Util.HandleAppErr(this, fn, "Can't clear reference - null Referent: " + Prettyprint())); }

                // If a list, delete all references:
                if (typeof(IList).IsAssignableFrom(ReferringField.FieldType))
                {
                    List<int> hit_indices = new List<int>();
                    IList ilist = ReferringField.GetValue(Referror) as IList;
                    if (null == ilist) { return (Util.HandleAppErr(this, fn, "Can't get IList for field " + ReferringField.Name)); }
                    for (int i = 0; i < ilist.Count; i++)
                    {
                        if (ilist[i].Equals(Referent))
                        {
                            hit_indices.Add(i);
                        }

                    }
                    for (int i = hit_indices.Count - 1; i >= 0; i--)
                    {
                        ilist.RemoveAt(hit_indices[i]);
                    }
                } // endif(list)

                // Otherwise, assume it's a value type:
                else
                {
                    Type ftype = ReferringField.FieldType;
                    if (!ftype.IsAssignableFrom(Referent.GetType()))
                    {
                        return (Util.HandleAppErr(this, fn, "Type mismatch: " + ftype.Name + " vs. "
                                    + Referent.GetType().Name));
                    }
                    // Don't know if this is nullable ... so we try it:
                    ReferringField.SetValue(Referror, null);

                } // end else(value type)

                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }

        /// <summary>
        /// The referring object.
        /// </summary>
        public object Referror = null;

        /// <summary>
        /// Information on the referring field.
        /// </summary>
        public FieldInfo ReferringField = null;

        /// <summary>
        /// The object to which the reference refers.
        /// </summary>
        public object Referent = null;
    }


    /// <summary>
    /// List of objects of a specified type.
    /// </summary>
    public class TypeList
    {
        public TypeList(Type _subjectType)
        {
            m_oSubjectType = _subjectType;
            TypeName = _subjectType.Name;
        }

        public TypeList()
        {
        }

        public override string ToString()
        {
            if (null == SubjectType) return ("TypeList(undefined)");
            return (SubjectType.Name);
        }

        public Type SubjectType
        {
            get
            {
                if (null == m_oSubjectType)
                {
                    m_oSubjectType = Type.GetType(TypeName);
                }
                return (m_oSubjectType);
            }
            set
            {
                TypeName = value.AssemblyQualifiedName;
                m_oSubjectType = value;

            }
        }

        private Type m_oSubjectType = null;

        public String TypeName = "";
        public List<Object> Instances = new List<Object>();
    }
}
