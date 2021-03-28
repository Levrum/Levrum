using Levrum.Utils.Infra;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AnalysisFramework.Infrastructure
{
    public abstract class Domain : NamedObj
    {


        /// <summary>
        /// Retrieves display types that are valid in this domain.   Types returned should be derived from Display.
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<Type> GetValidDisplayTypes();


        /// <summary>
        /// Retrieves instances of a specified type, when the type is known at compile time.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual IEnumerable<T> GetInstances<T>()
            where T : NamedObj
        {
            const string fn = "Domain.GetInstances()";
            List<T> retlist = new List<T>();
            try
            {
                Type oType = typeof(T);
                if (this.m_oRepo.ContainsType(oType)) { m_oRepo.GetInstances(oType); }  // If it's a type known to the repo, we return that;

                //return (GetInstancesCustom(oType));
                return (null);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (retlist);
            }
        }

        /// <summary>
        /// Retrieves instances of a specified type, when the type is not known at compile time.   
        /// The specified type must be derived from NamedObj.
        /// </summary>
        /// <param name="oType"></param>
        /// <returns></returns>
        public virtual IEnumerable GetInstances(Type oType)
        {
            const string fn = "Domain.GetInstances()";
            IList retlist = new List<object>();
            try
            {
                if (this.m_oRepo.ContainsType(oType)) { m_oRepo.GetInstances(oType); }  // If it's a type known to the repo, we return that;

                //return (GetInstancesCustom(oType));
                return (null);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (retlist);
            }
        }



        
        /// <summary>
        /// Repository associated with this domain.
        /// </summary>
        public Repository Repo { get { return (m_oRepo); } }


        private Repository m_oRepo = new Repository();
    }
}
