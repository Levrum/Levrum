using Levrum.Utils.Infra;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnalysisFramework.Infrastructure
{
    public class Api
    {
        public IEnumerable<Domain> GetDomains()
        {
            const string fn = "Api.GetDomains()";
            try
            {
                List<Domain> retlist = new List<Domain>();
                Util.HandleAppErrOnce(this, fn, "Not implemented yet");
                return (retlist);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (null);
            }
        } // end method()


        /// <summary>
        /// Register a domain.
        /// </summary>
        /// <param name="oDomain">Information about the dmoain to be registered</param>
        /// <param name="bOverwrite">Should this domain be overwritten, if it already exists?</param>
        /// <returns></returns>
        public virtual bool Remember(Domain oDomain, bool bOverwrite)
        {
            const string fn = "Api.GetDomains()";
            try
            {
                Util.HandleAppErrOnce(this, fn, "Not implemented yet");
                return (false);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }

        }

        /// <summary>
        /// De-register a domain.
        /// </summary>
        /// <param name="sDomainName">Name of domain to be de-registered</param>
        /// <returns></returns>
        public virtual bool Forget(string sDomainName)
        {
            const string fn = "Api.GetDomains()";
            try
            {
                Util.HandleAppErrOnce(this, fn, "Not implemented yet");
                return (false);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }

        }


    }
}
