using Levrum.Utils.Infra;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnalysisFramework.Infrastructure
{
    public class Domain : NamedObj
    {
        /// <summary>
        /// How this domain is to be accessed (e.g., URL).
        /// </summary>
        public string AccessPath = "";


        public IEnumerable<Type> GetTypes()
        {
            const string fn = "Domain.GetTypes()";
            try
            {
                Util.HandleAppErrOnce(this, fn, "Not implemented yet");
                return (null);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (null);
            }

        }

    }
}
