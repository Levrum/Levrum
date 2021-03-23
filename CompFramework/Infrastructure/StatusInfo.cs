using Levrum.Utils.Infra;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnalysisFramework.Infrastructure
{
    /// <summary>
    /// Information on an error or status.
    /// </summary>
    public class StatusInfo
    {
        public Sev Severity = Sev.Success;

        public bool IsOk
        { get { return (Severity <= Sev.Info); } }

        public string Message = "";

        public object Subject = null;

        /// <summary>
        /// Convenience function for generating a simple error as a status list, inline.
        /// </summary>
        /// <param name="sContext"></param>
        /// <param name="sMessage"></param>
        /// <returns></returns>
        public static List<StatusInfo> MakeError(string sContext, string sMessage)
        {
            StatusInfo si = new StatusInfo();
            si.Message = sMessage;
            si.Severity = Sev.AppError;
            List<StatusInfo> sis = new List<StatusInfo>();
            sis.Add(si);
            return (sis);
        }
    }
}
