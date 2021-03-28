using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace AnalysisFramework.Infrastructure
{
    /// <summary>
    /// Class capable of storing and retrieving named objects.  Names are assumed unique *per object type.*
    /// </summary>
    public class Repository
    {

        /// <summary>
        /// Insert an object into, or update its contents within, the repository.
        /// </summary>
        /// <param name="oDatum"></param>
        /// <returns></returns>
        public virtual bool Set(NamedObj oDatum)
        {
            return (false);

        }


        public virtual bool ContainsType<T>()
            where T : NamedObj
        {
            if (null==m_oDict) { return (false); }
            return (m_oDict.ContainsKey(typeof(T)));
        }

        public virtual bool ContainsType(Type oType)
        {
            if (null==m_oDict) { return (false); }
            return (m_oDict.ContainsKey(oType));
        }

        /// <summary>
        /// Retrieve all instances of a specific type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual IEnumerable<T> GetInstances<T>()
            where T : NamedObj
        {
            return (null);
        }

        /// <summary>
        /// Retrieve all instances of type, when type isn't known at compile time.
        /// </summary>
        /// <param name="oType"></param>
        /// <returns></returns>
        public virtual IEnumerable GetInstances(Type oType)
        {
            return (null);
        }


        public virtual NamedObj Get<T>(string sName)
            where T : NamedObj
        {
            return (null);

        }

        public virtual bool Load(string sPath)
        {
            return (false);
        }

        public virtual bool Save(string sPath)
        {
            return (false);
        }


        /// <summary>
        /// Repository data structure -- Decodes type in to a dictionary of names-to-objects.
        /// </summary>
        private Dictionary<Type, Dictionary<string, object>> m_oDict = new Dictionary<Type, Dictionary<string, object>>();

    }
}
