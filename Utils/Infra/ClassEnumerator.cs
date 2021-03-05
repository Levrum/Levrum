using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Levrum.Utils.Infra
{
    /// <summary>
    /// Class that enumerates classes known within the currently executing assembly.
    /// </summary>
    public static class ClassEnumerator
    {
        public static List<Type> FindClasses(Type oParentClass, Type oTargetAttribute = null)
        {
            const string fn = "ClassEnumerator.FindClasses()";
            List<Type> retlist = new List<Type>();
            try
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (!oParentClass.IsAssignableFrom(type)) { continue; }
                        if (null != oTargetAttribute)
                        {
                            if (!typeof(Attribute).IsAssignableFrom(oTargetAttribute))
                            {
                                Util.HandleAppErrOnce(typeof(ClassEnumerator), fn,"Filtering type '" + oTargetAttribute.Name + "' is not an attribute");
                                return(retlist);
                            }
                            Attribute att = type.GetCustomAttribute(oTargetAttribute);
                            if (null == att) { continue; }
                        } // endif(target attribute supplied)
                        retlist.Add(type);  // if we get here, we want this one!
                    } // end foreach(type in assembly)
                } // end foreach(known assembly)
                return(retlist);
            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(ClassEnumerator),fn,exc);
                return(retlist);
            }
        }


        /// <summary>
        /// Generate an instance of each class in the current AppDomain
        /// that (a) derives from a specific type and (b) has an argumentless constructor.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable GetInstancesOfClasses<T>()
            where T : class
	    {
			const string fn = "ClassEnumerator.GetInstancesOfAll()";
		    List<T> results = new List<T>();
			try
			{
				foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					foreach (Type type in assembly.GetTypes())
					{
						if (!typeof(T).IsAssignableFrom(type)) { continue; }
						if (type.IsAbstract) { continue; }
						if (!type.IsClass) { continue; }
                        ConstructorInfo ci = type.GetConstructor(new Type[] { }); // look for default ctor
						if (null == ci)
						{
							Util.HandleAppErrOnce(typeof(T),fn,"Type " + type.Name + " has no default constructor");
							continue;
						}
						object newobj = ci.Invoke(new object[] { });
                        T c3lo = newobj as T;
						if (null == c3lo)
						{
							Util.HandleAppErrOnce(typeof(T), fn, "Unable to convert new instance to type " + type.Name);
							continue;
						}
						results.Add(c3lo);


					} // end foreach(type)
				} // end foreach(assembly)

				return(results);
			}
			catch (Exception exc)
			{
				Util.HandleExc(typeof(T),fn,exc);
				return(results);
			}
		}

    }
}
